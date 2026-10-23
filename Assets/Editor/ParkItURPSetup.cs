using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ParkIt.Editor
{
    [InitializeOnLoad]
    public static class ParkItURPSetup
    {
        private const string PipelinePath = "Assets/Settings/ParkIt_URP.asset";
        static ParkItURPSetup() { EditorApplication.delayCall += EnsureMigration; }

        private static void EnsureMigration()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath) == null) Migrate();
        }

        [MenuItem("Park It/Configure URP")]
        public static void Migrate()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/ParkIt_Renderer.asset");
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, "Assets/Settings/ParkIt_Renderer.asset");
                }
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    pipeline.msaaSampleCount = 2;
                    pipeline.supportsHDR = false;
                    pipeline.shadowDistance = 40;
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }
                GraphicsSettings.defaultRenderPipeline = pipeline;
                int quality = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(quality, false);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader was not imported.");
                int converted = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (material == null) continue;
                    // Preserve serialized colors even if the original shader reference is missing.
                    var serialized = new SerializedObject(material);
                    var colors = serialized.FindProperty("m_SavedProperties.m_Colors");
                    Color color = Color.white;
                    for (int i = 0; i < colors.arraySize; i++)
                    {
                        var pair = colors.GetArrayElementAtIndex(i);
                        if (pair.FindPropertyRelative("first").stringValue == "_BaseColor")
                            color = pair.FindPropertyRelative("second").colorValue;
                    }
                    material.shader = shader;
                    material.SetColor("_BaseColor", color);
                    EditorUtility.SetDirty(material);
                    converted++;
                }
                // Runtime-generated materials need the shader retained in player builds.
                var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
                var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
                bool found = false;
                for (int i = 0; i < shaders.arraySize; i++) found |= shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                if (!found) { shaders.InsertArrayElementAtIndex(shaders.arraySize); shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader; }
                graphics.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ParkIt-URP-migration.txt", $"URP configured for {QualitySettings.names.Length} quality levels. Materials repaired: {converted}.\n");
                Debug.Log("[Park It] URP migration complete.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
