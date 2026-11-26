using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Parámetros de las acciones con balón: pases, tiros y entradas, incluida la asistencia de apuntado.
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de acciones", fileName = "ActionSettings")]
    public class ActionSettings : ScriptableObject
    {
        [Header("Pase corto (raso)")]
        public float shortPassMinDistance = 3f;
        public float shortPassMaxDistance = 32f;
        [Tooltip("Distancia preferida con un toque rápido (carga 0).")]
        public float shortPassPreferredMin = 8f;
        [Tooltip("Distancia preferida con el botón cargado (carga 1).")]
        public float shortPassPreferredMax = 24f;
        [Tooltip("Ángulo máximo entre la dirección apuntada y el compañero para considerarlo.")]
        public float shortPassConeDegrees = 40f;
        [Tooltip("Velocidad con la que el balón llega al receptor (m/s).")]
        public float shortPassArrivalSpeed = 8f;
        public float shortPassMinSpeed = 8f;
        public float shortPassMaxSpeed = 22f;
        [Tooltip("Pase al espacio (sin receptor): distancia con carga 0 y 1.")]
        public float spacePassMinDistance = 6f;
        public float spacePassMaxDistance = 22f;

        [Header("Pase largo / centro (por alto)")]
        public float longPassMinDistance = 10f;
        public float longPassMaxDistance = 50f;
        public float longPassPreferredMin = 18f;
        public float longPassPreferredMax = 42f;
        public float longPassConeDegrees = 35f;
        [Tooltip("Tiempo de vuelo = base + distancia × porMetro (limitado por el máximo).")]
        public float longPassFlightTimeBase = 0.7f;
        public float longPassFlightTimePerMeter = 0.03f;
        public float longPassMaxFlightTime = 2.2f;
        [Tooltip("El balón cae esta distancia por delante del receptor, para que bote hacia él.")]
        public float longPassLandShort = 1.5f;
        public float spaceLongPassMinDistance = 15f;
        public float spaceLongPassMaxDistance = 45f;

        [Header("Asistencia de pase")]
        [Tooltip("0 = el pase sale exactamente donde apunta el jugador; 1 = sale exacto al receptor elegido.")]
        [Range(0f, 1f)] public float passAssist = 0.85f;
        [Tooltip("Cuánto se adelanta el pase según la velocidad del receptor (1 = anticipación completa).")]
        [Range(0f, 1.5f)] public float leadFactor = 1f;
        [Tooltip("Distancia de un rival a la línea de pase a partir de la cual se considera libre.")]
        public float laneSafeDistance = 3f;
        [Tooltip("Distancia de un rival al receptor de un pase alto a partir de la cual se considera libre.")]
        public float receiverSafeDistance = 4f;
        [Range(0f, 1f)] public float weightAngle = 0.5f;
        [Range(0f, 1f)] public float weightDistance = 0.2f;
        [Range(0f, 1f)] public float weightLane = 0.3f;

        [Header("Tiro")]
        public float shotMinSpeed = 14f;
        public float shotMaxSpeed = 30f;
        [Tooltip("Altura del objetivo en la portería con potencia mínima y máxima.")]
        public float shotMinHeight = 0.3f;
        public float shotMaxHeight = 1.5f;
        [Tooltip("Por encima de esta carga el tiro sube de más (riesgo de mandarlo arriba).")]
        [Range(0.5f, 1f)] public float overPowerThreshold = 0.88f;
        public float overPowerExtraHeight = 1.4f;
        [Tooltip("Margen respecto a los postes al apuntar.")]
        public float shotPostMargin = 0.4f;
        [Tooltip("Inclinación del stick hacia arriba/abajo que equivale a apuntar al palo.")]
        [Range(0.2f, 1f)] public float shotStickFullDeflection = 0.7f;

        [Header("Precisión del tiro (grados de error máximo)")]
        public float shotBaseErrorDegrees = 1f;
        public float shotErrorPerMeter = 0.1f;
        [Tooltip("Radio en el que un rival cercano molesta al tirador.")]
        public float pressureRadius = 3f;
        public float pressureErrorDegrees = 5f;
        [Tooltip("Error extra al tirar de espaldas o perfilado (90° o más respecto al tiro).")]
        public float bodyAngleErrorDegrees = 6f;
        public float sprintErrorDegrees = 2f;

        [Header("Entrada (robo)")]
        [Tooltip("Distancia al balón a la que la entrada lo gana.")]
        public float tackleReach = 1.5f;
        public float tackleLungeSpeed = 7.5f;
        public float tackleDuration = 0.25f;
        public float tackleCooldown = 0.8f;
        [Tooltip("Aturdimiento del jugador que pierde el balón.")]
        public float tackleVictimStun = 0.6f;
        [Tooltip("Aturdimiento del que falla la entrada (castiga pulsar sin pensar).")]
        public float tackleMissStun = 0.45f;

        [Header("General")]
        [Tooltip("Ralentización tras golpear el balón (segundos).")]
        public float kickRecoverySeconds = 0.2f;
        [Tooltip("Si se pulsa pase/tiro sin balón, la orden se guarda este tiempo para golpear al primer toque.")]
        public float firstTimeBufferSeconds = 0.35f;
    }
}
