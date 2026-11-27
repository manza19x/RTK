using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Interfaz del partido con estilo de retransmisión:
    /// - marcador arriba a la izquierda (escudos, abreviaturas, resultado que "salta" al marcar, reloj, parte y
    ///   una barra bajo el equipo que tiene el balón);
    /// - avisos breves arriba (saques, faltas), tarjeta central (previa, descanso, final) y celebración de gol;
    /// - ficha del jugador controlado (dorsal, puesto, resistencia y carga) abajo a la izquierda;
    /// - radar con todos los jugadores y el balón abajo en el centro;
    /// - etiqueta con el dorsal sobre el jugador controlado y pista de controles.
    /// Funciona con cualquier regla que implemente <see cref="IMatchStatus"/> (partido o entrenamiento).
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        private const float RadarWidth = 330f;
        private const float RadarHeight = 212f;

        private MatchContext context;
        private IMatchStatus status;
        private MatchManager manager;
        private HumanTeamController human;
        private ReplaySystem replay;
        private RectTransform canvasRect;

        // Marcador
        private Text score, clock, phase;
        private Image homePossession, awayPossession, liveDot;
        private RectTransform scoreRect;
        private int lastHome = -1, lastAway = -1;
        private float scorePopAt = -10f;

        // Mensajes
        private GameObject toastRoot, bannerRoot, goalRoot;
        private CanvasGroup toastGroup, bannerGroup, goalGroup;
        private Text toastText, bannerTitle, bannerSubtitle, goalTitle, goalSubtitle;
        private Image goalBand;
        private RectTransform goalBandRect, goalTitleRect, toastRect;
        private string shownMessage;
        private float shownAt;
        private Team goalTeam;
        private string goalDetail;

        // Jugador
        private Text cardNumber, cardRole, chargeLabel;
        private Image cardStripe, staminaImage, chargeImage;
        private RectTransform staminaFill, chargeFill;
        private GameObject chargeRoot;
        private RectTransform tagRect;
        private Text tagText;
        private Image tagBackground, tagArrow;

        // Radar
        private RectTransform radarField;
        private readonly Dictionary<FootballPlayer, RectTransform> radarDots = new Dictionary<FootballPlayer, RectTransform>();
        private RectTransform radarBall, radarActive;

        private Text hint;
        private GameObject controlsPanel;
        private Button controlsClose;

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
            if (manager != null) manager.GoalScored += OnGoal;
            Build();
        }

        private void OnDestroy()
        {
            if (manager != null) manager.GoalScored -= OnGoal;
        }

        private void OnGoal(Team scorer)
        {
            goalTeam = scorer;
            FootballPlayer toucher = context.Ball.LastToucher;
            string who = toucher == null ? "" : toucher.Team != scorer ? "  ·  en propia puerta" : $"  ·  dorsal {toucher.ShirtNumber}";
            goalDetail = $"{scorer.Settings.teamName.ToUpperInvariant()}{who}  ·  {manager.CurrentMinute}'";
        }

        // ================================================================== Construcción

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD", 0);
            canvas.transform.SetParent(transform, false);
            canvasRect = (RectTransform)canvas.transform;
            Transform root = canvas.transform;

            BuildScoreboard(root);
            BuildMessages(root);
            BuildPlayerCard(root);
            BuildRadar(root);
            BuildPlayerTag(root);

            RectTransform hintRect = UIFactory.Anchored("Hint", root, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(380f, 44f));
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-40f, 40f);
            UIFactory.AddSprite(hintRect, UISprites.Pill, UITheme.PanelSoft);
            hint = UIFactory.Label("Text", hintRect, "", 20, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);

            controlsPanel = ControlsPanel.Build(root, () =>
            {
                controlsPanel.SetActive(false);
                UIFactory.ClearSelection();
            }, out controlsClose);
            controlsPanel.SetActive(false);
        }

        private void BuildScoreboard(Transform root)
        {
            Team home = context.HomeTeam;
            Team away = context.AwayTeam;
            RectTransform board = UIFactory.Rect("Scoreboard", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                 new Vector2(40f, -36f), new Vector2(640f, 62f));
            UIFactory.AddSprite(board, UISprites.Rounded, UITheme.Panel);

            // Equipo local | marcador | equipo visitante | reloj
            TeamBlock(board, home, 0f, out homePossession);
            RectTransform scoreBox = UIFactory.Rect("ScoreBox", board, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                                                    new Vector2(200f, 0f), new Vector2(120f, -10f));
            UIFactory.AddSprite(scoreBox, UISprites.RoundedSmall, UITheme.PanelLight);
            score = UIFactory.Label("Score", scoreBox, "0 - 0", 38, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreRect = (RectTransform)score.transform;
            TeamBlock(board, away, 320f, out awayPossession);

            RectTransform clockBox = UIFactory.Rect("Clock", board, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                                                    new Vector2(-6f, 0f), new Vector2(112f, -12f));
            UIFactory.AddSprite(clockBox, UISprites.RoundedSmall, UITheme.Accent);
            clock = UIFactory.Label("Text", clockBox, "00:00", 32, UITheme.TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Parte con punto de "en directo".
            RectTransform phaseTag = UIFactory.Rect("Phase", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                    new Vector2(40f, -104f), new Vector2(190f, 34f));
            UIFactory.AddSprite(phaseTag, UISprites.Pill, UITheme.PanelSoft);
            RectTransform dot = UIFactory.Anchored("Live", phaseTag, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(12f, 12f));
            liveDot = UIFactory.AddSprite(dot, UISprites.Circle, UITheme.Live, sliced: false);
            phase = UIFactory.AddText(UIFactory.Rect("Text", phaseTag, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), Vector2.zero),
                                      "", 20, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Escudo, abreviatura y barra de posesión de un equipo dentro del marcador.</summary>
        private static void TeamBlock(RectTransform board, Team team, float x, out Image possession)
        {
            RectTransform block = UIFactory.Rect("Team_" + team.Settings.shortName, board, new Vector2(0f, 0f), new Vector2(0f, 1f),
                                                 new Vector2(0f, 0.5f), new Vector2(x + 8f, 0f), new Vector2(190f, 0f));
            bool crestFirst = x < 1f;
            RectTransform crest = UIFactory.Anchored("Crest", block, new Vector2(crestFirst ? 0f : 1f, 0.5f),
                                                     new Vector2(crestFirst ? 28f : -28f, 0f), new Vector2(42f, 42f));
            UIFactory.AddSprite(crest, UISprites.Crest(team.Settings.primaryColor, team.Settings.secondaryColor), Color.white, sliced: false);

            RectTransform nameRect = UIFactory.Rect("Name", block, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                                    new Vector2(crestFirst ? 30f : -30f, 2f), new Vector2(-70f, 0f));
            UIFactory.AddText(nameRect, team.Settings.shortName, 34, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);

            RectTransform bar = UIFactory.Rect("Possession", block, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                               new Vector2(crestFirst ? 30f : -30f, 6f), new Vector2(70f, 5f));
            possession = UIFactory.AddSprite(bar, UISprites.Pill, team.Settings.primaryColor);
        }

        private void BuildMessages(Transform root)
        {
            // Aviso breve.
            toastRect = UIFactory.Anchored("Toast", root, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(760f, 56f));
            UIFactory.AddSprite(toastRect, UISprites.Pill, UITheme.Panel);
            RectTransform toastAccent = UIFactory.Rect("Accent", toastRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                                                       new Vector2(14f, 0f), new Vector2(8f, -22f));
            UIFactory.AddSprite(toastAccent, UISprites.Pill, UITheme.Accent);
            toastText = UIFactory.Label("Text", toastRect, "", 26, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            toastGroup = toastRect.gameObject.AddComponent<CanvasGroup>();
            toastRoot = toastRect.gameObject;

            // Tarjeta central.
            RectTransform banner = UIFactory.Anchored("Banner", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(980f, 230f));
            UIFactory.AddSprite(banner, UISprites.Rounded, UITheme.Panel);
            RectTransform bannerLine = UIFactory.Rect("Accent", banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                                      new Vector2(0f, -16f), new Vector2(180f, 6f));
            UIFactory.AddSprite(bannerLine, UISprites.Pill, UITheme.Accent);
            bannerTitle = UIFactory.AddText(UIFactory.Anchored("Title", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(940f, 90f)),
                                            "", 66, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerSubtitle = UIFactory.AddText(UIFactory.Anchored("Subtitle", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(940f, 60f)),
                                               "", 30, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerRoot = banner.gameObject;

            // Gol: banda del color del equipo a todo lo ancho.
            RectTransform goal = UIFactory.Rect("Goal", root, new Vector2(0f, 0.62f), new Vector2(1f, 0.62f), new Vector2(0.5f, 0.5f),
                                                Vector2.zero, new Vector2(0f, 270f));
            goalBandRect = UIFactory.Stretch("Band", goal);
            goalBand = UIFactory.AddImage(goalBandRect, Color.white);
            RectTransform shade = UIFactory.Stretch("Shade", goal);
            UIFactory.AddSprite(shade, UISprites.GradientUp, new Color(0f, 0f, 0f, 0.45f), sliced: false);
            RectTransform topLine = UIFactory.Rect("TopLine", goal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f));
            UIFactory.AddImage(topLine, Color.white);
            RectTransform bottomLine = UIFactory.Rect("BottomLine", goal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 6f));
            UIFactory.AddImage(bottomLine, Color.white);
            goalTitleRect = UIFactory.Anchored("Title", goal, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1400f, 190f));
            goalTitle = UIFactory.AddText(goalTitleRect, "¡GOL!", 170, Color.white, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIFactory.AddTextShadow(goalTitle, 5f, 0.5f);
            goalSubtitle = UIFactory.AddText(UIFactory.Anchored("Subtitle", goal, new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(1400f, 60f)),
                                             "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.AddTextShadow(goalSubtitle);
            goalGroup = goal.gameObject.AddComponent<CanvasGroup>();
            goalRoot = goal.gameObject;

            foreach (CanvasGroup group in new[] { toastGroup, bannerGroup, goalGroup })
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            toastRoot.SetActive(false);
            bannerRoot.SetActive(false);
            goalRoot.SetActive(false);
        }

        private void BuildPlayerCard(Transform root)
        {
            RectTransform card = UIFactory.Rect("PlayerCard", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(460f, 118f));
            UIFactory.AddSprite(card, UISprites.Rounded, UITheme.Panel);
            RectTransform stripe = UIFactory.Rect("Stripe", card, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                                                  new Vector2(10f, 0f), new Vector2(8f, -24f));
            cardStripe = UIFactory.AddSprite(stripe, UISprites.Pill, Color.white);

            cardNumber = UIFactory.AddText(UIFactory.Rect("Number", card, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                                                          new Vector2(26f, 0f), new Vector2(96f, 0f)),
                                           "", 72, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            cardRole = UIFactory.AddText(UIFactory.Rect("Role", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                                                        new Vector2(130f, -12f), new Vector2(-150f, 34f)),
                                         "", 24, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);

            UIFactory.AddText(UIFactory.Rect("StaminaLabel", card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                             new Vector2(130f, -50f), new Vector2(120f, 24f)),
                              "RESISTENCIA", 16, UITheme.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            staminaFill = UIFactory.Bar("Stamina", card, new Vector2(0f, 1f), new Vector2(340f, -62f), new Vector2(190f, 10f),
                                        UITheme.Accent, out staminaImage);

            RectTransform charge = UIFactory.Rect("Charge", card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                  new Vector2(130f, -80f), new Vector2(310f, 26f));
            chargeRoot = charge.gameObject;
            chargeLabel = UIFactory.AddText(UIFactory.Rect("Label", charge, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                                                           Vector2.zero, new Vector2(120f, 0f)),
                                            "", 16, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            chargeFill = UIFactory.Bar("Bar", charge, new Vector2(0f, 0.5f), new Vector2(210f, 0f), new Vector2(190f, 10f), UITheme.Gold, out chargeImage);
        }

        private void BuildRadar(Transform root)
        {
            RectTransform radar = UIFactory.Anchored("Radar", root, new Vector2(0.5f, 0f), new Vector2(0f, 30f + RadarHeight * 0.5f),
                                                     new Vector2(RadarWidth + 24f, RadarHeight + 24f));
            UIFactory.AddSprite(radar, UISprites.Rounded, new Color(0.02f, 0.08f, 0.05f, 0.72f));
            radarField = UIFactory.Anchored("Field", radar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(RadarWidth, RadarHeight));

            Pitch pitch = context.Pitch;
            Color line = new Color(1f, 1f, 1f, 0.35f);
            const float t = 2f;
            float w = RadarWidth, h = RadarHeight;
            RadarLine(new Vector2(0f, h * 0.5f), new Vector2(w, t), line);
            RadarLine(new Vector2(0f, -h * 0.5f), new Vector2(w, t), line);
            RadarLine(new Vector2(-w * 0.5f, 0f), new Vector2(t, h), line);
            RadarLine(new Vector2(w * 0.5f, 0f), new Vector2(t, h), line);
            RadarLine(Vector2.zero, new Vector2(t, h), line);
            RectTransform circle = UIFactory.Anchored("Circle", radarField, new Vector2(0.5f, 0.5f), Vector2.zero,
                                                      Vector2.one * (pitch.CenterCircleRadius * 2f / pitch.Length * w));
            UIFactory.AddSprite(circle, UISprites.Ring, line, sliced: false);
            for (int side = -1; side <= 1; side += 2)
            {
                float depth = pitch.PenaltyAreaDepth / pitch.Length * w;
                float boxH = pitch.PenaltyAreaWidth / pitch.Width * h;
                float x = side * (w * 0.5f - depth);
                RadarLine(new Vector2(x, 0f), new Vector2(t, boxH), line);
                RadarLine(new Vector2(side * (w * 0.5f - depth * 0.5f), boxH * 0.5f), new Vector2(depth, t), line);
                RadarLine(new Vector2(side * (w * 0.5f - depth * 0.5f), -boxH * 0.5f), new Vector2(depth, t), line);
                RadarLine(new Vector2(side * (w * 0.5f + 3f), 0f), new Vector2(4f, pitch.GoalWidth / pitch.Width * h), Color.white);
            }

            foreach (FootballPlayer player in context.AllPlayers)
            {
                TeamSettings team = player.Team.Settings;
                Color color = player.IsGoalkeeper ? team.goalkeeperColor : team.primaryColor;
                RectTransform outline = UIFactory.Anchored("Dot_" + player.name, radarField, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13f, 13f));
                UIFactory.AddSprite(outline, UISprites.Circle, new Color(0f, 0f, 0f, 0.7f), sliced: false);
                RectTransform fill = UIFactory.Anchored("Fill", outline, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
                UIFactory.AddSprite(fill, UISprites.Circle, color, sliced: false);
                radarDots[player] = outline;
            }

            radarActive = UIFactory.Anchored("Active", radarField, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            UIFactory.AddSprite(radarActive, UISprites.Ring, Color.white, sliced: false);
            radarBall = UIFactory.Anchored("Ball", radarField, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, 9f));
            UIFactory.AddSprite(radarBall, UISprites.Circle, Color.white, sliced: false);
        }

        private void RadarLine(Vector2 center, Vector2 size, Color color)
        {
            RectTransform rect = UIFactory.Anchored("Line", radarField, new Vector2(0.5f, 0.5f), center, size);
            UIFactory.AddImage(rect, color);
        }

        private void BuildPlayerTag(Transform root)
        {
            tagRect = UIFactory.Rect("PlayerTag", root, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(64f, 34f));
            tagBackground = UIFactory.AddSprite(tagRect, UISprites.Pill, UITheme.Panel);
            tagText = UIFactory.Label("Text", tagRect, "", 22, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform arrow = UIFactory.Rect("Arrow", tagRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                                                 new Vector2(0f, 2f), new Vector2(16f, 10f));
            tagArrow = UIFactory.AddSprite(arrow, UISprites.Triangle, Color.white, sliced: false);
            arrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        // ================================================================== Actualización

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
            UpdateMessages();
            UpdateRadar();
            hint.text = human.Input != null && human.Input.LastDeviceIsGamepad ? "R3  CONTROLES     START  PAUSA" : "F1  CONTROLES     ESC  PAUSA";
        }

        private void LateUpdate()
        {
            UpdatePlayerTag();
        }

        private void UpdateScoreboard()
        {
            if (status != null)
            {
                if (status.HomeGoals != lastHome || status.AwayGoals != lastAway)
                {
                    if (lastHome >= 0) scorePopAt = Time.unscaledTime;
                    lastHome = status.HomeGoals;
                    lastAway = status.AwayGoals;
                    score.text = $"{lastHome} - {lastAway}";
                }
                clock.text = status.ClockText;
                phase.text = status.PhaseText.ToUpperInvariant();
                bool live = status.IsBallInPlay;
                liveDot.color = live
                    ? new Color(UITheme.Live.r, UITheme.Live.g, UITheme.Live.b, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f))
                    : new Color(1f, 1f, 1f, 0.3f);
            }

            float pop = Mathf.Clamp01(1f - (Time.unscaledTime - scorePopAt) / 0.6f);
            scoreRect.localScale = Vector3.one * (1f + 0.55f * pop * pop);
            score.color = Color.Lerp(UITheme.Text, UITheme.Gold, pop);

            FootballPlayer owner = context.Ball.Owner;
            SetAlpha(homePossession, owner != null && owner.Team == context.HomeTeam ? 1f : 0.12f);
            SetAlpha(awayPossession, owner != null && owner.Team == context.AwayTeam ? 1f : 0.12f);
        }

        private void UpdatePlayerCard()
        {
            FootballPlayer player = human.ActivePlayer;
            if (player == null) return;
            cardNumber.text = player.ShirtNumber.ToString();
            cardStripe.color = player.Team.Settings.primaryColor;
            cardRole.text = RoleName(player.Role).ToUpperInvariant() +
                            (player.Motor.IsStunned ? "  <color=#FF6070>· ATURDIDO</color>" : player.Motor.IsSprinting ? "  <color=#FFD040>· SPRINT</color>" : "");
            UIFactory.SetBar(staminaFill, player.Stamina.Current01);
            staminaImage.color = player.Stamina.IsExhausted ? UITheme.Live : UITheme.Accent;

            FootballInput input = human.Input;
            ChargeButton charging = input.Shot.IsCharging ? input.Shot
                                  : input.LongPass.IsCharging ? input.LongPass
                                  : input.ShortPass.IsCharging ? input.ShortPass : null;
            chargeRoot.SetActive(charging != null);
            if (charging == null) return;
            chargeLabel.text = charging == input.Shot ? "POTENCIA" : charging == input.LongPass ? "PASE LARGO" : "PASE";
            UIFactory.SetBar(chargeFill, charging.Charge01);
            chargeImage.color = charging == input.Shot && charging.Charge01 > 0.88f ? UITheme.Live : UITheme.Gold;
        }

        private void UpdateMessages()
        {
            if (replay != null && replay.IsReplaying)
            {
                ShowOnly(bannerRoot);
                bannerTitle.text = "<color=#FF4050>●</color> REPETICIÓN";
                bannerSubtitle.text = "J / A para saltar";
                bannerGroup.alpha = 0.92f;
                return;
            }

            bool fullTime = manager != null && manager.State == MatchState.FullTime;
            bool active = status != null && !status.IsPaused && !fullTime && !string.IsNullOrEmpty(status.Message) &&
                          Time.unscaledTime < status.MessageUntil;
            if (!active)
            {
                ShowOnly(null);
                shownMessage = null;
                return;
            }

            if (status.Message != shownMessage)
            {
                shownMessage = status.Message;
                shownAt = Time.unscaledTime;
            }
            float age = Time.unscaledTime - shownAt;
            float remaining = status.MessageUntil - Time.unscaledTime;
            float fadeOut = float.IsPositiveInfinity(remaining) ? 1f : Mathf.Clamp01(remaining / 0.35f);
            float appear = Mathf.Clamp01(age / 0.3f);
            float eased = 1f - (1f - appear) * (1f - appear);

            string[] lines = status.Message.Split('\n');
            switch (status.MessageStyle)
            {
                case MessageStyle.Goal:
                    ShowOnly(goalRoot);
                    Color teamColor = goalTeam != null ? goalTeam.Settings.primaryColor : UITheme.Accent;
                    goalBand.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.92f);
                    goalBandRect.localScale = new Vector3(eased, 1f, 1f);
                    goalTitleRect.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, eased);
                    goalSubtitle.text = goalDetail ?? (lines.Length > 1 ? lines[1] : "");
                    goalGroup.alpha = fadeOut;
                    break;

                case MessageStyle.Banner:
                    ShowOnly(bannerRoot);
                    bannerTitle.text = lines[0].ToUpperInvariant();
                    bannerSubtitle.text = lines.Length > 1 ? string.Join("  ·  ", lines, 1, lines.Length - 1) : "";
                    bannerGroup.alpha = Mathf.Min(eased, fadeOut);
                    bannerRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                    break;

                default:
                    ShowOnly(toastRoot);
                    toastText.text = string.Join("  ·  ", lines);
                    toastGroup.alpha = Mathf.Min(eased, fadeOut);
                    toastRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-20f, -66f, eased));
                    break;
            }
        }

        private void ShowOnly(GameObject visible)
        {
            if (toastRoot.activeSelf != (visible == toastRoot)) toastRoot.SetActive(visible == toastRoot);
            if (bannerRoot.activeSelf != (visible == bannerRoot)) bannerRoot.SetActive(visible == bannerRoot);
            if (goalRoot.activeSelf != (visible == goalRoot)) goalRoot.SetActive(visible == goalRoot);
        }

        private void UpdateRadar()
        {
            Pitch pitch = context.Pitch;
            foreach (KeyValuePair<FootballPlayer, RectTransform> pair in radarDots)
            {
                pair.Value.anchoredPosition = ToRadar(pair.Key.Position, pitch);
            }
            radarBall.anchoredPosition = ToRadar(context.Ball.Position, pitch);
            FootballPlayer active = human.ActivePlayer;
            radarActive.gameObject.SetActive(active != null);
            if (active != null) radarActive.anchoredPosition = ToRadar(active.Position, pitch);
        }

        private static Vector2 ToRadar(Vector3 world, Pitch pitch)
        {
            Vector3 local = world - pitch.Center;
            float x = Mathf.Clamp(local.x / pitch.Length, -0.53f, 0.53f) * RadarWidth;
            float y = Mathf.Clamp(local.z / pitch.Width, -0.53f, 0.53f) * RadarHeight;
            return new Vector2(x, y);
        }

        private void UpdatePlayerTag()
        {
            FootballPlayer player = human.ActivePlayer;
            Camera cam = Camera.main;
            bool visible = player != null && cam != null && (replay == null || !replay.IsReplaying);
            if (visible)
            {
                Vector3 screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * 2.25f);
                visible = screen.z > 0f;
                if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
                {
                    tagRect.anchoredPosition = local + canvasRect.rect.size * 0.5f;
                    tagText.text = player.ShirtNumber.ToString();
                    Color teamColor = player.Team.Settings.primaryColor;
                    tagBackground.color = new Color(teamColor.r * 0.6f, teamColor.g * 0.6f, teamColor.b * 0.6f, 0.92f);
                    tagArrow.color = UITheme.Accent;
                }
            }
            if (tagRect.gameObject.activeSelf != visible) tagRect.gameObject.SetActive(visible);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = Mathf.MoveTowards(c.a, alpha, Time.unscaledDeltaTime * 4f);
            image.color = c;
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
