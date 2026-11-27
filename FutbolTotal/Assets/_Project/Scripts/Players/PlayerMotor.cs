using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Movimiento físico del futbolista. Recibe una intención (dirección + sprint) y la convierte en velocidad
    /// con aceleración, frenada y giro limitados.
    /// Decisión: Rigidbody con velocidad controlada por código (no CharacterController), para que los jugadores
    /// choquen entre sí de forma natural y la física y el balón compartan el mismo paso de tiempo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;

        private Rigidbody body;
        private PlayerStamina stamina;
        private FootballPlayer player;

        private Vector3 moveInput;
        private bool sprintRequested;
        private Vector3 velocity;

        private float stunnedUntil;
        private float slowedUntil;
        private float lungeUntil;
        private Vector3 lungeVelocity;

        public Vector3 Velocity => velocity;
        public float Speed => velocity.magnitude;
        public bool IsSprinting { get; private set; }
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsLunging => Time.time < lungeUntil;
        public float JogSpeed => settings.jogSpeed;
        public float SprintSpeed => settings.sprintSpeed;
        public float BodyRadius => settings.bodyRadius;
        /// <summary>Dirección de movimiento pedida en este momento (normalizada o cero).</summary>
        public Vector3 MoveInput => moveInput;

        private void Awake()
        {
            settings = SettingsProvider.Resolve(settings, SettingsProvider.MovementName, this);

            body = GetComponent<Rigidbody>();
            stamina = GetComponent<PlayerStamina>();
            player = GetComponent<FootballPlayer>();

            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        /// <summary>Intención de movimiento en el plano del campo. La magnitud (0..1) escala la velocidad.</summary>
        public void SetMoveInput(Vector3 worldDirection, bool sprint)
        {
            worldDirection.y = 0f;
            moveInput = Vector3.ClampMagnitude(worldDirection, 1f);
            sprintRequested = sprint;
        }

        public void Stop() => SetMoveInput(Vector3.zero, false);

        /// <summary>Sin control durante <paramref name="duration"/> segundos (entrada recibida o fallida).</summary>
        public void Stun(float duration)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
        }

        /// <summary>Velocidad reducida un instante (tras pasar o tirar).</summary>
        public void ApplyActionSlowdown(float duration)
        {
            slowedUntil = Mathf.Max(slowedUntil, Time.time + duration);
        }

        /// <summary>Impulso corto en una dirección fija (entrada).</summary>
        public void StartLunge(Vector3 direction, float speed, float duration)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = transform.forward;
            lungeVelocity = direction.normalized * speed;
            lungeUntil = Time.time + duration;
            transform.rotation = Quaternion.LookRotation(lungeVelocity);
            body.rotation = transform.rotation;
        }

        /// <summary>Coloca al jugador sin interpolación ni inercia (saques, reinicios).</summary>
        public void Teleport(Vector3 position, Vector3 facing)
        {
            position.y = 0f;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : transform.rotation;
            velocity = Vector3.zero;
            moveInput = Vector3.zero;
            lungeUntil = 0f;
            stunnedUntil = 0f;
            body.linearVelocity = Vector3.zero;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Partimos de la velocidad real del cuerpo: si ha chocado con otro jugador, la inercia lo refleja.
            Vector3 v = body.linearVelocity;
            velocity = new Vector3(v.x, 0f, v.z);

            if (IsLunging)
            {
                velocity = lungeVelocity;
                IsSprinting = false;
                stamina?.Tick(false, dt);
                body.linearVelocity = velocity;
                return;
            }

            Vector3 input = IsStunned ? Vector3.zero : moveInput;
            bool wantsSprint = sprintRequested && input.sqrMagnitude > 0.25f;
            IsSprinting = wantsSprint && (stamina == null || stamina.CanSprint);
            stamina?.Tick(IsSprinting, dt);

            bool hasBall = player != null && player.HasBall;
            float maxSpeed = IsSprinting ? settings.sprintSpeed : settings.jogSpeed;
            if (hasBall) maxSpeed *= settings.withBallSpeedMultiplier;
            if (Time.time < slowedUntil) maxSpeed *= settings.actionSlowdownMultiplier;

            float acceleration = IsSprinting ? settings.sprintAcceleration : settings.acceleration;
            velocity = Steer(velocity, input * maxSpeed, acceleration, settings.deceleration, dt);
            body.linearVelocity = velocity;

            Rotate(input, hasBall, dt);
        }

        /// <summary>
        /// Acerca la velocidad actual a la deseada separando dos componentes:
        /// - la componente lateral (respecto a la dirección deseada) se frena con fuerza → sin deslizamientos;
        /// - la componente en la dirección deseada acelera o frena según haga falta; si va en sentido contrario,
        ///   primero frena hasta cero (un cambio de sentido no es instantáneo).
        /// </summary>
        private static Vector3 Steer(Vector3 current, Vector3 desired, float acceleration, float deceleration, float dt)
        {
            float desiredSpeed = desired.magnitude;
            if (desiredSpeed < 1e-3f)
            {
                return Vector3.MoveTowards(current, Vector3.zero, deceleration * dt);
            }

            Vector3 direction = desired / desiredSpeed;
            float along = Vector3.Dot(current, direction);
            Vector3 lateral = current - direction * along;

            lateral = Vector3.MoveTowards(lateral, Vector3.zero, deceleration * dt);
            if (along < 0f) along = Mathf.MoveTowards(along, 0f, deceleration * dt);
            else if (along < desiredSpeed) along = Mathf.MoveTowards(along, desiredSpeed, acceleration * dt);
            else along = Mathf.MoveTowards(along, desiredSpeed, deceleration * dt);

            return direction * along + lateral;
        }

        private void Rotate(Vector3 input, bool hasBall, float dt)
        {
            // Miramos hacia donde nos movemos; parado, hacia donde se apunta (permite orientarse antes de pasar o tirar).
            Vector3 look = velocity.sqrMagnitude > 0.25f ? velocity : input;
            if (look.sqrMagnitude < 1e-4f) return;

            float rate = IsSprinting ? settings.sprintTurnRate : settings.turnRate;
            if (hasBall) rate *= settings.withBallTurnMultiplier;

            Quaternion target = Quaternion.LookRotation(look.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, target, rate * dt));
        }
    }
}
