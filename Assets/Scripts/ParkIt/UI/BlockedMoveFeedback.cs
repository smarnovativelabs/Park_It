using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkIt
{
    // Disabled-looking controls still explain a blocked move without becoming legal moves.
    public sealed class BlockedMoveFeedback : MonoBehaviour, IPointerClickHandler
    {
        public MoveType Move;
        private UnityEngine.UI.Button button;
        private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || button == null || button.interactable) return;
            var ui = ParkItUIManager.Instance;
            if (ui != null && (ui.TitleVisible || ui.AnyModalOpen)) return;
            Object.FindFirstObjectByType<CarController>()?.RejectMove(Move);
        }
    }
}
