using System;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Volumen dentro de una portería. Detecta el gol cuando el balón ha cruzado la línea POR COMPLETO
    /// (regla real: el centro del balón más allá de la línea más su radio) y frena el balón al tocar la red.
    /// No decide qué pasa después: solo avisa con <see cref="Scored"/>; las reglas del partido lo gestionan.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GoalZone : MonoBehaviour
    {
        [Tooltip("Portería en -X (-1) o en +X (+1).")]
        [SerializeField] private int goalSide = 1;
        [Tooltip("Frenada del balón dentro de la portería (fracción de velocidad perdida por segundo).")]
        [SerializeField] private float netDamping = 3f;

        /// <summary>Se lanza una vez por gol. Hay que llamar a <see cref="Rearm"/> antes del siguiente saque.</summary>
        public event Action<GoalZone> Scored;

        public int GoalSide => goalSide;
        public bool Armed { get; private set; } = true;

        public void Rearm() => Armed = true;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.TryGetComponent(out Ball ball)) return;
            var context = MatchContext.Current;
            if (context == null) return;

            float lineX = context.Pitch.GoalCenter(goalSide).x;
            float ballX = ball.Position.x;
            bool fullyCrossed = goalSide > 0 ? ballX > lineX + ball.Radius : ballX < lineX - ball.Radius;
            if (!fullyCrossed) return;

            ball.ApplyDamping(netDamping * Time.fixedDeltaTime);

            if (Armed)
            {
                Armed = false;
                Scored?.Invoke(this);
            }
        }
    }
}
