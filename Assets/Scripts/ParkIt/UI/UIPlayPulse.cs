using UnityEngine;

namespace ParkIt
{
    /// <summary>Subtle visual emphasis without resizing the button's layout slot.</summary>
    public sealed class UIPlayPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, .1f)] private float amount = .04f;
        [SerializeField, Min(.1f)] private float period = 2.2f;
        private Vector3 restScale;
        private float elapsed;

        private void OnEnable()
        {
            restScale = transform.localScale;
            elapsed = 0f;
        }

        private void LateUpdate()
        {
            elapsed = (elapsed + Time.unscaledDeltaTime) % Mathf.Max(.1f, period);
            float pulse = .5f - .5f * Mathf.Cos(elapsed * Mathf.PI * 2f / Mathf.Max(.1f, period));
            transform.localScale = restScale * (1f + amount * pulse);
        }

        private void OnDisable() => transform.localScale = restScale;
    }
}
