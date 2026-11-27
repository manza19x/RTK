using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Volumen relativo de cada tipo de sonido del partido (el volumen general está en Opciones).</summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de sonido", fileName = "SoundSettings")]
    public class SoundSettings : ScriptableObject
    {
        [Range(0f, 1f)] public float kick = 0.8f;
        [Range(0f, 1f)] public float bounce = 0.5f;
        [Range(0f, 1f)] public float post = 0.8f;
        [Range(0f, 1f)] public float net = 0.6f;
        [Range(0f, 1f)] public float whistle = 0.55f;
        [Tooltip("Murmullo del público de fondo.")]
        [Range(0f, 1f)] public float crowd = 0.35f;
        [Tooltip("Cuánto sube el público cuando el balón se acerca a una portería.")]
        [Range(0f, 1f)] public float crowdExcitement = 0.35f;
        [Range(0f, 1f)] public float cheer = 0.8f;
        [Range(0f, 1f)] public float uiClick = 0.4f;
        [Tooltip("Velocidad del balón (m/s) que corresponde al volumen máximo de un golpeo.")]
        public float kickSpeedForFullVolume = 25f;
    }
}
