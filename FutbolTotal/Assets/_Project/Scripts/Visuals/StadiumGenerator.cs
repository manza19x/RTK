using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Estadio generado a partir de las medidas del campo: cuatro gradas escalonadas con asientos, cubiertas,
    /// vallas publicitarias alrededor del césped y cuatro torres de focos.
    /// Las mallas se generan al vuelo (también en el editor); los materiales los ponen los hijos con estos nombres:
    /// Concrete, Seats, Roof, Boards, Towers, Lamps (cada uno con MeshFilter y MeshRenderer).
    /// También calcula dónde se sienta el público (<see cref="GetSpectatorSpots"/>).
    /// </summary>
    [ExecuteAlways]
    public class StadiumGenerator : MonoBehaviour
    {
        [SerializeField] private Pitch pitch;
        [Header("Gradas")]
        [Tooltip("Distancia de las bandas a la primera fila.")]
        [SerializeField] private float sideGap = 9f;
        [Tooltip("Distancia de las líneas de fondo a la primera fila (más espacio por las porterías).")]
        [SerializeField] private float endGap = 10f;
        [SerializeField] private int rows = 14;
        [SerializeField] private float rowDepth = 0.85f;
        [SerializeField] private float rowRise = 0.45f;
        [Tooltip("Altura del muro delantero de la grada.")]
        [SerializeField] private float frontWall = 1.4f;
        [SerializeField] private float seatSpacing = 0.6f;

        [Header("Vallas")]
        [SerializeField] private float boardDistance = 3.5f;
        [SerializeField] private float boardHeight = 0.9f;
        [SerializeField] private float boardLength = 6f;

        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private bool dirty = true;

        public Pitch Pitch => pitch;

        private struct Stand
        {
            public Vector3 Origin;  // centro del borde delantero, a ras de suelo
            public Vector3 Along;   // dirección a lo largo de la grada
            public Vector3 Out;     // dirección hacia atrás (alejándose del campo)
            public float Length;
        }

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
            foreach (Mesh mesh in meshes.Values)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            meshes.Clear();
        }

        private Stand[] Stands()
        {
            if (pitch == null) pitch = GetComponentInParent<Pitch>();
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;
            float longLength = pitch.Length + endGap * 1.6f;
            float shortLength = pitch.Width + sideGap * 1.6f;
            return new[]
            {
                new Stand { Origin = new Vector3(0f, 0f, W + sideGap), Along = Vector3.right, Out = Vector3.forward, Length = longLength },
                new Stand { Origin = new Vector3(0f, 0f, -W - sideGap), Along = Vector3.left, Out = Vector3.back, Length = longLength },
                new Stand { Origin = new Vector3(L + endGap, 0f, 0f), Along = Vector3.back, Out = Vector3.right, Length = shortLength },
                new Stand { Origin = new Vector3(-L - endGap, 0f, 0f), Along = Vector3.forward, Out = Vector3.left, Length = shortLength },
            };
        }

        private void Rebuild()
        {
            if (pitch == null) pitch = GetComponentInParent<Pitch>();
            if (pitch == null) return;
            dirty = false;

            var concrete = new MeshBuilder3D();
            var seats = new MeshBuilder3D();
            var roof = new MeshBuilder3D();
            foreach (Stand stand in Stands()) BuildStand(stand, concrete, seats, roof);

            Assign("Concrete", concrete);
            Assign("Seats", seats);
            Assign("Roof", roof);
            Assign("Boards", BuildBoards());
            BuildTowers(out MeshBuilder3D towers, out MeshBuilder3D lamps);
            Assign("Towers", towers);
            Assign("Lamps", lamps);
        }

        private void BuildStand(Stand stand, MeshBuilder3D concrete, MeshBuilder3D seats, MeshBuilder3D roof)
        {
            Vector3 along = stand.Along;
            Vector3 back = stand.Out;
            float length = stand.Length;

            // Muro delantero.
            concrete.AddBox(OrientedCenter(stand, 0.15f, frontWall * 0.5f), OrientedSize(stand, length, 0.3f, frontWall));

            // Escalones: cada fila es un bloque macizo desde el suelo hasta su altura.
            for (int r = 0; r < rows; r++)
            {
                float depth = 0.3f + r * rowDepth + rowDepth * 0.5f;
                float top = frontWall + r * rowRise;
                concrete.AddBox(OrientedCenter(stand, depth, top * 0.5f), OrientedSize(stand, length, rowDepth, top));
                seats.AddBox(OrientedCenter(stand, depth + rowDepth * 0.15f, top + 0.2f), OrientedSize(stand, length - 0.6f, 0.4f, 0.4f));
            }

            // Muro trasero alto.
            float totalDepth = 0.3f + rows * rowDepth;
            float backTop = frontWall + rows * rowRise + 4.5f;
            concrete.AddBox(OrientedCenter(stand, totalDepth + 0.2f, backTop * 0.5f), OrientedSize(stand, length, 0.4f, backTop));

            // Cubierta: cubre las filas de atrás y vuela hacia el campo.
            float roofDepth = totalDepth * 0.85f;
            roof.AddBox(OrientedCenter(stand, totalDepth - roofDepth * 0.5f + 0.4f, backTop + 0.1f), OrientedSize(stand, length + 1f, roofDepth, 0.25f));

            // Laterales de la grada.
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 center = OrientedCenter(stand, totalDepth * 0.5f, backTop * 0.5f) + along * (end * (length * 0.5f + 0.2f));
                concrete.AddBox(center, OrientedSize(stand, 0.4f, totalDepth, backTop));
            }
        }

        /// <summary>Centro de una caja a <paramref name="depth"/> metros detrás del borde delantero y a altura <paramref name="height"/>.</summary>
        private static Vector3 OrientedCenter(Stand stand, float depth, float height) =>
            stand.Origin + stand.Out * depth + Vector3.up * height;

        /// <summary>Tamaño en ejes del mundo de una caja de largo × fondo × alto orientada con la grada.</summary>
        private static Vector3 OrientedSize(Stand stand, float length, float depth, float height)
        {
            Vector3 along = new Vector3(Mathf.Abs(stand.Along.x), 0f, Mathf.Abs(stand.Along.z));
            Vector3 back = new Vector3(Mathf.Abs(stand.Out.x), 0f, Mathf.Abs(stand.Out.z));
            return along * length + back * depth + Vector3.up * height;
        }

        private MeshBuilder3D BuildBoards()
        {
            var boards = new MeshBuilder3D();
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;

            // Bandas: vallas mirando al campo.
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * (W + boardDistance);
                AddBoardRow(boards, new Vector3(-L - 2f, 0f, z), new Vector3(L + 2f, 0f, z), -side * Vector3.forward);
            }
            // Fondos: dejando libre la portería.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (L + boardDistance + 0.5f);
                float gapHalf = pitch.GoalWidth * 0.5f + 2.5f;
                AddBoardRow(boards, new Vector3(x, 0f, -W - 2f), new Vector3(x, 0f, -gapHalf), -side * Vector3.right);
                AddBoardRow(boards, new Vector3(x, 0f, gapHalf), new Vector3(x, 0f, W + 2f), -side * Vector3.right);
            }
            return boards;
        }

        /// <summary>Fila de vallas entre dos puntos; cada tramo es un cuadrilátero con la textura completa.</summary>
        private void AddBoardRow(MeshBuilder3D boards, Vector3 from, Vector3 to, Vector3 facing)
        {
            float length = Vector3.Distance(from, to);
            int count = Mathf.Max(1, Mathf.RoundToInt(length / boardLength));
            Vector3 step = (to - from) / count;
            Vector3 up = Vector3.up * boardHeight;
            for (int i = 0; i < count; i++)
            {
                Vector3 a = from + step * i;
                Vector3 b = a + step;
                // Orden de vértices para que la cara mire a "facing".
                if (Vector3.Dot(Vector3.Cross(up, b - a), facing) > 0f) boards.AddQuad(a, a + up, b + up, b);
                else boards.AddQuad(b, b + up, a + up, a);
            }
        }

        private void BuildTowers(out MeshBuilder3D towers, out MeshBuilder3D lamps)
        {
            towers = new MeshBuilder3D();
            lamps = new MeshBuilder3D();
            float x = pitch.HalfLength + endGap + 4f;
            float z = pitch.HalfWidth + sideGap + 4f;
            const float height = 32f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 basePoint = new Vector3(sx * x, 0f, sz * z);
                    towers.AddBox(basePoint + Vector3.up * height * 0.5f, new Vector3(0.9f, height, 0.9f));
                    // Panel de focos inclinado hacia el centro (aproximado con una caja horizontal).
                    Vector3 panelCenter = basePoint + Vector3.up * (height + 1.2f) - new Vector3(sx, 0f, sz).normalized * 0.8f;
                    towers.AddBox(panelCenter, new Vector3(4.2f, 2.6f, 4.2f));
                    lamps.AddBox(panelCenter - new Vector3(sx, 0f, sz).normalized * 1.5f, new Vector3(3.6f, 2.0f, 3.6f));
                }
            }
        }

        private void Assign(string childName, MeshBuilder3D builder)
        {
            Transform child = transform.Find(childName);
            if (child == null) return;
            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter == null) return;
            if (!meshes.TryGetValue(childName, out Mesh mesh) || mesh == null)
            {
                mesh = new Mesh { hideFlags = HideFlags.DontSave };
                meshes[childName] = mesh;
            }
            builder.ToMesh(mesh, "Stadium_" + childName);
            filter.sharedMesh = mesh;
        }

        /// <summary>Posiciones y orientaciones (mirando al campo) de todos los asientos.</summary>
        public void GetSpectatorSpots(List<Vector3> positions, List<Quaternion> rotations)
        {
            if (pitch == null) pitch = GetComponentInParent<Pitch>();
            if (pitch == null) return;
            foreach (Stand stand in Stands())
            {
                Quaternion facing = Quaternion.LookRotation(-stand.Out, Vector3.up);
                int count = Mathf.FloorToInt((stand.Length - 1f) / seatSpacing);
                for (int r = 0; r < rows; r++)
                {
                    float depth = 0.3f + r * rowDepth + rowDepth * 0.5f;
                    float top = frontWall + r * rowRise;
                    for (int i = 0; i < count; i++)
                    {
                        float offset = (i - (count - 1) * 0.5f) * seatSpacing;
                        Vector3 position = transform.TransformPoint(stand.Origin + stand.Out * (depth + rowDepth * 0.15f) +
                                                                    stand.Along * offset + Vector3.up * (top + 0.4f));
                        positions.Add(position);
                        rotations.Add(facing);
                    }
                }
            }
        }
    }
}
