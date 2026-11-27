using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Estadísticas del partido para la pantalla de resultado: goles (minuto, equipo, dorsal, en propia puerta),
    /// tiempo de posesión y tiros de cada equipo. Solo escucha eventos: no influye en el juego.
    /// </summary>
    public class MatchStats : MonoBehaviour
    {
        public struct GoalRecord
        {
            public TeamSide Side;
            public int Minute;
            public int ShirtNumber;
            public bool OwnGoal;
        }

        private readonly List<GoalRecord> goals = new List<GoalRecord>();
        private readonly float[] possessionSeconds = new float[2];
        private readonly int[] shots = new int[2];
        private readonly int[] fouls = new int[2];
        private MatchManager manager;
        private Ball ball;

        public IReadOnlyList<GoalRecord> Goals => goals;

        private void Start()
        {
            var context = MatchContext.Current;
            manager = context != null ? context.Rules as MatchManager : null;
            if (manager == null)
            {
                enabled = false;
                return;
            }
            ball = context.Ball;
            manager.MatchStarted += ResetStats;
            manager.GoalScored += OnGoal;
            ball.Kicked += OnKicked;
            context.FoulCommitted += OnFoul;
        }

        private void OnFoul(FoulEvent foul)
        {
            if (manager.IsBallInPlay) fouls[(int)foul.Offender.Team.Side]++;
        }

        public int Fouls(TeamSide side) => fouls[(int)side];

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.MatchStarted -= ResetStats;
                manager.GoalScored -= OnGoal;
            }
            if (ball != null) ball.Kicked -= OnKicked;
            if (MatchContext.Current != null) MatchContext.Current.FoulCommitted -= OnFoul;
        }

        private void Update()
        {
            if (!manager.IsBallInPlay || ball.Owner == null) return;
            possessionSeconds[(int)ball.Owner.Team.Side] += Time.deltaTime;
        }

        private void ResetStats()
        {
            goals.Clear();
            possessionSeconds[0] = possessionSeconds[1] = 0f;
            shots[0] = shots[1] = 0;
            fouls[0] = fouls[1] = 0;
        }

        private void OnGoal(Team scorer)
        {
            FootballPlayer toucher = ball.LastToucher;
            goals.Add(new GoalRecord
            {
                Side = scorer.Side,
                Minute = manager.CurrentMinute,
                ShirtNumber = toucher != null ? toucher.ShirtNumber : 0,
                OwnGoal = toucher != null && toucher.Team != scorer
            });
        }

        private void OnKicked(KickEvent kick)
        {
            if (kick.Type == KickType.Shot && kick.Kicker != null) shots[(int)kick.Kicker.Team.Side]++;
        }

        public int Shots(TeamSide side) => shots[(int)side];

        /// <summary>Porcentaje de posesión (0..100). 50 si todavía no ha habido posesión.</summary>
        public int PossessionPercent(TeamSide side)
        {
            float total = possessionSeconds[0] + possessionSeconds[1];
            if (total <= 0.01f) return 50;
            return Mathf.RoundToInt(possessionSeconds[(int)side] / total * 100f);
        }
    }
}
