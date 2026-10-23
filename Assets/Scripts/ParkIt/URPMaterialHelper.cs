using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParkIt
{
    public static class URPMaterialHelper
    {
        private static Shader cachedURPShader;
        private static readonly Dictionary<(Color, float, float, bool), Material> materials = new Dictionary<(Color, float, float, bool), Material>();

        public static Shader GetURPShader()
        {
            if (cachedURPShader == null) cachedURPShader = Shader.Find("Universal Render Pipeline/Lit");
            return cachedURPShader;
        }

        public static Material CreateMaterial(Color color, float smoothness = 0.5f, float metallic = 0.1f, bool transparent = false)
        {
            var key = (color, smoothness, metallic, transparent);
            if (materials.TryGetValue(key, out var existing) && existing != null) return existing;
            var shader = GetURPShader();
            if (shader == null) throw new System.InvalidOperationException("Park It requires the URP Lit shader. Run Park It/Configure URP.");
            var material = new Material(shader) { name = "ParkIt_Runtime_Material" };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (transparent)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
            materials[key] = material;
            return material;
        }
    }
}
