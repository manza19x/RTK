using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Dimensiones y geometría del campo. Fuente única de verdad para líneas, porterías y reglas espaciales.
    /// Convención: el campo está centrado en el origen de este objeto, sin rotación; las porterías están en ±X
    /// y las bandas en ±Z. Medidas por defecto de fútbol 7 (decisión de alcance; ver README).
    /// </summary>
    public class Pitch : MonoBehaviour
    {
        [Header("Terreno (m)")]
        [SerializeField] private float length = 72f;
        [SerializeField] private float width = 46f;
        [SerializeField] private float lineWidth = 0.12f;

        [Header("Porterías (m)")]
        [SerializeField] private float goalWidth = 6f;
        [SerializeField] private float goalHeight = 2.1f;
        [SerializeField] private float goalDepth = 1.8f;

        [Header("Áreas (m)")]
        [SerializeField] private float penaltyAreaDepth = 11f;
        [SerializeField] private float penaltyAreaWidth = 26f;
        [SerializeField] private float goalAreaDepth = 4f;
        [SerializeField] private float goalAreaWidth = 12f;
        [SerializeField] private float penaltySpotDistance = 9f;
        [SerializeField] private float centerCircleRadius = 6f;
        [SerializeField] private float cornerArcRadius = 1f;

        public float Length => length;
        public float Width => width;
        public float HalfLength => length * 0.5f;
        public float HalfWidth => width * 0.5f;
        public float LineWidth => lineWidth;
        public float GoalWidth => goalWidth;
        public float GoalHeight => goalHeight;
        public float GoalDepth => goalDepth;
        public float PenaltyAreaDepth => penaltyAreaDepth;
        public float PenaltyAreaWidth => penaltyAreaWidth;
        public float GoalAreaDepth => goalAreaDepth;
        public float GoalAreaWidth => goalAreaWidth;
        public float PenaltySpotDistance => penaltySpotDistance;
        public float CenterCircleRadius => centerCircleRadius;
        public float CornerArcRadius => cornerArcRadius;

        public Vector3 Center => Flat(transform.position);

        /// <summary>Centro de la línea de gol de la portería en <paramref name="side"/> (-1 = -X, +1 = +X), a ras de suelo.</summary>
        public Vector3 GoalCenter(int side) => Center + new Vector3(Mathf.Sign(side) * HalfLength, 0f, 0f);

        public bool IsInsidePlayingArea(Vector3 worldPosition, float margin = 0f)
        {
            Vector3 p = worldPosition - Center;
            return Mathf.Abs(p.x) <= HalfLength + margin && Mathf.Abs(p.z) <= HalfWidth + margin;
        }

        /// <summary>¿Está el punto dentro del área grande de la portería en <paramref name="side"/>?</summary>
        public bool IsInPenaltyArea(Vector3 worldPosition, int side)
        {
            Vector3 p = worldPosition - Center;
            float s = Mathf.Sign(side);
            float distanceFromGoalLine = HalfLength - p.x * s;
            return distanceFromGoalLine >= 0f && distanceFromGoalLine <= penaltyAreaDepth
                   && Mathf.Abs(p.z) <= penaltyAreaWidth * 0.5f;
        }

        public Vector3 ClampToPlayingArea(Vector3 worldPosition, float margin = 0f)
        {
            Vector3 p = worldPosition - Center;
            p.x = Mathf.Clamp(p.x, -HalfLength + margin, HalfLength - margin);
            p.z = Mathf.Clamp(p.z, -HalfWidth + margin, HalfWidth - margin);
            p.y = 0f;
            return Center + p;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawWireCube(Center, new Vector3(length, 0.01f, width));
            Gizmos.color = Color.yellow;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 g = GoalCenter(side) + new Vector3(Mathf.Sign(side) * goalDepth * 0.5f, goalHeight * 0.5f, 0f);
                Gizmos.DrawWireCube(g, new Vector3(goalDepth, goalHeight, goalWidth));
            }
        }
    }
}
