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
        private const string CharacterPath = Folder + "/Jugador.fbx";
        private const string ControllerPath = Folder + "/FutbolTotal_Animator.controller";
        private const string KitMaterialsFolder = Folder + "/Materials";

        /// <summary>Animaciones esperadas: archivo, ¿en bucle?, trigger que la lanza (null = locomoción).</summary>
        private static readonly (string file, bool loop, string trigger)[] Clips =
        {
            ("Idle", true, null),
            ("Correr", true, null),
            ("Sprint", true, null),
            ("Pase", false, HumanoidAnimatorBridge.PassTrigger),
            ("Tiro", false, HumanoidAnimatorBridge.ShotTrigger),
            ("Entrada", false, HumanoidAnimatorBridge.TackleTrigger),
            ("Estirada", false, HumanoidAnimatorBridge.DiveTrigger),
            ("Celebracion", false, HumanoidAnimatorBridge.CelebrateTrigger),
        };

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
            if (settings == null || !settings.useImportedModel) return null;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath) == null) return null;

            Avatar avatar = ConfigureCharacter();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning("[Fútbol Total] Jugador.fbx no tiene un esqueleto humanoide válido; se usa el cuerpo generado.");
                return null;
            }

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var clip in Clips)
            {
                AnimationClip loaded = ConfigureAnimation(clip.file, clip.loop, avatar);
                if (loaded != null) clips[clip.file] = loaded;
                else Debug.LogWarning($"[Fútbol Total] Falta {Folder}/{clip.file}.fbx: esa animación no se usará.");
            }
            if (!clips.ContainsKey("Idle") && !clips.ContainsKey("Correr"))
            {
                Debug.LogWarning("[Fútbol Total] Hacen falta al menos Idle.fbx o Correr.fbx; se usa el cuerpo generado.");
                return null;
            }

            EditorAssetUtility.EnsureFolder(KitMaterialsFolder);
            var result = new Result(settings)
            {
                Model = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath),
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
            if (AssetImporter.GetAtPath(CharacterPath) is ModelImporter importer)
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
            return AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Avatar>().FirstOrDefault();
        }

        private static AnimationClip ConfigureAnimation(string file, bool loop, Avatar avatar)
        {
            string path = $"{Folder}/{file}.fbx";
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
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(HumanoidAnimatorBridge.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(HumanoidAnimatorBridge.MirrorDiveParam, AnimatorControllerParameterType.Bool);
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
        public static bool AttachTo(Transform visual, Result result, Color kitColor, string variant, CharacterSettings settings)
        {
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
            return true;
        }

        private static string Sanitize(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
