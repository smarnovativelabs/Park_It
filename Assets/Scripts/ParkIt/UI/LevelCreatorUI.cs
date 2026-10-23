using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>
    /// "Diorama Level Creator": paint obstacles, oil slicks, the start car and the target bay on a grid,
    /// tune fuel and headings, see the solver verdict live, then Test &amp; Play the design.
    /// </summary>
    public class LevelCreatorUI : MonoBehaviour
    {
        private enum Brush { Obstacle, Oil, Car, Target, Eraser }

        private const int MinGrid = 3, MaxGrid = 9, MinFuel = 1, MaxFuel = 30;

        public ParkItUIManager UI { get => ui; set => ui = value; }
        private ParkItUIManager ui;
        private LevelData draft;
        private Brush brush = Brush.Obstacle;

        private RectTransform panel, body, settingsScroll, preview, gridFrame;
        private GridLayoutGroup grid, brushGrid, actionGrid;
        private Text widthValue, heightValue, fuelValue, brushCaption, carHeadingText, targetHeadingText, solverText;
        private Image solverIcon;
        private Slider fuelSlider;
        private readonly Dictionary<Brush, (Image bg, Text label)> brushButtons = new Dictionary<Brush, (Image, Text)>();
        private readonly List<(Vector2Int coord, Image bg, Image icon, Text label)> cells = new List<(Vector2Int, Image, Image, Text)>();
        private bool portrait;
        private bool suppressSlider;
        private bool authoredLayout;
        private readonly Dictionary<Brush, Sprite> brushSprites = new Dictionary<Brush, Sprite>();
        private readonly Dictionary<Brush, Color> brushColors = new Dictionary<Brush, Color>();

        public bool IsOpen => gameObject.activeSelf;

        // Baked canvases retain the hierarchy, but these runtime references and listeners
        // must be restored after a scene load or script reload.
        private void BindExistingHierarchy()
        {
            if (panel != null) return;
            panel = (RectTransform)transform.Find("Panel");
            authoredLayout = true;
            body = (RectTransform)panel.Find("Body");
            var scroll = body.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            settingsScroll = (RectTransform)scroll.transform;
            var content = scroll.content;
            preview = (RectTransform)body.Find("Preview");
            gridFrame = (RectTransform)preview.Find("GridFrame");
            grid = gridFrame.Find("Cells").GetComponent<UnityEngine.UI.GridLayoutGroup>();
            brushGrid = content.Find("Brushes").GetComponent<UnityEngine.UI.GridLayoutGroup>();
            actionGrid = content.Find("Actions").GetComponent<UnityEngine.UI.GridLayoutGroup>();
            brushCaption = preview.Find("Text").GetComponent<UnityEngine.UI.Text>();
            widthValue = content.Find("SizeRow/Width").GetComponentsInChildren<UnityEngine.UI.Text>(true)[1];
            heightValue = content.Find("SizeRow/Height").GetComponentsInChildren<UnityEngine.UI.Text>(true)[1];
            fuelValue = content.Find("FuelRow").GetComponentsInChildren<UnityEngine.UI.Text>(true)[1];
            fuelSlider = content.Find("FuelSlider").GetComponent<UnityEngine.UI.Slider>();
            carHeadingText = content.Find("Car Heading:/Rotate").GetComponentInChildren<UnityEngine.UI.Text>(true);
            targetHeadingText = content.Find("Target Heading:/Rotate").GetComponentInChildren<UnityEngine.UI.Text>(true);
            solverText = content.Find("Solver").GetComponentInChildren<UnityEngine.UI.Text>(true);
            solverIcon = content.Find("Solver/icon_zap").GetComponent<UnityEngine.UI.Image>();
            BindButton(panel.Find("Header/TestPlay"), TestAndPlay);
            BindButton(panel.Find("Header/Close"), Close);
            BindButton(content.Find("SizeRow/Width/Minus"), () => Resize(draft.gridWidth - 1, draft.gridHeight));
            BindButton(content.Find("SizeRow/Width/Plus"), () => Resize(draft.gridWidth + 1, draft.gridHeight));
            BindButton(content.Find("SizeRow/Height/Minus"), () => Resize(draft.gridWidth, draft.gridHeight - 1));
            BindButton(content.Find("SizeRow/Height/Plus"), () => Resize(draft.gridWidth, draft.gridHeight + 1));
            BindButton(content.Find("Car Heading:/Rotate"), () => { draft.startDirection = LevelData.Turn(draft.startDirection, true); Changed(); });
            BindButton(content.Find("Target Heading:/Rotate"), () => { draft.targetDirection = LevelData.Turn(draft.targetDirection, true); Changed(); });
            BindButton(content.Find("Actions/FromLevel"), CopyFromCurrentLevel);
            BindButton(content.Find("Actions/Clear"), ClearBoard);
            BindButton(content.Find("Actions/Export"), ExportCode);
            BindButton(content.Find("Actions/Import"), ImportCode);
            foreach (Brush value in System.Enum.GetValues(typeof(Brush)))
            {
                var selected = value;
                var button = content.Find(value == Brush.Eraser ? "EraserRow/Eraser" : "Brushes/" + value);
                brushButtons[value] = (button.GetComponent<UnityEngine.UI.Image>(), button.GetComponentInChildren<UnityEngine.UI.Text>(true));
                brushSprites[value] = brushButtons[value].bg.sprite;
                brushColors[value] = brushButtons[value].label.color;
                BindButton(button, () => { brush = selected; RefreshBrushes(); });
            }
            fuelSlider.onValueChanged.RemoveAllListeners();
            fuelSlider.onValueChanged.AddListener(v => { if (!suppressSlider && draft != null) { draft.maxFuel = Mathf.RoundToInt(v); Changed(); } });
        }

        private static void BindButton(Transform target, UnityEngine.Events.UnityAction action)
        {
            var button = target.GetComponent<UnityEngine.UI.Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public static LevelCreatorUI Create(Transform parent, ParkItUIManager ui)
        {
            var root = UIKit.Stretch(UIKit.Rect("LevelCreator", parent));
            var creator = root.gameObject.AddComponent<LevelCreatorUI>();
            creator.ui = ui;
            creator.Build();
            root.gameObject.SetActive(false);
            return creator;
        }

        // ---------- construction ----------
        private void Build()
        {
            UIKit.Dimmer(transform);
            var p = UIKit.Img("Panel", transform, "panel_modal", null, true);
            panel = p.rectTransform;
            UIKit.Place(panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1720, 980));
            UIKit.Layout(p, true, 0, null, TextAnchor.UpperCenter, true);

            // Header
            var header = UIKit.Rect("Header", panel);
            UIKit.Layout(header, false, 22, new RectOffset(40, 32, 0, 0), TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 140);
            UIKit.IconButton("Badge", header, "tile_amber", "icon_sliders", UIKit.Amber, 84, 44, null).interactable = false;
            var titles = UIKit.Rect("Titles", header);
            UIKit.Layout(titles, true, 4, null, TextAnchor.MiddleLeft, true);
            UIKit.Size(titles, -1, -1, 1);
            UIKit.Bold(titles, "Diorama Level Creator", 36, UIKit.TextMain);
            UIKit.Label(titles, "Design your own parking maze, oil slicks, and obstacles", 23, UIKit.TextMuted);
            UIKit.IconTextButton("TestPlay", header, "btn_green", "icon_play", UIKit.White, "Test & Play", UIKit.White, 28, 86, TestAndPlay, 28, 34);
            var close = UIKit.IconButton("Close", header, "white_rounded", "icon_close", UIKit.TextMuted, 66, 38, Close);
            close.targetGraphic.color = new Color(1, 1, 1, 0);
            UIKit.Size(UIKit.Img("Divider", panel, "white", UIKit.C("#dfd0ff")), -1, 2);

            body = UIKit.Rect("Body", panel);
            UIKit.Size(body, -1, -1, 1, 1);
            UIKit.Layout(body, false, 0, null, TextAnchor.UpperLeft, false, true);

            BuildSettings();
            BuildPreview();
            ParkItUIBuilder.StyleGlossyWindow(gameObject);
            ApplyReadableTypography();
        }

        // Explicit authoring operation, never run when binding a saved prefab.
        public void ApplyReadableTypography()
        {
            var p = transform.Find("Panel");
            var scroll = p.Find("Body").GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            var content = scroll.content;
            foreach (var text in content.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                text.fontSize = 32;
                text.resizeTextForBestFit = false;
                text.lineSpacing = 1.12f;
            }
            foreach (var button in content.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var size = button.GetComponent<UnityEngine.UI.LayoutElement>();
                if (size != null)
                {
                    bool stepper = button.name == "Minus" || button.name == "Plus";
                    size.minHeight = size.preferredHeight = stepper ? 64 : 100;
                    if (stepper) size.minWidth = size.preferredWidth = 64;
                }
            }
            foreach (string name in new[] { "Brushes", "Actions" })
            {
                var group = content.Find(name).GetComponent<UnityEngine.UI.GridLayoutGroup>();
                group.cellSize = new Vector2(group.cellSize.x, 100);
                UIKit.Size(group, -1, 216);
            }
            foreach (string name in new[] { "Width", "Height" })
                UIKit.Size(content.Find("SizeRow/" + name), -1, 100, 1);
            var caption = p.Find("Body/Preview/Text").GetComponent<UnityEngine.UI.Text>();
            caption.fontSize = 30;
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIKit.Size(caption, 900, 80);
            var titles = p.Find("Header/Titles").GetComponentsInChildren<UnityEngine.UI.Text>(true);
            titles[0].fontSize = 40;
            titles[1].fontSize = 26;
        }

        private void BuildSettings()
        {
            var scroll = ParkItUIManager.BuildScroll(body, out var content);
            settingsScroll = (RectTransform)scroll.transform;
            UIKit.Size(scroll, 600, -1, -1, 1);
            UIKit.Layout(content, true, 18, new RectOffset(40, 30, 34, 40), TextAnchor.UpperLeft, true);
            UIKit.Fit(content, false, true);

            Section(content, "GRID SIZE");
            var sizeRow = UIKit.Rect("SizeRow", content);
            UIKit.Layout(sizeRow, false, 16, null, TextAnchor.MiddleLeft, true);
            widthValue = Stepper(sizeRow, "Width", d => Resize(draft.gridWidth + d, draft.gridHeight));
            heightValue = Stepper(sizeRow, "Height", d => Resize(draft.gridWidth, draft.gridHeight + d));

            var fuelRow = UIKit.Rect("FuelRow", content);
            UIKit.Layout(fuelRow, false, 10, null, TextAnchor.MiddleLeft);
            Section(fuelRow, "MAX FUEL (MOVES)");
            UIKit.Size(UIKit.Rect("Flex", fuelRow), -1, -1, 1);
            fuelValue = UIKit.Caps(fuelRow, "8", 28, UIKit.Emerald, TextAnchor.MiddleRight);
            fuelValue.horizontalOverflow = HorizontalWrapMode.Overflow;
            fuelSlider = BuildSlider(content);

            UIKit.Size(UIKit.Rect("Gap", content), 4, 4);
            Section(content, "PLACEMENT BRUSH");
            var brushes = UIKit.Rect("Brushes", content);
            var bg = brushGrid = brushes.gameObject.AddComponent<GridLayoutGroup>();
            bg.cellSize = new Vector2(256, 78);
            bg.spacing = new Vector2(16, 16);
            bg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            bg.constraintCount = 2;
            UIKit.Size(brushes, -1, 78 * 2 + 16);
            BrushButton(brushes, Brush.Obstacle, "icon_obstacle", "Obstacle", UIKit.Amber);
            BrushButton(brushes, Brush.Oil, "icon_oil", "Oil Slick", UIKit.Sky);
            BrushButton(brushes, Brush.Car, "icon_car", "Start Car", UIKit.Rose);
            BrushButton(brushes, Brush.Target, "icon_target", "Target Bay", UIKit.Emerald);
            var eraser = UIKit.Rect("EraserRow", content);
            UIKit.Layout(eraser, false, 0, null, TextAnchor.MiddleCenter, true);
            BrushButton(eraser, Brush.Eraser, "icon_eraser", "Eraser (Clear Cell)", UIKit.C("#c084fc"));

            UIKit.Size(UIKit.Img("Divider", content, "white", UIKit.C("#dfd0ff")), -1, 2);
            carHeadingText = HeadingRow(content, "icon_car", UIKit.Rose, "Car Heading:", UIKit.Amber, () => { draft.startDirection = LevelData.Turn(draft.startDirection, true); Changed(); });
            targetHeadingText = HeadingRow(content, "icon_target", UIKit.Emerald, "Target Heading:", UIKit.Emerald, () => { draft.targetDirection = LevelData.Turn(draft.targetDirection, true); Changed(); });
            UIKit.Size(UIKit.Img("Divider", content, "white", UIKit.C("#dfd0ff")), -1, 2);

            var solver = UIKit.Img("Solver", content, "panel_inset");
            UIKit.Layout(solver, false, 16, new RectOffset(22, 22, 18, 18), TextAnchor.MiddleLeft);
            solverIcon = UIKit.Icon(solver.transform, "icon_zap", UIKit.Emerald, 36);
            solverText = UIKit.Label(solver.transform, "", 24, UIKit.TextSoft);
            UIKit.Size(solverText, -1, -1, 1);

            var actions = UIKit.Rect("Actions", content);
            var ag = actionGrid = actions.gameObject.AddComponent<GridLayoutGroup>();
            ag.cellSize = new Vector2(256, 72);
            ag.spacing = new Vector2(16, 16);
            ag.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ag.constraintCount = 2;
            UIKit.Size(actions, -1, 72 * 2 + 16);
            UIKit.IconTextButton("FromLevel", actions, "btn_secondary", "icon_copy", UIKit.Sky, "Copy Level", UIKit.TextMain, 22, 72, CopyFromCurrentLevel, 26, 12);
            UIKit.IconTextButton("Clear", actions, "btn_secondary", "icon_trash", UIKit.Rose, "Clear Board", UIKit.TextMain, 22, 72, ClearBoard, 26, 12);
            UIKit.IconTextButton("Export", actions, "btn_secondary", "icon_code", UIKit.Amber, "Export Code", UIKit.TextMain, 22, 72, ExportCode, 26, 12);
            UIKit.IconTextButton("Import", actions, "btn_secondary", "icon_save", UIKit.Emerald, "Import Code", UIKit.TextMain, 22, 72, ImportCode, 26, 12);
        }

        private void BuildPreview()
        {
            preview = UIKit.Rect("Preview", body);
            UIKit.Size(preview, -1, -1, 1, 1);
            UIKit.Layout(preview, true, 26, new RectOffset(30, 30, 30, 30), TextAnchor.MiddleCenter);

            var frame = UIKit.Img("GridFrame", preview, "grid_frame");
            gridFrame = frame.rectTransform;
            UIKit.Layout(frame, false, 0, new RectOffset(22, 22, 22, 22));
            var cellsRoot = UIKit.Rect("Cells", frame.transform);
            grid = cellsRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.spacing = new Vector2(12, 12);

            brushCaption = UIKit.Label(preview, "", 24, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);
            brushCaption.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static void Section(Transform parent, string text)
        {
            UIKit.Caps(parent, text, 23, UIKit.TextSoft).horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private Text Stepper(Transform parent, string label, System.Action<int> onStep)
        {
            var box = UIKit.Img(label, parent, "panel_section");
            UIKit.Layout(box, false, 6, new RectOffset(22, 10, 0, 0), TextAnchor.MiddleLeft);
            UIKit.Size(box, -1, 84, 1);
            UIKit.Label(box.transform, label, 24, UIKit.TextMuted).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", box.transform), -1, -1, 1);
            var minus = UIKit.IconButton("Minus", box.transform, "white_rounded", "icon_minus", UIKit.TextSoft, 54, 28, () => onStep(-1));
            minus.targetGraphic.color = new Color(1, 1, 1, 0);
            var value = UIKit.Caps(box.transform, "5", 28, UIKit.Amber, TextAnchor.MiddleCenter);
            UIKit.Size(value, 40, -1);
            var plus = UIKit.IconButton("Plus", box.transform, "white_rounded", "icon_plus", UIKit.TextSoft, 54, 28, () => onStep(1));
            plus.targetGraphic.color = new Color(1, 1, 1, 0);
            return value;
        }

        private Slider BuildSlider(Transform parent)
        {
            var root = UIKit.Rect("FuelSlider", parent);
            UIKit.Size(root, -1, 48);
            var track = UIKit.Img("Track", root, "bar_track", null, true);
            track.rectTransform.anchorMin = new Vector2(0, .5f);
            track.rectTransform.anchorMax = new Vector2(1, .5f);
            track.rectTransform.sizeDelta = new Vector2(0, 16);
            var fillArea = UIKit.Rect("FillArea", root);
            fillArea.anchorMin = new Vector2(0, .5f);
            fillArea.anchorMax = new Vector2(1, .5f);
            fillArea.sizeDelta = new Vector2(-20, 16);
            var fill = UIKit.Img("Fill", fillArea, "bar_fill_orange");
            fill.rectTransform.sizeDelta = new Vector2(20, 0);
            var handleArea = UIKit.Rect("HandleArea", root);
            UIKit.Stretch(handleArea);
            handleArea.offsetMin = new Vector2(22, 0);
            handleArea.offsetMax = new Vector2(-22, 0);
            var handle = UIKit.Img("Knob", handleArea, "knob", null, true);
            handle.rectTransform.sizeDelta = new Vector2(44, 0);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = MinFuel;
            slider.maxValue = MaxFuel;
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(v =>
            {
                if (suppressSlider || draft == null) return;
                draft.maxFuel = Mathf.RoundToInt(v);
                Changed();
            });
            return slider;
        }

        private void BrushButton(Transform parent, Brush b, string icon, string label, Color color)
        {
            var btn = UIKit.IconTextButton(b.ToString(), parent, "btn_secondary", icon, color, label, UIKit.TextMain, 24, 78, () => { brush = b; RefreshBrushes(); }, 30, 18);
            var t = btn.GetComponentInChildren<Text>();
            t.fontStyle = FontStyle.Normal;
            brushButtons[b] = ((Image)btn.targetGraphic, t);
        }

        private Text HeadingRow(Transform parent, string icon, Color iconColor, string label, Color accent, UnityEngine.Events.UnityAction onRotate)
        {
            var row = UIKit.Rect(label, parent);
            UIKit.Layout(row, false, 14, null, TextAnchor.MiddleLeft);
            UIKit.Icon(row, icon, iconColor, 32);
            UIKit.Label(row, label, 25, UIKit.TextSoft).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", row), -1, -1, 1);
            var btn = UIKit.IconTextButton("Rotate", row, "btn_dark", "icon_rotate_cw", accent, "N", UIKit.TextMain, 24, 62, onRotate, 26, 18);
            var t = btn.GetComponentInChildren<Text>();
            t.font = UIKit.Mono;
            return t;
        }

        // ---------- open / close ----------
        public void Open(LevelData current)
        {
            BindExistingHierarchy();
            if (draft == null) draft = LoadDraft() ?? FromLevel(current);
            UIPopupTransition.Show(gameObject);
            transform.SetAsLastSibling();
            RebuildGrid();
            Changed(false);
            RefreshBrushes();
        }

        public void Close() => UIPopupTransition.Hide(gameObject);

        public void ApplyLayout(bool isPortrait)
        {
            BindExistingHierarchy();
            portrait = isPortrait;
            if (authoredLayout && (panel.sizeDelta.y >= panel.sizeDelta.x) == portrait)
            {
                if (draft != null) RebuildGrid();
                return;
            }
            panel.sizeDelta = portrait ? new Vector2(1030, 1740) : new Vector2(1720, 980);
            UIKit.Layout(body, portrait, 0, null, TextAnchor.UpperLeft, true, !portrait);
            UIKit.Size(settingsScroll, portrait ? -1 : 760, portrait ? 900 : -1, portrait ? 1 : -1, portrait ? -1 : 1);
            settingsScroll.SetSiblingIndex(portrait ? 1 : 0);
            UIKit.Size(preview, -1, portrait ? 698 : -1, 1, portrait ? -1 : 1);
            var header = (RectTransform)panel.Find("Header");
            UIKit.Size(header, -1, 140);
            UIKit.Size(header.Find("TestPlay"), portrait ? 210 : -1, 86);
            // Settings content = scroll width - padding (40 + 30) - scrollbar gutter (30).
            float contentW = (portrait ? panel.sizeDelta.x : 760) - 100;
            float cellW = Mathf.Floor((contentW - 16) / 2f);
            brushGrid.cellSize = new Vector2(cellW, 100);
            actionGrid.cellSize = new Vector2(cellW, 100);
            if (draft != null) RebuildGrid();
        }

        // ---------- editing ----------
        private void Resize(int w, int h)
        {
            draft.gridWidth = Mathf.Clamp(w, MinGrid, MaxGrid);
            draft.gridHeight = Mathf.Clamp(h, MinGrid, MaxGrid);
            draft.obstacles.RemoveAll(c => !draft.IsWithinBounds(c));
            draft.oilSpills.RemoveAll(c => !draft.IsWithinBounds(c));
            draft.startCoord = Clamp(draft.startCoord);
            draft.targetCoord = Clamp(draft.targetCoord);
            RebuildGrid();
            Changed();
        }

        private Vector2Int Clamp(Vector2Int c) => new Vector2Int(Mathf.Clamp(c.x, 0, draft.gridWidth - 1), Mathf.Clamp(c.y, 0, draft.gridHeight - 1));

        private void Paint(Vector2Int c)
        {
            bool isStart = c == draft.startCoord, isTarget = c == draft.targetCoord;
            switch (brush)
            {
                case Brush.Obstacle:
                case Brush.Oil:
                    if (isStart || isTarget) { ui.Toast("Move the car or bay first — that tile is taken"); return; }
                    var list = brush == Brush.Obstacle ? draft.obstacles : draft.oilSpills;
                    var other = brush == Brush.Obstacle ? draft.oilSpills : draft.obstacles;
                    if (list.Contains(c)) list.Remove(c); else { list.Add(c); other.Remove(c); }
                    break;
                case Brush.Car:
                    draft.obstacles.Remove(c); draft.oilSpills.Remove(c);
                    if (isStart) draft.startDirection = LevelData.Turn(draft.startDirection, true);
                    draft.startCoord = c;
                    break;
                case Brush.Target:
                    draft.obstacles.Remove(c); draft.oilSpills.Remove(c);
                    if (isTarget) draft.targetDirection = LevelData.Turn(draft.targetDirection, true);
                    draft.targetCoord = c;
                    break;
                default:
                    draft.obstacles.Remove(c); draft.oilSpills.Remove(c);
                    break;
            }
            Changed();
        }

        private void ClearBoard()
        {
            draft.obstacles.Clear();
            draft.oilSpills.Clear();
            Changed();
            ui.Toast("Board cleared");
        }

        private void CopyFromCurrentLevel()
        {
            var level = GameManager.Instance != null ? GameManager.Instance.CurrentLevelData : null;
            if (level == null) return;
            draft = FromLevel(level);
            RebuildGrid();
            Changed();
            ui.Toast($"Loaded \"{level.levelTitle}\" into the creator");
        }

        private void ExportCode()
        {
            GUIUtility.systemCopyBuffer = JsonUtility.ToJson(draft, true);
            ui.Toast("Design code copied to clipboard");
        }

        private void ImportCode()
        {
            var text = GUIUtility.systemCopyBuffer;
            var imported = ScriptableObject.CreateInstance<LevelData>();
            try { JsonUtility.FromJsonOverwrite(text, imported); }
            catch (System.Exception) { ui.Toast("Clipboard doesn't hold a Park It level code"); return; }
            if (string.IsNullOrEmpty(text) || !text.Contains("gridWidth")) { ui.Toast("Clipboard doesn't hold a Park It level code"); return; }
            draft = FromLevel(imported);
            RebuildGrid();
            Changed();
            ui.Toast("Design imported from clipboard");
        }

        private void TestAndPlay()
        {
            var level = draft.Clone();
            level.name = "Custom_Diorama";
            level.levelIndex = 0;
            level.levelTitle = "Custom Diorama";
            level.objectiveHint = "Your own creation";
            level.difficulty = LevelDifficulty.Custom;
            var solution = LevelSolver.Solve(level);
            level.optimalMoves = solution.solvable ? solution.moves : level.maxFuel;
            if (!solution.solvable) ui.Toast("Heads up: the solver found no route to the bay");
            else if (solution.moves > level.maxFuel) ui.Toast($"Heads up: needs {solution.moves} moves but fuel is {level.maxFuel}");
            Close();
            ui.ShowTitle(false);
            GameManager.Instance?.PlayCustomLevel(level);
        }

        private void Changed(bool save = true)
        {
            if (save) ProgressSave.Instance.SetCreatorDraft(JsonUtility.ToJson(draft));
            widthValue.text = draft.gridWidth.ToString();
            heightValue.text = draft.gridHeight.ToString();
            fuelValue.text = draft.maxFuel.ToString();
            suppressSlider = true;
            fuelSlider.value = draft.maxFuel;
            suppressSlider = false;
            carHeadingText.text = draft.startDirection.ToString().Substring(0, 1);
            targetHeadingText.text = draft.targetDirection.ToString().Substring(0, 1);
            RefreshCells();
            RefreshSolver();
        }

        private void RefreshSolver()
        {
            var s = LevelSolver.Solve(draft);
            if (!s.solvable)
            {
                solverText.text = "<b>No route</b> to the bay with the required heading. Clear a path or rotate the target.";
                solverIcon.color = UIKit.Rose;
            }
            else if (s.moves > draft.maxFuel)
            {
                solverText.text = $"Needs <b>{s.moves} moves</b> but the tank only holds {draft.maxFuel}. Add fuel or simplify.";
                solverIcon.color = UIKit.Amber;
            }
            else
            {
                solverText.text = $"Solvable in <b>{s.moves} moves</b> — finish in {s.moves} for 3 stars.";
                solverIcon.color = UIKit.Emerald;
            }
        }

        private void RefreshBrushes()
        {
            foreach (var pair in brushButtons)
            {
                bool active = pair.Key == brush;
                if (!active && brushSprites.TryGetValue(pair.Key, out var savedSprite)) pair.Value.bg.sprite = savedSprite;
                else UIKit.SetSprite(pair.Value.bg, active ? "dock_yellow" : "dock_utility");
                pair.Value.label.color = active ? UIKit.Ink : brushColors.TryGetValue(pair.Key, out var savedColor) ? savedColor : UIKit.TextMain;
            }
            brushCaption.text = $"Click tiles to paint. Selected brush: <color=#7d35f5><b>{BrushName(brush)}</b></color>";
        }

        private static string BrushName(Brush b)
        {
            switch (b)
            {
                case Brush.Oil: return "OIL SLICK";
                case Brush.Car: return "START CAR";
                case Brush.Target: return "TARGET BAY";
                case Brush.Eraser: return "ERASER";
                default: return "OBSTACLE";
            }
        }

        // ---------- grid ----------
        private void RebuildGrid()
        {
            foreach (var c in cells)
            {
                if (c.bg == null) continue;
                c.bg.gameObject.SetActive(false);
                Destroy(c.bg.gameObject);
            }
            cells.Clear();

            float areaW = portrait ? 900 : 840, areaH = portrait ? 560 : 690;
            float cell = Mathf.Floor(Mathf.Clamp(Mathf.Min((areaW - 44) / draft.gridWidth, (areaH - 44) / draft.gridHeight) - grid.spacing.x, 56, 132));
            grid.cellSize = new Vector2(cell, cell);
            grid.constraintCount = draft.gridWidth;
            UIKit.Size(grid, draft.gridWidth * (cell + grid.spacing.x) - grid.spacing.x, draft.gridHeight * (cell + grid.spacing.y) - grid.spacing.y);

            for (int row = 0; row < draft.gridHeight; row++)
            {
                for (int x = 0; x < draft.gridWidth; x++)
                {
                    var coord = new Vector2Int(x, draft.gridHeight - 1 - row);
                    var b = UIKit.Button($"Cell_{coord.x}_{coord.y}", grid.transform, "cell", () => Paint(coord));
                    var icon = UIKit.Img("Icon", b.transform, "icon_obstacle");
                    UIKit.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, cell * .1f), new Vector2(cell * .42f, cell * .42f));
                    var label = UIKit.Caps(b.transform, "", Mathf.RoundToInt(Mathf.Clamp(cell * .28f, 20, 30)), UIKit.TextSoft, TextAnchor.MiddleCenter);
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    UIKit.Stretch(label.rectTransform);
                    cells.Add((coord, (Image)b.targetGraphic, icon, label));
                }
            }
            RefreshCells();
        }

        private void RefreshCells()
        {
            float cell = grid.cellSize.x;
            foreach (var c in cells)
            {
                string sprite = "cell", icon = null, text = $"{c.coord.x},{c.coord.y}";
                Color iconColor = Color.white, textColor = UIKit.TextDim;
                bool start = c.coord == draft.startCoord, target = c.coord == draft.targetCoord;
                if (draft.obstacles.Contains(c.coord)) { sprite = "cell_obstacle"; icon = "icon_obstacle"; iconColor = UIKit.Amber; text = ""; }
                else if (draft.oilSpills.Contains(c.coord)) { sprite = "cell_oil"; icon = "icon_oil"; iconColor = UIKit.Sky; text = ""; }
                if (target) { sprite = "cell_target"; icon = "icon_target"; iconColor = UIKit.Emerald; text = Letter(draft.targetDirection); textColor = UIKit.Emerald; }
                if (start)
                {
                    sprite = "cell_car"; icon = "icon_car"; iconColor = UIKit.Rose; textColor = UIKit.Rose;
                    text = target ? $"{Letter(draft.startDirection)}→{Letter(draft.targetDirection)}" : Letter(draft.startDirection);
                }
                UIKit.SetSprite(c.bg, sprite);
                c.icon.enabled = icon != null;
                if (icon != null) { UIKit.SetSprite(c.icon, icon); c.icon.color = iconColor; }
                c.label.text = text;
                c.label.color = textColor;
                c.label.rectTransform.offsetMin = new Vector2(0, icon != null ? 6 : 0);
                c.label.rectTransform.offsetMax = new Vector2(0, icon != null ? -cell * .62f : 0);
                c.label.alignment = icon != null ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
            }
        }

        private static string Letter(Direction d) => d.ToString().Substring(0, 1);

        // ---------- persistence ----------
        private static LevelData LoadDraft()
        {
            var json = ProgressSave.Instance.CreatorDraftJson;
            if (string.IsNullOrEmpty(json)) return null;
            var level = ScriptableObject.CreateInstance<LevelData>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, level);
                return FromLevel(level);
            }
            catch (System.ArgumentException)
            {
                Debug.LogWarning("[Park It] Saved creator draft is invalid; opening a new draft.");
                return null;
            }
            finally { Object.Destroy(level); }
        }

        private static LevelData FromLevel(LevelData source)
        {
            var d = source != null ? source.Clone() : ScriptableObject.CreateInstance<LevelData>();
            d.gridWidth = Mathf.Clamp(d.gridWidth, MinGrid, MaxGrid);
            d.gridHeight = Mathf.Clamp(d.gridHeight, MinGrid, MaxGrid);
            d.maxFuel = Mathf.Clamp(d.maxFuel, MinFuel, MaxFuel);
            if (d.obstacles == null) d.obstacles = new List<Vector2Int>();
            if (d.oilSpills == null) d.oilSpills = new List<Vector2Int>();
            d.obstacles.RemoveAll(c => !d.IsWithinBounds(c));
            d.oilSpills.RemoveAll(c => !d.IsWithinBounds(c));
            return d;
        }
    }
}
