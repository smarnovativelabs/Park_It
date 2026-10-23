using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    public partial class ParkItUIManager
    {
        private GameObject victoryRoot, defeatRoot;
        private Text victoryHeadline, victoryMoves, victoryOptimal, victoryFuel, victoryCapacity, victoryNextLabel, rewindLabel;
        private readonly Image[] victoryStarTiles = new Image[3];
        private readonly Image[] victoryStarIcons = new Image[3];
        private readonly Sprite[] authoredStarSprites = new Sprite[3];
        private readonly Color[] authoredStarColors = new Color[3];
        private Button rewindButton;

        public void BindResults(ParkItUIReferences refs)
        {
            if (refs == null) return;

            victoryRoot = refs.victoryRoot;
            victoryHeadline = refs.victoryHeadline;
            victoryMoves = refs.victoryMoves;
            victoryOptimal = refs.victoryOptimal;
            victoryFuel = refs.victoryFuel;
            victoryCapacity = refs.victoryCapacity;
            victoryNextLabel = refs.victoryNextLabel;
            if (refs.victoryStarTiles != null) System.Array.Copy(refs.victoryStarTiles, victoryStarTiles, Mathf.Min(refs.victoryStarTiles.Length, victoryStarTiles.Length));
            if (refs.victoryStarIcons != null) System.Array.Copy(refs.victoryStarIcons, victoryStarIcons, Mathf.Min(refs.victoryStarIcons.Length, victoryStarIcons.Length));
            for (int i = 0; i < 3; i++)
            {
                authoredStarSprites[i] = victoryStarTiles[i].sprite;
                authoredStarColors[i] = victoryStarIcons[i].color;
            }

            if (refs.victoryNextBtn != null)
            {
                refs.victoryNextBtn.onClick.RemoveAllListeners();
                refs.victoryNextBtn.onClick.AddListener(OnClickNextLevel);
            }
            if (refs.victoryReplayBtn != null)
            {
                refs.victoryReplayBtn.onClick.RemoveAllListeners();
                refs.victoryReplayBtn.onClick.AddListener(OnClickRestart);
            }
            if (refs.victoryLevelsBtn != null)
            {
                refs.victoryLevelsBtn.onClick.RemoveAllListeners();
                refs.victoryLevelsBtn.onClick.AddListener(() => { HideResults(); OpenLevelSelect(); });
            }

            defeatRoot = refs.defeatRoot;
            rewindButton = refs.defeatRewindBtn;
            rewindLabel = refs.defeatRewindLabel;
            if (refs.defeatRewindBtn != null)
            {
                refs.defeatRewindBtn.onClick.RemoveAllListeners();
                refs.defeatRewindBtn.onClick.AddListener(OnClickUndo);
            }
            if (refs.defeatRestartBtn != null)
            {
                refs.defeatRestartBtn.onClick.RemoveAllListeners();
                refs.defeatRestartBtn.onClick.AddListener(OnClickRestart);
            }
        }

        private void BuildResults()
        {
            // ---- victory ----
            var vPanel = ResultPanel("Victory", out victoryRoot);
            ResultIcon(vPanel, "tile_green", "icon_check_circle", UIKit.White);
            UIKit.Caps(vPanel, "TARGET BAY REACHED!", 24, UIKit.Emerald, TextAnchor.MiddleCenter);
            victoryHeadline = UIKit.Label(vPanel, "PERFECT PARK!", 56, UIKit.Rose, UIKit.Heavy, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Label(vPanel, "Vehicle parked with correct heading alignment.", 26, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);

            var stars = UIKit.Rect("Stars", vPanel);
            UIKit.Layout(stars, false, 18);
            for (int i = 0; i < 3; i++)
            {
                victoryStarTiles[i] = UIKit.Img("StarTile", stars, "tile_amber");
                UIKit.Size(victoryStarTiles[i], 98, 98);
                victoryStarIcons[i] = UIKit.Img("Sparkle", victoryStarTiles[i].transform, "icon_sparkle", UIKit.Amber);
                UIKit.Place(victoryStarIcons[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(54, 54));
            }

            var stats = UIKit.Img("Stats", vPanel, "panel_inset");
            UIKit.Layout(stats, false, 0, new RectOffset(20, 20, 26, 26), TextAnchor.MiddleCenter, true);
            StatColumn(stats.transform, "MOVES USED", UIKit.TextMain, out victoryMoves, out victoryOptimal);
            StatColumn(stats.transform, "FUEL LEFT", UIKit.Emerald, out victoryFuel, out victoryCapacity);

            var next = UIKit.IconTextButton("Next", vPanel, "btn_green", "icon_play", UIKit.White, "Next Puzzle Level", UIKit.White, 30, 100, OnClickNextLevel, 30);
            victoryNextLabel = next.GetComponentInChildren<Text>();
            var row = UIKit.Rect("Row", vPanel);
            UIKit.Layout(row, false, 16, null, TextAnchor.MiddleCenter, true);
            UIKit.IconTextButton("Replay", row, "btn_yellow", "icon_restart", UIKit.TextSoft, "Replay", UIKit.TextMain, 25, 84, OnClickRestart, 28, 12);
            UIKit.IconTextButton("Levels", row, "btn_secondary", "icon_grid", UIKit.Emerald, "Levels", UIKit.Emerald, 25, 84, () => { HideResults(); OpenLevelSelect(); }, 28, 12);
            // ---- defeat ----
            var dPanel = ResultPanel("Defeat", out defeatRoot);
            ResultIcon(dPanel, "tile_red", "icon_alert", UIKit.White);
            UIKit.Caps(dPanel, "OUT OF FUEL!", 24, UIKit.Rose, TextAnchor.MiddleCenter);
            UIKit.Label(dPanel, "TANK EMPTY", 56, UIKit.Rose, UIKit.Heavy, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Label(dPanel, "Your toy car ran out of moves before reaching the designated parking slot.", 26, UIKit.TextMuted, null, FontStyle.Normal, TextAnchor.MiddleCenter);
            UIKit.Size(UIKit.Rect("Gap", dPanel), 10, 8);
            rewindButton = UIKit.IconTextButton("Rewind", dPanel, "btn_blue", "icon_undo", UIKit.White, "Rewind Last Move", UIKit.White, 30, 100, OnClickUndo, 32);
            rewindLabel = rewindButton.GetComponentInChildren<Text>();
            UIKit.IconTextButton("Restart", dPanel, "btn_pink", "icon_restart", UIKit.C("#f21c8a"), "Restart Puzzle", UIKit.Ink, 27, 88, OnClickRestart, 28);
        }

        private RectTransform ResultPanel(string name, out GameObject root)
        {
            var r = UIKit.Rect(name, modalLayer);
            UIKit.Stretch(r);
            root = r.gameObject;
            UIKit.Dimmer(r);
            var panel = UIKit.Img("Panel", r, "panel_modal", null, true);
            UIKit.Place(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(880, 0));
            UIKit.Layout(panel, true, 20, new RectOffset(56, 56, 56, 52), TextAnchor.UpperCenter, true);
            UIKit.Fit(panel, false, true);
            root.SetActive(false);
            return panel.rectTransform;
        }

        private static void ResultIcon(Transform parent, string tile, string icon, Color color)
        {
            var wrap = UIKit.Rect("IconWrap", parent);
            UIKit.Layout(wrap, false, 0);
            var t = UIKit.Img("Tile", wrap, tile);
            UIKit.Size(t, 124, 124);
            var i = UIKit.Img("Icon", t.transform, icon, color);
            UIKit.Place(i.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(64, 64));
        }

        private static void StatColumn(Transform parent, string title, Color valueColor, out Text value, out Text sub)
        {
            var col = UIKit.Rect(title, parent);
            UIKit.Layout(col, true, 6, null, TextAnchor.MiddleCenter);
            UIKit.Caps(col, title, 20, UIKit.TextDim, TextAnchor.MiddleCenter);
            value = UIKit.Label(col, "0", 42, valueColor, UIKit.Mono, FontStyle.Bold, TextAnchor.MiddleCenter);
            sub = UIKit.Label(col, "", 22, UIKit.TextDim, null, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void PopulateVictory()
        {
            var level = CurrentLevel;
            if (level == null || carController == null) return;
            int moves = carController.MovesUsed;
            int stars = LevelCatalog.StarsFor(moves, level.optimalMoves);
            victoryHeadline.text = stars == 3 ? "PERFECT PARK!" : stars == 2 ? "NICE PARKING!" : "PARKED!";
            for (int i = 0; i < 3; i++)
            {
                bool on = i < stars;
                if (on) victoryStarTiles[i].sprite = authoredStarSprites[i];
                else UIKit.SetSprite(victoryStarTiles[i], "tile_slate");
                victoryStarIcons[i].color = on ? authoredStarColors[i] : UIKit.TextDim;
            }
            victoryMoves.text = moves.ToString();
            victoryOptimal.text = $"Optimal: {level.optimalMoves}";
            victoryFuel.text = carController.RemainingFuel.ToString();
            victoryCapacity.text = $"Capacity: {level.maxFuel}";
            bool custom = GameManager.Instance != null && GameManager.Instance.IsPlayingCustom;
            victoryNextLabel.text = custom ? "Back to Level Creator" : "Next Puzzle Level";
            RefreshLevelSelect();
            UIPopupTransition.Show(victoryRoot);
        }

        private void PopulateDefeat()
        {
            int undo = UndoManager.Instance != null ? UndoManager.Instance.UndoCount : 0;
            rewindLabel.text = $"Rewind Last Move ({undo} left)";
            rewindButton.interactable = undo > 0;
            UIPopupTransition.Show(defeatRoot);
        }
    }
}
