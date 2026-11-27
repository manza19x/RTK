using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Sonido de los botones de la interfaz (una fuente de audio compartida entre escenas).</summary>
    public static class UISound
    {
        private static AudioSource source;

        public static void PlayClick()
        {
            if (source == null)
            {
                var go = new GameObject("UISound");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.ignoreListenerPause = true;
            }
            SoundSettings settings = SettingsProvider.Load<SoundSettings>(SettingsProvider.SoundName);
            source.PlayOneShot(ProceduralAudio.Click, settings != null ? settings.uiClick : 0.4f);
        }
    }
}
