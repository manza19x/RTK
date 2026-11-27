namespace FutbolTotal
{
    /// <summary>Estados del partido. Solo en <see cref="Playing"/> corre el reloj y se detectan fueras de banda.</summary>
    public enum MatchState
    {
        /// <summary>Antes de empezar: jugadores colocados, esperando al jugador.</summary>
        PreMatch,
        /// <summary>Saque inicial (inicio de cada parte y tras gol).</summary>
        Kickoff,
        Playing,
        /// <summary>Saque de banda, de puerta o córner pendiente.</summary>
        SetPiece,
        GoalCelebration,
        HalfTime,
        FullTime
    }

    public enum RestartType
    {
        Kickoff,
        ThrowIn,
        GoalKick,
        Corner
    }
}
