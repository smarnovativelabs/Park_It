using System;
using System.Collections;
using UnityEngine;

namespace ParkIt
{
    public class CarVisuals : MonoBehaviour
    {
        [Header("Suspension Springs")]
        [SerializeField] private Transform chassisRoot;
        [SerializeField] private float springStiffness = 85f;
        [SerializeField] private float springDamping = 8.5f;

        [Header("Wheels")]
        [SerializeField] private Transform[] wheels;
        [SerializeField] private float wheelRollSpeed = 720f;

        [Header("Particles & Dust")]
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem skidParticles;

        private float rollAngle = 0f;
        private float rollVelocity = 0f;
        private float pitchAngle = 0f;
        private float pitchVelocity = 0f;

        private Vector3 wheelAxis = Vector3.up;
        private CarModelConfig activeConfig;
        private int selectedColor;
        private static readonly string[] ColorNames = { "Yellow", "Blue", "Green", "Red" };
        public string ColorName => ColorNames[selectedColor];
        public string NextColorName => ColorNames[NextColorIndex()];
        private int NextColorIndex()
        {
            if (!CanChangeColor) return selectedColor;
            var materials = new[] { activeConfig.yellowBodyMaterial, activeConfig.blueBodyMaterial, activeConfig.greenBodyMaterial, activeConfig.redBodyMaterial };
            for (int step = 1; step <= ColorNames.Length; step++)
            {
                int index = (selectedColor + step) % ColorNames.Length;
                if (materials[index] != null) return index;
            }
            return selectedColor;
        }
        private Material SelectedBodyMaterial => selectedColor == 1 ? activeConfig.blueBodyMaterial
            : selectedColor == 2 ? activeConfig.greenBodyMaterial
            : selectedColor == 3 ? activeConfig.redBodyMaterial : activeConfig.yellowBodyMaterial;
        public bool CanChangeColor => activeConfig != null && activeConfig.yellowBodyMaterial != null && activeConfig.blueBodyMaterial != null;

        public void CycleColor()
        {
            if (!CanChangeColor) return;
            selectedColor = NextColorIndex();
            ApplyBodyColor();
            ProgressSave.Instance.SetCarColor(selectedColor);
        }

        private void ApplyBodyColor()
        {
            if (!CanChangeColor) return;
            var paint = SelectedBodyMaterial;
            if (paint == null) { selectedColor = 0; paint = activeConfig.yellowBodyMaterial; }
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    if (material.name != activeConfig.bodyMaterialName && material != activeConfig.bodyMaterial
                        && material != activeConfig.yellowBodyMaterial && material != activeConfig.blueBodyMaterial
                        && material != activeConfig.greenBodyMaterial && material != activeConfig.redBodyMaterial) continue;
                    materials[i] = paint;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private void Awake()
        {
            var config = Resources.Load<CarModelConfig>(CarModelConfig.ResourcePath);
            if (config != null && config.model != null) BuildModelCar(config);
            else if (chassisRoot == null) BuildProceduralToyCar();
        }

        /// <summary>Uses the imported car model: scaled to fit a tile, nose to +Z, wheels on spin pivots.</summary>
        public void BuildModelCar(CarModelConfig config)
        {
            activeConfig = config;
            selectedColor = Application.isPlaying ? Mathf.Clamp(ProgressSave.Instance.CarColorIndex, 0, ColorNames.Length - 1) : 0;
            while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);

            var chassis = new GameObject("Chassis_Group");
            chassis.transform.SetParent(transform, false);
            chassisRoot = chassis.transform;

            var model = Instantiate(config.model, chassisRoot, false);
            model.name = "Car_Model";
            model.transform.localRotation = Quaternion.Euler(0, config.yawOffset, 0);
            foreach (var col in model.GetComponentsInChildren<Collider>()) DestroyImmediate(col);
            if (config.bodyMaterial != null)
            {
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && mats[i].name == config.bodyMaterialName) mats[i] = config.bodyMaterial;
                    r.sharedMaterials = mats;
                }
            }

            // Measure in chassis space, then scale to the target length and sit the tyres on the board.
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = LocalBounds(renderers);
            float scale = config.length / Mathf.Max(.0001f, Mathf.Max(bounds.size.x, bounds.size.z));
            model.transform.localScale = Vector3.one * scale;
            bounds = LocalBounds(renderers);
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            // Wheel meshes are baked off-centre, so give each one a pivot at its own centre to roll around.
            var spinning = new System.Collections.Generic.List<Transform>();
            foreach (var r in renderers)
            {
                if (string.IsNullOrEmpty(config.wheelNameContains) || !r.name.Contains(config.wheelNameContains)) continue;
                var pivot = new GameObject(r.name + "_Pivot").transform;
                pivot.SetParent(chassisRoot, false);
                pivot.position = r.bounds.center;
                pivot.localRotation = Quaternion.identity;
                r.transform.SetParent(pivot, true);
                spinning.Add(pivot);
            }
            wheels = spinning.ToArray();
            wheelAxis = Vector3.right;
            ApplyBodyColor();
        }

        private Bounds LocalBounds(Renderer[] renderers)
        {
            var b = new Bounds();
            bool first = true;
            foreach (var r in renderers)
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = chassisRoot.InverseTransformPoint(corner);
                    if (first) { b = new Bounds(local, Vector3.zero); first = false; }
                    else b.Encapsulate(local);
                }
            }
            return b;
        }

        public void BuildProceduralToyCar()
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            GameObject chassis = new GameObject("Chassis_Group");
            chassis.transform.SetParent(transform, false);
            chassisRoot = chassis.transform;
            wheelAxis = Vector3.up;

            // 1. Lower Chassis (Lacquered Toy Red)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body_Lower";
            body.transform.SetParent(chassisRoot, false);
            body.transform.localPosition = new Vector3(0, 0.38f, 0);
            body.transform.localScale = new Vector3(1.0f, 0.42f, 1.6f);
            var br = body.GetComponentInChildren<Renderer>();
            if (br != null) br.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(0.96f, 0.24f, 0.24f), 0.90f, 0.18f);

            // 2. Cabin Glass (Tinted Cyan)
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin_Glass";
            cabin.transform.SetParent(chassisRoot, false);
            cabin.transform.localPosition = new Vector3(0, 0.72f, -0.05f);
            cabin.transform.localScale = new Vector3(0.85f, 0.42f, 0.95f);
            var cr = cabin.GetComponentInChildren<Renderer>();
            if (cr != null) cr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(0.25f, 0.82f, 1f), 0.95f, 0.25f, true);

            // 3. Roof Panel (Crisp White)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(chassisRoot, false);
            roof.transform.localPosition = new Vector3(0, 0.94f, -0.05f);
            roof.transform.localScale = new Vector3(0.88f, 0.08f, 0.98f);
            var rr = roof.GetComponentInChildren<Renderer>();
            if (rr != null) rr.sharedMaterial = URPMaterialHelper.CreateMaterial(Color.white, 0.85f, 0.08f);

            // 4. Chrome Bumpers
            GameObject fBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fBumper.name = "Bumper_Front";
            fBumper.transform.SetParent(chassisRoot, false);
            fBumper.transform.localPosition = new Vector3(0, 0.26f, 0.84f);
            fBumper.transform.localScale = new Vector3(0.96f, 0.12f, 0.14f);
            var fbr = fBumper.GetComponentInChildren<Renderer>();
            if (fbr != null) fbr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(0.92f, 0.92f, 0.96f), 0.95f, 0.9f);

            // 5. Headlights
            GameObject hlL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hlL.name = "Headlight_L";
            hlL.transform.SetParent(chassisRoot, false);
            hlL.transform.localPosition = new Vector3(-0.34f, 0.45f, 0.81f);
            hlL.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            hlL.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
            var hlr = hlL.GetComponentInChildren<Renderer>();
            if (hlr != null) hlr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(1f, 0.98f, 0.6f), 0.95f, 0.2f);

            GameObject hlR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hlR.name = "Headlight_R";
            hlR.transform.SetParent(chassisRoot, false);
            hlR.transform.localPosition = new Vector3(0.34f, 0.45f, 0.81f);
            hlR.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            hlR.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
            var hrr = hlR.GetComponentInChildren<Renderer>();
            if (hrr != null) hrr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(1f, 0.98f, 0.6f), 0.95f, 0.2f);

            // 6. 4 Chunky Rubber Wheels with Golden Hubcaps
            Vector3[] wheelOffsets = new Vector3[]
            {
                new Vector3(-0.54f, 0.24f, 0.5f),
                new Vector3(0.54f, 0.24f, 0.5f),
                new Vector3(-0.54f, 0.24f, -0.5f),
                new Vector3(0.54f, 0.24f, -0.5f)
            };

            wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = $"Wheel_{i}";
                w.transform.SetParent(chassisRoot, false);
                w.transform.localPosition = wheelOffsets[i];
                w.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                w.transform.localScale = new Vector3(0.48f, 0.16f, 0.48f);
                var wr = w.GetComponentInChildren<Renderer>();
                if (wr != null) wr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(0.12f, 0.12f, 0.15f), 0.5f, 0.1f);

                // Hubcap
                GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hub.name = "Hubcap";
                hub.transform.SetParent(w.transform, false);
                hub.transform.localPosition = new Vector3(0, 1.05f, 0);
                hub.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
                var hubr = hub.GetComponentInChildren<Renderer>();
                if (hubr != null) hubr.sharedMaterial = URPMaterialHelper.CreateMaterial(new Color(1f, 0.84f, 0.15f), 0.90f, 0.6f);

                wheels[i] = w.transform;
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            // Second-order spring physics for chassis suspension wobble
            float rollAcc = -springStiffness * rollAngle - springDamping * rollVelocity;
            rollVelocity += rollAcc * dt;
            rollAngle += rollVelocity * dt;

            float pitchAcc = -springStiffness * pitchAngle - springDamping * pitchVelocity;
            pitchVelocity += pitchAcc * dt;
            pitchAngle += pitchVelocity * dt;

            if (chassisRoot != null)
            {
                chassisRoot.localRotation = Quaternion.Euler(pitchAngle, 0f, rollAngle);
            }
        }

        public void AnimateMove(Vector3 targetPos, float targetEulerY, MoveType type, bool isOilSlip, Action onComplete)
        {
            CancelMove();
            StartCoroutine(MoveRoutine(targetPos, targetEulerY, type, isOilSlip, onComplete));
        }

        public void CancelMove()
        {
            StopAllCoroutines();
            if (dustParticles != null) dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (skidParticles != null) skidParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ResetSuspension();
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, float targetEulerY, MoveType type, bool isOilSlip, Action onComplete)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.Euler(0, targetEulerY, 0);

            float duration = isOilSlip ? 0.52f : 0.38f;
            float elapsed = 0f;

            if (dustParticles != null) dustParticles.Play();
            if (isOilSlip && skidParticles != null) skidParticles.Play();

            switch (type)
            {
                case MoveType.TurnLeft:
                    rollVelocity = 18f;
                    break;
                case MoveType.TurnRight:
                    rollVelocity = -18f;
                    break;
                case MoveType.Reverse:
                    pitchVelocity = -12f;
                    break;
                default:
                    pitchVelocity = 15f;
                    break;
            }

            bool isCurve = (type == MoveType.TurnLeft || type == MoveType.TurnRight || type == MoveType.ShiftLeft || type == MoveType.ShiftRight);
            Vector3 midpoint = (startPos + targetPos) * 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (isCurve)
                {
                    float u = 1f - ease;
                    transform.position = u * u * startPos + 2f * u * ease * midpoint + ease * ease * targetPos;
                }
                else
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, ease);
                }

                transform.rotation = Quaternion.Slerp(startRot, targetRot, ease);

                if (wheels != null)
                {
                    foreach (var w in wheels)
                    {
                        if (w != null) w.Rotate(wheelAxis, (type == MoveType.Reverse ? -1f : 1f) * wheelRollSpeed * Time.deltaTime);
                    }
                }

                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;

            pitchVelocity += 14f;
            onComplete?.Invoke();
        }

        public void ResetSuspension()
        {
            rollAngle = 0;
            rollVelocity = 0;
            pitchAngle = 0;
            pitchVelocity = 0;
            if (chassisRoot != null) chassisRoot.localRotation = Quaternion.identity;
        }
    }
}

