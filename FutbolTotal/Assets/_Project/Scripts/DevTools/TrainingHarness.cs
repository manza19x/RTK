using System.Collections;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Herramienta de la Fase 1 (entrenamiento): coloca a los equipos, cuenta goles, permite pausar y recolocar el balón.
    /// NO es el sistema de reglas del partido: en la Fase 2 lo sustituye MatchManager (cronómetro, saques, final).
    /// </summary>
    public class TrainingHarness : MonoBehaviour
    {
        [SerializeField] private HumanTeamController human;
        [SerializeField] private GoalZone[] goals;
        [SerializeField] private MatchCamera matchCamera;
        [SerializeField] private float celebrationSeconds = 2f;

        private bool resetting;

        public int HomeGoals { get; private set; }
        public int AwayGoals { get; private set; }
        public bool IsPaused { get; private set; }
        public string Message { get; private set; }
        public float MessageUntil { get; private set; }

        private void Start()
        {
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Scored += OnGoal;
            }
            ResetPositions();
        }

        private void OnDestroy()
        {
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Scored -= OnGoal;
            }
            if (IsPaused) Time.timeScale = 1f;
        }

        private void Update()
        {
            FootballInput input = human != null ? human.Input : null;
            if (input == null) return;

            if (input.Pause.WasPressedThisFrame()) SetPaused(!IsPaused);
            if (IsPaused || resetting) return;

            if (input.DebugReset.WasPressedThisFrame()) PlaceBallAtActivePlayer();
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            human.InputEnabled = !paused && !resetting;
            // Al pausar y al reanudar se descartan cargas: soltar un botón al volver no dispara nada.
            human.CancelPendingInput();
        }

        private void OnGoal(GoalZone goal)
        {
            var context = MatchContext.Current;
            Team scorer = context.TeamAttacking(goal.GoalSide);
            if (scorer.Side == TeamSide.Home) HomeGoals++;
            else AwayGoals++;
            ShowMessage($"¡GOL de {scorer.Settings.teamName}!", celebrationSeconds);
            Debug.Log($"[Fútbol Total] Gol de {scorer.Settings.teamName}. {HomeGoals}-{AwayGoals}");
            StartCoroutine(ResetAfterGoal());
        }

        private IEnumerator ResetAfterGoal()
        {
            resetting = true;
            human.InputEnabled = false;
            yield return new WaitForSeconds(celebrationSeconds);
            ResetPositions();
            human.InputEnabled = !IsPaused;
            resetting = false;
        }

        public void ResetPositions()
        {
            var context = MatchContext.Current;
            if (context == null) return;
            context.HomeTeam.ResetToKickoffPositions();
            context.AwayTeam.ResetToKickoffPositions();
            context.Ball.ResetTo(context.Pitch.Center);
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Rearm();
            }
            human.CancelPendingInput();
            if (matchCamera != null) matchCamera.SnapToTarget();
        }

        private void PlaceBallAtActivePlayer()
        {
            var context = MatchContext.Current;
            FootballPlayer player = human.ActivePlayer;
            if (context == null || player == null) return;
            Vector3 spot = context.Pitch.ClampToPlayingArea(player.Position + player.Forward * 1.2f, 0.5f);
            context.Ball.ResetTo(spot);
            foreach (GoalZone goal in goals)
            {
                if (goal != null) goal.Rearm();
            }
            ShowMessage("Balón recolocado", 1f);
        }

        private void ShowMessage(string text, float seconds)
        {
            Message = text;
            MessageUntil = Time.unscaledTime + seconds;
        }
    }
}
