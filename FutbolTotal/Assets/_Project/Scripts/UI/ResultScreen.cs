using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Pantalla de final del partido: resultado desde el punto de vista del jugador (victoria, derrota o empate),
    /// escudos y marcador grande, goleadores con minuto y barras comparativas de posesión, tiros y faltas.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        private MatchManager manager;
        private MatchStats stats;
        private MatchContext context;
        private Team humanTeam;
        private GameObject root;
        private RectTransform card;
        private Text verdict, score, homeGoalsText, awayGoalsText;
        private Image verdictBand;
        private Button playAgain;
        private float shownAt;
        private readonly StatRow[] rows = new StatRow[3];

        private struct StatRow
        {
            public Text Home, Away;
            public RectTransform HomeFill, AwayFill;
        }

        private void Start()
        {
            context = MatchContext.Current;
            manager = context != null ? context.Rules as MatchManager : null;
            if (manager == null)
            {
                enabled = false;
                return;
            }
            stats = Object.FindFirstObjectByType<MatchStats>();
            HumanTeamController human = Object.FindFirstObjectByType<HumanTeamController>();
            humanTeam = human != null ? human.Team : null;

            Build();
            root.SetActive(false);
            manager.MatchEnded += Show;
            manager.MatchStarted += Hide;
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.MatchEnded -= Show;
            manager.MatchStarted -= Hide;
        }

        private void Build()
        {
            Team home = context.HomeTeam;
            Team away = context.AwayTeam;
            Canvas canvas = UIFactory.CreateCanvas("ResultScreen", 10);
            canvas.transform.SetParent(transform, false);
            RectTransform rootRect = UIFactory.Stretch("Root", canvas.transform);
            root = rootRect.gameObject;
            UIFactory.AddImage(rootRect, UITheme.Backdrop, raycast: true);

            card = UIFactory.Anchored("Card", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 880f));
            UIFactory.AddSprite(card, UISprites.Rounded, UITheme.Panel);

            // Cinta superior con el veredicto.
            RectTransform band = UIFactory.Rect("Verdict", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 110f));
            verdictBand = UIFactory.AddSprite(band, UISprites.Rounded, UITheme.Accent);
            verdict = UIFactory.Label("Text", band, "", 64, UITheme.TextDark, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            UIFactory.AddText(UIFactory.Rect("Sub", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(0f, 36f)),
                              "FINAL DEL PARTIDO", 22, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Escudos y marcador.
            TeamColumn(card, home, -400f);
            TeamColumn(card, away, 400f);
            score = UIFactory.AddText(UIFactory.Anchored("Score", card, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(500f, 150f)),
                                      "0 - 0", 130, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.AddTextShadow(score, 4f, 0.5f);

            // Goleadores bajo cada escudo.
            homeGoalsText = UIFactory.AddText(UIFactory.Anchored("HomeGoals", card, new Vector2(0.5f, 1f), new Vector2(-400f, -440f), new Vector2(380f, 110f)),
                                              "", 22, UITheme.TextDim, TextAnchor.UpperCenter);
            awayGoalsText = UIFactory.AddText(UIFactory.Anchored("AwayGoals", card, new Vector2(0.5f, 1f), new Vector2(400f, -440f), new Vector2(380f, 110f)),
                                              "", 22, UITheme.TextDim, TextAnchor.UpperCenter);

            // Estadísticas comparadas.
            string[] labels = { "POSESIÓN", "TIROS", "FALTAS" };
            for (int i = 0; i < labels.Length; i++)
            {
                rows[i] = StatBar(card, labels[i], -530f - i * 62f, home.Settings.primaryColor, away.Settings.primaryColor);
            }

            RectTransform buttons = UIFactory.Anchored("Buttons", card, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(940f, 76f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 30f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            playAgain = UIFactory.Button(buttons, "Jugar otra vez", () => manager.RestartMatch(), 420f, 72f, 30);
            UIFactory.Button(buttons, "Menú principal", () => SceneLoader.Load(SceneNames.MainMenu), 420f, 72f, 30);
        }

        private static void TeamColumn(RectTransform card, Team team, float x)
        {
            RectTransform crest = UIFactory.Anchored("Crest", card, new Vector2(0.5f, 1f), new Vector2(x, -250f), new Vector2(170f, 170f));
            UIFactory.AddSprite(crest, UISprites.Crest(team.Settings.primaryColor, team.Settings.secondaryColor), Color.white, sliced: false);
            UIFactory.AddText(UIFactory.Anchored("Name", card, new Vector2(0.5f, 1f), new Vector2(x, -368f), new Vector2(380f, 44f)),
                              team.Settings.teamName.ToUpperInvariant(), 30, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static StatRow StatBar(RectTransform card, string label, float y, Color homeColor, Color awayColor)
        {
            var result = new StatRow();
            UIFactory.AddText(UIFactory.Anchored("Label_" + label, card, new Vector2(0.5f, 1f), new Vector2(0f, y + 22f), new Vector2(300f, 26f)),
                              label, 20, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            result.Home = UIFactory.AddText(UIFactory.Anchored("Home_" + label, card, new Vector2(0.5f, 1f), new Vector2(-470f, y - 6f), new Vector2(120f, 36f)),
                                            "", 30, UITheme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            result.Away = UIFactory.AddText(UIFactory.Anchored("Away_" + label, card, new Vector2(0.5f, 1f), new Vector2(470f, y - 6f), new Vector2(120f, 36f)),
                                            "", 30, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);

            // Barra dividida: crece desde el centro hacia cada lado según la proporción.
            RectTransform track = UIFactory.Anchored("Track_" + label, card, new Vector2(0.5f, 1f), new Vector2(0f, y - 6f), new Vector2(760f, 14f));
            UIFactory.AddSprite(track, UISprites.Pill, UITheme.BarBackground);
            result.HomeFill = UIFactory.Rect("HomeFill", track, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            result.HomeFill.offsetMin = Vector2.zero;
            result.HomeFill.offsetMax = Vector2.zero;
            UIFactory.AddSprite(result.HomeFill, UISprites.Pill, homeColor);
            result.AwayFill = UIFactory.Rect("AwayFill", track, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            result.AwayFill.offsetMin = Vector2.zero;
            result.AwayFill.offsetMax = Vector2.zero;
            UIFactory.AddSprite(result.AwayFill, UISprites.Pill, awayColor);
            return result;
        }

        private static void SetRow(StatRow row, float home, float away, string homeText, string awayText)
        {
            float total = home + away;
            float homeShare = total > 0f ? home / total : 0.5f;
            row.Home.text = homeText;
            row.Away.text = awayText;
            // Barra dividida: el local ocupa su proporción desde la izquierda y el visitante el resto.
            row.HomeFill.anchorMin = new Vector2(0f, 0f);
            row.HomeFill.anchorMax = new Vector2(homeShare, 1f);
            row.AwayFill.anchorMin = new Vector2(homeShare, 0f);
            row.AwayFill.anchorMax = new Vector2(1f, 1f);
        }

        private void Show()
        {
            Team home = context.HomeTeam;
            Team away = context.AwayTeam;
            int homeGoals = manager.HomeGoals;
            int awayGoals = manager.AwayGoals;

            if (homeGoals == awayGoals)
            {
                verdict.text = "EMPATE";
                verdictBand.color = new Color(0.75f, 0.78f, 0.84f);
            }
            else if (humanTeam == null)
            {
                Team winner = homeGoals > awayGoals ? home : away;
                verdict.text = $"GANA {winner.Settings.teamName.ToUpperInvariant()}";
                verdictBand.color = UITheme.Accent;
            }
            else
            {
                bool humanWon = (humanTeam == home) == (homeGoals > awayGoals);
                verdict.text = humanWon ? "¡VICTORIA!" : "DERROTA";
                verdictBand.color = humanWon ? UITheme.Accent : UITheme.Live;
            }
            score.text = $"{homeGoals} - {awayGoals}";

            var homeList = new StringBuilder();
            var awayList = new StringBuilder();
            if (stats != null)
            {
                foreach (MatchStats.GoalRecord goal in stats.Goals)
                {
                    string who = goal.OwnGoal ? "en propia" : goal.ShirtNumber > 0 ? $"dorsal {goal.ShirtNumber}" : "";
                    (goal.Side == TeamSide.Home ? homeList : awayList).AppendLine($"{goal.Minute}'  {who}");
                }
                int homePossession = stats.PossessionPercent(TeamSide.Home);
                int awayPossession = stats.PossessionPercent(TeamSide.Away);
                SetRow(rows[0], homePossession, awayPossession, $"{homePossession}%", $"{awayPossession}%");
                SetRow(rows[1], stats.Shots(TeamSide.Home), stats.Shots(TeamSide.Away), stats.Shots(TeamSide.Home).ToString(), stats.Shots(TeamSide.Away).ToString());
                SetRow(rows[2], stats.Fouls(TeamSide.Home), stats.Fouls(TeamSide.Away), stats.Fouls(TeamSide.Home).ToString(), stats.Fouls(TeamSide.Away).ToString());
            }
            homeGoalsText.text = homeList.ToString();
            awayGoalsText.text = awayList.ToString();

            root.SetActive(true);
            shownAt = Time.unscaledTime;
            UIFactory.Select(playAgain);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.35f);
            float eased = 1f - (1f - t) * (1f - t);
            card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
            UIFactory.ClearSelection();
        }
    }
}
