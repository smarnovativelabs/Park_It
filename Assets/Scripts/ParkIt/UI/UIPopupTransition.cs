using UnityEngine;

namespace ParkIt
{
    /// <summary>Unscaled, interruptible modal transitions; backdrop stays full-screen.</summary>
    public sealed class UIPopupTransition : MonoBehaviour
    {
        private CanvasGroup group;
        private Transform panel;
        private Vector3 restScale;
        private float from, target, elapsed, duration;
        private bool running;

        private void Init()
        {
            if (group != null) return;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            panel = transform.Find("Panel");
            if (panel != null) restScale = panel.localScale;
        }

        public static void Show(GameObject root)
        {
            if (root == null) return;
            // Modal siblings share one presentation slot. Fade the previous one
            // out and put the new backdrop above it while both are transitioning.
            if (root.transform.parent != null)
                foreach (Transform sibling in root.transform.parent)
                    if (sibling.gameObject != root && sibling.gameObject.activeSelf && sibling.Find("Panel") != null)
                        Hide(sibling.gameObject);
            root.transform.SetAsLastSibling();
            var motion = root.GetComponent<UIPopupTransition>();
            if (motion == null) motion = root.AddComponent<UIPopupTransition>();
            motion.Init();
            bool wasActive = root.activeSelf;
            root.SetActive(true);
            if (!wasActive) motion.Apply(0f);
            motion.Begin(1f);
        }

        public static void Hide(GameObject root)
        {
            if (root == null || !root.activeSelf) return;
            var motion = root.GetComponent<UIPopupTransition>();
            if (motion == null) motion = root.AddComponent<UIPopupTransition>();
            motion.Init();
            motion.Begin(0f);
        }

        private void Begin(float value)
        {
            if (running && target == value) return;
            if (!running && Mathf.Approximately(group.alpha, value)) return;
            from = group.alpha;
            target = value;
            elapsed = 0f;
            duration = value > 0f ? .20f : .14f;
            running = true;
            group.interactable = false;
            group.blocksRaycasts = true;
            SfxPlayer.Play(value > 0f ? Sfx.PopupOpen : Sfx.PopupClose);
        }

        private void Apply(float value)
        {
            group.alpha = value;
            if (panel != null) panel.localScale = restScale * Mathf.Lerp(.95f, 1f, value);
        }

        private void Update()
        {
            if (!running) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Apply(Mathf.Lerp(from, target, t * t * (3f - 2f * t)));
            if (t < 1f) return;
            running = false;
            if (target == 0f) gameObject.SetActive(false);
            else group.interactable = true;
        }

        private void OnDisable()
        {
            running = false;
            if (group == null) return;
            Apply(1f);
            group.interactable = group.blocksRaycasts = true;
        }
    }
}
