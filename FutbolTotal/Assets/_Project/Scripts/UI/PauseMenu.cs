using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Menú de pausa. Aparece cuando las reglas están en pausa (Esc / Start) y desaparece al reanudar.
    /// Opciones: reanudar, reiniciar el partido, ver controles y volver al menú principal.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private IMatchStatus status;
        private IMatchControl control;
        private GameObject root, mainBox, controlsPanel;
        private Button resumeButton, controlsBack;
        private bool visible;

        private void Start()
        {
            var context = MatchContext.Current;
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
            UIFactory.AddImage(rootRect, UITheme.Backdrop, raycast: true);

            RectTransform box = UIFactory.Anchored("Box", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
            UIFactory.AddImage(box, UITheme.Panel);
            mainBox = box.gameObject;
            UIFactory.AddText(UIFactory.Anchored("Title", box, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(560f, 80f)),
                              "PAUSA", 56, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            RectTransform list = UIFactory.VerticalList("Buttons", box, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(460f, 330f), 16f);
            resumeButton = UIFactory.Button(list, "Reanudar", Resume);
            UIFactory.Button(list, "Reiniciar partido", Restart);
            UIFactory.Button(list, "Controles", OpenControls);
            UIFactory.Button(list, "Menú principal", () => SceneLoader.Load(SceneNames.MainMenu));

            controlsPanel = ControlsPanel.Build(rootRect, CloseControls, out controlsBack);
            controlsPanel.SetActive(false);
        }

        private void Update()
        {
            bool paused = status.IsPaused;
            if (paused && !visible) Show();
            else if (!paused && visible) Hide();
        }

        private void Show()
        {
            visible = true;
            root.SetActive(true);
            mainBox.SetActive(true);
            controlsPanel.SetActive(false);
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
