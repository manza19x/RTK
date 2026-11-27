using UnityEngine;

namespace FutbolTotal
{
    /// <summary>Materiales de un futbolista (equipación, piel, pelo, botas).</summary>
    public struct PlayerRigMaterials
    {
        public Material Shirt;
        public Material Shorts;
        public Material Socks;
        public Material Skin;
        public Material Hair;
        public Material Boots;
    }

    /// <summary>
    /// Construye un cuerpo articulado con primitivas: cadera, torso, cabeza, brazos con codo y piernas con rodilla.
    /// Cada articulación es un Transform vacío con el nombre de <see cref="PlayerRigBones"/>, para que
    /// <see cref="PlayerAnimator"/> lo anime por código. Medidas de un jugador de 1,80 m con los pies en y = 0.
    /// Para usar un modelo humano real, se sustituye este cuerpo por el modelo (ver docs/Fase6.md).
    /// </summary>
    public static class PlayerRigBuilder
    {
        public static Transform Build(Transform parent, PlayerRigMaterials m)
        {
            Transform hips = Joint(PlayerRigBones.Hips, parent, new Vector3(0f, 0.95f, 0f));
            Part(PrimitiveType.Capsule, "Shorts", hips, new Vector3(0f, -0.03f, 0f), new Vector3(0.36f, 0.14f, 0.25f), m.Shorts);

            Transform spine = Joint(PlayerRigBones.Spine, hips, new Vector3(0f, 0.04f, 0f));
            Part(PrimitiveType.Capsule, "Torso", spine, new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.3f, 0.25f), m.Shirt);
            Part(PrimitiveType.Cylinder, "Neck", spine, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 0.05f, 0.1f), m.Skin);

            Transform head = Joint(PlayerRigBones.Head, spine, new Vector3(0f, 0.72f, 0f));
            Part(PrimitiveType.Sphere, "Face", head, Vector3.zero, new Vector3(0.21f, 0.24f, 0.22f), m.Skin);
            Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0f, 0.05f, -0.015f), new Vector3(0.225f, 0.17f, 0.225f), m.Hair);

            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";

                Transform shoulder = Joint(PlayerRigBones.Shoulder + s, spine, new Vector3(side * 0.25f, 0.52f, 0f));
                Part(PrimitiveType.Capsule, "UpperArm", shoulder, new Vector3(0f, -0.14f, 0f), new Vector3(0.11f, 0.15f, 0.11f), m.Shirt);
                Transform elbow = Joint(PlayerRigBones.Elbow + s, shoulder, new Vector3(0f, -0.29f, 0f));
                Part(PrimitiveType.Capsule, "Forearm", elbow, new Vector3(0f, -0.13f, 0f), new Vector3(0.09f, 0.14f, 0.09f), m.Skin);

                Transform hip = Joint(PlayerRigBones.Hip + s, hips, new Vector3(side * 0.11f, -0.04f, 0f));
                Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.23f, 0.16f), m.Skin);
                Part(PrimitiveType.Capsule, "ShortsLeg", hip, new Vector3(0f, -0.08f, 0f), new Vector3(0.18f, 0.1f, 0.18f), m.Shorts);
                Transform knee = Joint(PlayerRigBones.Knee + s, hip, new Vector3(0f, -0.44f, 0f));
                Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.21f, 0.13f), m.Socks);
                Part(PrimitiveType.Cube, "Boot", knee, new Vector3(0f, -0.43f, 0.05f), new Vector3(0.11f, 0.08f, 0.27f), m.Boots);
            }
            return hips;
        }

        private static Transform Joint(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    /// <summary>Nombres de las articulaciones (los comparten el constructor y el animador).</summary>
    public static class PlayerRigBones
    {
        public const string Hips = "Hips";
        public const string Spine = "Spine";
        public const string Head = "Head";
        public const string Shoulder = "Shoulder";
        public const string Elbow = "Elbow";
        public const string Hip = "Hip";
        public const string Knee = "Knee";
    }
}
