using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static FutbolTotal.EditorTools.EditorAssetUtility;

namespace FutbolTotal.EditorTools
{
    /// <summary>
    /// Genera las escenas del juego con un solo clic: campo, porterías, balón, dos equipos de 7, cámara,
    /// indicador de jugador activo y HUD. También crea los assets de ajustes y los materiales temporales si no
    /// existen (si existen, los reutiliza sin modificarlos).
    /// - Partido: reglas completas con <see cref="MatchManager"/> (reloj, saques, descanso, final).
    /// - Entrenamiento: sin reloj ni saques, con <see cref="TrainingHarness"/>, para probar controles y física.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Root = "Assets/_Project";
        // Los ajustes viven en una carpeta Resources para que el juego pueda cargarlos por nombre (SettingsProvider).
        private const string SettingsFolder = Root + "/Resources/" + SettingsProvider.ResourcesFolder;
        private const string LegacySettingsFolder = Root + "/Settings";
        private const string MaterialsFolder = Root + "/Art/Materials";
        private const string TexturesFolder = Root + "/Art/Textures";
        private const string PhysicsFolder = Root + "/Art/Physics";
        private const string ScenesFolder = Root + "/Scenes";
        // Los nombres de archivo deben coincidir con SceneNames (el juego carga las escenas por nombre).
        private const string MainMenuScenePath = ScenesFolder + "/" + SceneNames.MainMenu + ".unity";
        private const string MatchScenePath = ScenesFolder + "/" + SceneNames.Match + ".unity";
        private const string TrainingScenePath = ScenesFolder + "/" + SceneNames.Training + ".unity";

        private sealed class Assets
        {
            public PlayerMovementSettings Movement;
            public BallSettings Ball;
            public ActionSettings Actions;
            public CameraSettings Camera;
            public MatchSettings Match;
            public AISettings AI;
            public SoundSettings Sound;
            public FormationSettings Formation;
            public TeamSettings Home;
            public TeamSettings Away;

            public Material Grass, GrassStripes, Lines, Surround, GoalFrame, Net, BallMaterial, Indicator, Skin;
            public Material HomeKit, HomeTrim, HomeKeeper, AwayKit, AwayTrim, AwayKeeper;

            public PhysicsMaterial GrassPhysics, BallPhysics, NetPhysics, PostPhysics, WallPhysics;

            // Versión visual mejorada (Fase 6).
            public Material Concrete, Seats, Roof, Board, Tower, Lamp, Boots, Sky;
            public Material[] Skins, Hairs;
        }

        [MenuItem("Fútbol Total/Crear todas las escenas", priority = 0)]
        public static void BuildAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(true);
            Build(false);
            BuildMainMenu(); // La última, para dejarla abierta: es la que arranca el juego.
            Debug.Log("[Fútbol Total] Escenas creadas: menú principal, partido y entrenamiento. Pulsa Play para empezar desde el menú.");
        }

        [MenuItem("Fútbol Total/Crear escena de menú principal", priority = 11)]
        public static void BuildMainMenuScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildMainMenu();
        }

        [MenuItem("Fútbol Total/Crear escena de partido", priority = 12)]
        public static void BuildMatchScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build(true);
        }

        [MenuItem("Fútbol Total/Crear escena de entrenamiento", priority = 13)]
        public static void BuildTrainingScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build(false);
        }

        /// <summary>Menú principal: el campo de fondo con una cámara que gira y la interfaz del menú.</summary>
        private static void BuildMainMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Assets assets = CreateAssets();
            ConfigureLight(assets);
            BuildPitch(assets, out _);

            GameObject cameraGo = GameObject.Find("Main Camera");
            if (cameraGo != null)
            {
                var camera = cameraGo.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 45f;
                camera.farClipPlane = 600f;
                cameraGo.AddComponent<MenuCameraOrbit>();
            }

            new GameObject("MainMenu").AddComponent<MainMenu>();

            EditorAssetUtility.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AddSceneToBuildSettings(MainMenuScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fútbol Total] Menú principal creado en {MainMenuScenePath}.");
        }

        private static void Build(bool match)
        {
            string scenePath = match ? MatchScenePath : TrainingScenePath;

            // Primero la escena y después los assets: abrir una escena en modo Single descarga de memoria los assets
            // no usados, y las referencias obtenidas antes podrían guardarse vacías.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Assets assets = CreateAssets();

            ConfigureLight(assets);

            var matchGo = new GameObject("Match");
            var context = matchGo.AddComponent<MatchContext>();

            Pitch pitch = BuildPitch(assets, out GoalZone[] goals);
            Ball ball = BuildBall(assets, pitch);

            Team home = BuildTeam("Team_Home", TeamSide.Home, 1, assets.Home, assets, pitch,
                                  assets.HomeKit, assets.HomeTrim, assets.HomeKeeper);
            Team away = BuildTeam("Team_Away", TeamSide.Away, -1, assets.Away, assets, pitch,
                                  assets.AwayKit, assets.AwayTrim, assets.AwayKeeper);

            SetReference(context, "ball", ball);
            SetReference(context, "pitch", pitch);
            SetReference(context, "homeTeam", home);
            SetReference(context, "awayTeam", away);

            // Control humano: equipo local.
            var input = home.gameObject.AddComponent<FootballInput>();
            var human = home.gameObject.AddComponent<HumanTeamController>();
            SetReference(human, "input", input);
            SetReference(human, "actionSettings", assets.Actions);

            MatchCamera matchCamera = BuildCamera(assets, human);
            SetReference(human, "cameraTransform", matchCamera.transform);

            BuildSelectionIndicator(assets, human);

            // Reglas: partido completo o entrenamiento libre. Ambos implementan IMatchStatus para el HUD.
            MonoBehaviour rules;
            if (match)
            {
                var manager = matchGo.AddComponent<MatchManager>();
                SetReference(manager, "settings", assets.Match);
                SetReference(manager, "human", human);
                SetReferences(manager, "goals", goals);
                SetReference(manager, "matchCamera", matchCamera);
                rules = manager;
            }
            else
            {
                var harness = matchGo.AddComponent<TrainingHarness>();
                SetReference(harness, "human", human);
                SetReferences(harness, "goals", goals);
                SetReference(harness, "matchCamera", matchCamera);
                rules = harness;
            }

            var hud = matchGo.AddComponent<DebugHud>();
            SetReference(hud, "human", human);
            SetReference(hud, "statusSource", rules);

            // Sonido del partido (sonidos generados por código; ver ProceduralAudio).
            var audioGo = new GameObject("Audio");
            var matchAudio = audioGo.AddComponent<MatchAudio>();
            SetReference(matchAudio, "settings", assets.Sound);

            // Interfaz de juego (se construye sola al arrancar; ver UIFactory).
            var ui = new GameObject("UI");
            ui.AddComponent<MatchHud>();
            ui.AddComponent<PauseMenu>();
            if (match)
            {
                matchGo.AddComponent<MatchStats>();
                ui.AddComponent<ResultScreen>();
            }

            EditorAssetUtility.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, scenePath);
            AddSceneToBuildSettings(scenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = matchGo;
            int missing = CountMissingSettings();
            if (missing == 0)
            {
                Debug.Log($"[Fútbol Total] Escena creada en {scenePath}. Todas las referencias a ajustes son correctas. Pulsa Play para probarla.");
            }
            else
            {
                Debug.LogError($"[Fútbol Total] Escena creada, pero {missing} referencias a assets de ajustes están vacías. " +
                               "El juego los cargará igualmente desde Assets/_Project/Resources/FutbolTotal.");
            }
        }

        /// <summary>Comprueba que cada componente de la escena guarda su referencia al asset de ajustes.</summary>
        private static int CountMissingSettings()
        {
            int missing = 0;
            void Check(Object component, string field)
            {
                if (component == null) return;
                SerializedProperty property = new SerializedObject(component).FindProperty(field);
                if (property != null && property.objectReferenceValue != null) return;
                missing++;
                Debug.LogWarning($"[Fútbol Total] Referencia vacía: {component.GetType().Name}.{field} en '{component.name}'.", component);
            }

            foreach (var motor in Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None)) Check(motor, "settings");
            foreach (var stamina in Object.FindObjectsByType<PlayerStamina>(FindObjectsSortMode.None)) Check(stamina, "settings");
            foreach (var actions in Object.FindObjectsByType<PlayerActions>(FindObjectsSortMode.None)) Check(actions, "settings");
            foreach (var ball in Object.FindObjectsByType<Ball>(FindObjectsSortMode.None)) Check(ball, "settings");
            foreach (var camera in Object.FindObjectsByType<MatchCamera>(FindObjectsSortMode.None)) Check(camera, "settings");
            foreach (var manager in Object.FindObjectsByType<MatchManager>(FindObjectsSortMode.None)) Check(manager, "settings");
            foreach (var teamAI in Object.FindObjectsByType<TeamAI>(FindObjectsSortMode.None)) Check(teamAI, "settings");
            foreach (var team in Object.FindObjectsByType<Team>(FindObjectsSortMode.None))
            {
                Check(team, "settings");
                Check(team, "formation");
            }
            return missing;
        }

        // ================================================================== Assets

        private static Assets CreateAssets()
        {
            EnsureFolder(SettingsFolder);
            MigrateLegacySettings();
            EnsureFolder(MaterialsFolder);
            EnsureFolder(TexturesFolder);
            EnsureFolder(PhysicsFolder);

            var a = new Assets
            {
                Movement = LoadOrCreateSettings<PlayerMovementSettings>(SettingsPath(SettingsProvider.MovementName), null),
                Ball = LoadOrCreateSettings<BallSettings>(SettingsPath(SettingsProvider.BallName), null),
                Actions = LoadOrCreateSettings<ActionSettings>(SettingsPath(SettingsProvider.ActionsName), null),
                Camera = LoadOrCreateSettings<CameraSettings>(SettingsPath(SettingsProvider.CameraName), null),
                Match = LoadOrCreateSettings<MatchSettings>(SettingsPath(SettingsProvider.MatchName), null),
                AI = LoadOrCreateSettings<AISettings>(SettingsPath(SettingsProvider.AIName), null),
                Sound = LoadOrCreateSettings<SoundSettings>(SettingsPath(SettingsProvider.SoundName), null),
                Formation = LoadOrCreateSettings<FormationSettings>(SettingsPath(SettingsProvider.FormationName),
                                                                    f => f.FillDefaultSevenASide()),
                // Equipos ficticios con nombres y colores originales.
                Home = LoadOrCreateSettings<TeamSettings>(SettingsPath(SettingsProvider.HomeTeamName), t =>
                {
                    t.teamName = "Club Aurora";
                    t.shortName = "AUR";
                    t.primaryColor = new Color(0.86f, 0.24f, 0.18f);
                    t.secondaryColor = new Color(0.98f, 0.95f, 0.88f);
                    t.goalkeeperColor = new Color(0.95f, 0.8f, 0.15f);
                }),
                Away = LoadOrCreateSettings<TeamSettings>(SettingsPath(SettingsProvider.AwayTeamName), t =>
                {
                    t.teamName = "Deportivo Marea";
                    t.shortName = "MAR";
                    t.primaryColor = new Color(0.16f, 0.36f, 0.82f);
                    t.secondaryColor = new Color(0.1f, 0.12f, 0.2f);
                    t.goalkeeperColor = new Color(0.35f, 0.85f, 0.45f);
                })
            };


            CreateVisualAssets(a);
            a.GoalFrame = LoadOrCreateMaterial(MaterialsFolder, "M_GoalFrame", Color.white, smoothness: 0.6f);
            a.Indicator = LoadOrCreateMaterial(MaterialsFolder, "M_SelectionIndicator", new Color(1f, 0.9f, 0.2f), unlit: true);
            a.Skin = LoadOrCreateMaterial(MaterialsFolder, "M_Skin", new Color(0.85f, 0.68f, 0.52f));

            a.HomeKit = LoadOrCreateMaterial(MaterialsFolder, "M_Home_Kit", a.Home.primaryColor);
            a.HomeTrim = LoadOrCreateMaterial(MaterialsFolder, "M_Home_Trim", a.Home.secondaryColor);
            a.HomeKeeper = LoadOrCreateMaterial(MaterialsFolder, "M_Home_Keeper", a.Home.goalkeeperColor);
            a.AwayKit = LoadOrCreateMaterial(MaterialsFolder, "M_Away_Kit", a.Away.primaryColor);
            a.AwayTrim = LoadOrCreateMaterial(MaterialsFolder, "M_Away_Trim", a.Away.secondaryColor);
            a.AwayKeeper = LoadOrCreateMaterial(MaterialsFolder, "M_Away_Keeper", a.Away.goalkeeperColor);

            // El balón tiene rozamiento 0 y combinación "Minimum": rueda con nuestra desaceleración, no con la de PhysX.
            a.BallPhysics = LoadOrCreatePhysicsMaterial(PhysicsFolder, "PM_Ball", 0.6f, 0f,
                                                        PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Minimum);
            a.GrassPhysics = LoadOrCreatePhysicsMaterial(PhysicsFolder, "PM_Grass", 0.55f, 0f,
                                                         PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Minimum);
            // La red absorbe el bote (Minimum tiene prioridad sobre Average) y frena por rozamiento (Maximum).
            a.NetPhysics = LoadOrCreatePhysicsMaterial(PhysicsFolder, "PM_Net", 0f, 0.6f,
                                                       PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Maximum);
            a.PostPhysics = LoadOrCreatePhysicsMaterial(PhysicsFolder, "PM_Post", 0.5f, 0f,
                                                        PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Minimum);
            a.WallPhysics = LoadOrCreatePhysicsMaterial(PhysicsFolder, "PM_Wall", 0.2f, 0f,
                                                        PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Minimum);

            AssetDatabase.SaveAssets();
            return a;
        }

        private static string SettingsPath(string assetName) => $"{SettingsFolder}/{assetName}.asset";

        /// <summary>
        /// Texturas generadas (césped con relieve, balón, vallas) y materiales de la versión visual mejorada.
        /// Usan nombres nuevos (sufijo _HD) para no depender de materiales de versiones anteriores.
        /// </summary>
        private static void CreateVisualAssets(Assets a)
        {
            var pitchProbe = new GameObject("PitchProbe").AddComponent<Pitch>(); // Medidas por defecto del campo.
            float surfaceX = pitchProbe.Length + 3f, surfaceZ = pitchProbe.Width + 3f;
            Object.DestroyImmediate(pitchProbe.gameObject);
            const float grassTile = 4f; // metros por repetición de la textura

            Texture2D grass = LoadOrCreateTexture(TexturesFolder + "/Grass_Albedo.png", () => ProceduralTextures.GrassAlbedo(1024));
            Texture2D grassNormal = LoadOrCreateTexture(TexturesFolder + "/Grass_Normal.png", () => ProceduralTextures.GrassNormal(1024), normalMap: true);
            Texture2D ball = LoadOrCreateTexture(TexturesFolder + "/Ball_HD.png", () => ProceduralTextures.Ball(1024, 512));
            Texture2D board = LoadOrCreateTexture(TexturesFolder + "/AdBoard.png",
                () => ProceduralTextures.AdBoard(512, 64, new Color(0.05f, 0.35f, 0.9f), new Color(0.95f, 0.85f, 0.2f), 1));

            a.Grass = LoadOrCreateMaterial(MaterialsFolder, "M_Grass_HD", new MaterialSpec
            {
                Albedo = grass, Normal = grassNormal, NormalScale = 0.6f, Smoothness = 0.12f,
                Tiling = new Vector2(surfaceX / grassTile, surfaceZ / grassTile)
            });
            a.GrassStripes = LoadOrCreateMaterial(MaterialsFolder, "M_GrassStripes_HD", new MaterialSpec
            {
                Albedo = grass, Normal = grassNormal, NormalScale = 0.6f, Smoothness = 0.12f,
                Color = new Color(0.8f, 0.86f, 0.8f), Tiling = Vector2.one // la malla de franjas ya trae UV en metros/4
            });
            a.Surround = LoadOrCreateMaterial(MaterialsFolder, "M_Surround_HD", new MaterialSpec
            {
                Albedo = grass, Normal = grassNormal, Smoothness = 0.08f, Color = new Color(0.62f, 0.68f, 0.6f),
                Tiling = new Vector2(40f, 30f)
            });
            a.Lines = LoadOrCreateMaterial(MaterialsFolder, "M_Lines_HD", new MaterialSpec { Color = new Color(0.93f, 0.94f, 0.92f), Smoothness = 0.1f });
            a.Net = LoadOrCreateMaterial(MaterialsFolder, "M_Net_HD", new MaterialSpec { Color = new Color(0.95f, 0.95f, 0.95f), Smoothness = 0.15f, DoubleSided = true });
            a.BallMaterial = LoadOrCreateMaterial(MaterialsFolder, "M_Ball_HD", new MaterialSpec { Albedo = ball, Smoothness = 0.55f });

            a.Concrete = LoadOrCreateMaterial(MaterialsFolder, "M_Concrete", new MaterialSpec { Color = new Color(0.58f, 0.58f, 0.6f), Smoothness = 0.15f });
            a.Seats = LoadOrCreateMaterial(MaterialsFolder, "M_Seats", new MaterialSpec { Color = new Color(0.16f, 0.22f, 0.34f), Smoothness = 0.35f });
            a.Roof = LoadOrCreateMaterial(MaterialsFolder, "M_Roof", new MaterialSpec { Color = new Color(0.32f, 0.34f, 0.38f), Smoothness = 0.55f, Metallic = 0.6f });
            a.Tower = LoadOrCreateMaterial(MaterialsFolder, "M_Tower", new MaterialSpec { Color = new Color(0.45f, 0.46f, 0.5f), Smoothness = 0.4f, Metallic = 0.5f });
            a.Lamp = LoadOrCreateMaterial(MaterialsFolder, "M_Lamp", new MaterialSpec { Color = Color.white, Emission = new Color(6f, 5.8f, 5.2f) });
            a.Board = LoadOrCreateMaterial(MaterialsFolder, "M_AdBoard", new MaterialSpec
            {
                Albedo = board, Emission = new Color(1.4f, 1.4f, 1.4f), EmissionMap = board, Smoothness = 0.6f
            });
            a.Boots = LoadOrCreateMaterial(MaterialsFolder, "M_Boots", new MaterialSpec { Color = new Color(0.07f, 0.07f, 0.08f), Smoothness = 0.6f });

            Color[] skinTones = { new Color(0.93f, 0.76f, 0.62f), new Color(0.76f, 0.56f, 0.4f), new Color(0.45f, 0.3f, 0.2f) };
            Color[] hairColors = { new Color(0.08f, 0.06f, 0.05f), new Color(0.3f, 0.18f, 0.1f), new Color(0.75f, 0.6f, 0.35f), new Color(0.15f, 0.12f, 0.1f) };
            a.Skins = new Material[skinTones.Length];
            for (int i = 0; i < skinTones.Length; i++)
                a.Skins[i] = LoadOrCreateMaterial(MaterialsFolder, $"M_Skin_{i}", new MaterialSpec { Color = skinTones[i], Smoothness = 0.3f });
            a.Hairs = new Material[hairColors.Length];
            for (int i = 0; i < hairColors.Length; i++)
                a.Hairs[i] = LoadOrCreateMaterial(MaterialsFolder, $"M_Hair_{i}", new MaterialSpec { Color = hairColors[i], Smoothness = 0.25f });

            a.Sky = LoadOrCreateSkybox(MaterialsFolder);
        }

        /// <summary>
        /// Versiones anteriores guardaban los ajustes en _Project/Settings. Se mueven a Resources conservando
        /// los valores que se hubieran cambiado a mano.
        /// </summary>
        private static void MigrateLegacySettings()
        {
            if (!AssetDatabase.IsValidFolder(LegacySettingsFolder)) return;
            string[] names =
            {
                SettingsProvider.MovementName, SettingsProvider.BallName, SettingsProvider.ActionsName,
                SettingsProvider.CameraName, SettingsProvider.FormationName,
                SettingsProvider.HomeTeamName, SettingsProvider.AwayTeamName
            };
            foreach (string name in names)
            {
                string from = $"{LegacySettingsFolder}/{name}.asset";
                string to = SettingsPath(name);
                if (AssetDatabase.LoadMainAssetAtPath(from) == null || AssetDatabase.LoadMainAssetAtPath(to) != null) continue;
                string error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"[Fútbol Total] No se pudo mover {from}: {error}");
            }
            if (AssetDatabase.FindAssets(string.Empty, new[] { LegacySettingsFolder }).Length == 0)
            {
                AssetDatabase.DeleteAsset(LegacySettingsFolder);
            }
        }

        // ================================================================== Escena

        /// <summary>Sol de tarde, cielo procedural, luz ambiental del cielo y una ligera neblina a lo lejos.</summary>
        private static void ConfigureLight(Assets a)
        {
            GameObject lightGo = GameObject.Find("Directional Light");
            Light light = lightGo != null ? lightGo.GetComponent<Light>() : null;
            if (light != null)
            {
                lightGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.85f;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.95f, 0.86f);
                RenderSettings.sun = light;
            }
            if (a.Sky != null)
            {
                RenderSettings.skybox = a.Sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1f;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.73f, 0.82f);
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 450f;
            DynamicGI.UpdateEnvironment();

            // Posprocesado y calidad de URP (ensamblado opcional: solo existe si el proyecto usa URP).
            System.Type quality = System.Type.GetType("FutbolTotal.VisualQualitySetup, FutbolTotal.Visuals.URP");
            if (quality != null) new GameObject("VisualQuality").AddComponent(quality);
            else Debug.Log("[Fútbol Total] URP no detectado: la escena se crea sin posprocesado.");
        }

        private static Pitch BuildPitch(Assets a, out GoalZone[] goals)
        {
            var pitchGo = new GameObject("Pitch");
            var pitch = pitchGo.AddComponent<Pitch>();
            float L = pitch.HalfLength;
            float W = pitch.HalfWidth;

            // Suelo físico: un único collider invisible cuya cara superior está en y = 0.
            const float surround = 7f;
            Vector3 groundSize = new Vector3(pitch.Length + surround * 2f, 1f, pitch.Width + surround * 2f);
            GameObject ground = CreateBox("GroundCollider", pitchGo.transform, new Vector3(0f, -0.5f, 0f), groundSize,
                                          null, a.GrassPhysics);
            ground.isStatic = true;

            // Capas visuales separadas unos milímetros en altura para evitar parpadeos (z-fighting).
            CreateVisual(PrimitiveType.Cube, "Surround", pitchGo.transform, new Vector3(0f, -0.06f, 0f),
                         new Vector3(pitch.Length + 120f, 0.1f, pitch.Width + 120f), a.Surround);
            CreateVisual(PrimitiveType.Cube, "PlayingSurface", pitchGo.transform, new Vector3(0f, -0.05f, 0f),
                         new Vector3(pitch.Length + 3f, 0.1f, pitch.Width + 3f), a.Grass);
            CreateMarkings("Stripes", pitchGo.transform, pitch, PitchMeshGenerator.Mode.Stripes, a.GrassStripes, 0.008f);
            CreateMarkings("Lines", pitchGo.transform, pitch, PitchMeshGenerator.Mode.Lines, a.Lines, 0.016f);

            // Límites invisibles: el balón no sale del recinto en la Fase 1 (en la Fase 2 habrá saques de banda y de puerta).
            var bounds = new GameObject("Boundaries").transform;
            bounds.SetParent(pitchGo.transform, false);
            float wallHeight = 30f;
            float outerX = L + surround;
            float outerZ = W + surround;
            CreateBox("Wall_North", bounds, new Vector3(0f, wallHeight * 0.5f, outerZ + 0.5f), new Vector3(outerX * 2f + 2f, wallHeight, 1f), null, a.WallPhysics);
            CreateBox("Wall_South", bounds, new Vector3(0f, wallHeight * 0.5f, -outerZ - 0.5f), new Vector3(outerX * 2f + 2f, wallHeight, 1f), null, a.WallPhysics);
            CreateBox("Wall_East", bounds, new Vector3(outerX + 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, outerZ * 2f + 2f), null, a.WallPhysics);
            CreateBox("Wall_West", bounds, new Vector3(-outerX - 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, outerZ * 2f + 2f), null, a.WallPhysics);

            goals = new[]
            {
                BuildGoal(pitchGo.transform, pitch, -1, a),
                BuildGoal(pitchGo.transform, pitch, 1, a)
            };
            BuildStadium(pitchGo.transform, pitch, a);
            return pitch;
        }

        /// <summary>Gradas, cubiertas, vallas, torres de focos y público (mallas generadas por StadiumGenerator).</summary>
        private static void BuildStadium(Transform parent, Pitch pitch, Assets a)
        {
            var stadiumGo = new GameObject("Stadium");
            stadiumGo.transform.SetParent(parent, false);
            (string name, Material material, bool shadows)[] parts =
            {
                ("Concrete", a.Concrete, true), ("Seats", a.Seats, true), ("Roof", a.Roof, true),
                ("Boards", a.Board, false), ("Towers", a.Tower, true), ("Lamps", a.Lamp, false)
            };
            foreach (var part in parts)
            {
                var child = new GameObject(part.name);
                child.transform.SetParent(stadiumGo.transform, false);
                child.AddComponent<MeshFilter>();
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = part.material;
                renderer.shadowCastingMode = part.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            var generator = stadiumGo.AddComponent<StadiumGenerator>();
            SetReference(generator, "pitch", pitch);
            stadiumGo.AddComponent<CrowdRenderer>();
        }

        private static void CreateMarkings(string name, Transform parent, Pitch pitch, PitchMeshGenerator.Mode mode,
                                           Material material, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var generator = go.AddComponent<PitchMeshGenerator>();
            SetReference(generator, "pitch", pitch);
            SetInt(generator, "mode", (int)mode);
            var serialized = new SerializedObject(generator);
            serialized.FindProperty("heightOffset").floatValue = height;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Portería con postes y larguero (colliders cápsula), red con colliders y volumen de gol.</summary>
        private static GoalZone BuildGoal(Transform parent, Pitch pitch, int side, Assets a)
        {
            var goalGo = new GameObject(side < 0 ? "Goal_West" : "Goal_East");
            goalGo.transform.SetParent(parent, false);
            goalGo.transform.position = pitch.GoalCenter(side);
            Transform t = goalGo.transform;

            const float postRadius = 0.06f;
            float halfWidth = pitch.GoalWidth * 0.5f;
            float height = pitch.GoalHeight;
            float depth = pitch.GoalDepth;
            float s = side;

            // Postes (cilindro primitivo: 2 m de alto y 1 m de diámetro a escala 1; trae CapsuleCollider).
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = i < 0 ? "Post_Right" : "Post_Left";
                post.transform.SetParent(t, false);
                post.transform.localPosition = new Vector3(0f, height * 0.5f, i * (halfWidth + postRadius));
                post.transform.localScale = new Vector3(postRadius * 2f, height * 0.5f, postRadius * 2f);
                post.GetComponent<MeshRenderer>().sharedMaterial = a.GoalFrame;
                post.GetComponent<Collider>().sharedMaterial = a.PostPhysics;
            }

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = "Crossbar";
            bar.transform.SetParent(t, false);
            bar.transform.localPosition = new Vector3(0f, height + postRadius, 0f);
            bar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bar.transform.localScale = new Vector3(postRadius * 2f, halfWidth + postRadius * 2f, postRadius * 2f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = a.GoalFrame;
            bar.GetComponent<Collider>().sharedMaterial = a.PostPhysics;

            // Red: fondo, laterales y techo.
            const float netThickness = 0.05f;
            Vector3 origin = t.position;
            CreateBox("Net_Back", t, origin + new Vector3(s * depth, height * 0.5f, 0f),
                      new Vector3(netThickness, height, pitch.GoalWidth), null, a.NetPhysics);
            CreateBox("Net_Top", t, origin + new Vector3(s * depth * 0.5f, height, 0f),
                      new Vector3(depth, netThickness, pitch.GoalWidth), null, a.NetPhysics);
            for (int i = -1; i <= 1; i += 2)
            {
                CreateBox(i < 0 ? "Net_SideRight" : "Net_SideLeft", t,
                          origin + new Vector3(s * depth * 0.5f, height * 0.5f, i * (halfWidth + postRadius)),
                          new Vector3(depth, height, netThickness), null, a.NetPhysics);
            }

            // Malla visual de hilos (las cajas de arriba solo hacen de colisión).
            var netGo = new GameObject("NetMesh");
            netGo.transform.SetParent(t, false);
            netGo.AddComponent<MeshFilter>();
            netGo.AddComponent<MeshRenderer>().sharedMaterial = a.Net;
            var net = netGo.AddComponent<NetMeshGenerator>();
            SetReference(net, "pitch", pitch);
            SetInt(net, "goalSide", side);

            // Volumen de gol: desde la línea hasta el fondo, entre postes y bajo el larguero.
            var zoneGo = new GameObject("GoalZone");
            zoneGo.transform.SetParent(t, false);
            zoneGo.transform.position = origin + new Vector3(s * depth * 0.5f, height * 0.5f, 0f);
            var box = zoneGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(depth, height, pitch.GoalWidth);
            var zone = zoneGo.AddComponent<GoalZone>();
            SetInt(zone, "goalSide", side);
            return zone;
        }

        private static Ball BuildBall(Assets a, Pitch pitch)
        {
            var ballGo = new GameObject("Ball");
            ballGo.transform.position = pitch.Center + Vector3.up * a.Ball.radius;

            var body = ballGo.AddComponent<Rigidbody>();
            body.mass = a.Ball.mass;
            var sphere = ballGo.AddComponent<SphereCollider>();
            sphere.radius = a.Ball.radius;
            sphere.sharedMaterial = a.BallPhysics;

            var ball = ballGo.AddComponent<Ball>();
            SetReference(ball, "settings", a.Ball);
            ballGo.AddComponent<BallPossession>();

            GameObject visual = CreateVisual(PrimitiveType.Sphere, "Visual", ballGo.transform, Vector3.zero,
                                             Vector3.one * a.Ball.radius * 2f, a.BallMaterial);
            var ballVisual = ballGo.AddComponent<BallVisual>();
            SetReference(ballVisual, "visual", visual.transform);
            SetReference(ballVisual, "ball", ball);
            return ball;
        }

        private static Team BuildTeam(string name, TeamSide side, int attackDirection, TeamSettings teamSettings, Assets a,
                                      Pitch pitch, Material kit, Material trim, Material keeperKit)
        {
            var teamGo = new GameObject(name);
            var team = teamGo.AddComponent<Team>();
            // IA del equipo (en el equipo humano controla a todos menos al jugador activo).
            var teamAI = teamGo.AddComponent<TeamAI>();
            SetReference(teamAI, "settings", a.AI);
            SetReference(team, "settings", teamSettings);
            SetReference(team, "formation", a.Formation);
            SetInt(team, "side", (int)side);
            SetInt(team, "attackDirection", attackDirection);

            List<FormationSlot> slots = a.Formation.slots;
            for (int i = 0; i < slots.Count; i++)
            {
                FormationSlot slot = slots[i];
                Vector2 kickoff = slot.basePosition;
                kickoff.x = Mathf.Min(kickoff.x, a.Formation.kickoffMaxDepth);
                Vector3 position = Team.FormationToWorld(kickoff, attackDirection, pitch.Center, pitch.HalfLength, pitch.HalfWidth);
                bool keeper = slot.role == PlayerRole.Goalkeeper;
                CreatePlayer(team.transform, $"{teamSettings.shortName}_{slot.shirtNumber:00}_{slot.role}", slot, i,
                             position, attackDirection, a, keeper ? keeperKit : kit, trim);
            }
            return team;
        }

        private static void CreatePlayer(Transform parent, string name, FormationSlot slot, int index, Vector3 position,
                                         int attackDirection, Assets a, Material kit, Material trim)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(attackDirection, 0f, 0f)));

            var body = go.AddComponent<Rigidbody>();
            body.mass = 75f;
            body.useGravity = false;

            // La cápsula queda 2 cm por encima del suelo: sin contacto con el césped no hay rozamiento que frene al jugador.
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.92f, 0f);
            capsule.height = 1.8f;
            capsule.radius = a.Movement.bodyRadius;

            // FootballPlayer añade PlayerMotor, PlayerStamina y PlayerActions por [RequireComponent];
            // se recuperan con GetComponent para no crear duplicados.
            var player = go.AddComponent<FootballPlayer>();
            go.AddComponent<PlayerBrain>();
            SetReference(go.GetComponent<PlayerMotor>(), "settings", a.Movement);
            SetReference(go.GetComponent<PlayerStamina>(), "settings", a.Movement);
            SetReference(go.GetComponent<PlayerActions>(), "settings", a.Actions);

            SetInt(player, "shirtNumber", slot.shirtNumber);
            SetInt(player, "role", (int)slot.role);
            SetInt(player, "formationIndex", index);
            SetReference(player, "bodyCollider", capsule);

            // Cuerpo articulado (cadera, torso, brazos, piernas) animado por PlayerAnimator.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            PlayerRigBuilder.Build(visual, new PlayerRigMaterials
            {
                Shirt = kit,
                Shorts = trim,
                Socks = kit,
                Skin = a.Skins[slot.shirtNumber % a.Skins.Length],
                Hair = a.Hairs[(slot.shirtNumber / 2) % a.Hairs.Length],
                Boots = a.Boots
            });
            go.AddComponent<PlayerAnimator>();
        }

        private static MatchCamera BuildCamera(Assets a, HumanTeamController human)
        {
            GameObject cameraGo = GameObject.Find("Main Camera");
            if (cameraGo == null)
            {
                cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }
            var camera = cameraGo.GetComponent<Camera>();
            camera.fieldOfView = a.Camera.fieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 600f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.transform.position = new Vector3(0f, a.Camera.height, -a.Camera.distance);
            cameraGo.transform.LookAt(Vector3.zero);

            var matchCamera = cameraGo.AddComponent<MatchCamera>();
            SetReference(matchCamera, "settings", a.Camera);
            SetReference(matchCamera, "humanController", human);
            return matchCamera;
        }

        private static void BuildSelectionIndicator(Assets a, HumanTeamController human)
        {
            var go = new GameObject("SelectionIndicator");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial = a.Indicator;
            var indicator = go.AddComponent<SelectionIndicator>();
            SetReference(indicator, "controller", human);
        }

        /// <summary>
        /// Añade la escena a Build Settings y las ordena: menú principal (la que arranca el juego), partido,
        /// entrenamiento y después cualquier otra.
        /// </summary>
        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            string[] order = { MainMenuScenePath, MatchScenePath, TrainingScenePath };
            EditorBuildSettings.scenes = scenes
                .OrderBy(s => System.Array.IndexOf(order, s.path) is int i && i >= 0 ? i : order.Length)
                .ToArray();
        }
    }
}
