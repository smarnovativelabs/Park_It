using System.Collections;
using UnityEngine;

namespace ParkIt
{
    public partial class ParkItUIManager
    {
        private Coroutine hintActionRoutine;
        private UnityEngine.UI.Button hintUndoButton, hintRestartButton;
        private UnityEngine.UI.Image hintMarker, hintRing;
        private Transform hintedControl;
        private Vector3 hintedRestScale;

        private void ClearHintAction()
        {
            if (hintActionRoutine != null) StopCoroutine(hintActionRoutine);
            hintActionRoutine = null;
            if (hintedControl != null)
            {
                if (hintedControl.TryGetComponent<UITapFeedback>(out var feedback)) feedback.SetHintPulse(1f);
                else hintedControl.localScale = hintedRestScale;
            }
            if (hintRing != null) hintRing.color = new Color(1, 1, 1, 0);
            if (hintMarker != null) hintMarker.gameObject.SetActive(false);
            hintedControl = null;
            hintRing = null;
        }

        public void ShowLevelHint()
        {
            if (carController == null || carController.IsBusy || carController.IsLevelComplete ||
                TitleVisible || AnyModalOpen || Time.timeScale == 0f) return;
            ClearHintAction();
            var level = GameManager.Instance.CurrentLevelData;
            if (level == null) return;
            var route = LevelSolver.SolveFrom(level, carController.CurrentCoord,
                carController.CurrentDirection, 144);
            SfxPlayer.Play(Sfx.HintTap);
            UnityEngine.UI.Button target;
            if (!route.solvable || route.path.Count == 0 || route.moves > carController.RemainingFuel)
                target = hintUndoButton != null && hintUndoButton.interactable ? hintUndoButton : hintRestartButton;
            else
            {
                if (!driveButtons.TryGetValue(route.path[0], out var drive)) return;
                target = drive.button;
                hintRing = drive.ring;
                drive.flash = 0f;
            }
            if (target == null || !target.interactable) return;
            hintedControl = target.transform;
            hintedRestScale = hintedControl.TryGetComponent<UITapFeedback>(out var feedback)
                ? feedback.RestScale : hintedControl.localScale;
            if (hintMarker == null)
            {
                hintMarker = UIKit.Img("HintMarker", hintedControl, "icon_hint_bulb", Color.white);
                hintMarker.raycastTarget = false;
                hintMarker.preserveAspect = true;
                hintMarker.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            }
            hintMarker.transform.SetParent(hintedControl, false);
            UIKit.Place(hintMarker.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 0),
                new Vector2(0, 8), new Vector2(48, 48));
            hintMarker.gameObject.SetActive(true);
            hintActionRoutine = StartCoroutine(PulseHint());
        }

        private IEnumerator PulseHint()
        {
            for (float elapsed = 0; elapsed < 5f; elapsed += Time.unscaledDeltaTime)
            {
                if (AnyModalOpen || TitleVisible || Time.timeScale == 0f) break;
                float pulse = .5f + .5f * Mathf.Sin(elapsed * Mathf.PI * 4);
                if (hintedControl.TryGetComponent<UITapFeedback>(out var feedback)) feedback.SetHintPulse(1f + .05f * pulse);
                else hintedControl.localScale = hintedRestScale * (1f + .05f * pulse);
                if (hintRing != null) hintRing.color = new Color(.12f, .70f, .24f, .45f + .5f * pulse);
                yield return null;
            }
            hintActionRoutine = null;
            ClearHintAction();
        }
    }
}
