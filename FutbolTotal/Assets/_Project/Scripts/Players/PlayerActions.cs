using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Acciones con balón de un futbolista: pase corto, pase largo, tiro y entrada.
    /// Recibe "qué quiere hacer" (dirección apuntada + carga 0..1) y calcula la velocidad del balón con
    /// <see cref="PassTargetSelector"/>, <see cref="ShotCalculator"/> y <see cref="KickMath"/>.
    /// La misma API la usará la IA, así humano e IA juegan con las mismas reglas.
    /// </summary>
    // Sin [RequireComponent(typeof(FootballPlayer))]: FootballPlayer ya exige este componente y una
    // dependencia circular hace que Unity cree componentes duplicados al añadirlos desde código.
    [DisallowMultipleComponent]
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private ActionSettings settings;

        private FootballPlayer player;
        private float tackleReadyAt;
        private bool tackleActive;
        private bool tackleResolved;

        public ActionSettings Settings => settings;
        public bool IsTackling => tackleActive;
        public bool CanTackle => Time.time >= tackleReadyAt && !player.HasBall && !player.Motor.IsStunned;

        private void Awake()
        {
            player = GetComponent<FootballPlayer>();
            settings = SettingsProvider.Resolve(settings, SettingsProvider.ActionsName, this);
        }

        // ---------------------------------------------------------------- Pases

        /// <summary>Pase raso. Con receptor válido en la dirección apuntada se adelanta a su carrera; si no, pase al espacio.</summary>
        public bool ShortPass(Vector3 aimDirection, float charge01, out FootballPlayer receiver)
        {
            receiver = null;
            if (!CanKick(out Ball ball)) return false;

            Vector3 aim = NormalizeAim(aimDirection);
            receiver = PassTargetSelector.FindTarget(player, aim, PassKind.Ground, charge01, settings);

            Vector3 from = ball.Position;
            float decel = ball.Settings.rollingDeceleration;
            Vector3 direction;
            float speed;

            if (receiver != null)
            {
                Vector3 receivePoint = PredictGroundReception(from, receiver, decel);
                Vector3 toReceiver = KickMath.Flat(receivePoint - from);
                direction = BlendAim(aim, toReceiver.normalized);
                speed = KickMath.GroundPassSpeed(toReceiver.magnitude, settings.shortPassArrivalSpeed, decel);
            }
            else
            {
                direction = aim;
                float distance = Mathf.Lerp(settings.spacePassMinDistance, settings.spacePassMaxDistance, charge01);
                speed = KickMath.GroundPassSpeed(distance, settings.shortPassArrivalSpeed * 0.5f, decel);
            }

            speed = Mathf.Clamp(speed, settings.shortPassMinSpeed, settings.shortPassMaxSpeed);
            ExecuteKick(ball, direction * speed, KickType.ShortPass, receiver);
            return true;
        }

        /// <summary>Pase por alto / centro. Cae un poco por delante del receptor para que el bote vaya hacia él.</summary>
        public bool LongPass(Vector3 aimDirection, float charge01, out FootballPlayer receiver)
        {
            receiver = null;
            if (!CanKick(out Ball ball)) return false;

            Vector3 aim = NormalizeAim(aimDirection);
            receiver = PassTargetSelector.FindTarget(player, aim, PassKind.Aerial, charge01, settings);

            Vector3 from = ball.Position;
            Vector3 landing;
            float flightTime;

            if (receiver != null)
            {
                // Dos iteraciones bastan: el tiempo de vuelo depende de la distancia y la distancia de la carrera del receptor.
                landing = receiver.Position;
                flightTime = LongPassFlightTime(KickMath.FlatDistance(from, landing));
                for (int i = 0; i < 2; i++)
                {
                    landing = receiver.Position + receiver.Motor.Velocity * (flightTime * settings.leadFactor);
                    flightTime = LongPassFlightTime(KickMath.FlatDistance(from, landing));
                }

                Vector3 toLanding = KickMath.Flat(landing - from);
                float distance = Mathf.Max(0f, toLanding.magnitude - settings.longPassLandShort);
                Vector3 direction = BlendAim(aim, toLanding.normalized);
                landing = from + direction * distance;
                flightTime = LongPassFlightTime(distance);
            }
            else
            {
                float distance = Mathf.Lerp(settings.spaceLongPassMinDistance, settings.spaceLongPassMaxDistance, charge01);
                landing = from + aim * distance;
                flightTime = LongPassFlightTime(distance);
            }

            landing.y = ball.Radius;
            Vector3 velocity = KickMath.VelocityForFlightTime(from, landing, flightTime);
            ExecuteKick(ball, velocity, KickType.LongPass, receiver);
            return true;
        }

        // ---------------------------------------------------------------- Tiro

        /// <param name="hasDirectionalInput">True si el jugador está inclinando el stick/teclas: entonces apunta con ellas.</param>
        public bool Shoot(Vector3 aimDirection, float charge01, bool hasDirectionalInput)
        {
            if (!CanKick(out Ball ball)) return false;

            var context = MatchContext.Current;
            ShotPlan plan = ShotCalculator.Plan(player, NormalizeAim(aimDirection), charge01, hasDirectionalInput,
                                                settings, ball, context.Pitch);
            ExecuteKick(ball, plan.Velocity, KickType.Shot, null);
            return true;
        }

        // ---------------------------------------------------------------- Entrada

        /// <summary>
        /// Entrada: impulso corto hacia el balón. Si durante el impulso el balón (no el rival) queda a su alcance, lo roba.
        /// Si falla, el jugador queda aturdido un momento: entrar sin pensar tiene un coste visible.
        /// </summary>
        public bool TryTackle()
        {
            if (!CanTackle) return false;
            var context = MatchContext.Current;
            if (context == null) return false;

            tackleReadyAt = Time.time + settings.tackleCooldown;
            tackleActive = true;
            tackleResolved = false;

            Vector3 toBall = KickMath.Flat(context.Ball.Position - player.Position);
            Vector3 direction = toBall.sqrMagnitude > 0.01f ? toBall.normalized : player.Forward;
            player.Motor.StartLunge(direction, settings.tackleLungeSpeed, settings.tackleDuration);
            return true;
        }

        private void FixedUpdate()
        {
            if (!tackleActive) return;
            var context = MatchContext.Current;
            if (context == null) return;
            Ball ball = context.Ball;

            if (!tackleResolved)
            {
                FootballPlayer owner = ball.Owner;
                bool rivalHasBall = owner != null && owner.Team != player.Team;
                if (rivalHasBall && KickMath.FlatDistance(player.Position, ball.Position) <= settings.tackleReach)
                {
                    owner.Motor.Stun(settings.tackleVictimStun);
                    ball.SetOwner(player);
                    tackleResolved = true;
                }
                else if (player.HasBall)
                {
                    tackleResolved = true; // Balón suelto recogido durante la entrada.
                }
            }

            if (!player.Motor.IsLunging)
            {
                tackleActive = false;
                if (!tackleResolved) player.Motor.Stun(settings.tackleMissStun);
            }
        }

        // ---------------------------------------------------------------- Utilidades

        private bool CanKick(out Ball ball)
        {
            var context = MatchContext.Current;
            ball = context != null ? context.Ball : null;
            return ball != null && ball.Owner == player && !player.Motor.IsStunned;
        }

        private void ExecuteKick(Ball ball, Vector3 velocity, KickType type, FootballPlayer receiver)
        {
            ball.Kick(player, velocity, type, receiver);
            player.Motor.ApplyActionSlowdown(settings.kickRecoverySeconds);
        }

        private Vector3 NormalizeAim(Vector3 aim)
        {
            aim = KickMath.Flat(aim);
            return aim.sqrMagnitude > 1e-4f ? aim.normalized : player.Forward;
        }

        /// <summary>Asistencia: mezcla la dirección del jugador con la dirección ideal según <see cref="ActionSettings.passAssist"/>.</summary>
        private Vector3 BlendAim(Vector3 playerAim, Vector3 idealDirection)
        {
            Vector3 blended = Vector3.Slerp(playerAim, idealDirection, settings.passAssist);
            blended.y = 0f;
            return blended.sqrMagnitude > 1e-4f ? blended.normalized : idealDirection;
        }

        /// <summary>Punto donde el receptor estará cuando llegue el pase raso (anticipación según su velocidad).</summary>
        private Vector3 PredictGroundReception(Vector3 from, FootballPlayer receiver, float decel)
        {
            Vector3 point = receiver.Position;
            for (int i = 0; i < 3; i++)
            {
                float distance = KickMath.FlatDistance(from, point);
                float speed = Mathf.Clamp(KickMath.GroundPassSpeed(distance, settings.shortPassArrivalSpeed, decel),
                                          settings.shortPassMinSpeed, settings.shortPassMaxSpeed);
                float time = KickMath.GroundTravelTime(distance, speed, decel);
                point = receiver.Position + receiver.Motor.Velocity * (time * settings.leadFactor);
            }
            var context = MatchContext.Current;
            return context != null ? context.Pitch.ClampToPlayingArea(point, 0.5f) : point;
        }

        private float LongPassFlightTime(float distance)
        {
            return Mathf.Min(settings.longPassMaxFlightTime,
                             settings.longPassFlightTimeBase + distance * settings.longPassFlightTimePerMeter);
        }
    }
}
