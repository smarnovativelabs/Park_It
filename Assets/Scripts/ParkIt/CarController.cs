using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public struct ValidMoveData
    {
        public MoveType moveType;
        public Vector2Int destination;
        public Direction resultingDirection;
        public bool isOilSlip;
    }

    public class CarController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private Vector2Int currentCoord;
        [SerializeField] private Direction currentDirection;
        [SerializeField] private int remainingFuel;

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private CarVisuals carVisuals;

        public event Action<int> OnFuelChanged;
        public event Action OnLevelWon;
        public event Action OnLevelLost;
        public event Action OnStateChanged;
        public event Action<ValidMoveData> OnMoveStarted;

        public int MovesUsed => gridManager != null && gridManager.CurrentLevel != null ? gridManager.CurrentLevel.maxFuel - remainingFuel : 0;
        public Vector2Int CurrentCoord => currentCoord;
        public Direction CurrentDirection => currentDirection;
        public int RemainingFuel => remainingFuel;
        public bool IsBusy { get; private set; }
        public bool IsLevelComplete { get; private set; }

        private void Awake()
        {
            if (carVisuals == null) carVisuals = GetComponent<CarVisuals>();
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        }

        public void InitializeCar(LevelData level)
        {
            SfxPlayer.StopAll();
            if (carVisuals != null) carVisuals.CancelMove();
            IsLevelComplete = false;
            currentCoord = level.startCoord;
            currentDirection = level.startDirection;
            remainingFuel = level.maxFuel;
            IsBusy = false;

            transform.position = gridManager.CoordToWorldPosition(currentCoord);
            transform.rotation = Quaternion.Euler(0, LevelData.DirectionToEulerY(currentDirection), 0);

            if (carVisuals != null)
            {
                carVisuals.PlayLanding();
            }

            OnFuelChanged?.Invoke(remainingFuel);
            UpdateCandidateMoves();
        }

        public void RestoreState(Vector2Int coord, Direction direction, int fuel)
        {
            SfxPlayer.StopAll();
            if (carVisuals != null) carVisuals.ClearColorSparkles();
            if (carVisuals != null) carVisuals.CancelMove();
            IsBusy = false;
            IsLevelComplete = false;
            currentCoord = coord;
            currentDirection = direction;
            remainingFuel = fuel;

            transform.position = gridManager.CoordToWorldPosition(currentCoord);
            transform.rotation = Quaternion.Euler(0, LevelData.DirectionToEulerY(currentDirection), 0);

            OnFuelChanged?.Invoke(remainingFuel);
            UpdateCandidateMoves();
        }

        public List<ValidMoveData> CalculateLegalMoves()
        {
            var moves = new List<ValidMoveData>();
            if (remainingFuel <= 0 || IsBusy || IsLevelComplete || gridManager == null) return moves;

            return MoveRules.GetLegalMoves(gridManager.CurrentLevel, currentCoord, currentDirection);
        }

        public void UpdateCandidateMoves()
        {
            if (IsBusy)
            {
                gridManager.ClearGhostNodes();
                return;
            }

            var legalMoves = CalculateLegalMoves();
            gridManager.DisplayGhostNodes(legalMoves, ExecuteMove);
            OnStateChanged?.Invoke();
        }

        public void ExecuteMove(ValidMoveData move)
        {
            var legalMoves = CalculateLegalMoves();
            if (!legalMoves.Exists(candidate => candidate.moveType == move.moveType
                && candidate.destination == move.destination
                && candidate.resultingDirection == move.resultingDirection
                && candidate.isOilSlip == move.isOilSlip)) return;

            if (UndoManager.Instance != null)
            {
                UndoManager.Instance.RecordSnapshot(new GameSnapshot(currentCoord, currentDirection, remainingFuel));
            }

            IsBusy = true;
            gridManager.ClearGhostNodes();

            remainingFuel--;
            OnFuelChanged?.Invoke(remainingFuel);
            OnMoveStarted?.Invoke(move);
            OnStateChanged?.Invoke();

            currentCoord = move.destination;
            currentDirection = move.resultingDirection;

            Vector3 worldDest = gridManager.CoordToWorldPosition(currentCoord);
            float targetEulerY = LevelData.DirectionToEulerY(currentDirection);

            carVisuals.AnimateMove(worldDest, targetEulerY, move.moveType, move.isOilSlip, () =>
            {
                IsBusy = false;
                CheckWinLoseConditions();
            });
        }

        private void CheckWinLoseConditions()
        {
            LevelData level = gridManager.CurrentLevel;
            if (level == null) return;

            if (level.IsTarget(currentCoord, currentDirection))
            {
                IsLevelComplete = true;
                carVisuals?.PlayVictory();
                OnStateChanged?.Invoke();
                OnLevelWon?.Invoke();
            }
            else if (remainingFuel <= 0)
            {
                IsLevelComplete = true;
                carVisuals?.PlayGameOver();
                OnStateChanged?.Invoke();
                OnLevelLost?.Invoke();
            }
            else
            {
                UpdateCandidateMoves();
            }
        }
    }
}
