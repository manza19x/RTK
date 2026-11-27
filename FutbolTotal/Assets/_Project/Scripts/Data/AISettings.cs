using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Parámetros de la IA. Todas las decisiones son deterministas: los mismos valores y la misma situación
    /// producen siempre la misma decisión, así el jugador puede entender y anticipar a la IA.
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de IA", fileName = "AISettings")]
    public class AISettings : ScriptableObject
    {
        [Header("Ritmo de decisión (s)")]
        [Tooltip("Cada cuánto reparte el equipo los roles (además de al cambiar la posesión).")]
        public float thinkInterval = 0.25f;
        [Tooltip("Cada cuánto decide el jugador con balón entre tirar, pasar o conducir.")]
        public float carrierDecisionInterval = 0.35f;
        [Tooltip("Cada cuánto busca un compañero un nuevo hueco para recibir.")]
        public float supportDecisionInterval = 0.6f;
        [Tooltip("Pausa al recibir antes de la primera decisión (control del balón).")]
        public float firstTouchDelay = 0.25f;

        [Header("Movimiento")]
        [Tooltip("Distancia al objetivo a la que el jugador se considera colocado.")]
        public float arrivalTolerance = 0.8f;
        [Tooltip("Por debajo de esta distancia el jugador reduce la velocidad para no pasarse.")]
        public float slowRadius = 3f;
        [Tooltip("Solo esprinta si el objetivo está más lejos que esto.")]
        public float sprintDistance = 9f;
        [Range(0f, 1f)] public float minStaminaToSprint = 0.35f;

        [Header("Forma del equipo (unidades de formación: 0 = portería propia, 1 = rival)")]
        [Tooltip("Cuánto se desplaza todo el bloque hacia el balón a lo largo del campo.")]
        [Range(0f, 1f)] public float blockShift = 0.55f;
        [Tooltip("Cuánto se adelanta el equipo cuando tiene el balón.")]
        [Range(0f, 0.3f)] public float attackPush = 0.1f;
        [Tooltip("Cuánto se repliega el equipo cuando defiende.")]
        [Range(0f, 0.3f)] public float defendDrop = 0.06f;
        [Tooltip("Cuánto se cierra el equipo hacia el lado del balón (0 = nada, 1 = todos en la línea del balón).")]
        [Range(0f, 1f)] public float lateralCompression = 0.3f;
        [Tooltip("Al defender, los defensas se quedan al menos esto por detrás del balón.")]
        [Range(0f, 0.2f)] public float defenderGoalSideMargin = 0.04f;
        [Range(0f, 0.5f)] public float minDepth = 0.08f;
        [Range(0.5f, 1f)] public float maxDepth = 0.9f;

        [Header("Jugador con balón")]
        public float shootRange = 22f;
        [Tooltip("Por debajo de esta distancia tira aunque haya un rival en la línea de tiro.")]
        public float closeShotRange = 11f;
        [Tooltip("Ángulo mínimo (grados) con el que se ve la portería para tirar.")]
        public float minShootAngle = 12f;
        [Tooltip("Un rival a menos de esta distancia de la línea de tiro la tapa.")]
        public float shotLaneRadius = 1.2f;
        [Range(0f, 1f)] public float minShotPower = 0.35f;
        [Range(0f, 1f)] public float maxShotPower = 0.8f;
        [Tooltip("Un rival más cerca que esto se considera presión.")]
        public float pressureDistance = 3.5f;
        [Tooltip("Radio en el que el conductor esquiva rivales que tiene delante.")]
        public float avoidRadius = 4f;
        public float avoidWeight = 1.2f;

        [Header("Pases de la IA")]
        public float minPassDistance = 5f;
        public float maxPassDistance = 40f;
        [Tooltip("Valor de cada metro que el pase hace avanzar hacia la portería rival.")]
        public float passProgressWeight = 0.03f;
        [Tooltip("Penalización por metro de distancia del pase.")]
        public float passDistancePenalty = 0.008f;
        [Tooltip("Puntuación mínima para pasar sin presión.")]
        public float passScoreFree = 0.75f;
        [Tooltip("Puntuación mínima para pasar bajo presión.")]
        public float passScoreUnderPressure = 0.35f;
        [Tooltip("Sin presión, solo pasa si el pase avanza al menos estos metros.")]
        public float minProgressWhenFree = 6f;
        [Tooltip("Si la línea de pase raso está más cerrada que esto, el pase va por alto.")]
        [Range(0f, 1f)] public float aerialLaneThreshold = 0.35f;

        [Header("Apoyo y desmarque")]
        [Tooltip("Desplazamiento lateral máximo al buscar una línea de pase libre.")]
        public float supportShift = 5f;
        [Tooltip("Distancia de la carrera hacia delante al desmarcarse.")]
        public float runDepth = 7f;
        [Tooltip("Distancia mínima deseada al compañero con balón.")]
        public float minSupportDistance = 7f;

        [Header("Defensa")]
        [Tooltip("Distancia al balón a la que el presionador intenta la entrada.")]
        public float tackleRange = 1.4f;
        [Tooltip("Tiempo dentro del alcance antes de entrar: deja margen para regatear.")]
        public float tackleReactionDelay = 0.35f;
        [Tooltip("Distancia a la que se coloca el presionador entre el conductor y la portería.")]
        public float pressStandOff = 0.6f;
        [Tooltip("Distancia de contención de los compañeros del jugador humano (no hacen entradas).")]
        public float containDistance = 2.5f;
        [Tooltip("Segundos que se anticipa el presionador al movimiento del conductor.")]
        public float pressLead = 0.3f;
        [Tooltip("Un rival a menos de esta distancia de la zona de un defensor pasa a estar marcado por él.")]
        public float markRadius = 9f;
        public float markDistance = 1.5f;
        [Range(0f, 1f)] public float markWeight = 0.7f;
        [Tooltip("Los compañeros del humano solo ayudan a presionar si el humano está más lejos que esto del balón.")]
        public float humanHelpDistance = 6f;

        [Header("Portero")]
        public float keeperLineDistance = 1.5f;
        [Tooltip("Lo más que se adelanta el portero cuando el balón está lejos.")]
        public float keeperMaxAdvance = 4f;
        [Tooltip("Velocidad del balón hacia la portería a partir de la cual el portero reacciona a un tiro.")]
        public float keeperThreatSpeed = 6f;
        [Tooltip("Tiempo de reacción del portero desde el golpeo.")]
        public float keeperReaction = 0.25f;
        [Tooltip("Por encima de este desplazamiento lateral el portero se estira en lugar de dar un paso.")]
        public float keeperStepReach = 0.7f;
        public float diveSpeed = 6f;
        public float diveDuration = 0.35f;
        [Tooltip("Segundos que el portero retiene el balón antes de sacarlo.")]
        public float keeperHoldSeconds = 0.8f;
        [Tooltip("Sale al encuentro de un rival con balón dentro del área a menos de esta distancia de la portería.")]
        public float keeperRushDistance = 8f;
    }
}
