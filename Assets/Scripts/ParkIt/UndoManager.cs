using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public struct GameSnapshot
    {
        public Vector2Int Coord;
        public Direction Facing;
        public int RemainingFuel;

        public GameSnapshot(Vector2Int coord, Direction facing, int remainingFuel)
        {
            Coord = coord;
            Facing = facing;
            RemainingFuel = remainingFuel;
        }
    }

    public class UndoManager : MonoBehaviour
    {
        public static UndoManager Instance { get; private set; }

        private readonly Stack<GameSnapshot> historyStack = new Stack<GameSnapshot>();
        [SerializeField] private CarController carController;

        public bool CanUndo
        {
            get
            {
                if (carController == null) carController = FindFirstObjectByType<CarController>();
                return historyStack.Count > 0 && carController != null && !carController.IsBusy;
            }
        }
        public int UndoCount => historyStack.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (carController == null)
            {
                carController = FindFirstObjectByType<CarController>();
            }
        }

        public void RecordSnapshot(GameSnapshot snapshot)
        {
            historyStack.Push(snapshot);
        }

        public void ClearHistory()
        {
            historyStack.Clear();
        }

        public void PerformUndo()
        {
            if (!CanUndo) return;

            GameSnapshot previous = historyStack.Pop();
            carController.RestoreState(previous.Coord, previous.Facing, previous.RemainingFuel);
            SfxPlayer.Play(Sfx.Undo);
            ParkItUIManager.Instance?.HideResults();
        }
    }
}
