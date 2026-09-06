using System;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Represents a single card slot in a battle zone (hand, back line, front line).
    /// item_definition_code_name is the lookup key.
    /// </summary>
    [Serializable]
    public class BattleCardSlot
    {
        public string id;
        public string container_id;
        public string created_at;
        public int    slot_index;
        public string item_definition_code_name;
        public string inventory_item_id;
        public string item_definition_id;
        public string item_definition_name;
        public string card_action;
        public bool   face_up = false;
        public bool   expose  = false;
        public bool   trigger = false;
        public int    final_atk;
        public int    final_def;
        public int    total_damage_received;

        public CardActionType CardAction => ParseCardAction(this.card_action);

        private static CardActionType ParseCardAction(string value)
        {
            if (string.IsNullOrEmpty(value)) return CardActionType.unknown;
            if (System.Enum.TryParse(value, out CardActionType result)) return result;
            return CardActionType.unknown;
        }
    }
}
