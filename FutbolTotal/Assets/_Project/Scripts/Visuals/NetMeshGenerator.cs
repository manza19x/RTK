using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Red de portería hecha de hilos (malla de tiras finas cruzadas), con una ligera caída en el fondo.
    /// Solo es visual: los choques del balón los siguen haciendo las cajas de colisión de la red.
    /// El objeto debe estar en el centro de la línea de gol, sin rotar.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NetMeshGenerator : MonoBehaviour
    {
        [SerializeField] private Pitch pitch;
        [Tooltip("Portería en -X (-1) o en +X (+1).")]
        [SerializeField] private int goalSide = 1;
        [Tooltip("Tamaño de cada cuadro de la red (m).")]
        [SerializeField] private float cellSize = 0.14f;
        [SerializeField] private float strandWidth = 0.016f;
        [Tooltip("Cuánto cae el centro del fondo de la red (m).")]
        [SerializeField] private float sag = 0.15f;

        private Mesh mesh;
        private bool dirty = true;

        private void OnEnable()
        {
            dirty = true;
            Rebuild();
        }

        private void OnValidate() => dirty = true;

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

            float s = Mathf.Sign(goalSide);
            float halfWidth = pitch.GoalWidth * 0.5f + 0.06f;
            float height = pitch.GoalHeight + 0.06f;
            float depth = pitch.GoalDepth;
            var builder = new MeshBuilder3D();

            // Fondo (plano YZ en x = profundidad), techo (plano XZ en y = altura) y laterales (planos XY en z = ±ancho).
            AddPanel(builder, p => new Vector3(s * depth + s * BackSag(p.x, p.y), p.y * height, (p.x * 2f - 1f) * halfWidth),
                     pitch.GoalWidth, height, Vector3.forward, Vector3.up, Vector3.right);
            AddPanel(builder, p => new Vector3(s * p.y * depth, height, (p.x * 2f - 1f) * halfWidth),
                     pitch.GoalWidth, depth, Vector3.forward, Vector3.right, Vector3.up);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * halfWidth;
                AddPanel(builder, p => new Vector3(s * p.x * depth, p.y * height, z),
                         depth, height, Vector3.right, Vector3.up, Vector3.forward);
            }

            if (mesh == null) mesh = new Mesh { hideFlags = HideFlags.DontSave };
            builder.ToMesh(mesh, "GoalNet");
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        /// <summary>Caída del fondo: máxima en el centro, nula en los bordes.</summary>
        private float BackSag(float u, float v) => sag * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * v);

        /// <summary>
        /// Panel de red parametrizado por (u, v) en [0,1]². Traza hilos en las dos direcciones, partidos en tramos
        /// para que sigan la curva del panel.
        /// </summary>
        private void AddPanel(MeshBuilder3D builder, System.Func<Vector2, Vector3> point, float sizeU, float sizeV,
                              Vector3 axisU, Vector3 axisV, Vector3 normal)
        {
            int countU = Mathf.Max(1, Mathf.RoundToInt(sizeU / cellSize));
            int countV = Mathf.Max(1, Mathf.RoundToInt(sizeV / cellSize));

            for (int i = 0; i <= countU; i++)
            {
                float u = i / (float)countU;
                for (int j = 0; j < countV; j++)
                {
                    Vector3 a = point(new Vector2(u, j / (float)countV));
                    Vector3 b = point(new Vector2(u, (j + 1) / (float)countV));
                    builder.AddStrand(a, b, axisU, normal, strandWidth);
                }
            }
            for (int j = 0; j <= countV; j++)
            {
                float v = j / (float)countV;
                for (int i = 0; i < countU; i++)
                {
                    Vector3 a = point(new Vector2(i / (float)countU, v));
                    Vector3 b = point(new Vector2((i + 1) / (float)countU, v));
                    builder.AddStrand(a, b, axisV, normal, strandWidth);
                }
            }
        }
    }
}
