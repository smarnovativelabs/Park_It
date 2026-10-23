#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ParkIt.Editor
{
    public class ParkItEditorTools : EditorWindow
    {
        [MenuItem("Park It/🚀 Setup & Bake Complete Scene (Same as Web)", false, 1)]
        public static void BakeCompleteScene()
        {
            ParkItBootstrapper.BuildCompleteScene();
            EditorUtility.DisplayDialog("Park It Setup", "Diorama Scene and UI successfully built and populated! Scene is ready to play with 0 pink materials.", "OK");
        }

        [MenuItem("Park It/📱 Build Android APK (1-Click)", false, 10)]
        public static void BuildAPK()
        {
            BuildAPKCommand.BuildAndroidAPK();
        }

        [MenuItem("Park It/Open Scene Setup Helper Window", false, 20)]
        public static void ShowWindow()
        {
            GetWindow<ParkItEditorTools>("Park It Studio");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Park It! — Unity 6 Mobile Studio", EditorStyles.boldLabel);
            GUILayout.Label("Ensure diorama models and mobile UI are loaded exactly as in the web version:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("🚀 Populate / Reset Scene (3D Models + UI)", GUILayout.Height(38)))
            {
                BakeCompleteScene();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("📱 Build Android APK (1-Click)", GUILayout.Height(34)))
            {
                BuildAPK();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Set Mobile Portrait Orientation (60 FPS)", GUILayout.Height(28)))
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                Application.targetFrameRate = 60;
                EditorUtility.DisplayDialog("Park It", "PlayerSettings locked to Portrait 60 FPS.", "OK");
            }
        }
    }
}
#endif
