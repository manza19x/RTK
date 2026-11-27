namespace FutbolTotal
{
    /// <summary>Órdenes que la interfaz (menú de pausa) puede dar a las reglas activas.</summary>
    public interface IMatchControl
    {
        void SetPaused(bool paused);
        /// <summary>Vuelve a empezar desde 0-0.</summary>
        void RestartMatch();
    }
}
