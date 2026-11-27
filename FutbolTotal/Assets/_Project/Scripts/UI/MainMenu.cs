using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Menú principal con estilo de juego deportivo: logotipo arriba a la izquierda, columna de botones grandes,
    /// tarjeta del partido (escudos, «VS» y ajustes elegidos) a la derecha y barra de ayuda abajo.
    /// Opciones: duración, dificultad, asistencia de pase y volumen (se guardan con <see cref="GameOptions"/>).
    /// Esc / B vuelve atrás desde Opciones o Controles.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject mainPanel, optionsPanel, controlsPanel;
        private Button playButton, lengthButton, controlsBack;
        private Button[] optionButtons;
        private Text matchInfo;
        private RectTransform logo;
        private float openedAt;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.volume = GameOptions.Volume;
            Build();
            ShowMain();
            openedAt = Time.unscaledTime;
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("MainMenu", 0);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            // Oscurecido de izquierda a derecha para que el texto se lea sobre el estadio.
            RectTransform shade = UIFactory.Rect("Shade", root, Vector2.zero, new Vector2(0.75f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.AddSprite(shade, UISprites.GradientLeft, new Color(0.01f, 0.02f, 0.05f, 0.92f), sliced: false);
            RectTransform bottomShade = UIFactory.Rect("BottomShade", root, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 260f));
            UIFactory.AddSprite(bottomShade, UISprites.GradientUp, new Color(0.01f, 0.02f, 0.05f, 0.85f), sliced: false);

            // Logotipo.
            logo = UIFactory.Rect("Logo", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -90f), new Vector2(900f, 190f));
            Text title = UIFactory.AddText(UIFactory.Rect("Title", logo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 120f)),
                                           "FÚTBOL <color=#0EE0AD>TOTAL</color>", 118, UITheme.Text, TextAnchor.UpperLeft, FontStyle.BoldAndItalic);
            UIFactory.AddTextShadow(title, 4f, 0.5f);
            RectTransform underline = UIFactory.Rect("Underline", logo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -128f), new Vector2(260f, 8f));
            UIFactory.AddSprite(underline, UISprites.Pill, UITheme.Accent);
            UIFactory.AddText(UIFactory.Rect("Tagline", logo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(6f, -146f), new Vector2(0f, 40f)),
                              "FÚTBOL 7  ·  PROTOTIPO", 26, UITheme.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);

            // Botones principales.
            RectTransform main = UIFactory.Rect("Main", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -390f), new Vector2(560f, 420f));
            var layout = main.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            mainPanel = main.gameObject;
            playButton = MenuButton(main, "Jugar partido", () => SceneLoader.Load(SceneNames.Match), true);
            MenuButton(main, "Entrenamiento", () => SceneLoader.Load(SceneNames.Training));
            MenuButton(main, "Controles", ShowControls);
            MenuButton(main, "Opciones", ShowOptions);
            MenuButton(main, "Salir", SceneLoader.Quit);

            BuildMatchCard(root);
            BuildOptions(root);

            // Barra de ayuda.
            RectTransform help = UIFactory.Rect("Help", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(-240f, 44f));
            UIFactory.AddText(help, "↑ ↓  NAVEGAR        INTRO / A  ELEGIR        ESC / B  VOLVER", 22, UITheme.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);

            controlsPanel = ControlsPanel.Build(root, ShowMain, out controlsBack);
            RefreshOptions();
        }

        private static Button MenuButton(Transform parent, string text, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            Button button = UIFactory.Button(parent, text, action, 540f, primary ? 84f : 68f, primary ? 38 : 30);
            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            var rect = (RectTransform)label.transform;
            rect.offsetMin = new Vector2(34f, 0f);
            return button;
        }

        private void BuildMatchCard(Transform root)
        {
            TeamSettings homeSettings = SettingsProvider.Load<TeamSettings>(SettingsProvider.HomeTeamName);
            TeamSettings awaySettings = SettingsProvider.Load<TeamSettings>(SettingsProvider.AwayTeamName);
            Color homeA = homeSettings != null ? homeSettings.primaryColor : new Color(0.86f, 0.24f, 0.18f);
            Color homeB = homeSettings != null ? homeSettings.secondaryColor : Color.white;
            Color awayA = awaySettings != null ? awaySettings.primaryColor : new Color(0.16f, 0.36f, 0.82f);
            Color awayB = awaySettings != null ? awaySettings.secondaryColor : new Color(0.1f, 0.12f, 0.2f);
            string homeName = homeSettings != null ? homeSettings.teamName : "Club Aurora";
            string awayName = awaySettings != null ? awaySettings.teamName : "Deportivo Marea";

            RectTransform card = UIFactory.Rect("MatchCard", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                                new Vector2(-120f, -20f), new Vector2(720f, 420f));
            UIFactory.AddSprite(card, UISprites.Rounded, UITheme.Panel);
            RectTransform header = UIFactory.Rect("Header", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 58f));
            UIFactory.AddSprite(header, UISprites.Rounded, UITheme.Accent);
            UIFactory.Label("Text", header, "PARTIDO RÁPIDO", 26, UITheme.TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

            CrestColumn(card, -200f, homeA, homeB, homeName, "LOCAL · TÚ");
            CrestColumn(card, 200f, awayA, awayB, awayName, "VISITANTE · IA");
            UIFactory.AddText(UIFactory.Anchored("VS", card, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(160f, 100f)),
                              "VS", 72, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);

            RectTransform info = UIFactory.Rect("Info", card, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-40f, 56f));
            UIFactory.AddSprite(info, UISprites.Pill, UITheme.PanelLight);
            matchInfo = UIFactory.Label("Text", info, "", 22, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static void CrestColumn(RectTransform card, float x, Color primary, Color secondary, string name, string role)
        {
            RectTransform crest = UIFactory.Anchored("Crest", card, new Vector2(0.5f, 0.5f), new Vector2(x, 50f), new Vector2(150f, 150f));
            UIFactory.AddSprite(crest, UISprites.Crest(primary, secondary), Color.white, sliced: false);
            UIFactory.AddText(UIFactory.Anchored("Name", card, new Vector2(0.5f, 0.5f), new Vector2(x, -55f), new Vector2(300f, 44f)),
                              name.ToUpperInvariant(), 30, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.AddText(UIFactory.Anchored("Role", card, new Vector2(0.5f, 0.5f), new Vector2(x, -92f), new Vector2(300f, 30f)),
                              role, 20, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void BuildOptions(Transform root)
        {
            RectTransform options = UIFactory.Rect("Options", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                   new Vector2(120f, -390f), new Vector2(760f, 480f));
            optionsPanel = options.gameObject;
            UIFactory.AddText(UIFactory.Rect("Title", options, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 50f)),
                              "OPCIONES", 36, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform list = UIFactory.Rect("List", options, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -64f), new Vector2(760f, 420f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            lengthButton = OptionButton(list, () => GameOptions.MatchLength = Next(GameOptions.MatchLength));
            Button difficulty = OptionButton(list, () => GameOptions.Difficulty = Next(GameOptions.Difficulty));
            Button assist = OptionButton(list, () => GameOptions.PassAssist = Next(GameOptions.PassAssist));
            Button volume = OptionButton(list, () => GameOptions.Volume = GameOptions.NextVolume(GameOptions.Volume));
            MenuButton(list, "Volver", ShowMain);
            optionButtons = new[] { lengthButton, difficulty, assist, volume };
        }

        private Button OptionButton(Transform parent, System.Action change)
        {
            Button button = MenuButton(parent, "", null);
            ((RectTransform)button.transform).sizeDelta = new Vector2(740f, 66f);
            button.onClick.AddListener(() =>
            {
                change();
                RefreshOptions();
            });
            return button;
        }

        private void RefreshOptions()
        {
            MatchSettings match = SettingsProvider.Load<MatchSettings>(SettingsProvider.MatchName);
            float half = (match != null ? match.halfDurationSeconds : 150f) * GameOptions.MatchLengthMultiplier;
            string halfText = $"{Mathf.FloorToInt(half / 60f)}:{Mathf.RoundToInt(half % 60f):00}";

            UIFactory.SetButtonText(optionButtons[0], $"Duración        <  {GameOptions.Describe(GameOptions.MatchLength)}  >     {halfText} por parte");
            UIFactory.SetButtonText(optionButtons[1], $"Dificultad       <  {GameOptions.Describe(GameOptions.Difficulty)}  >");
            UIFactory.SetButtonText(optionButtons[2], $"Ayuda al pasar  <  {GameOptions.Describe(GameOptions.PassAssist)}  >");
            UIFactory.SetButtonText(optionButtons[3], $"Volumen          <  {Mathf.RoundToInt(GameOptions.Volume * 100f)} %  >");
            if (matchInfo != null)
            {
                matchInfo.text = $"ESTADIO AURORA  ·  {halfText} POR PARTE  ·  DIFICULTAD {GameOptions.Describe(GameOptions.Difficulty).ToUpperInvariant()}";
            }
        }

        private static T Next<T>(T value) where T : System.Enum
        {
            T[] values = (T[])System.Enum.GetValues(typeof(T));
            int index = System.Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
        }

        private void Update()
        {
            // Entrada del logotipo.
            float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / 0.6f);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            logo.anchoredPosition = new Vector2(Mathf.Lerp(40f, 120f, eased), -90f);

            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back && !mainPanel.activeSelf) ShowMain();
        }

        private void ShowMain()
        {
            mainPanel.SetActive(true);
            optionsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            UIFactory.Select(playButton);
        }

        private void ShowOptions()
        {
            mainPanel.SetActive(false);
            optionsPanel.SetActive(true);
            UIFactory.Select(lengthButton);
        }

        private void ShowControls()
        {
            mainPanel.SetActive(false);
            controlsPanel.SetActive(true);
            UIFactory.Select(controlsBack);
        }
    }
}
