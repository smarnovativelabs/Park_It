using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    /// <summary>Procedural toy-diorama props: flat arrows, fire-hydrant obstacles and primitive helpers.</summary>
    public static class DioramaMeshes
    {
        private static Mesh triangleArrow;
        private static Mesh stemArrow;
        private static Mesh oilPuddle;
        private static readonly Dictionary<(Vector3, float, float), Mesh> roundedBlocks = new Dictionary<(Vector3, float, float), Mesh>();

        /// <summary>Low-poly rounded slab with a bevel catching the light along its top edge.</summary>
        public static GameObject RoundedBlock(string name, Transform parent, Vector3 position, Vector3 size, float radius, float bevel, Material material)
        {
            var key = (size, radius, bevel);
            if (!roundedBlocks.TryGetValue(key, out var mesh) || mesh == null)
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var rings = new Vector3[4, 24];
                for (int ring = 0; ring < 4; ring++)
                {
                    float inset = ring == 0 || ring == 3 ? bevel : 0;
                    float y = ring == 0 ? -size.y * .5f : ring == 1 ? -size.y * .5f + bevel
                        : ring == 2 ? size.y * .5f - bevel : size.y * .5f;
                    float r = Mathf.Max(.001f, radius - inset);
                    for (int corner = 0; corner < 4; corner++)
                    {
                        float cx = (corner == 0 || corner == 3 ? 1 : -1) * (size.x * .5f - radius);
                        float cz = (corner < 2 ? 1 : -1) * (size.z * .5f - radius);
                        for (int step = 0; step < 6; step++)
                        {
                            float a = (corner * 90f + step * 18f) * Mathf.Deg2Rad;
                            rings[ring, corner * 6 + step] = new Vector3(cx + Mathf.Cos(a) * r, y, cz + Mathf.Sin(a) * r);
                        }
                    }
                }
                for (int i = 0; i < 24; i++)
                {
                    int next = (i + 1) % 24;
                    Face(vertices, triangles, new Vector3(0, size.y * .5f, 0), rings[3, i], rings[3, next], Vector3.up);
                    Face(vertices, triangles, new Vector3(0, -size.y * .5f, 0), rings[0, next], rings[0, i], Vector3.down);
                    for (int ring = 0; ring < 3; ring++)
                    {
                        var outward = rings[ring, i] + rings[ring, next];
                        outward.y = 0;
                        Face(vertices, triangles, rings[ring, i], rings[ring, next], rings[ring + 1, next], outward);
                        Face(vertices, triangles, rings[ring, i], rings[ring + 1, next], rings[ring + 1, i], outward);
                    }
                }
                mesh = new Mesh { name = "Toy_Rounded_Slab" };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                // Shared analytic normals keep the bevels smooth even though each face
                // has separate vertices. Flat triangle normals made the toy plastic faceted.
                var normals = new List<Vector3>(vertices.Count);
                foreach (var v in vertices)
                {
                    var nearest = new Vector3(
                        Mathf.Clamp(v.x, -size.x * .5f + radius, size.x * .5f - radius),
                        0,
                        Mathf.Clamp(v.z, -size.z * .5f + radius, size.z * .5f - radius));
                    var outward = new Vector3(v.x - nearest.x, 0, v.z - nearest.z).normalized;
                    bool cap = Mathf.Abs(v.y) >= size.y * .5f - .0001f;
                    float vertical = v.y >= 0 ? 1f : -1f;
                    normals.Add(cap
                        ? Vector3.up * vertical
                        : (outward + Vector3.up * vertical * .45f).normalized);
                }
                mesh.SetNormals(normals);
                mesh.RecalculateBounds();
                roundedBlocks[key] = mesh;
            }
            return MeshObject(name, parent, mesh, material, position, 0, Vector3.one);
        }

        /// <summary>A softly lobed silhouette shared by the oil surface and its inset rim.</summary>
        public static Mesh OilPuddle
        {
            get
            {
                if (oilPuddle == null)
                {
                    var outline = new Vector2[64];
                    for (int i = 0; i < outline.Length; i++)
                    {
                        float a = i * Mathf.PI * 2f / outline.Length;
                        float radius = .72f + .09f * Mathf.Sin(a * 3f + .4f)
                            + .045f * Mathf.Cos(a * 5f);
                        outline[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * .84f);
                    }
                    oilPuddle = Prisms("Glossy_Oil_Puddle", .025f, outline);
                }
                return oilPuddle;
            }
        }

        /// <summary>Flat triangle pointing +Z (ghost-node arrow).</summary>
        public static Mesh TriangleArrow
        {
            get
            {
                if (triangleArrow == null)
                    triangleArrow = Prisms("Arrow_Triangle", .08f, new[] { new Vector2(-.36f, -.26f), new Vector2(0f, .34f), new Vector2(.36f, -.26f) });
                return triangleArrow;
            }
        }

        /// <summary>Arrow with a stem pointing +Z (parking-bay heading marker).</summary>
        public static Mesh StemArrow
        {
            get
            {
                if (stemArrow == null)
                    stemArrow = Prisms("Arrow_Stem", .06f,
                        new[] { new Vector2(-.42f, .02f), new Vector2(0f, .52f), new Vector2(.42f, .02f) },
                        new[] { new Vector2(-.13f, -.5f), new Vector2(-.13f, .04f), new Vector2(.13f, .04f), new Vector2(.13f, -.5f) });
                return stemArrow;
            }
        }

        private static Mesh Prisms(string name, float thickness, params Vector2[][] polygons)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            foreach (var poly in polygons)
            {
                for (int i = 1; i < poly.Length - 1; i++)
                {
                    Face(verts, tris, P(poly[0], thickness), P(poly[i], thickness), P(poly[i + 1], thickness), Vector3.up);
                    Face(verts, tris, P(poly[0], 0), P(poly[i + 1], 0), P(poly[i], 0), Vector3.down);
                }
                Vector2 centre = Vector2.zero;
                foreach (var p in poly) centre += p;
                centre /= poly.Length;
                for (int i = 0; i < poly.Length; i++)
                {
                    Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                    Vector2 mid = (a + b) * .5f - centre;
                    var outward = new Vector3(mid.x, 0, mid.y);
                    Face(verts, tris, P(a, 0), P(b, 0), P(b, thickness), outward);
                    Face(verts, tris, P(a, 0), P(b, thickness), P(a, thickness), outward);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 P(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        // Flat-shaded triangle wound so it faces 'outward'.
        private static void Face(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var t = b; b = c; c = t; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 localPos, float yaw, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Collider-free primitive (decor only; ghost nodes add their own tap collider).</summary>
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Chunky red toy fire hydrant used for every obstacle tile.</summary>
        public static GameObject FireHydrant(Transform parent, Vector3 position)
        {
            var root = new GameObject("Obstacle_Hydrant");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0, Mathf.Sin(position.x * 12.9f + position.z * 78.2f) * 25f, 0);
            var t = root.transform;

            var red = HydrantPlastic(new Color(1f, .075f, .095f), .84f, .42f);
            var darkRed = HydrantPlastic(new Color(.65f, .025f, .055f), .70f, .25f);
            var rimRed = HydrantPlastic(new Color(1f, .24f, .22f), .80f, .40f);
            var glint = HydrantPlastic(new Color(1f, .80f, .69f), .64f, .62f);

            RoundedBlock("Base_Shadow", t, new Vector3(0, .055f, 0), new Vector3(.92f, .10f, .92f), .46f, .035f, darkRed);
            RoundedBlock("Base", t, new Vector3(0, .13f, 0), new Vector3(.88f, .13f, .88f), .44f, .05f, red);
            RoundedBlock("Base_Rim", t, new Vector3(0, .195f, 0), new Vector3(.76f, .07f, .76f), .38f, .025f, rimRed);
            Prim(PrimitiveType.Cylinder, "Body", t, new Vector3(0, .53f, 0), new Vector3(.60f, .34f, .60f), red);
            RoundedBlock("Collar_Shadow", t, new Vector3(0, .84f, 0), new Vector3(.72f, .08f, .72f), .36f, .025f, darkRed);
            RoundedBlock("Collar", t, new Vector3(0, .90f, 0), new Vector3(.76f, .10f, .76f), .38f, .04f, rimRed);
            Prim(PrimitiveType.Sphere, "Dome", t, new Vector3(0, .96f, 0), new Vector3(.67f, .46f, .67f), red);
            Prim(PrimitiveType.Sphere, "Cap", t, new Vector3(0, 1.20f, 0), new Vector3(.19f, .18f, .19f), rimRed);
            foreach (int side in new[] { -1, 1 })
            {
                Prim(PrimitiveType.Cylinder, "Nozzle_" + side, t, new Vector3(side * .36f, .59f, 0), new Vector3(.29f, .12f, .29f), red, new Vector3(0, 0, 90));
                Prim(PrimitiveType.Sphere, "Nozzle_Cap_" + side, t, new Vector3(side * .48f, .59f, 0), new Vector3(.12f, .30f, .30f), rimRed);
            }
            Prim(PrimitiveType.Cylinder, "Nozzle_F_Rim", t, new Vector3(0, .54f, .31f), new Vector3(.34f, .07f, .34f), darkRed, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Sphere, "Nozzle_F", t, new Vector3(0, .54f, .39f), new Vector3(.28f, .28f, .12f), red);
            Prim(PrimitiveType.Cylinder, "Nozzle_Bolt", t, new Vector3(0, .54f, .46f), new Vector3(.09f, .025f, .09f), rimRed, new Vector3(90, 0, 0));

            // Small fixed glints reinforce the toy-plastic finish in the mobile lighting.
            Prim(PrimitiveType.Sphere, "Dome_Glint", t, new Vector3(-.12f, 1.105f, -.14f), new Vector3(.18f, .035f, .09f), glint, new Vector3(-30f, 0, 25f));
            Prim(PrimitiveType.Sphere, "Body_Glint", t, new Vector3(-.16f, .58f, -.245f), new Vector3(.055f, .30f, .035f), rimRed, new Vector3(0, 30f, 0));
            return root;
        }

        private static Material HydrantPlastic(Color color, float smoothness, float fill)
        {
            var material = URPMaterialHelper.CreateMaterial(color, smoothness, 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * fill);
            return material;
        }
    }
}
