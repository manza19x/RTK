using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FutbolTotal.EditorTools
{
    /// <summary>
    /// Prepara el personaje de Mixamo de Assets/_Project/Art/Mixamo para el juego:
    /// 1. Importa Jugador.fbx como Humanoid (crea su Avatar).
    /// 2. Importa cada animación con ese Avatar, en bucle las de locomoción y sin desplazamiento propio
    ///    (el movimiento lo hace el juego).
    /// 3. Crea el Animator Controller: mezcla quieto/trote/sprint según la velocidad y estados de pase, tiro,
    ///    entrada, estirada (con espejo) y celebración.
    /// 4. Crea materiales teñidos con los colores de cada equipo.
    /// Si falta Jugador.fbx, no hace nada y el juego usa el cuerpo generado por código.
    /// </summary>
    internal static class MixamoSetup
    {
        public const string Folder = "Assets/_Project/Art/Mixamo";
        private const string ControllerPath = Folder + "/FutbolTotal_Animator.controller";
        private const string KitMaterialsFolder = Folder + "/Materials";

        // Rutas detectadas en cada preparación (los archivos pueden tener el nombre que pone Mixamo).
        private static string characterPath;
        private static readonly Dictionary<string, string> clipPaths = new Dictionary<string, string>();

        /// <summary>
        /// Animaciones esperadas: nombre, ¿en bucle?, trigger que la lanza (null = locomoción) y palabras con las que
        /// se reconoce el archivo si no se renombró (por ejemplo «Soccer Pass.fbx»).
        /// </summary>
        private static readonly (string file, bool loop, string trigger, string[] keywords)[] Clips =
        {
            ("Idle", true, null, new[] { "idle" }),
            ("Sprint", true, null, new[] { "sprint", "fast run" }),
            ("Correr", true, null, new[] { "correr", "jog", "run" }),
            ("Pase", false, HumanoidAnimatorBridge.PassTrigger, new[] { "pase", "pass" }),
            ("Tiro", false, HumanoidAnimatorBridge.ShotTrigger, new[] { "tiro", "kick", "shot", "shoot", "strike", "penalty" }),
            ("Entrada", false, HumanoidAnimatorBridge.TackleTrigger, new[] { "entrada", "tackle", "slide" }),
            ("Estirada", false, HumanoidAnimatorBridge.DiveTrigger, new[] { "estirada", "goalkeeper", "keeper", "dive", "diving", "save" }),
            ("Celebracion", false, HumanoidAnimatorBridge.CelebrateTrigger, new[] { "celebr", "victory", "cheer" }),
        };

        /// <summary>
        /// Busca los FBX en cualquier carpeta llamada «Mixamo» dentro de Assets. El personaje es el FBX que tiene malla
        /// (las animaciones se descargan sin piel); cada animación se reconoce por su nombre en español o por palabras
        /// del nombre que pone Mixamo.
        /// </summary>
        private static void FindFiles()
        {
            characterPath = null;
            clipPaths.Clear();
            List<string> models = AssetDatabase.FindAssets("t:Model", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.ToLowerInvariant().Contains("/mixamo/") && p.ToLowerInvariant().EndsWith(".fbx"))
                .OrderBy(p => p)
                .ToList();

            foreach (string path in models)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null && model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0 &&
                    (characterPath == null || System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == "jugador"))
                {
                    characterPath = path;
                }
            }

            var used = new HashSet<string>();
            if (characterPath != null) used.Add(characterPath);
            // Primero los nombres exactos (Idle.fbx, Correr.fbx...), después por palabras clave.
            foreach (var clip in Clips)
            {
                string exact = models.FirstOrDefault(p => !used.Contains(p) &&
                    System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant() == clip.file.ToLowerInvariant());
                if (exact != null)
                {
                    clipPaths[clip.file] = exact;
                    used.Add(exact);
                }
            }
            foreach (var clip in Clips)
            {
                if (clipPaths.ContainsKey(clip.file)) continue;
                string match = models.FirstOrDefault(p => !used.Contains(p) &&
                    clip.keywords.Any(k => System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(k)));
                if (match != null)
                {
                    clipPaths[clip.file] = match;
                    used.Add(match);
                }
            }

            string found = string.Join(", ", clipPaths.Select(kv => $"{kv.Key} = {System.IO.Path.GetFileName(kv.Value)}"));
            string missing = string.Join(", ", Clips.Where(c => !clipPaths.ContainsKey(c.file)).Select(c => c.file));
            Debug.Log($"[Fútbol Total] Mixamo: {models.Count} FBX en carpetas «Mixamo». Personaje: " +
                      $"{(characterPath != null ? System.IO.Path.GetFileName(characterPath) : "NO ENCONTRADO")}. " +
                      $"Animaciones: {(found.Length > 0 ? found : "ninguna")}." +
                      (missing.Length > 0 ? $" Faltan: {missing}." : ""));
        }

        public sealed class Result
        {
            public GameObject Model;
            public RuntimeAnimatorController Controller;
            private readonly Dictionary<string, Material[]> kits = new Dictionary<string, Material[]>();
            private readonly CharacterSettings settings;

            public Result(CharacterSettings settings)
            {
                this.settings = settings;
            }

            /// <summary>Materiales del modelo con la equipación teñida (se crean una vez por color).</summary>
            public Material GetKitMaterial(Material original, Color kitColor, string variant)
            {
                if (original == null || !IsKitMaterial(original.name)) return original;
                string key = original.name + "_" + variant;
                if (kits.TryGetValue(key, out Material[] cached)) return cached[0];

                string path = $"{KitMaterialsFolder}/M_{Sanitize(original.name)}_{variant}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(original) { name = Sanitize(original.name) + "_" + variant };
                    material.color = kitColor;
                    AssetDatabase.CreateAsset(material, path);
                }
                kits[key] = new[] { material };
                return material;
            }

            /// <summary>¿Se tiñe este material? Por palabras clave; si no encaja ninguna, se tiñe todo lo que no sea piel/pelo.</summary>
            private bool IsKitMaterial(string name)
            {
                string lower = name.ToLowerInvariant();
                if (settings.keepMaterialKeywords.Any(k => !string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant()))) return false;
                if (settings.kitMaterialKeywords.Any(k => !string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant()))) return true;
                return KitFallback;
            }

            /// <summary>Si ningún material del modelo encaja con las palabras de equipación, se tiñen todos los "no piel".</summary>
            public bool KitFallback;
        }

        public static Result Prepare(CharacterSettings settings)
        {
            if (settings == null || !settings.useImportedModel)
            {
                Debug.Log("[Fútbol Total] Personaje importado desactivado en CharacterSettings: se usa el cuerpo generado.");
                return null;
            }
            FindFiles();
            if (characterPath == null)
            {
                Debug.LogWarning("[Fútbol Total] No hay personaje de Mixamo: pon el FBX del personaje (con piel) en una carpeta " +
                                 "llamada «Mixamo» dentro de Assets. Se usa el cuerpo generado.");
                return null;
            }

            Avatar avatar = ConfigureCharacter();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning("[Fútbol Total] Jugador.fbx no tiene un esqueleto humanoide válido; se usa el cuerpo generado.");
                return null;
            }

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var clip in Clips)
            {
                if (!clipPaths.TryGetValue(clip.file, out string clipPath)) continue;
                AnimationClip loaded = ConfigureAnimation(clipPath, clip.file, clip.loop, avatar);
                if (loaded != null) clips[clip.file] = loaded;
                else Debug.LogWarning($"[Fútbol Total] {System.IO.Path.GetFileName(clipPath)} no contiene una animación válida.");
            }
            if (!clips.ContainsKey("Idle") && !clips.ContainsKey("Correr"))
            {
                Debug.LogWarning("[Fútbol Total] Hacen falta al menos Idle.fbx o Correr.fbx; se usa el cuerpo generado.");
                return null;
            }

            EditorAssetUtility.EnsureFolder(KitMaterialsFolder);
            EditorAssetUtility.EnsureFolder(Folder);
            var result = new Result(settings)
            {
                Model = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath),
                Controller = BuildController(clips, settings)
            };

            // Informa de los materiales del modelo y decide si hace falta teñir "todo lo que no es piel".
            string[] names = result.Model.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().ToArray();
            bool anyKit = names.Any(n => settings.kitMaterialKeywords.Any(k => n.ToLowerInvariant().Contains(k.ToLowerInvariant())));
            result.KitFallback = !anyKit;
            Debug.Log($"[Fútbol Total] Personaje de Mixamo preparado. Materiales: {string.Join(", ", names)}. " +
                      (anyKit ? "Se tiñen los de la equipación." : "Ninguno parece de la equipación: se tiñen todos salvo piel y pelo."));
            return result;
        }

        private static Avatar ConfigureCharacter()
        {
            if (AssetImporter.GetAtPath(characterPath) is ModelImporter importer)
            {
                bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                               importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                               importer.importAnimation;
                if (changed)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = false;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAllAssetsAtPath(characterPath).OfType<Avatar>().FirstOrDefault();
        }

        private static AnimationClip ConfigureAnimation(string path, string file, bool loop, Avatar avatar)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return null;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = true;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = file;
                clip.loopTime = loop;
                clip.loopPose = loop;
                // Sin desplazamiento ni giro propio de la animación: el jugador lo mueve el código.
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        private static RuntimeAnimatorController BuildController(Dictionary<string, AnimationClip> clips, CharacterSettings settings)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            EditorAssetUtility.EnsureFolder(Folder);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(HumanoidAnimatorBridge.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(HumanoidAnimatorBridge.MirrorDiveParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = HumanoidAnimatorBridge.LocomotionSpeedParam,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            });
            foreach (var clip in Clips)
            {
                if (clip.trigger != null) controller.AddParameter(clip.trigger, AnimatorControllerParameterType.Trigger);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            // Locomoción: mezcla según la velocidad (0 = quieto, jogBlendPoint = trote, 1 = sprint).
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomocion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = HumanoidAnimatorBridge.SpeedParam;
            tree.useAutomaticThresholds = false;
            AnimationClip idle = clips.TryGetValue("Idle", out AnimationClip i) ? i : clips["Correr"];
            AnimationClip jog = clips.TryGetValue("Correr", out AnimationClip j) ? j : idle;
            AnimationClip sprint = clips.TryGetValue("Sprint", out AnimationClip s) ? s : jog;
            tree.AddChild(idle, 0f);
            tree.AddChild(jog, settings.jogBlendPoint);
            tree.AddChild(sprint, 1f);
            machine.defaultState = locomotion;
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = HumanoidAnimatorBridge.LocomotionSpeedParam;

            // Acciones: desde cualquier estado al recibir su trigger; vuelven a la locomoción al acabar.
            foreach (var clip in Clips)
            {
                if (clip.trigger == null || !clips.TryGetValue(clip.file, out AnimationClip motion)) continue;
                AnimatorState state = machine.AddState(clip.file);
                state.motion = motion;
                if (clip.file == "Estirada")
                {
                    state.mirrorParameterActive = true;
                    state.mirrorParameter = HumanoidAnimatorBridge.MirrorDiveParam;
                }

                AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
                enter.AddCondition(AnimatorConditionMode.If, 0f, clip.trigger);
                enter.duration = 0.08f;
                enter.canTransitionToSelf = false;

                AnimatorStateTransition exit = state.AddTransition(locomotion);
                exit.hasExitTime = true;
                exit.exitTime = clip.file == "Celebracion" ? 0.95f : 0.8f;
                exit.duration = 0.2f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>
        /// Coloca el modelo bajo <paramref name="visual"/>, lo escala a la altura deseada, le pone el Animator
        /// y tiñe la equipación. Devuelve false si no se pudo.
        /// </summary>
        public static bool AttachTo(Transform visual, Result result, Color kitColor, string variant, CharacterSettings settings,
                                    out Transform chest)
        {
            chest = null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(result.Model, visual);
            if (instance == null) return false;
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Escala por altura real del modelo (Mixamo puede venir en centímetros).
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                if (bounds.size.y > 0.01f) instance.transform.localScale = Vector3.one * (settings.modelHeight / bounds.size.y);
            }

            foreach (Renderer r in renderers)
            {
                Material[] materials = r.sharedMaterials;
                for (int m = 0; m < materials.Length; m++) materials[m] = result.GetKitMaterial(materials[m], kitColor, variant);
                r.sharedMaterials = materials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = result.Controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.isHuman)
            {
                chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            }
            return true;
        }

        private static string Sanitize(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
