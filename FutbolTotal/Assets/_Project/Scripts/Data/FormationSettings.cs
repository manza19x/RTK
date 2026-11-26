using System;
using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Un puesto de la formación.</summary>
    [Serializable]
    public struct FormationSlot
    {
        public PlayerRole role;
        public int shirtNumber;
        [Tooltip("Posición base normalizada. X: 0 = línea de gol propia, 1 = línea de gol rival. " +
                 "Y: -1 = banda izquierda, +1 = banda derecha (mirando hacia la portería rival).")]
        public Vector2 basePosition;
    }

    /// <summary>
    /// Formación en coordenadas normalizadas, independientes del tamaño del campo y del lado en que juegue el equipo.
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Formación", fileName = "Formation")]
    public class FormationSettings : ScriptableObject
    {
        public string displayName = "2-3-1";
        [Tooltip("En el saque inicial ningún jugador pasa de esta profundidad (0.5 = medio campo).")]
        [Range(0.3f, 0.5f)] public float kickoffMaxDepth = 0.46f;
        public List<FormationSlot> slots = new List<FormationSlot>();

        /// <summary>Rellena la formación por defecto de fútbol 7: portero + 2 defensas + 3 medios + 1 delantero.</summary>
        public void FillDefaultSevenASide()
        {
            displayName = "2-3-1";
            slots = new List<FormationSlot>
            {
                new FormationSlot { role = PlayerRole.Goalkeeper, shirtNumber = 1, basePosition = new Vector2(0.03f, 0f) },
                new FormationSlot { role = PlayerRole.Defender,   shirtNumber = 2, basePosition = new Vector2(0.24f, 0.45f) },
                new FormationSlot { role = PlayerRole.Defender,   shirtNumber = 4, basePosition = new Vector2(0.24f, -0.45f) },
                new FormationSlot { role = PlayerRole.Midfielder, shirtNumber = 7, basePosition = new Vector2(0.45f, 0.65f) },
                new FormationSlot { role = PlayerRole.Midfielder, shirtNumber = 8, basePosition = new Vector2(0.40f, 0f) },
                new FormationSlot { role = PlayerRole.Midfielder, shirtNumber = 11, basePosition = new Vector2(0.45f, -0.65f) },
                new FormationSlot { role = PlayerRole.Forward,    shirtNumber = 9, basePosition = new Vector2(0.62f, 0f) },
            };
        }
    }
}
