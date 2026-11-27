using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FutbolTotal
{
    /// <summary>
    /// Público: miles de espectadores dibujados con instanciado por GPU (una llamada de dibujo por color y bloque
    /// de 1023), sin un GameObject por persona. Se balancean un poco y los aficionados del equipo que marca
    /// saltan durante unos segundos. Solo funciona en Play (las posiciones las da <see cref="StadiumGenerator"/>).
    /// </summary>
    [RequireComponent(typeof(StadiumGenerator))]
    public class CrowdRenderer : MonoBehaviour
    {
        [Tooltip("Fracción de asientos ocupados.")]
        [SerializeField, Range(0f, 1f)] private float occupancy = 0.82f;
        [SerializeField] private Vector3 spectatorScale = new Vector3(0.42f, 0.5f, 0.32f);
        [SerializeField] private float celebrationSeconds = 4f;
        [SerializeField] private int seed = 7;

        private const int BatchSize = 1023;

        private class Group
        {
            public Material Material;
            public int Affiliation; // 0 local, 1 visitante, 2 neutral
            public readonly List<Vector3> Positions = new List<Vector3>();
            public readonly List<Quaternion> Rotations = new List<Quaternion>();
            public readonly List<float> Phases = new List<float>();
            public Matrix4x4[][] Batches;
        }

        private readonly List<Group> groups = new List<Group>();
        private Mesh mesh;
        private MatchManager manager;
        private float homeCelebrateUntil, awayCelebrateUntil;

        private void Start()
        {
            var context = MatchContext.Current;
            mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            Color homeA = context != null ? context.HomeTeam.Settings.primaryColor : new Color(0.86f, 0.24f, 0.18f);
            Color homeB = context != null ? context.HomeTeam.Settings.secondaryColor : Color.white;
            Color awayA = context != null ? context.AwayTeam.Settings.primaryColor : new Color(0.16f, 0.36f, 0.82f);
            Color awayB = context != null ? context.AwayTeam.Settings.secondaryColor : new Color(0.1f, 0.12f, 0.2f);

            AddGroup(homeA, 0);
            AddGroup(homeB, 0);
            AddGroup(awayA, 1);
            AddGroup(awayB, 1);
            AddGroup(new Color(0.35f, 0.35f, 0.38f), 2);
            AddGroup(new Color(0.75f, 0.68f, 0.55f), 2);

            var positions = new List<Vector3>();
            var rotations = new List<Quaternion>();
            GetComponent<StadiumGenerator>().GetSpectatorSpots(positions, rotations);
            var random = new System.Random(seed);
            for (int i = 0; i < positions.Count; i++)
            {
                if (random.NextDouble() > occupancy) continue;
                // Mitad de cada estadio para cada afición (por el lado del campo), con algo de mezcla y neutrales.
                double roll = random.NextDouble();
                bool homeEnd = positions[i].x < 0f;
                int group = roll < 0.1 ? 4 + random.Next(2)
                          : roll < 0.75 ? (homeEnd ? 0 : 2) + random.Next(2)
                          : (homeEnd ? 2 : 0) + random.Next(2);
                Group g = groups[group];
                g.Positions.Add(positions[i] + Vector3.up * (spectatorScale.y - 0.05f));
                g.Rotations.Add(rotations[i]);
                g.Phases.Add((float)random.NextDouble() * Mathf.PI * 2f);
            }

            foreach (Group g in groups)
            {
                int batches = Mathf.CeilToInt(g.Positions.Count / (float)BatchSize);
                g.Batches = new Matrix4x4[batches][];
                for (int b = 0; b < batches; b++)
                {
                    g.Batches[b] = new Matrix4x4[Mathf.Min(BatchSize, g.Positions.Count - b * BatchSize)];
                }
            }

            manager = context != null ? context.Rules as MatchManager : null;
            if (manager != null) manager.GoalScored += OnGoal;
        }

        private void OnDestroy()
        {
            if (manager != null) manager.GoalScored -= OnGoal;
            foreach (Group g in groups)
            {
                if (g.Material != null) Destroy(g.Material);
            }
        }

        private void AddGroup(Color color, int affiliation)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color, enableInstancing = true };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            groups.Add(new Group { Material = material, Affiliation = affiliation });
        }

        private void OnGoal(Team scorer)
        {
            if (scorer.Side == TeamSide.Home) homeCelebrateUntil = Time.time + celebrationSeconds;
            else awayCelebrateUntil = Time.time + celebrationSeconds;
        }

        private void Update()
        {
            if (mesh == null) return;
            float time = Time.time;
            foreach (Group g in groups)
            {
                bool celebrating = g.Affiliation == 0 ? time < homeCelebrateUntil
                                 : g.Affiliation == 1 ? time < awayCelebrateUntil : false;
                for (int b = 0; b < g.Batches.Length; b++)
                {
                    Matrix4x4[] batch = g.Batches[b];
                    for (int i = 0; i < batch.Length; i++)
                    {
                        int index = b * BatchSize + i;
                        float phase = g.Phases[index];
                        float bob = celebrating
                            ? Mathf.Abs(Mathf.Sin(time * 7f + phase)) * 0.45f
                            : Mathf.Sin(time * (1.5f + phase * 0.2f) + phase) * 0.02f;
                        batch[i] = Matrix4x4.TRS(g.Positions[index] + Vector3.up * bob, g.Rotations[index], spectatorScale);
                    }
                    Graphics.DrawMeshInstanced(mesh, 0, g.Material, batch, batch.Length, null, ShadowCastingMode.Off, true);
                }
            }
        }
    }
}
