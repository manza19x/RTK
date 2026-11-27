using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Menú principal: jugar, controles, opciones (duración, dificultad y asistencia de pase) y salir.
    /// Las opciones se guardan con <see cref="GameOptions"/> y se aplican al empezar el partido.
    /// Cada opción es un botón que cambia de valor al pulsarlo: cómodo con ratón, teclado y mando.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject mainPanel, optionsPanel, controlsPanel;
        private Button playButton, lengthButton, controlsBack;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.volume = GameOptions.Volume;
            Build();
            ShowMain();
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("MainMenu", 0);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            RectTransform shade = UIFactory.Stretch("Shade", root);
            UIFactory.AddImage(shade, new Color(0f, 0f, 0f, 0.35f));

            UIFactory.AddText(UIFactory.Anchored("Title", root, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1400f, 150f)),
                              "FÚTBOL TOTAL", 120, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.AddText(UIFactory.Anchored("Subtitle", root, new Vector2(0.5f, 1f), new Vector2(0f, -265f), new Vector2(1400f, 50f)),
                              "Club Aurora  vs  Deportivo Marea  ·  fútbol 7", 34, UITheme.Text);
            UIFactory.AddText(UIFactory.Anchored("Footer", root, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1600f, 40f)),
                              "Prototipo · ratón, teclado (flechas + Enter) o mando (stick + A)", 22, UITheme.TextDim);

            // Panel principal.
            RectTransform main = UIFactory.VerticalList("Main", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(480f, 380f), 18f);
            mainPanel = main.gameObject;
            playButton = UIFactory.Button(main, "Jugar", () => SceneLoader.Load(SceneNames.Match), 460f, 72f, 36);
            UIFactory.Button(main, "Controles", ShowControls);
            UIFactory.Button(main, "Opciones", ShowOptions);
            UIFactory.Button(main, "Salir", SceneLoader.Quit);

            // Opciones.
            RectTransform options = UIFactory.Anchored("Options", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(900f, 580f));
            UIFactory.AddImage(options, UITheme.Panel);
            optionsPanel = options.gameObject;
            UIFactory.AddText(UIFactory.Anchored("Title", options, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(800f, 60f)),
                              "OPCIONES", 44, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform list = UIFactory.VerticalList("List", options, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(820f, 440f), 16f);
            lengthButton = UIFactory.Button(list, "", null, 800f, 64f, 28);
            lengthButton.onClick.AddListener(() =>
            {
                GameOptions.MatchLength = Next(GameOptions.MatchLength);
                RefreshOptions();
            });
            Button difficultyButton = UIFactory.Button(list, "", null, 800f, 64f, 28);
            difficultyButton.onClick.AddListener(() =>
            {
                GameOptions.Difficulty = Next(GameOptions.Difficulty);
                RefreshOptions();
            });
            Button assistButton = UIFactory.Button(list, "", null, 800f, 64f, 28);
            assistButton.onClick.AddListener(() =>
            {
                GameOptions.PassAssist = Next(GameOptions.PassAssist);
                RefreshOptions();
            });
            Button volumeButton = UIFactory.Button(list, "", null, 800f, 64f, 28);
            volumeButton.onClick.AddListener(() =>
            {
                GameOptions.Volume = GameOptions.NextVolume(GameOptions.Volume);
                RefreshOptions();
            });
            UIFactory.Button(list, "Volver", ShowMain, 360f, 64f);
            optionButtons = new[] { lengthButton, difficultyButton, assistButton, volumeButton };

            controlsPanel = ControlsPanel.Build(root, ShowMain, out controlsBack);
            RefreshOptions();
        }

        private Button[] optionButtons;

        private void RefreshOptions()
        {
            MatchSettings match = SettingsProvider.Load<MatchSettings>(SettingsProvider.MatchName);
            float half = (match != null ? match.halfDurationSeconds : 150f) * GameOptions.MatchLengthMultiplier;
            int minutes = Mathf.FloorToInt(half / 60f);
            int seconds = Mathf.RoundToInt(half % 60f);

            UIFactory.SetButtonText(optionButtons[0], $"Duración: {GameOptions.Describe(GameOptions.MatchLength)}  ({minutes}:{seconds:00} por parte)");
            UIFactory.SetButtonText(optionButtons[1], $"Dificultad: {GameOptions.Describe(GameOptions.Difficulty)}");
            UIFactory.SetButtonText(optionButtons[2], $"Asistencia de pase: {GameOptions.Describe(GameOptions.PassAssist)}");
            UIFactory.SetButtonText(optionButtons[3], $"Volumen: {Mathf.RoundToInt(GameOptions.Volume * 100f)} %");
        }

        private static T Next<T>(T value) where T : System.Enum
        {
            T[] values = (T[])System.Enum.GetValues(typeof(T));
            int index = System.Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
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
