using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Controls the ghostly spirit particle system visual effect that rises
    /// from across the surface of a character card when it is defeated or HP is attacked.
    /// Features smooth height-based alpha fading so all skulls completely dissolve before completion.
    /// </summary>
    [AddComponentMenu("SG03/VFX/Ghost Defeat VFX Ctrl")]
    public class GhostDefeatVfxCtrl : PoolObj
    {
        [Header("VFX Settings")]
        [SerializeField] private float duration = 2.0f;
        [SerializeField] private bool useBloodRed = true;
        [SerializeField] private Vector3 cardSurfaceBoxScale = new Vector3(7.0f, 0.2f, 10.0f);

        [Header("Particle Counts")]
        [Tooltip("Number of skulls emitted per skull variant (there are 4 variants, so total skulls = count * 4)")]
        [SerializeField, Min(1)] private int skullsPerVariant = 4;
        [Tooltip("Total count of blood wisp particles")]
        [SerializeField, Min(0)] private int bloodWispsCount = 70;

        [Header("Emitters")]
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem[] ghostVariantEmitters;
        [SerializeField] private ParticleSystem soulBurst;
        [SerializeField] private ParticleSystem soulWisps;

        public float Duration => this.duration;
        public bool UseBloodRed { get => this.useBloodRed; set => this.useBloodRed = value; }
        public int SkullsPerVariant { get => this.skullsPerVariant; set => this.skullsPerVariant = Mathf.Max(1, value); }
        public int BloodWispsCount { get => this.bloodWispsCount; set => this.bloodWispsCount = Mathf.Max(0, value); }

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
            this.LoadSoulBurst();
            this.LoadSoulWisps();
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
            this.ConfigureDefeatEmitters(bloodRed);
            this.RestartEmitters();
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
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 1.7f);
                    main.startSize = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.y = new ParticleSystem.MinMaxCurve(0.8f, 1.25f);

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = this.BuildFadeGradient(skullColor);

                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 0f;

                    int initialSkulls = Mathf.Max(1, Mathf.CeilToInt(this.skullsPerVariant * 0.65f));
                    int secondarySkulls = this.skullsPerVariant - initialSkulls;
                    if (secondarySkulls > 0)
                    {
                        emission.SetBursts(new ParticleSystem.Burst[]
                        {
                            new ParticleSystem.Burst(0.0f, (short)initialSkulls),
                            new ParticleSystem.Burst(0.18f, (short)secondarySkulls)
                        });
                    }
                    else
                    {
                        emission.SetBursts(new ParticleSystem.Burst[]
                        {
                            new ParticleSystem.Burst(0.0f, (short)initialSkulls)
                        });
                    }
                    ps.gameObject.SetActive(true);
                }
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
                Color wispColor = bloodRed ? new Color(1f, 0.05f, 0.05f, 0.95f) : new Color(0.4f, 0.95f, 1f, 0.9f);
                wispsMain.startColor = wispColor;
                wispsMain.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.5f);
                wispsMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);

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
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.12f),
                    new GradientAlphaKey(1.0f, 0.55f),
                    new GradientAlphaKey(0.25f, 0.85f),
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
