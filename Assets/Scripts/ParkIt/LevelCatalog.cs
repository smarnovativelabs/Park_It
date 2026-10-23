using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    /// <summary>The twenty built-in puzzles. Optimal move counts come from the solver so they never drift.</summary>
    public static class LevelCatalog
    {
        public static List<LevelData> CreateLevels()
        {
            var list = new List<LevelData>
            {
                Make(1, "First Gear", "Straight pull-in to the marked bay", LevelDifficulty.Beginner, 5, 5,
                    new Vector2Int(2, 0), Direction.North, new Vector2Int(2, 4), Direction.North, 8,
                    new[] { V(1, 2), V(3, 2) }),

                Make(2, "Reverse Bay", "Back into the spot facing South", LevelDifficulty.Beginner, 5, 5,
                    new Vector2Int(1, 0), Direction.East, new Vector2Int(3, 4), Direction.South, 10,
                    new[] { V(2, 4), V(4, 4), V(2, 2), V(4, 1) }),

                Make(3, "Right Angle Alley", "Turn 90 degrees to face East", LevelDifficulty.Intermediate, 5, 5,
                    new Vector2Int(0, 0), Direction.North, new Vector2Int(4, 2), Direction.East, 9,
                    new[] { V(1, 1), V(1, 2), V(1, 3), V(3, 3) }),

                Make(4, "Slick Slide", "Glide across the oil slick to save fuel", LevelDifficulty.Intermediate, 6, 5,
                    new Vector2Int(0, 2), Direction.East, new Vector2Int(5, 2), Direction.East, 5,
                    new[] { V(2, 1), V(2, 3), V(4, 1), V(4, 3) },
                    new[] { V(2, 2), V(3, 2) }),

                Make(5, "Three-Point Turn", "Flip heading in a cramped toy alley", LevelDifficulty.Intermediate, 5, 5,
                    new Vector2Int(2, 1), Direction.North, new Vector2Int(2, 1), Direction.South, 9,
                    new[] { V(1, 0), V(3, 0), V(1, 2), V(3, 2), V(2, 4) }),

                Make(6, "Parallel Parking Dilemma", "Squeeze between barricades into slot", LevelDifficulty.Expert, 6, 6,
                    new Vector2Int(0, 0), Direction.North, new Vector2Int(5, 3), Direction.North, 12,
                    new[] { V(5, 2), V(5, 4), V(3, 2), V(3, 3), V(1, 4), V(2, 1) }),

                Make(7, "The Skidway Shortcut", "Chained oil slides with obstacle chicanes", LevelDifficulty.Expert, 7, 6,
                    new Vector2Int(0, 0), Direction.East, new Vector2Int(6, 5), Direction.North, 10,
                    new[] { V(1, 1), V(3, 1), V(4, 1), V(5, 3), V(4, 4), V(2, 3) },
                    new[] { V(2, 0), V(3, 0), V(6, 2), V(6, 3) }),

                Make(8, "Master Valet Gauntlet", "Precision maneuvering under strict fuel limit", LevelDifficulty.Master, 7, 7,
                    new Vector2Int(0, 0), Direction.North, new Vector2Int(6, 6), Direction.South, 13,
                    new[] { V(1, 1), V(1, 2), V(2, 4), V(3, 1), V(4, 3), V(5, 5), V(5, 1), V(6, 4) },
                    new[] { V(0, 3), V(3, 3), V(3, 4) }),

                Make(9, "Candy Chicane", "Weave through the staggered gates", LevelDifficulty.Expert, 6, 6,
                    V(0, 0), Direction.North, V(5, 5), Direction.East, 11,
                    new[] { V(1, 1), V(2, 1), V(3, 1), V(4, 1), V(1, 3), V(2, 3), V(3, 3), V(4, 3), V(5, 3) }),

                Make(10, "Frosted Runway", "Ride the slick lane, then line up the bay", LevelDifficulty.Expert, 6, 7,
                    V(0, 0), Direction.North, V(5, 6), Direction.North, 9,
                    new[] { V(1, 1), V(1, 2), V(1, 3), V(2, 4), V(3, 4), V(4, 4), V(4, 6) },
                    new[] { V(0, 1), V(0, 2), V(0, 3), V(3, 5) }),

                Make(11, "Back-In Boutique", "Approach the pocket and reverse in", LevelDifficulty.Expert, 6, 6,
                    V(0, 0), Direction.East, V(5, 5), Direction.South, 12,
                    new[] { V(4, 5), V(2, 0), V(2, 1), V(2, 3), V(2, 4), V(4, 2), V(4, 3) }),

                Make(12, "Twisted Ribbon", "Find the openings in three cross lanes", LevelDifficulty.Expert, 7, 7,
                    V(0, 0), Direction.East, V(6, 6), Direction.North, 19,
                    new[] { V(0, 1), V(1, 1), V(2, 1), V(3, 1), V(4, 1),
                        V(2, 3), V(3, 3), V(4, 3), V(5, 3), V(6, 3),
                        V(0, 5), V(1, 5), V(2, 5), V(3, 5), V(4, 5) }),

                Make(13, "Lemon Loop", "Circle the island and reverse into the bay", LevelDifficulty.Expert, 6, 6,
                    V(2, 0), Direction.East, V(3, 5), Direction.South, 11,
                    new[] { V(2, 1), V(3, 1), V(2, 2), V(3, 2), V(2, 3), V(3, 3) }),

                Make(14, "Double Dip", "Link two slides around the central blocks", LevelDifficulty.Expert, 7, 6,
                    V(0, 0), Direction.East, V(6, 5), Direction.West, 11,
                    new[] { V(1, 1), V(2, 1), V(3, 1), V(4, 2), V(4, 3), V(2, 4), V(3, 4) },
                    new[] { V(1, 0), V(2, 0), V(3, 0), V(6, 1), V(6, 2), V(6, 3) }),

                Make(15, "Sugar Switchbacks", "Change heading at each narrow opening", LevelDifficulty.Master, 7, 7,
                    V(0, 0), Direction.North, V(6, 6), Direction.East, 19,
                    new[] { V(1, 0), V(1, 1), V(1, 2), V(1, 3), V(1, 4),
                        V(3, 2), V(3, 3), V(3, 4), V(3, 5), V(3, 6),
                        V(5, 0), V(5, 1), V(5, 2), V(5, 3), V(5, 4) }),

                Make(16, "Slippery Spiral", "Slide along the rim, then turn inward", LevelDifficulty.Master, 7, 7,
                    V(0, 0), Direction.North, V(3, 3), Direction.South, 9,
                    new[] { V(1, 1), V(1, 2), V(1, 3), V(1, 4), V(2, 1), V(3, 1),
                        V(4, 1), V(5, 1), V(5, 2), V(5, 3), V(3, 4), V(4, 4) },
                    new[] { V(0, 2), V(0, 3), V(0, 4), V(2, 6), V(3, 6) }),

                Make(17, "Valet Vault", "Enter the enclosed bay facing West", LevelDifficulty.Master, 7, 6,
                    V(0, 0), Direction.East, V(5, 4), Direction.West, 10,
                    new[] { V(4, 3), V(5, 3), V(4, 4), V(4, 5), V(5, 5),
                        V(2, 1), V(2, 2), V(2, 3), V(0, 4) }),

                Make(18, "Neon Slalom", "Trade long slides for precise turns", LevelDifficulty.Master, 7, 7,
                    V(0, 0), Direction.North, V(6, 6), Direction.South, 11,
                    new[] { V(1, 1), V(2, 1), V(3, 1), V(4, 1), V(2, 3), V(3, 3),
                        V(4, 3), V(5, 3), V(6, 3), V(0, 5), V(1, 5), V(2, 5), V(3, 5), V(4, 5) },
                    new[] { V(0, 1), V(0, 2), V(2, 4), V(3, 4), V(5, 5) }),

                Make(19, "Royal Reverse", "Cross the maze and back into the crown bay", LevelDifficulty.Master, 7, 7,
                    V(0, 0), Direction.East, V(6, 6), Direction.South, 20,
                    new[] { V(1, 0), V(1, 1), V(1, 2), V(1, 3), V(1, 4),
                        V(3, 2), V(3, 3), V(3, 4), V(3, 5), V(3, 6),
                        V(5, 0), V(5, 1), V(5, 2), V(5, 3), V(5, 4), V(5, 6) }),

                Make(20, "Candy Grand Finale", "Master the gates, slides and final heading", LevelDifficulty.Master, 7, 7,
                    V(0, 0), Direction.North, V(6, 6), Direction.West, 16,
                    new[] { V(1, 0), V(1, 1), V(1, 2), V(1, 3), V(1, 4),
                        V(3, 2), V(3, 3), V(3, 4), V(3, 5), V(3, 6),
                        V(5, 0), V(5, 1), V(5, 2), V(5, 3), V(5, 4) },
                    new[] { V(0, 1), V(0, 2), V(2, 3), V(2, 2), V(4, 2), V(4, 3), V(6, 4) }),
            };
            return list;
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        private static LevelData Make(int index, string title, string hint, LevelDifficulty difficulty, int width, int height,
            Vector2Int start, Direction startDir, Vector2Int target, Direction targetDir, int fuel,
            Vector2Int[] obstacles, Vector2Int[] oil = null)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.name = $"Level_{index:00}_{title}";
            level.levelIndex = index;
            level.levelTitle = title;
            level.objectiveHint = hint;
            level.difficulty = difficulty;
            level.gridWidth = width;
            level.gridHeight = height;
            level.startCoord = start;
            level.startDirection = startDir;
            level.targetCoord = target;
            level.targetDirection = targetDir;
            level.maxFuel = fuel;
            level.obstacles = new List<Vector2Int>(obstacles);
            level.oilSpills = oil != null ? new List<Vector2Int>(oil) : new List<Vector2Int>();
            var solution = LevelSolver.Solve(level);
            level.optimalMoves = solution.solvable ? solution.moves : fuel;
            if (!solution.solvable || solution.moves > fuel)
                Debug.LogWarning($"[Park It] Level {index} '{title}' cannot be solved within {fuel} fuel.");
            return level;
        }

        // ---------- progress ----------
        public static int GetStars(int levelIndex) => ProgressSave.Instance.GetStars(levelIndex);

        public static void RecordStars(int levelIndex, int stars)
        {
            ProgressSave.Instance.RecordStars(levelIndex, stars);
        }

        public static int StarsFor(int movesUsed, int optimal)
        {
            if (movesUsed <= optimal) return 3;
            if (movesUsed <= optimal + 2) return 2;
            return 1;
        }
    }
}
