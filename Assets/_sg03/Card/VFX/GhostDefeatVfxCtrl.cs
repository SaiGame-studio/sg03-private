using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Controls the ghostly spirit particle system visual effect that rises
    /// from a character card when it takes damage or is defeated in battle.
    /// </summary>
    [AddComponentMenu("SG03/VFX/Ghost Defeat VFX Ctrl")]
    public class GhostDefeatVfxCtrl : PoolObj
    {
        [Header("VFX Settings")]
        [SerializeField] private float duration = 1.8f;
        [SerializeField] private float damageDuration = 0.7f;
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
        /// Activates the full ghostly departure effect when a character is defeated.
        /// </summary>
        public void Play()
        {
            this.gameObject.SetActive(true);
            this.ConfigureDefeatEmitters();
            this.RestartEmitters();
        }

        /// <summary>
        /// Activates a lighter, shorter ghost wisp effect when a character takes non-lethal damage.
        /// </summary>
        public void PlayDamage(float customDuration = 0.7f)
        {
            this.gameObject.SetActive(true);
            this.ConfigureDamageEmitters(customDuration);
            this.RestartEmitters();
        }

        private void ConfigureDefeatEmitters()
        {
            if (this.mainParticleSystem != null)
            {
                var main = this.mainParticleSystem.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
                var emission = this.mainParticleSystem.emission;
                emission.rateOverTime = 12f;
            }

            if (this.soulEmbers != null)
            {
                var embersMain = this.soulEmbers.main;
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 18f;
            }

            if (this.soulBurst != null)
            {
                this.soulBurst.gameObject.SetActive(true);
            }
        }

        private void ConfigureDamageEmitters(float customDuration)
        {
            if (this.mainParticleSystem != null)
            {
                var main = this.mainParticleSystem.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, Mathf.Min(0.6f, customDuration));
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                var emission = this.mainParticleSystem.emission;
                emission.rateOverTime = 5f;
            }

            if (this.soulEmbers != null)
            {
                var embersMain = this.soulEmbers.main;
                embersMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, Mathf.Min(0.5f, customDuration));
                embersMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                var embersEmission = this.soulEmbers.emission;
                embersEmission.rateOverTime = 7f;
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
