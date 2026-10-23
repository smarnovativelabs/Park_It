using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum MoveType
    {
        Forward,
        Reverse,
        TurnLeft,
        TurnRight,
        ShiftLeft,
        ShiftRight
    }

    public enum LevelDifficulty
    {
        Beginner,
        Intermediate,
        Expert,
        Master,
        Custom
    }

    [CreateAssetMenu(fileName = "LevelData_01", menuName = "Park It/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        public string levelTitle = "First Gear";
        [TextArea(2, 4)]
        public string objectiveHint = "Drive straight into the marked green parking bay.";
        public int levelIndex = 1;
        public LevelDifficulty difficulty = LevelDifficulty.Beginner;

        [Header("Grid Dimensions")]
        [Range(3, 12)] public int gridWidth = 5;
        [Range(3, 12)] public int gridHeight = 5;

        [Header("Starting State")]
        public Vector2Int startCoord = new Vector2Int(2, 0);
        public Direction startDirection = Direction.North;

        [Header("Target Parking Slot")]
        public Vector2Int targetCoord = new Vector2Int(2, 4);
        public Direction targetDirection = Direction.North;

        [Header("Fuel & Challenges")]
        [Tooltip("Maximum moves allowed before running out of fuel")]
        public int maxFuel = 8;
        [Tooltip("Target moves to achieve 3 stars")]
        public int optimalMoves = 4;

        [Header("Obstacles & Hazards")]
        [Tooltip("Cells occupied by walls, planters, or curbs")]
        public List<Vector2Int> obstacles = new List<Vector2Int>();

        [Tooltip("Cells with oil slicks causing the car to slip 1 extra tile forward")]
        public List<Vector2Int> oilSpills = new List<Vector2Int>();

        public bool IsWithinBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < gridWidth && coord.y >= 0 && coord.y < gridHeight;
        }

        public bool IsBlocked(Vector2Int coord)
        {
            if (!IsWithinBounds(coord)) return true;
            return obstacles != null && obstacles.Contains(coord);
        }

        public bool IsOil(Vector2Int coord)
        {
            return oilSpills != null && oilSpills.Contains(coord);
        }

        public bool IsTarget(Vector2Int coord, Direction dir)
        {
            return coord == targetCoord && dir == targetDirection;
        }

        public LevelData Clone()
        {
            var copy = CreateInstance<LevelData>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), copy);
            copy.obstacles = new List<Vector2Int>(obstacles ?? new List<Vector2Int>());
            copy.oilSpills = new List<Vector2Int>(oilSpills ?? new List<Vector2Int>());
            return copy;
        }

        public static Vector2Int DirectionToVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return new Vector2Int(0, 1);
                case Direction.East:  return new Vector2Int(1, 0);
                case Direction.South: return new Vector2Int(0, -1);
                case Direction.West:  return new Vector2Int(-1, 0);
                default:              return Vector2Int.up;
            }
        }

        public static Direction Turn(Direction current, bool clockwise)
        {
            int val = (int)current;
            return clockwise ? (Direction)((val + 1) % 4) : (Direction)((val + 3) % 4);
        }

        public static float DirectionToEulerY(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return 0f;
                case Direction.East:  return 90f;
                case Direction.South: return 180f;
                case Direction.West:  return 270f;
                default:              return 0f;
            }
        }
    }
}
