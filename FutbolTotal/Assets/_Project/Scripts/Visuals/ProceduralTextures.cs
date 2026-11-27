using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Texturas generadas por código (sin imágenes de terceros): césped con relieve, balón clásico de pentágonos
    /// y vallas publicitarias. El generador de escenas las guarda como PNG en Art/Textures; se pueden sustituir
    /// por texturas fotográficas con el mismo nombre.
    /// Todas las texturas de repetición son "tileables" (sin costuras al repetirse).
    /// </summary>
    public static class ProceduralTextures
    {
        // ------------------------------------------------------------------ Ruido

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static int Wrap(int value, int period) => ((value % period) + period) % period;

        /// <summary>Ruido de valor periódico en [0,1]: con u,v en [0,1) y periodos enteros se repite sin costuras.</summary>
        private static float ValueNoise(float u, float v, int periodX, int periodY, int seed)
        {
            float x = u * periodX;
            float y = v * periodY;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int ax = Wrap(x0, periodX), bx = Wrap(x0 + 1, periodX);
            int ay = Wrap(y0, periodY), by = Wrap(y0 + 1, periodY);
            float a = Mathf.Lerp(Hash(ax, ay, seed), Hash(bx, ay, seed), fx);
            float b = Mathf.Lerp(Hash(ax, by, seed), Hash(bx, by, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amplitude = 0.5f, total = 0f;
            int period = basePeriod;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u, v, period, period, seed + i * 31) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                period *= 2;
            }
            return sum / total;
        }

        /// <summary>Altura de la "hierba": manchas grandes, briznas alargadas y grano fino.</summary>
        private static float GrassHeight(float u, float v, int x, int y)
        {
            float clumps = Fbm(u, v, 6, 4, 11);
            float blades = ValueNoise(u, v, 256, 48, 23);
            float grain = Hash(x, y, 37);
            return clumps * 0.35f + blades * 0.45f + grain * 0.2f;
        }

        // ------------------------------------------------------------------ Césped

        public static Texture2D GrassAlbedo(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color dark = new Color(0.16f, 0.34f, 0.11f);
            Color light = new Color(0.33f, 0.55f, 0.2f);
            Color dry = new Color(0.42f, 0.5f, 0.24f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float h = GrassHeight(u, v, x, y);
                    float dryness = Mathf.SmoothStep(0.55f, 0.8f, Fbm(u, v, 4, 3, 91)) * 0.35f;
                    Color c = Color.Lerp(dark, light, h);
                    c = Color.Lerp(c, dry, dryness * h);
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        /// <summary>Mapa de normales a partir de la misma altura que el color (relieve de las briznas).</summary>
        public static Texture2D GrassNormal(int size, float strength = 2.5f)
        {
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[y * size + x] = GrassHeight(x / (float)size, y / (float)size, x, y);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float left = heights[y * size + Wrap(x - 1, size)];
                    float right = heights[y * size + Wrap(x + 1, size)];
                    float down = heights[Wrap(y - 1, size) * size + x];
                    float up = heights[Wrap(y + 1, size) * size + x];
                    Vector3 n = new Vector3((left - right) * strength, (down - up) * strength, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        // ------------------------------------------------------------------ Balón

        /// <summary>
        /// Balón clásico en proyección equirectangular (para la esfera de Unity): 12 pentágonos negros en los
        /// vértices de un icosaedro, 20 hexágonos blancos y costuras grises entre paneles.
        /// </summary>
        public static Texture2D Ball(int width, int height)
        {
            Vector3[] pentagons = IcosahedronVertices();
            Vector3[] hexagons = DodecahedronVertices(pentagons);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            var pixels = new Color[width * height];
            Color white = new Color(0.96f, 0.96f, 0.94f);
            Color black = new Color(0.08f, 0.08f, 0.1f);
            Color seam = new Color(0.55f, 0.55f, 0.55f);

            for (int y = 0; y < height; y++)
            {
                float latitude = (y + 0.5f) / height * Mathf.PI - Mathf.PI * 0.5f;
                for (int x = 0; x < width; x++)
                {
                    float longitude = (x + 0.5f) / width * Mathf.PI * 2f;
                    Vector3 d = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude),
                                            Mathf.Cos(latitude) * Mathf.Sin(longitude));

                    float best = -2f, second = -2f;
                    bool bestIsPentagon = false;
                    Consider(d, pentagons, true, ref best, ref second, ref bestIsPentagon);
                    Consider(d, hexagons, false, ref best, ref second, ref bestIsPentagon);

                    Color c = bestIsPentagon ? black : white;
                    // Costura: cerca del borde entre los dos paneles más próximos.
                    if (best - second < 0.012f) c = Color.Lerp(seam, c, (best - second) / 0.012f * 0.6f);
                    pixels[y * width + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static void Consider(Vector3 d, Vector3[] centers, bool pentagon, ref float best, ref float second, ref bool bestIsPentagon)
        {
            foreach (Vector3 center in centers)
            {
                // Los pentágonos son algo más pequeños que los hexágonos: se penaliza un poco su "distancia".
                float score = Vector3.Dot(d, center) - (pentagon ? 0.035f : 0f);
                if (score > best)
                {
                    second = best;
                    best = score;
                    bestIsPentagon = pentagon;
                }
                else if (score > second)
                {
                    second = score;
                }
            }
        }

        private static Vector3[] IcosahedronVertices()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] v =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Length; i++) v[i] = v[i].normalized;
            return v;
        }

        /// <summary>Centros de las 20 caras del icosaedro (= hexágonos del balón).</summary>
        private static Vector3[] DodecahedronVertices(Vector3[] ico)
        {
            var faces = new System.Collections.Generic.List<Vector3>();
            float edge = Vector3.Distance(ico[0], ico[1]) * 1.01f;
            for (int a = 0; a < ico.Length; a++)
                for (int b = a + 1; b < ico.Length; b++)
                    for (int c = b + 1; c < ico.Length; c++)
                    {
                        if (Vector3.Distance(ico[a], ico[b]) > edge || Vector3.Distance(ico[b], ico[c]) > edge ||
                            Vector3.Distance(ico[a], ico[c]) > edge) continue;
                        faces.Add((ico[a] + ico[b] + ico[c]).normalized);
                    }
            return faces.ToArray();
        }

        // ------------------------------------------------------------------ Dorsales

        /// <summary>Cifras de 5×7 celdas (fuente tipo marcador), de arriba abajo.</summary>
        private static readonly string[][] Digits =
        {
            new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
            new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
            new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
            new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
            new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
            new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
            new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
            new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
            new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
            new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" },
        };

        /// <summary>Dorsal blanco sobre fondo transparente (el color final lo pone el material).</summary>
        public static Texture2D ShirtNumber(int number, int size = 128)
        {
            string text = Mathf.Clamp(number, 0, 99).ToString();
            int columns = text.Length * 5 + (text.Length - 1);
            int cell = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(size * 0.9f / columns, size * 0.9f / 7f)));
            int offsetX = (size - columns * cell) / 2;
            int offsetY = (size - 7 * cell) / 2;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1f, 1f, 1f, 0f);

            for (int d = 0; d < text.Length; d++)
            {
                string[] glyph = Digits[text[d] - '0'];
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (glyph[row][col] != '1') continue;
                        int x0 = offsetX + (d * 6 + col) * cell;
                        int y0 = offsetY + (6 - row) * cell; // la fila 0 es la de arriba
                        for (int y = y0; y < y0 + cell; y++)
                            for (int x = x0; x < x0 + cell; x++)
                                pixels[y * size + x] = Color.white;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        // ------------------------------------------------------------------ Vallas

        /// <summary>Valla publicitaria inventada (franjas diagonales y degradado; sin marcas).</summary>
        public static Texture2D AdBoard(int width, int height, Color a, Color b, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            var pixels = new Color[width * height];
            float stripeWidth = 18f + (seed % 3) * 8f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float stripe = Mathf.Repeat((x + y * 1.5f) / stripeWidth, 2f) < 1f ? 1f : 0f;
                    float gradient = x / (float)width;
                    Color c = Color.Lerp(a, b, gradient * 0.6f + stripe * 0.4f);
                    float border = (y < 3 || y > height - 4) ? 0.6f : 1f;
                    pixels[y * width + x] = c * border;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
