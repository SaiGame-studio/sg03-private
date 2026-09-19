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

        [Header("Particle Counts")]
        [Tooltip("Number of skulls emitted per skull variant (there are 4 variants, so total skulls = count * 4)")]
        [SerializeField, Min(1)] private int skullsPerVariant = 3;
        [Tooltip("Total count of blood wisp particles")]
        [SerializeField, Min(0)] private int bloodWispsCount = 70;

        [Header("Emitters")]
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem[] ghostVariantEmitters;
        [SerializeField] private ParticleSystem soulWisps;

        public float Duration => this.duration;
        public bool UseBloodRed { get => this.useBloodRed; set => this.useBloodRed = value; }
        public int SkullsPerVariant { get => this.skullsPerVariant; set => this.skullsPerVariant = Mathf.Max(1, value); }
        public int BloodWispsCount { get => this.bloodWispsCount; set => this.bloodWispsCount = Mathf.Max(0, value); }

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
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, Mathf.Min(0.65f, customDuration));
                    main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 4.0f);
                    main.gravityModifier = 0.95f;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.space = ParticleSystemSimulationSpace.World;
                    vol.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                    vol.y = 0f;
                    vol.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = this.BuildFadeGradient(skullColor);

                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, (short)this.skullsPerVariant) });
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
                Color wispColor = bloodRed ? new Color(1f, 0.08f, 0.08f, 0.9f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                wispsMain.startColor = wispColor;
                wispsMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, Mathf.Min(0.6f, customDuration));
                wispsMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
                wispsMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 3.8f);
                wispsMain.gravityModifier = 0.85f;
                wispsMain.simulationSpace = ParticleSystemSimulationSpace.World;

                var wispsVol = this.soulWisps.velocityOverLifetime;
                wispsVol.enabled = true;
                wispsVol.space = ParticleSystemSimulationSpace.World;
                wispsVol.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                wispsVol.y = 0f;
                wispsVol.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

                var wispsCol = this.soulWisps.colorOverLifetime;
                wispsCol.enabled = true;
                wispsCol.color = this.BuildFadeGradient(wispColor);

                var wispsEmission = this.soulWisps.emission;
                wispsEmission.rateOverTime = 0f;
                wispsEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, (short)this.bloodWispsCount) });
                this.soulWisps.gameObject.SetActive(true);
            }
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
