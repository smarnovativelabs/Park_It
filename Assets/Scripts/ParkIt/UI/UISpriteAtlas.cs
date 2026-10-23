using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    [Serializable]
    public class UISheetEntry
    {
        public string name;
        public int x, y, w, h;
        public int l, b, r, t;
    }

    [Serializable]
    public class UISheetTable
    {
        public string texture;
        public int width, height;
        public UISheetEntry[] sprites;
    }

    /// <summary>
    /// Every UI sprite comes from the single ParkIt_UI_Sheet spritesheet.
    /// Uses the editor-sliced sprites when present, otherwise cuts them at runtime from the JSON table.
    /// </summary>
    public static class UISpriteAtlas
    {
        private const string SheetResource = "Sprites/UI/ParkIt_UI_Sheet";
        private static Dictionary<string, Sprite> sprites;

        public static Sprite Get(string name)
        {
            if (sprites == null) Load();
            if (sprites.TryGetValue(name, out var sprite)) return sprite;
            Debug.LogWarning($"[Park It] Sprite '{name}' missing from UI spritesheet.");
            return null;
        }

        private static void Load()
        {
            sprites = new Dictionary<string, Sprite>();
            foreach (var sprite in Resources.LoadAll<Sprite>(SheetResource)) sprites[sprite.name] = sprite;

            var tableAsset = Resources.Load<TextAsset>(SheetResource);
            if (tableAsset == null) return;
            var table = JsonUtility.FromJson<UISheetTable>(tableAsset.text);
            Texture2D texture = null;
            foreach (var entry in table.sprites)
            {
                if (sprites.ContainsKey(entry.name)) continue;
                if (texture == null) texture = Resources.Load<Texture2D>(SheetResource);
                if (texture == null) return;
                var sprite = Sprite.Create(texture, new Rect(entry.x, entry.y, entry.w, entry.h), new Vector2(.5f, .5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(entry.l, entry.b, entry.r, entry.t));
                sprite.name = entry.name;
                sprites[entry.name] = sprite;
            }
        }
    }
}
