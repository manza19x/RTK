using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Resuelve los assets de ajustes de forma robusta, por este orden:
    /// 1. El asset asignado en el Inspector del componente (permite ajustes distintos por objeto).
    /// 2. El asset compartido en Assets/_Project/Resources/FutbolTotal/ (lo crea el generador de escena).
    /// 3. Una instancia con los valores por defecto del código (el juego nunca se rompe por falta de ajustes).
    /// </summary>
    public static class SettingsProvider
    {
        /// <summary>Subcarpeta dentro de cualquier carpeta Resources del proyecto.</summary>
        public const string ResourcesFolder = "FutbolTotal";

        public const string MovementName = "PlayerMovementSettings";
        public const string BallName = "BallSettings";
        public const string ActionsName = "ActionSettings";
        public const string CameraName = "CameraSettings";
        public const string FormationName = "Formation_2-3-1";
        public const string HomeTeamName = "Team_ClubAurora";
        public const string AwayTeamName = "Team_DeportivoMarea";

        public static T Resolve<T>(T assigned, string assetName, Object context) where T : ScriptableObject
        {
            if (assigned != null) return assigned;

            T shared = Load<T>(assetName);
            if (shared != null) return shared;

            string owner = context != null ? context.name : "?";
            Debug.LogWarning($"[Fútbol Total] {owner}: no hay {typeof(T).Name} asignado ni en " +
                             $"Resources/{ResourcesFolder}/{assetName}; se usan valores por defecto.", context);
            return ScriptableObject.CreateInstance<T>();
        }

        public static T Load<T>(string assetName) where T : ScriptableObject
        {
            return Resources.Load<T>($"{ResourcesFolder}/{assetName}");
        }
    }
}
