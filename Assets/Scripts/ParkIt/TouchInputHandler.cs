using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkIt
{
    public class TouchInputHandler : MonoBehaviour
    {
        private Vector2 touchStartPos;
        private Vector2 lastPointerPos;
        private bool pointerActive, dragged, canOrbit;
        private int activeFinger = -1;
        private const float TapThreshold = 25f;

        private void Update()
        {
            #if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
            #else
            HandleTouchInput();
            #endif
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
                BeginPointer(Input.mousePosition, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            if (Input.GetMouseButton(0)) MovePointer(Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
                EndPointer(Input.mousePosition, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount != 1) { pointerActive = false; activeFinger = -1; return; }

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                activeFinger = touch.fingerId;
                BeginPointer(touch.position, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId));
            }
            else if (touch.fingerId == activeFinger)
            {
                if (touch.phase == TouchPhase.Canceled) pointerActive = false;
                else if (touch.phase == TouchPhase.Ended)
                    EndPointer(touch.position, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId));
                else MovePointer(touch.position);
            }
        }

        private bool GameplayOpen => ParkItUIManager.Instance == null ||
            (!ParkItUIManager.Instance.TitleVisible && !ParkItUIManager.Instance.AnyModalOpen);

        private void BeginPointer(Vector2 position, bool overUI)
        {
            touchStartPos = lastPointerPos = position;
            pointerActive = !overUI && GameplayOpen;
            dragged = false;
            Vector2 normalized = new Vector2(position.x / Mathf.Max(1, Screen.width), position.y / Mathf.Max(1, Screen.height));
            canOrbit = normalized.x >= .08f && normalized.x <= .92f && normalized.y >= .22f && normalized.y <= .78f;
        }

        private void MovePointer(Vector2 position)
        {
            if (!pointerActive) return;
            if (!GameplayOpen) { pointerActive = false; return; }
            bool wasDragged = dragged;
            if (Vector2.Distance(position, touchStartPos) >= TapThreshold) dragged = true;
            if (dragged && canOrbit)
                CameraRig.Instance?.RotateFromSwipe(position - (wasDragged ? lastPointerPos : touchStartPos));
            lastPointerPos = position;
        }

        private void EndPointer(Vector2 position, bool overUI)
        {
            MovePointer(position);
            // Board gestures only orbit the camera; driving uses the HUD controls.
            pointerActive = false;
        }

        private void OnDisable() { pointerActive = false; activeFinger = -1; }

    }
}
