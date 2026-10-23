#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ParkIt.Editor
{
    /// <summary>
    /// Repaints the car model's blue body paint in <see cref="CarModelConfig.bodyColor"/>, keeping decals,
    /// shading and the dark window panels. Writes a copy; the imported model files are never modified.
    /// </summary>
    public static class ParkItCarRecolor
    {
        private const string ConfigPath = "Assets/Resources/ParkItCarModel.asset";
        private const string OutputFolder = "Assets/Materials/CarPaint";

        [MenuItem("Park It/Rebuild Gameplay Car Colors", false, 41)]
        public static void RebuildGameplayColors()
        {
            var config = AssetDatabase.LoadAssetAtPath<CarModelConfig>(ConfigPath);
            var source = FindSourceMaterial(config);
            // Use the original blue image explicitly: the imported material may have been
            // reassigned to the yellow image in the Inspector.
            const string bluePath = "Assets/Car Model/1397429efcdb4373a9f3cd7ab75574c9_RGB_body.jpeg";
            const string yellowPath = "Assets/Car Model/image-1792662912762.jpg";
            if (source == null) throw new System.InvalidOperationException("Car body material is missing.");
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Materials", "CarPaint");
            config.blueBodyMaterial = CreatePaintMaterial(source, "Blue", AssetDatabase.LoadAssetAtPath<Texture2D>(bluePath));
            config.yellowBodyMaterial = CreatePaintMaterial(source, "Yellow", AssetDatabase.LoadAssetAtPath<Texture2D>(yellowPath));
            config.greenBodyMaterial = CreatePaintMaterial(source, "Green", CreatePaintTexture(bluePath, "Green", new Color(.15f, .85f, .3f)));
            config.redBodyMaterial = CreatePaintMaterial(source, "Red", CreatePaintTexture(bluePath, "Red", new Color(.95f, .12f, .16f)));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static Material CreatePaintMaterial(Material source, string colorName, Texture2D texture)
        {
            if (texture == null) throw new System.InvalidOperationException("Missing " + colorName + " car texture.");
            string path = OutputFolder + "/Car_Body_" + colorName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.CopyPropertiesFromMaterial(source);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreatePaintTexture(string sourcePath, string colorName, Color color)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                image.LoadImage(File.ReadAllBytes(sourcePath));
                var pixels = image.GetPixels32();
                Color.RGBToHSV(color, out float th, out float ts, out float tv);
                int painted = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color c = pixels[i];
                    Color.RGBToHSV(c, out float h, out float s, out float v);
                    if (h <= .5f || h >= .72f || s <= .3f || v <= .45f) continue;
                    var output = Color.HSVToRGB(th, Mathf.Clamp01(ts * Mathf.Min(1f, s / .8f)), Mathf.Clamp01(tv * Mathf.Min(1.1f, v)));
                    output.a = c.a;
                    pixels[i] = output;
                    painted++;
                }
                if (painted < pixels.Length / 10)
                    throw new System.InvalidOperationException("Source is not the blue car texture; refusing to generate " + colorName);
                image.SetPixels32(pixels);
                string path = OutputFolder + "/Car_Body_" + colorName + ".png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log("[Park It] " + colorName + ": recolored " + painted + " body pixels.");
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            finally { Object.DestroyImmediate(image); }
        }

        [MenuItem("Park It/Recolor Car Body", false, 40)]
        public static void Recolor()
        {
            var config = AssetDatabase.LoadAssetAtPath<CarModelConfig>(ConfigPath);
            if (config == null || config.model == null)
            {
                Debug.LogError("[Park It] Car model config missing: " + ConfigPath);
                return;
            }

            Material source = FindSourceMaterial(config);
            var sourceTex = source != null ? source.GetTexture("_BaseMap") as Texture2D : null;
            if (sourceTex == null)
            {
                Debug.LogError($"[Park It] Could not find the '{config.bodyMaterialName}' body texture on the car model.");
                return;
            }

            // Read the original file so compression / readability settings don't matter.
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            original.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sourceTex)));
            var pixels = original.GetPixels32();

            Color.RGBToHSV(config.bodyColor, out float th, out float ts, out float tv);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                Color.RGBToHSV(c, out float h, out float s, out float v);
                // Bright, saturated blue = body paint. Navy window panels (low value) and decals stay untouched.
                bool paint = h > .5f && h < .72f && s > .3f && v > .45f;
                if (!paint) continue;
                var outColor = Color.HSVToRGB(th, Mathf.Clamp01(ts * Mathf.Min(1f, s / .8f)), Mathf.Clamp01(tv * Mathf.Min(1.1f, v)));
                outColor.a = c.a;
                pixels[i] = outColor;
            }
            original.SetPixels32(pixels);

            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Materials", "CarPaint");
            string texPath = OutputFolder + "/Car_Body_Paint.png";
            File.WriteAllBytes(texPath, original.EncodeToPNG());
            Object.DestroyImmediate(original);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            var newTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            string matPath = OutputFolder + "/Car_Body_Paint.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else mat.CopyPropertiesFromMaterial(source);
            mat.SetTexture("_BaseMap", newTex);
            mat.SetTexture("_MainTex", newTex);
            EditorUtility.SetDirty(mat);

            config.bodyMaterial = mat;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Park It] Car body repainted #{ColorUtility.ToHtmlStringRGB(config.bodyColor)} → {matPath}");
        }

        private static Material FindSourceMaterial(CarModelConfig config)
        {
            foreach (var r in config.model.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name == config.bodyMaterialName) return m;
            return null;
        }
    }
}
#endif
