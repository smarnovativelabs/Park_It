using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    /// <summary>Single source of truth for which moves a car can make; shared by gameplay and the solver.</summary>
    public static class MoveRules
    {
        public static List<ValidMoveData> GetLegalMoves(LevelData level, Vector2Int coord, Direction dir)
        {
            var moves = new List<ValidMoveData>();
            if (level == null) return moves;

            Vector2Int forward = LevelData.DirectionToVector(dir);
            Direction leftDir = LevelData.Turn(dir, false);
            Direction rightDir = LevelData.Turn(dir, true);
            Vector2Int left = LevelData.DirectionToVector(leftDir);
            Vector2Int right = LevelData.DirectionToVector(rightDir);

            // Forward: oil slicks keep the car sliding until it reaches dry tarmac or something blocks it.
            Vector2Int ahead = coord + forward;
            if (!level.IsBlocked(ahead))
            {
                Vector2Int dest = ahead;
                bool slipped = false;
                while (level.IsOil(dest) && !level.IsBlocked(dest + forward))
                {
                    dest += forward;
                    slipped = true;
                }
                moves.Add(new ValidMoveData { moveType = MoveType.Forward, destination = dest, resultingDirection = dir, isOilSlip = slipped });
            }

            Vector2Int back = coord - forward;
            if (!level.IsBlocked(back))
                moves.Add(new ValidMoveData { moveType = MoveType.Reverse, destination = back, resultingDirection = dir });

            if (!level.IsBlocked(coord + left))
                moves.Add(new ValidMoveData { moveType = MoveType.TurnLeft, destination = coord + left, resultingDirection = leftDir });

            if (!level.IsBlocked(coord + right))
                moves.Add(new ValidMoveData { moveType = MoveType.TurnRight, destination = coord + right, resultingDirection = rightDir });

            // Shifts nudge diagonally forward and need the tile ahead clear.
            if (!level.IsBlocked(ahead))
            {
                if (!level.IsBlocked(ahead + left))
                    moves.Add(new ValidMoveData { moveType = MoveType.ShiftLeft, destination = ahead + left, resultingDirection = dir });
                if (!level.IsBlocked(ahead + right))
                    moves.Add(new ValidMoveData { moveType = MoveType.ShiftRight, destination = ahead + right, resultingDirection = dir });
            }

            return moves;
        }
    }

    /// <summary>Breadth-first solver: fewest moves from the start to the target bay with the right heading.</summary>
    public static class LevelSolver
    {
        public struct Result
        {
            public bool solvable;
            public int moves;
            public List<MoveType> path;
        }

        public static Result Solve(LevelData level, int moveLimit = 64)
        {
            return SolveFrom(level, level != null ? level.startCoord : Vector2Int.zero,
                level != null ? level.startDirection : Direction.North, moveLimit);
        }

        public static Result SolveFrom(LevelData level, Vector2Int start, Direction heading,
            int moveLimit = 64, bool allowShifts = true)
        {
            var result = new Result { solvable = false, moves = -1, path = new List<MoveType>() };
            if (level == null || level.IsBlocked(start) || level.IsBlocked(level.targetCoord)) return result;

            var startKey = (start, heading);
            var parent = new Dictionary<(Vector2Int, Direction), ((Vector2Int, Direction) from, MoveType move)>();
            var depth = new Dictionary<(Vector2Int, Direction), int> { [startKey] = 0 };
            var queue = new Queue<(Vector2Int, Direction)>();
            queue.Enqueue(startKey);

            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                if (level.IsTarget(state.Item1, state.Item2))
                {
                    result.solvable = true;
                    result.moves = depth[state];
                    var cursor = state;
                    while (parent.TryGetValue(cursor, out var link))
                    {
                        result.path.Insert(0, link.move);
                        cursor = link.from;
                    }
                    return result;
                }
                if (depth[state] >= moveLimit) continue;

                foreach (var move in MoveRules.GetLegalMoves(level, state.Item1, state.Item2))
                {
                    if (!allowShifts && (move.moveType == MoveType.ShiftLeft || move.moveType == MoveType.ShiftRight)) continue;
                    var next = (move.destination, move.resultingDirection);
                    if (depth.ContainsKey(next)) continue;
                    depth[next] = depth[state] + 1;
                    parent[next] = (state, move.moveType);
                    queue.Enqueue(next);
                }
            }
            return result;
        }
    }
}
