using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    public partial class ParkItUIManager
    {
        private class LevelCard
        {
            public int index;
            public Image background;
            public Image[] stars;
            public UnityEngine.UI.Button button;
            public UnityEngine.UI.Text hint;
            public string objectiveHint;
        }

        private GameObject titleRoot, levelSelectRoot, helpRoot;
        private RectTransform titleColumn, titleButtons, levelSelectPanel, levelGridContent;
        private GridLayoutGroup levelGrid;
        private Text titleFooter;
        private readonly List<LevelCard> levelCards = new List<LevelCard>();
        private CanvasGroup toastGroup;
        private Text toastText;
        private LevelCreatorUI creator;

        public void BindScreens(ParkItUIReferences refs)
        {
            if (refs == null) return;

            titleRoot = refs.titleRoot;
            titleColumn = refs.titleColumn;
            titleButtons = refs.titleButtons;
            titleFooter = refs.titleFooter;
            if (refs.titlePlayBtn != null)
            {
                refs.titlePlayBtn.onClick.RemoveAllListeners();
                refs.titlePlayBtn.onClick.AddListener(StartGame);
            }
            if (refs.titleLevelSelectBtn != null)
            {
                refs.titleLevelSelectBtn.onClick.RemoveAllListeners();
                refs.titleLevelSelectBtn.onClick.AddListener(OpenLevelSelect);
            }

            levelSelectRoot = refs.levelSelectRoot;
            levelSelectPanel = refs.levelSelectPanel;
            levelGridContent = refs.levelGridContent;
            levelGrid = refs.levelGrid;
            if (refs.levelSelectCloseBtn != null)
            {
                refs.levelSelectCloseBtn.onClick.RemoveAllListeners();
                refs.levelSelectCloseBtn.onClick.AddListener(CloseLevelSelect);
            }

            helpRoot = refs.helpRoot;
            if (refs.helpCloseBtn != null)
            {
                refs.helpCloseBtn.onClick.RemoveAllListeners();
                refs.helpCloseBtn.onClick.AddListener(CloseHelp);
            }
            if (refs.helpGotItBtn != null)
            {
                refs.helpGotItBtn.onClick.RemoveAllListeners();
                refs.helpGotItBtn.onClick.AddListener(AdvanceTutorial);
            }

            pauseRoot = refs.pauseRoot;
            if (refs.pauseMainMenuBtn != null)
            {
                refs.pauseMainMenuBtn.onClick.RemoveAllListeners();
                refs.pauseMainMenuBtn.onClick.AddListener(ReturnToMainMenu);
            }
            pauseLevelText = refs.pauseLevelText;
            pauseSoundText = refs.pauseSoundText;
            pauseSoundIcon = refs.pauseSoundIcon;
            if (refs.pauseResumeBtn != null)
            {
                refs.pauseResumeBtn.onClick.RemoveAllListeners();
                refs.pauseResumeBtn.onClick.AddListener(ClosePause);
            }
            if (refs.pauseRestartBtn != null)
            {
                refs.pauseRestartBtn.onClick.RemoveAllListeners();
                refs.pauseRestartBtn.onClick.AddListener(() => { ClosePause(); OnClickRestart(); });
            }
            if (refs.pauseLevelSelectBtn != null)
            {
                refs.pauseLevelSelectBtn.onClick.RemoveAllListeners();
                refs.pauseLevelSelectBtn.onClick.AddListener(() => { ClosePause(); OpenLevelSelect(); });
            }
            if (refs.pauseSoundBtn != null)
            {
                refs.pauseSoundBtn.onClick.RemoveAllListeners();
                refs.pauseSoundBtn.onClick.AddListener(() => { ToggleSound(); RefreshPause(); });
            }
            if (refs.pauseCloseBtn != null)
            {
                refs.pauseCloseBtn.onClick.RemoveAllListeners();
                refs.pauseCloseBtn.onClick.AddListener(ClosePause);
            }

            toastGroup = refs.toastGroup;
            toastText = refs.toastText;
        }

        public void CloseLevelSelect() => UIPopupTransition.Hide(levelSelectRoot);
        public void CloseHelp() { StopButtonTutorial(); UIPopupTransition.Hide(helpRoot); }
        public void OpenHelp()
        {
            ClosePause();
            UIPopupTransition.Hide(helpRoot);
            BeginButtonTutorial();
        }

        private int tutorialPage;
        private static readonly string[] TutorialTitles = {
            "PARK IN THE GREEN BAY", "MEET YOUR CONTROLS", "WATCH YOUR FUEL", "NEED A HINT?", "TRY AGAIN ANY TIME"
        };
        private static readonly string[] TutorialBodies = {
            "Reach the green parking bay. Point the car's nose in the same direction as the bay's arrow to win.",
            "Drive moves forward. Rev moves backward. Left and Right move to the side AND turn the car. Use the bottom buttons to drive.",
            "Each move uses 1 fuel. Hydrants block your path. Driving onto oil slides you forward until dry ground or a blocked tile.",
            "Tap the bulb just below Pause. A green highlight marks the next button to press. Hints cost no fuel. Tap again after moving for your next hint.",
            "Undo takes back a move. Restart gives you a fresh attempt. If a route cannot finish with your fuel, Hint points to Undo or Restart. Reopen this guide with the yellow Help button."
        };
        private static readonly string[] TutorialIcons = { "icon_target", "icon_arrow_up", "icon_fuel", "icon_hint_bulb", "icon_undo" };

        private void AdvanceTutorial()
        {
            if (tutorialPage < TutorialTitles.Length - 1) { tutorialPage++; RefreshTutorial(); return; }
            ProgressSave.Instance.CompleteTutorial();
            CloseHelp();
        }

        private void RefreshTutorial()
        {
            if (helpRoot == null) return;
            var panel = helpRoot.transform.Find("Panel");
            if (panel == null) return;
            var heading = panel.Find("Header")?.GetComponentInChildren<Text>();
            if (heading != null) heading.text = $"How to Play  {tutorialPage + 1}/5";
            int rowIndex = 0;
            foreach (Transform row in panel)
            {
                if (row.name != "Row") continue;
                bool visible = rowIndex++ == 0;
                row.gameObject.SetActive(visible);
                if (!visible) continue;
                var labels = row.GetComponentsInChildren<Text>(true);
                if (labels.Length >= 2) {
                    labels[0].text = TutorialTitles[tutorialPage];
                    labels[1].text = TutorialBodies[tutorialPage];
                } else if (labels.Length == 1) labels[0].text = TutorialTitles[tutorialPage] + "\n\n" + TutorialBodies[tutorialPage];
                var icon = row.GetComponentInChildren<Image>();
                if (icon != null) { UIKit.SetSprite(icon, TutorialIcons[tutorialPage]); icon.color = tutorialPage == 0 ? UIKit.Emerald : Color.white; }
            }
            var next = panel.Find("GotIt")?.GetComponentInChildren<Text>();
            if (next != null) next.text = tutorialPage == 4 ? "LET'S PLAY!" : "NEXT";
        }

        // ---------- title ----------
        private void BuildTitle()
        {
            var root = UIKit.Rect("TitleScreen", screenLayer);
            UIKit.Stretch(root);
            titleRoot = root.gameObject;
            UIKit.Dimmer(root).color = new Color(.72f, .43f, .91f, .82f);
            var vignette = UIKit.Img("Vignette", root, "vignette", new Color(.8f, .55f, 1f, .18f));
            vignette.preserveAspect = false;
            UIKit.Stretch(vignette.rectTransform);

            titleColumn = UIKit.Rect("Column", root);
            UIKit.Place(titleColumn, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(1100, 0));
            UIKit.Layout(titleColumn, true, 30, null, TextAnchor.MiddleCenter);
            UIKit.Fit(titleColumn, false, true);

            var logo = UIKit.Img("Logo", titleColumn, "logo_parkit");
            UIKit.Size(logo, 760, 169);
            var underline = UIKit.Img("Underline", titleColumn, "underline");
            UIKit.Size(underline, 300, 10);

            var blurb = UIKit.Label(titleColumn,
                "Navigate the chunky diorama vehicle across the puzzle grid.\nMatch the parking bay's exact slot and facing orientation\nbefore your fuel runs dry.",
                30, UIKit.TextSoft, UIKit.Sans, FontStyle.Normal, TextAnchor.MiddleCenter);
            blurb.lineSpacing = 1.25f;
            UIKit.Size(blurb, 1000, -1);

            UIKit.Size(UIKit.Rect("Gap", titleColumn), 10, 16);

            titleButtons = UIKit.Rect("Buttons", titleColumn);
            UIKit.Layout(titleButtons, false, 28);

            var startWrap = UIKit.Rect("StartWrap", titleButtons);
            UIKit.Layout(startWrap, false, 0);
            var glow = UIKit.Img("Glow", startWrap, "glow", new Color(1f, .62f, .04f, .4f));
            glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Stretch(glow.rectTransform, -34);
            UIKit.IconTextButton("StartEngine", startWrap, "btn_primary", "icon_play", UIKit.White, "Play", UIKit.White, 28, 104, StartGame, 30, 44);
            UIKit.IconTextButton("SelectLevel", titleButtons, "btn_yellow", "icon_trophy", UIKit.Amber, "Select Level", UIKit.TextMain, 28, 104, OpenLevelSelect, 34, 40);

            titleFooter = UIKit.Caps(root, "", 21, UIKit.TextDim, TextAnchor.MiddleCenter);
            titleFooter.gameObject.SetActive(false);
            UIKit.Place(titleFooter.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(1000, 80));
        }

        // ---------- level select ----------
        public void OpenLevelSelect()
        {
            ClosePause();
            CloseHelp();
            if (levelCards.Count == 0 && levelGridContent != null)
            {
                var levels = GameManager.Instance != null && GameManager.Instance.Levels.Count > 0 ? GameManager.Instance.Levels : (IReadOnlyList<LevelData>)LevelCatalog.CreateLevels();
                for (int k = 0; k < levels.Count; k++) levelCards.Add(BuildLevelCard(levels[k], k));
            }
            RefreshLevelSelect();
            UIPopupTransition.Show(levelSelectRoot);
        }

        private void BuildLevelSelect()
        {
            var root = UIKit.Rect("LevelSelect", modalLayer);
            UIKit.Stretch(root);
            levelSelectRoot = root.gameObject;
            UIKit.Dimmer(root, CloseLevelSelect);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            levelSelectPanel = panel.rectTransform;
            UIKit.Place(levelSelectPanel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(980, 1520));
            UIKit.Layout(panel, true, 0, new RectOffset(0, 0, 0, 0), TextAnchor.UpperCenter, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 16, new RectOffset(40, 28, 8, 0), TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 110);
            UIKit.Icon(header, "icon_trophy", UIKit.Amber, 46);
            UIKit.Bold(header, "Select Level", 42, UIKit.Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", header), -1, -1, 1);
            var close = UIKit.IconButton("Close", header, "btn_close", "icon_close", Color.white, 70, 38, CloseLevelSelect);
            close.targetGraphic.color = Color.white;

            var scroll = BuildScroll(panel.transform, out levelGridContent);
            UIKit.Size(scroll, -1, -1, 1, 1);
            levelGrid = levelGridContent.gameObject.AddComponent<GridLayoutGroup>();
            levelGrid.padding = new RectOffset(36, 36, 20, 36);
            levelGrid.spacing = new Vector2(24, 24);
            levelGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            levelGrid.constraintCount = 2;
            UIKit.Fit(levelGridContent, false, true);

            var levels = GameManager.Instance != null && GameManager.Instance.Levels.Count > 0 ? GameManager.Instance.Levels : (IReadOnlyList<LevelData>)LevelCatalog.CreateLevels();
            for (int k = 0; k < levels.Count; k++) levelCards.Add(BuildLevelCard(levels[k], k));
            levelSelectRoot.SetActive(false);
        }

        /// <summary>Vertical ScrollRect with a spritesheet scrollbar; returns the scroll root.</summary>
        public static ScrollRect BuildScroll(Transform parent, out RectTransform content)
        {
            var scrollRt = UIKit.Rect("Scroll", parent);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            var viewport = UIKit.Stretch(UIKit.Rect("Viewport", scrollRt));
            viewport.offsetMax = new Vector2(-42, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            content = UIKit.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;

            var bar = UIKit.Img("Scrollbar", scrollRt, "scroll_track", null, true);
            bar.rectTransform.anchorMin = new Vector2(1, 0);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(1, .5f);
            bar.rectTransform.sizeDelta = new Vector2(10, -56);
            bar.rectTransform.anchoredPosition = new Vector2(-22, 0);
            var area = UIKit.Stretch(UIKit.Rect("Area", bar.transform), 2);
            var handle = UIKit.Img("Handle", area, "scroll_handle", null, true);
            UIKit.Stretch(handle.rectTransform);
            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scroll;
        }

        private LevelCard BuildLevelCard(LevelData level, int index)
        {
            var b = UIKit.Button($"Level_{level.levelIndex}", levelGridContent, "dock_utility", () =>
            {
                if (GameManager.Instance == null || !GameManager.Instance.IsLevelUnlocked(index)) return;
                CloseLevelSelect();
                ShowTitle(false);
                GameManager.Instance?.LoadLevel(index);
            });
            UIKit.Layout(b, true, 4, new RectOffset(48, 48, 32, 36), TextAnchor.UpperLeft, true);

            var top = UIKit.Rect("Top", b.transform);
            UIKit.Layout(top, false, 10, null, TextAnchor.MiddleLeft);
            UIKit.Bold(top, $"Level {level.levelIndex}", 36, UIKit.Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", top), -1, -1, 1);
            UIKit.Badge(top, level.difficulty, 30);

            UIKit.Bold(b.transform, level.levelTitle, 48, UIKit.Ink);
            var hint = UIKit.Label(b.transform, level.objectiveHint, 36, UIKit.TextSoft);
            hint.lineSpacing = 1.15f;
            UIKit.Size(UIKit.Rect("Flex", b.transform), -1, -1, -1, 1);

            var bottom = UIKit.Rect("Bottom", b.transform);
            UIKit.Layout(bottom, false, 8, null, TextAnchor.MiddleLeft);
            var stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                stars[s] = UIKit.Img("Star", bottom, "star_empty");
                UIKit.Size(stars[s], 44, 44);
            }
            UIKit.Size(UIKit.Rect("Flex", bottom), -1, -1, 1);
            if (level.oilSpills != null && level.oilSpills.Count > 0)
            {
                UIKit.Icon(bottom, "icon_oil", UIKit.Sky, 24);
                UIKit.Bold(bottom, "Oil", 32, UIKit.Sky).horizontalOverflow = HorizontalWrapMode.Overflow;
                UIKit.Size(UIKit.Rect("Gap", bottom), 8, 4);
            }
            UIKit.Icon(bottom, "icon_obstacle", UIKit.Amber, 24);
            UIKit.Bold(bottom, (level.obstacles?.Count ?? 0).ToString(), 32, UIKit.Amber).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Gap", bottom), 8, 4);
            UIKit.Bold(bottom, $"{level.gridWidth} \u00d7 {level.gridHeight}", 32, UIKit.TextSoft).horizontalOverflow = HorizontalWrapMode.Overflow;

            return new LevelCard { index = index, background = (Image)b.targetGraphic, stars = stars,
                button = b, hint = hint, objectiveHint = level.objectiveHint };
        }

        private void RefreshLevelSelect()
        {
            var gm = GameManager.Instance;
            foreach (var card in levelCards)
            {
                bool unlocked = gm != null && gm.IsLevelUnlocked(card.index);
                card.button.interactable = unlocked;
                card.hint.text = unlocked ? card.objectiveHint : $"Locked - complete Level {card.index} first";
                bool current = gm != null && !gm.IsPlayingCustom && gm.CurrentLevelIndex == card.index;
                UIKit.SetSprite(card.background, current ? "dock_yellow" : "dock_utility");
                int levelNumber = gm != null && card.index < gm.Levels.Count ? gm.Levels[card.index].levelIndex : card.index + 1;
                int earned = LevelCatalog.GetStars(levelNumber);
                for (int s = 0; s < 3; s++) UIKit.SetSprite(card.stars[s], s < earned ? "star_gold" : "star_empty");
            }
        }

        // ---------- help ----------
        private void BuildHelp()
        {
            var root = UIKit.Rect("Help", modalLayer);
            UIKit.Stretch(root);
            helpRoot = root.gameObject;
            UIKit.Dimmer(root, CloseHelp);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(940, 0));
            UIKit.Layout(panel, true, 22, new RectOffset(56, 56, 48, 52), TextAnchor.UpperLeft, true);
            UIKit.Fit(panel, false, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 18, null, TextAnchor.MiddleLeft);
            UIKit.IconButton("Badge", header, "tile_amber", "icon_help", UIKit.Amber, 76, 40, null).interactable = false;
            UIKit.Label(header, "How to Play", 40, UIKit.TextMain, UIKit.Heavy, FontStyle.Bold).horizontalOverflow = HorizontalWrapMode.Overflow;

            HelpRow(panel.transform, "icon_target", UIKit.Emerald, "Park on the <b>green bay</b> facing its arrow before your fuel runs out.");
            HelpRow(panel.transform, "icon_arrow_up", UIKit.Amber, "Use the <b>Drive, Rev, Left and Right</b> buttons to move. Every move burns 1 fuel.");
            HelpRow(panel.transform, "icon_oil", UIKit.Sky, "<b>Oil slicks</b> keep you sliding forward until you reach dry tarmac.");
            HelpRow(panel.transform, "icon_hint_bulb", UIKit.Emerald, "Tap the bulb below Pause for a free hint. The green highlight shows your next control.");
            HelpRow(panel.transform, "icon_undo", UIKit.TextSoft, "Use Undo to take back a move, or Restart to try again.");

            UIKit.IconTextButton("GotIt", panel.transform, "btn_primary", "icon_check_circle", UIKit.Ink, "Next", UIKit.Ink, 30, 96, AdvanceTutorial, 34);
            if (helpRoot != null) helpRoot.SetActive(false);
            if (levelCards.Count == 0 && levelGridContent != null)
            {
                var levels = GameManager.Instance != null && GameManager.Instance.Levels.Count > 0 ? GameManager.Instance.Levels : (IReadOnlyList<LevelData>)LevelCatalog.CreateLevels();
                for (int k = 0; k < levels.Count; k++) levelCards.Add(BuildLevelCard(levels[k], k));
            }
        }

        private static void HelpRow(Transform parent, string icon, Color color, string text)
        {
            var row = UIKit.Rect("Row", parent);
            UIKit.Layout(row, false, 22, null, TextAnchor.MiddleLeft);
            UIKit.IconButton("Icon", row, "tile_slate", icon, color, 64, 34, null).interactable = false;
            var t = UIKit.Label(row, text, 27, UIKit.TextSoft);
            UIKit.Size(t, -1, -1, 1);
        }

        // ---------- pause ----------
        private GameObject pauseRoot;
        private Text pauseLevelText, pauseSoundText;
        private Image pauseSoundIcon;

        public void OpenPause()
        {
            if (pauseRoot == null) return;
            RefreshPause();
            UIPopupTransition.Show(pauseRoot);
            Time.timeScale = 0f;
        }

        public void ReturnToMainMenu()
        {
            ClosePause();
            HideResults();
            ShowTitle(true);
        }

        public void ClosePause()
        {
            if (pauseRoot == null) return;
            UIPopupTransition.Hide(pauseRoot);
            Time.timeScale = 1f;
        }

        private void RefreshPause()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentLevelData != null && pauseLevelText != null)
            {
                pauseLevelText.text = $"Level {gm.CurrentLevelData.levelIndex} \u00b7 {gm.CurrentLevelData.levelTitle}";
            }
            if (pauseSoundText != null)
            {
                pauseSoundText.text = SfxPlayer.Muted ? "Sound: Off" : "Sound: On";
            }
            if (pauseSoundIcon != null)
            {
                UIKit.SetSprite(pauseSoundIcon, SfxPlayer.Muted ? "icon_sound_off" : "icon_sound_on");
            }
        }

        private void BuildPause()
        {
            var root = UIKit.Rect("PauseScreen", modalLayer);
            UIKit.Stretch(root);
            pauseRoot = root.gameObject;
            UIKit.Dimmer(root, ClosePause);

            var panel = UIKit.Img("Panel", root, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(740, 720));
            UIKit.Layout(panel, true, 18, new RectOffset(44, 44, 32, 40), TextAnchor.UpperCenter, true);

            var header = UIKit.Rect("Header", panel.transform);
            UIKit.Layout(header, false, 16, null, TextAnchor.MiddleLeft);
            UIKit.Size(header, -1, 90);
            UIKit.Icon(header, "icon_pause", UIKit.Amber, 46);
            UIKit.Bold(header, "Game Paused", 42, UIKit.Ink).horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Size(UIKit.Rect("Flex", header), -1, -1, 1);
            var close = UIKit.IconButton("Close", header, "btn_close", "icon_close", Color.white, 70, 38, ClosePause);
            close.targetGraphic.color = Color.white;

            var pill = UIKit.Img("LevelPill", panel.transform, "pill_lavender");
            UIKit.Size(pill, 580, 54);
            UIKit.Layout(pill, false, 0, new RectOffset(20, 20, 2, 2), TextAnchor.MiddleCenter);
            pauseLevelText = UIKit.Bold(pill.transform, "Level 1 \u00b7 First Gear", 26, UIKit.Ink, TextAnchor.MiddleCenter);
            pauseLevelText.horizontalOverflow = HorizontalWrapMode.Overflow;

            UIKit.Size(UIKit.Rect("Gap", panel.transform), 10, 6);

            UIKit.IconTextButton("Resume", panel.transform, "btn_primary", "icon_play", Color.white, "RESUME", Color.white, 30, 96, ClosePause, 36);

            UIKit.IconTextButton("Restart", panel.transform, "btn_yellow", "icon_restart", UIKit.Amber, "RESTART LEVEL", UIKit.Ink, 28, 96, () =>
            {
                ClosePause();
                OnClickRestart();
            }, 36);

            UIKit.IconTextButton("LevelSelect", panel.transform, "btn_blue", "icon_trophy", Color.white, "SELECT LEVEL", Color.white, 28, 96, () =>
            {
                ClosePause();
                OpenLevelSelect();
            }, 36);

            var sndBtn = UIKit.IconTextButton("SoundToggle", panel.transform, "btn_secondary", SfxPlayer.Muted ? "icon_sound_off" : "icon_sound_on", UIKit.Ink, SfxPlayer.Muted ? "Sound: Off" : "Sound: On", UIKit.Ink, 26, 90, () =>
            {
                ToggleSound();
                RefreshPause();
            }, 34);
            pauseSoundIcon = sndBtn.transform.Find("Icon")?.GetComponent<Image>();
            pauseSoundText = sndBtn.GetComponentInChildren<Text>();

            pauseRoot.SetActive(false);
        }

        // ---------- toast ----------
        private void BuildToast()
        {
            var toast = UIKit.Img("Toast", toastLayer, "panel_hud");
            UIKit.Place(toast.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -120), Vector2.zero);
            UIKit.Layout(toast, false, 0, new RectOffset(36, 36, 22, 22));
            UIKit.Fit(toast);
            toastText = UIKit.Bold(toast.transform, "", 26, UIKit.TextMain, TextAnchor.MiddleCenter);
            toastText.horizontalOverflow = HorizontalWrapMode.Overflow;
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toast.gameObject.SetActive(false);
        }

        private void LayoutScreens(bool portrait)
        {
            // Title: buttons stack vertically on narrow portrait screens.
            var titleLayout = titleButtons.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if ((titleLayout is VerticalLayoutGroup) != portrait)
            {
                UIKit.Layout(titleButtons, portrait, titleLayout.spacing, titleLayout.padding,
                    titleLayout.childAlignment, titleLayout.childForceExpandWidth, titleLayout.childForceExpandHeight);
                titleColumn.sizeDelta = new Vector2(portrait ? 1000 : 1200, titleColumn.sizeDelta.y);
                titleFooter.rectTransform.sizeDelta = new Vector2(portrait ? 900 : 1400, titleFooter.rectTransform.sizeDelta.y);
            }

            // Full-width portrait cards keep descriptions readable.
            if ((levelSelectPanel.sizeDelta.y >= levelSelectPanel.sizeDelta.x) != portrait ||
                levelGrid.constraintCount != (portrait ? 1 : 2))
            {
                float panelW = portrait ? 980 : 1380, panelH = portrait ? 1520 : 920;
                levelSelectPanel.sizeDelta = new Vector2(panelW, panelH);
                int columns = levelGrid.constraintCount = portrait ? 1 : 2;
                float cellW = (panelW - 42 - levelGrid.padding.horizontal - levelGrid.spacing.x * (columns - 1)) / columns;
                levelGrid.cellSize = new Vector2(cellW, portrait ? 300 : 360);
                // A column-count change invalidates the previous scroll offset.
                LayoutRebuilder.ForceRebuildLayoutImmediate(levelGridContent);
                var scroll = levelSelectPanel.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null) scroll.verticalNormalizedPosition = 1;
            }
    }
}
}
