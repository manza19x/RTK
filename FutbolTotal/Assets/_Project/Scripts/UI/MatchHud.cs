using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Interfaz durante el partido:
    /// - arriba en el centro, marcador con nombres, resultado, reloj y parte, y quién tiene el balón;
    /// - abajo a la izquierda, el jugador controlado con su resistencia y la carga del botón que se mantiene;
    /// - una franja central para los mensajes (gol, saques, descanso...);
    /// - una pista discreta de controles abajo a la derecha y la pantalla de controles con F1.
    /// Funciona con cualquier regla que implemente <see cref="IMatchStatus"/> (partido o entrenamiento).
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        private MatchContext context;
        private IMatchStatus status;
        private MatchManager manager;
        private HumanTeamController human;

        private Text homeName, awayName, score, clock, possession, playerName, chargeLabel, banner, hint;
        private RectTransform staminaFill, chargeFill;
        private Image staminaImage, chargeImage;
        private GameObject chargeRoot, bannerRoot, controlsPanel;
        private CanvasGroup bannerGroup;
        private Button controlsClose;
        private ReplaySystem replay;

        private void Start()
        {
            context = MatchContext.Current;
            human = Object.FindFirstObjectByType<HumanTeamController>();
            if (context == null || human == null)
            {
                enabled = false;
                return;
            }
            status = context.Rules;
            manager = status as MatchManager;
            replay = Object.FindFirstObjectByType<ReplaySystem>();
            Build();
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD", 0);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;
            Team home = context.HomeTeam;
            Team away = context.AwayTeam;

            // Marcador.
            RectTransform board = UIFactory.Anchored("Scoreboard", root, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 70f));
            UIFactory.AddImage(board, UITheme.Panel);
            Chip(board, new Vector2(-298f, 0f), home.Settings.primaryColor);
            Chip(board, new Vector2(298f, 0f), away.Settings.primaryColor);
            homeName = UIFactory.AddText(UIFactory.Anchored("Home", board, new Vector2(0.5f, 0.5f), new Vector2(-185f, 0f), new Vector2(190f, 60f)),
                                         home.Settings.shortName, 38, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            awayName = UIFactory.AddText(UIFactory.Anchored("Away", board, new Vector2(0.5f, 0.5f), new Vector2(185f, 0f), new Vector2(190f, 60f)),
                                         away.Settings.shortName, 38, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            score = UIFactory.AddText(UIFactory.Anchored("Score", board, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 64f)),
                                      "0 - 0", 46, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            RectTransform clockBox = UIFactory.Anchored("Clock", root, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(300f, 40f));
            UIFactory.AddImage(clockBox, UITheme.PanelSoft);
            clock = UIFactory.Label("Text", clockBox, "", 26, UITheme.Text);
            possession = UIFactory.AddText(UIFactory.Anchored("Possession", root, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(400f, 30f)),
                                           "", 22, UITheme.TextDim);

            // Jugador controlado.
            RectTransform card = UIFactory.Anchored("PlayerCard", root, new Vector2(0f, 0f), new Vector2(230f, 85f), new Vector2(420f, 130f));
            UIFactory.AddImage(card, UITheme.PanelSoft);
            playerName = UIFactory.AddText(UIFactory.Anchored("Name", card, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(390f, 40f)),
                                           "", 26, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.AddText(UIFactory.Anchored("StaminaLabel", card, new Vector2(0.5f, 1f), new Vector2(-130f, -64f), new Vector2(130f, 30f)),
                              "Resistencia", 20, UITheme.TextDim, TextAnchor.MiddleLeft);
            staminaFill = UIFactory.Bar("Stamina", card, new Vector2(0.5f, 1f), new Vector2(65f, -64f), new Vector2(250f, 14f),
                                        new Color(0.3f, 0.9f, 0.4f), out staminaImage);

            RectTransform chargeHolder = UIFactory.Anchored("Charge", card, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(390f, 30f));
            chargeRoot = chargeHolder.gameObject;
            chargeLabel = UIFactory.AddText(UIFactory.Anchored("Label", chargeHolder, new Vector2(0.5f, 0.5f), new Vector2(-130f, 0f), new Vector2(130f, 30f)),
                                            "", 20, UITheme.TextDim, TextAnchor.MiddleLeft);
            chargeFill = UIFactory.Bar("Bar", chargeHolder, new Vector2(0.5f, 0.5f), new Vector2(65f, 0f), new Vector2(250f, 14f),
                                       UITheme.Accent, out chargeImage);

            // Mensajes centrales.
            RectTransform bannerRect = UIFactory.Rect("Banner", root, new Vector2(0f, 0.62f), new Vector2(1f, 0.62f), new Vector2(0.5f, 0.5f),
                                                      Vector2.zero, new Vector2(0f, 150f));
            UIFactory.AddImage(bannerRect, new Color(0f, 0f, 0f, 0.55f));
            bannerGroup = bannerRect.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.interactable = false;
            bannerGroup.blocksRaycasts = false;
            banner = UIFactory.Label("Text", bannerRect, "", 46, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerRoot = bannerRect.gameObject;

            hint = UIFactory.AddText(UIFactory.Anchored("Hint", root, new Vector2(1f, 0f), new Vector2(-190f, 30f), new Vector2(360f, 30f)),
                                     "", 20, UITheme.TextDim, TextAnchor.MiddleRight);

            controlsPanel = ControlsPanel.Build(root, () =>
            {
                controlsPanel.SetActive(false);
                UIFactory.ClearSelection();
            }, out controlsClose);
            controlsPanel.SetActive(false);
        }

        private static void Chip(Transform board, Vector2 position, Color color)
        {
            RectTransform chip = UIFactory.Anchored("Chip", board, new Vector2(0.5f, 0.5f), position, new Vector2(16f, 54f));
            UIFactory.AddImage(chip, color);
        }

        private void Update()
        {
            if (human.Input != null && human.Input.ToggleHelp.WasPressedThisFrame())
            {
                bool show = !controlsPanel.activeSelf;
                controlsPanel.SetActive(show);
                if (show) UIFactory.Select(controlsClose);
                else UIFactory.ClearSelection();
            }

            UpdateScoreboard();
            UpdatePlayerCard();
            UpdateBanner();
            hint.text = human.Input != null && human.Input.LastDeviceIsGamepad ? "R3: controles  ·  Start: pausa" : "F1: controles  ·  Esc: pausa";
        }

        private void UpdateScoreboard()
        {
            if (status != null)
            {
                score.text = $"{status.HomeGoals} - {status.AwayGoals}";
                clock.text = $"{status.ClockText}   <color=#B8BECC>{status.PhaseText}</color>";
            }

            FootballPlayer owner = context.Ball.Owner;
            if (owner != null)
            {
                string color = ColorUtility.ToHtmlStringRGB(owner.Team.Settings.primaryColor);
                possession.text = $"Balón: <color=#{color}><b>{owner.Team.Settings.teamName}</b></color>";
            }
            else
            {
                possession.text = "Balón suelto";
            }
        }

        private void UpdatePlayerCard()
        {
            FootballPlayer player = human.ActivePlayer;
            if (player == null) return;
            playerName.text = $"#{player.ShirtNumber}  {RoleName(player.Role)}" +
                              (player.Motor.IsStunned ? "  <color=#FF8080>aturdido</color>" : "");
            UIFactory.SetBar(staminaFill, player.Stamina.Current01);
            staminaImage.color = player.Stamina.IsExhausted ? new Color(0.95f, 0.3f, 0.3f) : new Color(0.3f, 0.9f, 0.4f);

            FootballInput input = human.Input;
            ChargeButton charging = input.Shot.IsCharging ? input.Shot
                                  : input.LongPass.IsCharging ? input.LongPass
                                  : input.ShortPass.IsCharging ? input.ShortPass : null;
            chargeRoot.SetActive(charging != null);
            if (charging == null) return;
            chargeLabel.text = charging == input.Shot ? "Tiro" : charging == input.LongPass ? "Pase largo" : "Pase";
            UIFactory.SetBar(chargeFill, charging.Charge01);
            chargeImage.color = charging == input.Shot && charging.Charge01 > 0.88f ? new Color(1f, 0.4f, 0.3f) : UITheme.Accent;
        }

        private void UpdateBanner()
        {
            if (replay != null && replay.IsReplaying)
            {
                bannerRoot.SetActive(true);
                bannerGroup.alpha = 0.85f;
                banner.text = "<color=#FF5050>●</color> REPETICIÓN\n<size=26>J / A para saltar</size>";
                return;
            }
            bool fullTime = manager != null && manager.State == MatchState.FullTime;
            bool active = status != null && !status.IsPaused && !fullTime &&
                          !string.IsNullOrEmpty(status.Message) && Time.unscaledTime < status.MessageUntil;
            bannerRoot.SetActive(active);
            if (!active) return;
            banner.text = status.Message;
            float remaining = status.MessageUntil - Time.unscaledTime;
            bannerGroup.alpha = float.IsPositiveInfinity(remaining) ? 1f : Mathf.Clamp01(remaining / 0.3f);
        }

        public static string RoleName(PlayerRole role)
        {
            switch (role)
            {
                case PlayerRole.Goalkeeper: return "Portero";
                case PlayerRole.Defender: return "Defensa";
                case PlayerRole.Midfielder: return "Centrocampista";
                default: return "Delantero";
            }
        }
    }
}
