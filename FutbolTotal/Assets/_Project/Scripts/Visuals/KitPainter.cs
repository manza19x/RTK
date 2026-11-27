using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Pinta las equipaciones al empezar el partido con los colores del equipo elegido, sin crear materiales nuevos
    /// (bloques de propiedades): camiseta y medias del color principal, pantalón del secundario, portero con su
    /// color y dorsal blanco o casi negro según lo clara que sea la camiseta.
    /// Reconoce las piezas del cuerpo generado por su nombre y, en el personaje de Mixamo, los materiales de la
    /// equipación (los que llevan «Kit» o «Keeper» en el nombre).
    /// </summary>
    public static class KitPainter
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");
        private static readonly Color DarkNumber = new Color(0.08f, 0.08f, 0.1f);
        private static readonly Color LightNumber = new Color(0.97f, 0.97f, 0.97f);

        public static void Apply(Team team)
        {
            TeamSettings s = team.Settings;
            var block = new MaterialPropertyBlock();
            foreach (FootballPlayer player in team.Players)
            {
                Transform visual = player.transform.Find("Visual");
                if (visual == null) continue;

                Color shirt = player.IsGoalkeeper ? s.goalkeeperColor : s.primaryColor;
                Color shorts = player.IsGoalkeeper ? shirt * 0.55f : s.secondaryColor;
                shorts.a = 1f;
                Color number = TeamCatalog.Luminance(shirt) > 0.6f ? DarkNumber : LightNumber;

                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    switch (renderer.gameObject.name)
                    {
                        case "ShirtNumber": Paint(renderer, 0, number, block); continue;
                        case "Torso":
                        case "UpperArm":
                        case "Shin": Paint(renderer, 0, shirt, block); continue;
                        case "Shorts":
                        case "ShortsLeg": Paint(renderer, 0, shorts, block); continue;
                    }

                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null) continue;
                        string name = materials[i].name;
                        if (name.Contains("Kit") || name.Contains("Keeper")) Paint(renderer, i, shirt, block);
                    }
                }
            }
        }

        private static void Paint(Renderer renderer, int materialIndex, Color color, MaterialPropertyBlock block)
        {
            block.Clear();
            renderer.GetPropertyBlock(block, materialIndex);
            block.SetColor(BaseColor, color);
            block.SetColor(LegacyColor, color);
            renderer.SetPropertyBlock(block, materialIndex);
        }
    }
}
