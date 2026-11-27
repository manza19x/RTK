using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Menú de pausa: panel lateral con el marcador actual y los botones (reanudar, reiniciar, controles, menú).
    /// Aparece cuando las reglas están en pausa (Esc / Start) y desaparece al reanudar.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private IMatchStatus status;
        private IMatchControl control;
        private MatchContext context;
        private GameObject root, mainBox, controlsPanel;
        private RectTransform side;
        private Button resumeButton, controlsBack;
        private Text scoreLine;
        private bool visible;
        private float shownAt;

        private void Start()
        {
            context = MatchContext.Current;
            status = context != null ? context.Rules : null;
            control = status as IMatchControl;
            if (status == null || control == null)
            {
                enabled = false;
                return;
            }
            Build();
            root.SetActive(false);
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("PauseMenu", 20);
            canvas.transform.SetParent(transform, false);
            RectTransform rootRect = UIFactory.Stretch("Root", canvas.transform);
            root = rootRect.gameObject;
            UIFactory.AddImage(rootRect, new Color(0f, 0f, 0f, 0.35f), raycast: true);

            side = UIFactory.Rect("Side", rootRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(680f, 0f));
            UIFactory.AddSprite(side, UISprites.GradientLeft, new Color(0.01f, 0.02f, 0.05f, 0.97f), sliced: false);
            mainBox = side.gameObject;

            UIFactory.AddText(UIFactory.Rect("Title", side, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(110f, -110f), new Vector2(-110f, 110f)),
                              "PAUSA", 96, UITheme.Text, TextAnchor.UpperLeft, FontStyle.BoldAndItalic);
            RectTransform accent = UIFactory.Rect("Accent", side, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -222f), new Vector2(180f, 7f));
            UIFactory.AddSprite(accent, UISprites.Pill, UITheme.Accent);
            scoreLine = UIFactory.AddText(UIFactory.Rect("Score", side, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(112f, -246f), new Vector2(-112f, 50f)),
                                          "", 32, UITheme.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);

            RectTransform list = UIFactory.Rect("Buttons", side, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -340f), new Vector2(480f, 380f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            resumeButton = SideButton(list, "Reanudar", Resume, true);
            SideButton(list, "Reiniciar partido", Restart);
            SideButton(list, "Controles", OpenControls);
            SideButton(list, "Menú principal", () => SceneLoader.Load(SceneNames.MainMenu));

            controlsPanel = ControlsPanel.Build(rootRect, CloseControls, out controlsBack);
            controlsPanel.SetActive(false);
        }

        private static Button SideButton(Transform parent, string text, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            Button button = UIFactory.Button(parent, text, action, 470f, primary ? 76f : 64f, primary ? 34 : 28);
            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)label.transform).offsetMin = new Vector2(32f, 0f);
            return button;
        }

        private void Update()
        {
            bool paused = status.IsPaused;
            if (paused && !visible) Show();
            else if (!paused && visible) Hide();

            if (visible)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.25f);
                float eased = 1f - (1f - t) * (1f - t);
                side.anchoredPosition = new Vector2(Mathf.Lerp(-120f, 0f, eased), 0f);
            }
        }

        private void Show()
        {
            visible = true;
            shownAt = Time.unscaledTime;
            root.SetActive(true);
            mainBox.SetActive(true);
            controlsPanel.SetActive(false);
            scoreLine.text = $"{context.HomeTeam.Settings.shortName}  {status.HomeGoals} - {status.AwayGoals}  {context.AwayTeam.Settings.shortName}" +
                             $"     {status.ClockText}  ·  {status.PhaseText.ToUpperInvariant()}";
            UIFactory.Select(resumeButton);
        }

        private void Hide()
        {
            visible = false;
            root.SetActive(false);
            UIFactory.ClearSelection();
        }

        private void Resume() => control.SetPaused(false);

        private void Restart()
        {
            control.SetPaused(false);
            control.RestartMatch();
        }

        private void OpenControls()
        {
            mainBox.SetActive(false);
            controlsPanel.SetActive(true);
            UIFactory.Select(controlsBack);
        }

        private void CloseControls()
        {
            controlsPanel.SetActive(false);
            mainBox.SetActive(true);
            UIFactory.Select(resumeButton);
        }
    }
}
