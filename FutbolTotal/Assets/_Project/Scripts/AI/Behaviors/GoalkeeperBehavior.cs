using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Portero. Prioridades, por orden:
    /// 1. Con el balón en las manos: lo retiene un momento y lo saca (pase a un compañero libre o en largo).
    /// 2. Tiro hacia su portería: tras su tiempo de reacción, calcula por dónde pasará el balón; si está cerca da un
    ///    paso, si no se estira (impulso lateral). Blocar o despejar lo deciden las reglas de <see cref="BallPossession"/>.
    /// 3. Peligro en el área: sale a por un balón suelto si llega antes que los rivales, o al encuentro del delantero.
    /// 4. Colocación: sobre la línea entre el balón y el centro de la portería, más adelantado cuanto más lejos esté el balón.
    /// </summary>
    public sealed class GoalkeeperBehavior : IPlayerBehavior
    {
        public void Tick(PlayerBrain brain)
        {
            FootballPlayer keeper = brain.Player;
            if (keeper.Motor.IsLunging) return;

            var context = MatchContext.Current;
            if (HandleBallInHands(brain, context)) return;
            if (ReactToShot(brain, context)) return;
            if (HandleDangerInBox(brain, context)) return;
            Position(brain, context);
        }

        private static bool HandleBallInHands(PlayerBrain brain, MatchContext context)
        {
            FootballPlayer keeper = brain.Player;
            if (!keeper.HasBall)
            {
                brain.HadBall = false;
                return false;
            }

            AISettings s = brain.Settings;
            if (!brain.HadBall)
            {
                brain.HadBall = true;
                brain.HoldBallUntil = Time.time + s.keeperHoldSeconds;
            }

            keeper.Motor.Stop();
            keeper.Motor.SetFacing(AIQueries.Forward(keeper.Team));
            if (Time.time < brain.HoldBallUntil) return true;

            ActionSettings actions = keeper.Actions.Settings;
            if (PassEvaluator.FindBest(keeper, s, actions, false, out PassOption option) && option.Progress > 0f &&
                option.Score >= s.passScoreUnderPressure)
            {
                PassEvaluator.Execute(keeper, option, actions);
            }
            else
            {
                keeper.Actions.LongPass(AIQueries.Forward(keeper.Team), 0.6f, out _);
            }
            return true;
        }

        private static bool ReactToShot(PlayerBrain brain, MatchContext context)
        {
            FootballPlayer keeper = brain.Player;
            AISettings s = brain.Settings;
            Ball ball = context.Ball;
            if (!ball.IsFree || !ball.LastKick.HasValue) return false;

            int goalSide = -keeper.Team.AttackDirection;
            Vector3 goal = keeper.Team.OwnGoalCenter;
            Vector3 velocity = ball.Velocity;
            if (velocity.x * goalSide < s.keeperThreatSpeed) return false;

            float timeToLine = (goal.x - ball.Position.x) / velocity.x;
            if (timeToLine <= 0f || timeToLine > 2f) return false;
            float zAtLine = ball.Position.z + velocity.z * timeToLine;
            if (Mathf.Abs(zAtLine - goal.z) > context.Pitch.GoalWidth * 0.5f + 0.5f) return false;

            float timeToKeeper = (keeper.Position.x - ball.Position.x) / velocity.x;
            if (timeToKeeper <= 0f) return false;
            float zAtKeeper = ball.Position.z + velocity.z * timeToKeeper;
            float lateral = zAtKeeper - keeper.Position.z;

            float kickTime = ball.LastKick.Value.Time;
            bool reacted = Time.time - kickTime >= s.keeperReaction;
            if (!reacted) return true; // Todavía no ha reaccionado: se queda quieto.

            if (brain.HandledKickTime != kickTime && Mathf.Abs(lateral) > s.keeperStepReach)
            {
                brain.HandledKickTime = kickTime;
                keeper.Motor.StartLunge(new Vector3(0f, 0f, Mathf.Sign(lateral)), s.diveSpeed, s.diveDuration);
                return true;
            }

            brain.MoveTo(new Vector3(keeper.Position.x, 0f, zAtKeeper), allowSprint: false, tolerance: 0.05f);
            return true;
        }

        private static bool HandleDangerInBox(PlayerBrain brain, MatchContext context)
        {
            FootballPlayer keeper = brain.Player;
            AISettings s = brain.Settings;
            Ball ball = context.Ball;
            Pitch pitch = context.Pitch;
            int goalSide = -keeper.Team.AttackDirection;

            if (ball.IsFree)
            {
                Vector3 point = AIQueries.InterceptPoint(keeper, ball, pitch);
                if (!pitch.IsInPenaltyArea(point, goalSide)) return false;
                AIQueries.NearestOpponent(keeper, point, out float rivalDistance);
                if (KickMath.FlatDistance(keeper.Position, point) >= rivalDistance) return false;
                brain.MoveTo(point, allowSprint: true, tolerance: 0.05f);
                return true;
            }

            FootballPlayer owner = ball.Owner;
            if (owner.Team == keeper.Team || !pitch.IsInPenaltyArea(owner.Position, goalSide)) return false;
            Vector3 goal = keeper.Team.OwnGoalCenter;
            if (KickMath.FlatDistance(owner.Position, goal) > s.keeperRushDistance) return false;

            // Sale al encuentro para reducir el ángulo.
            Vector3 target = owner.Position + KickMath.Flat(goal - owner.Position).normalized * 1.2f;
            brain.MoveTo(target, allowSprint: true, tolerance: 0.2f);
            return true;
        }

        private static void Position(PlayerBrain brain, MatchContext context)
        {
            FootballPlayer keeper = brain.Player;
            AISettings s = brain.Settings;
            Pitch pitch = context.Pitch;
            int goalSide = -keeper.Team.AttackDirection;
            Vector3 goal = keeper.Team.OwnGoalCenter;
            Vector3 toBall = KickMath.Flat(context.Ball.Position - goal);

            Vector3 direction = toBall.sqrMagnitude > 0.01f ? toBall.normalized : new Vector3(-goalSide, 0f, 0f);
            if (direction.x * -goalSide < 0.2f) direction = new Vector3(-goalSide, 0f, 0f);

            float advance = Mathf.Lerp(s.keeperLineDistance, s.keeperMaxAdvance,
                                       Mathf.InverseLerp(12f, 40f, toBall.magnitude));
            Vector3 target = goal + direction * advance;
            float maxZ = pitch.GoalWidth * 0.5f + 0.3f;
            target.z = Mathf.Clamp(target.z, goal.z - maxZ, goal.z + maxZ);

            brain.MoveTo(target, allowSprint: false, tolerance: 0.25f);
        }
    }
}
