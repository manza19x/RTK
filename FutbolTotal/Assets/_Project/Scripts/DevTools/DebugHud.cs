using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Superposición técnica para desarrollo (IMGUI), separada de la interfaz de juego (<see cref="MatchHud"/>):
    /// - F2 / L3: etiqueta sobre cada jugador con su rol de IA (o «TÚ»), para ver qué decide la IA;
    /// - F3: panel con datos internos (resistencia, cargas, balón, FPS).
    /// Ambos empiezan ocultos.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private HumanTeamController human;
        [Tooltip("MatchManager o TrainingHarness: cualquier componente que implemente IMatchStatus.")]
        [SerializeField] private MonoBehaviour statusSource;
        [SerializeField] private bool showRolesAtStart;
        [SerializeField] private bool showPanelAtStart;

        private const float Margin = 12f;
        private const float Padding = 10f;
        private const float LineHeight = 21f;
        private const float PanelWidth = 340f;

        private readonly Dictionary<FootballPlayer, PlayerBrain> brains = new Dictionary<FootballPlayer, PlayerBrain>();
        private IMatchStatus status;
        private bool showRoles;
        private bool showPanel;
        private float smoothedFps;

        private GUIStyle label;
        private GUIStyle roleLabel;
        private Texture2D white;
        private Texture2D panelBackground;

        private void Start()
        {
            if (human == null) human = Object.FindFirstObjectByType<HumanTeamController>();
            status = statusSource as IMatchStatus;
            showRoles = showRolesAtStart;
            showPanel = showPanelAtStart;
        }

        private void Update()
        {
            if (human != null && human.Input != null)
            {
                if (human.Input.ToggleAIDebug.WasPressedThisFrame()) showRoles = !showRoles;
                if (human.Input.ToggleDebugPanel.WasPressedThisFrame()) showPanel = !showPanel;
            }
            if (Time.unscaledDeltaTime > 0f) smoothedFps = Mathf.Lerp(smoothedFps, 1f / Time.unscaledDeltaTime, 0.05f);
        }

        private void OnDestroy()
        {
            if (white != null) Destroy(white);
            if (panelBackground != null) Destroy(panelBackground);
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            white = MakeTexture(Color.white);
            panelBackground = MakeTexture(new Color(0f, 0f, 0f, 0.6f));
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = false, clipping = TextClipping.Clip };
            label.normal.textColor = Color.white;
            roleLabel = new GUIStyle(label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            var context = MatchContext.Current;
            if (context == null || human == null || human.Input == null) return;
            if (!showRoles && !showPanel) return;
            EnsureStyles();

            if (showRoles) DrawRoleLabels(context);
            if (showPanel) DrawPanel(context);
        }

        /// <summary>Etiqueta sobre cada jugador con su rol de IA (o «TÚ»): hace visibles las decisiones de la IA.</summary>
        private void DrawRoleLabels(MatchContext context)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            foreach (FootballPlayer player in context.AllPlayers)
            {
                if (!brains.TryGetValue(player, out PlayerBrain brain))
                {
                    brain = player.GetComponent<PlayerBrain>();
                    brains[player] = brain;
                }
                string text = player.IsHumanControlled ? "TÚ" : brain != null ? AIRoleNames.Short(brain.Role) : "";
                Vector3 screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * 2.1f);
                if (screen.z <= 0f) continue;
                Rect rect = new Rect(screen.x - 40f, Screen.height - screen.y - 10f, 80f, 20f);
                GUI.color = Color.black;
                GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, roleLabel);
                GUI.color = player.Team.Side == TeamSide.Home ? new Color(1f, 0.85f, 0.8f) : new Color(0.8f, 0.9f, 1f);
                GUI.Label(rect, text, roleLabel);
                GUI.color = Color.white;
            }
        }

        private void DrawPanel(MatchContext context)
        {
            FootballPlayer player = human.ActivePlayer;
            FootballInput input = human.Input;
            Ball ball = context.Ball;

            // Debajo del marcador del HUD, a la izquierda, para no taparlo.
            Rect panel = new Rect(Margin, Screen.height * 0.25f, PanelWidth, Padding * 2f + LineHeight * 8);
            GUI.DrawTexture(panel, panelBackground);
            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float width = panel.width - Padding * 2f;

            Line(x, ref y, width, "<b>Depuración (F3)</b>");
            if (status != null) Line(x, ref y, width, $"Reglas: {status.PhaseText} · {status.ClockText} · en juego: {status.IsBallInPlay}");
            else y += LineHeight;
            if (player != null)
            {
                Line(x, ref y, width, $"Activo: {player} · {player.Motor.Speed:0.0} m/s{(player.Motor.IsSprinting ? " · sprint" : "")}");
                Bar(x, ref y, "Resistencia", player.Stamina.Current01, player.Stamina.IsExhausted ? Color.red : new Color(0.3f, 0.9f, 0.4f));
            }
            else
            {
                y += LineHeight * 2f;
            }
            Bar(x, ref y, "Carga pase", input.ShortPass.Charge01, Color.cyan);
            Bar(x, ref y, "Carga tiro", input.Shot.Charge01, new Color(1f, 0.55f, 0.2f));
            string owner = ball.Owner != null ? ball.Owner.ToString() : "suelto";
            Line(x, ref y, width, $"Balón: {owner} · {ball.Velocity.magnitude:0.0} m/s · h {ball.Position.y:0.00} m");
            Line(x, ref y, width, $"{smoothedFps:0} FPS");
        }

        private void Line(float x, ref float y, float width, string text)
        {
            GUI.Label(new Rect(x, y, width, LineHeight), text, label);
            y += LineHeight;
        }

        private void Bar(float x, ref float y, string title, float value, Color color)
        {
            GUI.Label(new Rect(x, y, 100f, LineHeight), title, label);
            Rect bar = new Rect(x + 105f, y + 6f, 190f, 10f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(bar, white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(value), bar.height), white);
            GUI.color = previous;
            y += LineHeight;
        }
    }
}
