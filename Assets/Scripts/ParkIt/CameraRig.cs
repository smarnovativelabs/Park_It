using UnityEngine;

namespace ParkIt
{
    /// <summary>Orthographic diorama camera: framing, the "45° Cam" toggle and a slow orbit behind the title screen.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public enum View { Tilted, Iso45, TopDown }

        private static readonly Vector2[] ViewAngles = { new Vector2(48f, 0f), new Vector2(38f, 45f), new Vector2(80f, 0f) };
        private static readonly string[] ViewLabels = { "Tilt Cam", "45° Cam", "Top Cam" };

        public View CurrentView { get; private set; } = View.Tilted;
        public bool Orbiting { get; set; }
        /// <summary>Fraction of the screen height the board may use (rest is HUD / dock).</summary>
        public float VerticalFill { get; set; } = .58f;

        private Camera cam;
        private Vector3 boardExtents = new Vector3(6, .9f, 6);
        private float orbitYaw;
        private float pitch, yaw;
        private Vector2 manualOrbit;
        private Transform candyBackdrop;
        private SpriteRenderer candyBackdropRenderer;

        public string NextViewLabel => ViewLabels[((int)CurrentView + 1) % ViewLabels.Length];

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.77f, .54f, .94f);
            var background = new GameObject("Candy_Atlas_Background");
            candyBackdrop = background.transform;
            candyBackdrop.SetParent(transform, false);
            candyBackdrop.localPosition = new Vector3(0, 0, 95);
            candyBackdropRenderer = background.AddComponent<SpriteRenderer>();
            candyBackdropRenderer.sprite = UISpriteAtlas.Get("candy_background");
            candyBackdropRenderer.sortingOrder = -100;
            pitch = ViewAngles[0].x;
            yaw = ViewAngles[0].y;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void CycleView()
        {
            manualOrbit = Vector2.zero;
            CurrentView = (View)(((int)CurrentView + 1) % ViewAngles.Length);
        }

        public void RotateFromSwipe(Vector2 pixelDelta)
        {
            if (Orbiting) return;
            manualOrbit.y = Mathf.Repeat(manualOrbit.y + pixelDelta.x / Mathf.Max(1, Screen.width) * 180f, 360f);
            float basePitch = ViewAngles[(int)CurrentView].x;
            manualOrbit.x = Mathf.Clamp(basePitch - pixelDelta.y / Mathf.Max(1, Screen.height) * 120f + manualOrbit.x, 20f, 80f) - basePitch;
        }

        public void FrameBoard(int width, int height, float tileSize)
        {
            // Include the glossy plinth's full overhang, not just the playing tiles.
            boardExtents = new Vector3((width * tileSize) * .5f + 1.01f, .98f, (height * tileSize) * .5f + 1.01f);
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (Orbiting) orbitYaw += dt * 7f;
            else orbitYaw = Mathf.MoveTowardsAngle(orbitYaw, 0f, dt * 120f);
            Apply(1f - Mathf.Exp(-dt * 8f));
        }

        /// <summary>Jump straight to the current view (no easing).</summary>
        public void Snap()
        {
            if (!Orbiting) orbitYaw = 0f;
            Apply(1f);
        }

        private void Apply(float k)
        {
            Vector2 goal = ViewAngles[(int)CurrentView] + manualOrbit;
            pitch = Mathf.Lerp(pitch, goal.x, k);
            yaw = Mathf.LerpAngle(yaw, goal.y + orbitYaw, k);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = -transform.forward * 60f;

            float halfW = 0f, halfH = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -boardExtents.x : boardExtents.x,
                    (i & 2) == 0 ? -boardExtents.y : 1.3f,
                    (i & 4) == 0 ? -boardExtents.z : boardExtents.z);
                halfW = Mathf.Max(halfW, Mathf.Abs(Vector3.Dot(transform.right, corner)));
                halfH = Mathf.Max(halfH, Mathf.Abs(Vector3.Dot(transform.up, corner)));
            }
            bool portraitGameplay = !Orbiting && cam.aspect < 1f;
            float widthFill = portraitGameplay ? .95f : .92f;
            float size = Mathf.Max(halfW / (Mathf.Max(.1f, cam.aspect) * widthFill), halfH / VerticalFill);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, k);
            // Centre the board slightly below mid-screen, between the HUD and controls.
            if (portraitGameplay)
                transform.position += transform.up * (cam.orthographicSize * .06f);
            if (candyBackdropRenderer != null && candyBackdropRenderer.sprite != null)
            {
                Vector2 spriteSize = candyBackdropRenderer.sprite.bounds.size;
                candyBackdrop.localScale = new Vector3(cam.orthographicSize * 2f * cam.aspect / spriteSize.x,
                    cam.orthographicSize * 2f / spriteSize.y, 1);
            }
        }
    }
}
