using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Anillo discreto bajo el jugador controlado, con una pequeña flecha que indica hacia dónde mira
    /// (importa para pasar y tirar). La malla se genera al iniciar; el material lo asigna la escena.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SelectionIndicator : MonoBehaviour
    {
        [SerializeField] private HumanTeamController controller;
        [SerializeField] private float radius = 0.75f;
        [SerializeField] private float thickness = 0.09f;
        [SerializeField] private float heightOffset = 0.03f;
        [Tooltip("Pulso de escala para que se distinga sin llamar demasiado la atención.")]
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float pulseSpeed = 4f;

        private MeshRenderer meshRenderer;
        private Mesh mesh;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var builder = new FlatMeshBuilder(0f);
            builder.AddArc(Vector2.zero, radius, thickness, 0f, 360f, 48);
            // Flecha de orientación (hacia +Z local = adelante).
            float tip = radius + 0.45f;
            float baseZ = radius + 0.12f;
            builder.AddTriangle(new Vector3(-0.18f, 0f, baseZ), new Vector3(0f, 0f, tip), new Vector3(0.18f, 0f, baseZ));
            mesh = builder.ToMesh(null, "SelectionIndicator");
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        private void LateUpdate()
        {
            FootballPlayer player = controller != null ? controller.ActivePlayer : null;
            meshRenderer.enabled = player != null;
            if (player == null) return;

            Vector3 position = player.transform.position;
            position.y = heightOffset;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(player.Forward, Vector3.up));
            float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(scale, 1f, scale);
        }
    }
}
