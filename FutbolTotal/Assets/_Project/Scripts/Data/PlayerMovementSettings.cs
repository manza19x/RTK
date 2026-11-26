using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Parámetros de movimiento y resistencia de los futbolistas. Un único asset compartido por
    /// todos los jugadores; en el futuro se puede tener uno por perfil (rápido, fuerte...).
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de movimiento", fileName = "PlayerMovementSettings")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("Velocidad (m/s)")]
        [Tooltip("Velocidad máxima corriendo sin sprint.")]
        public float jogSpeed = 5.6f;
        [Tooltip("Velocidad máxima con sprint.")]
        public float sprintSpeed = 8.2f;
        [Tooltip("Multiplicador de velocidad mientras se conduce el balón.")]
        [Range(0.5f, 1f)] public float withBallSpeedMultiplier = 0.92f;
        [Tooltip("Multiplicador de velocidad justo después de pasar o tirar.")]
        [Range(0.1f, 1f)] public float actionSlowdownMultiplier = 0.45f;

        [Header("Aceleración (m/s²)")]
        [Tooltip("Aceleración al ganar velocidad corriendo.")]
        public float acceleration = 20f;
        [Tooltip("Aceleración al ganar velocidad con sprint (más lenta: el sprint tarda en arrancar).")]
        public float sprintAcceleration = 11f;
        [Tooltip("Frenada. También elimina la velocidad lateral al cambiar de dirección: un valor alto evita deslizamientos.")]
        public float deceleration = 30f;

        [Header("Giro (grados/s)")]
        public float turnRate = 760f;
        [Tooltip("Giro con sprint: más lento para que correr rápido tenga un coste en control.")]
        public float sprintTurnRate = 400f;
        [Tooltip("Multiplicador del giro cuando el jugador lleva el balón.")]
        [Range(0.3f, 1f)] public float withBallTurnMultiplier = 0.85f;

        [Header("Cuerpo")]
        [Tooltip("Radio del cuerpo (debe coincidir con el CapsuleCollider). Se usa para colocar el balón al conducir.")]
        public float bodyRadius = 0.35f;

        [Header("Resistencia (0..1)")]
        [Tooltip("Resistencia que se gasta por segundo de sprint.")]
        public float sprintDrainPerSecond = 0.16f;
        [Tooltip("Resistencia recuperada por segundo sin sprint.")]
        public float staminaRegenPerSecond = 0.09f;
        [Tooltip("Segundos sin sprint antes de empezar a recuperar.")]
        public float staminaRegenDelay = 0.7f;
        [Tooltip("Si la resistencia llega a 0, no se puede volver a esprintar hasta recuperar este valor.")]
        [Range(0f, 1f)] public float staminaRecoverThreshold = 0.3f;
    }
}
