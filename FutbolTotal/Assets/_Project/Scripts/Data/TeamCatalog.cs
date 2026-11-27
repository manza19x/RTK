using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Catálogo de equipos ficticios (nombres, colores, escudo y estadio inventados; ninguno es un club real).
    /// El menú principal elige local y visitante por índice (<see cref="GameOptions"/>) y cada <see cref="Team"/>
    /// crea su <see cref="TeamSettings"/> al arrancar.
    /// </summary>
    public static class TeamCatalog
    {
        private struct Entry
        {
            public string Name, Short, Stadium;
            public Color Primary, Secondary, Keeper;
            public int Attack, Midfield, Defense;

            public Entry(string name, string shortName, string stadium, Color primary, Color secondary, Color keeper,
                         int attack, int midfield, int defense)
            {
                Name = name;
                Short = shortName;
                Stadium = stadium;
                Primary = primary;
                Secondary = secondary;
                Keeper = keeper;
                Attack = attack;
                Midfield = midfield;
                Defense = defense;
            }
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static readonly Entry[] Teams =
        {
            new Entry("Club Aurora", "AUR", "Estadio Aurora", C(0.86f, 0.24f, 0.18f), C(0.98f, 0.95f, 0.88f), C(0.95f, 0.8f, 0.15f), 78, 80, 76),
            new Entry("Deportivo Marea", "MAR", "Estadio del Puerto", C(0.16f, 0.36f, 0.82f), C(0.1f, 0.12f, 0.2f), C(0.35f, 0.85f, 0.45f), 77, 75, 80),
            new Entry("Atlético Volcán", "VOL", "Cráter Arena", C(0.95f, 0.45f, 0.1f), C(0.08f, 0.08f, 0.08f), C(0.2f, 0.7f, 0.95f), 84, 76, 70),
            new Entry("Unión Cóndor", "CON", "Estadio Altiplano", C(0.1f, 0.1f, 0.12f), C(0.95f, 0.95f, 0.95f), C(1f, 0.85f, 0.1f), 74, 82, 79),
            new Entry("Racing Brisa", "BRI", "Campo de la Brisa", C(0.45f, 0.75f, 0.95f), C(0.97f, 0.97f, 0.97f), C(0.15f, 0.15f, 0.15f), 72, 78, 74),
            new Entry("Sporting Pinar", "PIN", "Estadio El Pinar", C(0.1f, 0.55f, 0.25f), C(0.97f, 0.97f, 0.97f), C(0.9f, 0.3f, 0.6f), 76, 77, 77),
            new Entry("Ciudad Ámbar", "AMB", "Estadio Ámbar", C(0.95f, 0.7f, 0.15f), C(0.35f, 0.2f, 0.1f), C(0.3f, 0.3f, 0.9f), 80, 72, 73),
            new Entry("Estrella Polar", "EST", "Estadio Boreal", C(0.95f, 0.95f, 0.97f), C(0.3f, 0.55f, 0.9f), C(0.9f, 0.2f, 0.2f), 73, 79, 83),
            new Entry("Halcones del Sur", "HAL", "Nido del Sur", C(0.5f, 0.08f, 0.15f), C(0.95f, 0.78f, 0.3f), C(0.2f, 0.8f, 0.6f), 82, 80, 75),
            new Entry("Tormenta FC", "TOR", "Estadio Relámpago", C(0.45f, 0.2f, 0.7f), C(0.97f, 0.97f, 0.97f), C(0.95f, 0.85f, 0.2f), 79, 74, 71),
            new Entry("Montaña Azul", "MON", "Estadio La Cumbre", C(0.08f, 0.15f, 0.4f), C(0.95f, 0.75f, 0.2f), C(0.9f, 0.4f, 0.1f), 75, 76, 84),
            new Entry("Leones del Valle", "LEO", "Estadio del Valle", C(0.98f, 0.85f, 0.15f), C(0.08f, 0.08f, 0.08f), C(0.2f, 0.5f, 0.95f), 83, 78, 72),
            new Entry("Arena Dorada", "ARE", "Estadio Dorado", C(0.85f, 0.68f, 0.25f), C(0.08f, 0.08f, 0.08f), C(0.6f, 0.1f, 0.6f), 70, 73, 75),
            new Entry("Selva Verde", "SEL", "Estadio Jungla", C(0.05f, 0.35f, 0.15f), C(0.95f, 0.85f, 0.2f), C(0.9f, 0.3f, 0.1f), 77, 81, 74),
            new Entry("Faro del Norte", "FAR", "Estadio El Faro", C(0.8f, 0.12f, 0.12f), C(0.97f, 0.97f, 0.97f), C(0.15f, 0.6f, 0.9f), 74, 72, 78),
            new Entry("Celeste FC", "CEL", "Estadio Celeste", C(0.55f, 0.8f, 0.95f), C(0.1f, 0.15f, 0.35f), C(0.95f, 0.5f, 0.1f), 81, 83, 76),
            new Entry("Bahía Coral", "BAH", "Arena de la Bahía", C(1f, 0.45f, 0.4f), C(0.05f, 0.5f, 0.5f), C(0.25f, 0.25f, 0.3f), 73, 75, 70),
            new Entry("Titanes FC", "TIT", "Coliseo Titán", C(0.4f, 0.42f, 0.45f), C(1f, 0.5f, 0.1f), C(0.3f, 0.85f, 0.3f), 78, 71, 82),
            new Entry("Glaciar CF", "GLA", "Estadio Glaciar", C(0.8f, 0.92f, 1f), C(0.15f, 0.35f, 0.75f), C(0.95f, 0.3f, 0.3f), 71, 77, 80),
            new Entry("Cometa Rosa", "COM", "Órbita Arena", C(0.95f, 0.4f, 0.65f), C(0.08f, 0.08f, 0.08f), C(0.2f, 0.75f, 0.9f), 76, 79, 73),
        };

        public static int Count => Teams.Length;

        public static int Wrap(int index) => ((index % Count) + Count) % Count;

        public static string Name(int index) => Teams[Wrap(index)].Name;
        public static string ShortName(int index) => Teams[Wrap(index)].Short;
        public static string Stadium(int index) => Teams[Wrap(index)].Stadium;
        public static Color Primary(int index) => Teams[Wrap(index)].Primary;
        public static Color Secondary(int index) => Teams[Wrap(index)].Secondary;

        /// <summary>Crea los ajustes de un equipo del catálogo (instancia en memoria, no un asset).</summary>
        public static TeamSettings Create(int index, bool alternateKit = false)
        {
            Entry e = Teams[Wrap(index)];
            var settings = ScriptableObject.CreateInstance<TeamSettings>();
            settings.name = e.Name;
            settings.teamName = e.Name;
            settings.shortName = e.Short;
            settings.stadium = e.Stadium;
            settings.primaryColor = alternateKit ? AlternateShirt(e) : e.Primary;
            settings.secondaryColor = alternateKit ? e.Primary : e.Secondary;
            settings.goalkeeperColor = e.Keeper;
            settings.attack = e.Attack;
            settings.midfield = e.Midfield;
            settings.defense = e.Defense;
            return settings;
        }

        /// <summary>Segunda equipación: el color secundario, o blanco/negro si el secundario es poco vistoso.</summary>
        private static Color AlternateShirt(Entry e)
        {
            float saturation = Mathf.Max(e.Secondary.r, e.Secondary.g, e.Secondary.b) - Mathf.Min(e.Secondary.r, e.Secondary.g, e.Secondary.b);
            if (saturation > 0.25f) return e.Secondary;
            return Luminance(e.Primary) > 0.5f ? C(0.1f, 0.1f, 0.12f) : C(0.96f, 0.96f, 0.96f);
        }

        public static float Luminance(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

        /// <summary>¿Se confundirían dos camisetas en el campo?</summary>
        public static bool Clash(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db) < 0.42f;
        }

        /// <summary>
        /// Ajustes de local y visitante elegidos en el menú. Si el visitante es el mismo equipo, se pasa al siguiente;
        /// si sus colores chocan con los del local, viste la segunda equipación.
        /// </summary>
        public static void Resolve(out TeamSettings home, out TeamSettings away)
        {
            int homeIndex = Wrap(GameOptions.HomeTeamIndex);
            int awayIndex = Wrap(GameOptions.AwayTeamIndex);
            if (awayIndex == homeIndex) awayIndex = Wrap(awayIndex + 1);
            home = Create(homeIndex);
            away = Create(awayIndex);
            if (Clash(home.primaryColor, away.primaryColor))
            {
                Object.Destroy(away);
                away = Create(awayIndex, alternateKit: true);
            }
        }
    }

    /// <summary>
    /// Efecto moderado de las valoraciones del equipo en el juego (todas las fórmulas aquí, para poder ajustarlas):
    /// ataque → precisión del tiro y distancia de tiro de la IA; medio → velocidad; defensa → alcance de las
    /// entradas y reflejos del portero y de los defensores de la IA.
    /// </summary>
    public readonly struct TeamStrength
    {
        public readonly float Attack01, Midfield01, Defense01;

        public TeamStrength(TeamSettings settings)
        {
            Attack01 = settings != null ? Mathf.InverseLerp(60f, 90f, settings.attack) : 0.6f;
            Midfield01 = settings != null ? Mathf.InverseLerp(60f, 90f, settings.midfield) : 0.6f;
            Defense01 = settings != null ? Mathf.InverseLerp(60f, 90f, settings.defense) : 0.6f;
        }

        public float SpeedFactor => Mathf.Lerp(0.96f, 1.04f, Midfield01);
        public float ShotErrorFactor => Mathf.Lerp(1.2f, 0.8f, Attack01);
        public float TackleReachFactor => Mathf.Lerp(0.92f, 1.08f, Defense01);
        public float ReactionFactor => Mathf.Lerp(1.15f, 0.88f, Defense01);
        public float ShootRangeFactor => Mathf.Lerp(0.92f, 1.08f, Attack01);
    }
}
