using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Cámara de retransmisión: lateral y elevada, como en televisión.
    /// Sigue un punto de interés (balón adelantado según su velocidad, con algo de peso del jugador activo)
    /// manteniendo SIEMPRE el mismo ángulo: solo se desplaza, no gira. Así la dirección "arriba" del stick
    /// es estable y el jugador no se desorienta.
    /// Versión de la Fase 1; en la Fase 3 se añade zoom según la jugada y encuadres de gol/repetición.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MatchCamera : MonoBehaviour
    {
        [SerializeField] private CameraSettings settings;
        [Tooltip("Opcional: para dar peso al jugador activo en el encuadre.")]
        [SerializeField] private HumanTeamController humanController;

        private Camera cam;
        private Vector3 focus;
        private Vector3 focusVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (settings != null) cam.fieldOfView = settings.fieldOfView;
        }

        private void Start()
        {
            focus = ComputeDesiredFocus();
            ApplyTransform();
        }

        private void LateUpdate()
        {
            if (settings == null || MatchContext.Current == null) return;
            Vector3 desired = ComputeDesiredFocus();
            // unscaledDeltaTime: la cámara se asienta aunque el juego esté en pausa o a cámara lenta.
            focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity, settings.smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            ApplyTransform();
        }

        /// <summary>Recoloca la cámara al instante (tras un gol o un reinicio).</summary>
        public void SnapToTarget()
        {
            focus = ComputeDesiredFocus();
            focusVelocity = Vector3.zero;
            ApplyTransform();
        }

        private Vector3 ComputeDesiredFocus()
        {
            var context = MatchContext.Current;
            if (context == null || settings == null) return Vector3.zero;

            Ball ball = context.Ball;
            Vector3 lookAhead = Vector3.ClampMagnitude(KickMath.Flat(ball.Velocity) * settings.ballLookAhead,
                                                       settings.maxLookAheadDistance);
            Vector3 target = KickMath.Flat(ball.Position) + lookAhead;

            if (humanController != null && humanController.ActivePlayer != null)
            {
                target = Vector3.Lerp(target, humanController.ActivePlayer.Position, settings.activePlayerWeight);
            }

            Pitch pitch = context.Pitch;
            Vector3 local = target - pitch.Center;
            float maxX = Mathf.Max(0f, pitch.HalfLength - settings.endLineMargin);
            local.x = Mathf.Clamp(local.x, -maxX, maxX);
            local.z = Mathf.Clamp(local.z, -pitch.HalfWidth, pitch.HalfWidth) * settings.followAcrossFactor;
            local.y = 0f;
            return pitch.Center + local;
        }

        private void ApplyTransform()
        {
            if (settings == null) return;
            transform.position = focus + new Vector3(0f, settings.height, -settings.distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
