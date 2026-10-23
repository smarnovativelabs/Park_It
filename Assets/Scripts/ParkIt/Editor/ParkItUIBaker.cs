#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParkIt.Editor
{
    public static class ParkItUIBaker
    {
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string PrefabPath = "Assets/Prefabs/UI/ParkIt_Mobile_Canvas.prefab";

        [MenuItem("Park It/UI/Bake Canvas to Scene & Prefab", false, 15)]
        public static void BakeCanvasMenu()
        {
            BakeCanvas(true);
        }

        public static void BakeCanvas(bool showDialog = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            // 1. Locate or create ParkIt_Mobile_Canvas in the active scene
            GameObject canvasObj = GameObject.Find("ParkIt_Mobile_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("ParkIt_Mobile_Canvas");
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create ParkIt_Mobile_Canvas");
            }

            // 2. Build full visual hierarchy
            ParkItUIReferences refs = ParkItUIBuilder.BuildHierarchy(canvasObj);

            // 3. Connect to UI Manager on controllers
            ParkItUIManager uiMgr = Object.FindFirstObjectByType<ParkItUIManager>();
            if (uiMgr != null)
            {
                var so = new SerializedObject(uiMgr);
                var prop = so.FindProperty("uiReferences");
                if (prop != null)
                {
                    prop.objectReferenceValue = refs;
                    so.ApplyModifiedProperties();
                }
            }

            // 4. Save as Prefab Asset and connect scene instance
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[ParkIt UI Baker] Successfully baked UI Canvas Prefab at: {PrefabPath}");

            // 5. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("UI Prefab Baked",
                    "Park It Mobile Canvas successfully built, saved to Prefab (Assets/Prefabs/UI/ParkIt_Mobile_Canvas.prefab), and linked in the active scene!\n\nAll dynamic responsive math remains active at runtime.",
                    "OK");
            }
        }
    }
}
#endif
