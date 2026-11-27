using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Punto de acceso único a los objetos compartidos de un partido (balón, campo, equipos, jugadores).
    /// Decisión: en lugar de que cada script busque objetos en la escena, todos consultan este contexto.
    /// Hay exactamente uno por escena de partido; se ejecuta antes que el resto para que esté listo en Awake.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MatchContext : MonoBehaviour
    {
        public static MatchContext Current { get; private set; }

        [SerializeField] private Ball ball;
        [SerializeField] private Pitch pitch;
        [SerializeField] private Team homeTeam;
        [SerializeField] private Team awayTeam;
        [Tooltip("Paso fijo de física. 1/60 s hace el balón más estable que el valor por defecto (1/50).")]
        [SerializeField] private float fixedTimestep = 1f / 60f;

        private readonly List<FootballPlayer> allPlayers = new List<FootballPlayer>();

        public Ball Ball => ball;
        public Pitch Pitch => pitch;
        public Team HomeTeam => homeTeam;
        public Team AwayTeam => awayTeam;
        public IReadOnlyList<FootballPlayer> AllPlayers => allPlayers;
        /// <summary>Reglas activas (MatchManager o TrainingHarness). Null si la escena no tiene ninguna.</summary>
        public IMatchStatus Rules { get; private set; }

        public void RegisterRules(IMatchStatus rules) => Rules = rules;

        /// <summary>Se lanza cuando un jugador comete una falta (lo sancionan las reglas activas).</summary>
        public event System.Action<FoulEvent> FoulCommitted;

        public void ReportFoul(FoulEvent foul) => FoulCommitted?.Invoke(foul);

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Debug.LogWarning("[Fútbol Total] Hay más de un MatchContext en la escena; se usará el último.", this);
            }
            Current = this;
            Time.fixedDeltaTime = fixedTimestep;

            if (homeTeam == null || awayTeam == null || ball == null || pitch == null)
            {
                Debug.LogError("[Fútbol Total] MatchContext necesita referencias a Ball, Pitch y los dos equipos.", this);
                return;
            }
            homeTeam.SetOpponent(awayTeam);
            awayTeam.SetOpponent(homeTeam);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void RegisterPlayer(FootballPlayer player)
        {
            if (!allPlayers.Contains(player)) allPlayers.Add(player);
        }

        public Team GetTeam(TeamSide side) => side == TeamSide.Home ? homeTeam : awayTeam;

        /// <summary>Equipo que ataca hacia la portería situada en <paramref name="goalSide"/> (-1 o +1 en X).</summary>
        public Team TeamAttacking(int goalSide) => homeTeam.AttackDirection == goalSide ? homeTeam : awayTeam;
    }
}
