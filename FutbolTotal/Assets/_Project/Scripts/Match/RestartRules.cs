using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Resultado de un balón que ha salido del campo.</summary>
    public struct RestartDecision
    {
        public RestartType Type;
        /// <summary>Equipo que saca.</summary>
        public Team Team;
        /// <summary>Punto donde se coloca el balón.</summary>
        public Vector3 Spot;
        /// <summary>Hacia dónde mira el lanzador al colocarse.</summary>
        public Vector3 Facing;
        public FootballPlayer Taker;
    }

    /// <summary>
    /// Reglas de salida del balón y de saques, como funciones puras (sin estado). Simplificaciones declaradas:
    /// - El saque de banda se hace con el pie (pase), no con las manos.
    /// - No hay fueras de juego ni faltas.
    /// - El balón está fuera cuando ha cruzado la línea por completo (centro más allá de la línea + radio).
    /// </summary>
    public static class RestartRules
    {
        /// <summary>Detecta si el balón ha salido. Un balón que entra en la portería NO cuenta: lo gestiona <see cref="GoalZone"/>.</summary>
        public static bool TryDecide(Ball ball, MatchContext context, out RestartDecision decision)
        {
            decision = default;
            Pitch pitch = context.Pitch;
            Vector3 local = ball.Position - pitch.Center;
            float r = ball.Radius;
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;
            FootballPlayer lastToucher = ball.LastToucher;

            if (Mathf.Abs(local.x) > L + r)
            {
                bool inGoalMouth = Mathf.Abs(local.z) < pitch.GoalWidth * 0.5f && local.y < pitch.GoalHeight;
                if (inGoalMouth) return false;

                int goalSide = local.x > 0f ? 1 : -1;
                Team defending = context.TeamAttacking(-goalSide);
                if (lastToucher != null && lastToucher.Team == defending)
                {
                    decision = Corner(context, defending.Opponent, goalSide, local.z >= 0f ? 1 : -1);
                }
                else
                {
                    decision = GoalKick(context, defending, goalSide);
                }
                return true;
            }

            if (Mathf.Abs(local.z) > W + r)
            {
                Team team = lastToucher != null ? lastToucher.Team.Opponent : context.HomeTeam;
                decision = ThrowIn(context, team, local.x, local.z >= 0f ? 1 : -1);
                return true;
            }
            return false;
        }

        public static RestartDecision Kickoff(MatchContext context, Team team)
        {
            Vector3 spot = context.Pitch.Center;
            return new RestartDecision
            {
                Type = RestartType.Kickoff,
                Team = team,
                Spot = spot,
                Facing = new Vector3(team.AttackDirection, 0f, 0f),
                Taker = NearestOutfieldPlayer(team, spot)
            };
        }

        public static RestartDecision ThrowIn(MatchContext context, Team team, float localX, int touchSide)
        {
            Pitch pitch = context.Pitch;
            float x = Mathf.Clamp(localX, -pitch.HalfLength + 1f, pitch.HalfLength - 1f);
            Vector3 spot = pitch.Center + new Vector3(x, 0f, touchSide * (pitch.HalfWidth - 0.3f));
            Vector3 facing = new Vector3(team.AttackDirection * 0.5f, 0f, -touchSide).normalized;
            return new RestartDecision
            {
                Type = RestartType.ThrowIn,
                Team = team,
                Spot = spot,
                Facing = facing,
                Taker = NearestOutfieldPlayer(team, spot)
            };
        }

        public static RestartDecision GoalKick(MatchContext context, Team defending, int goalSide)
        {
            Pitch pitch = context.Pitch;
            Vector3 spot = pitch.Center + new Vector3(goalSide * (pitch.HalfLength - pitch.GoalAreaDepth), 0f, 0f);
            FootballPlayer keeper = defending.Goalkeeper;
            return new RestartDecision
            {
                Type = RestartType.GoalKick,
                Team = defending,
                Spot = spot,
                Facing = new Vector3(-goalSide, 0f, 0f),
                Taker = keeper != null ? keeper : NearestOutfieldPlayer(defending, spot)
            };
        }

        public static RestartDecision Corner(MatchContext context, Team attacking, int goalSide, int touchSide)
        {
            Pitch pitch = context.Pitch;
            Vector3 spot = pitch.Center + new Vector3(goalSide * (pitch.HalfLength - 0.4f), 0f,
                                                      touchSide * (pitch.HalfWidth - 0.4f));
            Vector3 penaltySpot = pitch.GoalCenter(goalSide) - new Vector3(goalSide * pitch.PenaltySpotDistance, 0f, 0f);
            return new RestartDecision
            {
                Type = RestartType.Corner,
                Team = attacking,
                Spot = spot,
                Facing = KickMath.Flat(penaltySpot - spot).normalized,
                Taker = NearestOutfieldPlayer(attacking, spot)
            };
        }

        public static FootballPlayer NearestOutfieldPlayer(Team team, Vector3 point)
        {
            FootballPlayer best = null;
            float bestDistance = float.MaxValue;
            foreach (FootballPlayer player in team.Players)
            {
                if (player.IsGoalkeeper || !player.isActiveAndEnabled) continue;
                float distance = KickMath.FlatDistance(player.Position, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = player;
                }
            }
            return best;
        }

        public static string Describe(RestartType type)
        {
            switch (type)
            {
                case RestartType.Kickoff: return "Saque inicial";
                case RestartType.ThrowIn: return "Saque de banda";
                case RestartType.GoalKick: return "Saque de puerta";
                case RestartType.Corner: return "Córner";
                default: return type.ToString();
            }
        }
    }
}
