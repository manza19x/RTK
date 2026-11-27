namespace FutbolTotal
{
    /// <summary>
    /// Estado del partido que necesita mostrar la interfaz. Lo implementan <see cref="MatchManager"/> (partido)
    /// y <see cref="TrainingHarness"/> (entrenamiento), así el HUD funciona en ambas escenas.
    /// </summary>
    public interface IMatchStatus
    {
        int HomeGoals { get; }
        int AwayGoals { get; }
        bool IsPaused { get; }
        /// <summary>True si el balón está en juego: la IA solo actúa entonces.</summary>
        bool IsBallInPlay { get; }
        /// <summary>Texto del reloj, p. ej. "23:41".</summary>
        string ClockText { get; }
        /// <summary>Fase del partido, p. ej. "1ª parte".</summary>
        string PhaseText { get; }
        /// <summary>Mensaje central actual (gol, saque, descanso...).</summary>
        string Message { get; }
        /// <summary>Hasta cuándo (Time.unscaledTime) se muestra el mensaje.</summary>
        float MessageUntil { get; }
    }
}
