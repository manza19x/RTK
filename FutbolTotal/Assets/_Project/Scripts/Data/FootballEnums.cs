namespace FutbolTotal
{
    /// <summary>Puesto del futbolista. Lo usan la formación, la IA y las reglas de control del portero.</summary>
    public enum PlayerRole
    {
        Goalkeeper,
        Defender,
        Midfielder,
        Forward
    }

    /// <summary>Equipo local o visitante. No implica dirección de ataque (eso lo define <see cref="Team"/>).</summary>
    public enum TeamSide
    {
        Home,
        Away
    }

    /// <summary>Tipo de golpeo, para que IA, cámara e interfaz puedan reaccionar a cada uno.</summary>
    public enum KickType
    {
        ShortPass,
        LongPass,
        Shot,
        Deflection
    }
}
