using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Controls the swift, lightweight ghost particle effect emitted when a card takes damage.
    /// </summary>
    [AddComponentMenu("SG03/VFX/Ghost Damage VFX Ctrl")]
    public class GhostDamageVfxCtrl : PoolObj
    {
        [Header("VFX Settings")]
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private bool useBloodRed = true;
        [SerializeField] private Vector3 cardSurfaceBoxScale = new Vector3(7.0f, 0.2f, 10.0f);

        [Header("Skull Settings")]
        [Tooltip("Number of skulls emitted per skull variant (there are 4 variants, so total skulls = count * 4)")]
        [SerializeField, Min(1)] private int skullsPerVariant = 3;
        [Tooltip("Min and Max lifetime of skull particles (seconds)")]
        [SerializeField] private Vector2 skullLifetime = new Vector2(0.5f, 0.65f);
        [Tooltip("Initial upward burst speed of skulls")]
        [SerializeField] private Vector2 skullPopSpeed = new Vector2(3.0f, 4.0f);
        [Tooltip("Gravity multiplier pulling skulls back down")]
        [SerializeField, Min(0f)] private float skullGravity = 0.95f;
        [Tooltip("Time window over which skulls randomly emerge from the card (seconds)")]
        [SerializeField, Min(0f)] private float skullEmergenceWindow = 0.14f;

        [Header("Blood Wisp Settings")]
        [Tooltip("Total count of blood wisp particles")]
        [SerializeField, Min(0)] private int bloodWispsCount = 70;
        [Tooltip("Min and Max lifetime of blood wisps (seconds, shorter than skulls)")]
        [SerializeField] private Vector2 wispLifetime = new Vector2(0.25f, 0.4f);
        [Tooltip("Initial upward burst speed of blood wisps (slower than skulls so they don't fly as high)")]
        [SerializeField] private Vector2 wispPopSpeed = new Vector2(1.5f, 2.3f);
        [Tooltip("Gravity multiplier pulling blood wisps back down")]
        [SerializeField, Min(0f)] private float wispGravity = 1.1f;
        [Tooltip("Min and Max particle size of blood wisps")]
        [SerializeField] private Vector2 wispSize = new Vector2(0.5f, 0.9f);
        [Tooltip("Time window over which blood wisps randomly emerge from the card (seconds)")]
        [SerializeField, Min(0f)] private float wispEmergenceWindow = 0.18f;

        [Header("Emitters")]
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem[] ghostVariantEmitters;
        [SerializeField] private ParticleSystem soulWisps;

        public float Duration => this.duration;
        public bool UseBloodRed { get => this.useBloodRed; set => this.useBloodRed = value; }
        public int SkullsPerVariant { get => this.skullsPerVariant; set => this.skullsPerVariant = Mathf.Max(1, value); }
        public Vector2 SkullLifetime { get => this.skullLifetime; set => this.skullLifetime = value; }
        public Vector2 SkullPopSpeed { get => this.skullPopSpeed; set => this.skullPopSpeed = value; }
        public float SkullGravity { get => this.skullGravity; set => this.skullGravity = Mathf.Max(0f, value); }
        public float SkullEmergenceWindow { get => this.skullEmergenceWindow; set => this.skullEmergenceWindow = Mathf.Max(0f, value); }

        public int BloodWispsCount { get => this.bloodWispsCount; set => this.bloodWispsCount = Mathf.Max(0, value); }
        public Vector2 WispLifetime { get => this.wispLifetime; set => this.wispLifetime = value; }
        public Vector2 WispPopSpeed { get => this.wispPopSpeed; set => this.wispPopSpeed = value; }
        public float WispGravity { get => this.wispGravity; set => this.wispGravity = Mathf.Max(0f, value); }
        public Vector2 WispSize { get => this.wispSize; set => this.wispSize = value; }
        public float WispEmergenceWindow { get => this.wispEmergenceWindow; set => this.wispEmergenceWindow = Mathf.Max(0f, value); }

        public override string GetName() => "GhostDamageVfx";

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadMainParticleSystem();
            this.LoadChildParticleSystems();
            this.LoadGhostVariantEmitters();
            this.LoadSpecializedEmitters();
        }

        protected virtual void LoadMainParticleSystem()
        {
            if (this.mainParticleSystem != null) return;
            this.mainParticleSystem = this.GetComponent<ParticleSystem>();
        }

        protected virtual void LoadChildParticleSystems()
        {
            if (this.childParticleSystems != null && this.childParticleSystems.Length > 0 && !System.Array.Exists(this.childParticleSystems, ps => ps == null)) return;
            this.childParticleSystems = this.GetComponentsInChildren<ParticleSystem>(true);
        }

        protected virtual void LoadGhostVariantEmitters()
        {
            if (this.ghostVariantEmitters != null && this.ghostVariantEmitters.Length > 0 && !System.Array.Exists(this.ghostVariantEmitters, ps => ps == null)) return;
            var list = new System.Collections.Generic.List<ParticleSystem>();
            for (int i = 1; i <= 4; i++)
            {
                Transform t = this.transform.Find($"Ghost_Variant{i}");
                if (t != null)
                {
                    var ps = t.GetComponent<ParticleSystem>();
                    if (ps != null) list.Add(ps);
                }
            }
            this.ghostVariantEmitters = list.ToArray();
        }

        protected virtual void LoadSpecializedEmitters()
        {
            this.LoadSoulWisps();
        }

        private void LoadSoulWisps()
        {
            if (this.soulWisps != null) return;
            Transform wisps = this.transform.Find("SoulWisps");
            if (wisps != null) this.soulWisps = wisps.GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Plays a fast, light ghost skull impact effect on taking damage.
        /// </summary>
        public void Play(float customDuration = 0.65f) => this.Play(customDuration, this.useBloodRed);

        public void Play(float customDuration, bool bloodRed)
        {
            this.gameObject.SetActive(true);
            this.ConfigureEmitters(customDuration, bloodRed);
            this.RestartEmitters();
        }

        private void ConfigureEmitters(float customDuration, bool bloodRed)
        {
            bool hasVariants = this.ghostVariantEmitters != null && this.ghostVariantEmitters.Length > 0;
            if (this.mainParticleSystem != null)
            {
                var mainEmission = this.mainParticleSystem.emission;
                mainEmission.enabled = !hasVariants;
            }

            Color skullColor = bloodRed ? new Color(1f, 0.95f, 0.95f, 0.95f) : new Color(1f, 1f, 1f, 0.95f);
            if (hasVariants)
            {
                foreach (var ps in this.ghostVariantEmitters)
                {
                    if (ps == null) continue;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(this.cardSurfaceBoxScale.x, this.cardSurfaceBoxScale.z, this.cardSurfaceBoxScale.y);
                    shape.rotation = new Vector3(-90f, 0f, 0f);

                    var main = ps.main;
                    main.startColor = skullColor;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(this.skullLifetime.x, Mathf.Min(this.skullLifetime.y, customDuration));
                    main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(this.skullPopSpeed.x, this.skullPopSpeed.y);
                    main.gravityModifier = this.skullGravity;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.space = ParticleSystemSimulationSpace.World;
                    vol.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                    vol.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                    vol.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = this.BuildFadeGradient(skullColor);

                    float skullWindow = Mathf.Min(this.skullEmergenceWindow, customDuration * 0.25f);
                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(this.BuildRandomSkullBursts(this.skullsPerVariant, skullWindow));
                    ps.gameObject.SetActive(true);
                }
            }

            if (this.soulWisps != null)
            {
                var wispsShape = this.soulWisps.shape;
                wispsShape.shapeType = ParticleSystemShapeType.Box;
                wispsShape.scale = new Vector3(this.cardSurfaceBoxScale.x, this.cardSurfaceBoxScale.z, this.cardSurfaceBoxScale.y);
                wispsShape.rotation = new Vector3(-90f, 0f, 0f);

                var wispsMain = this.soulWisps.main;
                Color wispColor = bloodRed ? new Color(1f, 0.05f, 0.05f, 1f) : new Color(0.4f, 0.95f, 1f, 0.95f);
                wispsMain.startColor = wispColor;
                wispsMain.startLifetime = new ParticleSystem.MinMaxCurve(this.wispLifetime.x, Mathf.Min(this.wispLifetime.y, customDuration));
                wispsMain.startSize = new ParticleSystem.MinMaxCurve(this.wispSize.x, this.wispSize.y);
                wispsMain.startSpeed = new ParticleSystem.MinMaxCurve(this.wispPopSpeed.x, this.wispPopSpeed.y);
                wispsMain.gravityModifier = this.wispGravity;
                wispsMain.simulationSpace = ParticleSystemSimulationSpace.World;

                var wispsVol = this.soulWisps.velocityOverLifetime;
                wispsVol.enabled = true;
                wispsVol.space = ParticleSystemSimulationSpace.World;
                wispsVol.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                wispsVol.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                wispsVol.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

                var wispsCol = this.soulWisps.colorOverLifetime;
                wispsCol.enabled = true;
                wispsCol.color = this.BuildFadeGradient(wispColor);

                float wispWindow = Mathf.Min(this.wispEmergenceWindow, customDuration * 0.30f);
                var wispsEmission = this.soulWisps.emission;
                wispsEmission.rateOverTime = 0f;
                wispsEmission.SetBursts(this.BuildRandomWispBursts(this.bloodWispsCount, wispWindow));
                this.soulWisps.gameObject.SetActive(true);
            }
        }

        private ParticleSystem.Burst[] BuildRandomSkullBursts(int totalSkulls, float windowDuration)
        {
            int burstCount = Mathf.Clamp(totalSkulls, 1, 8);
            var bursts = new ParticleSystem.Burst[burstCount];
            float[] times = new float[burstCount];
            for (int i = 0; i < burstCount; i++)
            {
                times[i] = Random.Range(0.0f, windowDuration);
            }
            System.Array.Sort(times);
            int remaining = totalSkulls;
            for (int i = 0; i < burstCount; i++)
            {
                int count = (i == burstCount - 1) ? remaining : Mathf.Max(1, remaining / (burstCount - i));
                remaining -= count;
                bursts[i] = new ParticleSystem.Burst(times[i], (short)count, (short)count);
            }
            return bursts;
        }

        private ParticleSystem.Burst[] BuildRandomWispBursts(int totalWisps, float windowDuration)
        {
            if (totalWisps <= 0) return new ParticleSystem.Burst[0];
            int burstCount = Mathf.Clamp(totalWisps / 7, 3, 8);
            var bursts = new ParticleSystem.Burst[burstCount];
            float[] times = new float[burstCount];
            for (int i = 0; i < burstCount; i++)
            {
                times[i] = Random.Range(0.0f, windowDuration);
            }
            System.Array.Sort(times);
            int remaining = totalWisps;
            for (int i = 0; i < burstCount; i++)
            {
                int count = (i == burstCount - 1) ? remaining : Mathf.Max(1, remaining / (burstCount - i));
                remaining -= count;
                bursts[i] = new ParticleSystem.Burst(times[i], (short)count, (short)count);
            }
            return bursts;
        }

        private ParticleSystem.MinMaxGradient BuildFadeGradient(Color baseColor)
        {
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseColor, 0f), new GradientColorKey(baseColor, 1f) },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.65f),
                    new GradientAlphaKey(0.4f, 0.85f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            return new ParticleSystem.MinMaxGradient(grad);
        }

        private void RestartEmitters()
        {
            if (this.childParticleSystems != null && this.childParticleSystems.Length > 0)
            {
                foreach (ParticleSystem ps in this.childParticleSystems)
                {
                    if (ps == null || !ps.gameObject.activeSelf) continue;
                    ps.Clear();
                    ps.Play();
                }
                return;
            }

            if (this.mainParticleSystem != null)
            {
                this.mainParticleSystem.Clear();
                this.mainParticleSystem.Play();
            }
        }

        public void Stop()
        {
            if (this.childParticleSystems != null && this.childParticleSystems.Length > 0)
            {
                foreach (ParticleSystem ps in this.childParticleSystems)
                {
                    if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (this.mainParticleSystem != null)
            {
                this.mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void ReturnToPool()
        {
            this.Stop();
            if (this.despawn != null)
            {
                this.despawn.DoDespawn();
                return;
            }

            this.gameObject.SetActive(false);
        }
    }
}
