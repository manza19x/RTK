using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Ir a por el balón. Tres variantes:
    /// - Tackle (presión): se coloca entre el conductor rival y su portería y, si mantiene el balón a su alcance
    ///   un instante (<see cref="AISettings.tackleReactionDelay"/>), hace una entrada. Ese margen permite regatearle.
    /// - Contain (contención): igual pero a distancia y sin entradas. Lo usan los compañeros del jugador humano.
    /// - Chase (balón suelto): va al punto donde puede alcanzar el balón.
    /// </summary>
    public sealed class PressBehavior : IPlayerBehavior
    {
        public enum Mode
        {
            Tackle,
            Contain,
            Chase
        }

        private readonly Mode mode;

        public PressBehavior(Mode mode)
        {
            this.mode = mode;
        }

        public void Tick(PlayerBrain brain)
        {
            FootballPlayer player = brain.Player;
            AISettings s = brain.Settings;
            var context = MatchContext.Current;
            Ball ball = context.Ball;
            FootballPlayer owner = ball.Owner;

            if (mode == Mode.Chase || owner == null)
            {
                brain.MoveTo(AIQueries.InterceptPoint(player, ball, context.Pitch), allowSprint: true, tolerance: 0.05f);
                return;
            }
            if (owner.Team == player.Team) return; // Lo resuelve TeamAI en la siguiente reasignación.

            Vector3 toOwnGoal = KickMath.Flat(player.Team.OwnGoalCenter - owner.Position).normalized;
            float standOff = mode == Mode.Contain ? s.containDistance : s.pressStandOff;
            Vector3 goalSide = owner.Position + toOwnGoal * standOff + owner.Motor.Velocity * s.pressLead;
            brain.MoveTo(goalSide, allowSprint: true, tolerance: 0.3f);

            if (mode != Mode.Tackle) return;

            float toBall = KickMath.FlatDistance(player.Position, ball.Position);
            if (toBall > s.tackleRange)
            {
                brain.InTackleRangeSince = -1f;
                return;
            }
            if (brain.InTackleRangeSince < 0f)
            {
                brain.InTackleRangeSince = Time.time;
            }
            else if (Time.time - brain.InTackleRangeSince >= s.tackleReactionDelay && player.Actions.CanTackle &&
                     !player.Actions.IsBehind(owner)) // La IA no entra por detrás: sería falta.
            {
                player.Actions.TryTackle();
                brain.InTackleRangeSince = -1f;
            }
        }
    }
}
