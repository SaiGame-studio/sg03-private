using System;

namespace SG03.UI
{
    [Serializable]
    public class BattleStatusMetadata
    {
        public string alpha_id;
        public string battle_difficulty;
        public string battle_mode;
        public string enemy_entity_key;
        public string next_move;
        public string session_id;
        public long started_at;
        public int duration_seconds;
        public BattleStatusMetadataOmega omega;
    }
}
