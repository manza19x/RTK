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
        private const string MatchScenePath = ScenesFolder + "/Partido.unity";
        private const string TrainingScenePath = ScenesFolder + "/Entrenamiento.unity";

        private sealed class Assets
        {
            public PlayerMovementSettings Movement;
            public BallSettings Ball;
            public ActionSettings Actions;
            public CameraSettings Camera;
            public MatchSettings Match;
            public FormationSettings Formation;
            public TeamSettings Home;
            public TeamSettings Away;

            public Material Grass, GrassStripes, Lines, Surround, GoalFrame, Net, BallMaterial, Indicator, Skin;
            public Material HomeKit, HomeTrim, HomeKeeper, AwayKit, AwayTrim, AwayKeeper;

            public PhysicsMaterial GrassPhysics, BallPhysics, NetPhysics, PostPhysics, WallPhysics;
        }

        [MenuItem("Fútbol Total/Crear escena de partido", priority = 0)]
        public static void BuildMatchScene() => Build(true);

        [MenuItem("Fútbol Total/Crear escena de entrenamiento", priority = 1)]
        public static void BuildTrainingScene() => Build(false);

        private static void Build(bool match)
        {
            string scenePath = match ? MatchScenePath : TrainingScenePath;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Primero la escena y después los assets: abrir una escena en modo Single descarga de memoria los assets
            // no usados, y las referencias obtenidas antes podrían guardarse vacías.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Assets assets = CreateAssets();

            ConfigureLight();

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

            EditorAssetUtility.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, scenePath);
            AddSceneToBuildSettings(scenePath, match);
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

            Texture2D ballTexture = LoadOrCreateBallTexture(TexturesFolder + "/BallPattern.png");

            a.Grass = LoadOrCreateMaterial(MaterialsFolder, "M_Grass", new Color(0.24f, 0.55f, 0.22f), smoothness: 0.05f);
            a.GrassStripes = LoadOrCreateMaterial(MaterialsFolder, "M_GrassStripes", new Color(0.2f, 0.48f, 0.19f), smoothness: 0.05f);
            a.Lines = LoadOrCreateMaterial(MaterialsFolder, "M_Lines", new Color(0.95f, 0.95f, 0.95f), unlit: true);
            a.Surround = LoadOrCreateMaterial(MaterialsFolder, "M_Surround", new Color(0.18f, 0.32f, 0.18f), smoothness: 0.05f);
            a.GoalFrame = LoadOrCreateMaterial(MaterialsFolder, "M_GoalFrame", Color.white, smoothness: 0.6f);
            a.Net = LoadOrCreateMaterial(MaterialsFolder, "M_Net", new Color(1f, 1f, 1f, 0.25f), smoothness: 0f, transparent: true);
            a.BallMaterial = LoadOrCreateMaterial(MaterialsFolder, "M_Ball", Color.white, smoothness: 0.5f, texture: ballTexture);
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

        private static void ConfigureLight()
        {
            GameObject lightGo = GameObject.Find("Directional Light");
            if (lightGo == null) return;
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var light = lightGo.GetComponent<Light>();
            light.shadows = LightShadows.Soft;
            light.intensity = 1.1f;
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
                         new Vector3(groundSize.x, 0.1f, groundSize.z), a.Surround);
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
            return pitch;
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
                      new Vector3(netThickness, height, pitch.GoalWidth), a.Net, a.NetPhysics);
            CreateBox("Net_Top", t, origin + new Vector3(s * depth * 0.5f, height, 0f),
                      new Vector3(depth, netThickness, pitch.GoalWidth), a.Net, a.NetPhysics);
            for (int i = -1; i <= 1; i += 2)
            {
                CreateBox(i < 0 ? "Net_SideRight" : "Net_SideLeft", t,
                          origin + new Vector3(s * depth * 0.5f, height * 0.5f, i * (halfWidth + postRadius)),
                          new Vector3(depth, height, netThickness), a.Net, a.NetPhysics);
            }

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
            SetReference(go.GetComponent<PlayerMotor>(), "settings", a.Movement);
            SetReference(go.GetComponent<PlayerStamina>(), "settings", a.Movement);
            SetReference(go.GetComponent<PlayerActions>(), "settings", a.Actions);

            SetInt(player, "shirtNumber", slot.shirtNumber);
            SetInt(player, "role", (int)slot.role);
            SetInt(player, "formationIndex", index);
            SetReference(player, "bodyCollider", capsule);

            // Cuerpo provisional: torso con la equipación, pantalón, cabeza y una marca frontal que indica la orientación.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            CreateVisual(PrimitiveType.Capsule, "Torso", visual, new Vector3(0f, 1.12f, 0f), new Vector3(0.62f, 0.42f, 0.42f), kit);
            CreateVisual(PrimitiveType.Cylinder, "Shorts", visual, new Vector3(0f, 0.72f, 0f), new Vector3(0.56f, 0.12f, 0.4f), trim);
            CreateVisual(PrimitiveType.Capsule, "Leg_L", visual, new Vector3(-0.13f, 0.36f, 0f), new Vector3(0.18f, 0.34f, 0.18f), a.Skin);
            CreateVisual(PrimitiveType.Capsule, "Leg_R", visual, new Vector3(0.13f, 0.36f, 0f), new Vector3(0.18f, 0.34f, 0.18f), a.Skin);
            CreateVisual(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.66f, 0f), Vector3.one * 0.3f, a.Skin);
            CreateVisual(PrimitiveType.Cube, "Front", visual, new Vector3(0f, 1.2f, 0.2f), new Vector3(0.3f, 0.16f, 0.06f), trim);
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
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.62f, 0.8f);
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

        /// <summary>Añade la escena a Build Settings. La de partido va la primera (es la que arranca el juego).</summary>
        private static void AddSceneToBuildSettings(string path, bool first)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) scenes.Insert(0, entry);
            else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
