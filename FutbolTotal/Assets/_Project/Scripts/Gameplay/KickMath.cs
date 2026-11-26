using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Cálculos de trayectoria. Funciones puras: no dependen de la escena, así son fáciles de probar y reutilizar
    /// por el jugador humano y por la IA.
    /// Suponen que el balón rueda con desaceleración constante y vuela sin rozamiento del aire
    /// (ver <see cref="BallSettings.rollingDeceleration"/>), lo que hace que los pases sean predecibles.
    /// </summary>
    public static class KickMath
    {
        public static float Gravity => -Physics.gravity.y;

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Velocidad inicial para que un balón raso recorra <paramref name="distance"/> y llegue con <paramref name="arrivalSpeed"/>.</summary>
        public static float GroundPassSpeed(float distance, float arrivalSpeed, float deceleration)
        {
            return Mathf.Sqrt(arrivalSpeed * arrivalSpeed + 2f * deceleration * Mathf.Max(0f, distance));
        }

        /// <summary>Tiempo que tarda un balón raso en recorrer <paramref name="distance"/>. Si se para antes, devuelve el tiempo hasta pararse.</summary>
        public static float GroundTravelTime(float distance, float initialSpeed, float deceleration)
        {
            if (deceleration <= 0f) return distance / Mathf.Max(0.01f, initialSpeed);
            float discriminant = initialSpeed * initialSpeed - 2f * deceleration * distance;
            if (discriminant <= 0f) return initialSpeed / deceleration;
            return (initialSpeed - Mathf.Sqrt(discriminant)) / deceleration;
        }

        /// <summary>Velocidad para ir de <paramref name="from"/> a <paramref name="to"/> en un tiro parabólico de duración <paramref name="flightTime"/>.</summary>
        public static Vector3 VelocityForFlightTime(Vector3 from, Vector3 to, float flightTime)
        {
            flightTime = Mathf.Max(0.05f, flightTime);
            Vector3 delta = to - from;
            Vector3 horizontal = Flat(delta) / flightTime;
            float vertical = (delta.y + 0.5f * Gravity * flightTime * flightTime) / flightTime;
            return horizontal + Vector3.up * vertical;
        }

        /// <summary>Distancia en el plano XZ de un punto a un segmento. <paramref name="t"/> es la posición proyectada (0 = a, 1 = b).</summary>
        public static float DistancePointToSegmentFlat(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            Vector3 ab = Flat(b - a);
            Vector3 ap = Flat(point - a);
            float lengthSq = ab.sqrMagnitude;
            t = lengthSq > 1e-6f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSq) : 0f;
            Vector3 closest = Flat(a) + ab * t;
            return FlatDistance(Flat(point), closest);
        }
    }
}
