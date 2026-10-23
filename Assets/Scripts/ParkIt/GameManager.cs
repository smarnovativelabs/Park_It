using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Level Sequence")]
        [Tooltip("Leave empty to use the eight built-in LevelCatalog puzzles.")]
        [SerializeField] private List<LevelData> levels = new List<LevelData>();
        [SerializeField] private int currentLevelIndex = 0;

        [Header("Components")]
        [SerializeField] private CarController carController;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ParkItUIManager uiManager;

        private LevelData customLevel;

        public event Action<LevelData> OnLevelLoaded;

        public IReadOnlyList<LevelData> Levels => levels;
        public int CurrentLevelIndex => currentLevelIndex;
        // Stars persist completion. Only a consecutive run of completed puzzles
        // unlocks later entries, including when loading older save data.
        public int HighestUnlockedLevelIndex
        {
            get
            {
                int index = 0;
                while (levels != null && index < levels.Count - 1 &&
                    LevelCatalog.GetStars(levels[index].levelIndex) > 0) index++;
                return index;
            }
        }

        public bool IsLevelUnlocked(int index) => levels != null && index >= 0 &&
            index < levels.Count && index <= HighestUnlockedLevelIndex;
        /// <summary>True while a Level Creator design is being test-played.</summary>
        public bool IsPlayingCustom => customLevel != null;
        public LevelData CurrentLevelData => customLevel != null ? customLevel
            : (levels != null && currentLevelIndex >= 0 && levels.Count > currentLevelIndex) ? levels[currentLevelIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;

            if (carController == null) carController = FindFirstObjectByType<CarController>();
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<ParkItUIManager>();

            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
                if (FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            }
            if (GetComponent<SfxPlayer>() == null) gameObject.AddComponent<SfxPlayer>();

            if (levels == null || levels.Count == 0) levels = LevelCatalog.CreateLevels();
            ProgressSave.Instance.Save();
            currentLevelIndex = Mathf.Clamp(ProgressSave.Instance.LastLevelIndex, 0, HighestUnlockedLevelIndex);

            if (carController != null)
            {
                carController.OnLevelWon += HandleWin;
                carController.OnMoveStarted += m => SfxPlayer.Play(m.isOilSlip ? Sfx.Slip : Sfx.Move);
                carController.OnLevelLost += () => SfxPlayer.Play(Sfx.Lose);
            }

            LoadCurrentLevel();
        }

        private void HandleWin()
        {
            SfxPlayer.Play(Sfx.Win);
            var level = CurrentLevelData;
            if (level == null || IsPlayingCustom) return;
            LevelCatalog.RecordStars(level.levelIndex, LevelCatalog.StarsFor(carController.MovesUsed, level.optimalMoves));
        }

        public void LoadCurrentLevel()
        {
            LevelData current = CurrentLevelData;
            if (current == null) return;

            UndoManager.Instance?.ClearHistory();
            gridManager.InitializeGrid(current);
            carController.InitializeCar(current);
            uiManager?.SetupLevelUI(current);
            OnLevelLoaded?.Invoke(current);
            Debug.Log($"[Park It] Loaded {(IsPlayingCustom ? "custom level" : $"Level {currentLevelIndex + 1}")}: {current.levelTitle}");
        }

        public void LoadLevel(int index)
        {
            if (!IsLevelUnlocked(index)) return;
            customLevel = null;
            currentLevelIndex = index;
            ProgressSave.Instance.SetLastLevel(currentLevelIndex);
            LoadCurrentLevel();
        }

        public void NextLevel()
        {
            if (levels == null || levels.Count == 0) return;
            LoadLevel(IsPlayingCustom ? currentLevelIndex : (currentLevelIndex + 1) % levels.Count);
        }

        public void RestartLevel()
        {
            LoadCurrentLevel();
        }

        /// <summary>Test-play a design from the Level Creator.</summary>
        public void PlayCustomLevel(LevelData level)
        {
            customLevel = level;
            LoadCurrentLevel();
        }
    }
}
