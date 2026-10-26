using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>
    /// Constructs the visual hierarchy for the Park It! UI Canvas.
    /// Can be executed in the Unity Editor to bake prefabs/scenes,
    /// or at runtime as a zero-setup fallback.
    /// </summary>
    public static class ParkItUIBuilder
    {
        public static ParkItUIReferences BuildHierarchy(GameObject canvasObj)
        {
            if (canvasObj == null) canvasObj = new GameObject("ParkIt_Mobile_Canvas");

            var canvas = canvasObj.GetComponent<Canvas>() ?? canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObj.GetComponent<CanvasScaler>() ?? canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referenceResolution = new Vector2(1080, 1920);

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                canvasObj.AddComponent<GraphicRaycaster>();

            for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvasObj.transform.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var refs = canvasObj.GetComponent<ParkItUIReferences>() ?? canvasObj.AddComponent<ParkItUIReferences>();
            refs.canvas = canvas;
            refs.scaler = scaler;

            var safe = UIKit.Stretch(UIKit.Rect("SafeArea", canvasObj.transform));
            if (safe.GetComponent<PortraitSafeArea>() == null) safe.gameObject.AddComponent<PortraitSafeArea>();
            refs.safeArea = safe;

            var uiRoot = UIKit.Stretch(UIKit.Rect("ParkIt_UI", safe));
            refs.uiRoot = uiRoot;

            var hudLayer = UIKit.Stretch(UIKit.Rect("HUD", uiRoot));
            refs.hudLayer = hudLayer;

            var screenLayer = UIKit.Stretch(UIKit.Rect("Screens", uiRoot));
            refs.screenLayer = screenLayer;

            var modalLayer = UIKit.Stretch(UIKit.Rect("Modals", uiRoot));
            refs.modalLayer = modalLayer;

            var toastLayer = UIKit.Stretch(UIKit.Rect("Toasts", uiRoot));
            refs.toastLayer = toastLayer;

            BuildHUD(hudLayer, refs);
            BuildTitle(screenLayer, refs);
            StyleTitleScreen(refs);
            StyleTitleBranding(refs);
            BuildLevelSelect(modalLayer, refs);
            BuildHelp(modalLayer, refs);
            BuildPause(modalLayer, refs);
            StylePauseAndHelp(refs);
            BuildResults(modalLayer, refs);
            BuildToast(toastLayer, refs);
            EnsureHintButton(refs);
            EnsureShiftControls(refs);

            var creator = LevelCreatorUI.Create(modalLayer, null);
            refs.levelCreator = creator;
            StyleRemainingPopups(refs);

            return refs;
        }

        // Also upgrades an existing baked card without rebuilding the canvas.
        public static void EnsureLevelCardDetails(ParkItUIReferences refs)
        {
            if (refs.levelCard == null) return;
            var card = refs.levelCard;
            var layout = card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            UIKit.Size(card, 372, 154);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 372);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 154);

            UIKit.Place(refs.levelNumberText.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(26, -18), new Vector2(170, 58));
            refs.levelNumberText.fontSize = 46;
            refs.levelNumberText.alignment = TextAnchor.MiddleLeft;
            refs.levelNumberText.resizeTextForBestFit = true;
            refs.levelNumberText.resizeTextMinSize = 28;
            refs.levelNumberText.resizeTextMaxSize = 46;
            UIKit.Place((RectTransform)refs.levelTitleText.transform.parent, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 18), new Vector2(240, 48));

            if (refs.levelBadge == null)
                refs.levelBadge = card.Find("DifficultyBadge")?.GetComponent<UnityEngine.UI.Image>();
            if (refs.levelBadge == null)
                refs.levelBadge = UIKit.Badge(card, LevelDifficulty.Beginner, 20);
            refs.levelBadge.name = "DifficultyBadge";
            refs.levelBadge.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().padding = new RectOffset(12, 12, 6, 6);
            UIKit.Place(refs.levelBadge.rectTransform, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -28), new Vector2(144, 38));

            if (refs.levelHelpButton == null)
                refs.levelHelpButton = card.Find("LevelHelp")?.GetComponent<UnityEngine.UI.Button>();
            if (refs.levelHelpButton == null)
                refs.levelHelpButton = UIKit.IconButton("LevelHelp", card, "dock_yellow", "icon_help",
                    UIKit.Ink, 64, 42, null);
            var helpIcon = refs.levelHelpButton.transform.Find("Icon") as RectTransform;
            if (helpIcon != null)
                UIKit.Place(helpIcon, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 2), new Vector2(42, 42));
            UIKit.Place((RectTransform)refs.levelHelpButton.transform, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-24, 14), new Vector2(64, 64));
        }

        public static void StyleTitleScreen(ParkItUIReferences refs)
        {
            if (refs.titleRoot == null) return;
            var backdrop = refs.titleRoot.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>();
            ApplyGloss(backdrop, "candy_background");
            backdrop.type = UnityEngine.UI.Image.Type.Simple;
            backdrop.preserveAspect = false;
            var panel = refs.titleColumn.GetComponent<UnityEngine.UI.Image>()
                ?? refs.titleColumn.gameObject.AddComponent<UnityEngine.UI.Image>();
            ApplyGloss(panel, "dock_panel");
            panel.raycastTarget = true;
            var layout = refs.titleColumn.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 56);
            layout.spacing = 24;
            foreach (Transform child in refs.titleColumn)
            {
                var label = child.GetComponent<UnityEngine.UI.Text>();
                if (label != null) UIKit.Size(label, 900, -1);
            }
            StylePopupButton(refs.titlePlayBtn, "dock_drive", Color.white);
            StylePopupButton(refs.titleLevelSelectBtn, "dock_yellow", UIKit.Ink);
            refs.titleFooter.color = UIKit.Ink;
        }

        // Explicit prefab authoring; runtime binding preserves this arrangement.
        public static void StyleTitleBranding(ParkItUIReferences refs)
        {
            var column = refs.titleColumn;
            var background = column.GetComponent<UnityEngine.UI.Image>();
            if (background != null) background.enabled = false;
            var layout = column.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 28;
            var underline = column.Find("Underline");
            if (underline != null) underline.gameObject.SetActive(false);

            var card = (column.Find("LogoCard") ?? column.Find("LogoMotionSlot/LogoCard")) as RectTransform;
            if (card == null) card = UIKit.Img("LogoCard", column, "panel_hud", Color.white, true).rectTransform;
            card.SetAsFirstSibling();
            UIKit.Size(card, 840, 286);
            UIKit.Layout(card, true, 0, new RectOffset(40, 40, 32, 36), TextAnchor.MiddleCenter);
            var logo = column.Find("Logo") ?? card.Find("Logo");
            logo.SetParent(card, false);
            logo.SetAsFirstSibling();
            UIKit.Size(logo, 720, 160);
            logo.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
            var tagline = card.Find("Tagline")?.GetComponent<UnityEngine.UI.Text>();
            if (tagline == null)
            {
                tagline = UIKit.Label(card, "CANDY ARCADE EDITION", 32, UIKit.C("#8038eb"), UIKit.Sans, FontStyle.Normal, TextAnchor.MiddleCenter);
                tagline.name = "Tagline";
            }
            UIKit.Size(tagline, 740, 44);

            var description = column.Find("DescriptionPanel") as RectTransform;
            if (description == null) description = UIKit.Img("DescriptionPanel", column, "panel_section", Color.white, true).rectTransform;
            description.SetSiblingIndex(1);
            UIKit.Size(description, 900, 160, 0);
            UIKit.Layout(description, true, 0, new RectOffset(40, 40, 24, 24), TextAnchor.MiddleCenter, true);
            var text = column.Find("Text")?.GetComponent<UnityEngine.UI.Text>() ?? description.GetComponentInChildren<UnityEngine.UI.Text>();
            text.transform.SetParent(description, false);
            text.text = "Steer the toy car into the target parking bay before\nfuel runs out!";
            text.fontSize = 32;
            text.color = UIKit.TextSoft;
            text.alignment = TextAnchor.MiddleCenter;
            text.lineSpacing = 1.2f;
            UIKit.Size(text, -1, -1, 1);
            AddTitleLogoMotion(refs);
            AddTitlePlayPulse(refs);
        }

        public static void AddTitlePlayPulse(ParkItUIReferences refs)
        {
            if (refs.titlePlayBtn != null && refs.titlePlayBtn.GetComponent<UIPlayPulse>() == null)
                refs.titlePlayBtn.gameObject.AddComponent<UIPlayPulse>();
        }

        public static void AddTitleLogoMotion(ParkItUIReferences refs)
        {
            var column = refs.titleColumn;
            var slot = column.Find("LogoMotionSlot") as RectTransform;
            if (slot != null) return;
            var card = column.Find("LogoCard") as RectTransform;
            if (card == null) return;
            int index = card.GetSiblingIndex();
            slot = UIKit.Rect("LogoMotionSlot", column);
            slot.SetSiblingIndex(index);
            var size = card.GetComponent<UnityEngine.UI.LayoutElement>();
            UIKit.Size(slot, size.preferredWidth, size.preferredHeight, 0, 0);
            card.SetParent(slot, false);
            UIKit.Place(card, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(size.preferredWidth, size.preferredHeight));
            card.gameObject.AddComponent<UILogoFloat>();
        }

        public static void StyleRemainingPopups(ParkItUIReferences refs)
        {
            StyleGlossyWindow(refs.levelSelectRoot);
            StyleGlossyWindow(refs.victoryRoot);
            StyleGlossyWindow(refs.defeatRoot);
            if (refs.levelCreator != null) StyleGlossyWindow(refs.levelCreator.gameObject);
        }

        public static void StyleGlossyWindow(GameObject root)
        {
            if (root == null) return;
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                string original = image.sprite != null ? image.sprite.name : "";
                string replacement = null;
                switch (original)
                {
                    case "panel_modal": replacement = "dock_panel"; break;
                    case "panel_inset": case "panel_section": case "grid_frame":
                    case "pill_lavender": case "card_level": case "card_level_hover":
                    case "btn_secondary": case "btn_dark": replacement = "dock_utility"; break;
                    case "card_level_selected": case "btn_brush_active":
                    case "btn_yellow": case "tile_amber": replacement = "dock_yellow"; break;
                    case "btn_blue": case "tile_green": replacement = "dock_cyan"; break;
                    case "btn_green": case "btn_primary": case "btn_pink":
                    case "btn_close": case "tile_red": replacement = "dock_drive"; break;
                    case "white_rounded":
                        if (image.GetComponent<UnityEngine.UI.Button>() != null)
                            replacement = image.name == "Close" ? "dock_drive" : "dock_lavender";
                        break;
                }
                if (replacement == null) continue;
                ApplyGloss(image, replacement);
                var button = image.GetComponent<UnityEngine.UI.Button>();
                if (button == null) continue;
                var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label != null && !button.name.StartsWith("Level_"))
                    label.color = replacement == "dock_drive" ? Color.white : UIKit.Ink;
            }
        }

        public static void StylePauseAndHelp(ParkItUIReferences refs)
        {
            var pausePanel = (RectTransform)refs.pauseRoot.transform.Find("Panel");
            if (refs.pauseMainMenuBtn == null)
                refs.pauseMainMenuBtn = pausePanel.Find("MainMenu")?.GetComponent<UnityEngine.UI.Button>();
            if (refs.pauseMainMenuBtn == null)
                refs.pauseMainMenuBtn = UIKit.IconTextButton("MainMenu", pausePanel, "dock_lavender", "icon_home",
                    UIKit.Ink, "MAIN MENU", UIKit.Ink, 28, 96, null, 36);
            refs.pauseMainMenuBtn.transform.SetSiblingIndex(refs.pauseLevelSelectBtn.transform.GetSiblingIndex() + 1);
            pausePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 834);
            StylePopupButton(refs.pauseMainMenuBtn, "dock_lavender", UIKit.Ink);
            foreach (var root in new[] { refs.pauseRoot, refs.helpRoot })
            {
                var panel = root.transform.Find("Panel").GetComponent<UnityEngine.UI.Image>();
                ApplyGloss(panel, "dock_panel");
            }
            ApplyGloss(refs.pauseLevelText.transform.parent.GetComponent<UnityEngine.UI.Image>(), "dock_utility");
            StylePopupButton(refs.pauseResumeBtn, "dock_drive", Color.white);
            StylePopupButton(refs.pauseRestartBtn, "dock_yellow", UIKit.Ink);
            StylePopupButton(refs.pauseLevelSelectBtn, "dock_cyan", UIKit.Ink);
            StylePopupButton(refs.pauseSoundBtn, "dock_utility", UIKit.Ink);
            StylePopupButton(refs.helpGotItBtn, "dock_drive", Color.white);
            ApplyGloss(refs.pauseCloseBtn.GetComponent<UnityEngine.UI.Image>(), "dock_drive");
            ApplyGloss(refs.helpCloseBtn.GetComponent<UnityEngine.UI.Image>(), "dock_drive");
            foreach (var icon in refs.helpRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                string replacement = null;
                switch (icon.name)
                {
                    case "icon_lightbulb": replacement = "icon_help"; break;
                    case "icon_flag": replacement = "icon_target"; break;
                    case "icon_pointer": replacement = "icon_phone"; break;
                    case "icon_steering": replacement = "icon_car"; break;
                }
                if (replacement != null)
                {
                    UIKit.SetSprite(icon, replacement);
                    icon.color = UIKit.Ink;
                }
            }
        }

        private static void StylePopupButton(UnityEngine.UI.Button button, string sprite, Color textColor)
        {
            ApplyGloss(button.GetComponent<UnityEngine.UI.Image>(), sprite);
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            label.color = textColor;
            var layout = button.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.padding.top = 2;
            layout.padding.bottom = 10;
            if (textColor == Color.white)
            {
                var outline = label.GetComponent<UnityEngine.UI.Outline>() ?? label.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = UIKit.Ink;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        public static void StyleTopControls(ParkItUIReferences refs)
        {
            if (refs.levelHelpButton == null) EnsureLevelCardDetails(refs);
            ResolveBindings(refs);
            ApplyGloss(refs.pauseButton.GetComponent<UnityEngine.UI.Image>(), "dock_yellow");
            ApplyGloss(refs.levelDesignButton.GetComponent<UnityEngine.UI.Image>(), "dock_cyan");
            ApplyGloss(refs.levelCard.GetComponent<UnityEngine.UI.Image>(), "dock_panel");
            ApplyGloss(refs.fuelPill.GetComponent<UnityEngine.UI.Image>(), "dock_panel");
            ApplyGloss(refs.carColorButton.GetComponent<UnityEngine.UI.Image>(), "dock_panel");
            ApplyGloss(refs.levelHelpButton.GetComponent<UnityEngine.UI.Image>(), "dock_yellow");
            ApplyGloss(refs.levelTitleText.transform.parent.GetComponent<UnityEngine.UI.Image>(), "dock_restart");
        }

        // Runtime binding must not reapply generation defaults to authored UI.
        public static void ResolveBindings(ParkItUIReferences refs)
        {
            if (refs.levelBadge == null)
                refs.levelBadge = refs.levelCard.Find("DifficultyBadge")?.GetComponent<UnityEngine.UI.Image>();
            if (refs.levelHelpButton == null)
                refs.levelHelpButton = refs.levelCard.Find("LevelHelp")?.GetComponent<UnityEngine.UI.Button>();
            if (refs.pauseMainMenuBtn == null && refs.pauseRoot != null)
                refs.pauseMainMenuBtn = refs.pauseRoot.transform.Find("Panel/MainMenu")?.GetComponent<UnityEngine.UI.Button>();
            // Some baked scenes place a separate toolbar at the top-left.
            // Bind the visible controls instead of the hidden original toolbar.
            var visibleToolbar = refs.hudLayer.Find("TopLeft/Toolbar");
            if (visibleToolbar != null && visibleToolbar.gameObject.activeSelf && visibleToolbar.parent.gameObject.activeSelf)
            {
                var pause = visibleToolbar.Find("icon_pause")?.GetComponent<UnityEngine.UI.Button>();
                var design = visibleToolbar.Find("icon_level_design")?.GetComponent<UnityEngine.UI.Button>();
                if (pause != null && design != null)
                {
                    refs.toolbar = (RectTransform)visibleToolbar;
                    refs.pauseButton = pause;
                    refs.levelDesignButton = design;
                }
            }
        }

        public static void EnsureHintButton(ParkItUIReferences refs)
        {
            if (refs.pauseButton == null) return;
            if (refs.hintButton == null)
                refs.hintButton = refs.pauseButton.transform.Find("HintButton")?.GetComponent<UnityEngine.UI.Button>();
            if (refs.hintButton == null)
            {
                refs.hintButton = UIKit.Button("HintButton", refs.pauseButton.transform, "dock_panel", null);
                var rect = (RectTransform)refs.hintButton.transform;
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = new Vector2(0, 114);
                rect.anchoredPosition = new Vector2(0, -18);
            }
            // Existing Hint parent, dimensions and background are artist-authored.
            if (refs.hintIcon == null)
            {
                var icon = UIKit.Img("HintIcon", refs.hintButton.transform, "icon_hint_bulb", Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                UIKit.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    Vector2.zero, new Vector2(78, 78));
                refs.hintIcon = icon;
            }
            var oldLabel = refs.hintButton.transform.Find("Text");
            if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            if (refs.hintPanel != null) refs.hintPanel.SetActive(false);
            if (refs.hintMoveButton != null) refs.hintMoveButton.gameObject.SetActive(false);
        }

        public static void EnsureShiftControls(ParkItUIReferences refs)
        {
            if (refs.dockDrive == null) return;
            if (refs.shiftLeftBtn == null)
            {
                CreateDriveBtn(refs.dockDrive, "Shift L", "dock_cyan", "icon_shift_left", UIKit.Ink,
                    128, 152, false, out refs.shiftLeftBtn, out refs.shiftLeftBg, out refs.shiftLeftRing);
                ((RectTransform)refs.shiftLeftBtn.transform).sizeDelta = new Vector2(128, 152);
                refs.shiftLeftBtn.name = "ShiftLeft";
                refs.shiftLeftBtn.transform.SetAsFirstSibling();
                refs.shiftLeftBtn.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 26;
            }
            if (refs.shiftRightBtn == null)
            {
                CreateDriveBtn(refs.dockDrive, "Shift R", "dock_cyan", "icon_shift_right", UIKit.Ink,
                    128, 152, false, out refs.shiftRightBtn, out refs.shiftRightBg, out refs.shiftRightRing);
                ((RectTransform)refs.shiftRightBtn.transform).sizeDelta = new Vector2(128, 152);
                refs.shiftRightBtn.name = "ShiftRight";
                refs.shiftRightBtn.transform.SetAsLastSibling();
                refs.shiftRightBtn.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 26;
            }
            refs.shiftLeftBtn.interactable = false;
            refs.shiftRightBtn.interactable = false;
            refs.shiftLeftBtn.gameObject.SetActive(false);
            refs.shiftRightBtn.gameObject.SetActive(false);
        }
        private static void ApplyGloss(UnityEngine.UI.Image image, string sprite)
        {
            UIKit.SetSprite(image, sprite);
            image.color = Color.white;
        }

        public static void StyleBottomControls(ParkItUIReferences refs)
        {
            if (refs.dockPanel == null) return;
            var panel = refs.dockPanel.GetComponent<UnityEngine.UI.Image>();
            panel.color = Color.clear;
            panel.raycastTarget = false;
            UIKit.Size(refs.dockPanel, 702, 90);
            var row = refs.dockPanel.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            row.padding = new RectOffset();
            row.spacing = 12;
            refs.bottomStack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 8;
            foreach (var b in new[] { refs.undoButton, refs.restartButton, refs.camButton })
            {
                UIKit.SetSprite((UnityEngine.UI.Image)b.targetGraphic, b == refs.restartButton ? "dock_restart" : "dock_utility");
                b.targetGraphic.color = Color.white;
                UIKit.Size(b, 226, 90);
                var layout = b.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.spacing = 10;
                layout.padding = new RectOffset(12, 12, 4, 10);
                var label = b.GetComponentInChildren<UnityEngine.UI.Text>();
                label.fontSize = 28;
            }
            // Keep the undo counter out of the icon/label layout.
            var countLayout = refs.undoBadge.GetComponent<UnityEngine.UI.LayoutElement>() ?? refs.undoBadge.AddComponent<UnityEngine.UI.LayoutElement>();
            countLayout.ignoreLayout = true;
            UIKit.Place((RectTransform)refs.undoBadge.transform, Vector2.one, Vector2.one, new Vector2(-4, -2), new Vector2(34, 28));
            refs.undoCountText.fontSize = 16;
            foreach (var b in new[] { refs.turnLeftBtn, refs.forwardBtn, refs.reverseBtn, refs.turnRightBtn })
            {
                bool drive = b == refs.forwardBtn;
                UIKit.SetSprite((UnityEngine.UI.Image)b.targetGraphic, drive ? "dock_drive" : "dock_yellow");
                b.targetGraphic.color = Color.white;
                var layout = b.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.padding = new RectOffset(8, 8, 12, 20);
                layout.spacing = 2;
                var label = b.GetComponentInChildren<UnityEngine.UI.Text>();
                label.fontSize = drive ? 40 : 32;
            }
        }

        private static void BuildHUD(RectTransform hudLayer, ParkItUIReferences refs)
        {
            // ---- level card (top-left) ----
            var card = UIKit.Img("LevelCard", hudLayer, "panel_hud", null, true);
            refs.levelCard = card.rectTransform;
            UIKit.Place(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), Vector2.zero);
            UIKit.Layout(card, true, 8, new RectOffset(26, 26, 20, 20), TextAnchor.MiddleCenter);
            UIKit.Size(card, 310, 154);
            UIKit.Fit(card);

            refs.levelCardButton = card.gameObject.AddComponent<Button>();
            refs.levelCardButton.targetGraphic = card;

            refs.levelNumberText = UIKit.Bold(card.transform, "LEVEL 1", 52, UIKit.Ink, TextAnchor.MiddleCenter);
            refs.levelNumberText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var subPill = UIKit.Img("SubtitlePill", card.transform, "dock_restart");
            UIKit.Size(subPill, 240, 48);
            UIKit.Layout(subPill, false, 0, new RectOffset(16, 16, 2, 2), TextAnchor.MiddleCenter);
            refs.levelTitleText = UIKit.Bold(subPill.transform, "First Gear", 28, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.FitLevelTitle(refs.levelTitleText);
            EnsureLevelCardDetails(refs);

            // ---- top-right cluster ----
            var topRight = UIKit.Rect("TopRight", hudLayer);
            refs.topRight = topRight;
            UIKit.Place(topRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), Vector2.zero);
            UIKit.Layout(topRight, true, 14, null, TextAnchor.UpperRight);
            UIKit.Fit(topRight);

            // Row 1: Toolbar
            var tools = UIKit.Rect("Toolbar", topRight);
            refs.toolbar = tools;
            UIKit.Layout(tools, false, 14, null, TextAnchor.MiddleRight);
            UIKit.Size(tools, 242, 114);
            refs.levelDesignButton = CreateToolBtn(tools, "btn_blue", "icon_level_design");
            refs.pauseButton = CreateToolBtn(tools, "btn_yellow", "icon_pause");

            // Row 2: Fuel Card
            var fuel = UIKit.Img("FuelPill", topRight, "panel_hud", null, true);
            refs.fuelPill = fuel.rectTransform;
            UIKit.Size(fuel, 242, 116);
            UIKit.Layout(fuel, false, 14, new RectOffset(18, 18, 14, 16), TextAnchor.MiddleCenter);
            UIKit.Icon(fuel.transform, "icon_fuel", UIKit.White, 72);
            var fcol = UIKit.Rect("Col", fuel.transform);
            UIKit.Layout(fcol, true, 2, null, TextAnchor.MiddleCenter);
            var fuelLabel = UIKit.Bold(fcol, "FUEL", 24, UIKit.Ink, TextAnchor.MiddleCenter);
            fuelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            var frow = UIKit.Rect("Row", fcol);
            UIKit.Layout(frow, false, 2, null, TextAnchor.MiddleCenter);
            refs.fuelText = UIKit.Bold(frow, "8", 46, UIKit.Ink, TextAnchor.MiddleCenter);
            refs.fuelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            refs.fuelMaxText = UIKit.Bold(frow, "/8", 46, UIKit.Ink, TextAnchor.MiddleCenter);
            refs.fuelMaxText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Row 3: Next Color Card
            BuildCarColorCard(topRight, refs);

            // Hidden Target pill
            var target = UIKit.Rect("TargetPill", hudLayer);
            refs.targetPill = target;
            target.gameObject.SetActive(false);
            refs.targetHeadingText = UIKit.Bold(target, "Facing NORTH", 18, UIKit.Emerald);
            refs.targetArrow = UIKit.Img("TargetArrow", target, "icon_arrow_up");

            // ---- bottom controls ----
            var bottomStack = UIKit.Rect("BottomStack", hudLayer);
            refs.bottomStack = bottomStack;
            UIKit.Place(bottomStack, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 36), Vector2.zero);
            UIKit.Layout(bottomStack, true, 18, null, TextAnchor.LowerCenter);
            UIKit.Fit(bottomStack);

            // Hidden Zen button
            var zen = UIKit.Rect("ZenToggle", bottomStack);
            zen.gameObject.SetActive(false);
            refs.zenIcon = UIKit.Img("ZenIcon", zen, "icon_chevron_down");
            refs.zenLabel = UIKit.Bold(zen, "", 12, UIKit.TextMuted);

            // Row 1 of Dock: Utility buttons inside a white card
            var panel = UIKit.Img("DockUtilityCard", bottomStack, "panel_hud", null, true);
            refs.dockPanel = panel.rectTransform;
            refs.dockUtility = refs.dockPanel;
            UIKit.Size(panel, 720, 96);
            UIKit.Layout(panel, false, 14, new RectOffset(20, 20, 10, 10), TextAnchor.MiddleCenter);

            refs.undoButton = UIKit.IconTextButton("Undo", refs.dockPanel, "white", "icon_undo", UIKit.White, "Undo", UIKit.Ink, 32, 74, null, 46, 16);
            refs.undoButton.targetGraphic.color = Color.clear;
            refs.undoGroup = refs.undoButton.gameObject.AddComponent<CanvasGroup>();
            var badge = UIKit.Img("Count", refs.undoButton.transform, "badge_count");
            refs.undoBadge = badge.gameObject;
            refs.undoBadge.SetActive(false);
            UIKit.Layout(badge, false, 0, new RectOffset(10, 10, 4, 4));
            refs.undoCountText = UIKit.Caps(badge.transform, "0", 20, UIKit.Sky, TextAnchor.MiddleCenter);
            refs.undoCountText.horizontalOverflow = HorizontalWrapMode.Overflow;

            refs.restartButton = UIKit.IconTextButton("Restart", refs.dockPanel, "white", "icon_restart", UIKit.White, "Restart", UIKit.Ink, 32, 74, null, 46, 16);
            refs.restartButton.targetGraphic.color = Color.clear;

            refs.camButton = UIKit.IconTextButton("Camera", refs.dockPanel, "white", "icon_camera", UIKit.White, "Camera", UIKit.Ink, 32, 74, null, 46, 16);
            refs.camButton.targetGraphic.color = Color.clear;
            refs.camLabel = refs.camButton.GetComponentInChildren<Text>();

            foreach (Transform control in refs.dockPanel) UIKit.Size(control, 218, 74);

            // Row 2 of Dock: 4 Standalone Candy Buttons (Left, Drive, Rev, Right)
            var dockDrive = UIKit.Rect("DriveRow", bottomStack);
            refs.dockDrive = dockDrive;
            UIKit.Layout(dockDrive, false, 18, null, TextAnchor.LowerCenter);
            UIKit.Fit(dockDrive);

            CreateDriveBtn(dockDrive, "Left", "btn_yellow", "icon_turn_left", UIKit.Ink, 152, 152, false, out refs.turnLeftBtn, out refs.turnLeftBg, out refs.turnLeftRing);
            CreateDriveBtn(dockDrive, "Drive", "btn_primary", "icon_arrow_up", UIKit.White, 192, 188, true, out refs.forwardBtn, out refs.forwardBg, out refs.forwardRing);
            CreateDriveBtn(dockDrive, "Rev", "btn_yellow", "icon_arrow_down", UIKit.Ink, 152, 152, false, out refs.reverseBtn, out refs.reverseBg, out refs.reverseRing);
            CreateDriveBtn(dockDrive, "Right", "btn_yellow", "icon_turn_right", UIKit.Ink, 152, 152, false, out refs.turnRightBtn, out refs.turnRightBg, out refs.turnRightRing);
            StyleBottomControls(refs);
            StyleTopControls(refs);
        }

        private static Button CreateToolBtn(Transform parent, string sprite, string icon)
        {
            var b = UIKit.IconButton(icon, parent, sprite, icon, UIKit.White, 114, 66, null);
            b.targetGraphic.color = Color.white;
            return b;
        }

        private static void CreateDriveBtn(Transform parent, string label, string sprite, string icon, Color labelColor, float w, float h, bool isDrive, out Button btn, out Image bg, out Image ring)
        {
            btn = UIKit.Button(label, parent, sprite, null);
            bg = (Image)btn.targetGraphic;
            UIKit.Layout(btn, true, 4, new RectOffset(0, 0, 14, 16), TextAnchor.MiddleCenter);
            UIKit.Size(btn, w, h);
            UIKit.Icon(btn.transform, icon, UIKit.White, isDrive ? 78 : 68);
            var t = UIKit.Bold(btn.transform, label, isDrive ? 42 : 34, labelColor, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (isDrive)
            {
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = UIKit.Ink;
                outline.effectDistance = new Vector2(2.5f, -2.5f);
            }
            ring = UIKit.Img("Ring", btn.transform, "btn_ring", new Color(1, 1, 1, 0));
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Stretch(ring.rectTransform, -7);
            if (btn.GetComponent<CanvasGroup>() == null) btn.gameObject.AddComponent<CanvasGroup>();
        }

        private static void BuildCarColorCard(Transform parent, ParkItUIReferences refs)
        {
            refs.carColorButton = UIKit.Button("CarColor", parent, "panel_hud", null);
            UIKit.Size(refs.carColorButton, 242, 260);
            var root = (RectTransform)refs.carColorButton.transform;
            var heading = UIKit.Bold(root, "NEXT COLOR", 22, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(heading.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(220, 26));

            refs.carColorPreview = UIKit.Img("CarRender", root, "car_preview_Blue");
            refs.carColorPreview.raycastTarget = false;
            UIKit.Place(refs.carColorPreview.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-4, 18), new Vector2(176, 110));

            var next = UIKit.Bold(root, ">", 28, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(next.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-16, 18), new Vector2(24, 32));

            refs.carColorLabel = UIKit.Bold(root, "Blue", 26, UIKit.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(refs.carColorLabel.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 56), new Vector2(180, 28));

            refs.carColorSwatches = new Image[4];
            var colors = new[] { UIKit.C("#ffce2b"), UIKit.C("#2896f5"), UIKit.C("#38cf56"), UIKit.C("#f53856") };
            for (int i = 0; i < colors.Length; i++)
            {
                var swatch = UIKit.Img("Variant" + i, root, "circle", colors[i]);
                UIKit.Place(swatch.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2((i - 1.5f) * 36, 24), new Vector2(22, 22));
                refs.carColorSwatches[i] = swatch;
            }

            refs.swatchRing = UIKit.Img("SwatchRing", root, "swatch_ring", UIKit.White);
            refs.swatchRing.raycastTarget = false;
            UIKit.Place(refs.swatchRing.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, .5f), refs.carColorSwatches[1].rectTransform.anchoredPosition, new Vector2(34, 34));
            refs.swatchRing.transform.SetAsLastSibling();
        }

        private static void BuildTitle(RectTransform screenLayer, ParkItUIReferences refs)
        {
            var root = UIKit.Rect("TitleScreen", screenLayer);
            UIKit.Stretch(root);
            refs.titleRoot = root.gameObject;
            UIKit.Dimmer(root).color = new Color(.72f, .43f, .91f, .82f);
            var vignette = UIKit.Img("Vignette", root, "vignette", new Color(.8f, .55f, 1f, .18f));
            vignette.preserveAspect = false;
            UIKit.Stretch(vignette.rectTransform);

            var col = UIKit.Rect("Column", root);
            refs.titleColumn = col;
            UIKit.Place(col, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(1100, 0));
            UIKit.Layout(col, true, 30, null, TextAnchor.MiddleCenter);
            UIKit.Fit(col, false, true);

            var logo = UIKit.Img("Logo", col, "logo_parkit");
            UIKit.Size(logo, 760, 169);
            var underline = UIKit.Img("Underline", col, "underline");
            UIKit.Size(underline, 300, 10);

            var blurb = UIKit.Label(col,
                "Navigate the chunky diorama vehicle across the puzzle grid.\nMatch the parking bay's exact slot and facing orientation\nbefore your fuel runs dry.",
                30, UIKit.TextSoft, UIKit.Sans, FontStyle.Normal, TextAnchor.MiddleCenter);
            blurb.lineSpacing = 1.25f;
            UIKit.Size(blurb, 1000, -1);

            UIKit.Size(UIKit.Rect("Gap", col), 10, 16);

            var btns = UIKit.Rect("Buttons", col);
            refs.titleButtons = btns;
            UIKit.Layout(btns, false, 28);

            var startWrap = UIKit.Rect("StartWrap", btns);
            UIKit.Layout(startWrap, false, 0);
            var glow = UIKit.Img("Glow", startWrap, "glow", new Color(1f, .62f, .04f, .4f));
            glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Stretch(glow.rectTransform, -34);
            refs.titlePlayBtn = UIKit.IconTextButton("StartEngine", startWrap, "btn_primary", "icon_play", UIKit.White, "Play", UIKit.White, 28, 104, null, 30, 44);
            refs.titleLevelSelectBtn = UIKit.IconTextButton("SelectLevel", btns, "btn_yellow", "icon_trophy", UIKit.Amber, "Select Level", UIKit.TextMain, 28, 104, null, 34, 40);

            refs.titleFooter = UIKit.Caps(root, "", 21, UIKit.TextDim, TextAnchor.MiddleCenter);
            refs.titleFooter.gameObject.SetActive(false);
            UIKit.Place(refs.titleFooter.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(1000, 80));
        }

        private static void BuildLevelSelect(RectTransform modalLayer, ParkItUIReferences refs)
        {
            var root = UIKit.Rect("LevelSelect", modalLayer);
            UIKit.Stretch(root);
            refs.levelSelectRoot = root.gameObject;
            UIKit.Dimmer(root);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            refs.levelSelectPanel = panel.rectTransform;
            UIKit.Place(refs.levelSelectPanel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(980, 1520));
            UIKit.Layout(panel, true, 0, new RectOffset(0, 0, 0, 0), TextAnchor.UpperCenter, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 16, new RectOffset(40, 28, 8, 0), TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 110);
            UIKit.Icon(header, "icon_trophy", UIKit.Amber, 46);
            UIKit.Bold(header, "Select Level", 42, UIKit.Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", header), -1, -1, 1);
            refs.levelSelectCloseBtn = UIKit.IconButton("Close", header, "btn_close", "icon_close", Color.white, 70, 38, null);
            refs.levelSelectCloseBtn.targetGraphic.color = Color.white;

            var scroll = ParkItUIManager.BuildScroll(panel.transform, out refs.levelGridContent);
            UIKit.Size(scroll, -1, -1, 1, 1);
            refs.levelGrid = refs.levelGridContent.gameObject.AddComponent<GridLayoutGroup>();
            refs.levelGrid.padding = new RectOffset(36, 36, 20, 36);
            refs.levelGrid.spacing = new Vector2(24, 24);
            refs.levelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            refs.levelGrid.constraintCount = 1;
            refs.levelGrid.cellSize = new Vector2(866, 300);
            UIKit.Fit(refs.levelGridContent, false, true);

            refs.levelSelectRoot.SetActive(false);
        }

        private static void BuildHelp(RectTransform modalLayer, ParkItUIReferences refs)
        {
            var root = UIKit.Rect("Help", modalLayer);
            UIKit.Stretch(root);
            refs.helpRoot = root.gameObject;
            UIKit.Dimmer(root);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(940, 0));
            UIKit.Layout(panel, true, 22, new RectOffset(56, 56, 48, 52), TextAnchor.UpperLeft, true);
            UIKit.Fit(panel, false, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 16, null, TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 64);
            UIKit.Icon(header, "icon_lightbulb", UIKit.Amber, 44);
            UIKit.Bold(header, "How to Play", 40, UIKit.Ink);
            UIKit.Size(UIKit.Rect("Flex", header), -1, -1, 1);
            refs.helpCloseBtn = UIKit.IconButton("Close", header, "btn_close", "icon_close", Color.white, 70, 38, null);

            var items = new (string, string, string)[]
            {
                ("1. THE OBJECTIVE", "Drive your chunky toy vehicle across the platform and bring it to a complete stop inside the target parking bay.", "icon_flag"),
                ("2. MATCH THE ORIENTATION", "Your car's nose must point in the exact target direction shown above the bay to win.", "icon_compass"),
                ("3. CONSERVE YOUR FUEL", "Every single forward, reverse or turn step burns 1 fuel move. Plan optimal path routes.", "icon_fuel"),
                ("4. DRIVE CONTROLS", "Use Drive, Rev, Left and Right to move. Glowing markers show possible destinations.", "icon_pointer"),
                ("5. SMART CONTROLS", "Steering pivots on the spot. Forward and reverse move one full tile.", "icon_steering")
            };
            foreach (var (title, body, icon) in items)
            {
                var row = UIKit.Rect("Row", panel.transform);
                UIKit.Layout(row, false, 18, null, TextAnchor.UpperLeft);
                UIKit.Icon(row, icon, UIKit.Amber, 36);
                var col = UIKit.Rect("Text", row);
                UIKit.Layout(col, true, 4, null, TextAnchor.UpperLeft, true);
                UIKit.Size(col, -1, -1, 1);
                UIKit.Caps(col, title, 20, UIKit.Ink);
                var desc = UIKit.Label(col, body, 24, UIKit.TextSoft, null, FontStyle.Normal);
                desc.lineSpacing = 1.25f;
            }

            refs.helpGotItBtn = UIKit.Button("GotIt", panel.transform, "btn_green", null);
            UIKit.Layout(refs.helpGotItBtn, false, 0, null, TextAnchor.MiddleCenter);
            UIKit.Size(refs.helpGotItBtn, -1, 94);
            UIKit.Bold(refs.helpGotItBtn.transform, "GOT IT, LET'S ROLL!", 30, UIKit.White, TextAnchor.MiddleCenter);

            refs.helpRoot.SetActive(false);
        }

        private static void BuildPause(RectTransform modalLayer, ParkItUIReferences refs)
        {
            var root = UIKit.Rect("PauseScreen", modalLayer);
            UIKit.Stretch(root);
            refs.pauseRoot = root.gameObject;
            UIKit.Dimmer(root);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(740, 720));
            UIKit.Layout(panel, true, 18, new RectOffset(44, 44, 32, 40), TextAnchor.UpperCenter, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 16, null, TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 90);
            UIKit.Icon(header, "icon_pause", UIKit.Amber, 46);
            UIKit.Bold(header, "Game Paused", 42, UIKit.Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", header), -1, -1, 1);
            refs.pauseCloseBtn = UIKit.IconButton("Close", header, "btn_close", "icon_close", Color.white, 70, 38, null);
            refs.pauseCloseBtn.targetGraphic.color = Color.white;

            var pill = UIKit.Img("LevelPill", panel.transform, "pill_lavender");
            UIKit.Size(pill, 580, 54);
            UIKit.Layout(pill, false, 0, new RectOffset(20, 20, 2, 2), TextAnchor.MiddleCenter);
            refs.pauseLevelText = UIKit.Bold(pill.transform, "Level 1 · First Gear", 26, UIKit.Ink, TextAnchor.MiddleCenter);
            refs.pauseLevelText.horizontalOverflow = HorizontalWrapMode.Overflow;

            UIKit.Size(UIKit.Rect("Gap", panel.transform), 10, 6);

            refs.pauseResumeBtn = UIKit.IconTextButton("Resume", panel.transform, "btn_primary", "icon_play", Color.white, "RESUME", Color.white, 30, 96, null, 36);
            refs.pauseRestartBtn = UIKit.IconTextButton("Restart", panel.transform, "btn_yellow", "icon_restart", UIKit.Amber, "RESTART LEVEL", UIKit.Ink, 28, 96, null, 36);
            refs.pauseLevelSelectBtn = UIKit.IconTextButton("LevelSelect", panel.transform, "btn_blue", "icon_trophy", Color.white, "SELECT LEVEL", Color.white, 28, 96, null, 36);

            refs.pauseSoundBtn = UIKit.IconTextButton("SoundToggle", panel.transform, "btn_secondary", "icon_sound_on", UIKit.Ink, "Sound: On", UIKit.Ink, 26, 90, null, 34);
            refs.pauseSoundIcon = refs.pauseSoundBtn.transform.Find("Icon")?.GetComponent<Image>();
            refs.pauseSoundText = refs.pauseSoundBtn.GetComponentInChildren<Text>();

            refs.pauseRoot.SetActive(false);
        }

        private static void BuildResults(RectTransform modalLayer, ParkItUIReferences refs)
        {
            // ---- victory ----
            var vPanel = CreateResultPanel("Victory", modalLayer, out refs.victoryRoot);
            CreateResultIcon(vPanel, "tile_green", "icon_check_circle", UIKit.White);
            UIKit.Caps(vPanel, "TARGET BAY REACHED!", 24, UIKit.Emerald, TextAnchor.MiddleCenter);
            refs.victoryHeadline = UIKit.Label(vPanel, "PERFECT PARK!", 56, UIKit.Rose, UIKit.Heavy, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Label(vPanel, "Vehicle parked with correct heading alignment.", 26, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);

            var stars = UIKit.Rect("Stars", vPanel);
            UIKit.Layout(stars, false, 18);
            for (int i = 0; i < 3; i++)
            {
                refs.victoryStarTiles[i] = UIKit.Img("StarTile", stars, "tile_amber");
                UIKit.Size(refs.victoryStarTiles[i], 98, 98);
                refs.victoryStarIcons[i] = UIKit.Img("Sparkle", refs.victoryStarTiles[i].transform, "icon_sparkle", UIKit.Amber);
                UIKit.Place(refs.victoryStarIcons[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(54, 54));
            }

            var stats = UIKit.Img("Stats", vPanel, "panel_inset");
            UIKit.Layout(stats, false, 0, new RectOffset(20, 20, 26, 26), TextAnchor.MiddleCenter, true);
            CreateStatColumn(stats.transform, "MOVES USED", UIKit.TextMain, out refs.victoryMoves, out refs.victoryOptimal);
            CreateStatColumn(stats.transform, "FUEL LEFT", UIKit.Emerald, out refs.victoryFuel, out refs.victoryCapacity);

            refs.victoryNextBtn = UIKit.IconTextButton("Next", vPanel, "dock_green", "icon_play", UIKit.White, "Next Puzzle Level", UIKit.White, 45, 100, null, 30);
            refs.victoryNextLabel = refs.victoryNextBtn.GetComponentInChildren<Text>();

            var row = UIKit.Rect("Row", vPanel);
            UIKit.Layout(row, false, 16, null, TextAnchor.MiddleCenter, true);
            refs.victoryReplayBtn = UIKit.IconTextButton("Replay", row, "btn_yellow", "icon_restart", UIKit.TextSoft, "Replay", UIKit.TextMain, 25, 84, null, 28, 12);
            refs.victoryLevelsBtn = UIKit.IconTextButton("Levels", row, "btn_secondary", "icon_grid", UIKit.Emerald, "Levels", UIKit.Emerald, 25, 84, null, 28, 12);

            // ---- defeat ----
            var dPanel = CreateResultPanel("Defeat", modalLayer, out refs.defeatRoot);
            CreateResultIcon(dPanel, "tile_red", "icon_alert", UIKit.White);
            UIKit.Caps(dPanel, "OUT OF FUEL!", 24, UIKit.Rose, TextAnchor.MiddleCenter);
            UIKit.Label(dPanel, "TANK EMPTY", 56, UIKit.Rose, UIKit.Heavy, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Label(dPanel, "Your toy car ran out of moves before reaching the designated parking slot.", 26, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);
            UIKit.Size(UIKit.Rect("Gap", dPanel), 10, 8);

            refs.defeatRewindBtn = UIKit.IconTextButton("Rewind", dPanel, "btn_blue", "icon_undo", UIKit.White, "Rewind Last Move", UIKit.White, 30, 100, null, 32);
            refs.defeatRewindLabel = refs.defeatRewindBtn.GetComponentInChildren<Text>();
            refs.defeatRestartBtn = UIKit.IconTextButton("Restart", dPanel, "btn_pink", "icon_restart", UIKit.C("#f21c8a"), "Restart Puzzle", UIKit.Ink, 27, 88, null, 28);
        }

        private static RectTransform CreateResultPanel(string name, RectTransform parent, out GameObject root)
        {
            var r = UIKit.Rect(name, parent);
            UIKit.Stretch(r);
            root = r.gameObject;
            UIKit.Dimmer(r);
            var panel = UIKit.Img("Panel", r, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(880, 0));
            UIKit.Layout(panel, true, 20, new RectOffset(52, 52, 44, 48), TextAnchor.UpperCenter, true);
            UIKit.Fit(panel, false, true);
            root.SetActive(false);
            return panel.rectTransform;
        }

        private static void CreateResultIcon(Transform parent, string tileSprite, string iconSprite, Color iconColor)
        {
            var tile = UIKit.Img("IconTile", parent, tileSprite);
            UIKit.Size(tile, 128, 128);
            var icon = UIKit.Img("Icon", tile.transform, iconSprite, iconColor);
            UIKit.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72));
        }

        private static void CreateStatColumn(Transform parent, string title, Color valueColor, out Text value, out Text sub)
        {
            var col = UIKit.Rect("Col", parent);
            UIKit.Layout(col, true, 4, null, TextAnchor.MiddleCenter);
            UIKit.Size(col, -1, -1, 1);
            UIKit.Caps(col, title, 20, UIKit.TextDim, TextAnchor.MiddleCenter);
            value = UIKit.Bold(col, "0", 46, valueColor, TextAnchor.MiddleCenter);
            sub = UIKit.Label(col, "Target: 0", 22, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private static void BuildToast(RectTransform toastLayer, ParkItUIReferences refs)
        {
            var toast = UIKit.Img("Toast", toastLayer, "panel_hud");
            refs.toastRect = toast.rectTransform;
            refs.toastRoot = toast.gameObject;
            UIKit.Place(refs.toastRect, new Vector2(.5f, .92f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 78));
            UIKit.Layout(toast, false, 0, new RectOffset(24, 24, 8, 8), TextAnchor.MiddleCenter);
            refs.toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            refs.toastGroup.alpha = 0f;
            refs.toastText = UIKit.Bold(toast.transform, "Message", 26, UIKit.White, TextAnchor.MiddleCenter);
            refs.toastRoot.SetActive(false);
        }
    }
}
