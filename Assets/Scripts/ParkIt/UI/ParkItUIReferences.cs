using System;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>
    /// Holds serialized references for the ParkIt UI Canvas prefab and scene instance.
    /// Enables hybrid workflow: visual prefab inspection in the Unity Editor,
    /// combined with runtime mathematical layout adjustments.
    /// </summary>
    [DisallowMultipleComponent]
    public class ParkItUIReferences : MonoBehaviour
    {
        [Header("Layers")]
        public Canvas canvas;
        public CanvasScaler scaler;
        public RectTransform safeArea;
        public RectTransform uiRoot;
        public RectTransform hudLayer;
        public RectTransform screenLayer;
        public RectTransform modalLayer;
        public RectTransform toastLayer;

        [Header("HUD - Level Card")]
        public RectTransform levelCard;
        public Button levelCardButton;
        public Text levelNumberText;
        public Text levelTitleText;
        public Image levelBadge;
        public Button levelHelpButton;

        [Header("HUD - Top Right & Cards")]
        public RectTransform topRight;
        public RectTransform toolbar;
        public Button levelDesignButton;
        public Button pauseButton;
        public Button hintButton;
        public UnityEngine.UI.Image hintIcon;
        public Button hintMoveButton;
        public GameObject hintPanel;
        public Text hintText;
        public RectTransform targetPill;
        public Text targetHeadingText;
        public Image targetArrow;
        public RectTransform fuelPill;
        public Text fuelText;
        public Text fuelMaxText;
        public Button carColorButton;
        public Text carColorLabel;
        public Image carColorPreview;
        public Image[] carColorSwatches = new Image[4];
        public Image swatchRing;

        [Header("HUD - Dock & Controls")]
        public RectTransform bottomStack;
        public RectTransform dockPanel;
        public RectTransform dockUtility;
        public RectTransform dockDrive;
        public Button undoButton;
        public CanvasGroup undoGroup;
        public GameObject undoBadge;
        public Text undoCountText;
        public Button restartButton;
        public Button camButton;
        public Text camLabel;
        public Button zenButton;
        public Text zenLabel;
        public Image zenIcon;

        [Header("HUD - Drive Buttons")]
        public Button shiftLeftBtn, shiftRightBtn;
        public Image shiftLeftBg, shiftRightBg, shiftLeftRing, shiftRightRing;
        public Button forwardBtn;
        public Image forwardBg;
        public Image forwardRing;

        public Button turnLeftBtn;
        public Image turnLeftBg;
        public Image turnLeftRing;

        public Button turnRightBtn;
        public Image turnRightBg;
        public Image turnRightRing;

        public Button reverseBtn;
        public Image reverseBg;
        public Image reverseRing;

        [Header("Title Screen")]
        public GameObject titleRoot;
        public RectTransform titleColumn;
        public RectTransform titleButtons;
        public Text titleFooter;
        public Button titlePlayBtn;
        public Button titleLevelSelectBtn;
        public Button titleHelpBtn;

        [Header("Level Select Screen")]
        public GameObject levelSelectRoot;
        public RectTransform levelSelectPanel;
        public RectTransform levelGridContent;
        public GridLayoutGroup levelGrid;
        public Button levelSelectCloseBtn;

        [Header("Help Screen")]
        public GameObject helpRoot;
        public Button helpCloseBtn;
        public Button helpGotItBtn;

        [Header("Pause Screen")]
        public GameObject pauseRoot;
        public Text pauseLevelText;
        public Text pauseSoundText;
        public Image pauseSoundIcon;
        public Button pauseResumeBtn;
        public Button pauseRestartBtn;
        public Button pauseLevelSelectBtn;
        public Button pauseMainMenuBtn;
        public Button pauseSoundBtn;
        public Button pauseCloseBtn;

        [Header("Victory Screen")]
        public GameObject victoryRoot;
        public Text victoryHeadline;
        public Text victoryMoves;
        public Text victoryOptimal;
        public Text victoryFuel;
        public Text victoryCapacity;
        public Text victoryNextLabel;
        public Image[] victoryStarTiles = new Image[3];
        public Image[] victoryStarIcons = new Image[3];
        public Button victoryNextBtn;
        public Button victoryReplayBtn;
        public Button victoryLevelsBtn;

        [Header("Defeat Screen")]
        public GameObject defeatRoot;
        public Button defeatRewindBtn;
        public Text defeatRewindLabel;
        public Button defeatRestartBtn;

        [Header("Toast")]
        public GameObject toastRoot;
        public Text toastText;
        public CanvasGroup toastGroup;
        public RectTransform toastRect;

        [Header("Modals - Level Creator")]
        public LevelCreatorUI levelCreator;
    }
}
