using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ParkIt
{
    /// <summary>Game progress, settings and creator draft stored in persistentDataPath.</summary>
    public sealed class ProgressSave
    {
        [Serializable] public class LevelProgress
        {
            public int levelId;
            public int stars;
        }

        [Serializable] public class SaveData
        {
            public int version = 1;
            public int lastLevelIndex;
            public bool soundMuted;
            public bool tutorialCompleted;
            public int carColorIndex;
            public string creatorDraftJson = "";
            public List<LevelProgress> levels = new List<LevelProgress>();
        }

        private static ProgressSave instance;
        public static ProgressSave Instance => instance ??
            (instance = new ProgressSave(Path.Combine(Application.persistentDataPath, "progress.json")));
        public string FilePath { get; }
        public int LastLevelIndex => data.lastLevelIndex;
        public bool SoundMuted => data.soundMuted;
        public bool TutorialCompleted => data.tutorialCompleted;
        public void CompleteTutorial() { data.tutorialCompleted = true; Save(); }
        public int CarColorIndex => Mathf.Clamp(data.carColorIndex, 0, 3);
        public string CreatorDraftJson => data.creatorDraftJson ?? "";
        private SaveData data;
        private bool preserveBackup;
        private bool writable = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => instance = null;

        public ProgressSave(string filePath)
        {
            FilePath = filePath;
            if (TryRead(filePath, out data)) return;
            if (TryRead(filePath + ".bak", out data))
            {
                preserveBackup = true;
                return;
            }
            data = new SaveData();
            // Keep unreadable files intact rather than silently overwriting progress.
            writable = !File.Exists(filePath) && !File.Exists(filePath + ".bak");
            if (!writable) Debug.LogWarning("[Park It] Progress could not be read; existing save files were preserved.");
        }

        private static bool TryRead(string path, out SaveData value)
        {
            value = null;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                if (!json.Contains("\"version\"") || !json.Contains("\"levels\"")) return false;
                value = JsonUtility.FromJson<SaveData>(json);
                if (value == null || value.version != 1 || value.levels == null) return false;
                value.lastLevelIndex = Mathf.Max(0, value.lastLevelIndex);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                return false;
            }
        }

        public int GetStars(int levelId)
        {
            int best = 0;
            foreach (var level in data.levels)
                if (level != null && level.levelId == levelId) best = Mathf.Max(best, Mathf.Clamp(level.stars, 0, 3));
            return best;
        }

        public void RecordStars(int levelId, int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars <= GetStars(levelId)) return;
            data.levels.RemoveAll(level => level == null || level.levelId == levelId);
            data.levels.Add(new LevelProgress { levelId = levelId, stars = stars });
            Save();
        }

        public void SetLastLevel(int index)
        {
            data.lastLevelIndex = Mathf.Max(0, index);
            Save();
        }

        public void SetSoundMuted(bool muted)
        {
            data.soundMuted = muted;
            Save();
        }

        public void SetCarColor(int index)
        {
            data.carColorIndex = Mathf.Clamp(index, 0, 3);
            Save();
        }

        public void SetCreatorDraft(string json)
        {
            data.creatorDraftJson = json ?? "";
            Save();
        }

        public bool Save()
        {
            if (!writable) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath))
                    File.Replace(temp, FilePath, preserveBackup ? null : FilePath + ".bak");
                else File.Move(temp, FilePath);
                preserveBackup = false;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.LogWarning("[Park It] Could not save progress: " + ex.Message);
                return false;
            }
        }
    }
}
