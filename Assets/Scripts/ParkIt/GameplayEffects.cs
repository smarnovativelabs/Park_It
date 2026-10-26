using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    /// <summary>Small bounded world effects. All temporary geometry shares one material.</summary>
    public sealed class GameplayEffects : MonoBehaviour
    {
        private sealed class Puff
        {
            public Transform node;
            public Renderer renderer;
            public Vector3 from, to, scale;
            public Color color;
            public float age, life, growth;
        }
        private readonly List<Puff> live = new List<Puff>();
        private readonly List<Mesh> bakedMeshes = new List<Mesh>();
        private MaterialPropertyBlock properties;
        private Material material;
        private Mesh ring, star;
        private GridManager grid;
        public int ActiveCount => live.Count;
        public static GameplayEffects For(GridManager grid)
        {
            return grid == null ? null : grid.GetComponent<GameplayEffects>() ?? grid.gameObject.AddComponent<GameplayEffects>();
        }
        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            grid = GetComponent<GridManager>();
            material = new Material(Resources.Load<Shader>("ParkItEffects")) { name = "ParkIt_Shared_Effects" };
            ring = MakeRing();
            star = MakeStar();
        }
        private Puff Add(string name, Mesh mesh, Vector3 position, Vector3 scale, Color color, float life, Vector3 travel, float growth = 0f)
        {
            if (live.Count >= 180) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var p = new Puff { node = go.transform, renderer = renderer, from = position, to = position + travel, scale = scale, color = color, life = life, growth = growth };
            Tint(p, 1f);
            live.Add(p);
            return p;
        }
        private void Tint(Puff p, float alpha)
        {
            var c = p.color; c.a *= alpha;
            properties.SetColor("_BaseColor", c);
            p.renderer.SetPropertyBlock(properties);
        }
        private static Mesh sphere, cube;
        private static Mesh Primitive(bool round)
        {
            if (round && sphere != null) return sphere;
            if (!round && cube != null) return cube;
            var go = GameObject.CreatePrimitive(round ? PrimitiveType.Sphere : PrimitiveType.Cube);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.SetActive(false); Destroy(go);
            if (round) sphere = mesh; else cube = mesh;
            return mesh;
        }
        private void Update()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i]; p.age += Time.deltaTime;
                float t = Mathf.Clamp01(p.age / p.life);
                p.node.position = Vector3.Lerp(p.from, p.to, t);
                p.node.localScale = p.scale * (1f + p.growth * t);
                Tint(p, (1f - t) * (1f - t));
                if (t >= 1f) { Destroy(p.node.gameObject); live.RemoveAt(i); }
            }
            if (live.Count == 0) ReleaseBakedMeshes();
        }
        private void ReleaseBakedMeshes()
        {
            foreach (var mesh in bakedMeshes) Destroy(mesh);
            bakedMeshes.Clear();
        }
        public void Clear()
        {
            foreach (var p in live) if (p.node != null) { p.node.gameObject.SetActive(false); Destroy(p.node.gameObject); }
            live.Clear();
            ReleaseBakedMeshes();
        }
        private void OnDestroy() { Clear(); Destroy(material); Destroy(ring); Destroy(star); }
        public void Marker(Vector3 position)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * .2f;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Add("Marker_Gold_Dot", Primitive(true), position + radial * .45f + Vector3.up * .25f,
                    Vector3.one * .13f, new Color(1f, .78f, .12f), .4f, radial * .4f + Vector3.up * .3f);
            }
        }
        public void Parking(Vector3 position)
        {
            Add("Parking_Success_Ripple", ring, position + Vector3.up * .27f, Vector3.one * .65f,
                new Color(.3f, 1f, .5f, .85f), .7f, Vector3.zero, 2.5f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = Add("Parking_Gold_Star", star, position + radial * .85f + Vector3.up * .4f,
                    Vector3.one * .23f, new Color(1f, .84f, .15f), .8f, radial * .25f + Vector3.up * .8f);
                if (p != null && Camera.main != null) p.node.rotation = Camera.main.transform.rotation;
            }
        }
        public void Landing(Vector3 position)
        {
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * .2f;
                var radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Add("Landing_Cream_Puff", Primitive(true), position + radial * .55f + Vector3.up * .08f,
                    new Vector3(.18f, .10f, .18f), new Color(1f, .9f, .73f, .7f), .45f, radial * .5f, 1.2f);
            }
        }
        public void Oil(Vector3 position, Quaternion rotation)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var lateral = rotation * Vector3.right * side;
                Add("Oil_Droplet", Primitive(true), position + lateral * .45f + Vector3.up * .12f,
                    Vector3.one * .09f, new Color(.04f, .12f, .18f, .8f), .35f, lateral * .35f + Vector3.up * .12f);
                var p = Add("Fading_Tyre_Track", Primitive(false), position + lateral * .35f + Vector3.up * .025f,
                    new Vector3(.10f, .008f, .3f), new Color(.03f, .08f, .12f, .55f), .9f, Vector3.zero);
                if (p != null) p.node.rotation = rotation;
            }
        }
        public void Rewind(Transform car, Vector3 destination, Quaternion targetRotation)
        {
            // Copy render meshes only: no controllers, colliders, lights or particle systems.
            foreach (var source in car.GetComponentsInChildren<MeshFilter>())
            {
                if (source.sharedMesh == null || source.name.Contains("Sparkle")) continue;
                RewindCopies(car, source.transform, source.sharedMesh, destination, targetRotation);
            }
            foreach (var source in car.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!source.enabled || source.sharedMesh == null) continue;
                var mesh = new Mesh { name = "Undo_Baked_Car" };
                // Include the renderer scale in the bake's coordinate conversion;
                // the copied Transform below then reproduces the authored size.
                source.BakeMesh(mesh, true);
                bakedMeshes.Add(mesh);
                RewindCopies(car, source.transform, mesh, destination, targetRotation);
            }
        }
        private void RewindCopies(Transform car, Transform source, Mesh mesh, Vector3 destination, Quaternion targetRotation)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = i / 3f;
                Vector3 pos = Vector3.Lerp(car.position, destination, t);
                Quaternion rot = Quaternion.Slerp(car.rotation, targetRotation, t);
                var local = car.InverseTransformPoint(source.position);
                var p = Add("Undo_Car_Silhouette", mesh, pos + rot * Vector3.Scale(local, car.lossyScale),
                    source.lossyScale, new Color(.35f, .85f, 1f, .24f), .45f + t * .15f, (destination - pos) * .15f);
                if (p != null) p.node.rotation = rot * Quaternion.Inverse(car.rotation) * source.rotation;
            }
        }
        public void Blocked(Vector2Int coord)
        {
            Vector3 pos = grid.CoordToWorldPosition(coord);
            Add("Blocked_Tile_Ring", ring, pos + Vector3.up * .05f, Vector3.one * .6f,
                new Color(1f, .45f, .3f, .7f), .3f, Vector3.zero, .4f);
            var root = GameObject.Find("Diorama_Root");
            if (root == null) return;
            foreach (Transform prop in root.transform)
            {
                if (Vector2.Distance(new Vector2(prop.position.x, prop.position.z), new Vector2(pos.x, pos.z)) > .1f || !prop.name.Contains("Hydrant")) continue;
                foreach (var source in prop.GetComponentsInChildren<MeshFilter>())
                {
                    var p = Add("Obstacle_Soft_Flash", source.sharedMesh, source.transform.position,
                        source.transform.lossyScale * 1.015f, new Color(1f, .8f, .45f, .45f), .3f, Vector3.zero);
                    if (p != null) p.node.rotation = source.transform.rotation;
                }
            }
        }
        private static Mesh MakeRing()
        {
            var v = new Vector3[64]; var triangles = new int[192];
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI / 16f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d; v[i * 2 + 1] = d * .91f;
                int n = (i + 1) % 32 * 2, k = i * 6;
                triangles[k] = i * 2; triangles[k+1] = n; triangles[k+2] = i*2+1;
                triangles[k+3] = n; triangles[k+4] = n+1; triangles[k+5] = i*2+1;
            }
            var mesh = new Mesh { name = "Effects_Ring", vertices = v, triangles = triangles }; mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh MakeStar()
        {
            var v = new Vector3[11]; var triangles = new int[30];
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 5f + Mathf.PI * .5f;
                v[i+1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * (i % 2 == 0 ? 1f : .45f);
                triangles[i*3] = 0; triangles[i*3+1] = i+1; triangles[i*3+2] = (i+1)%10+1;
            }
            var mesh = new Mesh { name = "Effects_Star", vertices = v, triangles = triangles }; mesh.RecalculateBounds(); return mesh;
        }
    }
}
