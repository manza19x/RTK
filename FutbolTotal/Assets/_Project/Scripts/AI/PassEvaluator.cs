using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Una opción de pase valorada.</summary>
    public struct PassOption
    {
        public FootballPlayer Receiver;
        public float Score;
        public float Progress;
        public float Distance;
        public bool Aerial;
    }

    /// <summary>
    /// Valora los pases posibles de un jugador. Criterios visibles y deterministas:
    /// línea de pase libre, receptor sin marca, cuánto avanza el balón y cuánto mide el pase.
    /// </summary>
    public static class PassEvaluator
    {
        public static bool FindBest(FootballPlayer passer, AISettings ai, ActionSettings actions, bool allowBackToKeeper,
                                    out PassOption best)
        {
            best = default;
            best.Score = float.MinValue;
            Team team = passer.Team;
            var opponents = team.Opponent.Players;

            foreach (FootballPlayer mate in team.Players)
            {
                if (mate == passer || !mate.isActiveAndEnabled) continue;
                if (mate.IsGoalkeeper && !allowBackToKeeper) continue;

                float distance = KickMath.FlatDistance(passer.Position, mate.Position);
                if (distance < ai.minPassDistance || distance > ai.maxPassDistance) continue;

                float progress = AIQueries.Progress(team, passer.Position, mate.Position);
                float lane = PassTargetSelector.LaneOpenness(passer.Position, mate.Position, opponents, actions.laneSafeDistance);
                float open = PassTargetSelector.ReceiverOpenness(mate.Position, opponents, actions.receiverSafeDistance);
                bool aerial = lane < ai.aerialLaneThreshold && distance >= actions.longPassMinDistance;

                // Un pase raso necesita línea libre; uno alto, que el receptor esté libre donde cae.
                float quality = aerial ? open * 0.8f : lane * 0.6f + open * 0.4f;
                float score = quality + progress * ai.passProgressWeight - distance * ai.passDistancePenalty;

                if (score > best.Score)
                {
                    best = new PassOption
                    {
                        Receiver = mate,
                        Score = score,
                        Progress = progress,
                        Distance = distance,
                        Aerial = aerial
                    };
                }
            }
            return best.Receiver != null;
        }

        /// <summary>Ejecuta el pase elegido con la carga adecuada a la distancia.</summary>
        public static bool Execute(FootballPlayer passer, PassOption option, ActionSettings actions)
        {
            Vector3 aim = KickMath.Flat(option.Receiver.Position - passer.Position).normalized;
            if (option.Aerial)
            {
                float charge = Mathf.InverseLerp(actions.longPassPreferredMin, actions.longPassPreferredMax, option.Distance);
                return passer.Actions.LongPass(aim, charge, out _);
            }
            else
            {
                float charge = Mathf.InverseLerp(actions.shortPassPreferredMin, actions.shortPassPreferredMax, option.Distance);
                return passer.Actions.ShortPass(aim, charge, out _);
            }
        }
    }
}
