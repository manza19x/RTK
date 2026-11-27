using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Duración y ritmos del partido (cronómetro, celebraciones, saques).</summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes del partido", fileName = "MatchSettings")]
    public class MatchSettings : ScriptableObject
    {
        [Header("Duración")]
        [Tooltip("Segundos reales que dura cada parte.")]
        public float halfDurationSeconds = 150f;
        [Tooltip("Minutos que muestra el marcador por cada parte (45 = reloj de partido real).")]
        public int displayedHalfMinutes = 45;

        [Header("Pausas del juego (segundos)")]
        public float goalCelebrationSeconds = 3f;
        public float halfTimeSeconds = 3f;
        [Tooltip("Tiempo que espera el equipo de la IA antes de sacar.")]
        public float aiRestartDelay = 1.2f;
        [Tooltip("Duración de los mensajes breves en pantalla.")]
        public float messageSeconds = 2.5f;

        [Header("Saques")]
        [Tooltip("Distancia mínima de los rivales al balón en saques de banda, de puerta y córners.")]
        public float restartClearance = 6f;
    }
}
