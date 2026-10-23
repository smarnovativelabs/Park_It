using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkIt
{
    // One scale owner combines the hint pulse with touch feedback.
    public sealed class UITapFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private UnityEngine.UI.Button button;
        private Vector3 restScale;
        private float press = 1f, hint = 1f;
        private bool held;
        public bool PlayTapSound { get; set; } = true;
        public Vector3 RestScale => restScale;

        public static void Attach(UnityEngine.UI.Button target)
        {
            if (target != null && !target.TryGetComponent<UITapFeedback>(out _))
                target.gameObject.AddComponent<UITapFeedback>();
        }

        private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); restScale = transform.localScale; }
        public void SetHintPulse(float value) { hint = value; Apply(); }
        private void Apply() => transform.localScale = restScale * (press * hint);
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
            held = true;
            if (PlayTapSound) SfxPlayer.Play(Sfx.UITap);
            press = .94f;
            Apply();
        }
        public void OnPointerUp(PointerEventData e) { held = false; }
        public void OnPointerExit(PointerEventData e) { held = false; }
        private void LateUpdate()
        {
            if (!button.IsInteractable()) held = false;
            press = Mathf.MoveTowards(press, held ? .94f : 1f, Time.unscaledDeltaTime * .5f);
            Apply();
        }
        private void OnDisable() { held = false; press = hint = 1f; Apply(); }
        private void OnApplicationFocus(bool focused) { if (!focused) held = false; }
    }
}
