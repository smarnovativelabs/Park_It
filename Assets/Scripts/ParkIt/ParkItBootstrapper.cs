using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>
    /// Zero-Setup Scene Bootstrapper for Unity 6.
    /// Runs automatically on Scene Load if components are missing,
    /// generating the entire diorama, tabletop, toy car, lights, camera, and UI Canvas with sprites!
    /// </summary>
    public class ParkItBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrapScene()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;

            Debug.Log("[Park It] Auto-bootstrapping complete diorama scene for Unity 6...");
            BuildCompleteScene();
        }

        public static void BuildCompleteScene()
        {
            // 1. Setup Camera (Orthographic Isometric 45°)
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.transform.position = new Vector3(16f, 20f, 16f);
            cam.transform.LookAt(Vector3.zero);
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 2. Setup Directional Key Light & Sky Fill Light
            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject keyObj = new GameObject("Directional_Key_Light");
                Light key = keyObj.AddComponent<Light>();
                key.type = LightType.Directional;
                key.color = new Color(1f, .97f, .92f);
                key.intensity = 1.25f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = .48f;
                key.shadowBias = .025f;
                key.shadowNormalBias = .08f;
                keyObj.transform.position = new Vector3(14f, 25f, 12f);
                keyObj.transform.rotation = Quaternion.Euler(52f, -35f, 0);

                GameObject fillObj = new GameObject("Directional_Fill_Light");
                Light fill = fillObj.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(.86f, .91f, 1f);
                fill.intensity = .40f;
                fill.shadows = LightShadows.None;
                fillObj.transform.position = new Vector3(-14f, 15f, -14f);
                fillObj.transform.rotation = Quaternion.Euler(40f, 145f, 0);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.46f, .52f, .64f);
            RenderSettings.ambientEquatorColor = new Color(.31f, .32f, .43f);
            RenderSettings.ambientGroundColor = new Color(.19f, .14f, .26f);

            // 3. Setup Core Managers
            GameObject controllerObj = GameObject.Find("GameController");
            if (controllerObj == null) controllerObj = new GameObject("GameController");

            GridManager gm = controllerObj.GetComponent<GridManager>() ?? controllerObj.AddComponent<GridManager>();
            UndoManager um = controllerObj.GetComponent<UndoManager>() ?? controllerObj.AddComponent<UndoManager>();
            TouchInputHandler tih = controllerObj.GetComponent<TouchInputHandler>() ?? controllerObj.AddComponent<TouchInputHandler>();

            // 4. Setup Toy Car
            GameObject carObj = GameObject.Find("Toy_Car");
            if (carObj == null) carObj = new GameObject("Toy_Car");
            CarVisuals cv = carObj.GetComponent<CarVisuals>() ?? carObj.AddComponent<CarVisuals>();
            CarController cc = carObj.GetComponent<CarController>() ?? carObj.AddComponent<CarController>();
            if (!Application.isPlaying)
            {
                var carConfig = Resources.Load<CarModelConfig>(CarModelConfig.ResourcePath);
                if (carConfig != null && carConfig.model != null) cv.BuildModelCar(carConfig);
                else cv.BuildProceduralToyCar();
            }

            // 5. Setup UI Canvas
            SetupMobileHUD(controllerObj);

            // 6. Setup Game Manager
            GameManager gmMgr = controllerObj.GetComponent<GameManager>() ?? controllerObj.AddComponent<GameManager>();
        }

        private static void SetupMobileHUD(GameObject controllerObj)
        {
            GameObject canvasObj = GameObject.Find("ParkIt_Mobile_Canvas");
            if (canvasObj == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/ParkIt_Mobile_Canvas");
#if UNITY_EDITOR
                if (prefab == null) prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ParkIt_Mobile_Canvas.prefab");
#endif
                if (prefab != null)
                {
                    canvasObj = Instantiate(prefab);
                    canvasObj.name = "ParkIt_Mobile_Canvas";
                }
                else
                {
                    canvasObj = new GameObject("ParkIt_Mobile_Canvas");
                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            ParkItUIManager ui = controllerObj.GetComponent<ParkItUIManager>() ?? controllerObj.AddComponent<ParkItUIManager>();
            ui.LoadAndApplySprites();
        }

        public static List<LevelData> GenerateDefaultLevels() => LevelCatalog.CreateLevels();
    }
}
