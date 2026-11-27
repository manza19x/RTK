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
                case RestartType.Penalty:
                    return TakePenalty(taker);

                case RestartType.FreeKick:
                    return TakeFreeKick(taker, context);

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

        /// <summary>Penalti de la IA: fuerte y al lado contrario de donde está el portero.</summary>
        private static bool TakePenalty(FootballPlayer taker)
        {
            Team team = taker.Team;
            Vector3 goal = team.OpponentGoalCenter;
            FootballPlayer keeper = team.Opponent.Goalkeeper;
            // Sin portero desplazado, elige lado según su dorsal: variado pero reproducible.
            float side = keeper != null && Mathf.Abs(keeper.Position.z - goal.z) > 0.2f
                ? -Mathf.Sign(keeper.Position.z - goal.z)
                : (taker.ShirtNumber % 2 == 0 ? 1f : -1f);
            return taker.Actions.Shoot(AimAtGoalSide(taker, side * 0.85f), 0.7f, true);
        }

        /// <summary>Tiro libre de la IA: chuta si está a tiro y con ángulo; si no, pasa a quien mejor esté.</summary>
        private static bool TakeFreeKick(FootballPlayer taker, MatchContext context)
        {
            Team team = taker.Team;
            AISettings ai = SettingsProvider.Load<AISettings>(SettingsProvider.AIName);
            ActionSettings actions = taker.Actions.Settings;
            Vector3 goal = team.OpponentGoalCenter;
            float distance = KickMath.FlatDistance(taker.Position, goal);
            float shootRange = ai != null ? ai.shootRange + 3f : 25f;

            if (distance <= shootRange && AIQueries.GoalMouthAngle(taker.Position, goal, context.Pitch.GoalWidth) >= 10f)
            {
                FootballPlayer keeper = team.Opponent.Goalkeeper;
                float side = keeper != null && keeper.Position.z > goal.z ? -0.8f : 0.8f;
                float power = Mathf.Lerp(0.5f, 0.85f, Mathf.InverseLerp(10f, shootRange, distance));
                return taker.Actions.Shoot(AimAtGoalSide(taker, side), power, true);
            }

            if (ai != null && PassEvaluator.FindBest(taker, ai, actions, false, out PassOption option))
            {
                return PassEvaluator.Execute(taker, option, actions);
            }
            return taker.Actions.LongPass(AIQueries.Forward(team), 0.6f, out _);
        }

        /// <summary>Dirección de tiro que ShotCalculator traduce en un lado de la portería (-1..1).</summary>
        private static Vector3 AimAtGoalSide(FootballPlayer taker, float side01)
        {
            float z = Mathf.Clamp(side01, -1f, 1f) * taker.Actions.Settings.shotStickFullDeflection;
            return new Vector3(Mathf.Sqrt(Mathf.Max(0f, 1f - z * z)) * taker.Team.AttackDirection, 0f, z);
        }
    }
}
