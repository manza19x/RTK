using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// HUD provisional de la Fase 1 con IMGUI (OnGUI): resistencia, carga de botones, posesión, marcador de
    /// entrenamiento y ayuda de controles. Se sustituye por la interfaz definitiva (uGUI) en la Fase 4.
    /// Usa GUI con rectángulos fijos (no GUILayout): así no depende de que las llamadas Begin/End de
    /// diseño coincidan entre los distintos eventos de OnGUI.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private HumanTeamController human;
        [Tooltip("MatchManager (partido) o TrainingHarness (entrenamiento): cualquier componente que implemente IMatchStatus.")]
        [SerializeField] private MonoBehaviour statusSource;
        [SerializeField] private bool showHelpAtStart = true;

        private const float Margin = 12f;
        private const float Padding = 10f;
        private const float LineHeight = 21f;
        private const float PanelWidth = 330f;

        private static readonly string[] KeyboardHelp =
        {
            "<b>Controles (teclado)</b>",
            "WASD: mover · Shift izq.: sprint",
            "J: pase corto (sin balón: entrada)",
            "L: pase largo / centro",
            "K: tiro (mantén para más potencia)",
            "Q: cambiar de jugador",
            "Esc: pausa · R: recolocar balón",
            "F2: ver el rol de la IA de cada jugador",
            "<size=11>Las acciones se cargan manteniendo y actúan al soltar.</size>"
        };

        private static readonly string[] GamepadHelp =
        {
            "<b>Controles (mando)</b>",
            "Stick izq.: mover · RT: sprint",
            "A: pase corto (sin balón: entrada)",
            "X: pase largo / centro",
            "B: tiro (mantén para más potencia)",
            "LB: cambiar de jugador",
            "Start: pausa · Select: recolocar balón",
            "L3: ver el rol de la IA de cada jugador",
            "<size=11>Las acciones se cargan manteniendo y actúan al soltar.</size>"
        };

        private bool showHelp;
        private bool showRoles;
        private readonly System.Collections.Generic.Dictionary<FootballPlayer, PlayerBrain> brains =
            new System.Collections.Generic.Dictionary<FootballPlayer, PlayerBrain>();
        private GUIStyle roleLabel;
        private float smoothedFps;

        private GUIStyle label;
        private GUIStyle centered;
        private GUIStyle big;
        private GUIStyle bigShadow;
        private Texture2D white;
        private Texture2D panelBackground;

        private IMatchStatus status;

        private void Start()
        {
            showHelp = showHelpAtStart;
            status = statusSource as IMatchStatus;
            if (statusSource != null && status == null)
            {
                Debug.LogWarning("[Fútbol Total] DebugHud: el componente de estado no implementa IMatchStatus.", this);
            }
        }

        private void Update()
        {
            if (human != null && human.Input != null && human.Input.ToggleHelp.WasPressedThisFrame()) showHelp = !showHelp;
            if (human != null && human.Input != null && human.Input.ToggleAIDebug.WasPressedThisFrame()) showRoles = !showRoles;
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
            panelBackground = MakeTexture(new Color(0f, 0f, 0f, 0.55f));

            label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = false, clipping = TextClipping.Clip };
            label.normal.textColor = Color.white;
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            roleLabel = new GUIStyle(centered) { fontSize = 11, fontStyle = FontStyle.Bold };
            big = new GUIStyle(centered) { fontSize = 38, fontStyle = FontStyle.Bold, wordWrap = true };
            bigShadow = new GUIStyle(big);
            bigShadow.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
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
            EnsureStyles();

            DrawStatusPanel(context);
            if (showRoles) DrawRoleLabels(context);
            if (showHelp) DrawHelpPanel();
            DrawCenterMessages();
        }

        private void DrawStatusPanel(MatchContext context)
        {
            FootballPlayer player = human.ActivePlayer;
            FootballInput input = human.Input;
            Ball ball = context.Ball;

            const int lines = 8;
            Rect panel = new Rect(Margin, Margin, PanelWidth, Padding * 2f + LineHeight * lines);
            GUI.DrawTexture(panel, panelBackground);

            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float width = panel.width - Padding * 2f;

            if (status != null)
            {
                Line(x, ref y, width, $"<b>{context.HomeTeam.Settings.shortName} {status.HomeGoals} - {status.AwayGoals} {context.AwayTeam.Settings.shortName}</b>   {status.ClockText}  <size=11>{status.PhaseText}</size>");
            }
            else
            {
                y += LineHeight;
            }

            if (player != null)
            {
                string state = player.Motor.IsStunned ? " <color=#ff8080>(aturdido)</color>"
                             : player.Motor.IsSprinting ? " <color=#ffd060>(sprint)</color>" : "";
                Line(x, ref y, width, $"Controlas: <b>{player}</b>{state}");
                Bar(x, ref y, "Resistencia", player.Stamina.Current01,
                    player.Stamina.IsExhausted ? Color.red : new Color(0.3f, 0.9f, 0.4f));
            }
            else
            {
                y += LineHeight * 2f;
            }

            Bar(x, ref y, "Pase corto", input.ShortPass.Charge01, Color.cyan);
            Bar(x, ref y, "Pase largo", input.LongPass.Charge01, new Color(0.4f, 0.6f, 1f));
            Bar(x, ref y, "Tiro", input.Shot.Charge01, new Color(1f, 0.55f, 0.2f));

            string owner = ball.Owner != null ? ball.Owner.ToString() : "suelto";
            Line(x, ref y, width, $"Balón: {owner} · {ball.Velocity.magnitude:0.0} m/s · altura {ball.Position.y:0.00} m");
            Line(x, ref y, width, $"<size=11>{smoothedFps:0} FPS · F1 / R3: ayuda</size>");
        }

        private void Line(float x, ref float y, float width, string text)
        {
            GUI.Label(new Rect(x, y, width, LineHeight), text, label);
            y += LineHeight;
        }

        private void Bar(float x, ref float y, string title, float value, Color color)
        {
            GUI.Label(new Rect(x, y, 95f, LineHeight), title, label);
            Rect bar = new Rect(x + 100f, y + 6f, 180f, 10f);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(bar, white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(value), bar.height), white);
            GUI.color = previous;

            y += LineHeight;
        }

        private void DrawHelpPanel()
        {
            string[] lines = human.Input.LastDeviceIsGamepad ? GamepadHelp : KeyboardHelp;
            Rect panel = new Rect(Screen.width - PanelWidth - Margin, Margin, PanelWidth,
                                  Padding * 2f + LineHeight * lines.Length);
            GUI.DrawTexture(panel, panelBackground);

            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float width = panel.width - Padding * 2f;
            foreach (string text in lines) Line(x, ref y, width, text);
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

        private void DrawCenterMessages()
        {
            if (status == null) return;
            Rect center = new Rect(0f, Screen.height * 0.25f, Screen.width, 130f);
            if (status.IsPaused)
            {
                GUI.Label(center, "PAUSA", big);
                GUI.Label(new Rect(0f, center.yMax, Screen.width, 30f), "Esc / Start para continuar", centered);
            }
            else if (Time.unscaledTime < status.MessageUntil && !string.IsNullOrEmpty(status.Message))
            {
                // Sombra para que el texto se lea sobre el césped.
                GUI.Label(new Rect(center.x + 2f, center.y + 2f, center.width, center.height), status.Message, bigShadow);
                GUI.Label(center, status.Message, big);
            }
        }
    }
}
