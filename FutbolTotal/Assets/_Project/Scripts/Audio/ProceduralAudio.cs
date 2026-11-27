using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Sonidos generados por código (síntesis sencilla), para no depender de archivos de audio de terceros.
    /// Cada clip se crea una sola vez y se reutiliza. Para usar sonidos grabados, basta con sustituir
    /// el clip que devuelve cada propiedad.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int Rate = 44100;
        private const float TwoPi = Mathf.PI * 2f;

        private static AudioClip kick, bounce, post, net, shortWhistle, longWhistle, crowdLoop, cheer, click;

        public static AudioClip Kick => kick != null ? kick : kick = CreateKick();
        public static AudioClip Bounce => bounce != null ? bounce : bounce = CreateBounce();
        public static AudioClip Post => post != null ? post : post = CreatePost();
        public static AudioClip Net => net != null ? net : net = CreateNet();
        public static AudioClip ShortWhistle => shortWhistle != null ? shortWhistle : shortWhistle = CreateWhistle("WhistleShort", 0.35f);
        public static AudioClip LongWhistle => longWhistle != null ? longWhistle : longWhistle = CreateWhistle("WhistleLong", 1.0f);
        public static AudioClip CrowdLoop => crowdLoop != null ? crowdLoop : crowdLoop = CreateCrowdLoop();
        public static AudioClip Cheer => cheer != null ? cheer : cheer = CreateCheer();
        public static AudioClip Click => click != null ? click : click = CreateClick();

        private static AudioClip ToClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Golpeo: un "tum" grave que baja de tono más un chasquido corto.</summary>
        private static AudioClip CreateKick()
        {
            var random = new System.Random(1);
            float[] data = new float[(int)(0.16f * Rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float frequency = Mathf.Lerp(150f, 55f, Mathf.Clamp01(t / 0.1f));
                phase += TwoPi * frequency / Rate;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t * 28f);
                float snap = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 160f) * 0.35f;
                data[i] = (body + snap) * 0.9f;
            }
            return ToClip("Kick", data);
        }

        private static AudioClip CreateBounce()
        {
            float[] data = new float[(int)(0.12f * Rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                phase += TwoPi * Mathf.Lerp(95f, 50f, t / 0.12f) / Rate;
                data[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 38f) * 0.7f;
            }
            return ToClip("Bounce", data);
        }

        /// <summary>Balón contra el poste: timbre metálico (dos parciales) con algo de golpe.</summary>
        private static AudioClip CreatePost()
        {
            float[] data = new float[(int)(0.9f * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float ring = Mathf.Sin(TwoPi * 820f * t) * Mathf.Exp(-t * 6f) * 0.5f
                             + Mathf.Sin(TwoPi * 1310f * t) * Mathf.Exp(-t * 9f) * 0.3f
                             + Mathf.Sin(TwoPi * 2150f * t) * Mathf.Exp(-t * 14f) * 0.15f;
                float hit = Mathf.Sin(TwoPi * 110f * t) * Mathf.Exp(-t * 40f) * 0.5f;
                data[i] = (ring + hit) * 0.8f;
            }
            return ToClip("Post", data);
        }

        /// <summary>Balón en la red: soplido de ruido filtrado.</summary>
        private static AudioClip CreateNet()
        {
            var random = new System.Random(2);
            float[] data = new float[(int)(0.45f * Rate)];
            float filtered = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered += (noise - filtered) * 0.15f;
                float envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 7f);
                data[i] = filtered * envelope * 1.6f;
            }
            return ToClip("Net", data);
        }

        /// <summary>Silbato: tono agudo con vibrato rápido.</summary>
        private static AudioClip CreateWhistle(string name, float seconds)
        {
            float[] data = new float[(int)(seconds * Rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float frequency = 2900f + 90f * Mathf.Sin(TwoPi * 28f * t);
                phase += TwoPi * frequency / Rate;
                float envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((seconds - t) / 0.05f);
                float tone = Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2f);
                data[i] = tone * envelope * 0.35f;
            }
            return ToClip(name, data);
        }

        /// <summary>Murmullo del público: ruido grave filtrado y modulado. Los extremos se funden para que el bucle no se note.</summary>
        private static AudioClip CreateCrowdLoop()
        {
            const float seconds = 6f;
            var random = new System.Random(3);
            int length = (int)(seconds * Rate);
            float[] data = new float[length];
            float low = 0f, band = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.06f;
                band += (low - band) * 0.3f;
                float swell = 0.8f + 0.2f * Mathf.Sin(TwoPi * 0.31f * t) * Mathf.Sin(TwoPi * 0.13f * t + 1f);
                data[i] = (low - band * 0.5f) * swell * 3f;
            }
            int fade = Rate / 2;
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                data[i] = data[i] * w + data[length - fade + i] * (1f - w);
            }
            System.Array.Resize(ref data, length - fade);
            return ToClip("CrowdLoop", data);
        }

        /// <summary>Celebración de gol: el público sube de golpe y se va apagando.</summary>
        private static AudioClip CreateCheer()
        {
            const float seconds = 3.5f;
            var random = new System.Random(4);
            float[] data = new float[(int)(seconds * Rate)];
            float low = 0f, mid = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.12f;
                mid += (noise - mid) * 0.35f;
                float envelope = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((seconds - t) / 1.6f);
                float flutter = 0.85f + 0.15f * Mathf.Sin(TwoPi * 6f * t);
                data[i] = (low * 1.4f + (mid - low) * 0.5f) * envelope * flutter * 1.8f;
            }
            return ToClip("Cheer", data);
        }

        private static AudioClip CreateClick()
        {
            float[] data = new float[(int)(0.05f * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Sin(TwoPi * 1250f * t) * Mathf.Exp(-t * 70f) * 0.5f;
            }
            return ToClip("Click", data);
        }
    }
}
