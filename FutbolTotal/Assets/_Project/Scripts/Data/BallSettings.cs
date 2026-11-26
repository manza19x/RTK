using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Física del balón y reglas de control (conducción, recepción, desvíos).
    /// El balón no usa rozamiento de PhysX para rodar: aplicamos una desaceleración constante propia,
    /// así los pases se pueden calcular de forma exacta (ver <see cref="KickMath"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes del balón", fileName = "BallSettings")]
    public class BallSettings : ScriptableObject
    {
        [Header("Física")]
        [Tooltip("Radio en metros. Un balón real mide 0.11; usamos algo más para que se lea bien desde la cámara.")]
        public float radius = 0.15f;
        public float mass = 0.43f;
        [Tooltip("Desaceleración al rodar por el césped (m/s²). Determina cuánto recorre un pase raso.")]
        public float rollingDeceleration = 2.4f;
        [Tooltip("Velocidad máxima permitida (m/s), para evitar valores físicos absurdos.")]
        public float maxSpeed = 36f;
        [Tooltip("Margen de altura para considerar que el balón está en el suelo.")]
        public float groundedTolerance = 0.06f;

        [Header("Conducción")]
        [Tooltip("Distancia del balón delante del cuerpo al conducir trotando.")]
        public float dribbleDistanceJog = 0.45f;
        [Tooltip("Distancia del balón delante del cuerpo al conducir a máxima velocidad (se aleja: más fácil de robar).")]
        public float dribbleDistanceSprint = 1.4f;
        [Tooltip("Rigidez con la que el balón sigue al pie trotando (1/s). Más alto = más pegado.")]
        public float dribbleStiffnessJog = 15f;
        [Tooltip("Rigidez a máxima velocidad. Más bajo = el balón se retrasa en los giros.")]
        public float dribbleStiffnessSprint = 6f;
        [Tooltip("Si el balón queda más lejos que esto del conductor, se pierde el control.")]
        public float maxDribbleDistance = 2.6f;

        [Header("Recepción")]
        [Tooltip("Distancia horizontal a la que un jugador de campo puede hacerse con el balón.")]
        public float controlRadius = 0.9f;
        [Tooltip("Altura máxima a la que un jugador de campo controla el balón.")]
        public float controlMaxHeight = 1.2f;
        [Tooltip("Velocidad por encima de la cual el balón rebota en el jugador en lugar de quedar controlado.")]
        public float maxControllableSpeed = 21f;
        [Tooltip("Fracción de velocidad que conserva el balón tras un rebote en un jugador.")]
        [Range(0f, 1f)] public float deflectionDamping = 0.35f;
        [Tooltip("Tiempo durante el que quien golpea el balón no puede volver a controlarlo.")]
        public float kickerIgnoreTime = 0.3f;

        [Header("Portero (dentro de su área)")]
        public float goalkeeperControlRadius = 1.4f;
        public float goalkeeperMaxHeight = 2.5f;
        [Tooltip("Por encima de esta velocidad el portero despeja en lugar de blocar.")]
        public float goalkeeperMaxCatchSpeed = 23f;
    }
}
