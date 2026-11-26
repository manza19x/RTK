using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Identidad del futbolista y punto de acceso a sus componentes. No contiene lógica de control:
    /// lo mueve quien tenga el mando (el jugador humano mediante <see cref="HumanTeamController"/>
    /// o, a partir de la Fase 2, la IA). Ambos usan la misma API de <see cref="PlayerMotor"/> y <see cref="PlayerActions"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerStamina), typeof(PlayerActions))]
    public class FootballPlayer : MonoBehaviour
    {
        [SerializeField] private int shirtNumber = 1;
        [SerializeField] private PlayerRole role = PlayerRole.Midfielder;
        [Tooltip("Índice del puesto en la formación del equipo.")]
        [SerializeField] private int formationIndex;
        [SerializeField] private CapsuleCollider bodyCollider;

        public Team Team { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public PlayerStamina Stamina { get; private set; }
        public PlayerActions Actions { get; private set; }

        public int ShirtNumber => shirtNumber;
        public PlayerRole Role => role;
        public int FormationIndex => formationIndex;
        public bool IsGoalkeeper => role == PlayerRole.Goalkeeper;

        /// <summary>True mientras el jugador humano controla a este futbolista.</summary>
        public bool IsHumanControlled { get; set; }

        /// <summary>Posición a ras de suelo.</summary>
        public Vector3 Position
        {
            get
            {
                Vector3 p = transform.position;
                p.y = 0f;
                return p;
            }
        }

        public Vector3 Forward
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            }
        }

        public bool HasBall
        {
            get
            {
                var context = MatchContext.Current;
                return context != null && context.Ball.Owner == this;
            }
        }

        private void Awake()
        {
            Motor = GetComponent<PlayerMotor>();
            Stamina = GetComponent<PlayerStamina>();
            Actions = GetComponent<PlayerActions>();
            if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider>();
        }

        /// <summary>Lo llama <see cref="Team"/> en su Awake.</summary>
        public void Initialize(Team team)
        {
            Team = team;
            MatchContext.Current?.RegisterPlayer(this);
        }

        private void Start()
        {
            var context = MatchContext.Current;
            if (context != null && bodyCollider != null)
            {
                // Ver Ball: el contacto jugador-balón lo gestionan reglas, no colisiones físicas.
                Physics.IgnoreCollision(context.Ball.Collider, bodyCollider, true);
            }
        }

        public override string ToString() => Team != null ? $"{Team.Settings.shortName} {shirtNumber}" : $"#{shirtNumber}";
    }
}
