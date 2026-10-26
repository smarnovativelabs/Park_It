using UnityEngine;
using UnityEngine.UI;

namespace ParkIt
{
    public partial class ParkItUIManager
    {
        private int buttonTutorialStep = -1;
        private Button tutorialTarget, tutorialNext;
        private Text tutorialTip;
        private Image tutorialHighlight;
        private RectTransform tutorialOverlay, tutorialPanel;
        private readonly Image[] tutorialDim = new Image[4];
        private bool tutorialAdvanceRequested;
        private float tutorialPanelReadyAt;
        private readonly Vector3[] tutorialCorners = new Vector3[4];
        private static readonly string[] ButtonTips = {
            "Drive: move forward", "Rev: move backward", "Left: move and turn left",
            "Right: move and turn right", "Hint: show the next move",
            "Undo: take back a move", "Restart: try the level again"
        };

        private void BeginButtonTutorial()
        {
            StopButtonTutorial();
            ClearHintAction();
            if (uiReferences == null || TitleVisible) return;
            if (tutorialTip == null)
            {
                tutorialOverlay = UIKit.Rect("TutorialSpotlight", uiRoot);
                UIKit.Stretch(tutorialOverlay);
                for (int i = 0; i < tutorialDim.Length; i++)
                    tutorialDim[i] = UIKit.Img("Dim" + i, tutorialOverlay, null, new Color(.13f, .04f, .22f, .70f), true);
                var panel = UIKit.Img("TutorialPanel", tutorialOverlay, "dock_panel", Color.white, true);
                tutorialPanel = panel.rectTransform;
                tutorialTip = UIKit.Bold(tutorialPanel, "", 38, UIKit.Ink, TextAnchor.MiddleCenter);
                tutorialTip.name = "ButtonTutorialTip";
                tutorialTip.horizontalOverflow = HorizontalWrapMode.Wrap;
                tutorialTip.verticalOverflow = VerticalWrapMode.Overflow;
                tutorialTip.raycastTarget = false;
                tutorialTip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UIKit.Stretch(tutorialTip.rectTransform, 42);
                tutorialNext = panel.gameObject.AddComponent<Button>();
                tutorialNext.targetGraphic = panel;
                tutorialNext.transition = Selectable.Transition.None;
                tutorialNext.onClick.AddListener(RequestTutorialAdvance);
                UIKit.IconButton("SkipTutorial", tutorialPanel, "dock_drive", "icon_close", Color.white, 56, 30, StopButtonTutorial);
                var close = (RectTransform)tutorialPanel.Find("SkipTutorial");
                UIKit.Place(close, Vector2.one, Vector2.one, new Vector2(-16, -16), new Vector2(56, 56));
            }
            tutorialOverlay.gameObject.SetActive(true);
            tutorialOverlay.SetAsLastSibling();
            buttonTutorialStep = 0;
            ShowButtonTutorialStep();
        }

        private void RequestTutorialAdvance()
        {
            if (buttonTutorialStep < 0 || Time.unscaledTime < tutorialPanelReadyAt || tutorialAdvanceRequested) return;
            tutorialAdvanceRequested = true;
        }

        // An actual control tap must advance even during the panel's double-tap guard.
        private void RequestTutorialControlAdvance()
        {
            if (buttonTutorialStep >= 0) tutorialAdvanceRequested = true;
        }

        private void ShowButtonTutorialStep()
        {
            if (tutorialTarget != null) tutorialTarget.onClick.RemoveListener(RequestTutorialControlAdvance);
            var r = uiReferences;
            var targets = new[] { r.forwardBtn, r.reverseBtn, r.turnLeftBtn, r.turnRightBtn,
                r.hintButton, r.undoButton, r.restartButton };
            tutorialTarget = targets[buttonTutorialStep];
            if (tutorialTarget == null) { StopButtonTutorial(); return; }
            tutorialTarget.onClick.AddListener(RequestTutorialControlAdvance);
            if (tutorialHighlight == null)
            {
                tutorialHighlight = UIKit.Img("TutorialHighlight", tutorialTarget.transform, "btn_ring", Color.white);
                tutorialHighlight.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            tutorialHighlight.transform.SetParent(tutorialTarget.transform, false);
            UIKit.Stretch(tutorialHighlight.rectTransform, -8);
            tutorialHighlight.raycastTarget = false;
            tutorialHighlight.gameObject.SetActive(true);
            tutorialTip.text = ButtonTips[buttonTutorialStep] + "\n\n<size=26>" + (buttonTutorialStep + 1) + "/7 · Tap here " + (buttonTutorialStep == 6 ? "to finish" : "for next") + "</size>";
            tutorialTip.gameObject.SetActive(true);
            tutorialAdvanceRequested = false;
            tutorialPanelReadyAt = Time.unscaledTime + .35f;
        }

        private void UpdateButtonTutorial()
        {
            if (buttonTutorialStep < 0) return;
            if (TitleVisible || Time.timeScale == 0 || carController == null || carController.IsLevelComplete ||
                levelSelectRoot.activeSelf || victoryRoot.activeSelf || defeatRoot.activeSelf || (creator != null && creator.IsOpen))
            { StopButtonTutorial(); return; }
            if (tutorialAdvanceRequested)
            {
                if (carController.IsBusy) return;
                if (++buttonTutorialStep == ButtonTips.Length)
                { ProgressSave.Instance.CompleteTutorial(); StopButtonTutorial(); return; }
                ShowButtonTutorialStep();
            }
            tutorialHighlight.color = new Color(.12f, .70f, .24f, .6f + .3f * Mathf.Sin(Time.unscaledTime * 5));
            // Four dim rectangles leave the real button visible and tappable, without cloning/reparenting it.
            var bounds = tutorialOverlay.rect;
            ((RectTransform)tutorialTarget.transform).GetWorldCorners(tutorialCorners);
            var lo = tutorialOverlay.InverseTransformPoint(tutorialCorners[0]);
            var hi = tutorialOverlay.InverseTransformPoint(tutorialCorners[2]);
            float left = Mathf.Clamp(lo.x - 18, bounds.xMin, bounds.xMax);
            float right = Mathf.Clamp(hi.x + 18, bounds.xMin, bounds.xMax);
            float bottom = Mathf.Clamp(lo.y - 18, bounds.yMin, bounds.yMax);
            float top = Mathf.Clamp(hi.y + 18, bounds.yMin, bounds.yMax);
            PlaceTutorialDim(0, bounds.xMin, top, bounds.xMax, bounds.yMax);
            PlaceTutorialDim(1, bounds.xMin, bounds.yMin, bounds.xMax, bottom);
            PlaceTutorialDim(2, bounds.xMin, bottom, left, top);
            PlaceTutorialDim(3, right, bottom, bounds.xMax, top);
            UIKit.Place(tutorialPanel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(Mathf.Min(760, bounds.width - 64), 310));
        }

        private void PlaceTutorialDim(int index, float left, float bottom, float right, float top)
        {
            UIKit.Place(tutorialDim[index].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2((left + right) * .5f, (bottom + top) * .5f), new Vector2(right - left, top - bottom));
        }

        private void StopButtonTutorial()
        {
            buttonTutorialStep = -1;
            tutorialAdvanceRequested = false;
            if (tutorialTarget != null) tutorialTarget.onClick.RemoveListener(RequestTutorialControlAdvance);
            tutorialTarget = null;
            if (tutorialHighlight != null) tutorialHighlight.gameObject.SetActive(false);
            if (tutorialTip != null) tutorialTip.gameObject.SetActive(false);
            if (tutorialOverlay != null) tutorialOverlay.gameObject.SetActive(false);
        }
    }
}
