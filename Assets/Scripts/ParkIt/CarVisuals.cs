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

        [Header("Toy Car Animation")]
        [SerializeField] private float idleSwayDegrees = .45f;
        [SerializeField] private float drivingPitchDegrees = 3f;
        [SerializeField] private float turnLeanDegrees = 5f;
        [SerializeField] private float drivingBounceHeight = .025f;
        [SerializeField] private float skidYawDegrees = 6f;
        private bool moving, slipping, chassisPoseCaptured;
        private float moveProgress, turnSign, driveSign = 1f, animationClock;
        private Vector3 chassisRestPosition;
        private Quaternion chassisRestRotation;
        public const float VictoryAnimationDuration = .8f;
        [SerializeField] private float victoryHopHeight = .20f;
        [SerializeField] private Vector3 headlightFlashPosition = new Vector3(.32f, .49f, .78f);
        private bool celebrating;
        private float victoryElapsed;
        private GameObject victoryHeadlights;
        public const float GameOverAnimationDuration = .75f;
        [SerializeField] private float gameOverShakeDegrees = 2f;
        private bool losing, defeated;
        private float defeatElapsed;
        private Renderer[] headlightRenderers;
        private Light[] headlightLights;
        private MaterialPropertyBlock headlightProperties;
        [Header("Level Arrival")]
        [SerializeField] private float landingHeight = .24f;
        [SerializeField] private float landingDuration = .55f;
        private bool landing;
        private float landingElapsed, movementLandingHeight;
        [Header("Color Change Sparkles")]
        [SerializeField] private float colorSparkleDuration = .45f;
        private Transform colorSparkleRoot;
        private Transform[] colorSparkles;
        private float colorSparkleElapsed;
        private bool sparkling;
        private static Mesh sparkleMesh;

        [Header("Wheels")]
        [SerializeField] private Transform[] wheels;
        [SerializeField] private float wheelRollSpeed = 720f;

        [Header("Particles & Dust")]
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem skidParticles;
        [Header("Exhaust Smoke")]
        [SerializeField] private Vector3 exhaustPosition = new Vector3(0f, .48f, -1.02f);
        [SerializeField] private float idleSmokeRate = 6f;
        [SerializeField] private float drivingSmokeRate = 20f;
        private ParticleSystem exhaustSmoke;
        private Material smokeMaterial;
        private Texture2D smokeTexture;
        private CarController smokeController;
        private ParticleSystem[] tyreSkidSmoke;
        private ParticleSystem victoryConfetti;
        private Material confettiMaterial;
        private Texture2D confettiTexture;

        private const int ConfettiBudget = 24;
        private float appliedSmokeRate = -1f;

        private void BuildVictoryConfetti()
        {
            if (victoryConfetti == null)
            {
                // Small rounded ribbons with a baked soft highlight, tinted by each particle.
                confettiTexture = new Texture2D(16, 24, TextureFormat.RGBA32, false);
                confettiTexture.name = "Candy_Confetti_Ribbon";
                confettiTexture.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color[16 * 24];
                for (int y = 0; y < 24; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        float edgeX = Mathf.Max(0, Mathf.Abs(x - 7.5f) - 4.5f);
                        float edgeY = Mathf.Max(0, Mathf.Abs(y - 11.5f) - 8.5f);
                        float alpha = Mathf.Clamp01(3f - Mathf.Sqrt(edgeX * edgeX + edgeY * edgeY));
                        float shade = .90f + .10f * Mathf.Sin((x + .5f) / 16f * Mathf.PI);
                        pixels[y * 16 + x] = new Color(shade, shade, shade, alpha);
                    }
                confettiTexture.SetPixels(pixels); confettiTexture.Apply(false, true);
                confettiMaterial = new Material(Resources.Load<Shader>("CandyParticles"));
                confettiMaterial.name = "Candy_Victory_Confetti";
                confettiMaterial.SetTexture("_BaseMap", confettiTexture);
                confettiMaterial.SetColor("_BaseColor", Color.white);
                confettiMaterial.renderQueue = 3000;

                var root = new GameObject("Victory_Candy_Confetti");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0, 1.05f, 0);
                root.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                victoryConfetti = root.AddComponent<ParticleSystem>();
                victoryConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = victoryConfetti.main;
                main.playOnAwake = false; main.loop = false; main.duration = .95f;
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = false;
                main.maxParticles = ConfettiBudget; main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, .9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f); main.gravityModifier = .55f;
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(.16f, .23f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(.24f, .34f);
                main.startSizeZ = .1f; main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                var palette = new Gradient { mode = GradientMode.Fixed };
                palette.SetKeys(new[] {
                    new GradientColorKey(new Color(1f,.08f,.32f),0),
                    new GradientColorKey(new Color(1f,.92f,.05f),.25f),
                    new GradientColorKey(new Color(.05f,1f,1f),.5f),
                    new GradientColorKey(new Color(.65f,.18f,1f),.75f),
                    new GradientColorKey(new Color(1f,.92f,.05f),1)},
                    new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(1,1) });
                var colors = new ParticleSystem.MinMaxGradient(palette);
                colors.mode = ParticleSystemGradientMode.RandomColor; main.startColor = colors;
                var emission = victoryConfetti.emission;
                emission.rateOverTime = 0;
                var shape = victoryConfetti.shape;
                shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = .45f; shape.angle = 55;
                var rotation = victoryConfetti.rotationOverLifetime;
                rotation.enabled = true; rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
                var fade = new Gradient();
                fade.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[] {new GradientAlphaKey(1,0),new GradientAlphaKey(1,.65f),new GradientAlphaKey(0,1)});
                var color = victoryConfetti.colorOverLifetime; color.enabled = true; color.color = fade;
                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = confettiMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ConfigureParticleRenderer(renderer);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

            }
        }

        private void PlayVictoryConfetti()
        {
            BuildVictoryConfetti();
            victoryConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            victoryConfetti.Play();
            victoryConfetti.Emit(ConfettiBudget);
        }

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
            PlayColorSparkles();
        }

        private void PlayColorSparkles()
        {
            if (chassisRoot == null) return;
            if (colorSparkleRoot == null)
            {
                if (sparkleMesh == null)
                {
                    var vertices = new Vector3[9];
                    var triangles = new int[48];
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI * .25f;
                        float radius = i % 2 == 0 ? .5f : .13f;
                        vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                        int next = (i + 1) % 8 + 1;
                        int j = i * 6;
                        triangles[j] = 0; triangles[j + 1] = i + 1; triangles[j + 2] = next;
                        triangles[j + 3] = 0; triangles[j + 4] = next; triangles[j + 5] = i + 1;
                    }
                    sparkleMesh = new Mesh { name = "Car_Four_Point_Sparkle" };
                    sparkleMesh.vertices = vertices; sparkleMesh.triangles = triangles;
                    sparkleMesh.RecalculateNormals(); sparkleMesh.RecalculateBounds();
                }
                colorSparkleRoot = new GameObject("Color_Change_Sparkles").transform;
                colorSparkleRoot.SetParent(chassisRoot, false);
                var material = URPMaterialHelper.CreateMaterial(new Color(1f, .95f, .64f), .5f, 0);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(2f, 1.8f, 1f));
                colorSparkles = new Transform[8];
                for (int i = 0; i < colorSparkles.Length; i++)
                {
                    var star = new GameObject("Sparkle_" + i);
                    star.transform.SetParent(colorSparkleRoot, false);
                    star.AddComponent<MeshFilter>().sharedMesh = sparkleMesh;
                    var renderer = star.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    colorSparkles[i] = star.transform;
                }
            }
            colorSparkleElapsed = 0;
            sparkling = true;
            colorSparkleRoot.gameObject.SetActive(true);
            UpdateColorSparkles(0f);
            SfxPlayer.Play(Sfx.ColorSparkle);
        }

        private void UpdateColorSparkles(float dt)
        {
            if (!sparkling) return;
            colorSparkleElapsed += dt;
            float t = Mathf.Clamp01(colorSparkleElapsed / Mathf.Max(.1f, colorSparkleDuration));
            if (t >= 1f) { sparkling = false; colorSparkleRoot.gameObject.SetActive(false); return; }
            var camera = Camera.main;
            for (int i = 0; i < colorSparkles.Length; i++)
            {
                float angle = i * Mathf.PI * .25f;
                var star = colorSparkles[i];
                float spread = 1f + .35f * t;
                star.localPosition = new Vector3(Mathf.Cos(angle) * .68f * spread, .65f + .3f * t + (i % 2) * .12f, Mathf.Sin(angle) * .9f * spread);
                float size = Mathf.Sin(t * Mathf.PI) * (i % 2 == 0 ? .30f : .21f);
                star.localScale = Vector3.one * size;
                if (camera != null) star.rotation = camera.transform.rotation * Quaternion.Euler(0, 0, i * 17f + t * 60f);
            }
        }

        public void ClearColorSparkles()
        {
            SfxPlayer.Stop(Sfx.ColorSparkle);
            sparkling = false;
            colorSparkleElapsed = 0f;
            if (colorSparkleRoot != null) colorSparkleRoot.gameObject.SetActive(false);
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
            smokeController = GetComponent<CarController>();
            BuildExhaustSmoke();
            BuildTyreSkidSmoke();
            BuildVictoryConfetti(); // Allocate once during level setup, not on the first win.
        }

        private void BuildTyreSkidSmoke()
        {
            if (chassisRoot == null || smokeMaterial == null || wheels == null) return;
            tyreSkidSmoke = new ParticleSystem[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] == null) continue;
                var root = new GameObject("Tyre_Skid_Smoke_" + i);
                // Follow chassis steering, not the spinning wheel transform.
                root.transform.SetParent(chassisRoot, false);
                Vector3 wheelCenter = chassisRoot.InverseTransformPoint(wheels[i].position);
                root.transform.localPosition = new Vector3(wheelCenter.x + Mathf.Sign(wheelCenter.x) * .13f, .15f, wheelCenter.z);
                root.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var particles = root.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false; main.loop = true; main.maxParticles = 8;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.useUnscaledTime = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.40f, .60f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.10f, .22f);
                main.startSize = new ParticleSystem.MinMaxCurve(.22f, .32f);
                main.startColor = new Color(.92f, .96f, 1f, .78f);
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 40f; shape.radius = .055f;
                var emission = particles.emission; emission.rateOverTime = 12f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
                var size = particles.sizeOverLifetime;
                size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .55f, 1, 1.9f));
                var color = particles.colorOverLifetime;
                color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) });
                color.color = gradient;
                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = smokeMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ConfigureParticleRenderer(renderer);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                tyreSkidSmoke[i] = particles;
            }
        }

        private static void ConfigureParticleRenderer(ParticleSystemRenderer renderer)
        {
            // This unlit shader needs only position, tint and UV; no lighting/probe work.
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            });
        }
        private void StopTyreSkidSmoke(bool clear)
        {
            if (tyreSkidSmoke == null) return;
            foreach (var particles in tyreSkidSmoke)
                if (particles != null) particles.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        private void BuildExhaustSmoke()
        {
            if (chassisRoot == null || exhaustSmoke != null) return;
            smokeTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            smokeTexture.name = "Soft_Exhaust_Puff";
            smokeTexture.wrapMode = TextureWrapMode.Clamp;
            smokeTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius * radius), 2f);
                    pixels[y * 32 + x] = new Color(.96f, .97f, 1f, alpha);
                }
            smokeTexture.SetPixels(pixels); smokeTexture.Apply(false, true);
            smokeMaterial = new Material(Resources.Load<Shader>("CandyParticles"));
            smokeMaterial.name = "Car_Exhaust_Smoke";
            smokeMaterial.SetTexture("_BaseMap", smokeTexture);
            smokeMaterial.SetColor("_BaseColor", Color.white);
            smokeMaterial.renderQueue = 3000;

            var root = new GameObject("Exhaust_Smoke");
            root.transform.SetParent(chassisRoot, false);
            root.transform.localPosition = exhaustPosition;
            root.transform.localRotation = Quaternion.Euler(0, 180, 0);
            exhaustSmoke = root.AddComponent<ParticleSystem>();
            exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = exhaustSmoke.main;
            main.playOnAwake = false; main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = false; main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .25f);
            main.startSize = new ParticleSystem.MinMaxCurve(.25f, .36f);
            main.startColor = new Color(1f, 1f, 1f, .75f);
            var shape = exhaustSmoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 18f; shape.radius = .035f;
            var velocity = exhaustSmoke.velocityOverLifetime;
            velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = .40f;
            var size = exhaustSmoke.sizeOverLifetime;
            size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .55f, 1, 2.1f));
            var color = exhaustSmoke.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var emission = exhaustSmoke.emission; emission.rateOverTime = 0f;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = smokeMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ConfigureParticleRenderer(renderer);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            exhaustSmoke.Play();
        }

        private void UpdateExhaustSmoke()
        {
            if (exhaustSmoke == null) return;
            bool show = !defeated && (smokeController == null || !smokeController.IsLevelComplete) &&
                (ParkItUIManager.Instance == null || !ParkItUIManager.Instance.TitleVisible);
            // Retain large bright puffs, but bound emission and avoid redundant native writes.
            float rate = show ? (moving ? Mathf.Min(drivingSmokeRate, 16f) : Mathf.Min(idleSmokeRate, 5f)) : 0f;
            if (Mathf.Approximately(rate, appliedSmokeRate)) return;
            appliedSmokeRate = rate;
            var emission = exhaustSmoke.emission;
            emission.rateOverTime = rate;
        }

        private void OnDestroy()
        {
            if (confettiMaterial != null) Destroy(confettiMaterial);
            if (confettiTexture != null) Destroy(confettiTexture);
            if (smokeMaterial != null) Destroy(smokeMaterial);
            if (smokeTexture != null) Destroy(smokeTexture);
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
            UpdateExhaustSmoke();
            // Clamp only the spring integration. Timed effects must keep pace with
            // movement and result popups even when an editor/mobile frame stalls.
            float animationDt = Time.deltaTime;
            float dt = Mathf.Min(animationDt, 0.05f);
            UpdateColorSparkles(animationDt);
            animationClock += animationDt;
            float hop = 0f;
            if (landing)
            {
                float previousLandingTime = landingElapsed;
                landingElapsed += animationDt;
                float impactTime = Mathf.Max(.1f, landingDuration) * .55f;
                if (previousLandingTime < impactTime && landingElapsed >= impactTime &&
                    (ParkItUIManager.Instance == null || !ParkItUIManager.Instance.TitleVisible))
                    SfxPlayer.Play(Sfx.Landing);
                float t = Mathf.Clamp01(landingElapsed / Mathf.Max(.1f, landingDuration));
                // Short drop, then a small rebound; tyres never sink below the board.
                hop = t < .55f ? landingHeight * (1f - Mathf.Pow(t / .55f, 2f))
                    : landingHeight * .20f * Mathf.Sin((t - .55f) / .45f * Mathf.PI);
                if (t >= 1f) { landing = false; hop = 0f; }
            }
            float defeatShake = 0f;
            if (losing)
            {
                defeatElapsed += animationDt;
                float t = Mathf.Clamp01(defeatElapsed / GameOverAnimationDuration);
                float remaining = 1f - t;
                defeatShake = Mathf.Sin(t * Mathf.PI * 8f) * remaining * remaining * gameOverShakeDegrees;
                SetHeadlightBrightness(remaining * remaining);
                if (t >= 1f) { losing = false; victoryHeadlights.SetActive(false); }
            }
            if (celebrating)
            {
                victoryElapsed += animationDt;
                float t = Mathf.Clamp01(victoryElapsed / VictoryAnimationDuration);
                // A small main hop followed by a softer settling hop; no scale squash.
                hop = t < .65f ? Mathf.Sin(t / .65f * Mathf.PI) * victoryHopHeight
                    : Mathf.Sin((t - .65f) / .35f * Mathf.PI) * victoryHopHeight * .22f;
                victoryHeadlights.SetActive((t < .22f) || (t >= .40f && t < .62f));
                if (t >= 1f) { celebrating = false; hop = 0; victoryHeadlights.SetActive(false); }
            }

            // Second-order spring physics for chassis suspension wobble
            float rollAcc = -springStiffness * rollAngle - springDamping * rollVelocity;
            rollVelocity += rollAcc * dt;
            rollAngle += rollVelocity * dt;

            float pitchAcc = -springStiffness * pitchAngle - springDamping * pitchVelocity;
            pitchVelocity += pitchAcc * dt;
            pitchAngle += pitchVelocity * dt;

            if (chassisRoot != null)
            {
                CaptureChassisPose();
                float envelope = moving ? Mathf.Sin(moveProgress * Mathf.PI) : 0f;
                float idle = moving || defeated || landing ? 0f : Mathf.Sin(animationClock * 2.2f) * idleSwayDegrees;
                float lean = turnSign * turnLeanDegrees * envelope;
                float pitch = moving ? -driveSign * drivingPitchDegrees * Mathf.Sin(moveProgress * Mathf.PI * 2f) : 0f;
                float skid = slipping ? Mathf.Sin(moveProgress * Mathf.PI * 4f) * skidYawDegrees * envelope : 0f;
                float bounce = drivingBounceHeight * Mathf.Abs(Mathf.Sin(moveProgress * Mathf.PI * 2f)) * envelope;
                float arrivalSettle = moving ? movementLandingHeight * Mathf.Pow(1f - moveProgress, 2f) : 0f;
                chassisRoot.localPosition = chassisRestPosition + Vector3.up * (bounce + hop + arrivalSettle);
                chassisRoot.localRotation = chassisRestRotation * Quaternion.Euler(pitchAngle + pitch, skid, rollAngle + lean + idle + defeatShake);
            }
        }

        private void CaptureChassisPose()
        {
            if (chassisPoseCaptured || chassisRoot == null) return;
            chassisRestPosition = chassisRoot.localPosition;
            chassisRestRotation = chassisRoot.localRotation;
            chassisPoseCaptured = true;
        }

        public void PlayVictory()
        {
            ClearColorSparkles();
            CancelMove();
            if (chassisRoot == null) return;
            EnsureHeadlights();
            SetHeadlightBrightness(1f);
            victoryElapsed = 0f;
            celebrating = true;
            victoryHeadlights.SetActive(true);
            SfxPlayer.Play(Sfx.VictoryHop);
            PlayVictoryConfetti();
        }

        public void PlayLanding()
        {
            ClearColorSparkles();
            CancelMove();
            if (chassisRoot == null) return;
            landingElapsed = 0f;
            landing = true;
            chassisRoot.localPosition = chassisRestPosition + Vector3.up * landingHeight;
        }

        public void PlayGameOver()
        {
            ClearColorSparkles();
            CancelMove();
            if (chassisRoot == null) return;
            EnsureHeadlights();
            SetHeadlightBrightness(1f);
            defeatElapsed = 0f;
            losing = defeated = true;
            victoryHeadlights.SetActive(true);
            SfxPlayer.Play(Sfx.GameOverPowerDown);
        }

        private void SetHeadlightBrightness(float brightness)
        {
            if (headlightRenderers == null) return;
            if (headlightProperties == null) headlightProperties = new MaterialPropertyBlock();
            headlightProperties.SetColor("_BaseColor", Color.Lerp(new Color(.16f, .15f, .12f), new Color(1f, .96f, .70f), brightness));
            headlightProperties.SetColor("_EmissionColor", new Color(3f, 2.7f, 1.4f) * brightness);
            foreach (var renderer in headlightRenderers) renderer.SetPropertyBlock(headlightProperties);
            foreach (var light in headlightLights) light.intensity = 1.8f * brightness;
        }

        private void EnsureHeadlights()
        {
            if (victoryHeadlights == null)
            {
                victoryHeadlights = new GameObject("Victory_Headlight_Flashes");
                victoryHeadlights.transform.SetParent(chassisRoot, false);
                var glow = URPMaterialHelper.CreateMaterial(new Color(1f, .96f, .70f), .6f, 0f);
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", new Color(3f, 2.7f, 1.4f));
                foreach (int side in new[] { -1, 1 })
                {
                    var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bulb.name = side < 0 ? "Flash_Left" : "Flash_Right";
                    bulb.transform.SetParent(victoryHeadlights.transform, false);
                    bulb.transform.localPosition = new Vector3(headlightFlashPosition.x * side, headlightFlashPosition.y, headlightFlashPosition.z);
                    bulb.transform.localScale = new Vector3(.16f, .10f, .06f);
                    var collider = bulb.GetComponent<Collider>();
                    collider.enabled = false;
                    Destroy(collider);
                    var renderer = bulb.GetComponent<Renderer>();
                    renderer.sharedMaterial = glow;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var light = bulb.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, .94f, .72f);
                    light.range = 1.4f;
                    light.intensity = 1.8f;
                    light.shadows = LightShadows.None;
                }
                headlightRenderers = victoryHeadlights.GetComponentsInChildren<Renderer>(true);
                headlightLights = victoryHeadlights.GetComponentsInChildren<Light>(true);
            }
        }

        public void AnimateMove(Vector3 targetPos, float targetEulerY, MoveType type, bool isOilSlip, Action onComplete)
        {
            float remainingArrivalHeight = landing && chassisRoot != null
                ? Mathf.Max(0f, chassisRoot.localPosition.y - chassisRestPosition.y) : 0f;
            CancelMove();
            movementLandingHeight = remainingArrivalHeight;
            if (chassisRoot != null) chassisRoot.localPosition = chassisRestPosition + Vector3.up * remainingArrivalHeight;
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
            moving = true;
            slipping = isOilSlip;
            moveProgress = 0f;
            driveSign = type == MoveType.Reverse ? -1f : 1f;
            turnSign = type == MoveType.TurnLeft || type == MoveType.ShiftLeft ? -1f
                : type == MoveType.TurnRight || type == MoveType.ShiftRight ? 1f : 0f;

            if (dustParticles != null) dustParticles.Play();
            if (isOilSlip && skidParticles != null) skidParticles.Play();
            if (isOilSlip && tyreSkidSmoke != null)
                foreach (var particles in tyreSkidSmoke) if (particles != null) particles.Play();

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
                moveProgress = t;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 previousPos = transform.position;

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
                        // Match wheel spin to distance, so wheels slow with the car.
                        if (w != null) w.Rotate(wheelAxis, driveSign * wheelRollSpeed * Vector3.Distance(previousPos, transform.position) * .2f);
                    }
                }

                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;

            moving = slipping = false;
            if (dustParticles != null) dustParticles.Stop();
            if (skidParticles != null) skidParticles.Stop();
            StopTyreSkidSmoke(false);
            pitchVelocity += driveSign * 18f;
            onComplete?.Invoke();
        }

        public void ResetSuspension()
        {
            if (victoryConfetti != null) victoryConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            StopTyreSkidSmoke(true);
            if (exhaustSmoke != null) exhaustSmoke.Clear();
            SfxPlayer.Stop(Sfx.Landing);
            SfxPlayer.Stop(Sfx.VictoryHop);
            SfxPlayer.Stop(Sfx.GameOverPowerDown);
            landing = false;
            landingElapsed = movementLandingHeight = 0f;
            celebrating = false;
            losing = defeated = false;
            defeatElapsed = 0f;
            SetHeadlightBrightness(1f);
            victoryElapsed = 0f;
            if (victoryHeadlights != null) victoryHeadlights.SetActive(false);
            moving = slipping = false;
            moveProgress = turnSign = 0f;
            rollAngle = 0;
            rollVelocity = 0;
            pitchAngle = 0;
            pitchVelocity = 0;
            if (chassisRoot != null)
            {
                CaptureChassisPose();
                chassisRoot.localPosition = chassisRestPosition;
                chassisRoot.localRotation = chassisRestRotation;
            }
        }
    }
}

