using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Animación por código del cuerpo articulado (<see cref="PlayerRigBuilder"/>):
    /// - carrera: zancada y braceo proporcionales a la velocidad, con inclinación al esprintar;
    /// - golpeo: pierna atrás y adelante al pasar o tirar;
    /// - entrada: deslizamiento con las piernas por delante; estirada del portero hacia el lado del balón;
    /// - aturdido: tambaleo; celebración: brazos arriba y saltos cuando marca su equipo.
    /// Solo cambia rotaciones visuales: no afecta a la física ni a las reglas.
    /// </summary>
    [RequireComponent(typeof(FootballPlayer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Tooltip("Metros por zancada completa a velocidad de sprint.")]
        [SerializeField] private float strideLength = 2.4f;
        [SerializeField] private float blendSpeed = 14f;

        private FootballPlayer player;
        private Transform visual, hips, spine, head;
        private readonly Transform[] shoulders = new Transform[2];
        private readonly Transform[] elbows = new Transform[2];
        private readonly Transform[] hipJoints = new Transform[2];
        private readonly Transform[] knees = new Transform[2];
        private Vector3 hipsRest;
        private bool ready;

        private float phase;
        private float kickStarted = -10f;
        private float celebrateUntil;
        private MatchManager manager;
        private Ball ball;

        private const float KickDuration = 0.32f;

        private void Awake()
        {
            player = GetComponent<FootballPlayer>();
            visual = transform.Find("Visual");
            if (visual == null) return;
            hips = FindDeep(visual, PlayerRigBones.Hips);
            spine = FindDeep(visual, PlayerRigBones.Spine);
            head = FindDeep(visual, PlayerRigBones.Head);
            string[] sides = { "L", "R" };
            for (int i = 0; i < 2; i++)
            {
                shoulders[i] = FindDeep(visual, PlayerRigBones.Shoulder + sides[i]);
                elbows[i] = FindDeep(visual, PlayerRigBones.Elbow + sides[i]);
                hipJoints[i] = FindDeep(visual, PlayerRigBones.Hip + sides[i]);
                knees[i] = FindDeep(visual, PlayerRigBones.Knee + sides[i]);
            }
            ready = hips != null && spine != null && shoulders[0] != null && hipJoints[0] != null && knees[1] != null;
            if (ready) hipsRest = hips.localPosition;
        }

        private void Start()
        {
            var context = MatchContext.Current;
            if (context == null) return;
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

        private void OnKicked(KickEvent kick)
        {
            if (kick.Kicker == player && kick.Type != KickType.Deflection) kickStarted = Time.time;
        }

        private void OnGoal(Team scorer)
        {
            if (scorer == player.Team && !player.IsGoalkeeper) celebrateUntil = Time.time + 2.6f;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (!ready) return;
            PlayerMotor motor = player.Motor;
            float speed = motor.Speed;
            float amount = Mathf.Clamp01(speed / Mathf.Max(0.1f, motor.SprintSpeed));

            // Frecuencia de zancada proporcional a la velocidad (con un mínimo para que el trote no parezca a cámara lenta).
            float stride = Mathf.Lerp(strideLength * 0.55f, strideLength, amount);
            phase += speed / stride * Mathf.PI * 2f * Time.deltaTime;
            float s = Mathf.Sin(phase);
            float c = Mathf.Cos(phase);

            // Pose base: carrera.
            float legSwing = Mathf.Lerp(8f, 42f, amount) * Mathf.Min(1f, speed / 1.2f);
            float[] leg = { legSwing * s, -legSwing * s };
            float[] knee = { KneeBend(phase, amount, speed), KneeBend(phase + Mathf.PI, amount, speed) };
            float[] arm = { -leg[0] * 0.8f, -leg[1] * 0.8f };
            float[] elbow = { 55f + 25f * amount, 55f + 25f * amount };
            float[] armOut = { 6f, 6f };
            float lean = 3f + 12f * amount;
            float bob = Mathf.Abs(c) * 0.045f * amount - 0.02f * amount;
            Vector3 visualEuler = Vector3.zero;

            // Golpeo (pierna derecha).
            float kickT = (Time.time - kickStarted) / KickDuration;
            if (kickT >= 0f && kickT <= 1f)
            {
                float back = Mathf.Clamp01(kickT / 0.35f);
                float through = Mathf.Clamp01((kickT - 0.35f) / 0.65f);
                leg[1] = Mathf.Lerp(-40f * back, 75f, through);
                knee[1] = Mathf.Lerp(80f * back, 8f, through);
                leg[0] = -10f;
                knee[0] = 15f;
                armOut[0] = armOut[1] = 35f;
                arm[0] = 25f;
                arm[1] = -20f;
                lean = 8f - 14f * through;
            }

            // Entrada o estirada.
            if (motor.IsLunging)
            {
                Vector3 direction = motor.LungeDirection;
                if (player.IsGoalkeeper)
                {
                    float side = Vector3.Dot(direction, transform.right) >= 0f ? 1f : -1f;
                    visualEuler = new Vector3(0f, 0f, -side * 70f);
                    arm[0] = arm[1] = -165f;
                    elbow[0] = elbow[1] = 5f;
                    leg[0] = leg[1] = 5f;
                    knee[0] = knee[1] = 10f;
                    bob = 0.25f;
                }
                else
                {
                    visualEuler = new Vector3(-35f, 0f, 0f);
                    leg[0] = 70f;
                    leg[1] = 55f;
                    knee[0] = 5f;
                    knee[1] = 40f;
                    arm[0] = -40f;
                    arm[1] = 30f;
                    bob = -0.35f;
                }
            }
            else if (motor.IsStunned)
            {
                visualEuler = new Vector3(Mathf.Sin(Time.time * 18f) * 6f, 0f, Mathf.Sin(Time.time * 11f) * 8f);
            }

            // Celebración.
            if (Time.time < celebrateUntil && speed < 3f)
            {
                arm[0] = arm[1] = -160f;
                armOut[0] = armOut[1] = 25f;
                elbow[0] = elbow[1] = 15f;
                bob = Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.18f;
            }

            // Respiración en reposo.
            if (speed < 0.3f) lean = 2f + Mathf.Sin(Time.time * 2.2f) * 1.2f;

            float k = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
            for (int i = 0; i < 2; i++)
            {
                // Rotar en Z positivo lleva el brazo hacia +X: hacia fuera para el derecho, hacia dentro para el izquierdo.
                float outward = i == 0 ? -armOut[i] : armOut[i];
                Blend(shoulders[i], Quaternion.Euler(-arm[i], 0f, outward), k);
                Blend(elbows[i], Quaternion.Euler(-elbow[i], 0f, 0f), k);
                Blend(hipJoints[i], Quaternion.Euler(-leg[i], 0f, 0f), k);
                Blend(knees[i], Quaternion.Euler(knee[i], 0f, 0f), k);
            }
            Blend(spine, Quaternion.Euler(lean, 0f, 0f), k);
            if (head != null) Blend(head, Quaternion.Euler(-lean * 0.5f, 0f, 0f), k);
            Blend(visual, Quaternion.Euler(visualEuler), k);
            hips.localPosition = Vector3.Lerp(hips.localPosition, hipsRest + Vector3.up * bob, k);
        }

        /// <summary>La rodilla se dobla sobre todo cuando la pierna vuelve hacia delante.</summary>
        private static float KneeBend(float phase, float amount, float speed)
        {
            if (speed < 0.3f) return 4f;
            float swing = Mathf.Max(0f, -Mathf.Sin(phase - 0.6f));
            return 10f + swing * Mathf.Lerp(35f, 95f, amount);
        }

        private static void Blend(Transform joint, Quaternion target, float k)
        {
            if (joint == null) return;
            joint.localRotation = Quaternion.Slerp(joint.localRotation, target, k);
        }
    }
}
