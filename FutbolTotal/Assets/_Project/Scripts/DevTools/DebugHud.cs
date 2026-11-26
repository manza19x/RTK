using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// HUD provisional de la Fase 1 con IMGUI (OnGUI): resistencia, carga de botones, posesión, marcador de
    /// entrenamiento y ayuda de controles. Se sustituye por la interfaz definitiva (uGUI) en la Fase 5.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private HumanTeamController human;
        [SerializeField] private TrainingHarness harness;
        [SerializeField] private bool showHelpAtStart = true;

        private bool showHelp;
        private GUIStyle label;
        private GUIStyle big;
        private GUIStyle box;
        private Texture2D white;
        private float smoothedFps;

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
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            label.normal.textColor = Color.white;
            big = new GUIStyle(label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            box = new GUIStyle(GUI.skin.box);
            box.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
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
            if (context == null || human == null) return;
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

            GUILayout.BeginArea(new Rect(12, 12, 300, 230), box);
            if (harness != null)
            {
                GUILayout.Label($"<b>{context.HomeTeam.Settings.shortName} {harness.HomeGoals} - {harness.AwayGoals} {context.AwayTeam.Settings.shortName}</b>   <size=11>(entrenamiento)</size>", label);
            }
            if (player != null)
            {
                string state = player.Motor.IsStunned ? " <color=#ff8080>(aturdido)</color>" : player.Motor.IsSprinting ? " <color=#ffd060>(sprint)</color>" : "";
                GUILayout.Label($"Controlas: <b>{player}</b>{state}", label);
                Bar("Resistencia", player.Stamina.Current01, player.Stamina.IsExhausted ? Color.red : new Color(0.3f, 0.9f, 0.4f));
            }
            Bar("Pase corto", input.ShortPass.Charge01, Color.cyan);
            Bar("Pase largo", input.LongPass.Charge01, new Color(0.4f, 0.6f, 1f));
            Bar("Tiro", input.Shot.Charge01, new Color(1f, 0.55f, 0.2f));

            string owner = ball.Owner != null ? ball.Owner.ToString() : "suelto";
            GUILayout.Label($"Balón: {owner} · {ball.Velocity.magnitude:0.0} m/s · altura {ball.Position.y:0.00} m", label);
            GUILayout.Label($"<size=11>{smoothedFps:0} FPS · F1 / R3: ayuda</size>", label);
            GUILayout.EndArea();
        }

        private void Bar(string title, float value, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, label, GUILayout.Width(90));
            Rect rect = GUILayoutUtility.GetRect(180, 12, GUILayout.ExpandWidth(false));
            rect.y += 5;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(rect, white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), white);
            GUI.color = previous;
            GUILayout.EndHorizontal();
        }

        private void DrawHelpPanel()
        {
            bool pad = human.Input.LastDeviceIsGamepad;
            float width = 330;
            GUILayout.BeginArea(new Rect(Screen.width - width - 12, 12, width, 250), box);
            GUILayout.Label(pad ? "<b>Controles (mando)</b>" : "<b>Controles (teclado)</b>", label);
            if (pad)
            {
                GUILayout.Label("Stick izq.: mover · RT: sprint", label);
                GUILayout.Label("A: pase corto (sin balón: entrada)", label);
                GUILayout.Label("X: pase largo / centro", label);
                GUILayout.Label("B: tiro (mantén para más potencia)", label);
                GUILayout.Label("LB: cambiar de jugador", label);
                GUILayout.Label("Start: pausa · Select: recolocar balón", label);
            }
            else
            {
                GUILayout.Label("WASD: mover · Shift izq.: sprint", label);
                GUILayout.Label("J: pase corto (sin balón: entrada)", label);
                GUILayout.Label("L: pase largo / centro", label);
                GUILayout.Label("K: tiro (mantén para más potencia)", label);
                GUILayout.Label("Q: cambiar de jugador", label);
                GUILayout.Label("Esc: pausa · R: recolocar balón", label);
            }
            GUILayout.Label("<size=11>Los botones de acción se cargan manteniendo y actúan al soltar.</size>", label);
            GUILayout.EndArea();
        }

        private void DrawCenterMessages()
        {
            if (harness == null) return;
            Rect center = new Rect(0, Screen.height * 0.3f, Screen.width, 60);
            if (harness.IsPaused)
            {
                GUI.Label(center, "PAUSA", big);
                GUI.Label(new Rect(0, center.yMax, Screen.width, 30), "Esc / Start para continuar", new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });
            }
            else if (Time.unscaledTime < harness.MessageUntil && !string.IsNullOrEmpty(harness.Message))
            {
                GUI.Label(center, harness.Message, big);
            }
        }
    }
}
