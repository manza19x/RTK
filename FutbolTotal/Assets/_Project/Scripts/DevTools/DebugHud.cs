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
        [SerializeField] private TrainingHarness harness;
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
            "<size=11>Las acciones se cargan manteniendo y actúan al soltar.</size>"
        };

        private bool showHelp;
        private float smoothedFps;

        private GUIStyle label;
        private GUIStyle centered;
        private GUIStyle big;
        private Texture2D white;
        private Texture2D panelBackground;

        private void Start()
        {
            showHelp = showHelpAtStart;
        }

        private void Update()
        {
            if (human != null && human.Input != null && human.Input.ToggleHelp.WasPressedThisFrame()) showHelp = !showHelp;
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
            big = new GUIStyle(centered) { fontSize = 42, fontStyle = FontStyle.Bold };
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

            if (harness != null)
            {
                Line(x, ref y, width, $"<b>{context.HomeTeam.Settings.shortName} {harness.HomeGoals} - {harness.AwayGoals} {context.AwayTeam.Settings.shortName}</b>   <size=11>(entrenamiento)</size>");
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

        private void DrawCenterMessages()
        {
            if (harness == null) return;
            Rect center = new Rect(0f, Screen.height * 0.3f, Screen.width, 60f);
            if (harness.IsPaused)
            {
                GUI.Label(center, "PAUSA", big);
                GUI.Label(new Rect(0f, center.yMax, Screen.width, 30f), "Esc / Start para continuar", centered);
            }
            else if (Time.unscaledTime < harness.MessageUntil && !string.IsNullOrEmpty(harness.Message))
            {
                GUI.Label(center, harness.Message, big);
            }
        }
    }
}
