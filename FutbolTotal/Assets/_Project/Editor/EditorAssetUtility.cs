using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FutbolTotal.EditorTools
{
    /// <summary>
    /// Utilidades del editor para crear assets temporales (materiales, físicas, ajustes) y asignar campos serializados.
    /// Los assets que ya existen se reutilizan sin tocarlos, para no perder ajustes hechos a mano.
    /// </summary>
    internal static class EditorAssetUtility
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static T LoadOrCreateSettings<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ------------------------------------------------------------------ Materiales

        private static bool IsUniversalPipeline
        {
            get
            {
                RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
                return pipeline != null && pipeline.GetType().Name.Contains("Universal");
            }
        }

        private static Shader FindShader(bool unlit)
        {
            Shader shader = IsUniversalPipeline
                ? Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                : Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[Fútbol Total] No se encontró un shader compatible (¿HDRP?). Usa URP o el pipeline integrado.");
            }
            return shader;
        }

        /// <summary>Material temporal de color liso. Fácil de sustituir por uno definitivo con el mismo nombre.</summary>
        public static Material LoadOrCreateMaterial(string folder, string name, Color color, bool unlit = false,
                                                    float smoothness = 0.2f, Texture2D texture = null, bool transparent = false)
        {
            string path = $"{folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var material = new Material(FindShader(unlit)) { name = name };
            material.color = color;
            if (texture != null) material.mainTexture = texture;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (transparent) MakeTransparent(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Activa transparencia en URP/Lit o Standard. Si falla, el material queda opaco (no rompe nada).</summary>
        private static void MakeTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        /// <summary>Textura de balón generada (manchas oscuras sobre blanco) para que se aprecie el giro.</summary>
        public static Texture2D LoadOrCreateBallTexture(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int width = 256;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color dark = new Color(0.12f, 0.12f, 0.14f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width * Mathf.PI * 2f * 3f;
                    float v = y / (float)height * Mathf.PI * 3f;
                    float pattern = Mathf.Sin(u) * Mathf.Sin(v);
                    texture.SetPixel(x, y, pattern > 0.6f ? dark : Color.white);
                }
            }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ Física

        public static PhysicsMaterial LoadOrCreatePhysicsMaterial(string folder, string name, float bounciness, float friction,
                                                                   PhysicsMaterialCombine bounceCombine,
                                                                   PhysicsMaterialCombine frictionCombine)
        {
            string path = $"{folder}/{name}.physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null) return existing;

            var material = new PhysicsMaterial(name)
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction,
                bounceCombine = bounceCombine,
                frictionCombine = frictionCombine
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------ Campos serializados

        private static SerializedProperty Find(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Fútbol Total] Campo '{field}' no encontrado en {serialized.targetObject.GetType().Name}.");
            }
            return property;
        }

        public static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = Find(serialized, field);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetReferences(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = Find(serialized, field);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Asigna un entero o un enum (por su valor numérico).</summary>
        public static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = Find(serialized, field);
            if (property == null) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Geometría

        /// <summary>Primitiva solo visual (sin collider).</summary>
        public static GameObject CreateVisual(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
                                              Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Caja con collider (y opcionalmente visible).</summary>
        public static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 size, Material material,
                                           PhysicsMaterial physics)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            else Object.DestroyImmediate(renderer);
            go.GetComponent<Collider>().sharedMaterial = physics;
            return go;
        }
    }
}
