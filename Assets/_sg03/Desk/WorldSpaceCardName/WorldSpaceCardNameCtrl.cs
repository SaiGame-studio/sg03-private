using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>Drives the world-space Card Name value displayed at the center of a card.</summary>
    [AddComponentMenu("SG03/BattleState/World Space Card Name UI")]
    public sealed class WorldSpaceCardNameCtrl : PoolObj
    {
        [Header("Required runtime references")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Display")]
        [SerializeField] private bool faceMainCamera = true;
        [Tooltip("Distance beyond the card's center surface where the Card Name UI is displayed.")]
        [SerializeField, Min(0f)] private float aboveCardYOffset = 0.35f;
        [Tooltip("Global Z-axis offset relative to the card's center.")]
        [SerializeField] private float cardZOffset = -0.5f;

        [Header("Parenting")]
        [SerializeField] private Card3DCtrl cardCtrl;
        private Label cardNameLabel;
        private Vector3 baseWorldRotation;
        private bool hasBaseWorldRotation;
        private Coroutine deferredUiRefreshRoutine;
        [SerializeField] private string cardDisplayName = string.Empty;

        private void LateUpdate()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void OnEnable()
        {
            this.HandleEnabled();
        }

        private void OnDisable()
        {
            this.HandleDisabled();
        }

        protected override void Start()
        {
            this.InitializeCardNameUi();
        }

        private void InitializeCardNameUi()
        {
            this.RefreshUi();
        }

        private void HandleEnabled()
        {
            this.RefreshUi();
            this.RefreshUiWhenDocumentIsReady();
        }

        private void HandleDisabled()
        {
            this.deferredUiRefreshRoutine = null;
        }

        private void RefreshUiWhenDocumentIsReady()
        {
            if (this.deferredUiRefreshRoutine != null) return;
            this.deferredUiRefreshRoutine = this.StartCoroutine(this.RefreshUiWhenDocumentIsReadyRoutine());
        }

        private IEnumerator RefreshUiWhenDocumentIsReadyRoutine()
        {
            yield return null;
            this.deferredUiRefreshRoutine = null;
            if (!this.isActiveAndEnabled) yield break;
            this.RefreshUi();
        }

        public override string GetName()
        {
            return nameof(WorldSpaceCardNameCtrl);
        }

        /// <summary>Sets the world-space position before this UI is assigned a card.</summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>Assigns the card this UI follows without inheriting its transform.</summary>
        public void SetCard(Card3DCtrl newCard)
        {
            if (newCard == null) return;

            this.cardCtrl = newCard;
            this.UpdateWorldPositionFromCard();
            this.baseWorldRotation = this.transform.eulerAngles;
            this.hasBaseWorldRotation = true;
            this.SetCardName(this.cardCtrl.GetCardDisplayName());
        }

        /// <summary>Clears the card currently shown in this pooled UI's Inspector.</summary>
        public void ClearCard()
        {
            this.cardCtrl = null;
        }

        /// <summary>Sets the card name currently shown by this UI.</summary>
        public void SetCardName(string displayName)
        {
            this.cardDisplayName = displayName ?? string.Empty;
            this.RefreshUi();
        }

        private void UpdateWorldSpacePresentation()
        {
            this.UpdateWorldPositionFromCard();
            this.FaceMainCamera();
        }

        private void UpdateWorldPositionFromCard()
        {
            if (this.cardCtrl == null) return;

            float zOffset = this.cardZOffset;
            if (this.cardCtrl.CardOwner == Owner.omega)
            {
                zOffset = -this.cardZOffset;
            }
            Vector3 offset = new Vector3(0f, this.aboveCardYOffset, zOffset);

            Card3D card = this.cardCtrl.GetComponent<Card3D>();
            if (card != null && card.TryGetTopEdgeWorldPosition(out Vector3 topEdge) && card.TryGetStatsCenterWorldPosition(out Vector3 statsCenter))
            {
                Vector3 middle = (topEdge + statsCenter) * 0.5f;
                this.transform.position = middle + offset;
                return;
            }

            this.transform.position = this.cardCtrl.transform.position + offset;
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;

            Vector3 directionFromCamera = this.transform.position - Camera.main.transform.position;
            if (directionFromCamera.sqrMagnitude < Mathf.Epsilon) return;

            float horizontalDistance = new Vector2(directionFromCamera.x, directionFromCamera.z).magnitude;
            Vector3 worldRotation = this.hasBaseWorldRotation
                ? this.baseWorldRotation
                : this.transform.eulerAngles;
            worldRotation.x = -Mathf.Atan2(directionFromCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(worldRotation);
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadUiDocument();
            this.BindUi();
        }

        private void LoadUiDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
        }

        private void BindUi()
        {
            if (this.uiDocument == null)
            {
                this.LoadUiDocument();
            }

            if (this.uiDocument == null)
            {
                Debug.LogWarning($"{this.name}: UIDocument is missing.", this.gameObject);
                return;
            }

            VisualElement root = this.uiDocument.rootVisualElement;
            if (root == null) return;

            this.cardNameLabel = root.Q<Label>("CardNameLabel");
            if (this.cardNameLabel == null)
            {
                Debug.LogWarning($"{this.name}: UI element 'CardNameLabel' is missing.", this.gameObject);
            }
        }

        public void RefreshUi()
        {
            this.BindUi();
            if (this.cardNameLabel != null)
            {
                this.cardNameLabel.text = this.cardDisplayName;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            this.RefreshUi();
        }
#endif
    }
}
