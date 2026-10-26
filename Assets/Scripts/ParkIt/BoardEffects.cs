using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public sealed class BoardEffects : MonoBehaviour
    {
        private struct Arrival { public Transform node; public Vector3 rest; public float delay; }
        private readonly List<Arrival> arrivals = new List<Arrival>();
        private Transform arrow, glint;
        private Transform shimmer;
        private Material shimmerMaterial;
        private Vector3 arrowScale, glintRest;
        private float elapsed;
        public bool IsArriving => arrivals.Count > 0;
        public void Begin(IEnumerable<GameObject> props, int width, int height, float tileSize)
        {
            Finish(); elapsed = 0f;
            foreach (var prop in props)
            {
                if (prop == null || prop.name == "Tabletop_Plinth") continue;
                var node = prop.transform;
                float x = node.position.x / tileSize + (width - 1) * .5f;
                float y = node.position.z / tileSize + (height - 1) * .5f;
                float delay = (x + y) / Mathf.Max(1, width + height - 2) * .20f;
                if (!prop.name.StartsWith("Tile_")) delay += .08f;
                arrivals.Add(new Arrival { node = node, rest = node.localPosition, delay = delay });
                node.localPosition += Vector3.down * .35f;
            }
        }
        public void BindBay(Transform target)
        {
            arrow = target.Find("Parking_Facing_Indicator"); glint = target.Find("Bay_Glint");
            if (arrow != null) arrowScale = arrow.localScale;
            if (glint != null) glintRest = glint.localPosition;
            if (shimmerMaterial != null) Destroy(shimmerMaterial);
            shimmerMaterial = new Material(Resources.Load<Shader>("ParkItEffects"));
            shimmerMaterial.SetColor("_BaseColor", new Color(1f, 1f, .85f, .15f));
            shimmer = DioramaMeshes.RoundedBlock("Bay_Shimmer", target, new Vector3(-.6f, .19f, 0),
                new Vector3(.12f, .005f, 1.25f), .02f, .002f, shimmerMaterial).transform;
        }
        public void Finish()
        {
            foreach (var item in arrivals) if (item.node != null) item.node.localPosition = item.rest;
            arrivals.Clear();
        }
        private void Update()
        {
            elapsed += Time.deltaTime;
            foreach (var item in arrivals)
            {
                if (item.node == null) continue;
                float t = Mathf.Clamp01((elapsed - item.delay) / .23f);
                float offset = -.35f * Mathf.Pow(1f - t, 3f) + .025f * Mathf.Sin(t * Mathf.PI);
                item.node.localPosition = item.rest + Vector3.up * offset;
            }
            if (elapsed >= .52f) Finish();
            if (arrow != null) arrow.localScale = arrowScale * (1f + .055f * Mathf.Sin(elapsed * 2.4f));
            if (glint != null)
            {
                float phase = elapsed % 4f;
                glint.localPosition = glintRest + Vector3.right * (phase < .8f ? Mathf.Sin(phase / .8f * Mathf.PI) * .75f : 0f);
            }
            if (shimmer != null)
            {
                float phase = elapsed % 4f;
                shimmer.gameObject.SetActive(phase < .8f);
                shimmer.localPosition = new Vector3(Mathf.Lerp(-.65f, .65f, phase / .8f), .19f, 0);
            }
        }
        private void OnDestroy() { if (shimmerMaterial != null) Destroy(shimmerMaterial); }
        private void OnDisable() { Finish(); if (arrow != null) arrow.localScale = arrowScale; if (glint != null) glint.localPosition = glintRest; }
    }
}
