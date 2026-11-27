using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Consultas espaciales compartidas por los comportamientos de IA (funciones puras).</summary>
    public static class AIQueries
    {
        /// <summary>Dirección de ataque del equipo en el mundo (+X o −X).</summary>
        public static Vector3 Forward(Team team) => new Vector3(team.AttackDirection, 0f, 0f);

        /// <summary>Metros que se avanza hacia la portería rival yendo de <paramref name="from"/> a <paramref name="to"/>.</summary>
        public static float Progress(Team team, Vector3 from, Vector3 to) => (to.x - from.x) * team.AttackDirection;

        public static FootballPlayer NearestOpponent(FootballPlayer player, Vector3 point, out float distance)
        {
            FootballPlayer nearest = null;
            distance = float.MaxValue;
            Team opponents = player.Team.Opponent;
            if (opponents == null) return null;
            foreach (FootballPlayer opponent in opponents.Players)
            {
                float d = KickMath.FlatDistance(opponent.Position, point);
                if (d < distance)
                {
                    distance = d;
                    nearest = opponent;
                }
            }
            return nearest;
        }

        /// <summary>Ángulo (grados) con el que se ve la boca de gol desde un punto.</summary>
        public static float GoalMouthAngle(Vector3 from, Vector3 goalCenter, float goalWidth)
        {
            Vector3 left = goalCenter + new Vector3(0f, 0f, goalWidth * 0.5f);
            Vector3 right = goalCenter - new Vector3(0f, 0f, goalWidth * 0.5f);
            return Vector3.Angle(KickMath.Flat(left - from), KickMath.Flat(right - from));
        }

        /// <summary>¿Hay algún jugador de campo rival tapando la línea de tiro?</summary>
        public static bool ShotLaneClear(Vector3 from, Vector3 to, Team opponents, float radius)
        {
            foreach (FootballPlayer opponent in opponents.Players)
            {
                if (opponent.IsGoalkeeper) continue;
                float d = KickMath.DistancePointToSegmentFlat(opponent.Position, from, to, out float t);
                if (t > 0.05f && t < 0.98f && d < radius) return false;
            }
            return true;
        }

        /// <summary>
        /// Posición aproximada del balón dentro de <paramref name="seconds"/> segundos, teniendo en cuenta que
        /// rueda frenando (misma desaceleración que <see cref="Ball"/>).
        /// </summary>
        public static Vector3 PredictBall(Ball ball, Pitch pitch, float seconds)
        {
            Vector3 velocity = KickMath.Flat(ball.Velocity);
            float speed = velocity.magnitude;
            Vector3 position = KickMath.Flat(ball.Position);
            if (speed < 0.05f) return position;

            float travel;
            if (ball.IsGrounded)
            {
                float decel = Mathf.Max(0.01f, ball.Settings.rollingDeceleration);
                float stopTime = speed / decel;
                float t = Mathf.Min(seconds, stopTime);
                travel = speed * t - 0.5f * decel * t * t;
            }
            else
            {
                travel = speed * seconds;
            }
            return pitch.ClampToPlayingArea(position + velocity / speed * travel, 0.3f);
        }

        /// <summary>Punto donde un jugador puede alcanzar el balón suelto (se refina dos veces).</summary>
        public static Vector3 InterceptPoint(FootballPlayer player, Ball ball, Pitch pitch)
        {
            Vector3 point = KickMath.Flat(ball.Position);
            float speed = Mathf.Max(1f, player.Motor.SprintSpeed);
            for (int i = 0; i < 2; i++)
            {
                float time = KickMath.FlatDistance(player.Position, point) / speed;
                point = PredictBall(ball, pitch, time);
            }
            return point;
        }

        public static bool IsProtectedKeeper(FootballPlayer player, Pitch pitch)
        {
            return player != null && player.IsGoalkeeper &&
                   pitch.IsInPenaltyArea(player.Position, -player.Team.AttackDirection);
        }
    }
}
