using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Defensor de zona: ocupa su puesto en la formación defensiva (que sigue al balón) y, si un rival entra
    /// en su zona, se coloca entre ese rival y la portería, algo inclinado hacia el balón para cortar el pase.
    /// </summary>
    public sealed class ZoneCoverBehavior : IPlayerBehavior
    {
        public void Tick(PlayerBrain brain)
        {
            FootballPlayer player = brain.Player;
            AISettings s = brain.Settings;
            Ball ball = MatchContext.Current.Ball;

            Vector3 target = FormationShape.Target(player, ball.Position, false, s);

            FootballPlayer mark = brain.MarkTarget;
            if (mark != null)
            {
                Vector3 toGoal = KickMath.Flat(player.Team.OwnGoalCenter - mark.Position).normalized;
                Vector3 markSpot = mark.Position + toGoal * s.markDistance;
                Vector3 toBall = KickMath.Flat(ball.Position - markSpot);
                if (toBall.sqrMagnitude > 0.01f) markSpot += toBall.normalized * 0.8f;
                target = Vector3.Lerp(target, markSpot, s.markWeight);
            }

            bool far = KickMath.FlatDistance(player.Position, target) > s.sprintDistance;
            brain.MoveTo(target, allowSprint: far);
        }
    }
}
