namespace FutbolTotal
{
    /// <summary>Rol que el equipo asigna a cada jugador controlado por la IA.</summary>
    public enum AIRole
    {
        Goalkeeper,
        /// <summary>Lleva el balón: decide entre tirar, pasar o conducir.</summary>
        BallCarrier,
        /// <summary>Su equipo ataca: apoya al conductor o se desmarca buscando una línea de pase libre.</summary>
        Support,
        /// <summary>Balón suelto: va a buscarlo (o a recibir un pase dirigido a él).</summary>
        Chase,
        /// <summary>Presiona al rival con balón y hace entradas.</summary>
        Press,
        /// <summary>Como Press pero sin entradas: compañero del jugador humano que le cubre las espaldas.</summary>
        Contain,
        /// <summary>Defiende su zona de la formación y marca al rival que entra en ella.</summary>
        Zone
    }

    public static class AIRoleNames
    {
        /// <summary>Etiqueta corta para mostrar sobre los jugadores (F2).</summary>
        public static string Short(AIRole role)
        {
            switch (role)
            {
                case AIRole.Goalkeeper: return "POR";
                case AIRole.BallCarrier: return "BALÓN";
                case AIRole.Support: return "APOYO";
                case AIRole.Chase: return "BUSCA";
                case AIRole.Press: return "PRESIÓN";
                case AIRole.Contain: return "CUBRE";
                case AIRole.Zone: return "ZONA";
                default: return role.ToString();
            }
        }
    }
}
