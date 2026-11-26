using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    public enum PassKind
    {
        /// <summary>Pase raso: importa que la línea de pase esté libre.</summary>
        Ground,
        /// <summary>Pase alto: importa que el receptor esté libre donde cae el balón.</summary>
        Aerial
    }

    /// <summary>
    /// Elige el compañero más razonable para un pase. Criterios (todos visibles para el jugador):
    /// 1. Ángulo respecto a la dirección apuntada (lo más importante: el jugador decide hacia dónde).
    /// 2. Distancia respecto a la preferida, que depende de la carga del botón (toque corto = cerca, cargado = lejos).
    /// 3. Línea de pase libre de rivales (raso) o receptor libre de marca (alto).
    /// Sin aleatoriedad: la misma situación siempre da el mismo receptor.
    /// </summary>
    public static class PassTargetSelector
    {
        public static FootballPlayer FindTarget(FootballPlayer passer, Vector3 aimDirection, PassKind kind,
                                                float charge01, ActionSettings s)
        {
            Team team = passer.Team;
            if (team == null) return null;
            IReadOnlyList<FootballPlayer> opponents = team.Opponent != null ? team.Opponent.Players : null;

            bool ground = kind == PassKind.Ground;
            float cone = ground ? s.shortPassConeDegrees : s.longPassConeDegrees;
            float minDistance = ground ? s.shortPassMinDistance : s.longPassMinDistance;
            float maxDistance = ground ? s.shortPassMaxDistance : s.longPassMaxDistance;
            float preferred = ground
                ? Mathf.Lerp(s.shortPassPreferredMin, s.shortPassPreferredMax, charge01)
                : Mathf.Lerp(s.longPassPreferredMin, s.longPassPreferredMax, charge01);

            FootballPlayer best = null;
            float bestScore = float.MinValue;

            foreach (FootballPlayer mate in team.Players)
            {
                if (mate == passer || !mate.isActiveAndEnabled) continue;
                if (!ground && mate.IsGoalkeeper) continue; // Nunca un pase alto al propio portero.

                Vector3 toMate = KickMath.Flat(mate.Position - passer.Position);
                float distance = toMate.magnitude;
                if (distance < minDistance || distance > maxDistance) continue;

                float angle = Vector3.Angle(aimDirection, toMate);
                if (angle > cone) continue;

                float angleScore = 1f - angle / cone;
                float distanceScore = 1f - Mathf.Clamp01(Mathf.Abs(distance - preferred) / (maxDistance - minDistance));
                float openScore = ground
                    ? LaneOpenness(passer.Position, mate.Position, opponents, s.laneSafeDistance)
                    : ReceiverOpenness(mate.Position, opponents, s.receiverSafeDistance);

                float score = angleScore * s.weightAngle + distanceScore * s.weightDistance + openScore * s.weightLane;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = mate;
                }
            }
            return best;
        }

        /// <summary>1 = ningún rival cerca de la línea de pase; 0 = un rival encima de ella.</summary>
        public static float LaneOpenness(Vector3 from, Vector3 to, IReadOnlyList<FootballPlayer> opponents, float safeDistance)
        {
            if (opponents == null) return 1f;
            float openness = 1f;
            foreach (FootballPlayer opponent in opponents)
            {
                float distance = KickMath.DistancePointToSegmentFlat(opponent.Position, from, to, out float t);
                // Rivales a la espalda del pasador no cortan el pase.
                if (t <= 0.02f) continue;
                openness = Mathf.Min(openness, Mathf.Clamp01((distance - 0.5f) / safeDistance));
            }
            return openness;
        }

        /// <summary>1 = receptor sin rivales cerca; 0 = rival pegado.</summary>
        public static float ReceiverOpenness(Vector3 receiver, IReadOnlyList<FootballPlayer> opponents, float safeDistance)
        {
            if (opponents == null) return 1f;
            float nearest = float.MaxValue;
            foreach (FootballPlayer opponent in opponents)
            {
                nearest = Mathf.Min(nearest, KickMath.FlatDistance(opponent.Position, receiver));
            }
            return Mathf.Clamp01(nearest / safeDistance);
        }
    }
}
