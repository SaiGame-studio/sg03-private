using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    /// <summary>Displays the server-provided shop list in ShopSideBar.</summary>
    public class ShopPanelUI
    {
        private readonly ScrollView shopList;
        private readonly Label contentTitle;
        private readonly Label contentDescription;
        private readonly Label itemsStatus;
        private readonly TextField itemSearchField;
        private readonly ScrollView itemList;
        private readonly Shop shop;
        private readonly CurrencyWallet currencyWallet;
        private Button selectedShopButton;
        private ShopData selectedShop;
        private ShopItemViewData[] currentItems;
        private ShopCurrencyData currentShopCurrency;
        private string itemSearchTerm;
        private readonly Dictionary<string, int> selectedQuantities = new Dictionary<string, int>();

        public ShopPanelUI(VisualElement panelRoot, Shop shop, CurrencyWallet currencyWallet)
        {
            this.shopList = panelRoot.Q<ScrollView>("ShopList");
            this.contentTitle = panelRoot.Q<Label>("ShopContentTitle");
            this.contentDescription = panelRoot.Q<Label>("ShopContentDescription");
            this.itemsStatus = panelRoot.Q<Label>("ShopItemsStatus");
            this.itemSearchField = panelRoot.Q<TextField>("ShopItemSearchField");
            this.itemList = panelRoot.Q<ScrollView>("ShopItemList");
            this.shop = shop;
            this.currencyWallet = currencyWallet;
            this.itemSearchField?.RegisterValueChangedCallback(change => this.FilterItemsByName(change.newValue));
            if (this.currencyWallet != null)
                this.currencyWallet.OnBalancesUpdated += this.RefreshItemAffordability;

            this.ShowSidebarState("Loading shops...");
            if (shop == null)
            {
                this.ShowSidebarState("Shop service is unavailable.", true);
                return;
            }

            this.shop.GetShops(
                onSuccess: response => this.ShowShops(response?.shops),
                onError: error => this.ShowSidebarState(string.IsNullOrWhiteSpace(error) ? "Unable to load shops." : error, true));
        }

        private void ShowShops(ShopData[] shops)
        {
            if (this.shopList == null) return;
            this.shopList.Clear();
            this.selectedShopButton = null;

            if (shops == null || shops.Length == 0)
            {
                this.ShowSidebarState("No shops available.");
                return;
            }

            foreach (ShopData shop in shops)
            {
                if (shop == null) continue;
                this.shopList.Add(this.BuildShopButton(shop));
            }
        }

        private Button BuildShopButton(ShopData shop)
        {
            Button button = new Button();
            button.AddToClassList("shop-nav-btn");

            Label name = new Label(string.IsNullOrWhiteSpace(shop.name) ? shop.shop_key : shop.name);
            name.AddToClassList("shop-nav-btn__name");
            button.Add(name);

            string meta = string.IsNullOrWhiteSpace(shop.shop_type) ? $"{shop.item_count} items" : $"{shop.shop_type} · {shop.item_count} items";
            Label details = new Label(meta);
            details.AddToClassList("shop-nav-btn__meta");
            button.Add(details);

            button.RegisterCallback<ClickEvent>(_ => this.SelectShop(shop, button));
            return button;
        }

        private void SelectShop(ShopData shop, Button button)
        {
            this.selectedShopButton?.RemoveFromClassList("shop-nav-btn--active");
            this.selectedShopButton = button;
            this.selectedShopButton.AddToClassList("shop-nav-btn--active");
            this.selectedShop = shop;

            if (this.contentTitle != null)
                this.contentTitle.text = string.IsNullOrWhiteSpace(shop.name) ? shop.shop_key : shop.name;
            if (this.contentDescription != null)
                this.contentDescription.text = string.IsNullOrWhiteSpace(shop.description)
                    ? $"{shop.item_count} items available."
                    : shop.description;

            this.itemList?.Clear();
            this.ShowItemsState("Loading items...");
            this.LoadShopItems(shop.id);
        }

        // ShopItemsResponse in the read-only SaiGame dependency does not yet contain
        // the new item-level and shop-level `currency` objects. Parse this endpoint
        // locally until that dependency model is updated.
        private void LoadShopItems(string shopId)
        {
            SaiServer server = SaiServer.Instance;
            if (server == null)
            {
                this.ShowItemsState("Shop service is unavailable.", true);
                return;
            }

            string endpoint = $"/api/v1/games/{server.GameId}/shops/{shopId}/items";
            server.StartCoroutine(server.GetRequest(
                endpoint,
                response =>
                {
                    if (!this.IsSelectedShop(shopId)) return;
                    try
                    {
                        ShopItemsWithCurrencyResponse data = JsonUtility.FromJson<ShopItemsWithCurrencyResponse>(response);
                        this.ShowItems(data?.items, data?.currency);
                    }
                    catch (Exception exception)
                    {
                        this.ShowItemsState($"Unable to read shop items: {exception.Message}", true);
                    }
                },
                error =>
                {
                    if (this.IsSelectedShop(shopId))
                        this.ShowItemsState(string.IsNullOrWhiteSpace(error) ? "Unable to load shop items." : error, true);
                }));
        }

        private void ShowItems(ShopItemViewData[] items, ShopCurrencyData shopCurrency)
        {
            if (this.itemList == null) return;
            this.itemList.Clear();
            this.currentItems = items;
            this.currentShopCurrency = shopCurrency;

            if (items == null || items.Length == 0)
            {
                this.ShowItemsState("This shop has no items available.");
                return;
            }

            this.ShowItemsState(string.Empty);
            this.DisplayFilteredItems();
        }

        private void FilterItemsByName(string searchTerm)
        {
            this.itemSearchTerm = searchTerm;
            this.DisplayFilteredItems();
        }

        private void DisplayFilteredItems()
        {
            if (this.itemList == null || this.currentItems == null) return;
            this.itemList.Clear();

            bool hasItems = false;
            foreach (ShopItemViewData item in this.currentItems)
            {
                if (item == null) continue;
                item.shop_currency = this.currentShopCurrency;
                if (!this.ItemNameMatchesSearch(item)) continue;
                this.itemList.Add(this.BuildItemCard(item));
                hasItems = true;
            }

            this.ShowItemsState(hasItems ? string.Empty : "No items match your search.");
        }

        private bool ItemNameMatchesSearch(ShopItemViewData item)
        {
            if (string.IsNullOrWhiteSpace(this.itemSearchTerm)) return true;

            string itemName = this.GetItemDisplayName(item);
            return itemName.IndexOf(this.itemSearchTerm.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private VisualElement BuildItemCard(ShopItemViewData item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("shop-item-card");

            VisualElement details = new VisualElement();
            details.AddToClassList("shop-item-card__details");
            Label name = new Label(this.GetItemDisplayName(item));
            name.AddToClassList("shop-item-card__name");
            details.Add(name);

            if (!string.IsNullOrWhiteSpace(item.description))
            {
                Label description = new Label(item.description);
                description.AddToClassList("shop-item-card__description");
                details.Add(description);
            }

            Label meta = new Label(this.BuildItemMeta(item));
            meta.AddToClassList("shop-item-card__meta");
            details.Add(meta);
            card.Add(details);

            Button buyButton = new Button();
            buyButton.AddToClassList("shop-item-card__buy");
            card.Add(buyButton);

            int selectedQuantity = this.GetSelectedQuantity(item.id);
            IntegerField quantityInput = new IntegerField { value = selectedQuantity, isDelayed = false };
            quantityInput.AddToClassList("shop-item-card__quantity-input");
            card.Add(quantityInput);

            Action<int> updatePurchaseButton = quantity =>
            {
                int validQuantity = Mathf.Max(1, quantity);
                if (quantityInput.value != validQuantity)
                    quantityInput.SetValueWithoutNotify(validQuantity);
                this.selectedQuantities[item.id] = validQuantity;

                buyButton.text = $"Buy for {(long)item.price * validQuantity} {this.GetCurrencyName(item)}";
                buyButton.SetEnabled(this.CanPurchaseQuantity(item, validQuantity));
            };
            quantityInput.RegisterValueChangedCallback(change => updatePurchaseButton(change.newValue));
            buyButton.RegisterCallback<ClickEvent>(_ => this.PurchaseItem(item, quantityInput.value, buyButton));
            updatePurchaseButton(quantityInput.value);
            return card;
        }

        private int GetSelectedQuantity(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && this.selectedQuantities.TryGetValue(itemId, out int quantity)
                ? Mathf.Max(1, quantity)
                : 1;
        }

        private bool CanPurchaseQuantity(ShopItemViewData item, int quantity)
        {
            if (string.IsNullOrWhiteSpace(item.id) || quantity < 1 || this.currencyWallet == null || !this.currencyWallet.IsLoaded)
                return false;
            if (item.purchase_limit > 0)
                if (quantity > item.purchase_limit - item.purchased_count)
                    return false;

            long totalPrice = (long)item.price * quantity;
            return totalPrice >= 0
                && totalPrice <= int.MaxValue
                && this.currencyWallet.CanAfford(this.GetCurrencyItemDefinitionId(item), (int)totalPrice);
        }

        private void PurchaseItem(ShopItemViewData item, int quantity, Button buyButton)
        {
            if (this.selectedShop == null || !this.CanPurchaseQuantity(item, quantity)) return;

            string shopId = this.selectedShop.id;
            buyButton.SetEnabled(false);
            this.ShowItemsState("Purchasing...");
            this.shop.PurchaseItem(
                shopId,
                item.id,
                quantity: quantity,
                idempotencyKey: Guid.NewGuid().ToString("N"),
                onSuccess: _ =>
                {
                    if (!this.IsSelectedShop(shopId)) return;
                    this.ShowItemsState("Purchase successful.");
                    this.currencyWallet?.Refresh();
                    this.LoadShopItems(shopId);
                },
                onError: error =>
                {
                    if (!this.IsSelectedShop(shopId)) return;
                    buyButton.SetEnabled(true);
                    this.ShowItemsState(string.IsNullOrWhiteSpace(error) ? "Purchase failed." : error, true);
                });
        }

        private bool IsSelectedShop(string shopId)
            => this.selectedShop != null && this.selectedShop.id == shopId;

        private string BuildItemMeta(ShopItemViewData item)
        {
            string currency = this.GetCurrencyName(item);
            string stock = item.stock <= 0 ? "Unlimited stock" : $"Stock: {item.stock}";
            if (item.purchase_limit > 0)
                stock += $" · Limit: {item.purchased_count}/{item.purchase_limit}";
            return $"{item.price} {currency} · {stock}";
        }

        private string GetItemDisplayName(ShopItemViewData item)
            => string.IsNullOrWhiteSpace(item.display_name) ? item.item_def_id : item.display_name;

        private string GetCurrencyName(ShopItemViewData item)
        {
            string currency = item.currency?.item_name;
            if (string.IsNullOrWhiteSpace(currency)) currency = item.shop_currency?.item_name;
            if (string.IsNullOrWhiteSpace(currency)) currency = item.currency_item_def_id;
            return string.IsNullOrWhiteSpace(currency) ? "currency" : currency;
        }

        private string GetCurrencyItemDefinitionId(ShopItemViewData item)
        {
            string currencyDefinitionId = item.currency?.item_def_id;
            if (string.IsNullOrWhiteSpace(currencyDefinitionId)) currencyDefinitionId = item.shop_currency?.item_def_id;
            return string.IsNullOrWhiteSpace(currencyDefinitionId) ? item.currency_item_def_id : currencyDefinitionId;
        }

        private void RefreshItemAffordability()
        {
            if (this.currentItems != null)
                this.ShowItems(this.currentItems, this.currentShopCurrency);
        }

        public void Dispose()
        {
            if (this.currencyWallet != null)
                this.currencyWallet.OnBalancesUpdated -= this.RefreshItemAffordability;
        }

        [Serializable]
        private class ShopItemsWithCurrencyResponse
        {
            public ShopCurrencyData currency;
            public ShopItemViewData[] items;
        }

        [Serializable]
        private class ShopCurrencyData
        {
            public string item_def_id;
            public string item_code;
            public string item_name;
        }

        [Serializable]
        private class ShopItemViewData
        {
            public string id;
            public string item_def_id;
            public string display_name;
            public string description;
            public int price;
            public string currency_item_def_id;
            public string purchase_limit_type;
            public int purchase_limit;
            public int stock;
            public int purchased_count;
            public ShopCurrencyData currency;

            // Assigned from the response's shared currency when an item has none.
            [NonSerialized] public ShopCurrencyData shop_currency;
        }

        private void ShowItemsState(string message, bool isError = false)
        {
            if (this.itemsStatus == null) return;
            this.itemsStatus.text = message;
            this.itemsStatus.style.display = string.IsNullOrWhiteSpace(message)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            if (isError)
                this.itemsStatus.AddToClassList("shop-items__status--error");
            else
                this.itemsStatus.RemoveFromClassList("shop-items__status--error");
        }

        private void ShowSidebarState(string message, bool isError = false)
        {
            if (this.shopList == null) return;
            this.shopList.Clear();
            Label state = new Label(message);
            state.AddToClassList("shop-sidebar__state");
            if (isError) state.AddToClassList("shop-sidebar__state--error");
            this.shopList.Add(state);
        }
    }
}
