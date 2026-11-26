using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Identidad visual de un equipo ficticio (nombre y colores de la equipación temporal).</summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Equipo", fileName = "TeamSettings")]
    public class TeamSettings : ScriptableObject
    {
        public string teamName = "Club Aurora";
        [Tooltip("Abreviatura de 3 letras para el marcador.")]
        public string shortName = "AUR";
        public Color primaryColor = new Color(0.85f, 0.2f, 0.2f);
        public Color secondaryColor = Color.white;
        public Color goalkeeperColor = new Color(0.2f, 0.75f, 0.3f);
    }
}
