using System;
using System.Collections;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Base controller that owns pre-wired references to <see cref="Card3D"/> and
    /// <see cref="CardLoader"/> on the same GameObject.
    /// Extend this class when a controller only needs card data and loading,
    /// without the review-movement functionality of <see cref="Card3DReviewCtrl"/>.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D Ctrl")]
    [RequireComponent(typeof(Card3D))]
    [RequireComponent(typeof(CardLoader))]
    [RequireComponent(typeof(CardMovement))]
    public partial class Card3DCtrl : PoolObj
    {
        // ─── Static card events ───────────────────────────────────────────────────

        public static event Action<Card3DCtrl> HoverEntered;
        public static event Action<Card3DCtrl> HoverExited;
        public static event Action<Card3DCtrl> CardSelected;
        public static event Action<Card3DCtrl, bool> FaceStateChanged;
        public static event Action<Card3DCtrl, bool> TriggerStateChanged;
        public static event Action<Card3DCtrl, Location> LocationChanged;

        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3D card;
        [SerializeField] private CardLoader loader;
        [SerializeField] private CardMovement movement;
        [SerializeField] private CardFullDetailManipulator fullDetailManipulator;

        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private Owner              cardOwner;
        [SerializeField] private string             codeName;
        [SerializeField] private string             inventoryItemId;
        [SerializeField] private CardDefinitionData definition;
        [SerializeField] private bool               expose;
        [SerializeField] private bool               isTrigger;
        [SerializeField] private bool               isHover;
        [SerializeField] private Card3DCtrl         attacker;
        private int runtimeAtk = -1;
        private bool isFullDetail;
        private bool showFinalDefOnlyOnHover;

        // ─── Optional external references ─────────────────────────────────────────

        [Header("Optional References")]
        [SerializeField] private CardHolderCtrl cardHolder;
        [SerializeField] private ObjectPool objectPool;
        [SerializeField] private WorldSpaceHpBarCtrl hpBarPrefab;
        [SerializeField] private WorldSpaceHpBarCtrl hpBarInstance;
        [SerializeField] private WorldSpaceAtkCtrl atkUiPrefab;
        [SerializeField] private WorldSpaceAtkCtrl atkUiInstance;
        private ClientActions clientActions;
        private BattleStateCtrl battleStateCtrl;
        private Coroutine spawnHpBarRoutine;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        public override string GetName() => this.name;

        public void NotifyHoverEntered()
        {
            if (!this.isHover)
                this.showFinalDefOnlyOnHover = !this.IsHpBarVisible();

            this.isHover = true;
            this.RefreshHpBarVisibility();
            this.RefreshHpBarDisplayMode();
            HoverEntered?.Invoke(this);
        }

        public void NotifyHoverExited()
        {
            this.isHover = false;
            this.showFinalDefOnlyOnHover = false;
            this.RefreshHpBarVisibility();
            this.RefreshHpBarDisplayMode();
            HoverExited?.Invoke(this);
        }
        public void NotifySelected()     => CardSelected?.Invoke(this);
        public static void NotifyDeselected() => CardSelected?.Invoke(null);
        public void NotifyLocationChanged(Location newLocation)
        {
            if (newLocation == Location.in_void) this.DespawnStatUis();
            LocationChanged?.Invoke(this, newLocation);
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3D();
            this.LoadCardLoader();
            this.LoadCardMovement();
            this.LoadFullDetailManipulator();
            this.LoadObjectPool();
            this.LoadBattleStateCtrl();
        }

        protected virtual void OnDisable()
        {
            this.SetAttacker(null);
            this.DespawnStatUis();
        }

        protected virtual void LoadCard3D()
        {
            if (this.card != null) return;
            this.card = this.GetComponent<Card3D>();
            Debug.LogWarning(transform.name + "LoadCard3D", gameObject);
        }

        protected virtual void LoadCardLoader()
        {
            if (this.loader != null) return;
            this.loader = this.GetComponent<CardLoader>();
            Debug.LogWarning(transform.name + "LoadCardLoader", gameObject);
        }

        protected virtual void LoadCardMovement()
        {
            if (this.movement != null) return;
            this.movement = this.GetComponent<CardMovement>();
            Debug.LogWarning(transform.name + "LoadCardMovement", gameObject);
        }

        protected virtual void LoadFullDetailManipulator()
        {
            if (this.fullDetailManipulator != null) return;
            this.fullDetailManipulator = this.GetComponent<CardFullDetailManipulator>();
        }

        protected virtual void LoadObjectPool()
        {
            if (this.objectPool != null) return;
            this.objectPool = GameObject.FindAnyObjectByType<ObjectPool>();
        }

        private void SpawnHpBarAt(CardHolderCtrl holder)
        {
            if (holder == null || holder != this.cardHolder) return;
            if (this.IsBattleResuming())
            {
                this.DespawnHpBar();
                return;
            }

            if (!this.ShouldShowHpBar(holder))
            {
                this.DespawnHpBar();
                return;
            }

            this.EnsureSingleHpBarInstance();
            this.LoadObjectPool();
            if (this.objectPool == null || this.objectPool.PoolPrefabs == null)
            {
                Debug.LogWarning($"{this.name}: ObjectPool is not ready for the HP bar.", this);
                return;
            }

            if (this.hpBarPrefab == null)
            {
                this.hpBarPrefab = this.objectPool.PoolPrefabs.GetByName("HpBarUI") as WorldSpaceHpBarCtrl;
            }

            if (this.hpBarPrefab == null)
            {
                Debug.LogWarning($"{this.name}: HpBarUI is missing from ObjectPoolPrefabs.", this);
                return;
            }

            if (this.hpBarInstance == null)
            {
                this.hpBarInstance = this.objectPool.SpawnInactive(this.hpBarPrefab, Vector3.zero);
            }

            if (this.hpBarInstance == null) return;

            this.hpBarInstance.SetPosition(holder.transform.position);
            this.hpBarInstance.SetCard(this);
            this.hpBarInstance.gameObject.SetActive(true);
            this.UpdateDamagePreviewFromAttacker();
            this.RefreshHpBarDisplayMode();
        }

        private void SpawnAtkUiAt(CardHolderCtrl holder)
        {
            if (holder == null || holder != this.cardHolder) return;
            this.SpawnAtkUi();
        }

        /// <summary>Spawns and displays the world-space ATK UI for this card.</summary>
        public void SpawnAtkUi()
        {
            int attack = this.GetDamagePreviewAttack();
            if (attack == 0)
            {
                this.DespawnAtkUi();
                return;
            }

            this.EnsureSingleAtkUiInstance();
            this.LoadObjectPool();
            if (this.objectPool == null || this.objectPool.PoolPrefabs == null)
            {
                Debug.LogWarning($"{this.name}: ObjectPool is not ready for the ATK UI.", this);
                return;
            }

            if (this.atkUiPrefab == null)
            {
                this.atkUiPrefab = this.objectPool.PoolPrefabs.GetByName("AtkUI") as WorldSpaceAtkCtrl;
            }

            if (this.atkUiPrefab == null)
            {
                Debug.LogWarning($"{this.name}: AtkUI is missing from ObjectPoolPrefabs.", this);
                return;
            }

            if (this.atkUiInstance == null)
            {
                this.atkUiInstance = this.objectPool.SpawnInactive(this.atkUiPrefab, Vector3.zero);
            }

            if (this.atkUiInstance == null) return;

            Vector3 pos = this.cardHolder != null ? this.cardHolder.transform.position : this.transform.position;
            this.atkUiInstance.SetPosition(pos);
            this.atkUiInstance.SetCard(this);
            this.atkUiInstance.SetAttack(attack);
            this.atkUiInstance.gameObject.SetActive(true);
        }

        private bool ShouldShowHpBar(CardHolderCtrl holder)
        {
            if (this.isFullDetail) return false;
            if (!this.IsCharacter()) return false;
            // Hover and accumulated damage deliberately override turn visibility.
            // A damaged character must keep its HP bar visible until the resolved
            // turn reset clears total_damage_received.
            if (this.isHover) return true;
            if (this.HasAccumulatedDamage()) return true;
            if (this.ShouldHideHpBarForCurrentTurn()) return false;
            if (this.cardOwner == Owner.alpha) return holder != null && holder.HolderLink == Link.front;
            return this.cardOwner == Owner.omega && this.expose && this.FaceState == FaceState.FaceUp;
        }

        private bool ShouldHideHpBarForCurrentTurn()
        {
            this.LoadClientActions();
            return this.clientActions != null && this.clientActions.HpBarHiddenOwner == this.cardOwner;
        }

        private bool ShouldShowAtkUi(CardHolderCtrl holder)
        {
            if (this.isFullDetail) return false;
            if (!this.IsCharacter() && !this.HasAddedAttack()) return false;
            if (this.GetDamagePreviewAttack() == 0) return false;
            if (this.Location == Location.in_hand || this.Location == Location.in_void) return false;
            if (this.ShouldHideAtkUiForCurrentTurn()) return false;
            if (this.cardOwner == Owner.alpha) return holder != null && holder.HolderLink == Link.front;
            return this.cardOwner == Owner.omega && this.expose && this.FaceState == FaceState.FaceUp;
        }

        private bool HasAddedAttack()
        {
            return this.definition != null
                && this.definition.TryGetBaseStat("atk_added", out _);
        }

        private bool ShouldHideAtkUiForCurrentTurn()
        {
            this.LoadClientActions();
            if (this.clientActions == null || !this.clientActions.HpBarHiddenOwner.HasValue) return false;
            return this.clientActions.HpBarHiddenOwner.Value != this.cardOwner;
        }

        private bool HasAccumulatedDamage()
        {
            BattleCardSlot slot = this.GetBattleSlot();
            return slot != null && slot.total_damage_received > 0;
        }

        private BattleCardSlot GetBattleSlot()
        {
            this.LoadBattleStateCtrl();
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null || string.IsNullOrEmpty(this.inventoryItemId)) return null;

            return this.FindBattleSlot(state.AlphaHand)
                ?? this.FindBattleSlot(state.AlphaFrontLine)
                ?? this.FindBattleSlot(state.AlphaBackLine)
                ?? this.FindBattleSlot(state.AlphaTheVoid)
                ?? this.FindBattleSlot(state.AlphaTheSource)
                ?? this.FindBattleSlot(state.OmegaHand)
                ?? this.FindBattleSlot(state.OmegaFrontLine)
                ?? this.FindBattleSlot(state.OmegaBackLine)
                ?? this.FindBattleSlot(state.OmegaTheVoid);
        }

        private BattleCardSlot FindBattleSlot(BattleCardSlot[] slots)
        {
            if (slots == null) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot != null && slot.inventory_item_id == this.inventoryItemId) return slot;
            }

            return null;
        }

        private void RefreshHpBarVisibility()
        {
            if (this.IsBattleResuming())
            {
                this.DespawnStatUis();
                return;
            }

            this.RefreshHpBarOnlyVisibility();
            this.RefreshAtkUiVisibility();
        }

        private void RefreshHpBarOnlyVisibility()
        {
            if (!this.ShouldShowHpBar(this.cardHolder))
            {
                this.DespawnHpBar();
                return;
            }

            if (this.cardHolder == null) return;
            if (this.IsAnimating)
            {
                this.SpawnHpBarAfterCardSettles(this.cardHolder);
                return;
            }

            this.SpawnHpBarAt(this.cardHolder);
        }

        /// <summary>Refreshes ATK UI visibility according to turn and placement rules.</summary>
        public void RefreshAtkUiVisibility()
        {
            if (this.IsBattleResuming())
            {
                this.DespawnAtkUi();
                return;
            }

            if (!this.ShouldShowAtkUi(this.cardHolder))
            {
                this.DespawnAtkUi();
                return;
            }

            this.SpawnAtkUi();
        }

        private bool IsBattleResuming()
        {
            this.LoadClientActions();
            return this.clientActions != null && this.clientActions.IsResuming;
        }

        private void LoadClientActions()
        {
            if (this.clientActions != null) return;
            this.LoadBattleStateCtrl();
            this.clientActions = this.battleStateCtrl?.ClientActions;
            if (this.clientActions == null)
                this.clientActions = FindAnyObjectByType<ClientActions>();
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = FindAnyObjectByType<BattleStateCtrl>();
        }

        /// <summary>Re-evaluates the HP bar after a resume action sequence is complete.</summary>
        public void RefreshHpBarAfterResume()
        {
            this.RefreshHpBarVisibility();
        }

        /// <summary>Refreshes this card's active HP bar after either side ends a turn.</summary>
        public void RefreshHpBarAfterTurnEnd()
        {
            this.hpBarInstance?.RefreshHealthFromTurnEnd();
        }

        /// <summary>Re-evaluates HP-bar visibility when the active side changes.</summary>
        public void RefreshHpBarAfterTurnChange()
        {
            this.RefreshHpBarVisibility();
        }

        /// <summary>Previews a pending positive damage amount on this card's HP bar.</summary>
        public void SetHealthPreview(float positiveDelta)
        {
            this.hpBarInstance?.RefreshHealthFromBattleState();
            this.hpBarInstance?.SetHealthPreview(Mathf.Max(0f, positiveDelta));
            this.RefreshHpBarDisplayMode();
        }

        /// <summary>Clears this card's pending HP-bar preview.</summary>
        public void ClearHealthPreview()
        {
            this.hpBarInstance?.ClearHealthPreview();
            this.RefreshHpBarDisplayMode();
        }

        /// <summary>
        /// Refreshes a planned damage preview after an effect changes this
        /// card's battle stats. The value is always recalculated from Attacker.
        /// </summary>
        public void RefreshPlannedDamagePreview()
        {
            this.hpBarInstance?.RefreshHealthFromBattleState();
            this.UpdateDamagePreviewFromAttacker();
        }

        private void DespawnHpBar()
        {
            this.EnsureSingleHpBarInstance();
            if (this.hpBarInstance != null)
            {
                this.ReturnHpBarToPool(this.hpBarInstance);
                this.hpBarInstance = null;
            }
        }

        /// <summary>Despawns the world-space ATK UI for this card.</summary>
        public void DespawnAtkUi()
        {
            this.EnsureSingleAtkUiInstance();
            if (this.atkUiInstance == null) return;

            this.ReturnAtkUiToPool(this.atkUiInstance);
            this.atkUiInstance = null;
        }

        /// <summary>Returns this card's world-space HP and ATK UI to their pools.</summary>
        public void DespawnStatUis()
        {
            this.DespawnHpBar();
            this.DespawnAtkUi();
        }

        private void RefreshHpBarDisplayMode()
        {
            if (this.hpBarInstance == null) return;

            this.hpBarInstance.SetDisplayMode(
                !this.isHover,
                this.isHover && this.showFinalDefOnlyOnHover);
        }

        private bool IsHpBarVisible()
        {
            return this.hpBarInstance != null && this.hpBarInstance.gameObject.activeInHierarchy;
        }

        private void RefreshHpBarAfterFaceStateChanged()
        {
            this.RefreshHpBarVisibility();
            this.hpBarInstance?.RefreshWorldSpacePresentation();
        }

        private void EnsureSingleHpBarInstance()
        {
            WorldSpaceHpBarCtrl[] hpBars = this.GetComponentsInChildren<WorldSpaceHpBarCtrl>(true);
            foreach (WorldSpaceHpBarCtrl hpBar in hpBars)
            {
                if (hpBar == this.hpBarInstance) continue;
                if (this.hpBarInstance == null)
                {
                    this.hpBarInstance = hpBar;
                    continue;
                }

                this.ReturnHpBarToPool(hpBar);
            }
        }

        private void ReturnHpBarToPool(WorldSpaceHpBarCtrl hpBar)
        {
            if (hpBar == null) return;

            hpBar.ClearCard();
            hpBar.Despawn?.DoDespawn();
        }

        private void EnsureSingleAtkUiInstance()
        {
            WorldSpaceAtkCtrl[] atkUis = this.GetComponentsInChildren<WorldSpaceAtkCtrl>(true);
            foreach (WorldSpaceAtkCtrl atkUi in atkUis)
            {
                if (atkUi == this.atkUiInstance) continue;
                if (this.atkUiInstance == null)
                {
                    this.atkUiInstance = atkUi;
                    continue;
                }

                this.ReturnAtkUiToPool(atkUi);
            }
        }

        private void ReturnAtkUiToPool(WorldSpaceAtkCtrl atkUi)
        {
            if (atkUi == null) return;

            atkUi.ClearCard();
            atkUi.Despawn?.DoDespawn();
        }

        private void ReturnHpBarToPool()
        {
            this.DespawnHpBar();
        }

        private void SpawnHpBarAfterCardSettles(CardHolderCtrl holder)
        {
            if (this.spawnHpBarRoutine != null) this.StopCoroutine(this.spawnHpBarRoutine);
            this.spawnHpBarRoutine = this.StartCoroutine(this.SpawnHpBarAfterCardSettlesRoutine(holder));
        }

        private IEnumerator SpawnHpBarAfterCardSettlesRoutine(CardHolderCtrl holder)
        {
            yield return null;
            yield return new WaitUntil(() => !this.movement.IsAnimating);

            this.SpawnHpBarAt(holder);
            this.spawnHpBarRoutine = null;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Loads CardData via Addressables and applies it to the Card3D.</summary>
        public async void LoadCard() => await this.loader.LoadAndApply();

        /// <summary>Loads CardData by code name and applies it to the Card3D.</summary>
        public async void LoadCardByCodeName(string codeName) => await this.loader.ShowByCodeName(codeName);

        /// <summary>Applies the textures currently set on Card3D.</summary>
        public void ApplyTextures() => this.card.ApplyTextures();

        /// <summary>Shows the card front face immediately.</summary>
        public void ShowFront() => this.card.ShowFront();

        /// <summary>Shows the card back face immediately.</summary>
        public void ShowBack() => this.card.ShowBack();

        /// <summary>Flips the card with animation.</summary>
        public void Flip() => this.card.Flip();

        /// <summary>Assigns new CardData and immediately applies its textures.</summary>
        public void SetCardData(CardData data) => this.card.SetCardData(data);

        /// <summary>
        /// Sets the fallback display name shown in CardNameText when the
        /// assigned CardData has no CardName filled in.
        /// Call before <see cref="LoadCardByCodeName"/> so the name is ready
        /// when ApplyCardText runs.
        /// </summary>
        public void SetFallbackName(string name) => this.card.SetFallbackName(name);

        /// <summary>
        /// Sets fallback ATK / DEF / Stars shown when the assigned CardData has zeros.
        /// Pass stats parsed from <c>ItemDefinitionData.base_stats</c>.
        /// </summary>
        public void SetFallbackStats(CardBaseStats stats) => this.card.SetFallbackStats(stats);

        public void SetAuraAtk(int finalAtk)
        {
            // Do not write aura ATK to Card3D.AtkText: the card face always shows base ATK.
            // runtimeAtk is consumed exclusively by AtkUI and combat damage preview.
            this.runtimeAtk = finalAtk;
            this.RefreshAtkUiVisibility();
        }

        /// <summary>
        /// Sets the description shown in DescriptionText. Pass
        /// <c>CardDefinitionMetadata.description</c>.
        /// </summary>
        public void SetFallbackDescription(string description) => this.card.SetFallbackDescription(description);

        /// <summary>Sets the card type used to select its frame and stat visibility.</summary>
        public void SetCardType(string type) => this.card.SetCardType(type);

        /// <summary>Links a <see cref="CardHolderCtrl"/> to this card and moves the card to the holder's position.
        /// <paramref name="onReady"/> is invoked after RotateZ180 completes (new card) or immediately after move starts (existing holder).</summary>
        public void SetCardHolder(CardHolderCtrl holder, System.Action onReady = null)
        {
            if (this.IsMovingVoidToLine) return;
            if (this.movement.IsFlipping) return;
            bool isNewHolder = this.cardHolder == null;
            this.cardHolder = holder;
            if (this.cardHolder == null)
            {
                this.DespawnHpBar();
                return;
            }
            if (!isNewHolder)
            {
                this.MoveBackToHolder();
                return;
            }
            this.movement.MoveTo(this.cardHolder.transform, this.cardHolder.HolderLocation, () =>
            {
                this.RotateZ180(() =>
                {
                    this.SpawnHpBarAt(this.cardHolder);
                    onReady?.Invoke();
                });
            });
            this.FaceDownUnknown();
        }

        /// <summary>Smoothly moves the card to the specified transform, syncing both position and rotation.</summary>
        public void MoveAndRotate(Transform target, Location destination)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.MoveAndRotate(target, destination);
        }

        /// <summary>Smoothly moves the card to the specified world position and rotation.</summary>
        public void MoveAndRotate(Vector3 worldPosition, Quaternion rotation, Location destination)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.MoveAndRotate(worldPosition, rotation, destination);
        }

        /// <summary>Smoothly moves the card to the specified transform, position only (no rotation change).</summary>
        public void MoveTo(Transform target, Location destination)
        {
            if (this.IsMovingVoidToLine) return;
            CardHolderCtrl holder = target != null ? target.GetComponent<CardHolderCtrl>() : null;
            if (holder == null)
            {
                this.movement.MoveTo(target, destination);
                return;
            }

            this.movement.MoveTo(target, destination, () => this.SpawnHpBarAt(holder));
        }

        /// <summary>Smoothly moves the card to the specified world position, no rotation change.</summary>
        public void MoveTo(Vector3 worldPosition, Location destination)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.MoveTo(worldPosition, destination);
        }

        /// <summary>Moves to the target horizontally while retaining the card's current world-space Y position.</summary>
        public void MoveToKeepY(Transform target, Location destination)
        {
            if (this.IsMovingVoidToLine) return;
            CardHolderCtrl holder = target != null ? target.GetComponent<CardHolderCtrl>() : null;
            if (holder == null)
            {
                this.movement.MoveToKeepY(target, destination, null);
                return;
            }

            this.movement.MoveToKeepY(target, destination, () => this.SpawnHpBarAt(holder));
        }

        /// <summary>Cancels the current transition and immediately starts returning this card to its hand slot.</summary>
        public void ReturnToHand(Transform handTarget)
        {
            if (this.IsMovingVoidToLine) return;
            this.cardHolder?.SetCard(null);
            this.AssignCardHolder(null);
            this.movement.ReturnToHand(handTarget);
        }

        /// <summary>Smoothly rotates the card in-place to the target world-space rotation.</summary>
        public void RotateTo(Quaternion targetRotation)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.RotateTo(targetRotation);
        }

        /// <summary>Moves the card to <paramref name="holder"/>'s position and flips face-down via the Unknown axis.
        /// Intended for hand → line transitions.</summary>
        public void MoveToUnknow(CardHolderCtrl holder, System.Action onReady = null)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.MoveToUnknow(holder, () =>
            {
                onReady?.Invoke();
                this.SpawnHpBarAfterCardSettles(holder);
            });
        }

        /// <summary>Assigns the card-holder reference without triggering any movement or animation.</summary>
        public void AssignCardHolder(CardHolderCtrl holder)
        {
            this.cardHolder = holder;
            if (holder == null) this.DespawnHpBar();
        }

        /// <summary>Rotates the card 180 degrees around the world Z axis, then invokes <paramref name="onComplete"/>.</summary>
        public void RotateZ180(System.Action onComplete = null)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.RotateY180(onComplete);
        }

        /// <summary>Smoothly rotates the card to face-down using the Unknown axis, without rising.</summary>
        public void FaceDownUnknown()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.FaceDownUnknown();
            FaceStateChanged?.Invoke(this, false);
            this.RefreshHpBarAfterFaceStateChanged();
        }

        /// <summary>Smoothly rotates the card to face-up using the Unknown axis, without rising.</summary>
        public void FaceUpUnknown()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.FaceUpUnknown();
            FaceStateChanged?.Invoke(this, true);
            this.RefreshHpBarAfterFaceStateChanged();
        }

        /// <summary>Smoothly rotates the card to face-up.</summary>
        public void FaceUp()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.FaceUp();
            FaceStateChanged?.Invoke(this, true);
            this.RefreshHpBarAfterFaceStateChanged();
        }

        /// <summary>Smoothly rotates the card to face-down.</summary>
        public void FaceDown()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.FaceDown();
            FaceStateChanged?.Invoke(this, false);
            this.RefreshHpBarAfterFaceStateChanged();
        }

        /// <summary>Moves the card to the full-detail point without changing its logical location.</summary>
        public void MoveToFullDetail(Transform point)
        {
            if (this.IsMovingVoidToLine) return;
            this.DespawnStatUis();
            this.movement.MoveToFullDetail(point);
        }

        /// <summary>Returns the card from full-detail back to its selected position in hand.</summary>
        public void ReturnFromFullDetail()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.ReturnFromFullDetail();
            this.RefreshHpBarVisibility();
        }

        /// <summary>Controls HP-bar visibility while this card is displayed in full-detail view.</summary>
        public void SetFullDetailMode(bool enabled)
        {
            if (this.isFullDetail == enabled) return;

            this.isFullDetail = enabled;
            this.fullDetailManipulator?.SetInteractionActive(enabled);
            if (enabled)
            {
                this.DespawnStatUis();
            }
        }

        /// <summary>Plays the damage run-up animation: card rises then returns to its current position.</summary>
        public void RunUp()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.RunUp();
        }

        /// <summary>Plays the damage shake animation on the Z axis.</summary>
        public void Damaged()
        {
            if (this.IsMovingVoidToLine) return;
            this.RefreshHpBarVisibility();
            this.movement.Damaged();
        }

        /// <summary>Plays the ability activation animation.</summary>
        public void AbilityActive()
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.AbilityActive();
        }

        /// <summary>Plays the attack lunge animation: card charges toward the defender then returns.</summary>
        public void AttackLunge(Vector3 defenderPosition)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.AttackLunge(defenderPosition);
        }

        /// <summary>Plays the attack animation with a small backstep before the lunge, then returns.</summary>
        public void AttackBackstepLunge(Vector3 defenderPosition)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.AttackBackstepLunge(defenderPosition);
        }

        /// <summary>Moves the card back to its currently assigned <see cref="CardHolderCtrl"/> position (no flip).</summary>
        public void MoveBackToHolder()
        {
            if (this.IsMovingVoidToLine) return;
            if (this.cardHolder == null) return;
            this.movement.MoveBackToLineHolder(this.cardHolder);
        }

        /// <summary>Moves the card forward toward the defender and stops there (no return).</summary>
        public void PlanningLunge(Vector3 defenderPosition)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.PlanningLunge(defenderPosition);
        }

        /// <summary>Moves the card directly to the given destination (no stop-distance offset, no return).</summary>
        public void PlanningLungeTo(Vector3 destination)
        {
            if (this.IsMovingVoidToLine) return;
            this.movement.PlanningLungeTo(destination);
        }

        /// <summary>Plays the ability activation animation: card rises + scales up, holds, then returns.</summary>
        public void ActivateAbility() => this.RunUp();

        /// <summary>Toggles the card between face-up and face-down.</summary>
        public void ToggleFace()
        {
            if (this.movement.FaceState == FaceState.FaceUp)
            {
                this.FaceDown();
                return;
            }
            this.FaceUp();
        }

        /// <summary>Current logical location of this card.</summary>
        public Location Location   => this.movement.Location;
        public bool    IsFlipping  => this.movement.IsFlipping;
        public bool    IsAnimating => this.movement.IsAnimating;
        public string  InventoryItemId => this.inventoryItemId;

        /// <summary>The Omega card currently planning an attack against this card.</summary>
        public Card3DCtrl Attacker => this.attacker;

        /// <summary>
        /// Assigns the card planning an attack against this card. The damage
        /// preview is derived from that attacker and is cleared with the link.
        /// </summary>
        public void SetAttacker(Card3DCtrl value)
        {
            this.attacker = value;
            this.UpdateDamagePreviewFromAttacker();
        }

        private void UpdateDamagePreviewFromAttacker()
        {
            if (this.attacker == null)
            {
                this.ClearHealthPreview();
                return;
            }

            int attack = this.attacker.IsOmegaCardHidden()
                ? 0
                : this.attacker.GetDamagePreviewAttack();
            this.SetHealthPreview(attack);
        }

        /// <summary>
        /// Gets the damage amount used for a pending attack preview. An ability
        /// with <c>atk_added</c> combines that bonus with the attack of the
        /// required character while that character is in the attacker's Front Line.
        /// </summary>
        public int GetDamagePreviewAttack()
        {
            if (this.definition == null) return 0;

            int addedAttack = this.definition?.GetBaseStatInt("atk_added") ?? 0;
            if (!this.definition.TryGetBaseStat("atk_added", out _))
            {
                if (this.runtimeAtk >= 0) return this.runtimeAtk;
                BattleCardSlot slot = this.GetBattleSlot();
                if (slot != null && slot.final_atk > 0) return slot.final_atk;
                return Mathf.Max(0, this.definition.GetBaseStatInt("atk"));
            }

            string requiredCharacterCode = this.definition.metadata?.char_code_required;
            if (string.IsNullOrWhiteSpace(requiredCharacterCode)) return 0;

            BattleCardSlot[] frontLine = this.cardOwner == Owner.alpha
                ? this.battleStateCtrl?.BattleState?.AlphaFrontLine
                : this.battleStateCtrl?.BattleState?.OmegaFrontLine;
            CardDefinitionData requiredCharacter = FindRequiredCharacterInFrontLine(
                frontLine,
                this.battleStateCtrl?.BattleCardDefinitions,
                requiredCharacterCode);
            if (requiredCharacter == null) return 0;

            int requiredCharacterAttack = requiredCharacter.GetBaseStatInt("atk");
            return Mathf.Max(0, requiredCharacterAttack + addedAttack);
        }

        private static CardDefinitionData FindRequiredCharacterInFrontLine(
            BattleCardSlot[] frontLine,
            BattleCardDefinitions definitions,
            string requiredCharacterCode)
        {
            if (frontLine == null || definitions == null || string.IsNullOrWhiteSpace(requiredCharacterCode)) return null;

            foreach (BattleCardSlot slot in frontLine)
            {
                CardDefinitionData character = definitions.GetDefinitionByCode(slot?.item_definition_code_name);
                if (character?.metadata?.type != "character") continue;
                if (character.item_code == requiredCharacterCode
                    || character.metadata.char_code_required == requiredCharacterCode) return character;
            }

            return null;
        }

        public void SetMoveDuration(float d)  => this.movement.SetMoveDuration(d);
        public void SetRotateDuration(float d) => this.movement.SetRotateDuration(d);

        public void SetInventoryItemId(string id)
        {
            if (this.inventoryItemId != id)
            {
                this.SetAttacker(null);
                this.runtimeAtk = -1;
            }
            this.inventoryItemId = id;
        }

        /// <summary>The holder this card is currently assigned to, or null if none.</summary>
        public CardHolderCtrl CardHolder => this.cardHolder;
        public bool IsMovingVoidToLine => this.movement != null && this.movement.IsVoidToLineTransitionActive;

        /// <summary>The type of this card (character or support), derived from Definition.Metadata.type.</summary>
        public CardType CardType => Enum.TryParse(this.definition?.metadata?.type, out CardType t) ? t : default;

        // ─── Per-owner spawn counters ─────────────────────────────────────────────
        // Counted independently so alpha and omega each start at 1.

        private static int alphaSpawnIndex = 0;
        private static int omegaSpawnIndex = 0;

        /// <summary>Sets the owner of this card (alpha or omega) and prefixes the
        /// GameObject name with [alpha] or [omega] so cards are easy to identify
        /// in the Hierarchy and Debug logs.
        /// The trailing index is counted independently per owner so alpha and omega
        /// each start from 1 (e.g. [alpha]Card3D_1, [omega]Card3D_1).</summary>
        public void SetOwner(Owner owner)
        {
            this.cardOwner = owner;
            string prefix = $"[{owner}]";
            if (!this.name.StartsWith(prefix))
            {
                // Derive the bare prefab name by stripping any existing owner prefix
                // and the trailing _N index added by Spawner.UpdateName.
                string baseName = this.name;
                if (baseName.StartsWith("[alpha]")) baseName = baseName.Substring(7);
                else if (baseName.StartsWith("[omega]")) baseName = baseName.Substring(7);
                int underscoreIdx = baseName.LastIndexOf('_');
                if (underscoreIdx >= 0 && int.TryParse(baseName.Substring(underscoreIdx + 1), out _))
                    baseName = baseName.Substring(0, underscoreIdx);

                int index = owner == Owner.alpha ? ++alphaSpawnIndex : ++omegaSpawnIndex;
                this.name = $"{prefix}{baseName}_{index}";
            }

            this.RefreshHpBarVisibility();
        }

        /// <summary>The owner (alpha or omega) of this card.</summary>
        public Owner CardOwner => this.cardOwner;

        /// <summary>Returns true if this card's type is character.</summary>
        public bool IsCharacter() => this.definition?.metadata?.type == "character";

        /// <summary>Stores the definition data looked up by code name from BattleCardDefinitions.</summary>
        public void SetDefinition(CardDefinitionData def)
        {
            this.definition = def;
            this.SetCardType(def?.metadata?.type);
            this.RefreshHpBarVisibility();
        }

        /// <summary>The definition data currently assigned to this card.</summary>
        public CardDefinitionData Definition => this.definition;

        /// <summary>Stores the code name used to look up this card's definition.</summary>
        public void SetCodeName(string code) => this.codeName = code;

        /// <summary>The code name assigned to this card.</summary>
        public string CodeName => this.codeName;

        /// <summary>Marks whether this card is exposed (always face-up).</summary>
        public void SetExpose(bool value)
        {
            this.expose = value;
            this.RefreshHpBarVisibility();
        }

        /// <summary>Returns true if this card is exposed and must not be flipped face-down.</summary>
        public bool Expose => this.expose;

        /// <summary>Returns true if this card is a trigger.</summary>
        public bool IsTrigger => this.isTrigger;

        /// <summary>Returns true if this card is currently being hovered.</summary>
        public bool IsHover => this.isHover;

        /// <summary>Sets whether this card is a trigger.</summary>
        public void SetIsTrigger(bool value)
        {
            if (this.isTrigger == value) return;
            this.isTrigger = value;
            TriggerStateChanged?.Invoke(this, value);
        }

        /// <summary>Returns true when this is an omega card that should not reveal its tooltip
        /// (hidden unless both exposed and face-up).</summary>
        public bool IsOmegaCardHidden()
        {
            if (this.cardOwner != Owner.omega) return false;
            return !this.expose || this.FaceState != FaceState.FaceUp;
        }

        /// <summary>The current face state of this card.</summary>
        public FaceState FaceState => this.movement.FaceState;
    }
}
