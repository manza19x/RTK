using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Jugador de la IA con balón. Cada <see cref="AISettings.carrierDecisionInterval"/> (o al instante si le
    /// presionan de cerca) decide, por este orden:
    /// 1. TIRAR si está a distancia de tiro, ve bien la portería y la línea de tiro está libre (o está muy cerca).
    /// 2. PASAR si está presionado y hay una opción aceptable, o si un pase hace avanzar claramente el juego.
    ///    Bajo presión en su propio tercio y sin opción, despeja en largo.
    /// 3. CONDUCIR hacia la portería rival esquivando a los rivales que tiene delante.
    /// </summary>
    public sealed class BallCarrierBehavior : IPlayerBehavior
    {
        public void Tick(PlayerBrain brain)
        {
            FootballPlayer player = brain.Player;
            AISettings s = brain.Settings;
            var context = MatchContext.Current;

            AIQueries.NearestOpponent(player, player.Position, out float pressure);
            bool urgent = pressure < s.pressureDistance * 0.6f;

            if (Time.time >= brain.NextDecisionTime || (urgent && Time.time >= brain.NextDecisionTime - s.carrierDecisionInterval * 0.5f))
            {
                brain.NextDecisionTime = Time.time + s.carrierDecisionInterval;
                if (TryShoot(player, context, s)) return;
                if (TryPass(player, context, s, pressure)) return;
                brain.DribbleDirection = ChooseDribbleDirection(player, context, s);
            }

            if (brain.DribbleDirection.sqrMagnitude < 0.01f) brain.DribbleDirection = AIQueries.Forward(player.Team);
            bool space = pressure > s.pressureDistance * 2f;
            bool sprint = space && player.Stamina.Current01 > s.minStaminaToSprint;
            player.Motor.SetMoveInput(brain.DribbleDirection, sprint);
        }

        private static bool TryShoot(FootballPlayer player, MatchContext context, AISettings s)
        {
            Team team = player.Team;
            Pitch pitch = context.Pitch;
            Ball ball = context.Ball;
            Vector3 goal = team.OpponentGoalCenter;
            float distance = KickMath.FlatDistance(player.Position, goal);

            if (distance > s.shootRange) return false;
            if (AIQueries.GoalMouthAngle(player.Position, goal, pitch.GoalWidth) < s.minShootAngle) return false;
            bool laneClear = AIQueries.ShotLaneClear(ball.Position, goal, team.Opponent, s.shotLaneRadius);
            if (!laneClear && distance > s.closeShotRange) return false;

            // Apunta al lado contrario de donde está el portero.
            ActionSettings actions = player.Actions.Settings;
            FootballPlayer keeper = team.Opponent.Goalkeeper;
            float keeperOffset = keeper != null ? keeper.Position.z - goal.z : 0f;
            float side = keeperOffset > 0f ? -0.8f : 0.8f;
            float z = side * actions.shotStickFullDeflection;
            Vector3 aim = new Vector3(Mathf.Sqrt(Mathf.Max(0f, 1f - z * z)) * team.AttackDirection, 0f, z);

            float power = Mathf.Lerp(s.minShotPower, s.maxShotPower, Mathf.InverseLerp(6f, s.shootRange, distance));
            return player.Actions.Shoot(aim, power, true);
        }

        private static bool TryPass(FootballPlayer player, MatchContext context, AISettings s, float pressure)
        {
            bool pressed = pressure < s.pressureDistance;
            ActionSettings actions = player.Actions.Settings;

            if (PassEvaluator.FindBest(player, s, actions, allowBackToKeeper: pressed, out PassOption option))
            {
                bool worthIt = pressed
                    ? option.Score >= s.passScoreUnderPressure
                    : option.Score >= s.passScoreFree && option.Progress >= s.minProgressWhenFree;
                if (worthIt) return PassEvaluator.Execute(player, option, actions);
            }

            // Sin opción clara y acosado cerca de su portería: despeje en largo hacia delante.
            if (pressure < s.pressureDistance * 0.6f && InOwnThird(player, context.Pitch))
            {
                return player.Actions.LongPass(AIQueries.Forward(player.Team), 0.7f, out _);
            }
            return false;
        }

        private static bool InOwnThird(FootballPlayer player, Pitch pitch)
        {
            float depth = (player.Position.x - pitch.Center.x) * player.Team.AttackDirection;
            return depth < -pitch.HalfLength / 3f;
        }

        private static Vector3 ChooseDribbleDirection(FootballPlayer player, MatchContext context, AISettings s)
        {
            Team team = player.Team;
            Pitch pitch = context.Pitch;
            Vector3 forward = AIQueries.Forward(team);
            Vector3 toGoal = KickMath.Flat(team.OpponentGoalCenter - player.Position).normalized;

            // Lejos de la portería avanza recto; cerca, se orienta hacia ella.
            float closeness = Mathf.InverseLerp(40f, 15f, KickMath.FlatDistance(player.Position, team.OpponentGoalCenter));
            Vector3 desired = Vector3.Lerp(forward, toGoal, closeness).normalized;

            // Esquiva a los rivales que tiene delante.
            foreach (FootballPlayer opponent in team.Opponent.Players)
            {
                Vector3 away = KickMath.Flat(player.Position - opponent.Position);
                float distance = away.magnitude;
                if (distance < 0.01f || distance > s.avoidRadius) continue;
                if (Vector3.Dot(-away, desired) <= 0f) continue; // Está detrás: no molesta.
                desired += away / distance * (1f - distance / s.avoidRadius) * s.avoidWeight;
            }

            // No se va contra la banda.
            float localZ = player.Position.z - pitch.Center.z;
            if (Mathf.Abs(localZ) > pitch.HalfWidth - 3f) desired.z -= Mathf.Sign(localZ) * 0.8f;

            desired.y = 0f;
            return desired.sqrMagnitude > 0.01f ? desired.normalized : forward;
        }
    }
}
