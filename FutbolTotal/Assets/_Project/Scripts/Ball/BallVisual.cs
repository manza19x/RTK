using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Hace girar el modelo del balón según su desplazamiento (rueda sin deslizar).
    /// El Rigidbody tiene la rotación congelada para que la física sea estable; esto es solo visual.
    /// </summary>
    public class BallVisual : MonoBehaviour
    {
        [Tooltip("Hijo con la malla del balón.")]
        [SerializeField] private Transform visual;
        [SerializeField] private Ball ball;

        private Vector3 lastPosition;

        private void Start()
        {
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (visual == null || ball == null) return;
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;

            float distance = delta.magnitude;
            if (distance < 1e-4f) return;

            Vector3 axis = Vector3.Cross(Vector3.up, delta / distance);
            float angle = distance / Mathf.Max(0.01f, ball.Radius) * Mathf.Rad2Deg;
            visual.Rotate(axis, angle, Space.World);
        }
    }
}
