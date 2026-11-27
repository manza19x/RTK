using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Resultado de planificar un tiro (útil también para depurar y para la IA).</summary>
    public struct ShotPlan
    {
        public Vector3 Velocity;
        /// <summary>Punto de la portería al que se apuntaba antes de aplicar el error.</summary>
        public Vector3 AimPoint;
        /// <summary>Error máximo aplicado, en grados.</summary>
        public float ErrorDegrees;
    }

    /// <summary>
    /// Calcula un tiro:
    /// - Dirección: con el stick inclinado se apunta a un lado de la portería (arriba = palo lejano a la cámara);
    ///   sin inclinar, se tira hacia donde mira el jugador, limitado a la portería (asistencia suave).
    /// - Potencia (carga): más velocidad y más altura; por encima de un umbral el balón tiende a irse alto.
    /// - Error: crece con la distancia, la presión de un rival cercano, tirar mal perfilado y tirar esprintando.
    ///   El error es aleatorio dentro de ese margen, pero el margen se explica siempre por esos factores.
    /// </summary>
    public static class ShotCalculator
    {
        public static ShotPlan Plan(FootballPlayer shooter, Vector3 aimDirection, float power01, bool hasDirectionalInput,
                                    ActionSettings s, Ball ball, Pitch pitch)
        {
            int side = shooter.Team.AttackDirection;
            Vector3 goal = pitch.GoalCenter(side);
            Vector3 from = ball.Position;
            float usableHalfWidth = Mathf.Max(0.1f, pitch.GoalWidth * 0.5f - s.shotPostMargin);

            float lateral;
            if (hasDirectionalInput)
            {
                // Eje Z del mundo = arriba/abajo en pantalla con la cámara lateral.
                lateral = Mathf.Clamp(aimDirection.z / s.shotStickFullDeflection, -1f, 1f) * usableHalfWidth;
            }
            else
            {
                Vector3 facing = shooter.Forward;
                float dx = goal.x - from.x;
                if (facing.x * Mathf.Sign(dx) > 0.2f)
                {
                    float rayLength = dx / facing.x;
                    lateral = Mathf.Clamp(from.z + facing.z * rayLength - goal.z, -usableHalfWidth, usableHalfWidth);
                }
                else
                {
                    lateral = 0f;
                }
            }

            power01 = Mathf.Clamp01(power01);
            float height = Mathf.Lerp(s.shotMinHeight, s.shotMaxHeight, power01);
            if (power01 > s.overPowerThreshold)
            {
                height += (power01 - s.overPowerThreshold) / (1f - s.overPowerThreshold) * s.overPowerExtraHeight;
            }

            Vector3 aimPoint = new Vector3(goal.x, height, goal.z + lateral);
            float speed = Mathf.Lerp(s.shotMinSpeed, s.shotMaxSpeed, power01);
            float distance = KickMath.FlatDistance(from, aimPoint);
            Vector3 velocity = KickMath.VelocityForFlightTime(from, aimPoint, distance / speed);

            float error = ComputeError(shooter, from, aimPoint, distance, s);
            float yawError = Random.Range(-error, error);
            float pitchError = Random.Range(-error, error) * 0.5f;

            velocity = Quaternion.AngleAxis(yawError, Vector3.up) * velocity;
            Vector3 right = Vector3.Cross(Vector3.up, KickMath.Flat(velocity).normalized);
            velocity = Quaternion.AngleAxis(-pitchError, right) * velocity;

            return new ShotPlan { Velocity = velocity, AimPoint = aimPoint, ErrorDegrees = error };
        }

        /// <summary>Margen de error en grados. Pública para poder mostrarlo en pantalla o usarlo en la IA.</summary>
        public static float ComputeError(FootballPlayer shooter, Vector3 from, Vector3 aimPoint, float distance, ActionSettings s)
        {
            float error = s.shotBaseErrorDegrees + s.shotErrorPerMeter * distance;

            Team opponents = shooter.Team.Opponent;
            if (opponents != null)
            {
                float nearest = float.MaxValue;
                foreach (FootballPlayer opponent in opponents.Players)
                {
                    nearest = Mathf.Min(nearest, KickMath.FlatDistance(opponent.Position, shooter.Position));
                }
                float pressure = 1f - Mathf.Clamp01((nearest - 0.8f) / Mathf.Max(0.1f, s.pressureRadius - 0.8f));
                error += pressure * s.pressureErrorDegrees;
            }

            float bodyAngle = Vector3.Angle(shooter.Forward, KickMath.Flat(aimPoint - from));
            error += Mathf.Clamp01(bodyAngle / 90f) * s.bodyAngleErrorDegrees;

            if (shooter.Motor.IsSprinting) error += s.sprintErrorDegrees;
            return error * shooter.Team.Strength.ShotErrorFactor;
        }
    }
}
