using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Handles desk-tab rendering, preset loading, and card-count display.
    // Fires OnPresetTabSelected immediately when a tab is clicked (basic data).
    // Fires OnPresetSlotsLoaded after the full slot data is fetched from the server.
    public class GameDeskTabsUI
    {
        public event Action<PresetData> OnPresetTabSelected;
        public event Action<PresetData> OnPresetSlotsLoaded;

        private readonly Func<ItemPreset> getItemPreset;

        private VisualElement deskTabs;
        private readonly List<Button> deskButtons = new List<Button>();
        private Label cardCountLabel;

        public GameDeskTabsUI(Func<ItemPreset> getItemPreset)
        {
            this.getItemPreset = getItemPreset;
        }

        public void Bind(VisualElement root)
        {
            this.deskTabs = root.Q("DeskTabs");
            this.cardCountLabel = root.Q<Label>("CardCountLabel");
        }

        public void SetVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (this.deskTabs != null) this.deskTabs.style.display = display;
            if (this.cardCountLabel != null) this.cardCountLabel.style.display = display;
        }

        public void LoadPresets()
        {
            ItemPreset current = this.getItemPreset();
            if (current == null) return;
            current.GetPresets(this.HandlePresetsLoaded, this.HandlePresetsLoadFailed);
        }

        private void HandlePresetsLoaded(PresetResponse response)
        {
            this.RenderDeskTabs(response?.containers);
        }

        private void HandlePresetsLoadFailed(string error)
        {
            this.RenderDeskTabs(null);
        }

        private void RenderDeskTabs(PresetData[] presets)
        {
            if (this.deskTabs == null) return;
            this.ClearDeskTabs();
            if (presets == null) return;
            PresetData firstPreset = null;
            Button firstButton = null;
            PresetData defaultPreset = null;
            Button defaultButton = null;
            for (int i = 0; i < presets.Length; i++)
            {
                PresetData preset = presets[i];
                if (preset == null) continue;
                Button button = this.AddDeskTab(preset, i);
                if (firstPreset == null)
                {
                    firstPreset = preset;
                    firstButton = button;
                }

                if (defaultPreset == null && DeskList.HasDefaultDeskMetadata(preset))
                {
                    defaultPreset = preset;
                    defaultButton = button;
                }
            }

            PresetData presetToSelect = defaultPreset ?? firstPreset;
            Button buttonToSelect = defaultButton ?? firstButton;
            if (presetToSelect != null && buttonToSelect != null)
                this.OnPresetDeskTabClicked(presetToSelect, buttonToSelect);
        }

        private void ClearDeskTabs()
        {
            foreach (Button btn in this.deskButtons)
                btn.RemoveFromHierarchy();
            this.deskButtons.Clear();
        }

        private Button AddDeskTab(PresetData preset, int index)
        {
            Button btn = new Button();
            btn.name = $"preset-desk-tab-{index + 1}";
            btn.text = this.GetPresetDisplayName(preset, index);
            btn.AddToClassList("game-tab");
            PresetData captured = preset;
            btn.RegisterCallback<ClickEvent>(_ => this.OnPresetDeskTabClicked(captured, btn));
            this.deskButtons.Add(btn);
            this.deskTabs.Add(btn);
            return btn;
        }

        private string GetPresetDisplayName(PresetData preset, int index)
        {
            if (preset == null) return $"Desk {index + 1}";
            if (!string.IsNullOrWhiteSpace(preset.name)) return preset.name;
            if (preset.definition != null && !string.IsNullOrWhiteSpace(preset.definition.name)) return preset.definition.name;
            return $"Desk {index + 1}";
        }

        private void OnPresetDeskTabClicked(PresetData preset, Button selected)
        {
            this.ClearDeskSelection();
            selected.AddToClassList("game-tab--active");
            this.OnPresetTabSelected?.Invoke(preset);
            if (preset == null) return;
            if (string.IsNullOrWhiteSpace(preset.id)) return;
            ItemPreset current = this.getItemPreset();
            if (current == null) return;
            this.SetCardCountLoading();
            current.GetPreset(preset.id, this.HandlePresetSlotsLoaded, this.HandlePresetSlotsLoadFailed);
        }

        private void ClearDeskSelection()
        {
            foreach (Button btn in this.deskButtons)
                btn.RemoveFromClassList("game-tab--active");
        }

        private void HandlePresetSlotsLoaded(PresetData preset)
        {
            ItemPreset current = this.getItemPreset();
            this.UpdatePresetInspectorData(preset, current);
            this.SetCardCount(this.GetFilledSlotCount(preset));
            this.OnPresetSlotsLoaded?.Invoke(preset);
        }

        private void HandlePresetSlotsLoadFailed(string error)
        {
            this.SetCardCount(0);
        }

        private void UpdatePresetInspectorData(PresetData updatedPreset, ItemPreset current)
        {
            if (updatedPreset == null) return;
            if (current == null) return;
            PresetResponse currentPresets = current.CurrentPresets;
            if (currentPresets == null) return;
            if (currentPresets.containers == null) return;
            for (int i = 0; i < currentPresets.containers.Length; i++)
            {
                PresetData existing = currentPresets.containers[i];
                if (existing == null) continue;
                if (existing.id != updatedPreset.id) continue;
                currentPresets.containers[i] = updatedPreset;
                this.MarkItemPresetDirty(current);
                return;
            }
        }

        private void MarkItemPresetDirty(ItemPreset current)
        {
#if UNITY_EDITOR
            if (current == null) return;
            UnityEditor.EditorUtility.SetDirty(current);
#endif
        }

        private int GetFilledSlotCount(PresetData preset)
        {
            if (preset == null) return 0;
            if (preset.slots == null) return 0;
            int count = 0;
            foreach (PresetSlotData slot in preset.slots)
            {
                if (slot == null) continue;
                if (string.IsNullOrWhiteSpace(slot.inventory_item_id)) continue;
                count++;
            }
            return count;
        }

        private void SetCardCountLoading()
        {
            if (this.cardCountLabel == null) return;
            this.cardCountLabel.text = "Card Count: ...";
        }

        private void SetCardCount(int count)
        {
            if (this.cardCountLabel == null) return;
            this.cardCountLabel.text = $"Card Count: {count}";
        }

        public void Dispose()
        {
            this.ClearDeskTabs();
        }
    }
}
