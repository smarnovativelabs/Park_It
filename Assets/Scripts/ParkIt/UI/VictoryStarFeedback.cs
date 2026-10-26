using UnityEngine;

namespace ParkIt
{
    public sealed class VictoryStarFeedback : MonoBehaviour
    {
        private Transform[] stars;
        private Vector3[] scales;
        private UnityEngine.UI.Image[,] sparkles;
        private float elapsed;
        private int earned;
        private bool running;
        public void Play(UnityEngine.UI.Image[] tiles, UnityEngine.UI.Image[] icons, int count)
        {
            ResetPose();
            if (stars == null)
            {
                stars = new Transform[tiles.Length]; scales = new Vector3[tiles.Length];
                sparkles = new UnityEngine.UI.Image[tiles.Length, 6];
                for (int i = 0; i < tiles.Length; i++)
                {
                    stars[i] = tiles[i].transform; scales[i] = stars[i].localScale;
                    for (int j = 0; j < 6; j++)
                    {
                        var go = new GameObject("Reward_Sparkle", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
                        go.transform.SetParent(stars[i], false);
                        go.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                        var img = go.GetComponent<UnityEngine.UI.Image>(); img.sprite = icons[i].sprite; img.raycastTarget = false;
                        img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(.5f, .5f);
                        img.rectTransform.sizeDelta = Vector2.one * 18f; img.color = Color.clear;
                        sparkles[i,j] = img;
                    }
                }
            }
            earned = count; elapsed = 0; running = true;
            for (int i = 0; i < earned; i++) stars[i].localScale = Vector3.zero;
        }
        private void Update()
        {
            if (!running) return;
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < stars.Length; i++)
            {
                if (i >= earned) continue;
                float t = Mathf.Clamp01((elapsed - .2f - i * .22f) / .36f);
                float scale = t < .65f ? Mathf.Lerp(0f, 1.18f, Mathf.Sin(t / .65f * Mathf.PI * .5f)) : Mathf.Lerp(1.18f, 1f, (t - .65f) / .35f);
                stars[i].localScale = scales[i] * scale;
                for (int j = 0; j < 6; j++)
                {
                    float a = j * Mathf.PI / 3f; float spread = (i == 2 ? 65f : 48f) * t;
                    var img = sparkles[i,j]; img.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spread;
                    img.color = new Color(1f, .78f, .15f, Mathf.Sin(t * Mathf.PI));
                    img.rectTransform.localScale = Vector3.one * (1f - t * .6f);
                }
            }
            if (elapsed > 1.2f) { ResetPose(); running = false; }
        }
        public void ResetPose()
        {
            running = false;
            if (stars == null) return;
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null) stars[i].localScale = scales[i];
                for (int j = 0; j < 6; j++) if (sparkles[i,j] != null) sparkles[i,j].color = Color.clear;
            }
        }
        private void OnDisable() { ResetPose(); }
    }
}
