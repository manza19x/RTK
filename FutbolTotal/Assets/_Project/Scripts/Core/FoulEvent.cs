using UnityEngine;

namespace FutbolTotal
{
    public enum FoulReason
    {
        /// <summary>Entrada llegando por la espalda del rival.</summary>
        FromBehind,
        /// <summary>La entrada choca con el rival sin llegar al balón.</summary>
        NoBallContact
    }

    /// <summary>Una falta cometida. La detecta <see cref="PlayerActions"/>; la sanciona quien aplique las reglas.</summary>
    public readonly struct FoulEvent
    {
        public readonly FootballPlayer Offender;
        public readonly FootballPlayer Victim;
        /// <summary>Lugar de la falta (posición del jugador que la recibe).</summary>
        public readonly Vector3 Position;
        public readonly FoulReason Reason;

        public FoulEvent(FootballPlayer offender, FootballPlayer victim, Vector3 position, FoulReason reason)
        {
            Offender = offender;
            Victim = victim;
            Position = position;
            Reason = reason;
        }

        public string Describe() => Reason == FoulReason.FromBehind ? "entrada por detrás" : "no tocó el balón";
    }
}
