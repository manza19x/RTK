using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// "Cerebro" de un futbolista controlado por la IA. Ejecuta el comportamiento de su rol actual
    /// (lo asigna <see cref="TeamAI"/>) y guarda la pequeña memoria que necesitan los comportamientos.
    /// No actúa si el jugador lo controla el humano ni cuando el balón no está en juego.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FootballPlayer))]
    public class PlayerBrain : MonoBehaviour
    {
        private IPlayerBehavior behavior;

        public FootballPlayer Player { get; private set; }
        public TeamAI TeamAI { get; private set; }
        public AISettings Settings => TeamAI.Settings;
        public AIRole Role { get; private set; } = AIRole.Zone;

        // Memoria de los comportamientos.
        /// <summary>Rival que marca este jugador cuando defiende su zona (lo asigna TeamAI).</summary>
        public FootballPlayer MarkTarget { get; set; }
        public float NextDecisionTime { get; set; }
        public Vector3 MoveTarget { get; set; }
        public Vector3 DribbleDirection { get; set; }
        public float InTackleRangeSince { get; set; } = -1f;
        /// <summary>Momento del último golpeo al que ya reaccionó el portero.</summary>
        public float HandledKickTime { get; set; } = -1f;
        public float HoldBallUntil { get; set; }
        public bool HadBall { get; set; }

        private void Awake()
        {
            Player = GetComponent<FootballPlayer>();
        }

        public void Initialize(TeamAI teamAI)
        {
            TeamAI = teamAI;
            SetRole(Player.IsGoalkeeper ? AIRole.Goalkeeper : AIRole.Zone);
        }

        public void SetRole(AIRole role)
        {
            if (role == Role && behavior != null) return;
            Role = role;
            behavior = AIBehaviors.For(role);
            InTackleRangeSince = -1f;
            NextDecisionTime = Time.time + (role == AIRole.BallCarrier && TeamAI != null ? Settings.firstTouchDelay : 0f);
            DribbleDirection = Vector3.zero;
        }

        private void Update()
        {
            if (TeamAI == null || Player.IsHumanControlled) return;
            if (!TeamAI.CanAct)
            {
                Player.Motor.Stop();
                return;
            }
            behavior?.Tick(this);
        }

        // ------------------------------------------------------------------ Ayudas de movimiento

        /// <summary>
        /// Lleva al jugador hacia <paramref name="target"/>, frenando al acercarse para no pasarse.
        /// Devuelve true si ya está colocado (entonces se orienta hacia el balón).
        /// </summary>
        public bool MoveTo(Vector3 target, bool allowSprint, float tolerance = -1f)
        {
            if (tolerance < 0f) tolerance = Settings.arrivalTolerance;
            Vector3 toTarget = KickMath.Flat(target - Player.Position);
            float distance = toTarget.magnitude;

            if (distance <= tolerance)
            {
                Player.Motor.Stop();
                FaceBall();
                return true;
            }

            float speedScale = Mathf.Clamp(distance / Mathf.Max(0.1f, Settings.slowRadius), 0.35f, 1f);
            bool sprint = allowSprint && distance > Settings.sprintDistance &&
                          Player.Stamina.Current01 > Settings.minStaminaToSprint;
            Player.Motor.SetMoveInput(toTarget / distance * speedScale, sprint);
            return false;
        }

        public void FaceBall()
        {
            var context = MatchContext.Current;
            if (context != null) Player.Motor.SetFacing(context.Ball.Position - Player.Position);
        }
    }
}
