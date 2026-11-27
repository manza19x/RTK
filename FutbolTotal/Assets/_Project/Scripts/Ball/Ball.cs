using System;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Datos de un golpeo, para que otros sistemas (control humano, IA, cámara, UI) reaccionen.</summary>
    public readonly struct KickEvent
    {
        public readonly FootballPlayer Kicker;
        public readonly KickType Type;
        /// <summary>Compañero al que iba dirigido el pase (null en tiros y pases al espacio).</summary>
        public readonly FootballPlayer IntendedReceiver;
        public readonly Vector3 Velocity;
        public readonly float Time;

        public KickEvent(FootballPlayer kicker, KickType type, FootballPlayer intendedReceiver, Vector3 velocity, float time)
        {
            Kicker = kicker;
            Type = type;
            IntendedReceiver = intendedReceiver;
            Velocity = velocity;
            Time = time;
        }
    }

    /// <summary>
    /// Balón: física propia y estado de posesión.
    /// Decisiones clave:
    /// - El balón no colisiona físicamente con los futbolistas (se ignoran las colisiones). La interacción
    ///   jugador-balón la deciden reglas explícitas (<see cref="BallPossession"/>), que son estables y ajustables;
    ///   los choques físicos cápsula-esfera producen rebotes imprevisibles.
    /// - Rueda con una desaceleración constante y vuela sin rozamiento, para que los pases se calculen con exactitud.
    /// - Rotación física congelada; el giro visual lo hace <see cref="BallVisual"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        [SerializeField] private BallSettings settings;

        private Rigidbody body;
        private SphereCollider sphere;
        private FootballPlayer ignoredPlayer;
        private float ignoreUntil;

        /// <summary>Jugador que conduce el balón (null si está suelto o en vuelo).</summary>
        public FootballPlayer Owner { get; private set; }
        /// <summary>Último jugador que tocó el balón (para saques de banda, córners, etc. en fases posteriores).</summary>
        public FootballPlayer LastToucher { get; private set; }
        public KickEvent? LastKick { get; private set; }

        public BallSettings Settings => settings;
        public Collider Collider => sphere;
        public float Radius => settings.radius;
        public Vector3 Position => body.position;
        public Vector3 Velocity => body.linearVelocity;
        public bool IsFree => Owner == null;
        public float TimeSinceKick => LastKick.HasValue ? Time.time - LastKick.Value.Time : float.PositiveInfinity;

        public bool IsGrounded =>
            body.position.y <= settings.radius + settings.groundedTolerance && Mathf.Abs(body.linearVelocity.y) < 1f;

        /// <summary>(anterior, nuevo). Se lanza al ganar o perder la posesión.</summary>
        public event Action<FootballPlayer, FootballPlayer> OwnerChanged;
        public event Action<KickEvent> Kicked;
        /// <summary>Choque físico del balón (suelo, postes, red, muros). Lo usa el sonido.</summary>
        public event Action<Collision> Bounced;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            settings = SettingsProvider.Resolve(settings, SettingsProvider.BallName, this);

            sphere.radius = settings.radius;
            body.mass = settings.mass;
            body.useGravity = true;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void OnCollisionEnter(Collision collision) => Bounced?.Invoke(collision);

        /// <summary>¿Debe <paramref name="player"/> ignorar el balón (acaba de golpearlo)?</summary>
        public bool IsIgnoring(FootballPlayer player) => player == ignoredPlayer && Time.time < ignoreUntil;

        public void SetOwner(FootballPlayer player)
        {
            if (Owner == player) return;
            FootballPlayer previous = Owner;
            Owner = player;
            if (player != null) LastToucher = player;
            OwnerChanged?.Invoke(previous, player);
        }

        /// <summary>Golpea el balón con una velocidad ya calculada (pase, tiro...). Libera la posesión.</summary>
        public void Kick(FootballPlayer kicker, Vector3 velocity, KickType type, FootballPlayer intendedReceiver)
        {
            velocity = Vector3.ClampMagnitude(velocity, settings.maxSpeed);
            SetOwner(null);
            LastToucher = kicker;
            IgnoreBriefly(kicker);

            // Despega el balón del suelo un instante en golpeos elevados para evitar que el contacto con el césped
            // se coma parte de la velocidad vertical en el primer paso de física.
            if (velocity.y > 0.5f && body.position.y < settings.radius + 0.02f)
            {
                body.position += Vector3.up * 0.02f;
            }
            body.linearVelocity = velocity;

            var kick = new KickEvent(kicker, type, intendedReceiver, velocity, Time.time);
            LastKick = kick;
            Kicked?.Invoke(kick);
        }

        /// <summary>Rebote involuntario en un jugador (balón demasiado rápido para controlarlo).</summary>
        public void Deflect(FootballPlayer by, Vector3 newVelocity)
        {
            LastToucher = by;
            IgnoreBriefly(by);
            body.linearVelocity = Vector3.ClampMagnitude(newVelocity, settings.maxSpeed);
            Kicked?.Invoke(new KickEvent(by, KickType.Deflection, null, newVelocity, Time.time));
        }

        /// <summary>Coloca el balón parado en un punto (saques, reinicios).</summary>
        public void ResetTo(Vector3 position)
        {
            SetOwner(null);
            LastKick = null;
            ignoredPlayer = null;
            position.y = settings.radius;
            body.linearVelocity = Vector3.zero;
            body.position = position;
            transform.position = position;
        }

        /// <summary>Pierde una fracción de la velocidad (red de la portería).</summary>
        public void ApplyDamping(float fraction)
        {
            body.linearVelocity *= Mathf.Clamp01(1f - fraction);
        }

        private void IgnoreBriefly(FootballPlayer player)
        {
            ignoredPlayer = player;
            ignoreUntil = Time.time + settings.kickerIgnoreTime;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (Owner != null) UpdateDribble(dt);
            else ApplyRollingResistance(dt);

            if (body.linearVelocity.sqrMagnitude > settings.maxSpeed * settings.maxSpeed)
            {
                body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, settings.maxSpeed);
            }
        }

        /// <summary>
        /// Conducción: el balón persigue un punto delante del pie con un "muelle" amortiguado.
        /// Al correr rápido el punto se aleja y el muelle se ablanda: el balón va más suelto y se retrasa en los giros,
        /// que es lo que hace que esprintar con balón tenga riesgo.
        /// </summary>
        private void UpdateDribble(float dt)
        {
            PlayerMotor motor = Owner.Motor;
            Vector3 position = body.position;

            if (KickMath.FlatDistance(Owner.Position, position) > settings.maxDribbleDistance)
            {
                SetOwner(null);
                return;
            }

            float speedFactor = Mathf.InverseLerp(motor.JogSpeed, motor.SprintSpeed, motor.Speed);
            float offset = motor.BodyRadius + settings.radius +
                           Mathf.Lerp(settings.dribbleDistanceJog, settings.dribbleDistanceSprint, speedFactor);
            Vector3 target = Owner.Position + Owner.Forward * offset;

            Vector3 toTarget = KickMath.Flat(target - position);
            float stiffness = Mathf.Lerp(settings.dribbleStiffnessJog, settings.dribbleStiffnessSprint, speedFactor);
            // stiffness·dt < 1 garantiza que el muelle no oscile; lo limitamos por si alguien cambia el paso de física.
            stiffness = Mathf.Min(stiffness, 0.9f / dt);

            Vector3 desired = motor.Velocity + toTarget * stiffness;
            desired = Vector3.ClampMagnitude(desired, motor.SprintSpeed * 1.6f);

            Vector3 current = body.linearVelocity;
            body.linearVelocity = new Vector3(desired.x, Mathf.Min(current.y, 0f), desired.z);
        }

        private void ApplyRollingResistance(float dt)
        {
            if (!IsGrounded) return;
            Vector3 v = body.linearVelocity;
            Vector3 horizontal = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), Vector3.zero, settings.rollingDeceleration * dt);
            body.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }
    }
}
