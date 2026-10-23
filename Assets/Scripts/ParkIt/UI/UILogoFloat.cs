using UnityEngine;

namespace ParkIt
{
    /// <summary>Soft repeating bounce inside a fixed layout slot, independent of game time.</summary>
    public sealed class UILogoFloat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 20f;
        [SerializeField, Min(0.1f)] private float period = 2.8f;

        private RectTransform rect;
        private Vector2 restPosition;
        private float elapsed;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
            elapsed = 0f;
        }

        private void LateUpdate()
        {
            elapsed = (elapsed + Time.unscaledDeltaTime) % Mathf.Max(.1f, period);
            float phase = elapsed / Mathf.Max(.1f, period);
            // One main hop, a smaller settling hop, then a quiet pause.
            float hop = phase < .5f ? Mathf.Sin(phase / .5f * Mathf.PI) :
                phase < .75f ? .22f * Mathf.Sin((phase - .5f) / .25f * Mathf.PI) : 0f;
            rect.anchoredPosition = restPosition + Vector2.up * (hop * amplitude);
        }

        private void OnDisable()
        {
            if (rect != null)
            {
                rect.anchoredPosition = restPosition;
            }
        }
    }
}
