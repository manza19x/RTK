using UnityEngine;

namespace FutbolTotal
{
    public enum MatchLengthOption { Short, Normal, Long }
    public enum DifficultyOption { Easy, Normal, Hard }
    public enum AssistOption { Low, Normal, High }

    /// <summary>
    /// Opciones del jugador, guardadas entre sesiones con PlayerPrefs. Se eligen en el menú principal
    /// y se aplican al empezar el partido. "Normal" respeta siempre los valores de los assets de ajustes.
    /// </summary>
    public static class GameOptions
    {
        private const string LengthKey = "FutbolTotal.MatchLength";
        private const string DifficultyKey = "FutbolTotal.Difficulty";
        private const string AssistKey = "FutbolTotal.PassAssist";
        private const string VolumeKey = "FutbolTotal.Volume";

        /// <summary>Volumen general (0..1), en pasos de 25 %.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.75f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                AudioListener.volume = Mathf.Clamp01(value);
            }
        }

        /// <summary>Siguiente paso de volumen: 0, 25, 50, 75, 100 %.</summary>
        public static float NextVolume(float current) => current >= 0.99f ? 0f : Mathf.Round(current * 4f + 1f) / 4f;

        public static MatchLengthOption MatchLength
        {
            get => (MatchLengthOption)PlayerPrefs.GetInt(LengthKey, (int)MatchLengthOption.Normal);
            set => Save(LengthKey, (int)value);
        }

        public static DifficultyOption Difficulty
        {
            get => (DifficultyOption)PlayerPrefs.GetInt(DifficultyKey, (int)DifficultyOption.Normal);
            set => Save(DifficultyKey, (int)value);
        }

        public static AssistOption PassAssist
        {
            get => (AssistOption)PlayerPrefs.GetInt(AssistKey, (int)AssistOption.Normal);
            set => Save(AssistKey, (int)value);
        }

        /// <summary>Multiplicador de la duración de cada parte respecto a MatchSettings.</summary>
        public static float MatchLengthMultiplier
        {
            get
            {
                switch (MatchLength)
                {
                    case MatchLengthOption.Short: return 0.6f;
                    case MatchLengthOption.Long: return 1.6f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Asistencia de pase para el jugador humano a partir del valor del asset.</summary>
        public static float ApplyPassAssist(float assetValue)
        {
            switch (PassAssist)
            {
                case AssistOption.Low: return assetValue * 0.7f;
                case AssistOption.High: return 1f;
                default: return assetValue;
            }
        }

        public static string Describe(MatchLengthOption value)
        {
            switch (value)
            {
                case MatchLengthOption.Short: return "Corta";
                case MatchLengthOption.Long: return "Larga";
                default: return "Normal";
            }
        }

        public static string Describe(DifficultyOption value)
        {
            switch (value)
            {
                case DifficultyOption.Easy: return "Fácil";
                case DifficultyOption.Hard: return "Difícil";
                default: return "Normal";
            }
        }

        public static string Describe(AssistOption value)
        {
            switch (value)
            {
                case AssistOption.Low: return "Baja";
                case AssistOption.High: return "Alta";
                default: return "Normal";
            }
        }

        private static void Save(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
