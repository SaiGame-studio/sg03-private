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
        [SerializeField] private Vector3 cardSurfaceBoxScale = new Vector3(7.0f, 0.2f, 10.0f);
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem soulEmbers;
        [SerializeField] private ParticleSystem soulBurst;

        public float Duration => this.duration;
        public float DamageDuration => this.damageDuration;

        public override string GetName() => "GhostDefeatVfx";

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadMainParticleSystem();
            this.LoadChildParticleSystems();
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

        protected virtual void LoadSpecializedEmitters()
        {
            this.LoadSoulEmbers();
            this.LoadSoulBurst();
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

        /// <summary>
        /// Activates a dense, full ghostly departure effect spread across the card surface on defeat.
        /// </summary>
        public void Play()
        {
            this.gameObject.SetActive(true);
            this.EnsureComponentsLoaded();
            this.ConfigureDefeatEmitters();
            this.RestartEmitters();
        }

        /// <summary>
        /// Activates a lighter, lower density ghost wisp effect spread across the card surface on damage hit.
        /// </summary>
        public void PlayDamage(float customDuration = 0.7f)
        {
            this.gameObject.SetActive(true);
            this.EnsureComponentsLoaded();
            this.ConfigureDamageEmitters(customDuration);
            this.RestartEmitters();
        }

        private void EnsureComponentsLoaded()
        {
            if (this.mainParticleSystem == null || this.childParticleSystems == null || this.childParticleSystems.Length == 0)
            {
                this.LoadComponents();
            }
        }

        private void ConfigureDefeatEmitters()
        {
            if (this.mainParticleSystem != null)
            {
                var shape = this.mainParticleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = this.cardSurfaceBoxScale;

                var main = this.mainParticleSystem.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);

                var emission = this.mainParticleSystem.emission;
                emission.rateOverTime = 40f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 25) });
            }

            if (this.soulEmbers != null)
            {
                var embersShape = this.soulEmbers.shape;
                embersShape.shapeType = ParticleSystemShapeType.Box;
                embersShape.scale = this.cardSurfaceBoxScale;

                var embersMain = this.soulEmbers.main;
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.7f);
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);

                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 60f;
                embersEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 35) });
            }

            if (this.soulBurst != null)
            {
                var burstShape = this.soulBurst.shape;
                burstShape.shapeType = ParticleSystemShapeType.Box;
                burstShape.scale = this.cardSurfaceBoxScale;

                this.soulBurst.gameObject.SetActive(true);
            }
        }

        private void ConfigureDamageEmitters(float customDuration)
        {
            if (this.mainParticleSystem != null)
            {
                var shape = this.mainParticleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = this.cardSurfaceBoxScale;

                var main = this.mainParticleSystem.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, Mathf.Min(0.6f, customDuration));
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);

                var emission = this.mainParticleSystem.emission;
                emission.rateOverTime = 12f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 6) });
            }

            if (this.soulEmbers != null)
            {
                var embersShape = this.soulEmbers.shape;
                embersShape.shapeType = ParticleSystemShapeType.Box;
                embersShape.scale = this.cardSurfaceBoxScale;

                var embersMain = this.soulEmbers.main;
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, Mathf.Min(0.5f, customDuration));
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);

                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 16f;
                embersEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });
            }

            if (this.soulBurst != null)
            {
                this.soulBurst.gameObject.SetActive(false);
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
