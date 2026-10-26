using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    /// <summary>Builds the candy tabletop: purple plinth, cream curb and soft blue slate tiles.</summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float tileSize = 2.0f;
        [SerializeField] private Transform dioramaRoot;

        private LevelData currentLevel;
        private readonly List<GameObject> activeProps = new List<GameObject>();
        private readonly List<GameObject> activeGhostNodes = new List<GameObject>();

        public float TileSize => tileSize;
        public LevelData CurrentLevel => currentLevel;

        // Muted blue-grey slate, with softer joints matching the reference board.
        private static readonly Color TileA = new Color(.515f, .61f, .675f);
        private static readonly Color TileB = new Color(.545f, .645f, .71f);
        private static readonly Color Grout = new Color(.34f, .44f, .51f);
        private static readonly Color Rim = new Color(1f, .89f, .72f);
        private static readonly Color Plinth = new Color(.63f, .15f, .95f);
        private static readonly Color GhostGold = new Color(1f, .85f, .20f, 1f);
        private static readonly Color GhostOil = new Color(.24f, .80f, 1f, .75f);
        private static readonly Color BayGreen = new Color(.18f, .92f, .46f);

        public void InitializeGrid(LevelData level)
        {
            GameplayEffects.For(this).Clear();
            GetComponent<BoardEffects>()?.Finish();
            currentLevel = level;
            ClearGrid();

            if (dioramaRoot == null)
            {
                GameObject root = GameObject.Find("Diorama_Root");
                if (root == null) root = new GameObject("Diorama_Root");
                dioramaRoot = root.transform;
            }
            // Remove anything left over from older scene bakes.
            for (int i = dioramaRoot.childCount - 1; i >= 0; i--)
            {
                var child = dioramaRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            BuildTabletop(level.gridWidth, level.gridHeight);
            Vector3 origin = GetGridOrigin(level.gridWidth, level.gridHeight);

            for (int x = 0; x < level.gridWidth; x++)
            {
                for (int y = 0; y < level.gridHeight; y++)
                {
                    Vector3 pos = origin + new Vector3(x * tileSize, 0, y * tileSize);
                    var coord = new Vector2Int(x, y);
                    activeProps.Add(CreateTile(pos, x, y));
                    if (level.obstacles != null && level.obstacles.Contains(coord))
                        activeProps.Add(DioramaMeshes.FireHydrant(dioramaRoot, pos));
                    if (level.oilSpills != null && level.oilSpills.Contains(coord))
                        activeProps.Add(CreateOil(pos));
                    if (coord == level.targetCoord)
                        activeProps.Add(CreateTargetSlot(pos, level.targetDirection));
                }
            }

            CameraRig.Instance?.FrameBoard(level.gridWidth, level.gridHeight, tileSize);
            var motion = GetComponent<BoardEffects>() ?? gameObject.AddComponent<BoardEffects>();
            motion.Begin(activeProps, level.gridWidth, level.gridHeight, tileSize);
        }

        private void ClearGrid()
        {
            foreach (var p in activeProps)
            {
                if (p == null) continue;
                p.SetActive(false);
                Destroy(p);
            }
            activeProps.Clear();
            ClearGhostNodes();
        }

        private void BuildTabletop(int width, int height)
        {
            float w = width * tileSize, h = height * tileSize;
            const float rim = .72f;
            var root = new GameObject("Tabletop_Plinth").transform;
            root.SetParent(dioramaRoot, false);
            activeProps.Add(root.gameObject);

            var plinth = CreatePlatformMaterial(Plinth, .78f, .28f);
            var rimMat = CreatePlatformMaterial(Rim, .76f, .38f);
            var grout = CreatePlatformMaterial(Grout, .32f, .10f);

            var purpleShadow = CreatePlatformMaterial(new Color(.34f, .06f, .59f), .65f, .12f);
            var purpleLip = CreatePlatformMaterial(new Color(.82f, .46f, 1f), .82f, .26f);
            DioramaMeshes.RoundedBlock("Plinth_Outline", root, new Vector3(0, -.88f, 0), new Vector3(w + 1.88f, .20f, h + 1.88f), 1.24f, .09f, purpleShadow);
            DioramaMeshes.RoundedBlock("Plinth_Base", root, new Vector3(0, -.53f, 0), new Vector3(w + 2.02f, .78f, h + 2.02f), 1.30f, .36f, plinth);
            DioramaMeshes.RoundedBlock("Purple_Top_Lip", root, new Vector3(0, -.22f, 0), new Vector3(w + 1.85f, .18f, h + 1.85f), 1.22f, .085f, purpleLip);
            DioramaMeshes.RoundedBlock("Cream_Rim", root, new Vector3(0, -.39f, 0), new Vector3(w + rim * 2, .32f, h + rim * 2), .80f, .12f, rimMat);
            // Flush pieces share the corner cross section, so no open seams remain.
            for (int x = 0; x < width; x++)
            {
                float px = (x - (width - 1) * .5f) * tileSize;
                bool end = x == 0 || x == width - 1;
                if (end) px += x == 0 ? .335f : -.335f;
                foreach (int side in new[] { -1, 1 })
                    DioramaMeshes.CreamStraight("Rim_End_" + x + "_" + side, root,
                        new Vector3(px, -.005f, side * (h * .5f + .33f)),
                        tileSize - (end ? .67f : 0), 90, rimMat);
            }
            for (int y = 0; y < height; y++)
            {
                float pz = (y - (height - 1) * .5f) * tileSize;
                bool end = y == 0 || y == height - 1;
                if (end) pz += y == 0 ? .335f : -.335f;
                foreach (int side in new[] { -1, 1 })
                    DioramaMeshes.CreamStraight("Rim_Side_" + y + "_" + side, root,
                        new Vector3(side * (w * .5f + .33f), -.005f, pz),
                        tileSize - (end ? .67f : 0), 0, rimMat);
            }
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    DioramaMeshes.CreamCorner("Rim_Corner_" + sx + "_" + sz, root,
                        new Vector3(sx * (w * .5f - .67f), -.005f, sz * (h * .5f - .67f)),
                        sx > 0 ? (sz > 0 ? 0 : 90) : (sz > 0 ? 270 : 180), rimMat);
            // Recess the joints below the top bevel so its rounded profile catches light.
            DioramaMeshes.RoundedBlock("Grout_Bed", root, new Vector3(0, -.20f, 0), new Vector3(w + .06f, .18f, h + .06f), .22f, .04f, grout);
        }

        private GameObject CreateTile(Vector3 pos, int x, int y)
        {
            var mat = CreatePlatformMaterial((x + y) % 2 == 0 ? TileA : TileB, .68f, .10f);
            var tile = DioramaMeshes.RoundedBlock($"Tile_{x}_{y}", dioramaRoot, Vector3.zero, new Vector3(tileSize * .984f, .28f, tileSize * .984f), .25f, .12f, mat);
            // Keep the driving surface at y=0, with the extra depth below it.
            tile.transform.position = pos + Vector3.down * .14f;
            return tile;
        }

        // A small baked-in fill keeps the candy palette readable under the scene's dark
        // ambient lighting, without changing the lighting of cars or other authored props.
        private static Material CreatePlatformMaterial(Color color, float smoothness, float fill)
        {
            var material = URPMaterialHelper.CreateMaterial(color, smoothness, 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * fill);
            return material;
        }

        private GameObject CreateOil(Vector3 pos)
        {
            var oil = new GameObject("Oil_Slick");
            oil.transform.SetParent(dioramaRoot, false);
            oil.transform.position = pos + Vector3.up * .01f;
            var shadow = CreatePlatformMaterial(new Color(.025f, .15f, .28f), .65f, .15f);
            var rim = CreatePlatformMaterial(new Color(.13f, .78f, .96f), .86f, .36f);
            var slick = CreatePlatformMaterial(new Color(.035f, .34f, .57f), .92f, .28f);
            var sheen = CreatePlatformMaterial(new Color(.24f, .64f, .94f), .85f, .40f);
            var glint = CreatePlatformMaterial(new Color(.80f, .98f, 1f), .72f, .62f);
            // Opaque layered surfaces avoid transparency sorting against tiles and move markers.
            var mesh = DioramaMeshes.OilPuddle;
            DioramaMeshes.MeshObject("Puddle_Shadow", oil.transform, mesh, shadow, Vector3.zero, 0, Vector3.one);
            DioramaMeshes.MeshObject("Puddle_Rim", oil.transform, mesh, rim, new Vector3(0, .027f, 0), 0, new Vector3(.95f, .6f, .95f));
            DioramaMeshes.MeshObject("Puddle", oil.transform, mesh, slick, new Vector3(0, .044f, 0), 0, new Vector3(.86f, .6f, .86f));
            DioramaMeshes.Prim(PrimitiveType.Sphere, "Blue_Sheen", oil.transform, new Vector3(-.12f, .063f, .13f), new Vector3(.67f, .012f, .23f), sheen, new Vector3(0, -25f, 0));
            DioramaMeshes.Prim(PrimitiveType.Sphere, "Gloss_Streak", oil.transform, new Vector3(-.20f, .074f, .21f), new Vector3(.43f, .012f, .075f), glint, new Vector3(0, -25f, 0));
            DioramaMeshes.Prim(PrimitiveType.Sphere, "Gloss_Dot", oil.transform, new Vector3(.34f, .068f, -.17f), new Vector3(.11f, .014f, .075f), glint);
            return oil;
        }

        private GameObject CreateTargetSlot(Vector3 pos, Direction dir)
        {
            var target = new GameObject("Target_Parking_Slot");
            target.transform.SetParent(dioramaRoot, false);
            target.transform.position = pos + Vector3.up * .005f;
            target.transform.rotation = Quaternion.Euler(0, LevelData.DirectionToEulerY(dir), 0);
            var t = target.transform;

            var shadow = CreatePlatformMaterial(new Color(.05f, .38f, .12f), .65f, .20f);
            var frame = CreatePlatformMaterial(BayGreen, .82f, .38f);
            var lip = CreatePlatformMaterial(new Color(.66f, 1f, .48f), .76f, .48f);
            var fill = CreatePlatformMaterial(new Color(.12f, .65f, .22f), .65f, .32f);
            var whiteMat = CreatePlatformMaterial(new Color(1f, 1f, .92f), .65f, .55f);

            float s = tileSize * .92f;
            DioramaMeshes.RoundedBlock("Bay_Shadow", t, new Vector3(0, .025f, 0), new Vector3(s, .07f, s), .22f, .025f, shadow);
            DioramaMeshes.RoundedBlock("Bay_Frame", t, new Vector3(0, .075f, 0), new Vector3(s - .04f, .13f, s - .04f), .21f, .055f, frame);
            DioramaMeshes.RoundedBlock("Bay_Inner_Lip", t, new Vector3(0, .14f, 0), new Vector3(s - .25f, .045f, s - .25f), .16f, .018f, lip);
            DioramaMeshes.RoundedBlock("Bay_Fill", t, new Vector3(0, .163f, 0), new Vector3(s - .34f, .045f, s - .34f), .13f, .018f, fill);

            DioramaMeshes.RoundedBlock("Bay_Glint", t, new Vector3(-.42f, .148f, .79f), new Vector3(.48f, .018f, .055f), .025f, .007f, whiteMat);

            // Preserve the original white P, leaving a separate lane for the heading arrow.
            DioramaMeshes.RoundedBlock("P_Spine", t, new Vector3(-.42f, .215f, 0), new Vector3(.19f, .06f, .90f), .06f, .02f, whiteMat);
            DioramaMeshes.RoundedBlock("P_Top", t, new Vector3(-.135f, .215f, .355f), new Vector3(.57f, .06f, .19f), .06f, .02f, whiteMat);
            DioramaMeshes.RoundedBlock("P_Mid", t, new Vector3(-.135f, .215f, -.015f), new Vector3(.57f, .06f, .19f), .06f, .02f, whiteMat);
            DioramaMeshes.RoundedBlock("P_Loop", t, new Vector3(.095f, .215f, .17f), new Vector3(.19f, .06f, .46f), .065f, .02f, whiteMat);

            // Local +Z is the required car facing direction, not the approach direction.
            var marker = new GameObject("Parking_Facing_Indicator").transform;
            marker.SetParent(t, false);
            marker.localPosition = new Vector3(.48f, .205f, 0);
            DioramaMeshes.MeshObject("Heading_Outline", marker, DioramaMeshes.TriangleArrow, shadow,
                Vector3.zero, 0, new Vector3(.64f, .22f, 1.20f));
            DioramaMeshes.MeshObject("Heading_Arrow", marker, DioramaMeshes.TriangleArrow, whiteMat,
                new Vector3(0, .025f, 0), 0, new Vector3(.56f, .22f, 1.08f));
            foreach (var renderer in marker.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            var motion = GetComponent<BoardEffects>() ?? gameObject.AddComponent<BoardEffects>();
            motion.BindBay(t);
            return target;
        }

        public Vector3 CoordToWorldPosition(Vector2Int coord)
        {
            if (currentLevel == null) return Vector3.zero;
            Vector3 origin = GetGridOrigin(currentLevel.gridWidth, currentLevel.gridHeight);
            return new Vector3(origin.x + coord.x * tileSize, 0, origin.z + coord.y * tileSize);
        }

        public Vector3 GetGridOrigin(int width, int height)
        {
            return new Vector3(-((width - 1) * tileSize) * .5f, 0, -((height - 1) * tileSize) * .5f);
        }

        public void DisplayGhostNodes(List<ValidMoveData> validMoves, Action<ValidMoveData> onNodeSelected)
        {
            ClearGhostNodes();
            foreach (var move in validMoves)
            {
                // Keep movement discs from covering the parking direction marking.
                if (currentLevel != null && move.destination == currentLevel.targetCoord) continue;
                Vector3 wPos = CoordToWorldPosition(move.destination) + Vector3.up * .08f;
                GameObject nodeObj = CreateGhostNode(wPos, move);
                nodeObj.GetComponent<GhostNodeView>().Initialize(move, () => onNodeSelected?.Invoke(move));
                activeGhostNodes.Add(nodeObj);
            }
        }

        // Golden disc with a white arrow showing where the move sends the car.
        private GameObject CreateGhostNode(Vector3 pos, ValidMoveData move)
        {
            var node = new GameObject($"GhostNode_{move.moveType}");
            node.transform.SetParent(dioramaRoot, false);
            node.transform.position = pos;
            var collider = node.AddComponent<BoxCollider>();
            collider.size = new Vector3(tileSize * .9f, .4f, tileSize * .9f);

            var shadow = CreatePlatformMaterial(move.isOilSlip ? new Color(.04f, .38f, .57f) : new Color(.72f, .34f, .02f), .65f, .22f);
            var ring = CreatePlatformMaterial(move.isOilSlip ? new Color(.65f, .97f, 1f) : new Color(1f, .95f, .61f), .78f, .52f);
            var face = CreatePlatformMaterial(move.isOilSlip ? GhostOil : GhostGold, .80f, .42f);
            var arrowMat = CreatePlatformMaterial(new Color(1f, 1f, .95f), .62f, .62f);
            DioramaMeshes.RoundedBlock("Disc_Shadow", node.transform, new Vector3(0, -.015f, 0), new Vector3(1.42f, .10f, 1.42f), .71f, .035f, shadow);
            DioramaMeshes.RoundedBlock("Disc_Rim", node.transform, new Vector3(0, .045f, 0), new Vector3(1.39f, .10f, 1.39f), .695f, .035f, ring);
            DioramaMeshes.RoundedBlock("Disc", node.transform, new Vector3(0, .095f, 0), new Vector3(1.20f, .065f, 1.20f), .60f, .025f, face);

            float yaw = LevelData.DirectionToEulerY(move.resultingDirection);
            if (move.moveType == MoveType.Reverse) yaw += 180f;
            DioramaMeshes.MeshObject("Arrow_Shadow", node.transform, DioramaMeshes.StemArrow, shadow, new Vector3(.025f, .135f, -.025f), yaw, new Vector3(.91f, .55f, .91f));
            DioramaMeshes.MeshObject("Arrow", node.transform, DioramaMeshes.StemArrow, arrowMat, new Vector3(0, .157f, 0), yaw, new Vector3(.85f, .70f, .85f));
            DioramaMeshes.RoundedBlock("Disc_Glint", node.transform, new Vector3(-.24f, .137f, .43f), new Vector3(.27f, .018f, .06f), .025f, .007f, arrowMat);

            node.AddComponent<GhostNodeView>();
            return node;
        }

        public void ClearGhostNodes()
        {
            foreach (var node in activeGhostNodes)
            {
                if (node == null) continue;
                node.SetActive(false);
                Destroy(node);
            }
            activeGhostNodes.Clear();
        }
    }
}
