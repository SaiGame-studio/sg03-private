using System;
using System.Collections;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    public partial class ClientActions
    {
        private Coroutine ExecuteAlphaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.StartCoroutine(this.WaitDefinitionsThenSpawn(() => this.cardSpawning?.SpawnAlphaSourceCards(count)));
        }

        private Coroutine ExecuteOmegaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.StartCoroutine(this.WaitDefinitionsThenSpawn(() => this.cardSpawning?.SpawnOmegaSourceCards(count)));
        }

        private IEnumerator WaitDefinitionsThenSpawn(Func<Coroutine> spawnAction)
        {
            yield return new WaitUntil(() => this.battleCardDefinitions != null && this.battleCardDefinitions.IsLoaded);
            yield return spawnAction();
        }

        private Coroutine ExecuteAlphaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaSourceToHandRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaSourceToHandRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.SetAlphaSourceCardData(inventoryItemId);
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] card={(card != null ? card.name : "NULL")}, id={inventoryItemId}, slot={slotIndex}");
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] before commit — IsAnimating={card?.IsAnimating}, Location={card?.Location}");
            this.cardSpawning?.CommitAlphaSourceToHand(card, inventoryItemId, slotIndex);
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] after commit — IsAnimating={card?.IsAnimating}, Location={card?.Location}");
            if (card != null) yield return this.StartCoroutine(this.WaitForCard(card));
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] WaitForCard done — IsAnimating={card?.IsAnimating}");
        }

        private Coroutine ExecuteOmegaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaSourceToHand(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaHandToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaHandToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) yield break;
            if (this.IsLocalPlayerDeploy(inventoryItemId, Link.front, slotIndex))
            {
                yield return this.StartCoroutine(this.WaitForCard(card));
                yield break;
            }
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveAlphaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaHandInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaHandToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaHandToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) yield break;
            if (this.IsLocalPlayerDeploy(inventoryItemId, Link.back, slotIndex))
            {
                yield return this.StartCoroutine(this.WaitForCard(card));
                yield break;
            }
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveAlphaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaHandInBackLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaHandToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaHandToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.PeekOmegaHandCard();
            if (card == null) yield break;
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveOmegaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaHandInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaHandToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaHandToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.PeekOmegaHandCard();
            if (card == null) yield break;
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveOmegaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaHandInBackLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaVoidToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaVoidToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaVoidToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            CardHolderCtrl holder = this.cardSpawning?.GetAlphaVoidToFrontLineHolder(slotIndex);
            yield return this.StartCoroutine(this.VoidToLineRoutine(
                inventoryItemId,
                holder,
                () => this.cardSpawning?.MoveAlphaVoidToFrontLine(inventoryItemId, slotIndex)));
        }

        private Coroutine ExecuteOmegaVoidToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaVoidToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaVoidToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            CardHolderCtrl holder = this.cardSpawning?.GetOmegaVoidToFrontLineHolder(slotIndex);
            yield return this.StartCoroutine(this.VoidToLineRoutine(
                inventoryItemId,
                holder,
                () => this.cardSpawning?.MoveOmegaVoidToFrontLine(inventoryItemId, slotIndex)));
        }

        private Coroutine ExecuteAlphaVoidToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaVoidToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaVoidToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            CardHolderCtrl holder = this.cardSpawning?.GetAlphaVoidToBackLineHolder(slotIndex);
            yield return this.StartCoroutine(this.VoidToLineRoutine(
                inventoryItemId,
                holder,
                () => this.cardSpawning?.MoveAlphaVoidToBackLine(inventoryItemId, slotIndex)));
        }

        private Coroutine ExecuteOmegaVoidToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaVoidToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaVoidToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            CardHolderCtrl holder = this.cardSpawning?.GetOmegaVoidToBackLineHolder(slotIndex);
            yield return this.StartCoroutine(this.VoidToLineRoutine(
                inventoryItemId,
                holder,
                () => this.cardSpawning?.MoveOmegaVoidToBackLine(inventoryItemId, slotIndex)));
        }

        private IEnumerator VoidToLineRoutine(
            string inventoryItemId,
            CardHolderCtrl holder,
            Func<Card3DCtrl> startMove)
        {
            if (holder == null || startMove == null) yield break;

            holder.RefreshHeldCardPlacement();
            Card3DCtrl card = holder.HeldCard;
            if (this.IsCardSettledOnHolder(card, inventoryItemId, holder)) yield break;

            card = startMove();
            if (card == null) yield break;

            while (!this.IsCardSettledOnHolder(card, inventoryItemId, holder))
            {
                holder.RefreshHeldCardPlacement();
                if (!card.IsAnimating && !card.IsMovingVoidToLine && !holder.IsHeldCardOnHolder)
                {
                    card = startMove();
                    if (card == null) yield break;
                }
                yield return null;
            }
        }

        private bool IsCardSettledOnHolder(Card3DCtrl card, string inventoryItemId, CardHolderCtrl holder)
        {
            if (card == null || holder == null || holder.HeldCard != card) return false;
            if (!string.Equals(card.InventoryItemId, inventoryItemId, StringComparison.Ordinal)) return false;
            holder.RefreshHeldCardPlacement();
            return holder.IsHeldCardOnHolder && !card.IsAnimating && !card.IsMovingVoidToLine;
        }
    }
}
