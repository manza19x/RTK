using UnityEngine.InputSystem;

namespace FutbolTotal
{
    /// <summary>
    /// Botón de acción con carga: mantener pulsado carga (0..1) y la acción se dispara UNA vez al soltar.
    /// Mantenerlo pulsado nunca repite la acción; para otra hay que soltar y volver a pulsar.
    /// </summary>
    public sealed class ChargeButton
    {
        private readonly InputAction action;
        private readonly float fullChargeSeconds;

        public ChargeButton(InputAction action, float fullChargeSeconds)
        {
            this.action = action;
            this.fullChargeSeconds = fullChargeSeconds > 0f ? fullChargeSeconds : 1f;
        }

        public InputAction Action => action;
        public bool IsCharging { get; private set; }
        /// <summary>Carga actual mientras se mantiene pulsado.</summary>
        public float Charge01 { get; private set; }
        /// <summary>True solo en el frame en que se suelta el botón tras una pulsación válida.</summary>
        public bool ReleasedThisFrame { get; private set; }
        /// <summary>Carga en el momento de soltar.</summary>
        public float ReleasedCharge { get; private set; }

        public void Tick(float deltaTime)
        {
            ReleasedThisFrame = false;

            if (action.WasPressedThisFrame())
            {
                IsCharging = true;
                Charge01 = 0f;
            }
            if (!IsCharging) return;

            if (action.IsPressed())
            {
                Charge01 = UnityEngine.Mathf.Min(1f, Charge01 + deltaTime / fullChargeSeconds);
            }
            else
            {
                IsCharging = false;
                ReleasedThisFrame = true;
                ReleasedCharge = Charge01;
                Charge01 = 0f;
            }
        }

        /// <summary>Descarta la pulsación en curso (p. ej. al pausar o reanudar).</summary>
        public void Cancel()
        {
            IsCharging = false;
            Charge01 = 0f;
            ReleasedThisFrame = false;
        }
    }
}
