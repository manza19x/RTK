using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Conecta un modelo humanoide con Animator (por ejemplo, de Mixamo) con el estado del futbolista:
    /// velocidad → mezcla quieto/trote/sprint; pase y tiro → animación de golpeo; entrada; estirada del portero
    /// (en espejo según el lado); celebración al marcar su equipo.
    /// Solo es visual: el movimiento lo sigue haciendo <see cref="PlayerMotor"/> (sin root motion).
    /// </summary>
    [RequireComponent(typeof(FootballPlayer))]
    public class HumanoidAnimatorBridge : MonoBehaviour
    {
        public const string SpeedParam = "Speed";
        public const string PassTrigger = "Pass";
        public const string ShotTrigger = "Shot";
        public const string TackleTrigger = "Tackle";
        public const string DiveTrigger = "Dive";
        public const string CelebrateTrigger = "Celebrate";
        public const string MirrorDiveParam = "MirrorDive";
        public const string LocomotionSpeedParam = "LocoSpeed";

        [Tooltip("Ver CharacterSettings.diveClipGoesRight.")]
        [SerializeField] private bool diveClipGoesRight = true;
        [SerializeField] private float speedDamping = 0.12f;

        private static readonly int SpeedHash = Animator.StringToHash(SpeedParam);
        private static readonly int PassHash = Animator.StringToHash(PassTrigger);
        private static readonly int ShotHash = Animator.StringToHash(ShotTrigger);
        private static readonly int TackleHash = Animator.StringToHash(TackleTrigger);
        private static readonly int DiveHash = Animator.StringToHash(DiveTrigger);
        private static readonly int CelebrateHash = Animator.StringToHash(CelebrateTrigger);
        private static readonly int MirrorHash = Animator.StringToHash(MirrorDiveParam);
        private static readonly int LocoSpeedHash = Animator.StringToHash(LocomotionSpeedParam);

        private CharacterSettings character;

        private FootballPlayer player;
        private Animator animator;
        private Ball ball;
        private MatchManager manager;
        private bool wasLunging;

        private void Awake()
        {
            player = GetComponent<FootballPlayer>();
            animator = GetComponentInChildren<Animator>();
            character = SettingsProvider.Load<CharacterSettings>(SettingsProvider.CharacterName);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void Start()
        {
            var context = MatchContext.Current;
            if (context == null || animator == null) return;
            ball = context.Ball;
            ball.Kicked += OnKicked;
            manager = context.Rules as MatchManager;
            if (manager != null) manager.GoalScored += OnGoal;
        }

        private void OnDestroy()
        {
            if (ball != null) ball.Kicked -= OnKicked;
            if (manager != null) manager.GoalScored -= OnGoal;
        }

        private void Update()
        {
            if (animator == null) return;
            PlayerMotor motor = player.Motor;
            float speed01 = motor.Speed / Mathf.Max(0.1f, motor.SprintSpeed);
            animator.SetFloat(SpeedHash, speed01, speedDamping, Time.deltaTime);
            animator.SetFloat(LocoSpeedHash, LocomotionPlayback(motor.Speed, speed01), speedDamping, Time.deltaTime);

            bool lunging = motor.IsLunging;
            if (lunging && !wasLunging)
            {
                if (player.IsGoalkeeper)
                {
                    bool goesRight = Vector3.Dot(motor.LungeDirection, transform.right) >= 0f;
                    animator.SetBool(MirrorHash, goesRight != diveClipGoesRight);
                    animator.SetTrigger(DiveHash);
                }
                else
                {
                    animator.SetTrigger(TackleHash);
                }
            }
            wasLunging = lunging;
        }

        /// <summary>
        /// Multiplicador de reproducción de la locomoción: velocidad real / velocidad a la que "corre" la animación
        /// mezclada. Así la zancada coincide con el desplazamiento y los pies no patinan.
        /// </summary>
        private float LocomotionPlayback(float speed, float speed01)
        {
            if (character == null || speed < 0.5f) return 1f;
            float jogPoint = character.jogBlendPoint;
            float animationSpeed = speed01 <= jogPoint
                ? character.jogAnimationSpeed * Mathf.Max(0.35f, speed01 / jogPoint)
                : Mathf.Lerp(character.jogAnimationSpeed, character.sprintAnimationSpeed, (speed01 - jogPoint) / (1f - jogPoint));
            return Mathf.Clamp(speed / Mathf.Max(0.1f, animationSpeed), character.playbackRange.x, character.playbackRange.y);
        }

        private void OnKicked(KickEvent kick)
        {
            if (kick.Kicker != player) return;
            if (kick.Type == KickType.Shot) animator.SetTrigger(ShotHash);
            else if (kick.Type == KickType.ShortPass || kick.Type == KickType.LongPass) animator.SetTrigger(PassHash);
        }

        private void OnGoal(Team scorer)
        {
            if (scorer == player.Team && !player.IsGoalkeeper) animator.SetTrigger(CelebrateHash);
        }
    }
}
