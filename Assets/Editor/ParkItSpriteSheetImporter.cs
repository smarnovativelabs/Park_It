#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ParkIt.Editor
{
    /// <summary>
    /// Slices Assets/Resources/Sprites/UI/ParkIt_UI_Sheet.png into named (9-sliced) sprites using the
    /// JSON table written by Tools/SpriteSheet/Generate-UISpritesheet.ps1.
    /// </summary>
    public class ParkItSpriteSheetImporter : AssetPostprocessor
    {
        public const string SheetPath = "Assets/Resources/Sprites/UI/ParkIt_UI_Sheet.png";
        public const string TablePath = "Assets/Resources/Sprites/UI/ParkIt_UI_Sheet.json";

        private void OnPreprocessTexture()
        {
            if (assetPath != SheetPath) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (path == TablePath || path == SheetPath)
                {
                    EditorApplication.delayCall -= SliceIfNeeded;
                    EditorApplication.delayCall += SliceIfNeeded;
                    return;
                }
            }
        }

        private static void SliceIfNeeded()
        {
            var table = LoadTable();
            if (table == null) return;
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null) return;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            if (Matches(provider.GetSpriteRects(), table)) return;
            Slice(importer, provider, table);
        }

        [MenuItem("Park It/Re-slice UI Spritesheet", false, 30)]
        public static void ForceSlice()
        {
            var table = LoadTable();
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (table == null || importer == null)
            {
                Debug.LogError("[Park It] UI spritesheet or slice table missing. Run Tools/SpriteSheet/Generate-UISpritesheet.ps1.");
                return;
            }
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Slice(importer, provider, table);
        }

        private static void Slice(TextureImporter importer, ISpriteEditorDataProvider provider, UISheetTable table)
        {
            var rects = new List<SpriteRect>();
            var ids = new List<SpriteNameFileIdPair>();
            foreach (var s in table.sprites)
            {
                var id = StableGuid(s.name);
                rects.Add(new SpriteRect
                {
                    name = s.name,
                    rect = new Rect(s.x, s.y, s.w, s.h),
                    border = new Vector4(s.l, s.b, s.r, s.t),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f),
                    spriteID = id
                });
                ids.Add(new SpriteNameFileIdPair(s.name, id));
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(ids);
            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log($"[Park It] UI spritesheet sliced into {rects.Count} sprites.");
        }

        private static bool Matches(SpriteRect[] existing, UISheetTable table)
        {
            if (existing == null || existing.Length != table.sprites.Length) return false;
            var byName = new Dictionary<string, SpriteRect>();
            foreach (var r in existing) byName[r.name] = r;
            foreach (var s in table.sprites)
            {
                if (!byName.TryGetValue(s.name, out var r)) return false;
                if (r.rect != new Rect(s.x, s.y, s.w, s.h) || r.border != new Vector4(s.l, s.b, s.r, s.t)) return false;
            }
            return true;
        }

        private static UISheetTable LoadTable()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath);
            return text == null ? null : JsonUtility.FromJson<UISheetTable>(text.text);
        }

        // Same name -> same sprite ID, so references survive regeneration.
        private static GUID StableGuid(string name)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("ParkItUI/" + name));
                var sb = new StringBuilder();
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return new GUID(sb.ToString());
            }
        }
    }
}
#endif
