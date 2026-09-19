using UnityEngine;

namespace SG03
{
    public partial class Card3DCtrl
    {
        [Header("Card Name UI")]
        [SerializeField] private WorldSpaceCardNameCtrl cardNameUiPrefab;
        [SerializeField] private WorldSpaceCardNameCtrl cardNameUiInstance;

        public bool TryGetCardNameAnchor(out Vector3 position)
        {
            position = this.transform.position;
            if (this.card == null || !this.card.TryGetTopEdgeWorldPosition(out Vector3 topEdge)
                || !this.card.TryGetStatsCenterWorldPosition(out Vector3 statsCenter)) return false;
            position = (topEdge + statsCenter) * 0.5f;
            return true;
        }

        /// <summary>Resolves the display name of this card from definition, card, or object name.</summary>
        public string GetCardDisplayName()
        {
            if (this.definition != null && !string.IsNullOrEmpty(this.definition.name))
            {
                return this.definition.name;
            }

            if (this.card != null && !string.IsNullOrEmpty(this.card.FallbackName))
            {
                return this.card.FallbackName;
            }

            if (!string.IsNullOrEmpty(this.codeName))
            {
                return this.codeName;
            }

            return this.name;
        }

        /// <summary>Determines whether Card Name UI should be visible for this card.</summary>
        public bool ShouldShowCardNameUi()
        {
            if (this.IsBattleResuming() || !this.isHover) return false;
            if (this.Location == Location.in_hand || this.Location == Location.in_void || this.Location == Location.in_source) return false;
            if (!this.HasIdentifiedCodeName()) return false;
            return true;
        }

        /// <summary>Checks whether this card has an identified code name.</summary>
        public bool HasIdentifiedCodeName()
        {
            if (!string.IsNullOrWhiteSpace(this.codeName) && !this.codeName.Equals("unknown", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (this.definition != null && !string.IsNullOrWhiteSpace(this.definition.item_code) && !this.definition.item_code.Equals("unknown", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>Refreshes Card Name UI visibility based on hover state, location, and code name.</summary>
        public void RefreshCardNameUiVisibility()
        {
            if (!this.ShouldShowCardNameUi())
            {
                this.DespawnCardNameUi();
                return;
            }

            this.SpawnCardNameUi();
        }

        /// <summary>Spawns and displays the world-space Card Name UI for this card.</summary>
        public void SpawnCardNameUi()
        {
            this.EnsureSingleCardNameUiInstance();
            this.LoadObjectPool();
            if (this.objectPool == null || this.objectPool.PoolPrefabs == null)
            {
                Debug.LogWarning($"{this.name}: ObjectPool is not ready for the Card Name UI.", this);
                return;
            }

            if (this.cardNameUiPrefab == null)
            {
                this.cardNameUiPrefab = this.objectPool.PoolPrefabs.GetByName("CardNameUI") as WorldSpaceCardNameCtrl;
            }

            if (this.cardNameUiPrefab == null)
            {
                Debug.LogWarning($"{this.name}: CardNameUI is missing from ObjectPoolPrefabs.", this);
                return;
            }

            if (this.cardNameUiInstance == null)
            {
                this.cardNameUiInstance = this.objectPool.SpawnInactive(this.cardNameUiPrefab, Vector3.zero);
            }

            if (this.cardNameUiInstance == null) return;

            Vector3 pos = this.cardHolder != null ? this.cardHolder.transform.position : this.transform.position;
            this.cardNameUiInstance.SetPosition(pos);
            this.cardNameUiInstance.gameObject.SetActive(true);
            this.cardNameUiInstance.SetCard(this);
            this.cardNameUiInstance.RefreshUi();
        }

        /// <summary>Despawns the world-space Card Name UI for this card.</summary>
        public void DespawnCardNameUi()
        {
            this.EnsureSingleCardNameUiInstance();
            if (this.cardNameUiInstance == null) return;

            this.ReturnCardNameUiToPool(this.cardNameUiInstance);
            this.cardNameUiInstance = null;
        }

        private void EnsureSingleCardNameUiInstance()
        {
            WorldSpaceCardNameCtrl[] nameUis = this.GetComponentsInChildren<WorldSpaceCardNameCtrl>(true);
            foreach (WorldSpaceCardNameCtrl nameUi in nameUis)
            {
                if (nameUi == this.cardNameUiInstance) continue;
                if (this.cardNameUiInstance == null)
                {
                    this.cardNameUiInstance = nameUi;
                    continue;
                }

                this.ReturnCardNameUiToPool(nameUi);
            }
        }

        private void ReturnCardNameUiToPool(WorldSpaceCardNameCtrl nameUi)
        {
            if (nameUi == null) return;

            nameUi.ClearCard();
            nameUi.Despawn?.DoDespawn();
        }
    }
}
