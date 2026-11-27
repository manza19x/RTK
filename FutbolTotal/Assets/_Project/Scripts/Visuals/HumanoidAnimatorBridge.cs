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

        private FootballPlayer player;
        private Animator animator;
        private Ball ball;
        private MatchManager manager;
        private bool wasLunging;

        private void Awake()
        {
            player = GetComponent<FootballPlayer>();
            animator = GetComponentInChildren<Animator>();
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
            animator.SetFloat(SpeedHash, motor.Speed / Mathf.Max(0.1f, motor.SprintSpeed), speedDamping, Time.deltaTime);

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
