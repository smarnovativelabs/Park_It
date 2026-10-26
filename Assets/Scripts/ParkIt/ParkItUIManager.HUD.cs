using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    public partial class ParkItUIManager
    {
        private class DriveButton
        {
            public Button button;
            public Image background;
            public string enabledSprite;
            public Sprite authoredSprite;
            public CanvasGroup group;
            public Image ring;
            public float flash;
        }

        // Top-left level card
        private Text levelNumberText, levelTitleText;
        private Image levelBadge;
        // Top-right cluster
        private RectTransform levelCard, topRight, topRowA, topRowB, targetPill, fuelPill, toolbar;
        private Text targetHeadingText, fuelText, fuelMaxText;
        private Image targetArrow, soundIcon;
        // Dock
        private RectTransform dockRoot, dockPanel, dockUtility, dockDrive, dockSpacer, bottomStack;
        private Button undoButton;
        private CanvasGroup undoGroup;
        private GameObject undoBadge;
        private Text undoCountText, camLabel, zenLabel;
        private Image zenIcon;
        private Button carColorButton;
        private Text carColorLabel;
        private Image carColorPreview;
        private Image[] carColorSwatches;
        private Image swatchRing;
        private bool zenMode;
        private Color normalFuelColor;
        private Vector3 authoredLevelScale, authoredTopRightScale;
        private readonly Dictionary<MoveType, DriveButton> driveButtons = new Dictionary<MoveType, DriveButton>();

        public void BindHUD(ParkItUIReferences refs)
        {
            if (refs == null) return;

            ParkItUIBuilder.ResolveBindings(refs);
            ParkItUIBuilder.EnsureHintButton(refs);
            ParkItUIBuilder.EnsureShiftControls(refs);
            UITapFeedback.Attach(refs.hintButton);
            UITapFeedback.Attach(refs.shiftLeftBtn);
            UITapFeedback.Attach(refs.shiftRightBtn);
            if (refs.hintButton != null)
            {
                refs.hintButton.onClick.RemoveAllListeners();
                refs.hintButton.onClick.AddListener(ShowLevelHint);
            }
            hintUndoButton = refs.undoButton;
            hintRestartButton = refs.restartButton;
            levelCard = refs.levelCard;
            levelNumberText = refs.levelNumberText;
            levelTitleText = refs.levelTitleText;
            levelBadge = refs.levelBadge;
            if (refs.levelHelpButton != null)
            {
                refs.levelHelpButton.onClick.RemoveAllListeners();
                refs.levelHelpButton.onClick.AddListener(OpenHelp);
            }
            if (refs.levelCardButton != null)
            {
                refs.levelCardButton.onClick.RemoveAllListeners();
                refs.levelCardButton.onClick.AddListener(OpenLevelSelect);
            }

            topRight = refs.topRight;
            authoredLevelScale = levelCard.localScale;
            authoredTopRightScale = topRight.localScale;
            toolbar = refs.toolbar;
            if (refs.levelDesignButton != null)
            {
                refs.levelDesignButton.onClick.RemoveAllListeners();
                refs.levelDesignButton.onClick.AddListener(OpenCreator);
            }
            if (refs.pauseButton != null)
            {
                refs.pauseButton.onClick.RemoveAllListeners();
                refs.pauseButton.onClick.AddListener(OpenPause);
            }

            fuelPill = refs.fuelPill;
            fuelText = refs.fuelText;
            normalFuelColor = fuelText.color;
            fuelMaxText = refs.fuelMaxText;

            targetPill = refs.targetPill;
            targetHeadingText = refs.targetHeadingText;
            targetArrow = refs.targetArrow;

            carColorButton = refs.carColorButton;
            carColorLabel = refs.carColorLabel;
            carColorPreview = refs.carColorPreview;
            carColorSwatches = refs.carColorSwatches;
            swatchRing = refs.swatchRing;
            if (refs.carColorButton != null)
            {
                refs.carColorButton.onClick.RemoveAllListeners();
                refs.carColorButton.onClick.AddListener(CycleCarColor);
            }

            bottomStack = refs.bottomStack;
            dockPanel = refs.dockPanel;
            dockUtility = refs.dockUtility;
            dockDrive = refs.dockDrive;
            dockRoot = refs.dockPanel;

            undoButton = refs.undoButton;
            undoGroup = refs.undoGroup;
            undoBadge = refs.undoBadge;
            undoCountText = refs.undoCountText;
            if (refs.undoButton != null)
            {
                refs.undoButton.onClick.RemoveAllListeners();
                refs.undoButton.onClick.AddListener(OnClickUndo);
            }

            if (refs.restartButton != null)
            {
                refs.restartButton.onClick.RemoveAllListeners();
                refs.restartButton.onClick.AddListener(OnClickRestart);
            }

            camLabel = refs.camLabel;
            if (refs.camButton != null)
            {
                refs.camButton.onClick.RemoveAllListeners();
                refs.camButton.onClick.AddListener(CycleCamera);
            }

            zenLabel = refs.zenLabel;
            zenIcon = refs.zenIcon;
            if (refs.zenButton != null)
            {
                refs.zenButton.onClick.RemoveAllListeners();
                refs.zenButton.onClick.AddListener(ToggleZen);
            }

            driveButtons.Clear();
            foreach (var button in new[] { refs.shiftLeftBtn, refs.shiftRightBtn })
            {
                if (button == null) continue;
                button.onClick.RemoveAllListeners();
                button.interactable = false;
                button.gameObject.SetActive(false);
            }
            BindDriveButton(MoveType.Forward, refs.forwardBtn, refs.forwardBg, refs.forwardRing, "dock_drive", OnClickDrive);
            BindDriveButton(MoveType.TurnLeft, refs.turnLeftBtn, refs.turnLeftBg, refs.turnLeftRing, "dock_yellow", OnClickTurnLeft);
            BindDriveButton(MoveType.TurnRight, refs.turnRightBtn, refs.turnRightBg, refs.turnRightRing, "dock_yellow", OnClickTurnRight);
            BindDriveButton(MoveType.Reverse, refs.reverseBtn, refs.reverseBg, refs.reverseRing, "dock_yellow", OnClickReverse);
        }

        private void BindDriveButton(MoveType type, Button btn, Image bg, Image ring, string enabledSprite, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            var blocked = btn.GetComponent<BlockedMoveFeedback>() ?? btn.gameObject.AddComponent<BlockedMoveFeedback>();
            blocked.Move = type;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            var group = btn.GetComponent<CanvasGroup>() ?? btn.gameObject.AddComponent<CanvasGroup>();
            driveButtons[type] = new DriveButton
            {
                button = btn,
                background = bg != null ? bg : (Image)btn.targetGraphic,
                enabledSprite = enabledSprite,
                authoredSprite = (bg != null ? bg : (Image)btn.targetGraphic).sprite,
                group = group,
                ring = ring
            };
        }

        private void BuildHUD()
        {
            // ---- level card (top-left) ----
            var card = UIKit.Img("LevelCard", hudLayer, "panel_hud", null, true);
            levelCard = card.rectTransform;
            UIKit.Place(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), Vector2.zero);
            UIKit.Layout(card, true, 8, new RectOffset(26, 26, 20, 20), TextAnchor.MiddleCenter);
            UIKit.Size(card, 310, 154);
            UIKit.Fit(card);

            var cardBtn = card.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = card;
            cardBtn.onClick.AddListener(OpenLevelSelect);

            levelNumberText = UIKit.Bold(card.transform, "LEVEL 1", 52, UIKit.Ink, TextAnchor.MiddleCenter);
            levelNumberText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var subPill = UIKit.Img("SubtitlePill", card.transform, "pill_lavender");
            UIKit.Size(subPill, 240, 48);
            UIKit.Layout(subPill, false, 0, new RectOffset(16, 16, 2, 2), TextAnchor.MiddleCenter);
            levelTitleText = UIKit.Bold(subPill.transform, "First Gear", 28, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.FitLevelTitle(levelTitleText);

            // ---- top-right cluster ----
            topRight = UIKit.Rect("TopRight", hudLayer);
            UIKit.Place(topRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), Vector2.zero);
            UIKit.Layout(topRight, true, 14, null, TextAnchor.UpperRight);
            UIKit.Fit(topRight);

            // Row 1: Toolbar with Level Design & Pause side-by-side
            var tools = UIKit.Rect("Toolbar", topRight);
            toolbar = tools;
            UIKit.Layout(tools, false, 14, null, TextAnchor.MiddleRight);
            UIKit.Size(tools, 242, 114);
            ToolButton(tools, "icon_level_design", UIKit.White, OpenCreator, "btn_blue");
            ToolButton(tools, "icon_pause", UIKit.White, OpenPause, "btn_yellow");

            // Row 2: Fuel Card
            var fuel = UIKit.Img("FuelPill", topRight, "panel_hud", null, true);
            fuelPill = fuel.rectTransform;
            UIKit.Size(fuel, 242, 116);
            UIKit.Layout(fuel, false, 14, new RectOffset(18, 18, 14, 16), TextAnchor.MiddleCenter);
            UIKit.Icon(fuel.transform, "icon_fuel", UIKit.White, 72);
            var fcol = UIKit.Rect("Col", fuel.transform);
            UIKit.Layout(fcol, true, 2, null, TextAnchor.MiddleCenter);
            var fuelLabel = UIKit.Bold(fcol, "FUEL", 24, UIKit.Ink, TextAnchor.MiddleCenter);
            fuelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            var frow = UIKit.Rect("Row", fcol);
            UIKit.Layout(frow, false, 2, null, TextAnchor.MiddleCenter);
            fuelText = UIKit.Bold(frow, "8", 46, UIKit.Ink, TextAnchor.MiddleCenter);
            fuelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            fuelMaxText = UIKit.Bold(frow, "/8", 46, UIKit.Ink, TextAnchor.MiddleCenter);
            fuelMaxText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Row 3: Next Color Card
            BuildCarColorCard();

            // Hidden Target pill (reference has no target heading on HUD)
            var target = UIKit.Rect("TargetPill", hudLayer);
            targetPill = target;
            target.gameObject.SetActive(false);
            targetHeadingText = UIKit.Bold(target, "Facing NORTH", 18, UIKit.Emerald);
            targetArrow = UIKit.Img("TargetArrow", target, "icon_arrow_up");

            // ---- bottom controls ----
            bottomStack = UIKit.Rect("BottomStack", hudLayer);
            UIKit.Place(bottomStack, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 36), Vector2.zero);
            UIKit.Layout(bottomStack, true, 18, null, TextAnchor.LowerCenter);
            UIKit.Fit(bottomStack);

            // Hidden Zen button
            var zen = UIKit.Rect("ZenToggle", bottomStack);
            zen.gameObject.SetActive(false);
            zenIcon = UIKit.Img("ZenIcon", zen, "icon_chevron_down");
            zenLabel = UIKit.Bold(zen, "", 12, UIKit.TextMuted);

            // Row 1 of Dock: Utility buttons inside a white card
            var panel = UIKit.Img("DockUtilityCard", bottomStack, "panel_hud", null, true);
            dockPanel = panel.rectTransform;
            dockUtility = dockPanel;
            dockRoot = dockPanel;
            UIKit.Size(panel, 720, 96);
            UIKit.Layout(panel, false, 14, new RectOffset(20, 20, 10, 10), TextAnchor.MiddleCenter);

            undoButton = UIKit.IconTextButton("Undo", dockPanel, "white", "icon_undo", UIKit.White, "Undo", UIKit.Ink, 32, 74, OnClickUndo, 46, 16);
            undoButton.targetGraphic.color = Color.clear;
            undoGroup = undoButton.gameObject.AddComponent<CanvasGroup>();
            var badge = UIKit.Img("Count", undoButton.transform, "badge_count");
            undoBadge = badge.gameObject;
            undoBadge.SetActive(false);
            UIKit.Layout(badge, false, 0, new RectOffset(10, 10, 4, 4));
            undoCountText = UIKit.Caps(badge.transform, "0", 20, UIKit.Sky, TextAnchor.MiddleCenter);
            undoCountText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var rst = UIKit.IconTextButton("Restart", dockPanel, "white", "icon_restart", UIKit.White, "Restart", UIKit.Ink, 32, 74, OnClickRestart, 46, 16);
            rst.targetGraphic.color = Color.clear;

            var cam = UIKit.IconTextButton("Camera", dockPanel, "white", "icon_camera", UIKit.White, "Camera", UIKit.Ink, 32, 74, CycleCamera, 46, 16);
            cam.targetGraphic.color = Color.clear;
            camLabel = cam.GetComponentInChildren<Text>();

            foreach (Transform control in dockPanel) UIKit.Size(control, 218, 74);

            // Row 2 of Dock: 4 Standalone Candy Buttons (Left, Drive, Rev, Right)
            dockDrive = UIKit.Rect("DriveRow", bottomStack);
            UIKit.Layout(dockDrive, false, 18, null, TextAnchor.LowerCenter);
            UIKit.Fit(dockDrive);

            AddDriveButton(MoveType.TurnLeft, "btn_yellow", "icon_turn_left", UIKit.White, "Left", UIKit.Ink, 152, 152, false);
            AddDriveButton(MoveType.Forward, "btn_primary", "icon_arrow_up", UIKit.White, "Drive", UIKit.White, 192, 188, true);
            AddDriveButton(MoveType.Reverse, "btn_yellow", "icon_arrow_down", UIKit.White, "Rev", UIKit.Ink, 152, 152, false);
            AddDriveButton(MoveType.TurnRight, "btn_yellow", "icon_turn_right", UIKit.White, "Right", UIKit.Ink, 152, 152, false);
        }

        private Image ToolButton(Transform parent, string icon, Color color, UnityEngine.Events.UnityAction action, string btnSprite = null)
        {
            string sprite = btnSprite ?? (icon == "icon_gear" ? "btn_pink" : icon == "icon_pause" ? "btn_yellow" : "btn_blue");
            var b = UIKit.IconButton(icon, parent, sprite, icon, UIKit.White, 114, 66, action);
            b.targetGraphic.color = Color.white;
            return b.transform.Find("Icon").GetComponent<Image>();
        }

        private void CycleCarColor()
        {
            var visuals = carController != null ? carController.GetComponent<CarVisuals>() : FindFirstObjectByType<CarVisuals>();
            if (visuals == null || !visuals.CanChangeColor) return;
            visuals.CycleColor();
            RefreshCarColor();
        }

        private void BuildCarColorCard()
        {
            carColorButton = UIKit.Button("CarColor", topRight, "panel_hud", CycleCarColor);
            UIKit.Size(carColorButton, 242, 260);
            var root = (RectTransform)carColorButton.transform;
            var heading = UIKit.Bold(root, "NEXT COLOR", 22, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(heading.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(220, 26));

            carColorPreview = UIKit.Img("CarRender", root, "car_preview_Blue");
            carColorPreview.raycastTarget = false;
            UIKit.Place(carColorPreview.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-4, 18), new Vector2(176, 110));

            var next = UIKit.Bold(root, ">", 28, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(next.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-16, 18), new Vector2(24, 32));

            carColorLabel = UIKit.Bold(root, "Blue", 26, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(carColorLabel.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 56), new Vector2(180, 28));

            carColorSwatches = new Image[4];
            var colors = new[] { UIKit.C("#ffce2b"), UIKit.C("#2896f5"), UIKit.C("#38cf56"), UIKit.C("#f53856") };
            for (int i = 0; i < colors.Length; i++)
            {
                var swatch = UIKit.Img("Variant" + i, root, "circle", colors[i]);
                UIKit.Place(swatch.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2((i - 1.5f) * 36, 24), new Vector2(22, 22));
                carColorSwatches[i] = swatch;
            }

            swatchRing = UIKit.Img("SwatchRing", root, "swatch_ring", UIKit.White);
            swatchRing.raycastTarget = false;
            UIKit.Place(swatchRing.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, .5f), carColorSwatches[1].rectTransform.anchoredPosition, new Vector2(34, 34));
            swatchRing.transform.SetAsLastSibling();
        }

        private void RefreshCarColor()
        {
            var visuals = carController != null ? carController.GetComponent<CarVisuals>() : FindFirstObjectByType<CarVisuals>();
            if (carColorButton != null) carColorButton.interactable = visuals != null && visuals.CanChangeColor;
            string colorName = visuals != null ? visuals.NextColorName : "Blue";
            if (carColorLabel != null) carColorLabel.text = colorName;
            if (carColorPreview != null) UIKit.SetSprite(carColorPreview, "car_preview_" + colorName);
            string[] variants = { "Yellow", "Blue", "Green", "Red" };
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] == colorName && swatchRing != null && carColorSwatches != null && carColorSwatches[i] != null)
                {
                    swatchRing.transform.SetAsLastSibling();
                    swatchRing.rectTransform.anchoredPosition = carColorSwatches[i].rectTransform.anchoredPosition;
                }
            }
        }

        private void AddDriveButton(MoveType type, string sprite, string icon, Color iconColor, string label, Color labelColor, float w, float h, bool isDrive)
        {
            UnityEngine.Events.UnityAction action;
            switch (type)
            {
                case MoveType.Forward: action = OnClickDrive; break;
                case MoveType.Reverse: action = OnClickReverse; break;
                case MoveType.TurnLeft: action = OnClickTurnLeft; break;
                default: action = OnClickTurnRight; break;
            }
            var b = UIKit.Button(label, dockDrive, sprite, action);
            b.gameObject.AddComponent<BlockedMoveFeedback>().Move = type;
            UIKit.Layout(b, true, 4, new RectOffset(0, 0, isDrive ? 14 : 14, 16), TextAnchor.MiddleCenter);
            UIKit.Size(b, w, h);
            UIKit.Icon(b.transform, icon, iconColor, isDrive ? 78 : 68);
            var t = UIKit.Bold(b.transform, label, isDrive ? 42 : 34, labelColor, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (isDrive)
            {
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = UIKit.Ink;
                outline.effectDistance = new Vector2(2.5f, -2.5f);
            }
            var ring = UIKit.Img("Ring", b.transform, "btn_ring", new Color(1, 1, 1, 0));
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Stretch(ring.rectTransform, -7);
            driveButtons[type] = new DriveButton
            {
                button = b,
                background = (Image)b.targetGraphic,
                enabledSprite = sprite,
                group = b.gameObject.AddComponent<CanvasGroup>(),
                ring = ring
            };
        }

        private void LayoutHUD(bool portrait)
        {
            if (targetPill != null) targetPill.gameObject.SetActive(false);
            // Button sizes, parentage and ordering belong to the saved hierarchy.
        }

        public void UpdateFuelDisplay(int currentFuel)
        {
            var level = CurrentLevel;
            int maxFuel = level != null ? level.maxFuel : 8;
            fuelText.text = currentFuel.ToString();
            fuelText.color = currentFuel <= 2 ? UIKit.Rose : normalFuelColor;
            fuelMaxText.text = $"/{maxFuel}";
            var feedback = fuelText.GetComponent<FuelFeedback>() ?? fuelText.gameObject.AddComponent<FuelFeedback>();
            Transform icon = fuelPill != null && fuelPill.childCount > 0 ? fuelPill.GetChild(0) : null;
            feedback.SetFuel(currentFuel, icon);
        }

        private void RefreshHUD()
        {
            RefreshCarColor();
            if (carController == null) carController = FindFirstObjectByType<CarController>();
            if (carController == null || carController.IsBusy) return;

            var legal = new HashSet<MoveType>();
            foreach (var m in carController.CalculateLegalMoves()) legal.Add(m.moveType);
            foreach (var pair in driveButtons)
            {
                bool on = legal.Contains(pair.Key);
                pair.Value.button.interactable = on;
                if (on && pair.Value.authoredSprite != null)
                    pair.Value.background.sprite = pair.Value.authoredSprite;
                else UIKit.SetSprite(pair.Value.background, on ? pair.Value.enabledSprite : "btn_disabled");
                pair.Value.group.alpha = on ? 1f : .75f;
            }

            int undo = UndoManager.Instance != null ? UndoManager.Instance.UndoCount : 0;
            undoButton.interactable = undo > 0;
            undoGroup.alpha = undo > 0 ? 1f : .75f;
            undoBadge.SetActive(undo > 0);
            undoCountText.text = undo.ToString();
        }

        private void FlashDriveButton(MoveType type)
        {
            if (driveButtons.TryGetValue(type, out var d)) d.flash = 1f;
        }

        private void AnimateHUD()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var d in driveButtons.Values)
            {
                if (d.flash <= 0f) continue;
                d.flash = Mathf.Max(0f, d.flash - dt * 2.5f);
                d.ring.color = new Color(1, 1, 1, d.flash);
            }
            float available = hudLayer.rect.width - 28 * 2 - 24;
            float needed = levelCard.rect.width * authoredLevelScale.x + topRight.rect.width * authoredTopRightScale.x;
            float scale = needed > available && needed > 0 ? available / needed : 1f;
            levelCard.localScale = new Vector3(authoredLevelScale.x * scale, authoredLevelScale.y * scale, authoredLevelScale.z);
            topRight.localScale = new Vector3(authoredTopRightScale.x * scale, authoredTopRightScale.y * scale, authoredTopRightScale.z);
        }
    }
}
