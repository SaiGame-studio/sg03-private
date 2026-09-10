using System;

namespace SG03.UI
{
    [Serializable]
    public class BattlePlanningEntry
    {
        public string action;
        public string attacker_inv_id;
        public string defender_inv_id;
    }

    [Serializable]
    public class BattleStatusOutput
    {
        public int    turn;
        public int    action;
        public int    alpha_hp;
        public int    omega_hp;
        public BattleCardSlot[] alpha_the_source;
        public BattleCardSlot[] alpha_hand;
        public BattleCardSlot[] alpha_back_line;
        public BattleCardSlot[] alpha_front_line;
        public int                 alpha_the_source_count;
        public int                 omega_the_source_count;
        public BattleCardSlot[]    omega_the_source;
        public int                 alpha_the_void_count;
        public int                 omega_the_void_count;
        public BattleCardSlot[] alpha_the_void;
        public BattleCardSlot[] omega_the_void;
        public BattleCardSlot[] omega_hand;
        public int             omega_hand_count;
        public BattleCardSlot[] omega_front_line;
        public BattleCardSlot[] omega_back_line;
        public BattlePlanningEntry[] omega_planning;
        public string              next_move;
        public string[]            client_actions;
        public string[]            debug_log;
        public string              error;
        public string              battle_difficulty;
        public string              status;
        public bool                alpha_defending;
        public bool                omega_defending;
        public bool                is_development;
        public BattleStatusMetadata metadata;
    }
}
