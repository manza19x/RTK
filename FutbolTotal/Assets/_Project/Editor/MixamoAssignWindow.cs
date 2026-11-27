using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FutbolTotal.EditorTools
{
    /// <summary>
    /// Ventana para decir qué es cada FBX descargado de Mixamo cuando todos tienen el mismo nombre
    /// (Mixamo los llama por el código del personaje, p. ej. «Ch38_nonPBR (3).fbx»).
    /// Para cada archivo: «Ver» lo selecciona para reproducirlo en la vista previa del Inspector, y un desplegable
    /// asigna su papel. «Aplicar» renombra los archivos (Jugador, Idle, Correr...) para que el generador los use.
    /// Menú: Fútbol Total ▸ Asignar animaciones de Mixamo.
    /// </summary>
    public class MixamoAssignWindow : EditorWindow
    {
        private static readonly string[] Roles =
            { "— (no usar)", "Idle", "Correr", "Sprint", "Pase", "Tiro", "Entrada", "Estirada", "Celebracion" };

        private static readonly string[] RoleHelp =
        {
            "", "quieto", "trote", "carrera rápida", "pase", "chute fuerte", "entrada en plancha", "estirada del portero", "celebración"
        };

        private class Entry
        {
            public string Path;
            public bool IsCharacter;
            public float Length;
            public int Role;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private Vector2 scroll;
        private string message;

        [MenuItem("Fútbol Total/Asignar animaciones de Mixamo", priority = 30)]
        public static void Open()
        {
            var window = GetWindow<MixamoAssignWindow>(true, "Asignar animaciones de Mixamo");
            window.minSize = new Vector2(620f, 480f);
            window.Refresh();
        }

        private void Refresh()
        {
            entries.Clear();
            message = null;
            IEnumerable<string> paths = AssetDatabase.FindAssets("t:Model", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.ToLowerInvariant().Contains("/mixamo/") && p.ToLowerInvariant().EndsWith(".fbx"))
                .OrderBy(p => p, new NaturalComparer());

            foreach (string path in paths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool isCharacter = model != null && model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0;
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                string name = Path.GetFileNameWithoutExtension(path);
                int role = System.Array.FindIndex(Roles, r => r.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                entries.Add(new Entry
                {
                    Path = path,
                    IsCharacter = isCharacter,
                    Length = clip != null ? clip.length : 0f,
                    Role = role > 0 ? role : 0
                });
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1. Pulsa «Ver» en un archivo: se selecciona y en el Inspector (abajo del todo) aparece la vista previa; " +
                "pulsa ▶ para ver la animación.\n" +
                "2. Elige en el desplegable qué es. Cada papel solo puede usarse una vez; los repetidos déjalos en «no usar».\n" +
                "3. Pulsa «Aplicar» y después «Fútbol Total ▸ Crear todas las escenas».", MessageType.Info);

            if (GUILayout.Button("Volver a buscar archivos")) Refresh();
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay archivos FBX en ninguna carpeta llamada «Mixamo» dentro de Assets.", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (Entry entry in entries)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ver", GUILayout.Width(50f)))
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(entry.Path);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.LabelField(Path.GetFileName(entry.Path), GUILayout.Width(220f));
                if (entry.IsCharacter)
                {
                    EditorGUILayout.LabelField("PERSONAJE (se llamará Jugador)", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(entry.Length > 0f ? $"{entry.Length:0.0} s" : "", GUILayout.Width(50f));
                    entry.Role = EditorGUILayout.Popup(entry.Role, Roles.Select((r, i) => i == 0 ? r : $"{r}  ({RoleHelp[i]})").ToArray());
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            string[] missing = Roles.Skip(1).Where((r, i) => entries.All(e => e.Role != i + 1)).ToArray();
            if (missing.Length > 0) EditorGUILayout.HelpBox("Sin asignar: " + string.Join(", ", missing), MessageType.None);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);

            if (GUILayout.Button("Aplicar", GUILayout.Height(32f))) Apply();
        }

        private void Apply()
        {
            var duplicates = entries.Where(e => !e.IsCharacter && e.Role > 0).GroupBy(e => e.Role).Where(g => g.Count() > 1).ToList();
            if (duplicates.Count > 0)
            {
                message = "Hay papeles repetidos: " + string.Join(", ", duplicates.Select(g => Roles[g.Key])) +
                          ". Deja uno de cada y el resto en «no usar».";
                return;
            }

            var targets = new List<(Entry entry, string name)>();
            Entry character = entries.FirstOrDefault(e => e.IsCharacter);
            if (character != null) targets.Add((character, "Jugador"));
            targets.AddRange(entries.Where(e => !e.IsCharacter && e.Role > 0).Select(e => (e, Roles[e.Role])));

            // Primero se aparta cualquier archivo que ya tenga uno de los nombres finales, para no chocar al renombrar.
            var finalNames = new HashSet<string>(targets.Select(t => t.name.ToLowerInvariant()));
            int temp = 0;
            foreach (Entry entry in entries)
            {
                string name = Path.GetFileNameWithoutExtension(entry.Path).ToLowerInvariant();
                if (!finalNames.Contains(name)) continue;
                string error = AssetDatabase.RenameAsset(entry.Path, $"_tmp_mixamo_{temp++}");
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning("[Fútbol Total] " + error);
                else entry.Path = Path.GetDirectoryName(entry.Path).Replace('\\', '/') + $"/_tmp_mixamo_{temp - 1}.fbx";
            }

            int renamed = 0;
            foreach ((Entry entry, string name) in targets)
            {
                string error = AssetDatabase.RenameAsset(entry.Path, name);
                if (string.IsNullOrEmpty(error)) renamed++;
                else Debug.LogWarning($"[Fútbol Total] No se pudo renombrar {entry.Path}: {error}");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Refresh();
            message = $"Listo: {renamed} archivos renombrados. Ahora usa «Fútbol Total ▸ Crear todas las escenas».";
            Debug.Log("[Fútbol Total] " + message);
        }

        /// <summary>Orden natural: «(2)» antes que «(10)».</summary>
        private class NaturalComparer : IComparer<string>
        {
            public int Compare(string a, string b) => EditorUtility.NaturalCompare(a, b);
        }
    }
}
