#if UNITY_EDITOR
using UnityEngine;

namespace ParkIt.Editor
{
    /// <summary>Renders the game camera plus the overlay UI into a PNG (works even when the Game view is unfocused).</summary>
    public static class ParkItCapture
    {
        public static string Capture(string path, int width, int height)
        {
            var cam = Camera.main;
            var canvas = GameObject.Find("ParkIt_Mobile_Canvas")?.GetComponent<Canvas>();
            var rt = new RenderTexture(width, height, 24);
            var prevMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            var prevTarget = cam.targetTexture;
            try
            {
                if (ParkIt.CameraRig.Instance != null) ParkIt.CameraRig.Instance.Snap();
                cam.targetTexture = rt;
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;
                    Canvas.ForceUpdateCanvases();
                }
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = prevTarget;
                if (canvas != null) canvas.renderMode = prevMode;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            return path;
        }
    }
}
#endif
