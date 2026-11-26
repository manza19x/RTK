using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Elige a qué compañero pasar el control. Reglas, por orden:
    /// 1. Si hay un pase en vuelo hacia un compañero, el receptor.
    /// 2. Si no, el jugador de campo más cercano a donde estará el balón en un instante, con ventaja
    ///    para quien esté entre el balón y la propia portería (mejor colocado para defender).
    /// El portero nunca se selecciona manualmente (se controla solo cuando recibe el balón).
    /// </summary>
    public static class PlayerSwitchSelector
    {
        private const float LookAheadSeconds = 0.4f;
        private const float GoalSideBonusMeters = 3f;
        private const float PassInFlightWindow = 2f;

        public static FootballPlayer SelectBest(Team team, Ball ball, FootballPlayer exclude)
        {
            if (ball.Owner != null && ball.Owner.Team == team && ball.Owner != exclude) return ball.Owner;

            if (ball.IsFree && ball.LastKick.HasValue && ball.TimeSinceKick < PassInFlightWindow)
            {
                FootballPlayer receiver = ball.LastKick.Value.IntendedReceiver;
                if (receiver != null && receiver.Team == team && receiver != exclude) return receiver;
            }

            Vector3 predicted = KickMath.Flat(ball.Position + ball.Velocity * LookAheadSeconds);
            Vector3 ownGoal = team.OwnGoalCenter;
            float ballToGoal = KickMath.FlatDistance(predicted, ownGoal);

            FootballPlayer best = null;
            float bestScore = float.MaxValue;
            foreach (FootballPlayer player in team.Players)
            {
                if (player == exclude || player.IsGoalkeeper || !player.isActiveAndEnabled) continue;

                float score = KickMath.FlatDistance(player.Position, predicted);
                if (KickMath.FlatDistance(player.Position, ownGoal) < ballToGoal) score -= GoalSideBonusMeters;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = player;
                }
            }
            return best != null ? best : exclude;
        }
    }
}
