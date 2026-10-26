using UnityEngine;

namespace ParkIt
{
    public sealed class FuelFeedback : MonoBehaviour
    {
        private Vector3 rest;
        private Transform icon;
        private Vector3 iconRest;
        private int previous = -1;
        private float kick, clock;
        private bool low;
        private void Awake() { rest = transform.localScale; }
        public void SetFuel(int fuel, Transform fuelIcon)
        {
            if (icon == null && fuelIcon != null) { icon = fuelIcon; iconRest = icon.localScale; }
            if (previous >= 0 && previous != fuel) kick = 1f;
            previous = fuel; low = fuel > 0 && fuel <= 2;
        }
        private void Update()
        {
            kick = Mathf.Max(0f, kick - Time.unscaledDeltaTime / .28f);
            transform.localScale = rest * (1f + .18f * Mathf.Sin(kick * Mathf.PI));
            clock += Time.deltaTime;
            if (icon != null) icon.localScale = iconRest * (low ? 1f + .055f * Mathf.Sin(clock * 4f) : 1f);
        }
        private void OnDisable() { kick = 0f; transform.localScale = rest; if (icon != null) icon.localScale = iconRest; }
    }
}
