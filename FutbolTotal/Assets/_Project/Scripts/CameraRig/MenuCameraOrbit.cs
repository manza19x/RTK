using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Cámara del menú principal: gira despacio alrededor del campo.</summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float radius = 50f;
        [SerializeField] private float height = 24f;
        [Tooltip("Grados por segundo.")]
        [SerializeField] private float speed = 4f;

        private float angle = 20f;

        private void LateUpdate()
        {
            angle += speed * Time.unscaledDeltaTime;
            float radians = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Sin(radians) * radius, height, -Mathf.Cos(radians) * radius);
            transform.LookAt(center);
        }
    }
}
