using System;
using UnityEngine;

namespace ParkIt
{
    public class GhostNodeView : MonoBehaviour
    {
        private ValidMoveData moveData;
        private Action onSelectCallback;
        private float elapsed;

        public void Initialize(ValidMoveData move, Action onSelect)
        {
            moveData = move;
            onSelectCallback = onSelect;
            elapsed = 0f;
            transform.localScale = Vector3.one * .05f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / .22f);
            transform.localScale = Vector3.one * (1f - Mathf.Pow(1f - t, 3f) + .10f * Mathf.Sin(t * Mathf.PI));
            float bob = Mathf.Sin(Time.time * 4f) * 0.04f;
            transform.position = new Vector3(transform.position.x, 0.08f + bob, transform.position.z);
        }

        public void OnSelected()
        {
            onSelectCallback?.Invoke();
        }
    }
}
