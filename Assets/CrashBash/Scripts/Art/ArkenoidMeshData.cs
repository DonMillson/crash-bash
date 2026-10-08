using System;
using System.Collections.Generic;

namespace CrashBashRemake
{
    [Serializable]
    public struct ArkVertex
    {
        public float X, Y, Z;
        public ArkVertex(float x, float y, float z) { X = x; Y = y; Z = z; }
    }
    public sealed class ArkenoidMeshData
    {
        public readonly List<ArkVertex> Vertices = new List<ArkVertex>();
        public readonly List<int> Triangles = new List<int>();
        public void Quad(int a, int b, int c, int d)
        { Triangles.AddRange(new[] { a, b, c, a, c, d }); }

        // An original lofted hull. The nose narrows rather than scaling a primitive cube.
        public static ArkenoidMeshData Hull(float width, float depth, float height, int segments = 20)
        {
            var m = new ArkenoidMeshData();
            float[] ys = { -height * .5f, -height * .28f, height * .30f, height * .5f };
            float[] radii = { .70f, 1, .96f, .73f };
            for (int ring = 0; ring < ys.Length; ring++)
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * (float)Math.PI * 2 / segments;
                    float z = (float)Math.Sin(angle);
                    float taper = z > 0 ? 1 - z * .22f : 1;
                    m.Vertices.Add(new ArkVertex((float)Math.Cos(angle) * width * .5f * radii[ring] * taper,
                        ys[ring], z * depth * .5f * radii[ring]));
                }
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i < segments; i++)
                {
                    int j = (i + 1) % segments;
                    m.Quad(ring * segments + i, (ring + 1) * segments + i, (ring + 1) * segments + j, ring * segments + j);
                }
            int bottom = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, ys[0], 0));
            int top = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, ys[3], 0));
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % segments;
                m.Triangles.AddRange(new[] { bottom, i, j, top, 3 * segments + j, 3 * segments + i });
            }
            return m;
        }
        public static ArkenoidMeshData Ellipsoid(float x, float y, float z, int sides = 20, int rings = 12)
        {
            var m = new ArkenoidMeshData();
            for (int row = 0; row <= rings; row++)
            {
                float latitude = (float)Math.PI * row / rings;
                for (int col = 0; col <= sides; col++)
                {
                    float longitude = (float)Math.PI * 2 * col / sides;
                    m.Vertices.Add(new ArkVertex((float)(Math.Sin(latitude) * Math.Cos(longitude)) * x * .5f,
                        (float)Math.Cos(latitude) * y * .5f, (float)(Math.Sin(latitude) * Math.Sin(longitude)) * z * .5f));
                }
            }
            for (int row = 0; row < rings; row++)
                for (int col = 0; col < sides; col++)
                {
                    int a = row * (sides + 1) + col;
                    m.Quad(a, a + 1, a + sides + 2, a + sides + 1);
                }
            return m;
        }
        public static ArkenoidMeshData Torus(float radius, float tube, int segments = 32, int sides = 8)
        {
            var m = new ArkenoidMeshData();
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                {
                    float a = i * (float)Math.PI * 2 / segments, b = j * (float)Math.PI * 2 / sides;
                    float r = radius + (float)Math.Cos(b) * tube;
                    m.Vertices.Add(new ArkVertex((float)Math.Cos(a) * r, (float)Math.Sin(b) * tube, (float)Math.Sin(a) * r));
                }
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                    m.Quad(i * sides + j, i * sides + (j + 1) % sides,
                        ((i + 1) % segments) * sides + (j + 1) % sides, ((i + 1) % segments) * sides + j);
            return m;
        }
        public static ArkenoidMeshData Prism(float width, float height, float depth, float bevel)
        {
            var m = new ArkenoidMeshData();
            float x = width * .5f, z = depth * .5f;
            bevel = Math.Min(bevel, Math.Min(x, z) * .8f);
            ArkVertex[] polygon = {
                new ArkVertex(-x + bevel, 0, -z), new ArkVertex(x - bevel, 0, -z),
                new ArkVertex(x, 0, -z + bevel), new ArkVertex(x, 0, z - bevel),
                new ArkVertex(x - bevel, 0, z), new ArkVertex(-x + bevel, 0, z),
                new ArkVertex(-x, 0, z - bevel), new ArkVertex(-x, 0, -z + bevel),
            };
            foreach (ArkVertex v in polygon) m.Vertices.Add(new ArkVertex(v.X, -height * .5f, v.Z));
            foreach (ArkVertex v in polygon) m.Vertices.Add(new ArkVertex(v.X, height * .5f, v.Z));
            int bottom = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, -height * .5f, 0));
            int top = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, height * .5f, 0));
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                m.Quad(i, i + 8, j + 8, j);
                m.Triangles.AddRange(new[] { bottom, i, j, top, j + 8, i + 8 });
            }
            return m;
        }
        public static ArkenoidMeshData Cylinder(float radius, float height, int segments = 24)
        {
            var m = new ArkenoidMeshData();
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < segments; i++)
                {
                    float a = (float)Math.PI * 2 * i / segments;
                    m.Vertices.Add(new ArkVertex((float)Math.Cos(a) * radius, (row - .5f) * height, (float)Math.Sin(a) * radius));
                }
            int bottom = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, -height * .5f, 0));
            int top = m.Vertices.Count; m.Vertices.Add(new ArkVertex(0, height * .5f, 0));
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % segments;
                m.Quad(i, i + segments, j + segments, j);
                m.Triangles.AddRange(new[] { bottom, i, j, top, j + segments, i + segments });
            }
            return m;
        }
    }

    public enum ArkSurface { Paint, Alloy, Rubber, Seat, Light, Exhaust }
    public sealed class ArkCraftPart
    {
        public string Name;
        public ArkenoidMeshData Mesh;
        public ArkVertex Position;
        public ArkVertex Rotation;
        public ArkSurface Surface;
        public ArkCraftPart(string name, ArkenoidMeshData mesh, ArkVertex position, ArkSurface surface, ArkVertex rotation = default)
        { Name = name; Mesh = mesh; Position = position; Surface = surface; Rotation = rotation; }
    }
    public static class ArkenoidCraftRecipe
    {
        public static List<ArkCraftPart> Create()
        {
            var parts = new List<ArkCraftPart> {
                new ArkCraftPart("Lofted pressure hull", ArkenoidMeshData.Hull(1.08f,.80f,.23f), new ArkVertex(0,.08f,0), ArkSurface.Paint),
                new ArkCraftPart("Upper deck bevel", ArkenoidMeshData.Hull(.90f,.67f,.08f), new ArkVertex(0,.20f,0), ArkSurface.Alloy),
                new ArkCraftPart("Front deflector", ArkenoidMeshData.Hull(1.05f,.19f,.14f), new ArkVertex(0,.12f,.37f), ArkSurface.Paint),
                new ArkCraftPart("Rubber skirt", ArkenoidMeshData.Hull(1.11f,.83f,.08f), new ArkVertex(0,-.05f,0), ArkSurface.Rubber),
                new ArkCraftPart("Pilot seat base", ArkenoidMeshData.Prism(.32f,.10f,.35f,.06f), new ArkVertex(0,.27f,-.10f), ArkSurface.Seat),
                new ArkCraftPart("Pilot seat back", ArkenoidMeshData.Ellipsoid(.36f,.35f,.12f), new ArkVertex(0,.42f,-.25f), ArkSurface.Seat),
                new ArkCraftPart("Control console", ArkenoidMeshData.Hull(.38f,.16f,.12f), new ArkVertex(0,.34f,.20f), ArkSurface.Rubber),
                new ArkCraftPart("Navigation screen", ArkenoidMeshData.Prism(.18f,.012f,.08f,.018f), new ArkVertex(0,.40f,.20f), ArkSurface.Light),
                new ArkCraftPart("Levitation coil", ArkenoidMeshData.Torus(.38f,.018f), new ArkVertex(0,-.11f,0), ArkSurface.Light),
            };
            foreach (int sign in new[] { -1, 1 })
            {
                string side = sign < 0 ? "Port" : "Starboard";
                parts.Add(new ArkCraftPart(side + " outrigger pod", ArkenoidMeshData.Hull(.31f,.82f,.24f), new ArkVertex(sign*.55f,.02f,0), ArkSurface.Paint));
                parts.Add(new ArkCraftPart(side + " pod armour", ArkenoidMeshData.Hull(.24f,.60f,.07f), new ArkVertex(sign*.55f,.17f,0), ArkSurface.Alloy));
                parts.Add(new ArkCraftPart(side + " rear turbine rim", ArkenoidMeshData.Torus(.094f,.021f), new ArkVertex(sign*.55f,.03f,-.40f), ArkSurface.Alloy, new ArkVertex(90,0,0)));
                parts.Add(new ArkCraftPart(side + " turbine cavity", ArkenoidMeshData.Cylinder(.076f,.03f), new ArkVertex(sign*.55f,.03f,-.401f), ArkSurface.Rubber, new ArkVertex(90,0,0)));
                parts.Add(new ArkCraftPart(side + " exhaust glow", ArkenoidMeshData.Torus(.064f,.012f), new ArkVertex(sign*.55f,.03f,-.42f), ArkSurface.Exhaust, new ArkVertex(90,0,0)));
                parts.Add(new ArkCraftPart(side + " headlamp", ArkenoidMeshData.Ellipsoid(.12f,.065f,.028f), new ArkVertex(sign*.39f,.19f,.405f), ArkSurface.Light));
                parts.Add(new ArkCraftPart(side + " control handle", ArkenoidMeshData.Cylinder(.022f,.13f), new ArkVertex(sign*.16f,.43f,.17f), ArkSurface.Rubber, new ArkVertex(30,0,0)));
                parts.Add(new ArkCraftPart(side + " hover nozzle", ArkenoidMeshData.Cylinder(.09f,.08f), new ArkVertex(sign*.46f,-.13f,-.06f), ArkSurface.Alloy));
                parts.Add(new ArkCraftPart(side + " hover jet", ArkenoidMeshData.Ellipsoid(.10f,.16f,.10f), new ArkVertex(sign*.46f,-.24f,-.06f), ArkSurface.Exhaust));
            }
            return parts;
        }
    }
}
