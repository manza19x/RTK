using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Encuadre de la cámara de retransmisión (lateral, elevada).</summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de cámara", fileName = "CameraSettings")]
    public class CameraSettings : ScriptableObject
    {
        [Tooltip("Altura de la cámara sobre el césped.")]
        public float height = 21f;
        [Tooltip("Distancia hacia la banda (eje -Z) desde el punto de interés.")]
        public float distance = 30f;
        [Range(15f, 70f)] public float fieldOfView = 40f;
        [Tooltip("Segundos que se adelanta el encuadre según la velocidad del balón.")]
        public float ballLookAhead = 0.35f;
        public float maxLookAheadDistance = 6f;
        [Tooltip("Peso del jugador activo en el encuadre (0 = solo balón).")]
        [Range(0f, 1f)] public float activePlayerWeight = 0.25f;
        [Tooltip("Cuánto sigue la cámara el balón a lo ancho del campo (0 = fija en el centro).")]
        [Range(0f, 1f)] public float followAcrossFactor = 0.55f;
        [Tooltip("Margen respecto a las líneas de fondo para no enseñar demasiado fuera del campo.")]
        public float endLineMargin = 10f;
        public float smoothTime = 0.3f;
    }
}
