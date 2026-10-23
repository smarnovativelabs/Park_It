using System;
using UnityEngine;

namespace ParkIt
{
    public class GhostNodeView : MonoBehaviour
    {
        private ValidMoveData moveData;
        private Action onSelectCallback;

        public void Initialize(ValidMoveData move, Action onSelect)
        {
            moveData = move;
            onSelectCallback = onSelect;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * 4f) * 0.04f;
            transform.position = new Vector3(transform.position.x, 0.08f + bob, transform.position.z);
        }

        public void OnSelected()
        {
            onSelectCallback?.Invoke();
        }
    }
}
