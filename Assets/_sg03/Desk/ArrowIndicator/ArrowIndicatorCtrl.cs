using SaiGame.Services;
using UnityEngine;
using UnityEngine.Rendering;

namespace SG03
{
    /// <summary>A living blood-magic tether flowing from the selected card into its target.</summary>
    [AddComponentMenu("SG03/Battle/Arrow Indicator Ctrl")]
    public class ArrowIndicatorCtrl : SaiBehaviour
    {
        [Header("Serialized scene dependencies")]
        [SerializeField] private LineRenderer lineBody;
        [SerializeField] private LineRenderer targetRing;
        [SerializeField] private Material arrowMaterial;

        [Header("Spear head")]
        [SerializeField, Min(0.1f)] private float headLength = 4.2f;
        [SerializeField, Range(10f, 50f)] private float headAngle = 32f;

        [Header("Living tether")]
        [SerializeField, Min(0.1f)] private float ribbonWidth = 1.25f;
        [SerializeField, Min(0f)] private float arcHeight = 2.4f;
        [SerializeField, Min(0f)] private float swayAmount = 0.16f;
        [SerializeField, Min(0.01f)] private float revealDuration = 0.16f;

        private const int SegmentCount = 48;
        private const int HeadSegments = 12;
        private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int PathLengthId = Shader.PropertyToID("_PathLength");
        private static readonly int HeadModeId = Shader.PropertyToID("_HeadMode");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int HeadLengthId = Shader.PropertyToID("_HeadLength");
        private static readonly int RibbonScaleId = Shader.PropertyToID("_RibbonScale");
        private readonly Vector3[] bodyPoints = new Vector3[SegmentCount + HeadSegments];
        private readonly Vector3[] ringPoints = new Vector3[65];
        private MaterialPropertyBlock properties;
        private Vector3 source;
        private Vector3 destination;
        private float shownAt;
        private bool visible;
        private bool ready;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadArrowMaterial();
            this.LoadLineBody();
            this.LoadTargetRing();
            this.PrepareRenderers();
        }

        private void LoadArrowMaterial()
        {
            if (this.arrowMaterial != null) return;
#if UNITY_EDITOR
            this.arrowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/Desk/ArrowIndicator/ArrowIndicatorMat.mat");
#endif
        }

        private void LoadLineBody()
        {
            if (this.lineBody != null) return;
            Transform child = this.transform.Find("LineBody");
            if (child != null) this.lineBody = child.GetComponent<LineRenderer>();
        }

        private void LoadTargetRing()
        {
            if (this.targetRing != null) return;
            Transform child = this.transform.Find("TargetRing");
            if (child != null) this.targetRing = child.GetComponent<LineRenderer>();
        }

        private void PrepareRenderers()
        {
            this.ready = this.lineBody != null && this.targetRing != null && this.arrowMaterial != null;
            if (!this.ready)
            {
                Debug.LogError("Arrow indicator requires serialized LineBody, TargetRing and material references.", this);
                return;
            }

            this.properties = new MaterialPropertyBlock();
            this.ConfigureRenderer(this.lineBody, 10);
            this.ConfigureRenderer(this.targetRing, 9);
            this.targetRing.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            // One constant-width ribbon gives the shader uninterrupted world-distance UVs.
            // The shader shapes both the narrow tether and the swept-back spear wings.
            this.lineBody.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        }

        private void ConfigureRenderer(LineRenderer line, int order)
        {
            line.sharedMaterial = this.arrowMaterial;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.numCapVertices = 0;
            line.numCornerVertices = 2;
            line.sortingOrder = order;
            line.positionCount = 0;
        }

        private void LateUpdate()
        {
            this.AnimateArrow();
        }

        private void OnDisable()
        {
            this.ClearArrow();
        }

        /// <summary>Safe to call every frame: only a newly visible arrow restarts its reveal.</summary>
        public void Show(Vector3 from, Vector3 to)
        {
            this.gameObject.SetActive(true);
            if (!this.ready) return;
            if (!this.visible) this.shownAt = Time.unscaledTime;
            this.source = from;
            this.destination = to;
            this.visible = true;
        }

        public void UpdateTarget(Vector3 from, Vector3 to)
        {
            this.source = from;
            this.destination = to;
        }

        public void Hide()
        {
            this.ClearArrow();
            this.gameObject.SetActive(false);
        }

        private void ClearArrow()
        {
            this.visible = false;
            if (this.lineBody != null) this.lineBody.positionCount = 0;
            if (this.targetRing != null) this.targetRing.positionCount = 0;
        }

        private void AnimateArrow()
        {
            if (!this.ready || !this.visible) return;
            Vector3 delta = this.destination - this.source;
            float distance = delta.magnitude;
            if (distance < 0.1f)
            {
                this.lineBody.positionCount = 0;
                this.targetRing.positionCount = 0;
                return;
            }

            float time = Time.unscaledTime;
            Vector3 direction = delta / distance;
            Vector3 side = ComputePerpendicular(direction);
            float length = Mathf.Min(this.headLength, distance * 0.3f);
            Vector3 headStart = this.destination - direction * length;
            float pathLength = this.DrawBody(headStart, side, distance, time) + length;
            this.DrawHead(headStart);
            float width = this.ribbonWidth * Mathf.Min(1f, distance / 4f);
            float canvasWidth = Mathf.Max(width, length * Mathf.Tan(this.headAngle * Mathf.Deg2Rad));
            this.lineBody.widthMultiplier = canvasWidth;
            float circumference = this.DrawTargetRing(distance, time);
            float opacity = Mathf.SmoothStep(0f, 1f, (time - this.shownAt) / Mathf.Max(0.01f, this.revealDuration));
            this.SetShaderProperties(this.lineBody, time, pathLength, 0f, opacity, length, canvasWidth / Mathf.Max(0.01f, width));
            this.SetShaderProperties(this.targetRing, time, circumference, 2f, opacity * 0.8f);
        }

        private float DrawBody(Vector3 end, Vector3 side, float distance, float time)
        {
            float height = Mathf.Min(this.arcHeight, distance * 0.12f);
            float sway = Mathf.Min(this.swayAmount, distance * 0.015f);
            float pathLength = 0f;
            for (int index = 0; index < SegmentCount; index++)
            {
                float t = index / (float)(SegmentCount - 1);
                // Zero displacement and derivative at both ends keeps the spear aligned.
                float envelope = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f);
                Vector3 offset = Vector3.up * height + side * (Mathf.Sin(t * 12f - time * 2.8f) * sway);
                this.bodyPoints[index] = Vector3.Lerp(this.source, end, t) + offset * envelope;
                if (index > 0) pathLength += Vector3.Distance(this.bodyPoints[index - 1], this.bodyPoints[index]);
            }
            return pathLength;
        }

        private void DrawHead(Vector3 start)
        {
            for (int index = 1; index <= HeadSegments; index++)
                this.bodyPoints[SegmentCount - 1 + index] = Vector3.Lerp(start, this.destination, index / (float)HeadSegments);
            this.lineBody.positionCount = this.bodyPoints.Length;
            this.lineBody.SetPositions(this.bodyPoints);
        }

        private float DrawTargetRing(float distance, float time)
        {
            float radius = Mathf.Min(1.5f, distance * 0.2f) * (1f + Mathf.Sin(time * 3f) * 0.045f);
            for (int index = 0; index < this.ringPoints.Length; index++)
            {
                float angle = index * Mathf.PI * 2f / (this.ringPoints.Length - 1);
                this.ringPoints[index] = this.destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            this.targetRing.widthMultiplier = Mathf.Min(0.38f, radius * 0.3f);
            this.targetRing.positionCount = this.ringPoints.Length;
            this.targetRing.SetPositions(this.ringPoints);
            return radius * Mathf.PI * 2f;
        }

        private void SetShaderProperties(LineRenderer line, float time, float length, float head, float opacity,
            float spearLength = 0f, float ribbonScale = 1f)
        {
            this.properties.SetFloat(EffectTimeId, time);
            this.properties.SetFloat(PathLengthId, length);
            this.properties.SetFloat(HeadModeId, head);
            this.properties.SetFloat(OpacityId, opacity);
            this.properties.SetFloat(HeadLengthId, spearLength);
            this.properties.SetFloat(RibbonScaleId, ribbonScale);
            line.SetPropertyBlock(this.properties);
        }

        private static Vector3 ComputePerpendicular(Vector3 direction)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
            return Vector3.Cross(direction, reference).normalized;
        }
    }
}
