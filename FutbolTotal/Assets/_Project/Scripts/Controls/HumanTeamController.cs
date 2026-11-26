using System;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Traduce la entrada del jugador humano en órdenes para el futbolista activo de su equipo,
    /// y decide los cambios de jugador (manuales y automáticos).
    ///
    /// Reglas de los botones:
    /// - Pase corto, pase largo y tiro se cargan manteniendo y se ejecutan al soltar (una vez).
    /// - Con balón: ejecutan la acción. Sin balón: el pase corto hace una entrada si un rival lleva el balón;
    ///   en otro caso la orden se guarda un instante y se ejecuta al recibir (golpeo "al primer toque").
    /// - Cambio de jugador: solo sin balón. Al pasar a un compañero, el control pasa al receptor.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(Team))]
    public class HumanTeamController : MonoBehaviour
    {
        [SerializeField] private FootballInput input;
        [Tooltip("Cámara de referencia: arriba en el stick = hacia donde mira la cámara.")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private ActionSettings actionSettings;
        [Tooltip("Al pasar a un compañero, controlar automáticamente al receptor.")]
        [SerializeField] private bool switchToPassReceiver = true;

        private struct PendingAction
        {
            public bool Active;
            public KickType Type;
            public float Charge;
            public float ExpiresAt;
        }

        private const float StickDeadZone = 0.2f;

        private Team team;
        private Ball ball;
        private PendingAction pending;

        public Team Team => team;
        public FootballInput Input => input;
        public FootballPlayer ActivePlayer { get; private set; }
        /// <summary>Desactivar durante pausas, celebraciones o saques.</summary>
        public bool InputEnabled { get; set; } = true;

        public event Action<FootballPlayer> ActivePlayerChanged;

        private void Awake()
        {
            team = GetComponent<Team>();
            if (input == null) input = GetComponent<FootballInput>();
            if (actionSettings == null) actionSettings = ScriptableObject.CreateInstance<ActionSettings>();
        }

        private void Start()
        {
            var context = MatchContext.Current;
            if (context == null)
            {
                enabled = false;
                return;
            }
            ball = context.Ball;
            ball.OwnerChanged += OnBallOwnerChanged;
            ball.Kicked += OnBallKicked;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            SetActivePlayer(PlayerSwitchSelector.SelectBest(team, ball, null));
        }

        private void OnDestroy()
        {
            if (ball == null) return;
            ball.OwnerChanged -= OnBallOwnerChanged;
            ball.Kicked -= OnBallKicked;
        }

        private void Update()
        {
            FootballPlayer player = ActivePlayer;
            if (player == null) return;

            if (!InputEnabled || Time.timeScale <= 0f)
            {
                player.Motor.Stop();
                return;
            }

            Vector2 stick = input.ReadMove();
            bool hasDirection = stick.sqrMagnitude > StickDeadZone * StickDeadZone;
            Vector3 move = hasDirection ? CameraRelative(stick) : Vector3.zero;
            player.Motor.SetMoveInput(move, input.SprintHeld);

            if (input.SwitchPlayer.WasPressedThisFrame()) SwitchManually();

            Vector3 aim = hasDirection ? move.normalized : player.Forward;
            HandleButton(input.ShortPass, KickType.ShortPass, aim, hasDirection);
            HandleButton(input.LongPass, KickType.LongPass, aim, hasDirection);
            HandleButton(input.Shot, KickType.Shot, aim, hasDirection);
            ExecutePendingIfPossible(aim, hasDirection);
        }

        private void HandleButton(ChargeButton button, KickType type, Vector3 aim, bool hasDirection)
        {
            if (!button.ReleasedThisFrame) return;
            FootballPlayer player = ActivePlayer;

            if (player.HasBall)
            {
                Execute(type, aim, button.ReleasedCharge, hasDirection);
                return;
            }

            bool rivalHasBall = ball.Owner != null && ball.Owner.Team != team;
            if (type == KickType.ShortPass && rivalHasBall)
            {
                player.Actions.TryTackle();
                return;
            }

            pending = new PendingAction
            {
                Active = true,
                Type = type,
                Charge = button.ReleasedCharge,
                ExpiresAt = Time.time + (actionSettings != null ? actionSettings.firstTimeBufferSeconds : 0.35f)
            };
        }

        private void ExecutePendingIfPossible(Vector3 aim, bool hasDirection)
        {
            if (!pending.Active) return;
            if (Time.time > pending.ExpiresAt)
            {
                pending.Active = false;
                return;
            }
            if (!ActivePlayer.HasBall) return;

            pending.Active = false;
            Execute(pending.Type, aim, pending.Charge, hasDirection);
        }

        private void Execute(KickType type, Vector3 aim, float charge, bool hasDirection)
        {
            PlayerActions actions = ActivePlayer.Actions;
            switch (type)
            {
                case KickType.ShortPass:
                    actions.ShortPass(aim, charge, out _);
                    break;
                case KickType.LongPass:
                    actions.LongPass(aim, charge, out _);
                    break;
                case KickType.Shot:
                    actions.Shoot(aim, charge, hasDirection);
                    break;
            }
        }

        private void SwitchManually()
        {
            if (ActivePlayer.HasBall) return;
            FootballPlayer next = PlayerSwitchSelector.SelectBest(team, ball, ActivePlayer);
            SetActivePlayer(next);
        }

        private void OnBallKicked(KickEvent kick)
        {
            if (!switchToPassReceiver || kick.Kicker == null || kick.Kicker.Team != team) return;
            if (kick.IntendedReceiver != null && kick.IntendedReceiver.Team == team) SetActivePlayer(kick.IntendedReceiver);
        }

        private void OnBallOwnerChanged(FootballPlayer previous, FootballPlayer current)
        {
            // Si un compañero gana el balón, el jugador pasa a controlarlo.
            if (current != null && current.Team == team && current != ActivePlayer) SetActivePlayer(current);
        }

        public void SetActivePlayer(FootballPlayer player)
        {
            if (player == null || player == ActivePlayer) return;
            if (ActivePlayer != null)
            {
                ActivePlayer.IsHumanControlled = false;
                ActivePlayer.Motor.Stop();
            }
            ActivePlayer = player;
            player.IsHumanControlled = true;
            pending.Active = false;
            ActivePlayerChanged?.Invoke(player);
        }

        /// <summary>Cancela órdenes y cargas en curso (pausa, reinicio).</summary>
        public void CancelPendingInput()
        {
            pending.Active = false;
            input.CancelCharges();
        }

        /// <summary>Convierte el stick (x = derecha, y = arriba) en una dirección en el campo relativa a la cámara.</summary>
        private Vector3 CameraRelative(Vector2 stick)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = KickMath.Flat(cameraTransform.forward);
                if (forward.sqrMagnitude < 1e-4f) forward = KickMath.Flat(cameraTransform.up);
                forward.Normalize();
                right = Vector3.Cross(Vector3.up, forward);
            }
            return Vector3.ClampMagnitude(right * stick.x + forward * stick.y, 1f);
        }
    }
}
