using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrashBashRemake
{
    /// <summary>Owns authored runtime meshes/materials. Nothing is extracted from PS1 art.</summary>
    public sealed class ArkenoidRenderResources : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public T Own<T>(T asset) where T : Object { owned.Add(asset); return asset; }
        public Mesh Mesh(string name, ArkenoidMeshData data)
        {
            if (meshes.TryGetValue(name, out Mesh cached)) return cached;
            var mesh = Own(new Mesh { name = name });
            var vertices = new Vector3[data.Vertices.Count];
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                ArkVertex v = data.Vertices[i]; vertices[i] = new Vector3(v.X, v.Y, v.Z);
                uv[i] = new Vector2(v.X + .5f, v.Z + .5f);
            }
            if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            meshes[name] = mesh; return mesh;
        }
        public Material Material(string name, Color color, float metallic = .3f, float smoothness = .6f, float emission = 0)
        {
            if (materials.TryGetValue(name, out Material cached)) return cached;
            Shader shader = GraphicsSettings.currentRenderPipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!shader) shader = Shader.Find("Standard");
            var material = Own(new Material(shader) { name = name, color = color });
            material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            materials[name] = material; return material;
        }
        public Material Glow()
        {
            if (materials.TryGetValue("Additive pulse", out Material cached)) return cached;
            Shader shader = Shader.Find("CrashBash/Glow") ?? Shader.Find("Unlit/Color");
            var material = Own(new Material(shader) { name = "Additive pulse" });
            material.SetColor("_Color", Color.white); materials[material.name] = material; return material;
        }
        public GameObject Part(Transform parent, string name, Mesh mesh, Vector3 position, Material material, Quaternion rotation = default)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation == default(Quaternion) ? Quaternion.identity : rotation;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            return part;
        }
        public Texture2D FloorTexture()
        {
            const int size = 256;
            var texture = Own(new Texture2D(size, size, TextureFormat.RGB24, true) { name = "Authored brushed floor panels", wrapMode = TextureWrapMode.Repeat });
            var colors = new Color[size * size]; var noise = new System.Random(840);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float shade = .70f + (float)noise.NextDouble() * .08f;
                if (x % 64 < 2 || y % 64 < 2) shade *= .45f;
                if ((x + y) % 127 == 0) shade *= .8f;
                colors[y * size + x] = new Color(shade, shade, shade);
            }
            texture.SetPixels(colors); texture.Apply(); return texture;
        }
        void OnDestroy() { foreach (Object asset in owned) if (asset) { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); } owned.Clear(); }
    }
}
