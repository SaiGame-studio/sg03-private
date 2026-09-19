using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Controls the ghostly spirit particle system visual effect that rises
    /// from across the surface of a character card when it takes damage or is defeated.
    /// </summary>
    [AddComponentMenu("SG03/VFX/Ghost Defeat VFX Ctrl")]
    public class GhostDefeatVfxCtrl : PoolObj
    {
        [Header("VFX Settings")]
        [SerializeField] private float duration = 1.8f;
        [SerializeField] private float damageDuration = 1f;
        [SerializeField] private bool useBloodRed = true;
        [SerializeField] private Vector3 cardSurfaceBoxScale = new Vector3(7.0f, 0.2f, 10.0f);
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem[] ghostVariantEmitters;
        [SerializeField] private ParticleSystem soulEmbers;
        [SerializeField] private ParticleSystem soulBurst;
        [SerializeField] private ParticleSystem soulWisps;

        public float Duration => this.duration;
        public float DamageDuration => this.damageDuration;
        public bool UseBloodRed { get => this.useBloodRed; set => this.useBloodRed = value; }

        public override string GetName() => "GhostDefeatVfx";

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
            if (this.childParticleSystems != null && this.childParticleSystems.Length > 0) return;
            this.childParticleSystems = this.GetComponentsInChildren<ParticleSystem>(true);
        }

        protected virtual void LoadGhostVariantEmitters()
        {
            if (this.ghostVariantEmitters != null && this.ghostVariantEmitters.Length > 0) return;
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
            this.LoadSoulEmbers();
            this.LoadSoulBurst();
            this.LoadSoulWisps();
        }

        private void LoadSoulEmbers()
        {
            if (this.soulEmbers != null) return;
            Transform embers = this.transform.Find("SoulEmbers");
            if (embers != null) this.soulEmbers = embers.GetComponent<ParticleSystem>();
        }

        private void LoadSoulBurst()
        {
            if (this.soulBurst != null) return;
            Transform burst = this.transform.Find("SoulBurst");
            if (burst != null) this.soulBurst = burst.GetComponent<ParticleSystem>();
        }

        private void LoadSoulWisps()
        {
            if (this.soulWisps != null) return;
            Transform wisps = this.transform.Find("SoulWisps");
            if (wisps != null) this.soulWisps = wisps.GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Activates a dense, full ghostly departure effect spread across the card surface on defeat.
        /// </summary>
        public void Play() => this.Play(this.useBloodRed);

        public void Play(bool bloodRed)
        {
            this.gameObject.SetActive(true);
            this.EnsureComponentsLoaded();
            this.ConfigureDefeatEmitters(bloodRed);
            this.RestartEmitters();
        }

        /// <summary>
        /// Activates a lighter, lower density ghost wisp effect spread across the card surface on damage hit.
        /// </summary>
        public void PlayDamage(float customDuration = 0.7f) => this.PlayDamage(customDuration, this.useBloodRed);

        public void PlayDamage(float customDuration, bool bloodRed)
        {
            this.gameObject.SetActive(true);
            this.EnsureComponentsLoaded();
            this.ConfigureDamageEmitters(customDuration, bloodRed);
            this.RestartEmitters();
        }

        private void EnsureComponentsLoaded()
        {
            if (this.mainParticleSystem == null || this.childParticleSystems == null || this.childParticleSystems.Length == 0)
            {
                this.LoadComponents();
            }

            if (this.ghostVariantEmitters == null || this.ghostVariantEmitters.Length == 0)
            {
                this.LoadGhostVariantEmitters();
            }
        }

        private void ConfigureDefeatEmitters(bool bloodRed)
        {
            bool hasVariants = this.ghostVariantEmitters != null && this.ghostVariantEmitters.Length > 0;
            if (this.mainParticleSystem != null)
            {
                var mainEmission = this.mainParticleSystem.emission;
                mainEmission.enabled = !hasVariants;
            }

            Color skullColor = bloodRed ? new Color(1f, 0.95f, 0.95f, 1f) : Color.white;
            if (hasVariants)
            {
                foreach (var ps in this.ghostVariantEmitters)
                {
                    if (ps == null) continue;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = this.cardSurfaceBoxScale;

                    var main = ps.main;
                    main.startColor = skullColor;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.0f);
                    main.startSize = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.y = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);

                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 3f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 2) });
                    ps.gameObject.SetActive(true);
                }
            }

            if (this.soulEmbers != null)
            {
                var embersShape = this.soulEmbers.shape;
                embersShape.shapeType = ParticleSystemShapeType.Box;
                embersShape.scale = this.cardSurfaceBoxScale;

                var embersMain = this.soulEmbers.main;
                embersMain.startColor = bloodRed ? new Color(1f, 0.08f, 0.08f, 1f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.7f);
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);

                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 40f;
                embersEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 25) });
            }

            if (this.soulBurst != null)
            {
                var burstShape = this.soulBurst.shape;
                burstShape.shapeType = ParticleSystemShapeType.Box;
                burstShape.scale = this.cardSurfaceBoxScale;

                var burstMain = this.soulBurst.main;
                burstMain.startColor = bloodRed ? new Color(0.9f, 0.05f, 0.05f, 0.85f) : new Color(0.6f, 0.9f, 1f, 0.8f);

                this.soulBurst.gameObject.SetActive(true);
            }

            if (this.soulWisps != null)
            {
                var wispsShape = this.soulWisps.shape;
                wispsShape.shapeType = ParticleSystemShapeType.Box;
                wispsShape.scale = this.cardSurfaceBoxScale;

                var wispsMain = this.soulWisps.main;
                wispsMain.startColor = bloodRed ? new Color(1f, 0.05f, 0.05f, 0.95f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                wispsMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);

                var wispsEmission = this.soulWisps.emission;
                wispsEmission.rateOverTime = 30f;
                wispsEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 15) });
                this.soulWisps.gameObject.SetActive(true);
            }
        }

        private void ConfigureDamageEmitters(float customDuration, bool bloodRed)
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
                    shape.scale = this.cardSurfaceBoxScale;

                    var main = ps.main;
                    main.startColor = skullColor;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, Mathf.Min(0.7f, customDuration));
                    main.startSize = new ParticleSystem.MinMaxCurve(2.0f, 2.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.y = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);

                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 1f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });
                    ps.gameObject.SetActive(true);
                }
            }

            if (this.soulEmbers != null)
            {
                var embersShape = this.soulEmbers.shape;
                embersShape.shapeType = ParticleSystemShapeType.Box;
                embersShape.scale = this.cardSurfaceBoxScale;

                var embersMain = this.soulEmbers.main;
                embersMain.startColor = bloodRed ? new Color(1f, 0.1f, 0.1f, 1f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, Mathf.Min(0.5f, customDuration));
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);

                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 16f;
                embersEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });
            }

            if (this.soulBurst != null)
            {
                this.soulBurst.gameObject.SetActive(false);
            }

            if (this.soulWisps != null)
            {
                var wispsShape = this.soulWisps.shape;
                wispsShape.shapeType = ParticleSystemShapeType.Box;
                wispsShape.scale = this.cardSurfaceBoxScale;

                var wispsMain = this.soulWisps.main;
                wispsMain.startColor = bloodRed ? new Color(1f, 0.08f, 0.08f, 0.9f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                wispsMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);

                var wispsEmission = this.soulWisps.emission;
                wispsEmission.rateOverTime = 12f;
                wispsEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 5) });
                this.soulWisps.gameObject.SetActive(true);
            }
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

        /// <summary>
        /// Stops all particle emission and clears active particles.
        /// </summary>
        public void Stop()
        {
            if (this.childParticleSystems != null && this.childParticleSystems.Length > 0)
            {
                foreach (ParticleSystem ps in this.childParticleSystems)
                {
                    if (ps == null) continue;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                return;
            }

            if (this.mainParticleSystem != null)
            {
                this.mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// Returns this object to the pool or disables it when unpooled.
        /// </summary>
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
