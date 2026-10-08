using UnityEngine;
namespace CrashBashRemake
{
    public sealed class ArkenoidCraftVisual : MonoBehaviour
    {
        public static ArkenoidCraftVisual Build(Transform parent, ArenaSide side, Color accent)
        {
            var root = new GameObject("Hovercraft Visual").AddComponent<ArkenoidCraftVisual>();
            root.transform.SetParent(parent, false);
            Material shell = MakeMaterial(accent);
            Material dark = MakeMaterial(new Color(.06f, .07f, .09f));

            Add(root.transform, "Hull", PrimitiveType.Capsule, new Vector3(0,.12f,0), new Vector3(.68f,.20f,.52f), shell, Quaternion.Euler(0,0,90));
            Add(root.transform, "Nose", PrimitiveType.Sphere, new Vector3(0,.14f,.43f), new Vector3(.58f,.18f,.52f), shell, Quaternion.identity);
            Add(root.transform, "Left Pod", PrimitiveType.Sphere, new Vector3(-.48f,.02f,0), new Vector3(.28f,.16f,.56f), dark, Quaternion.identity);
            Add(root.transform, "Right Pod", PrimitiveType.Sphere, new Vector3(.48f,.02f,0), new Vector3(.28f,.16f,.56f), dark, Quaternion.identity);
            Add(root.transform, "Seat", PrimitiveType.Cylinder, new Vector3(0,.30f,-.05f), new Vector3(.28f,.12f,.28f), dark, Quaternion.identity);

            float yaw = side == ArenaSide.Top ? 180f : side == ArenaSide.Left ? 90f : side == ArenaSide.Right ? -90f : 0f;
            root.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (Collider c in root.GetComponentsInChildren<Collider>()) Destroy(c);
            return root;
        }

        static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .55f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .78f);
            return material;
        }

        static void Add(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
        }
    }
}