#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt.Editor
{
    // Explicit file requests only; never runs during an ordinary project session.
    [InitializeOnLoad]
    public static class CandyVisualReview
    {
        private const string Request = "Temp/CandyReview.request";
        private static double ready;
        private static string output;
        private static double statusAt;
        static CandyVisualReview() { EditorApplication.update += Tick; }
        public static void BatchStart()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ParkIt_MobileMain.unity");
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kind = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = System.Activator.CreateInstance(sizeType, System.Enum.ToObject(kind, 1), 1080, 1920, "Candy Review");
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex").SetValue(view, count - 1);
            File.WriteAllText(Request, System.Environment.GetEnvironmentVariable("PARKIT_REVIEW_OUTPUT"));
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(Request) && EditorApplication.timeSinceStartup > statusAt)
            {
                statusAt = EditorApplication.timeSinceStartup + 2;
                File.WriteAllText("Temp/CandyReview.status", "playing=" + EditorApplication.isPlaying + "; UI=" + ParkItUIManager.Instance + "; Rig=" + CameraRig.Instance);
            }
            if (output == null && File.Exists(Request))
            {
                if (!EditorApplication.isPlaying) { EditorApplication.isPlaying = true; return; }
                if (ParkItUIManager.Instance == null || CameraRig.Instance == null) return;
                output = File.ReadAllText(Request).Trim();
                File.Delete(Request);
                ParkItUIManager.Instance.ShowTitle(false);
                CameraRig.Instance.Orbiting = false;
                CameraRig.Instance.Snap();
                ready = EditorApplication.timeSinceStartup + 2;
            }
            if (output == null || EditorApplication.timeSinceStartup < ready) return;
            var path = output; output = null;
            try { Capture(path); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (System.Exception e) { File.WriteAllText(path + ".error.txt", e.ToString()); }
        }

        public static void Capture(string path)
        {
            var camera = Camera.main;
            var canvas = GameObject.Find("ParkIt_Mobile_Canvas").GetComponent<Canvas>();
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; var distance = canvas.planeDistance;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var rt = new RenderTexture(1080, 1920, 24);
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                CameraRig.Instance.Snap();
                camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                File.WriteAllText(path + ".txt", "Unity Camera.Render with live gameplay and Canvas at 1080x1920.\n" +
                    "Canvas scale=" + canvas.scaleFactor + "; board=" + GameManager.Instance.CurrentLevelData.levelTitle);
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.planeDistance = distance;
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                Object.DestroyImmediate(texture); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
#endif
