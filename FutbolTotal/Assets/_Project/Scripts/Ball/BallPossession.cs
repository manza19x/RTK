using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Reglas de quién se hace con un balón suelto. Se ejecuta antes que <see cref="Ball"/> en cada paso de física.
    /// - Gana el balón el jugador más cercano a su trayectoria (se comprueba el segmento recorrido en el paso,
    ///   así un pase rápido no "atraviesa" a un jugador).
    /// - Si llega demasiado rápido, rebota en el jugador en vez de quedar controlado (bloqueos, despejes del portero).
    /// - El portero dentro de su área tiene más alcance y altura (manos).
    /// </summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Ball))]
    public class BallPossession : MonoBehaviour
    {
        private Ball ball;
        private Vector3 previousPosition;

        private void Awake()
        {
            ball = GetComponent<Ball>();
        }

        private void Start()
        {
            previousPosition = ball.Position;
        }

        private void FixedUpdate()
        {
            var context = MatchContext.Current;
            if (context == null) return;

            Vector3 position = ball.Position;
            if (ball.IsFree) TryAcquire(context, previousPosition, position);
            previousPosition = position;
        }

        private void TryAcquire(MatchContext context, Vector3 from, Vector3 to)
        {
            BallSettings s = ball.Settings;
            FootballPlayer best = null;
            float bestDistance = float.MaxValue;
            bool bestUsesHands = false;
            float lowestHeight = Mathf.Min(from.y, to.y);

            var players = context.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                FootballPlayer player = players[i];
                if (!player.isActiveAndEnabled || player.Motor.IsStunned || ball.IsIgnoring(player)) continue;

                bool hands = player.IsGoalkeeper &&
                             context.Pitch.IsInPenaltyArea(player.Position, -player.Team.AttackDirection);
                float radius = hands ? s.goalkeeperControlRadius : s.controlRadius;
                float maxHeight = hands ? s.goalkeeperMaxHeight : s.controlMaxHeight;
                if (lowestHeight > maxHeight) continue;

                float distance = KickMath.DistancePointToSegmentFlat(player.Position, from, to, out _);
                if (distance <= radius && distance < bestDistance)
                {
                    best = player;
                    bestDistance = distance;
                    bestUsesHands = hands;
                }
            }

            if (best == null) return;

            float maxSpeed = bestUsesHands ? s.goalkeeperMaxCatchSpeed : s.maxControllableSpeed;
            if (ball.Velocity.magnitude > maxSpeed)
            {
                Deflect(best);
                return;
            }
            ball.SetOwner(best);
        }

        /// <summary>Rebote determinista: el balón sale reflejado respecto al cuerpo, con poca velocidad.</summary>
        private void Deflect(FootballPlayer player)
        {
            Vector3 velocity = ball.Velocity;
            Vector3 normal = KickMath.Flat(ball.Position - player.Position);
            if (normal.sqrMagnitude < 1e-4f || Vector3.Dot(velocity, normal) > 0f)
            {
                // El balón ya ha pasado el centro del cuerpo: lo devolvemos por donde venía.
                normal = -KickMath.Flat(velocity);
            }
            normal.Normalize();

            Vector3 reflected = Vector3.Reflect(velocity, normal) * ball.Settings.deflectionDamping;
            reflected.y = Mathf.Abs(reflected.y) * 0.5f + 1.5f;
            ball.Deflect(player, reflected);
        }
    }
}
