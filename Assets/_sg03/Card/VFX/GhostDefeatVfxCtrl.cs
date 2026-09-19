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
        [SerializeField] private Vector3 cardSurfaceBoxScale = new Vector3(7.0f, 0.2f, 10.0f);

        [Header("Skull Settings")]
        [Tooltip("Number of skulls emitted per skull variant (there are 4 variants, so total skulls = count * 4)")]
        [SerializeField, Min(1)] private int skullsPerVariant = 4;
        [Tooltip("Min and Max lifetime of skull particles (seconds)")]
        [SerializeField] private Vector2 skullLifetime = new Vector2(1.3f, 1.6f);
        [Tooltip("Upward ascent velocity of skulls (Y axis in world space)")]
        [SerializeField] private Vector2 skullAscentSpeed = new Vector2(1.6f, 2.3f);
        [Tooltip("Time window over which skulls randomly emerge from the card (seconds)")]
        [SerializeField, Min(0f)] private float skullEmergenceWindow = 0.45f;

        [Header("Blood Wisp Settings")]
        [SerializeField] private bool enableBloodWisps = false;
        [SerializeField] private bool useBloodRed = true;
        [Tooltip("Total count of blood wisp particles")]
        [SerializeField, Min(0)] private int bloodWispsCount = 40;
        [Tooltip("Min and Max lifetime of blood wisps (seconds, shorter than skulls)")]
        [SerializeField] private Vector2 wispLifetime = new Vector2(0.5f, 0.8f);
        [Tooltip("Upward ascent velocity of blood wisps (slower than skulls so they don't fly as high)")]
        [SerializeField] private Vector2 wispAscentSpeed = new Vector2(0.7f, 1.2f);
        [Tooltip("Min and Max particle size of blood wisps")]
        [SerializeField] private Vector2 wispSize = new Vector2(0.2f, 0.7f);
        [Tooltip("Time window over which blood wisps randomly emerge from the card (seconds)")]
        [SerializeField, Min(0f)] private float wispEmergenceWindow = 0.1f;

        [Header("Blood Mist Settings")]
        [SerializeField] private bool enableBloodMist = true;
        [Tooltip("Total count of blood mist particles")]
        [SerializeField, Min(0)] private int bloodMistCount = 18;
        [Tooltip("Min and Max lifetime of blood mist (seconds)")]
        [SerializeField] private Vector2 mistLifetime = new Vector2(1.6f, 2.2f);
        [Tooltip("Min and Max start size of blood mist particles (broad, wide puffs)")]
        [SerializeField] private Vector2 mistStartSize = new Vector2(5.5f, 8.0f);
        [Tooltip("Gentle upward ascent velocity of blood mist (bay len 1 chut)")]
        [SerializeField] private Vector2 mistRiseSpeed = new Vector2(0.6f, 1.2f);
        [Tooltip("Horizontal outward diffusion speed along X axis (phat trien rong ra o truc x)")]
        [SerializeField, Min(0f)] private float mistSpreadSpeed = 4.8f;
        [Tooltip("Peak opacity of mist (ro rang va mo ao)")]
        [SerializeField, Range(0.05f, 1f)] private float mistMaxAlpha = 0.78f;
        [Tooltip("Time window over which mist particles randomly emerge (seconds)")]
        [SerializeField, Min(0f)] private float mistEmergenceWindow = 0.35f;

        [Header("Emitters")]
        [SerializeField] private ParticleSystem mainParticleSystem;
        [SerializeField] private ParticleSystem[] childParticleSystems;
        [SerializeField] private ParticleSystem[] ghostVariantEmitters;
        [SerializeField] private ParticleSystem soulBurst;
        [SerializeField] private ParticleSystem soulWisps;
        [SerializeField] private ParticleSystem bloodMist;

        public float Duration => this.duration;
        public bool EnableBloodWisps { get => this.enableBloodWisps; set => this.enableBloodWisps = value; }
        public bool UseBloodRed { get => this.useBloodRed; set => this.useBloodRed = value; }
        public int SkullsPerVariant { get => this.skullsPerVariant; set => this.skullsPerVariant = Mathf.Max(1, value); }
        public Vector2 SkullLifetime { get => this.skullLifetime; set => this.skullLifetime = value; }
        public Vector2 SkullAscentSpeed { get => this.skullAscentSpeed; set => this.skullAscentSpeed = value; }
        public float SkullEmergenceWindow { get => this.skullEmergenceWindow; set => this.skullEmergenceWindow = Mathf.Max(0f, value); }

        public int BloodWispsCount { get => this.bloodWispsCount; set => this.bloodWispsCount = Mathf.Max(0, value); }
        public Vector2 WispLifetime { get => this.wispLifetime; set => this.wispLifetime = value; }
        public Vector2 WispAscentSpeed { get => this.wispAscentSpeed; set => this.wispAscentSpeed = value; }
        public Vector2 WispSize { get => this.wispSize; set => this.wispSize = value; }
        public float WispEmergenceWindow { get => this.wispEmergenceWindow; set => this.wispEmergenceWindow = Mathf.Max(0f, value); }

        public bool EnableBloodMist { get => this.enableBloodMist; set => this.enableBloodMist = value; }
        public int BloodMistCount { get => this.bloodMistCount; set => this.bloodMistCount = Mathf.Max(0, value); }
        public Vector2 MistLifetime { get => this.mistLifetime; set => this.mistLifetime = value; }
        public Vector2 MistStartSize { get => this.mistStartSize; set => this.mistStartSize = value; }
        public Vector2 MistRiseSpeed { get => this.mistRiseSpeed; set => this.mistRiseSpeed = value; }
        public float MistSpreadSpeed { get => this.mistSpreadSpeed; set => this.mistSpreadSpeed = Mathf.Max(0f, value); }
        public float MistMaxAlpha { get => this.mistMaxAlpha; set => this.mistMaxAlpha = Mathf.Clamp01(value); }
        public float MistEmergenceWindow { get => this.mistEmergenceWindow; set => this.mistEmergenceWindow = Mathf.Max(0f, value); }
        public ParticleSystem BloodMist => this.bloodMist;

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
            this.LoadBloodMist();
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

        private void LoadBloodMist()
        {
            if (this.bloodMist != null) return;
            Transform mist = this.transform.Find("BloodMist");
            if (mist != null) this.bloodMist = mist.GetComponent<ParticleSystem>();
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
                    shape.scale = new Vector3(this.cardSurfaceBoxScale.x, this.cardSurfaceBoxScale.z, this.cardSurfaceBoxScale.y);
                    shape.rotation = new Vector3(-90f, 0f, 0f);

                    var main = ps.main;
                    main.startColor = skullColor;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(this.skullLifetime.x, this.skullLifetime.y);
                    main.startSize = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    main.gravityModifier = 0f;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;

                    var vol = ps.velocityOverLifetime;
                    vol.enabled = true;
                    vol.space = ParticleSystemSimulationSpace.World;
                    vol.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
                    vol.y = new ParticleSystem.MinMaxCurve(this.skullAscentSpeed.x, this.skullAscentSpeed.y);
                    vol.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = this.BuildFadeGradient(skullColor);

                    var emission = ps.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(this.BuildRandomSkullBursts(this.skullsPerVariant, this.skullEmergenceWindow));
                    ps.gameObject.SetActive(true);
                }
            }

            if (this.soulBurst != null)
            {
                var burstShape = this.soulBurst.shape;
                burstShape.shapeType = ParticleSystemShapeType.Box;
                burstShape.scale = new Vector3(this.cardSurfaceBoxScale.x, this.cardSurfaceBoxScale.z, this.cardSurfaceBoxScale.y);
                burstShape.rotation = new Vector3(-90f, 0f, 0f);

                var burstMain = this.soulBurst.main;
                burstMain.startColor = bloodRed ? new Color(0.9f, 0.05f, 0.05f, 0.85f) : new Color(0.6f, 0.9f, 1f, 0.8f);

                this.soulBurst.gameObject.SetActive(true);
            }

            if (this.soulWisps != null)
            {
                if (!this.enableBloodWisps)
                {
                    this.soulWisps.gameObject.SetActive(false);
                }
                else
                {
                    var wispsShape = this.soulWisps.shape;
                    wispsShape.shapeType = ParticleSystemShapeType.Box;
                    wispsShape.scale = new Vector3(this.cardSurfaceBoxScale.x, this.cardSurfaceBoxScale.z, this.cardSurfaceBoxScale.y);
                    wispsShape.rotation = new Vector3(-90f, 0f, 0f);

                    var wispsMain = this.soulWisps.main;
                    Color wispColor = bloodRed ? new Color(1f, 0.05f, 0.05f, 1.0f) : new Color(0.4f, 0.95f, 1f, 0.95f);
                    wispsMain.startColor = wispColor;
                    wispsMain.startLifetime = new ParticleSystem.MinMaxCurve(this.wispLifetime.x, this.wispLifetime.y);
                    wispsMain.startSize = new ParticleSystem.MinMaxCurve(this.wispSize.x, this.wispSize.y);
                    wispsMain.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    wispsMain.gravityModifier = 0f;
                    wispsMain.simulationSpace = ParticleSystemSimulationSpace.World;

                    var wispsVol = this.soulWisps.velocityOverLifetime;
                    wispsVol.enabled = true;
                    wispsVol.space = ParticleSystemSimulationSpace.World;
                    wispsVol.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
                    wispsVol.y = new ParticleSystem.MinMaxCurve(this.wispAscentSpeed.x, this.wispAscentSpeed.y);
                    wispsVol.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

                    var wispsCol = this.soulWisps.colorOverLifetime;
                    wispsCol.enabled = true;
                    wispsCol.color = this.BuildFadeGradient(wispColor);

                    var wispsEmission = this.soulWisps.emission;
                    wispsEmission.rateOverTime = 0f;
                    wispsEmission.SetBursts(this.BuildRandomWispBursts(this.bloodWispsCount, this.wispEmergenceWindow));
                    this.soulWisps.gameObject.SetActive(true);
                }
            }

            this.ConfigureBloodMist(bloodRed);
        }

        private void ConfigureBloodMist(bool bloodRed)
        {
            if (this.bloodMist == null) return;
            if (!this.enableBloodMist)
            {
                this.bloodMist.gameObject.SetActive(false);
                return;
            }

            var mistRend = this.bloodMist.GetComponent<ParticleSystemRenderer>();
            if (mistRend != null)
            {
                mistRend.sortingOrder = 2;
                mistRend.maxParticleSize = 3.0f;
            }

            this.bloodMist.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            var mistShape = this.bloodMist.shape;
            mistShape.shapeType = ParticleSystemShapeType.Box;
            mistShape.scale = new Vector3(this.cardSurfaceBoxScale.x * 0.8f, this.cardSurfaceBoxScale.z * 0.8f, this.cardSurfaceBoxScale.y);
            mistShape.rotation = new Vector3(-90f, 0f, 0f);

            var mistMain = this.bloodMist.main;
            Color mistColor = bloodRed ? new Color(0.92f, 0.05f, 0.05f, 1f) : new Color(0.45f, 0.85f, 1f, 1f);
            mistMain.startColor = mistColor;
            mistMain.startLifetime = new ParticleSystem.MinMaxCurve(this.mistLifetime.x, this.mistLifetime.y);
            mistMain.startSize = new ParticleSystem.MinMaxCurve(this.mistStartSize.x, this.mistStartSize.y);
            mistMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            mistMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            mistMain.gravityModifier = 0f;
            mistMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var mistVol = this.bloodMist.velocityOverLifetime;
            mistVol.enabled = true;
            mistVol.space = ParticleSystemSimulationSpace.World;
            mistVol.x = new ParticleSystem.MinMaxCurve(-this.mistSpreadSpeed, this.mistSpreadSpeed);
            mistVol.y = new ParticleSystem.MinMaxCurve(this.mistRiseSpeed.x, this.mistRiseSpeed.y);
            mistVol.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

            var mistSol = this.bloodMist.sizeOverLifetime;
            mistSol.enabled = true;
            mistSol.separateAxes = true;

            AnimationCurve sizeXCurve = new AnimationCurve();
            sizeXCurve.AddKey(0f, 1.0f);
            sizeXCurve.AddKey(0.35f, 3.2f);
            sizeXCurve.AddKey(0.75f, 5.0f);
            sizeXCurve.AddKey(1f, 6.0f);
            mistSol.x = new ParticleSystem.MinMaxCurve(1f, sizeXCurve);

            AnimationCurve sizeYCurve = new AnimationCurve();
            sizeYCurve.AddKey(0f, 1.0f);
            sizeYCurve.AddKey(0.35f, 2.2f);
            sizeYCurve.AddKey(0.75f, 3.4f);
            sizeYCurve.AddKey(1f, 4.0f);
            mistSol.y = new ParticleSystem.MinMaxCurve(1f, sizeYCurve);
            mistSol.z = new ParticleSystem.MinMaxCurve(1f, sizeYCurve);

            var mistCol = this.bloodMist.colorOverLifetime;
            mistCol.enabled = true;
            mistCol.color = this.BuildMistFadeGradient(mistColor, this.mistMaxAlpha);

            var mistEmission = this.bloodMist.emission;
            mistEmission.enabled = true;
            mistEmission.rateOverTime = 0f;
            mistEmission.SetBursts(this.BuildRandomBursts(this.bloodMistCount, this.mistEmergenceWindow));
            this.bloodMist.gameObject.SetActive(true);
        }

        private ParticleSystem.MinMaxGradient BuildMistFadeGradient(Color baseColor, float peakAlpha)
        {
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseColor, 0f), new GradientColorKey(baseColor, 1f) },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(peakAlpha, 0.12f),
                    new GradientAlphaKey(peakAlpha * 0.80f, 0.65f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            return new ParticleSystem.MinMaxGradient(grad);
        }

        private ParticleSystem.Burst[] BuildRandomBursts(int totalCount, float windowDuration)
        {
            if (totalCount <= 0) return new ParticleSystem.Burst[0];
            int burstCount = Mathf.Clamp(totalCount / 3, 2, 6);
            var bursts = new ParticleSystem.Burst[burstCount];
            float[] times = new float[burstCount];
            for (int i = 0; i < burstCount; i++)
            {
                times[i] = Random.Range(0.0f, windowDuration);
            }
            System.Array.Sort(times);
            int remaining = totalCount;
            for (int i = 0; i < burstCount; i++)
            {
                int count = (i == burstCount - 1) ? remaining : Mathf.Max(1, remaining / (burstCount - i));
                remaining -= count;
                bursts[i] = new ParticleSystem.Burst(times[i], (short)count, (short)count);
            }
            return bursts;
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
            int burstCount = Mathf.Clamp(totalWisps / 9, 3, 8);
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
                    new GradientAlphaKey(1.0f, 0.55f),
                    new GradientAlphaKey(0.35f, 0.85f),
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
