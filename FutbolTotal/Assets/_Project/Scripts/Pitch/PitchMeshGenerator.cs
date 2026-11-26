using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Dibuja las marcas del campo (líneas, áreas, círculos) o las franjas del césped a partir de <see cref="Pitch"/>.
    /// Se genera al vuelo (también en el editor), así cambiar las medidas del campo actualiza el dibujo
    /// sin tener que rehacer texturas ni modelos.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PitchMeshGenerator : MonoBehaviour
    {
        public enum Mode
        {
            Lines,
            Stripes
        }

        [SerializeField] private Pitch pitch;
        [SerializeField] private Mode mode = Mode.Lines;
        [Tooltip("Número de franjas de corte del césped (solo modo Stripes).")]
        [SerializeField] private int stripeCount = 12;
        [Tooltip("Altura sobre el césped para evitar parpadeos (z-fighting).")]
        [SerializeField] private float heightOffset = 0.01f;

        private Mesh mesh;
        private bool dirty = true;

        private void OnEnable()
        {
            dirty = true;
            Rebuild();
        }

        private void OnValidate()
        {
            dirty = true;
        }

        private void Update()
        {
            if (dirty) Rebuild();
        }

        private void OnDisable()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }

        private void Rebuild()
        {
            if (pitch == null) pitch = GetComponentInParent<Pitch>();
            if (pitch == null) return;
            dirty = false;

            var builder = new FlatMeshBuilder(heightOffset);
            if (mode == Mode.Lines) BuildLines(builder);
            else BuildStripes(builder);

            if (mesh == null)
            {
                mesh = new Mesh { hideFlags = HideFlags.DontSave };
            }
            builder.ToMesh(mesh, mode == Mode.Lines ? "PitchLines" : "PitchStripes");
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void BuildStripes(FlatMeshBuilder b)
        {
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;
            int count = Mathf.Max(2, stripeCount);
            float stripe = pitch.Length / count;
            for (int i = 0; i < count; i += 2)
            {
                float x0 = -L + i * stripe;
                b.AddRect(x0, x0 + stripe, -W, W);
            }
        }

        private void BuildLines(FlatMeshBuilder b)
        {
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;
            float w = pitch.LineWidth;
            const int circleSegments = 64;

            // Bandas, líneas de fondo y línea de medio campo.
            b.AddRect(-L - w * 0.5f, L + w * 0.5f, W - w * 0.5f, W + w * 0.5f);
            b.AddRect(-L - w * 0.5f, L + w * 0.5f, -W - w * 0.5f, -W + w * 0.5f);
            b.AddRect(-L - w * 0.5f, -L + w * 0.5f, -W, W);
            b.AddRect(L - w * 0.5f, L + w * 0.5f, -W, W);
            b.AddRect(-w * 0.5f, w * 0.5f, -W, W);

            // Círculo central y punto central.
            b.AddArc(Vector2.zero, pitch.CenterCircleRadius, w, 0f, 360f, circleSegments);
            b.AddDisc(Vector2.zero, 0.2f, 16);

            for (int side = -1; side <= 1; side += 2)
            {
                BuildBox(b, side, pitch.PenaltyAreaDepth, pitch.PenaltyAreaWidth, w);
                BuildBox(b, side, pitch.GoalAreaDepth, pitch.GoalAreaWidth, w);

                // Punto de penalti.
                Vector2 spot = new Vector2(side * (L - pitch.PenaltySpotDistance), 0f);
                b.AddDisc(spot, 0.18f, 16);

                // Semicírculo del área: la parte del círculo centrado en el punto de penalti que queda fuera del área.
                float radius = pitch.CenterCircleRadius;
                float outside = pitch.PenaltyAreaDepth - pitch.PenaltySpotDistance;
                if (radius > outside)
                {
                    float halfSpan = Mathf.Acos(outside / radius) * Mathf.Rad2Deg;
                    float towardsCenter = side > 0 ? 180f : 0f;
                    b.AddArc(spot, radius, w, towardsCenter - halfSpan, towardsCenter + halfSpan, 24);
                }

                // Arcos de córner.
                for (int band = -1; band <= 1; band += 2)
                {
                    Vector2 corner = new Vector2(side * L, band * W);
                    float start = Mathf.Atan2(0f, -side) * Mathf.Rad2Deg;
                    float end = start + Mathf.DeltaAngle(start, Mathf.Atan2(-band, 0f) * Mathf.Rad2Deg);
                    b.AddArc(corner, pitch.CornerArcRadius, w, start, end, 12);
                }
            }
        }

        /// <summary>Área rectangular pegada a la línea de gol en <paramref name="side"/>.</summary>
        private void BuildBox(FlatMeshBuilder b, int side, float depth, float boxWidth, float w)
        {
            float L = pitch.HalfLength;
            float halfBox = boxWidth * 0.5f;
            float frontX = side * (L - depth);
            float goalX = side * L;
            float minX = Mathf.Min(frontX, goalX);
            float maxX = Mathf.Max(frontX, goalX);

            b.AddRect(frontX - w * 0.5f, frontX + w * 0.5f, -halfBox - w * 0.5f, halfBox + w * 0.5f);
            b.AddRect(minX, maxX, halfBox - w * 0.5f, halfBox + w * 0.5f);
            b.AddRect(minX, maxX, -halfBox - w * 0.5f, -halfBox + w * 0.5f);
        }
    }
}
