using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Ajustes del personaje importado (Mixamo). Si no hay modelo o se desactiva, se usa el cuerpo articulado
    /// generado por código (Fase 6).
    /// </summary>
    [CreateAssetMenu(menuName = "Fútbol Total/Ajustes de personaje", fileName = "CharacterSettings")]
    public class CharacterSettings : ScriptableObject
    {
        [Tooltip("Usar el modelo de Assets/_Project/Art/Mixamo si existe.")]
        public bool useImportedModel = true;

        [Tooltip("Altura a la que se escala el modelo (m).")]
        public float modelHeight = 1.8f;

        [Tooltip("Marca si en la animación Estirada el portero se lanza hacia SU derecha. Si en el juego se estira " +
                 "hacia el lado equivocado, cambia esta casilla y vuelve a crear las escenas.")]
        public bool diveClipGoesRight = true;

        [Tooltip("Palabras que identifican los materiales de la equipación (se tiñen con los colores del equipo). " +
                 "Los nombres de los materiales del modelo aparecen en la consola al crear las escenas.")]
        public string[] kitMaterialKeywords = { "shirt", "jersey", "top", "short", "pant", "sock", "cloth", "kit", "uniform", "outfit" };

        [Tooltip("Palabras que identifican materiales que NO se tiñen (piel, pelo, ojos...).")]
        public string[] keepMaterialKeywords = { "skin", "hair", "eye", "head", "face", "teeth", "lash", "brow", "shoe", "boot" };

        [Tooltip("Velocidad (fracción del sprint) a la que suena la animación de trote en la mezcla.")]
        [Range(0.2f, 0.9f)] public float jogBlendPoint = 0.6f;
    }
}
