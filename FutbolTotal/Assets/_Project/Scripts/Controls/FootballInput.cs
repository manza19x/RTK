using UnityEngine;
using UnityEngine.InputSystem;

namespace FutbolTotal
{
    /// <summary>
    /// Entrada del jugador con el nuevo Input System.
    /// Decisión: las acciones se definen en código y no en un asset .inputactions. Así no hace falta generar
    /// clases ni configurar nada en el editor, y todas las asignaciones están en un único sitio legible.
    /// Las acciones son InputAction normales: el menú de opciones (Fase 5) podrá reasignarlas con
    /// PerformInteractiveRebinding y guardar las asignaciones con SaveBindingOverridesAsJson.
    ///
    /// Teclado: WASD mover · Shift izq. sprint · J pase corto / entrada · L pase largo · K tiro · Q cambiar jugador · Esc pausa.
    /// Mando:   stick izq. mover · RT sprint · A pase corto / entrada · X pase largo · B tiro · LB cambiar · Start pausa.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FootballInput : MonoBehaviour
    {
        [Tooltip("Segundos manteniendo un botón para alcanzar la carga máxima.")]
        [SerializeField] private float fullChargeSeconds = 0.9f;

        private InputActionMap map;

        public InputAction Move { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction SwitchPlayer { get; private set; }
        public InputAction Pause { get; private set; }
        /// <summary>Herramienta de pruebas: recolocar el balón (R / Select).</summary>
        public InputAction DebugReset { get; private set; }
        /// <summary>Mostrar u ocultar la ayuda de controles (F1 / stick derecho pulsado).</summary>
        public InputAction ToggleHelp { get; private set; }
        /// <summary>Mostrar u ocultar el rol de IA de cada jugador (F2 / stick izquierdo pulsado).</summary>
        public InputAction ToggleAIDebug { get; private set; }
        /// <summary>Panel técnico de depuración (F3, solo teclado).</summary>
        public InputAction ToggleDebugPanel { get; private set; }

        public ChargeButton ShortPass { get; private set; }
        public ChargeButton LongPass { get; private set; }
        public ChargeButton Shot { get; private set; }

        /// <summary>True si el último dispositivo usado fue un mando (para mostrar los iconos adecuados).</summary>
        public bool LastDeviceIsGamepad { get; private set; }

        public Vector2 ReadMove() => Move.ReadValue<Vector2>();
        public bool SprintHeld => Sprint.IsPressed();

        private void Awake()
        {
            BuildActions();
        }

        private void OnEnable() => map?.Enable();

        private void OnDisable() => map?.Disable();

        private void OnDestroy() => map?.Dispose();

        private void BuildActions()
        {
            map = new InputActionMap("Gameplay");

            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Sprint = AddButton("Sprint", "<Keyboard>/leftShift", "<Gamepad>/rightTrigger");
            SwitchPlayer = AddButton("SwitchPlayer", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            Pause = AddButton("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            DebugReset = AddButton("DebugReset", "<Keyboard>/r", "<Gamepad>/select");
            ToggleHelp = AddButton("ToggleHelp", "<Keyboard>/f1", "<Gamepad>/rightStickPress");
            ToggleAIDebug = AddButton("ToggleAIDebug", "<Keyboard>/f2", "<Gamepad>/leftStickPress");
            ToggleDebugPanel = AddButton("ToggleDebugPanel", "<Keyboard>/f3", null);

            ShortPass = new ChargeButton(AddButton("ShortPass", "<Keyboard>/j", "<Gamepad>/buttonSouth"), fullChargeSeconds);
            LongPass = new ChargeButton(AddButton("LongPass", "<Keyboard>/l", "<Gamepad>/buttonWest"), fullChargeSeconds);
            Shot = new ChargeButton(AddButton("Shot", "<Keyboard>/k", "<Gamepad>/buttonEast"), fullChargeSeconds);
        }

        private InputAction AddButton(string name, string keyboardPath, string gamepadPath)
        {
            InputAction action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboardPath);
            if (!string.IsNullOrEmpty(gamepadPath)) action.AddBinding(gamepadPath);
            return action;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            ShortPass.Tick(dt);
            LongPass.Tick(dt);
            Shot.Tick(dt);
            TrackLastDevice();
        }

        /// <summary>Cancela cargas en curso. Se usa al pausar/reanudar para no disparar acciones por accidente.</summary>
        public void CancelCharges()
        {
            ShortPass.Cancel();
            LongPass.Cancel();
            Shot.Cancel();
        }

        private void TrackLastDevice()
        {
            foreach (InputAction action in map.actions)
            {
                if (!action.WasPerformedThisFrame()) continue;
                InputControl control = action.activeControl;
                if (control != null) LastDeviceIsGamepad = control.device is Gamepad;
            }
        }
    }
}
