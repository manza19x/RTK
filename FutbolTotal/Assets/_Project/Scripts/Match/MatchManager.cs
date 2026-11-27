using System;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Sistema central de reglas del partido: estados, cronómetro, goles, saques, descanso, final y pausa.
    /// Es el único que decide cuándo se juega y cuándo no; el resto de sistemas reaccionan a sus eventos
    /// o consultan su estado (<see cref="IMatchStatus"/>).
    ///
    /// Flujo: PreMatch → Kickoff → Playing ⇄ (SetPiece | GoalCelebration → Kickoff) → HalfTime → Kickoff → … → FullTime.
    /// Simplificaciones declaradas: el reloj se para fuera de juego (estilo arcade), no hay tiempo añadido,
    /// fueras de juego ni faltas, y los saques de banda se hacen con el pie.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class MatchManager : MonoBehaviour, IMatchStatus, IMatchControl
    {
        [SerializeField] private MatchSettings settings;
        [SerializeField] private HumanTeamController human;
        [SerializeField] private GoalZone[] goals;
        [SerializeField] private MatchCamera matchCamera;

        private MatchContext context;
        private int initialHomeDirection = 1;
        private Team firstHalfKickoffTeam;
        private Team lastScorer;
        private ReplaySystem replay;
        private float stateEndsAt;
        private float aiRestartAt;
        private bool humanInputAllowed;

        public MatchState State { get; private set; } = MatchState.PreMatch;
        public int Half { get; private set; } = 1;
        /// <summary>Segundos reales jugados en la parte actual.</summary>
        public float HalfElapsed { get; private set; }
        public int HomeGoals { get; private set; }
        public int AwayGoals { get; private set; }
        public bool IsPaused { get; private set; }
        public string Message { get; private set; }
        public float MessageUntil { get; private set; }
        public MessageStyle MessageStyle { get; private set; }
        public RestartDecision CurrentRestart { get; private set; }
        public MatchSettings Settings => settings;
        public bool IsBallInPlay => State == MatchState.Playing && !IsPaused;

        public event Action<MatchState> StateChanged;
        public event Action<Team> GoalScored;
        public event Action MatchEnded;
        /// <summary>Se lanza al empezar (o reiniciar) un partido, antes del saque inicial.</summary>
        public event Action MatchStarted;

        /// <summary>Segundos reales por parte: MatchSettings × la duración elegida en Opciones.</summary>
        public float HalfDuration => settings.halfDurationSeconds * GameOptions.MatchLengthMultiplier;

        /// <summary>Minuto de partido al estilo de la televisión (1 a 90).</summary>
        public int CurrentMinute => Mathf.Clamp(Mathf.FloorToInt(DisplaySeconds / 60f) + 1, 1, settings.displayedHalfMinutes * 2);

        private float DisplaySeconds
        {
            get
            {
                float halfSeconds = settings.displayedHalfMinutes * 60f;
                float progress = Mathf.Clamp01(HalfElapsed / Mathf.Max(1f, HalfDuration));
                return (Half - 1) * halfSeconds + progress * halfSeconds;
            }
        }

        // ------------------------------------------------------------------ IMatchStatus

        public string ClockText
        {
            get
            {
                int total = Mathf.FloorToInt(DisplaySeconds);
                return $"{total / 60:00}:{total % 60:00}";
            }
        }

        public string PhaseText
        {
            get
            {
                switch (State)
                {
                    case MatchState.PreMatch: return "Previa";
                    case MatchState.HalfTime: return "Descanso";
                    case MatchState.FullTime: return "Final";
                    default: return Half == 1 ? "1ª parte" : "2ª parte";
                }
            }
        }

        // ------------------------------------------------------------------ Ciclo de vida

        private void Awake()
        {
            settings = SettingsProvider.Resolve(settings, SettingsProvider.MatchName, this);
            MatchContext.Current?.RegisterRules(this);
        }

        private void Start()
        {
            context = MatchContext.Current;
            if (context == null || human == null)
            {
                Debug.LogError("[Fútbol Total] MatchManager necesita un MatchContext en la escena y el HumanTeamController.", this);
                enabled = false;
                return;
            }

            initialHomeDirection = context.HomeTeam.AttackDirection;
            replay = UnityEngine.Object.FindFirstObjectByType<ReplaySystem>();
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Scored += OnGoal;
            }
            context.Ball.Kicked += OnBallKicked;
            context.FoulCommitted += OnFoul;
            EnterPreMatch();
        }

        private void OnDestroy()
        {
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Scored -= OnGoal;
            }
            if (context != null && context.Ball != null) context.Ball.Kicked -= OnBallKicked;
            if (context != null) context.FoulCommitted -= OnFoul;
            if (IsPaused) Time.timeScale = 1f;
        }

        private void Update()
        {
            FootballInput input = human.Input;
            bool canPause = State != MatchState.PreMatch && State != MatchState.FullTime;
            if (canPause && input.Pause.WasPressedThisFrame()) SetPaused(!IsPaused);
            if (IsPaused) return;

            switch (State)
            {
                case MatchState.PreMatch:
                    if (input.ShortPass.ReleasedThisFrame) StartMatch();
                    break;

                case MatchState.Playing:
                    HalfElapsed += Time.deltaTime;
                    if (HalfElapsed >= HalfDuration) EndHalf();
                    break;

                case MatchState.Kickoff:
                case MatchState.SetPiece:
                    UpdateRestart();
                    break;

                case MatchState.GoalCelebration:
                    // Si hay repetición, se espera a que termine (o a que el jugador la salte).
                    if (Time.time >= stateEndsAt && (replay == null || !replay.IsBusy)) AfterGoal();
                    break;

                case MatchState.HalfTime:
                    if (Time.time >= stateEndsAt) StartSecondHalf();
                    break;

            }
        }

        private void FixedUpdate()
        {
            if (State != MatchState.Playing || context == null) return;
            if (RestartRules.TryDecide(context.Ball, context, out RestartDecision decision))
            {
                BeginRestart(decision, MatchState.SetPiece);
            }
        }

        // ------------------------------------------------------------------ Inicio, partes y final

        private void EnterPreMatch()
        {
            SetState(MatchState.PreMatch);
            Half = 1;
            HalfElapsed = 0f;
            context.HomeTeam.ResetToKickoffPositions();
            context.AwayTeam.ResetToKickoffPositions();
            context.Ball.ResetTo(context.Pitch.Center);
            LockPlayers(true);
            SetHumanInput(false);
            ShowMessage($"{context.HomeTeam.Settings.teamName} vs {context.AwayTeam.Settings.teamName}\n{context.HomeTeam.Settings.stadium}\nPulsa J / A para empezar", float.PositiveInfinity, MessageStyle.Banner);
            if (matchCamera != null) matchCamera.SnapToTarget();
        }

        /// <summary>Empieza el partido (lo llamará también el menú principal en la Fase 4).</summary>
        public void StartMatch()
        {
            firstHalfKickoffTeam = context.HomeTeam;
            MatchStarted?.Invoke();
            BeginKickoff(firstHalfKickoffTeam);
        }

        /// <summary>Vuelve a jugar desde 0-0 con los equipos en su campo inicial.</summary>
        public void RestartMatch()
        {
            if (IsPaused) SetPaused(false);
            HomeGoals = 0;
            AwayGoals = 0;
            Half = 1;
            HalfElapsed = 0f;
            context.HomeTeam.SetAttackDirection(initialHomeDirection);
            context.AwayTeam.SetAttackDirection(-initialHomeDirection);
            StartMatch();
        }

        private void EndHalf()
        {
            if (Half == 1)
            {
                SetState(MatchState.HalfTime);
                StopPlay();
                stateEndsAt = Time.time + settings.halfTimeSeconds;
                ShowMessage($"Descanso\n{ScoreLine()}", settings.halfTimeSeconds, MessageStyle.Banner);
            }
            else
            {
                EndMatch();
            }
        }

        private void StartSecondHalf()
        {
            Half = 2;
            HalfElapsed = 0f;
            // Cambio de campo.
            context.HomeTeam.SetAttackDirection(-context.HomeTeam.AttackDirection);
            context.AwayTeam.SetAttackDirection(-context.AwayTeam.AttackDirection);
            BeginKickoff(firstHalfKickoffTeam.Opponent);
            string direction = human.Team.AttackDirection > 0 ? "la derecha" : "la izquierda";
            ShowMessage($"Segunda parte\nCambio de campo: ahora atacas hacia {direction}", settings.messageSeconds + 1f, MessageStyle.Banner);
        }

        private void EndMatch()
        {
            SetState(MatchState.FullTime);
            StopPlay();
            ShowMessage($"Final\n{ScoreLine()}", float.PositiveInfinity, MessageStyle.Banner);
            MatchEnded?.Invoke();
        }

        /// <summary>Detiene el juego: balón al centro, jugadores quietos y sin control humano.</summary>
        private void StopPlay()
        {
            context.Ball.ResetTo(context.Pitch.Center);
            LockPlayers(true);
            SetHumanInput(false);
        }

        // ------------------------------------------------------------------ Goles

        private void OnGoal(GoalZone goal)
        {
            if (State != MatchState.Playing) return;

            Team scorer = context.TeamAttacking(goal.GoalSide);
            lastScorer = scorer;
            if (scorer.Side == TeamSide.Home) HomeGoals++;
            else AwayGoals++;

            SetState(MatchState.GoalCelebration);
            LockPlayers(true);
            SetHumanInput(false);
            stateEndsAt = Time.time + settings.goalCelebrationSeconds;
            ShowMessage($"¡GOL de {scorer.Settings.teamName}!\n{ScoreLine()}", settings.goalCelebrationSeconds, MessageStyle.Goal);
            GoalScored?.Invoke(scorer);
        }

        private void AfterGoal()
        {
            if (HalfElapsed >= HalfDuration)
            {
                EndHalf();
                return;
            }
            // Saca el equipo que ha encajado.
            BeginKickoff(lastScorer.Opponent);
        }

        // ------------------------------------------------------------------ Faltas

        /// <summary>Falta: penalti si ocurre dentro del área del equipo que la comete; si no, tiro libre.</summary>
        private void OnFoul(FoulEvent foul)
        {
            if (State != MatchState.Playing) return;

            Team awarded = foul.Victim.Team;
            int offenderGoalSide = -foul.Offender.Team.AttackDirection;
            bool penalty = context.Pitch.IsInPenaltyArea(foul.Position, offenderGoalSide);
            RestartDecision decision = penalty
                ? RestartRules.Penalty(context, awarded, foul.Victim)
                : RestartRules.FreeKick(context, awarded, foul.Position, foul.Victim);

            BeginRestart(decision, MatchState.SetPiece);

            string who = $"{foul.Offender.Team.Settings.shortName} {foul.Offender.ShirtNumber}";
            string action = decision.Team == human.Team
                ? (penalty ? "apunta y tira con K" : "apunta y pasa (J / L) o tira (K)")
                : $"para {decision.Team.Settings.teamName}";
            ShowMessage(penalty
                ? $"¡PENALTI!  Falta de {who} ({foul.Describe()})\n{action}"
                : $"Falta de {who} ({foul.Describe()})\nTiro libre: {action}", settings.messageSeconds + 1f);
        }

        /// <summary>
        /// Colocación del penalti: el portero defensor en su línea y el resto de jugadores fuera del área,
        /// por detrás del punto de penalti.
        /// </summary>
        private void ArrangePenalty(RestartDecision decision)
        {
            Pitch pitch = context.Pitch;
            int goalSide = decision.Team.AttackDirection;
            Team defending = decision.Team.Opponent;
            float waitX = pitch.Center.x + goalSide * (pitch.HalfLength - pitch.PenaltyAreaDepth - 2f);

            int index = 0;
            foreach (Team team in new[] { decision.Team, defending })
            {
                foreach (FootballPlayer player in team.Players)
                {
                    if (player == decision.Taker) continue;
                    if (player == defending.Goalkeeper)
                    {
                        Vector3 line = pitch.GoalCenter(goalSide) - new Vector3(goalSide * 0.3f, 0f, 0f);
                        player.Motor.Teleport(line, new Vector3(-goalSide, 0f, 0f));
                        continue;
                    }
                    if (player.IsGoalkeeper) continue; // El portero del equipo que tira se queda en su portería.

                    bool tooClose = pitch.IsInPenaltyArea(player.Position, goalSide) ||
                                    KickMath.FlatDistance(player.Position, decision.Spot) < pitch.CenterCircleRadius;
                    if (!tooClose) continue;

                    float z = pitch.Center.z + ((index % 8) - 3.5f) * 3f;
                    float x = waitX - goalSide * (index / 8) * 2f;
                    player.Motor.Teleport(new Vector3(x, 0f, z), new Vector3(goalSide, 0f, 0f));
                    index++;
                }
            }
        }

        // ------------------------------------------------------------------ Saques

        private void BeginKickoff(Team team)
        {
            context.HomeTeam.ResetToKickoffPositions();
            context.AwayTeam.ResetToKickoffPositions();
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Rearm();
            }
            BeginRestart(RestartRules.Kickoff(context, team), MatchState.Kickoff);
            if (matchCamera != null) matchCamera.SnapToTarget();
        }

        /// <summary>
        /// Prepara un saque: coloca balón y lanzador, aparta a los rivales y bloquea el movimiento de todos.
        /// El juego se reanuda en cuanto el lanzador golpea el balón (<see cref="OnBallKicked"/>).
        /// </summary>
        private void BeginRestart(RestartDecision decision, MatchState state)
        {
            if (decision.Taker == null)
            {
                Debug.LogWarning("[Fútbol Total] No hay lanzador para el saque; se coloca el balón y se sigue jugando.", this);
                context.Ball.ResetTo(decision.Spot);
                ResumePlay();
                return;
            }

            CurrentRestart = decision;
            SetState(state);

            Ball ball = context.Ball;
            FootballPlayer taker = decision.Taker;
            float offset = taker.Motor.BodyRadius + ball.Radius + ball.Settings.dribbleDistanceJog;

            ball.ResetTo(decision.Spot);
            taker.Motor.Teleport(decision.Spot - decision.Facing * offset, decision.Facing);
            float clearance = decision.Type == RestartType.Kickoff
                ? context.Pitch.CenterCircleRadius + 0.5f
                : settings.restartClearance;
            ClearOpponents(decision.Team.Opponent, decision.Spot, clearance);
            if (decision.Type == RestartType.Penalty) ArrangePenalty(decision);
            ball.SetOwner(taker);
            LockPlayers(true);

            bool humanTakes = decision.Team == human.Team;
            human.SwitchingEnabled = !humanTakes;
            if (humanTakes)
            {
                human.SetActivePlayer(taker);
                SetHumanInput(true);
                ShowMessage($"{RestartRules.Describe(decision.Type)}: apunta y pasa con J / L", settings.messageSeconds);
            }
            else
            {
                SetHumanInput(false);
                aiRestartAt = Time.time + settings.aiRestartDelay;
                ShowMessage($"{RestartRules.Describe(decision.Type)} para {decision.Team.Settings.teamName}", settings.messageSeconds);
            }
        }

        private void UpdateRestart()
        {
            RestartDecision restart = CurrentRestart;
            if (restart.Team != human.Team && Time.time >= aiRestartAt)
            {
                aiRestartAt = float.PositiveInfinity;
                if (!AutoRestart.Take(restart.Taker, restart.Type, context)) ResumePlay();
            }
            else if (!context.Ball.IsFree && context.Ball.Owner != restart.Taker)
            {
                ResumePlay(); // Por seguridad: si otro jugador tiene el balón, el saque ya no tiene sentido.
            }
        }

        private void OnBallKicked(KickEvent kick)
        {
            bool restarting = State == MatchState.Kickoff || State == MatchState.SetPiece;
            if (restarting && kick.Kicker == CurrentRestart.Taker) ResumePlay();
        }

        private void ResumePlay()
        {
            SetState(MatchState.Playing);
            LockPlayers(false);
            human.SwitchingEnabled = true;
            SetHumanInput(true);
        }

        /// <summary>Aparta a los rivales que estén demasiado cerca del punto de saque.</summary>
        private void ClearOpponents(Team opponents, Vector3 spot, float distance)
        {
            Pitch pitch = context.Pitch;
            foreach (FootballPlayer player in opponents.Players)
            {
                Vector3 away = KickMath.Flat(player.Position - spot);
                if (away.magnitude >= distance) continue;
                if (away.sqrMagnitude < 0.01f) away = new Vector3(-opponents.Opponent.AttackDirection, 0f, 0f);
                Vector3 target = pitch.ClampToPlayingArea(spot + away.normalized * distance, 0.5f);
                player.Motor.Teleport(target, KickMath.Flat(spot - target));
            }
        }

        // ------------------------------------------------------------------ Pausa

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            ApplyHumanInput();
            // Al pausar y al reanudar se descartan cargas: soltar un botón al volver no dispara nada.
            human.CancelPendingInput();
        }

        // ------------------------------------------------------------------ Utilidades

        private void SetState(MatchState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private void LockPlayers(bool locked)
        {
            context.HomeTeam.SetMovementLocked(locked);
            context.AwayTeam.SetMovementLocked(locked);
        }

        private void SetHumanInput(bool allowed)
        {
            humanInputAllowed = allowed;
            ApplyHumanInput();
            // Evita que la misma pulsación que cambia de estado (p. ej. empezar) dispare además una acción.
            human.CancelPendingInput();
        }

        private void ApplyHumanInput() => human.InputEnabled = humanInputAllowed && !IsPaused;

        private void ShowMessage(string text, float seconds, MessageStyle style = MessageStyle.Toast)
        {
            MessageStyle = style;
            Message = text;
            MessageUntil = float.IsPositiveInfinity(seconds) ? float.PositiveInfinity : Time.unscaledTime + seconds;
        }

        private string ScoreLine() =>
            $"{context.HomeTeam.Settings.shortName} {HomeGoals} - {AwayGoals} {context.AwayTeam.Settings.shortName}";
    }
}
