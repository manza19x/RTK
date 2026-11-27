using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Coordinador de la IA de un equipo. Reparte los roles para que el equipo juegue junto:
    /// - con balón: el conductor decide y el resto apoya o se desmarca;
    /// - balón suelto: UN jugador va a por él (el receptor si era un pase dirigido);
    /// - sin balón: UN jugador presiona al conductor y el resto defiende su zona marcando al rival más cercano.
    /// Así nunca persiguen todos el balón. Los roles se recalculan cada <see cref="AISettings.thinkInterval"/>
    /// y siempre que cambia la posesión.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Team))]
    public class TeamAI : MonoBehaviour
    {
        [SerializeField] private AISettings settings;

        private readonly List<PlayerBrain> brains = new List<PlayerBrain>();
        private readonly HashSet<FootballPlayer> claimedMarks = new HashSet<FootballPlayer>();
        private Team team;
        private HumanTeamController human;
        private float nextThink;
        private FootballPlayer lastOwner;

        public AISettings Settings => settings;
        public Team Team => team;
        public IReadOnlyList<PlayerBrain> Brains => brains;
        /// <summary>True si este equipo tiene un jugador humano (sus compañeros contienen en vez de entrar).</summary>
        public bool IsHumanTeam => human != null;
        /// <summary>True si el equipo tiene el balón o fue el último en tocarlo.</summary>
        public bool Attacking { get; private set; }

        /// <summary>La IA solo actúa con el balón en juego (no durante saques, celebraciones ni pausas).</summary>
        public bool CanAct
        {
            get
            {
                var context = MatchContext.Current;
                return context != null && (context.Rules == null || context.Rules.IsBallInPlay);
            }
        }

        private void Awake()
        {
            settings = SettingsProvider.Resolve(settings, SettingsProvider.AIName, this);
            team = GetComponent<Team>();
            human = GetComponent<HumanTeamController>();

            // El equipo rival usa una copia ajustada a la dificultad elegida (el asset original no se toca).
            if (!IsHumanTeam)
            {
                settings = Instantiate(settings);
                ownsSettingsCopy = true;
                DifficultyScaling.Apply(settings, GameOptions.Difficulty);
            }
        }

        private bool ownsSettingsCopy;

        private void OnDestroy()
        {
            if (ownsSettingsCopy && settings != null) Destroy(settings);
        }

        private void Start()
        {
            foreach (FootballPlayer player in team.Players)
            {
                PlayerBrain brain = player.GetComponent<PlayerBrain>();
                if (brain == null) brain = player.gameObject.AddComponent<PlayerBrain>();
                brain.Initialize(this);
                brains.Add(brain);
            }
        }

        private void Update()
        {
            var context = MatchContext.Current;
            if (context == null) return;
            FootballPlayer owner = context.Ball.Owner;
            if (owner != lastOwner || Time.time >= nextThink)
            {
                lastOwner = owner;
                nextThink = Time.time + settings.thinkInterval;
                AssignRoles(context);
            }
        }

        private void AssignRoles(MatchContext context)
        {
            Ball ball = context.Ball;
            FootballPlayer owner = ball.Owner;
            Attacking = owner != null ? owner.Team == team : ball.LastToucher != null && ball.LastToucher.Team == team;

            FootballPlayer chaser = owner == null ? PickChaser(context) : null;
            FootballPlayer presser = owner != null && owner.Team != team && !AIQueries.IsProtectedKeeper(owner, context.Pitch)
                ? PickPresser(owner)
                : null;

            foreach (PlayerBrain brain in brains)
            {
                FootballPlayer player = brain.Player;
                AIRole role;
                if (player.IsGoalkeeper) role = AIRole.Goalkeeper;
                else if (player == owner) role = AIRole.BallCarrier;
                else if (player == chaser) role = AIRole.Chase;
                else if (player == presser) role = IsHumanTeam ? AIRole.Contain : AIRole.Press;
                else if (Attacking) role = AIRole.Support;
                else role = AIRole.Zone;
                brain.SetRole(role);
            }

            AssignMarks(ball);
        }

        /// <summary>Balón suelto: el receptor de un pase dirigido o el jugador que antes llega.</summary>
        private FootballPlayer PickChaser(MatchContext context)
        {
            Ball ball = context.Ball;
            if (ball.LastKick.HasValue && ball.TimeSinceKick < 2.5f)
            {
                FootballPlayer receiver = ball.LastKick.Value.IntendedReceiver;
                if (receiver != null && receiver.Team == team)
                {
                    return receiver.IsHumanControlled ? null : receiver;
                }
            }

            FootballPlayer best = null;
            float bestTime = float.MaxValue;
            foreach (FootballPlayer player in team.Players)
            {
                if (player.IsGoalkeeper) continue; // El portero decide por su cuenta si sale.
                Vector3 point = AIQueries.InterceptPoint(player, ball, context.Pitch);
                float time = KickMath.FlatDistance(player.Position, point) / Mathf.Max(1f, player.Motor.SprintSpeed);
                if (time < bestTime)
                {
                    bestTime = time;
                    best = player;
                }
            }
            // Si quien mejor llega es el jugador humano, ningún compañero de la IA va también.
            return best != null && best.IsHumanControlled ? null : best;
        }

        /// <summary>El jugador de campo más cercano al conductor rival presiona.</summary>
        private FootballPlayer PickPresser(FootballPlayer owner)
        {
            if (IsHumanTeam && human.ActivePlayer != null &&
                KickMath.FlatDistance(human.ActivePlayer.Position, owner.Position) < settings.humanHelpDistance)
            {
                return null; // El humano ya presiona; los demás guardan la posición.
            }

            FootballPlayer best = null;
            float bestDistance = float.MaxValue;
            foreach (FootballPlayer player in team.Players)
            {
                if (player.IsGoalkeeper || player.IsHumanControlled) continue;
                float distance = KickMath.FlatDistance(player.Position, owner.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = player;
                }
            }
            return best;
        }

        /// <summary>Cada defensor de zona marca al rival libre más cercano a su zona (sin repetir marcas).</summary>
        private void AssignMarks(Ball ball)
        {
            claimedMarks.Clear();
            foreach (PlayerBrain brain in brains)
            {
                brain.MarkTarget = null;
                if (brain.Role != AIRole.Zone) continue;

                Vector3 zone = FormationShape.Target(brain.Player, ball.Position, false, settings);
                FootballPlayer best = null;
                float bestDistance = settings.markRadius;
                foreach (FootballPlayer opponent in team.Opponent.Players)
                {
                    if (opponent.IsGoalkeeper || opponent == ball.Owner || claimedMarks.Contains(opponent)) continue;
                    float distance = KickMath.FlatDistance(opponent.Position, zone);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = opponent;
                    }
                }
                if (best != null)
                {
                    claimedMarks.Add(best);
                    brain.MarkTarget = best;
                }
            }
        }
    }
}
