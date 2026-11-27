using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Pantalla de resultado al final del partido: resultado desde el punto de vista del jugador, marcador,
    /// goles con minuto y dorsal, posesión y tiros. Botones para jugar otra vez o volver al menú.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        private MatchManager manager;
        private MatchStats stats;
        private MatchContext context;
        private Team humanTeam;
        private GameObject root;
        private Text verdict, scoreLine, goalsText, statsText;
        private Button playAgain;

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
            Canvas canvas = UIFactory.CreateCanvas("ResultScreen", 10);
            canvas.transform.SetParent(transform, false);
            RectTransform rootRect = UIFactory.Stretch("Root", canvas.transform);
            root = rootRect.gameObject;
            UIFactory.AddImage(rootRect, UITheme.Backdrop, raycast: true);

            RectTransform box = UIFactory.Anchored("Box", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 820f));
            UIFactory.AddImage(box, UITheme.Panel);

            UIFactory.AddText(UIFactory.Anchored("Title", box, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(1000f, 60f)),
                              "FINAL DEL PARTIDO", 40, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            verdict = UIFactory.AddText(UIFactory.Anchored("Verdict", box, new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(1000f, 80f)),
                                        "", 64, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreLine = UIFactory.AddText(UIFactory.Anchored("Score", box, new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(1000f, 70f)),
                                          "", 46, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.AddText(UIFactory.Anchored("GoalsTitle", box, new Vector2(0.5f, 1f), new Vector2(-260f, -300f), new Vector2(460f, 40f)),
                              "Goles", 28, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            goalsText = UIFactory.AddText(UIFactory.Anchored("Goals", box, new Vector2(0.5f, 1f), new Vector2(-260f, -480f), new Vector2(460f, 320f)),
                                          "", 26, UITheme.Text, TextAnchor.UpperLeft);

            UIFactory.AddText(UIFactory.Anchored("StatsTitle", box, new Vector2(0.5f, 1f), new Vector2(260f, -300f), new Vector2(460f, 40f)),
                              "Estadísticas", 28, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            statsText = UIFactory.AddText(UIFactory.Anchored("Stats", box, new Vector2(0.5f, 1f), new Vector2(260f, -400f), new Vector2(460f, 160f)),
                                          "", 26, UITheme.Text, TextAnchor.UpperLeft);

            RectTransform buttons = UIFactory.Anchored("Buttons", box, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(940f, 70f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 30f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            playAgain = UIFactory.Button(buttons, "Jugar otra vez", () => manager.RestartMatch(), 400f, 66f);
            UIFactory.Button(buttons, "Menú principal", () => SceneLoader.Load(SceneNames.MainMenu), 400f, 66f);
        }

        private void Show()
        {
            Team home = context.HomeTeam;
            Team away = context.AwayTeam;
            int homeGoals = manager.HomeGoals;
            int awayGoals = manager.AwayGoals;

            if (homeGoals == awayGoals) verdict.text = "EMPATE";
            else if (humanTeam == null) verdict.text = homeGoals > awayGoals ? $"GANA {home.Settings.teamName.ToUpper()}" : $"GANA {away.Settings.teamName.ToUpper()}";
            else
            {
                bool humanWon = (humanTeam == home) == (homeGoals > awayGoals);
                verdict.text = humanWon ? "¡VICTORIA!" : "DERROTA";
            }
            scoreLine.text = $"{home.Settings.teamName}  {homeGoals} - {awayGoals}  {away.Settings.teamName}";

            var builder = new StringBuilder();
            if (stats == null || stats.Goals.Count == 0)
            {
                builder.Append("<color=#B8BECC>Sin goles</color>");
            }
            else
            {
                foreach (MatchStats.GoalRecord goal in stats.Goals)
                {
                    Team team = goal.Side == TeamSide.Home ? home : away;
                    string who = goal.OwnGoal ? "en propia puerta" : goal.ShirtNumber > 0 ? $"dorsal {goal.ShirtNumber}" : "";
                    builder.AppendLine($"{goal.Minute}'  <b>{team.Settings.shortName}</b>  {who}");
                }
            }
            goalsText.text = builder.ToString();

            if (stats != null)
            {
                statsText.text =
                    $"Posesión     {stats.PossessionPercent(TeamSide.Home)}%  -  {stats.PossessionPercent(TeamSide.Away)}%\n" +
                    $"Tiros           {stats.Shots(TeamSide.Home)}  -  {stats.Shots(TeamSide.Away)}";
            }
            else
            {
                statsText.text = "";
            }

            root.SetActive(true);
            UIFactory.Select(playAgain);
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
            UIFactory.ClearSelection();
        }
    }
}
