using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Posición de cada jugador dentro de la formación según la jugada. Partiendo de su puesto base:
    /// - todo el bloque se desplaza a lo largo del campo siguiendo al balón;
    /// - con balón el equipo se adelanta, sin balón se repliega;
    /// - el equipo se cierra hacia el lado del balón;
    /// - al defender, los defensas nunca quedan por delante del balón.
    /// Así el equipo mantiene su forma y la recupera sola después de cada jugada.
    /// </summary>
    public static class FormationShape
    {
        public static Vector3 Target(FootballPlayer player, Vector3 ballPosition, bool attacking, AISettings s)
        {
            Team team = player.Team;
            Pitch pitch = MatchContext.Current.Pitch;
            Vector2 slot = team.SlotBasePosition(player.FormationIndex);

            float ballDepth = Mathf.InverseLerp(-pitch.HalfLength, pitch.HalfLength,
                                                (ballPosition.x - pitch.Center.x) * team.AttackDirection);
            float depth = slot.x + (ballDepth - 0.5f) * s.blockShift + (attacking ? s.attackPush : -s.defendDrop);
            if (!attacking && player.Role == PlayerRole.Defender)
            {
                depth = Mathf.Min(depth, ballDepth - s.defenderGoalSideMargin);
            }
            depth = Mathf.Clamp(depth, s.minDepth, s.maxDepth);

            Vector3 world = team.FormationToWorld(new Vector2(depth, slot.y));
            world.z = Mathf.Lerp(world.z, ballPosition.z, s.lateralCompression);
            return pitch.ClampToPlayingArea(world, 1f);
        }
    }
}
