using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>
    /// Builds and drives the whole Park It! interface at runtime: title screen, level select,
    /// HUD + driving dock, result modals, help, toasts and the Level Creator.
    /// All graphics come from the ParkIt_UI_Sheet spritesheet (see UISpriteAtlas / UIKit).
    /// </summary>
    public partial class ParkItUIManager : MonoBehaviour
    {
        public static ParkItUIManager Instance { get; private set; }

        [SerializeField] private ParkItUIReferences uiReferences;
        public ParkItUIReferences UIReferences => uiReferences;

        private CarController carController;
        private Canvas canvas;
        private CanvasScaler scaler;
        private RectTransform uiRoot;
        private RectTransform hudLayer, screenLayer, modalLayer, toastLayer;
        private bool built;
        private bool? lastPortrait;
        private Vector2 lastScreen;
        private Coroutine resultRoutine;

        public bool IsPortrait => Screen.height >= Screen.width;

        private void Awake()
        {
            Instance = this;
            InitializeUI();
        }

        public void InitializeUI()
        {
            if (built) return;

            if (uiReferences == null)
            {
                var canvasObj = GameObject.Find("ParkIt_Mobile_Canvas");
                if (canvasObj != null) uiReferences = canvasObj.GetComponent<ParkItUIReferences>();
            }

            if (uiReferences != null && uiReferences.hudLayer != null)
            {
                BindUI(uiReferences);
            }
            else
            {
                BuildUI();
            }
        }

        private void Start()
        {
            carController = FindFirstObjectByType<CarController>();
            if (carController != null)
            {
                carController.OnFuelChanged += UpdateFuelDisplay;
                carController.OnStateChanged += RefreshHUD;
                carController.OnLevelWon += ShowVictory;
                carController.OnLevelLost += ShowDefeat;
            }
            HideResults();
            ShowTitle(true);
        }

        private void OnDestroy()
        {
            if (carController != null)
            {
                carController.OnFuelChanged -= UpdateFuelDisplay;
                carController.OnStateChanged -= RefreshHUD;
                carController.OnLevelWon -= ShowVictory;
                carController.OnLevelLost -= ShowDefeat;
            }
            if (Instance == this) Instance = null;
        }

        /// <summary>Kept for the scene bootstrapper / editor tools; the UI is generated on Awake.</summary>
        public void LoadAndApplySprites() { }

        public void BindUI(ParkItUIReferences refs)
        {
            if (refs == null) return;
            uiReferences = refs;
            built = true;

            canvas = refs.canvas;
            scaler = refs.scaler;
            uiRoot = refs.uiRoot;
            hudLayer = refs.hudLayer;
            screenLayer = refs.screenLayer;
            modalLayer = refs.modalLayer;
            toastLayer = refs.toastLayer;

            BindHUD(refs);
            BindScreens(refs);
            BindResults(refs);

            if (refs.levelCreator != null)
            {
                creator = refs.levelCreator;
                creator.UI = this;
            }
            else if (modalLayer != null)
            {
                creator = modalLayer.GetComponentInChildren<LevelCreatorUI>(true) ?? LevelCreatorUI.Create(modalLayer, this);
                refs.levelCreator = creator;
            }

            ApplyLayout(true);
        }

        private void BuildUI()
        {
            if (built) return;

            var canvasObj = GameObject.Find("ParkIt_Mobile_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("ParkIt_Mobile_Canvas");
                canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            uiReferences = ParkItUIBuilder.BuildHierarchy(canvasObj);
            BindUI(uiReferences);
        }

        private void Update()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            if (screen != lastScreen) ApplyLayout(false);
            HandleKeyboard();
            AnimateHUD();
        }

        private void ApplyLayout(bool force)
        {
            lastScreen = new Vector2(Screen.width, Screen.height);
            bool portrait = IsPortrait;
            scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = portrait ? 0f : 1f;
            if (!force && lastPortrait == portrait) return;
            lastPortrait = portrait;

            LayoutHUD(portrait);
            LayoutScreens(portrait);
            creator?.ApplyLayout(portrait);
            if (CameraRig.Instance != null) CameraRig.Instance.VerticalFill = portrait ? .5f : .6f;
        }

        // ---------- state ----------
        public bool AnyModalOpen => levelSelectRoot.activeSelf || helpRoot.activeSelf || victoryRoot.activeSelf || defeatRoot.activeSelf
            || (creator != null && creator.IsOpen);
        public bool TitleVisible => titleRoot.activeSelf;

        public void SetupLevelUI(LevelData level)
        {
            if (resultRoutine != null) { StopCoroutine(resultRoutine); resultRoutine = null; }
            HideResults();
            levelNumberText.text = level.difficulty == LevelDifficulty.Custom ? "CUSTOM" : $"LEVEL {level.levelIndex}";
            levelTitleText.text = level.levelTitle;
            UIKit.SetBadge(levelBadge, level.difficulty);
            targetHeadingText.text = $"Facing {level.targetDirection.ToString().ToUpper()}";
            targetArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -LevelData.DirectionToEulerY(level.targetDirection));
            UpdateFuelDisplay(level.maxFuel);
            RefreshLevelSelect();
            RefreshHUD();
        }

        public void HideResults()
        {
            if (victoryRoot != null) victoryRoot.SetActive(false);
            if (defeatRoot != null) defeatRoot.SetActive(false);
        }

        private LevelData CurrentLevel => GameManager.Instance != null ? GameManager.Instance.CurrentLevelData : null;

        // ---------- actions ----------
        public void OnClickDrive() => ExecuteMoveType(MoveType.Forward);
        public void OnClickReverse() => ExecuteMoveType(MoveType.Reverse);
        public void OnClickTurnLeft() => ExecuteMoveType(MoveType.TurnLeft);
        public void OnClickTurnRight() => ExecuteMoveType(MoveType.TurnRight);
        public void OnClickUndo() { UndoManager.Instance?.PerformUndo(); RefreshHUD(); }
        public void OnClickRestart() => GameManager.Instance?.RestartLevel();

        public void OnClickNextLevel()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.IsPlayingCustom) { HideResults(); OpenCreator(); return; }
            GameManager.Instance.NextLevel();
        }

        private void ExecuteMoveType(MoveType type)
        {
            if (carController == null || carController.IsBusy || AnyModalOpen || TitleVisible) return;
            foreach (var m in carController.CalculateLegalMoves())
            {
                if (m.moveType != type) continue;
                FlashDriveButton(type);
                carController.ExecuteMove(m);
                return;
            }
        }

        public void StartGame()
        {
            ShowTitle(false);
        }

        public void ShowTitle(bool show)
        {
            titleRoot.SetActive(show);
            hudLayer.gameObject.SetActive(!show);
            if (CameraRig.Instance != null) CameraRig.Instance.Orbiting = show;
        }

        public void OpenCreator()
        {
            levelSelectRoot.SetActive(false);
            helpRoot.SetActive(false);
            HideResults();
            creator.Open(CurrentLevel);
        }

        public void ToggleSound()
        {
            SfxPlayer.Muted = !SfxPlayer.Muted;
            if (soundIcon != null) UIKit.SetSprite(soundIcon, SfxPlayer.Muted ? "icon_sound_off" : "icon_sound_on");
            Toast(SfxPlayer.Muted ? "Sound muted" : "Sound on");
        }

        public void ToggleZen()
        {
            zenMode = !zenMode;
            dockRoot.gameObject.SetActive(!zenMode);
            zenLabel.text = zenMode ? "Show Controls" : "Hide Controls (Zen Mode)";
            UIKit.SetSprite(zenIcon, zenMode ? "icon_chevron_up" : "icon_chevron_down");
        }

        public void CycleCamera()
        {
            if (CameraRig.Instance == null) return;
            CameraRig.Instance.CycleView();
            camLabel.text = CameraRig.Instance.NextViewLabel;
        }

        // ---------- keyboard ----------
        private void HandleKeyboard()
        {
            if (creator != null && creator.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) creator.Close();
                return;
            }
            if (TitleVisible)
            {
                if (levelSelectRoot.activeSelf) { if (Input.GetKeyDown(KeyCode.Escape)) levelSelectRoot.SetActive(false); return; }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) StartGame();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (levelSelectRoot.activeSelf) levelSelectRoot.SetActive(false);
                else if (helpRoot.activeSelf) helpRoot.SetActive(false);
                return;
            }
            if (victoryRoot.activeSelf && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) { OnClickNextLevel(); return; }
            if (Input.GetKeyDown(KeyCode.U) || (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))) { OnClickUndo(); return; }
            if (Input.GetKeyDown(KeyCode.R)) { OnClickRestart(); return; }
            if (AnyModalOpen) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) OnClickDrive();
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) OnClickReverse();
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) OnClickTurnLeft();
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) OnClickTurnRight();
            else if (Input.GetKeyDown(KeyCode.Q)) ExecuteMoveType(MoveType.ShiftLeft);
            else if (Input.GetKeyDown(KeyCode.E)) ExecuteMoveType(MoveType.ShiftRight);
            else if (Input.GetKeyDown(KeyCode.C)) CycleCamera();
            else if (Input.GetKeyDown(KeyCode.Z)) ToggleZen();
        }

        // ---------- results ----------
        public void ShowVictory()
        {
            if (resultRoutine != null) StopCoroutine(resultRoutine);
            resultRoutine = StartCoroutine(ShowResultDelayed(true));
        }

        public void ShowDefeat()
        {
            if (resultRoutine != null) StopCoroutine(resultRoutine);
            resultRoutine = StartCoroutine(ShowResultDelayed(false));
        }

        private IEnumerator ShowResultDelayed(bool won)
        {
            yield return new WaitForSeconds(.45f);
            resultRoutine = null;
            if (carController == null || !carController.IsLevelComplete) yield break;
            if (won) PopulateVictory(); else PopulateDefeat();
        }

        // ---------- toast ----------
        private Coroutine toastRoutine;

        public void Toast(string message)
        {
            toastText.text = message;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toastGroup.gameObject.SetActive(true);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime * 6f) { toastGroup.alpha = t; yield return null; }
            toastGroup.alpha = 1;
            yield return new WaitForSecondsRealtime(2f);
            for (float t = 1; t > 0; t -= Time.unscaledDeltaTime * 4f) { toastGroup.alpha = t; yield return null; }
            toastGroup.gameObject.SetActive(false);
        }
    }
}
