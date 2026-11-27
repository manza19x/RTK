using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Compañero cuando su equipo ataca. Parte de su puesto en la formación de ataque y prueba unos pocos
    /// huecos cercanos (a los lados y hacia delante); elige el que deja la línea de pase más libre y le aleja
    /// de rivales y compañeros. Así apoya al conductor o se desmarca, según dónde esté el espacio.
    /// Los delanteros prueban además carreras más profundas.
    /// </summary>
    public sealed class SupportBehavior : IPlayerBehavior
    {
        private static readonly Vector2[] Offsets =
        {
            new Vector2(0f, 0f), new Vector2(0f, -1f), new Vector2(0f, 1f),
            new Vector2(1f, 0f), new Vector2(1f, -1f), new Vector2(1f, 1f),
            new Vector2(-0.5f, 0f)
        };

        public void Tick(PlayerBrain brain)
        {
            if (Time.time >= brain.NextDecisionTime)
            {
                brain.NextDecisionTime = Time.time + brain.Settings.supportDecisionInterval;
                brain.MoveTarget = FindSpot(brain);
            }
            brain.MoveTo(brain.MoveTarget, allowSprint: true);
        }

        private static Vector3 FindSpot(PlayerBrain brain)
        {
            FootballPlayer player = brain.Player;
            AISettings s = brain.Settings;
            var context = MatchContext.Current;
            Ball ball = context.Ball;
            Team team = player.Team;

            Vector3 baseSpot = FormationShape.Target(player, ball.Position, true, s);
            FootballPlayer carrier = ball.Owner != null && ball.Owner.Team == team ? ball.Owner : null;
            if (carrier == null) return baseSpot;

            ActionSettings actions = player.Actions.Settings;
            var opponents = team.Opponent.Players;
            Vector3 forward = AIQueries.Forward(team);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            float depthScale = player.Role == PlayerRole.Forward ? 1.5f : 1f;

            Vector3 best = baseSpot;
            float bestScore = float.MinValue;
            foreach (Vector2 offset in Offsets)
            {
                Vector3 candidate = baseSpot + forward * (offset.x * s.runDepth * depthScale) + side * (offset.y * s.supportShift);
                candidate = context.Pitch.ClampToPlayingArea(candidate, 2f);

                float lane = PassTargetSelector.LaneOpenness(carrier.Position, candidate, opponents, actions.laneSafeDistance);
                float open = PassTargetSelector.ReceiverOpenness(candidate, opponents, actions.receiverSafeDistance);
                float score = lane * 0.6f + open * 0.4f
                              + AIQueries.Progress(team, carrier.Position, candidate) * 0.01f
                              - KickMath.FlatDistance(candidate, baseSpot) * 0.02f;

                float toCarrier = KickMath.FlatDistance(candidate, carrier.Position);
                if (toCarrier < s.minSupportDistance) score -= (s.minSupportDistance - toCarrier) * 0.2f;

                foreach (FootballPlayer mate in team.Players)
                {
                    if (mate == player || mate == carrier) continue;
                    if (KickMath.FlatDistance(candidate, mate.Position) < 5f) score -= 0.3f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
