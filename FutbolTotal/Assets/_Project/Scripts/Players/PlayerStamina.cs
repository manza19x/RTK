using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Resistencia para el sprint: se gasta al esprintar y se recupera poco a poco tras una breve pausa.
    /// Si se agota, el sprint queda bloqueado hasta recuperar un umbral (evita el "sprint a tirones").
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;

        private float regenBlockedUntil;

        /// <summary>Resistencia actual entre 0 y 1.</summary>
        public float Current01 { get; private set; } = 1f;
        public bool IsExhausted { get; private set; }
        public bool CanSprint => !IsExhausted && Current01 > 0f;

        /// <summary>Lo llama <see cref="PlayerMotor"/> en cada paso de física.</summary>
        public void Tick(bool sprinting, float dt)
        {
            if (settings == null) return;

            if (sprinting)
            {
                Current01 = Mathf.Max(0f, Current01 - settings.sprintDrainPerSecond * dt);
                regenBlockedUntil = Time.time + settings.staminaRegenDelay;
                if (Current01 <= 0f) IsExhausted = true;
            }
            else if (Time.time >= regenBlockedUntil)
            {
                Current01 = Mathf.Min(1f, Current01 + settings.staminaRegenPerSecond * dt);
            }

            if (IsExhausted && Current01 >= settings.staminaRecoverThreshold) IsExhausted = false;
        }

        public void Refill()
        {
            Current01 = 1f;
            IsExhausted = false;
        }
    }
}
