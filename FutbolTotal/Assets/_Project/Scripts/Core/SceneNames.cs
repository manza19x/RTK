using UnityEngine;
using UnityEngine.SceneManagement;

namespace FutbolTotal
{
    /// <summary>Nombres de las escenas (deben coincidir con los archivos de Assets/_Project/Scenes).</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MenuPrincipal";
        public const string Match = "Partido";
        public const string Training = "Entrenamiento";
    }

    /// <summary>Cambio de escena seguro: restaura el tiempo y avisa si la escena no está en Build Settings.</summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Fútbol Total] La escena '{sceneName}' no está en Build Settings. " +
                               "Usa «Fútbol Total ▸ Crear todas las escenas».");
                return;
            }
            SceneManager.LoadScene(sceneName);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
