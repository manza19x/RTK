using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Ejecuta un saque por un equipo de la IA con decisiones simples y previsibles:
    /// saque inicial hacia atrás, saque de banda corto hacia delante, saque de puerta en largo y córner al punto de penalti.
    /// Usa las mismas acciones que el jugador humano (<see cref="PlayerActions"/>).
    /// </summary>
    public static class AutoRestart
    {
        public static bool Take(FootballPlayer taker, RestartType type, MatchContext context)
        {
            if (taker == null) return false;
            int direction = taker.Team.AttackDirection;
            Pitch pitch = context.Pitch;
            Vector3 ballPosition = context.Ball.Position;

            switch (type)
            {
                case RestartType.Kickoff:
                    return taker.Actions.ShortPass(new Vector3(-direction, 0f, 0f), 0.1f, out _);

                case RestartType.ThrowIn:
                {
                    float touchSide = Mathf.Sign(ballPosition.z - pitch.Center.z);
                    Vector3 aim = new Vector3(direction * 0.6f, 0f, -touchSide).normalized;
                    return taker.Actions.ShortPass(aim, 0.3f, out _);
                }

                case RestartType.GoalKick:
                    return taker.Actions.LongPass(new Vector3(direction, 0f, 0f), 0.55f, out _);

                case RestartType.Corner:
                {
                    Vector3 penaltySpot = pitch.GoalCenter(direction) - new Vector3(direction * pitch.PenaltySpotDistance, 0f, 0f);
                    Vector3 aim = KickMath.Flat(penaltySpot - ballPosition).normalized;
                    return taker.Actions.LongPass(aim, 0.35f, out _);
                }
            }
            return false;
        }
    }
}
