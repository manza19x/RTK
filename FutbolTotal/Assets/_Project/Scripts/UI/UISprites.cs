using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Gráficos de la interfaz generados por código (sin imágenes externas): rectángulos redondeados escalables
    /// (9-slice), degradados, círculos, anillos, triángulos y escudos de equipo. Se crean una vez y se reutilizan.
    /// </summary>
    public static class UISprites
    {
        private static Sprite rounded, roundedSmall, pill, circle, ring, triangle, gradientLeft, gradientUp, white;
        private static readonly Dictionary<string, Sprite> crests = new Dictionary<string, Sprite>();

        /// <summary>Rectángulo redondeado (radio 18 px), escalable sin deformar las esquinas.</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = RoundedRect(64, 18);
        public static Sprite RoundedSmall => roundedSmall != null ? roundedSmall : roundedSmall = RoundedRect(32, 7);
        /// <summary>Píldora: extremos totalmente redondos.</summary>
        public static Sprite Pill => pill != null ? pill : pill = RoundedRect(64, 31);
        public static Sprite Circle => circle != null ? circle : circle = Disc(64, 0f);
        public static Sprite Ring => ring != null ? ring : ring = Disc(128, 0.86f);
        public static Sprite Triangle => triangle != null ? triangle : triangle = CreateTriangle(64);
        /// <summary>Degradado horizontal: opaco a la izquierda, transparente a la derecha.</summary>
        public static Sprite GradientLeft => gradientLeft != null ? gradientLeft : gradientLeft = Gradient(true);
        /// <summary>Degradado vertical: opaco abajo, transparente arriba.</summary>
        public static Sprite GradientUp => gradientUp != null ? gradientUp : gradientUp = Gradient(false);
        public static Sprite White => white != null ? white : white = RoundedRect(8, 0);

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
        }

        private static Sprite RoundedRect(int size, int radius)
        {
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;
                    if (radius > 0)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            float border = Mathf.Max(radius, 1);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>Círculo relleno (<paramref name="hole"/> = 0) o anillo (radio interior relativo).</summary>
        private static Sprite Disc(int size, float hole)
        {
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float outer = Mathf.Clamp01(r - d);
                    float inner = hole > 0f ? Mathf.Clamp01(d - r * hole) : 1f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Min(outer, inner));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Triángulo que apunta hacia abajo (marcador sobre el jugador).</summary>
        private static Sprite CreateTriangle(int size)
        {
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float halfWidth = (y + 0.5f) / size * size * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - size * 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(halfWidth - d + 0.5f));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite Gradient(bool horizontal)
        {
            const int length = 128;
            Texture2D texture = horizontal ? NewTexture(length, 4) : NewTexture(4, length);
            for (int i = 0; i < length; i++)
            {
                float alpha = 1f - i / (float)(length - 1);
                alpha = alpha * alpha * (3f - 2f * alpha);
                for (int j = 0; j < 4; j++)
                {
                    if (horizontal) texture.SetPixel(i, j, new Color(1f, 1f, 1f, alpha));
                    else texture.SetPixel(j, i, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Escudo inventado del equipo: forma clásica de escudo con borde del color secundario, banda diagonal
        /// y una estrella-rombo en el centro. Sin marcas reales.
        /// </summary>
        public static Sprite Crest(Color primary, Color secondary)
        {
            string key = ColorUtility.ToHtmlStringRGB(primary) + ColorUtility.ToHtmlStringRGB(secondary);
            if (crests.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            const int size = 128;
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;   // -1..1
                    float v = (y + 0.5f) / size;             // 0 abajo, 1 arriba
                    float outside = ShieldDistance(u, v);
                    if (outside > 0.03f)
                    {
                        pixels[y * size + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }
                    float edgeAlpha = Mathf.Clamp01((0.03f - outside) / 0.03f);
                    Color c = primary;
                    if (outside > -0.09f) c = secondary;                              // borde
                    else if (Mathf.Abs(u * 0.8f - (v - 0.5f)) < 0.13f) c = secondary;  // banda diagonal
                    if (Mathf.Abs(u) + Mathf.Abs(v - 0.55f) < 0.17f) c = Color.Lerp(secondary, Color.white, 0.5f); // rombo
                    // Ligero brillo arriba.
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01((v - 0.75f) * 0.6f));
                    c.a = edgeAlpha;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            crests[key] = sprite;
            return sprite;
        }

        /// <summary>Distancia aproximada (positiva = fuera) a un escudo: rectángulo arriba y punta curva abajo.</summary>
        private static float ShieldDistance(float u, float v)
        {
            float halfWidth = v > 0.45f ? 0.82f : 0.82f * Mathf.Sqrt(Mathf.Clamp01(v / 0.45f)) * (0.35f + 0.65f * v / 0.45f);
            float side = Mathf.Abs(u) - halfWidth;
            float top = v - 0.96f;
            float bottom = 0.04f - v;
            return Mathf.Max(side, Mathf.Max(top, bottom));
        }
    }
}
