# Lua Error Localization Catalog

This catalog documents all error messages produced by backend Lua scripts located under `Assets/SaiGame/LuaScript/Scripts/*.lua`.
It standardizes every raw error string into a unique `ErrorKey`, regex pattern, and parameter list for C# Unity localization.

---

## 1. Session & Battle State Errors

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_SESSION_NOT_FOUND` | `current battle session not found` / `no active battle session found` / `battle session not found` | No active battle session found. | Không tìm thấy phiên trận đấu. | `battle_debug_turn.lua`, `battle_end.lua`, `lib_battle_common.lua`, `get_card_definitions.lua` |
| `ERR_SESSION_RESOLVE_FAILED` | `failed to resolve session_id` | Failed to resolve battle session ID. | Không thể xác định ID phiên đấu. | `alpha_card_active.lua` |
| `ERR_BATTLE_ALREADY_COMPLETED` | `battle is already completed` | The battle has already completed. | Trận đấu đã kết thúc. | `alpha_card_active.lua`, `alpha_card_deploy.lua`, `alpha_defending_end.lua`, `alpha_turn_end.lua`, `battle_debug_turn.lua` |
| `ERR_BATTLE_STATE_SAVE_FAILED` | `failed to save battle state: {0}` / `failed to save item_defs to battle state: {0}` | Failed to save battle state: {0}. | Không thể lưu trạng thái trận đấu. | `alpha_card_active.lua`, `alpha_card_deploy.lua`, `alpha_defending_end.lua`, `get_card_definitions.lua` |

---

## 2. Payload & Input Validation Errors

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_PAYLOAD_MISSING_ATTACKER` | `missing attacker_inventory_item_id` | Missing attacker card ID in request. | Thiếu ID thẻ tấn công trong yêu cầu. | `alpha_card_active.lua` |
| `ERR_PAYLOAD_MISSING_DEFENDER` | `missing defender_inventory_item_id` | Missing defender card ID in request. | Thiếu ID thẻ phòng thủ trong yêu cầu. | `alpha_card_active.lua` |
| `ERR_PAYLOAD_HAND_REQUIRED` | `hand is required` | Hand card list is required. | Danh sách thẻ bài trên tay là bắt buộc. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_HAND_INVALID_TYPE` | `hand must be an array of inventory_item_ids` | Hand must be an array of item IDs. | Bài trên tay phải là danh sách ID thẻ. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_FRONT_LINE_INVALID` | `front_line must be an array of inventory_item_ids` | Front line must be an array of item slots. | Hàng trước phải là danh sách ô thẻ hợp lệ. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_BACK_LINE_INVALID` | `back_line must be an array of inventory_item_ids` | Back line must be an array of item slots. | Hàng sau phải là danh sách ô thẻ hợp lệ. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_HAND_ITEM_NOT_STRING` | `hand[{0}] must be a string` | Invalid card ID format in hand at index {0}. | Định dạng ID thẻ trên tay không hợp lệ tại vị trí {0}. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_DUPLICATE_HAND_ITEM` | `duplicate inventory_item_id in hand: {0}` | Duplicate card ID found in hand: {0}. | Thẻ bị trùng lặp trên tay: {0}. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_SLOT_NOT_OBJECT` | `front_line[{0}] must be an object` / `back_line[{0}] must be an object` | Invalid slot data at line index {0}. | Dữ liệu vị trí thẻ không hợp lệ tại index {0}. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_SLOT_INVALID_INDEX` | `front_line[{0}].slot_index must be an integer in [0, 4]` / `back_line[{0}]...` | Slot index must be between 0 and 4. | Vị trí ô thẻ phải nằm trong khoảng từ 0 đến 4. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_CARD_CONFLICT` | `inventory_item_id {0} appears in both hand and front_line` / `appears in {1} and back_line` | Card {0} cannot be placed in multiple locations. | Thẻ {0} xuất hiện ở nhiều vị trí cùng lúc. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_CARD_NOT_IN_HAND` | `front_line[{0}] card ({1}) not found in alpha_hand` / `back_line[{0}]...` | Card {1} is not present in hand. | Thẻ {1} không có trên tay của người chơi. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_CARD_NOT_IN_STATE` | `payload item ({0}) does not exist in battle state` / `state item ({0}) is missing from payload` | Deploy data does not match current battle state. | Thẻ {0} không khớp với trạng thái trận đấu. | `alpha_card_deploy.lua` |
| `ERR_PAYLOAD_TARGET_INVALID` | `target must be 'alpha' or 'omega'` | Target must be either alpha or omega. | Mục tiêu phải là alpha hoặc omega. | `battle_debug_turn.lua` |
| `ERR_PAYLOAD_HP_REQUIRED` | `hp is required` | HP parameter is required. | Giá trị HP là bắt buộc. | `battle_debug_turn.lua` |

---

## 3. Battle Phase & Deployment Rules

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_DEPLOY_WRONG_PHASE` | `alpha_card_deploy can only run when next_move is alpha_turn` | Deployment can only be performed during your turn. | Chỉ có thể ra thẻ trong lượt của bạn. | `alpha_card_deploy.lua` |
| `ERR_DEPLOY_HAND_EMPTY` | `alpha_hand is empty; run init_cards first` | Hand is empty. Battle initialization required. | Bài trên tay trống. Cần khởi tạo trận đấu trước. | `alpha_card_deploy.lua` |
| `ERR_DEPLOY_CHARACTER_LIMIT` | `only 1 new character card may be deployed per turn ({0} new character cards received)` / `only 1 character card may be deployed per turn (a character card was already deployed in turn {0})` | You can only deploy 1 character card per turn. | Bạn chỉ được phép ra tối đa 1 thẻ nhân vật mỗi lượt. | `alpha_card_deploy.lua` |
| `ERR_ACTION_ALREADY_ATTACKED` | `attacker card has already attacked this turn` | This card has already attacked this turn. | Thẻ này đã tấn công trong lượt này. | `alpha_card_active.lua` |
| `ERR_ACTION_CANNOT_ATTACK_OWN_HP` | `cannot attack own hp` | Cannot attack your own HP. | Không thể tấn công HP của chính mình. | `alpha_card_active.lua` |
| `ERR_ACTION_OMEGA_FRONT_LINE_NOT_EMPTY` | `cannot attack omega while omega front line still has cards` | Cannot attack Omega directly while front line cards remain. | Không thể tấn công Omega khi hàng trước còn bài. | `alpha_card_active.lua` |
| `ERR_ACTION_ALPHA_FRONT_LINE_NOT_EMPTY` | `cannot attack alpha_hp while alpha front line still has characters` | Cannot attack Alpha directly while front line characters remain. | Không thể tấn công Alpha khi hàng trước còn bài. | `alpha_defending_end.lua` |
| `ERR_SUMMON_REQUIRES_CARD` | `summon requires a card` | Summon operation requires a valid card. | Thao tác triệu hồi yêu cầu thẻ hợp lệ. | `lib_battle_common.lua` |
| `ERR_SUMMON_TURN_RESTRICTED` | `{0}-star card can only be summoned from turn 4: {1}` | {0}-star cards can only be summoned starting from turn 4 ({1}). | Thẻ {0} sao ({1}) chỉ triệu hồi từ lượt 4 trở đi. | `lib_battle_common.lua` |
| `ERR_SUMMON_STAR_MISSING` | `summon card star is missing: {0}` | Star definition missing for card: {0}. | Thẻ {0} thiếu thông tin cấp sao. | `lib_battle_common.lua` |

---

## 4. Deck Building & Game Config

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_CONFIG_MODE_REQUIRED` | `battle_mode is required (fast, normal, long)` | Battle mode selection is required. | Yêu cầu chọn chế độ trận đấu. | `battle_start.lua` |
| `ERR_CONFIG_MODE_INVALID` | `battle_mode must be one of: fast, normal, long` | Invalid battle mode selected. | Chế độ trận đấu không hợp lệ. | `battle_start.lua` |
| `ERR_CONFIG_ENEMY_KEY_REQUIRED` | `enemy_entity_key is required` | Enemy entity key is required. | Thiếu thông tin đối thủ. | `battle_start.lua` |
| `ERR_CONFIG_PRESET_ID_REQUIRED` | `preset_instance_id is required` | Deck preset ID is required. | Thiếu ID bộ thẻ. | `battle_start.lua` |
| `ERR_CONFIG_ACTIVE_SESSION_EXISTS` | `player already has an active battle session` | You already have an active battle session in progress. | Bạn đang có một trận đấu chưa hoàn thành. | `battle_start.lua` |
| `ERR_CONFIG_ENEMY_NOT_FOUND` | `enemy not found` | Selected enemy definition not found. | Không tìm thấy dữ liệu đối thủ. | `battle_start.lua` |
| `ERR_CONFIG_PRESET_NOT_FOUND` | `preset not found` | Selected deck preset not found. | Không tìm thấy bộ thẻ đã chọn. | `battle_start.lua` |
| `ERR_DECK_TOO_SMALL` | `player deck must have at least 25 cards (has {0})` / `enemy deck...` | Deck must contain at least 25 cards (currently has {0}). | Bộ thẻ phải có ít nhất 25 lá (hiện có {0} lá). | `battle_start.lua` |
| `ERR_DECK_TOO_LARGE` | `player deck must have fewer than 52 cards (has {0})` / `enemy deck...` | Deck cannot contain 52 or more cards (currently has {0}). | Bộ thẻ phải ít hơn 52 lá (hiện có {0} lá). | `battle_start.lua` |
| `ERR_DECK_MISSING_ITEM_DEF` | `player deck contains a card without an item definition` / `player deck item definition not found: {0}` | Deck contains an invalid or undefined card. | Bộ thẻ chứa lá bài không hợp lệ. | `battle_start.lua` |
| `ERR_DECK_ITEM_DEF_CODE_MISSING` | `player deck item definition has no item code: {0}` | Deck card definition has no item code. | Định nghĩa lá bài trong bộ thẻ thiếu mã định danh. | `battle_start.lua` |
| `ERR_DECK_COPY_LIMIT_EXCEEDED` | `player deck cannot contain more than 3 copies of card {0}` | Cannot include more than 3 copies of card {0}. | Không thể chứa quá 3 bản sao của lá {0}. | `battle_start.lua` |
| `ERR_SOUL_DEF_NOT_FOUND` | `soul item definition not found` | Currency (Soul) definition not found. | Không tìm thấy định nghĩa Linh Hồn (Soul). | `battle_start.lua` |
| `ERR_SOUL_INSUFFICIENT` | `not enough soul to start battle (requires 5)` | Not enough Soul energy to enter battle (Requires 5). | Không đủ Linh Hồn để đấu (Yêu cầu 5). | `battle_start.lua` |
| `ERR_CARD_DEF_MISSING` | `attacker card has no item_definition_code_name` / `item def not found...` | Card definition details not found: {0}. | Không tìm thấy định nghĩa lá bài: {0}. | `alpha_card_active.lua`, `alpha_defending_end.lua` |
| `ERR_GACHA_PACK_NOT_FOUND` | `gacha pack not found (id: {0}): {1}` | Reward pack not found: {0}. | Không tìm thấy gói phần thưởng: {0}. | `battle_end.lua` |
| `ERR_GACHA_PACK_OPEN_FAILED` | `failed to open gacha pack '{0}'...` | Failed to claim reward pack: {0}. | Không thể mở gói phần thưởng: {0}. | `battle_end.lua` |

---

## 5. Combat & Target Preconditions

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_ATTACKER_NOT_ON_FIELD` | `attacker card not found in any battle line` / `attacker card not found: {0}` | Attacking card is no longer on the battle line. | Lá bài tấn công không còn trên bàn đấu. | `alpha_card_active.lua`, `alpha_defending_end.lua` |
| `ERR_DEFENDER_NOT_FOUND` | `defender card not found in battle state` / `defender card not found: {0}` | Target defender card not found. | Không tìm thấy lá bài phòng thủ mục tiêu. | `alpha_card_active.lua`, `alpha_defending_end.lua` |
| `ERR_ATTACKER_NOT_CHARACTER` | `attacker is not a character` | Only character cards can perform standard attacks. | Chỉ có thẻ nhân vật mới tấn công thường được. | `alpha_card_active.lua`, `alpha_defending_end.lua` |
| `ERR_TARGET_OUTSIDE_LINES` | `defender card is outside battle lines and attacker has no attack ability` | Target is outside battle lines and cannot be attacked normally. | Mục tiêu nằm ngoài hàng đấu, không thể tấn công. | `alpha_card_active.lua` |
| `ERR_TARGET_POSITION_DISALLOWED` | `target position is not allowed for this ability: {0}` | Target position is out of reach for this ability. | Vị trí mục tiêu nằm ngoài tầm tác dụng. | `alpha_card_active.lua` |

---

## 6. Ability Skill Executions

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_ABILITY_REQUIRES_CARD_TARGET` | `{0} requires a specific card target` | Ability {0} requires a targeted card. | Kỹ năng {0} yêu cầu chọn lá bài mục tiêu. | `alpha_card_active.lua` |
| `ERR_ABILITY_CANNOT_TARGET_PLAYER_HP` | `{0} cannot target player hp` | Ability {0} cannot target player HP. | Kỹ năng {0} không thể nhắm HP người chơi. | `alpha_card_active.lua` |
| `ERR_ABILITY_ANIMATE_DEAD_NO_RIA` | `animate_dead requires ria in front_line` | Animate Dead requires Ria on the front line. | Gọi Hồn yêu cầu Ria ở hàng trước. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_ANIMATE_DEAD_ATK_INVALID` | `animate_dead requires a positive base_stats.atk` | Animate Dead requires a positive ATK value. | Gọi Hồn yêu cầu chỉ số ATK dương. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_KING_RETURN_NO_RIA_TARGET` | `king_return requires a Ria target` / `king_return target must be Ria` / `king_return target must be on own front_line` | King Return requires targeting Ria on your front line. | King Return yêu cầu nhắm Ria ở hàng trước của bạn. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_KING_RETURN_SOURCE_INVALID` | `king_return source card is not on a battle line` | King Return must be activated from a battle line. | King Return phải được kích hoạt từ một hàng trên sân. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_KING_RETURN_NO_SKELETON` | `king_return requires at least 2 Skeleton in own front_line` | King Return requires at least two Skeleton cards on your front line. | King Return yêu cầu ít nhất hai Skeleton ở hàng trước của bạn. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_KING_RETURN_NO_KING_VOID` | `king_return requires Skeleton King in own the_void` | King Return requires Skeleton King in the void. | King Return yêu cầu Skeleton King trong Hư Không. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_KING_RETURN_ATK_INVALID` | `king_return requires a positive base_stats.atk` | King Return requires a positive ATK value. | King Return yêu cầu chỉ số ATK dương. | `lib_ability_advanced.lua` |
| `ERR_ABILITY_EAGLE_EYE_NO_TARGET` | `eagle_eye requires a target card` | Eagle Eye requires a target card. | Mắt Đại Bàng yêu cầu chọn lá bài mục tiêu. | `lib_ability_human.lua` |
| `ERR_ABILITY_EAGLE_EYE_NOT_ON_LINE` | `eagle_eye source card is not on a battle line` | Source card for Eagle Eye is not on the battle line. | Mắt Đại Bàng phải ở trên hàng đấu. | `lib_ability_human.lua` |
| `ERR_ABILITY_EAGLE_EYE_MUST_BE_CHAR` | `eagle_eye can target only a character card` | Eagle Eye can only target character cards. | Mắt Đại Bàng chỉ nhắm vào thẻ nhân vật. | `lib_ability_human.lua` |
| `ERR_ABILITY_EAGLE_EYE_MUST_BE_FACEDOWN` | `eagle_eye requires a face-down character target` | Eagle Eye requires a face-down enemy character. | Mắt Đại Bàng yêu cầu thẻ nhân vật đang úp. | `lib_ability_human.lua` |
| `ERR_ABILITY_EAGLE_EYE_NO_LYRA` | `eagle_eye requires Lyra on the caster's front_line` | Eagle Eye requires Lyra on your front line. | Mắt Đại Bàng yêu cầu Lyra ở hàng trước. | `lib_ability_human.lua` |
| `ERR_ABILITY_SPINNING_SLASH_NO_AZURE` | `spinning_slash requires untriggered azure_blade in front_line` | Spinning Slash requires an active Azure Blade on the front line. | Trảm Phong yêu cầu Azure Blade ở hàng trước. | `lib_ability_human.lua` |
| `ERR_ABILITY_CROSS_GUARD_NO_AZURE` | `cross_guard requires untriggered azure_blade in front_line` | Cross Guard requires an active Azure Blade on the front line. | Thuẫn Hộ Vệ yêu cầu Azure Blade ở hàng trước. | `lib_ability_human.lua` |
| `ERR_ABILITY_TOTEM_PULSE_NO_SHAMAN` | `totem_pulse requires untriggered goblin_shaman in front_line` | Totem Pulse requires an active Goblin Shaman on the front line. | Mạch Phù Thủy yêu cầu Goblin Shaman ở hàng trước. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_BACK_STAB_NO_GRUNT` | `back_stab requires untriggered goblin_grunt in front_line` | Back Stab requires an active Goblin Grunt on the front line. | Đâm Lén yêu cầu Goblin Grunt chưa kích hoạt ở hàng trước. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_BACK_STAB_SELF_TARGET` | `back_stab cannot target the selected goblin_grunt` | Back Stab cannot target the acting Goblin Grunt itself. | Đâm Lén không thể tự nhắm vào Goblin Grunt đang thực hiện kỹ năng. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_BRUTE_CALL_NO_SHAMAN` | `brute_call requires a Goblin Shaman target` / `brute_call target must be Goblin Shaman` / `brute_call requires an untriggered Goblin Shaman in own front_line` | Brute Call requires targeting an untriggered Goblin Shaman on your front line. | Triệu Hồi Đồ Tể yêu cầu Goblin Shaman chưa kích hoạt ở hàng trước. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_BRUTE_CALL_NOT_ON_LINE` | `brute_call source card is not on a battle line` / `target must be on own front_line` | Brute Call must be cast on your own front line. | Triệu Hồi Đồ Tể phải ở hàng trước. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_BRUTE_CALL_NO_BRUTE_VOID` | `brute_call requires Goblin Brute in own the_void` | Brute Call requires Goblin Brute in the void. | Triệu Hồi Đồ Tể yêu cầu Goblin Brute ở Hư Không. | `lib_ability_natureborn.lua` |
| `ERR_ABILITY_HOLY_GLOW_NO_ELF` | `holy_glow requires an untriggered female Lightborn character in front_line` | Holy Glow requires an untriggered female Lightborn Character on the front line. | Hào Quang Thánh Linh yêu cầu một Character Lightborn nữ chưa kích hoạt ở hàng trước. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_STATIC_BIND_NO_TARGET` | `static_bind requires a target character` | Static Bind requires a target character. | Static Bind yêu cầu một Character mục tiêu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_STATIC_BIND_TARGET_INVALID` | `static_bind can target only a character card` / `static_bind target must be on a battle line` | Static Bind can target only a character on the battlefield. | Static Bind chỉ nhắm Character trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_STATIC_BIND_SOURCE_INVALID` | `static_bind source card is not on a battle line` | Static Bind source card is not on the battlefield. | Lá Static Bind phải ở trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_STATIC_BIND_NO_AZURA` | `static_bind requires an untriggered Azura in front_line` | Static Bind requires an untriggered Azura on your front line. | Static Bind yêu cầu Azura chưa kích hoạt ở hàng trước của bạn. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_STATIC_BIND_DAMAGE_INVALID` | `static_bind requires a positive base_stats.atk` | Static Bind requires a positive ATK value. | Static Bind yêu cầu chỉ số atk dương. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_NO_TARGET` | `lightning_strike requires a target character` | Lightning Strike requires a target character. | Lightning Strike yêu cầu một Character mục tiêu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_TARGET_INVALID` | `lightning_strike can target only a character card` / `lightning_strike target must be on a battle line` / `lightning_strike target requires a slot_index` | Lightning Strike requires a slotted Character on the battlefield. | Lightning Strike yêu cầu một Character có vị trí trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_SOURCE_INVALID` | `lightning_strike source card is not on a battle line` | Source card for Lightning Strike is not on the battlefield. | Lá Lightning Strike phải ở trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_NO_AZURA` | `lightning_strike requires an untriggered Azura in front_line` | Lightning Strike requires an active Azura on your front line. | Lightning Strike yêu cầu Azura chưa kích hoạt ở hàng trước của bạn. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_DAMAGE_INVALID` | `lightning_strike requires a positive base_stats.atk` | Lightning Strike requires a positive ATK value. | Lightning Strike yêu cầu chỉ số atk dương. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LIGHTNING_STRIKE_TARGET_STAR_MISSING` | `lightning_strike target star is missing: {0}` | Lightning Strike requires a star value for each target. | Lightning Strike yêu cầu chỉ số sao của mỗi mục tiêu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_SKELETON_SHIELD_NO_RIA` | `skeleton_shield requires ria in front_line` | Skeleton Shield requires Ria on the front line. | Khiên Xương yêu cầu Ria ở hàng trước. | `lib_ability_darkborn.lua` |
| `ERR_ABILITY_SKELETON_SHIELD_NO_SKELETON` | `skeleton_shield requires skeleton in front_line different from target_card` | Skeleton Shield requires a Skeleton card to swap positions. | Khiên Xương yêu cầu 1 lá Skeleton khác đỡ đòn. | `lib_ability_darkborn.lua` |
| `ERR_ABILITY_SKELETON_SHIELD_NOT_TARGETED` | `skeleton_shield requires target card to be targeted by opponent planning attack` | Target is not currently targeted by an incoming enemy attack. | Mục tiêu không bị đối thủ nhắm tấn công. | `lib_ability_darkborn.lua` |
| `ERR_ABILITY_ABYSSAL_MIST_SOURCE_INVALID` | `abyssal_mist source card is not on the battlefield` | Abyssal Mist must be activated from the battlefield. | Abyssal Mist phải được kích hoạt trên bàn đấu. | `lib_ability_aura.lua` |
| `ERR_ABILITY_ABYSSAL_MIST_ALREADY_ACTIVE` | `abyssal_mist is already active` | Abyssal Mist is already active. | Abyssal Mist đang hoạt động. | `lib_ability_aura.lua` |
| `ERR_ABILITY_ABYSSAL_MIST_NO_MISTHY` | `abyssal_mist requires untriggered misthy on the battlefield` | Abyssal Mist requires an active Misthy on the battlefield. | Abyssal Mist yêu cầu Misthy chưa kích hoạt trên bàn đấu. | `lib_ability_aura.lua` |
| `ERR_ABILITY_ABYSSAL_MIST_STATS_INVALID` | `abyssal_mist requires positive base_stats.atk_added and base_stats.def_added` | Abyssal Mist requires positive ATK and DEF bonus values. | Abyssal Mist yêu cầu chỉ số cộng ATK và DEF dương. | `lib_ability_aura.lua` |
| `ERR_ABILITY_LUX_MAXIMA_NO_TARGET` | `lux_maxima requires a target Darkborn Aura` | Lux Maxima requires a Darkborn Aura target. | Lux Maxima yêu cầu một Aura Darkborn mục tiêu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LUX_MAXIMA_SOURCE_INVALID` | `lux_maxima source card is not on a battle line` | Lux Maxima must be activated from a battle line. | Lux Maxima phải được kích hoạt trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LUX_MAXIMA_NO_DIANA` | `lux_maxima requires Diana on the battlefield` | Lux Maxima requires Diana on the battlefield. | Lux Maxima yêu cầu Diana có mặt trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_LUX_MAXIMA_TARGET_INVALID` | `lux_maxima target must be on a battle line` / `lux_maxima target must be a configured Darkborn Aura` | Lux Maxima can target only a configured Darkborn Aura on the battlefield. | Lux Maxima chỉ nhắm một Aura Darkborn đã cấu hình trên bàn đấu. | `lib_ability_lightborn.lua` |
| `ERR_ABILITY_TITAN_FALL_NO_HUMAN` | `titan_fall requires a Human target card` / `titan_fall target must be a Human character` | Titan Fall requires targeting a Human character. | Titan Giáng Thế yêu cầu mục tiêu Tộc Người. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_FALL_NOT_ATTACKED` | `titan_fall target is not being attacked` | Titan Fall target is not currently being attacked. | Mục tiêu Titan Giáng Thế không bị tấn công. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_FALL_DEF_BUFF_LOW` | `titan_fall target requires at least +{0} DEF` | Target requires at least +{0} DEF buff to activate Titan Fall. | Mục tiêu cần tăng ít nhất +{0} Giáp. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_FALL_SURVIVES` | `titan_fall cannot trigger: target Human would survive with {0} DEF remaining` | Cannot trigger Titan Fall: target Human would survive. | Titan Giáng Thế không thể dùng vì Tộc Người chưa bị diệt. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_FALL_NO_AZURE` | `titan_fall requires azure_blade on the field` | Titan Fall requires Azure Blade on the battlefield. | Titan Giáng Thế yêu cầu Azure Blade trên bàn. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_FALL_NO_TITAN_VOID` | `titan_fall requires titan in the_void` | Titan Fall requires Titan in the void. | Titan Giáng Thế yêu cầu Titan trong Hư Không. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_SPEAR_NO_TITAN` | `titan_spear_sweep requires Titan on the battlefield` | Titan Spear Sweep requires Titan on the battlefield. | Quét Giáo Titan yêu cầu Titan trên bàn đấu. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_SPEAR_NOT_READY` | `titan_spear_sweep requires Titan to be ready` | Titan Spear Sweep requires Titan to be untriggered. | Quét Giáo Titan yêu cầu Titan chưa kích hoạt. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_SPEAR_ATK_INVALID` | `titan_spear_sweep requires a positive base_stats.atk` | Titan Spear Sweep requires a positive ATK value. | Quét Giáo Titan yêu cầu chỉ số ATK dương. | `lib_ability_mid_game.lua` |
| `ERR_ABILITY_TITAN_SPEAR_SHOCKWAVE_ATK_INVALID` | `titan_spear_sweep requires a positive base_stats.shockwave_atk` | Titan Spear Sweep requires a positive shockwave attack value. | Quét Giáo Titan yêu cầu chỉ số dư chấn dương. | `lib_ability_mid_game.lua` |

---

## 7. Xena Awakened & Ritual Abilities

| Error Key | Original Pattern / Server Text | English Default Text | Vietnamese Reference | Source Files |
| :--- | :--- | :--- | :--- | :--- |
| `ERR_XENA_INVALID_CONFIG` | `xena_awakened requires valid configuration` | Invalid awakening skill configuration. | Thức Tỉnh Xena thiếu cấu hình hợp lệ. | `lib_ability_xena.lua` |
| `ERR_XENA_REQUIRES_TARGET` | `{0} requires a target card` | Skill {0} requires a target card. | Kỹ năng {0} yêu cầu lá bài mục tiêu. | `lib_ability_xena.lua` |
| `ERR_XENA_TARGET_MUST_BE_CHAR` | `{0} target must be a Character` | Target must be a character card. | Mục tiêu phải là một thẻ nhân vật. | `lib_ability_xena.lua` |
| `ERR_XENA_TARGET_MUST_BE_FORM` | `{0} target must be {1}` | The awakening skill can only target its matching Xena form. | Kỹ năng Thức Tỉnh chỉ có thể nhắm đúng hình thái Xena tương ứng. | `lib_ability_xena.lua` |
| `ERR_XENA_TARGET_NOT_ATTACKED` | `{0} target is not being attacked` | Target is not under attack. | Mục tiêu không bị tấn công. | `lib_ability_xena.lua` |
| `ERR_XENA_REQUIRES_SACRIFICE` | `{0} requires {1} adjacent allied card(s) to sacrifice` | Skill {0} requires {1} adjacent allied card(s) to sacrifice. | Kỹ năng {0} yêu cầu hiến tế {1} lá đồng minh. | `lib_ability_xena.lua` |
| `ERR_XENA_REQUIRES_SUCCESSOR` | `{0} requires {1} in own the_void` | Skill {0} requires {1} in your void zone. | Kỹ năng {0} yêu cầu lá {1} trong Hư Không. | `lib_ability_xena.lua` |
| `ERR_DEMON_RITE_NO_TRIGGER_CARD` | `demon_rite requires a triggering card` | Demon Rite must be triggered by another card. | Nghi Lễ Quỷ Dữ phải qua thẻ bài khác. | `lib_ability_xena.lua` |
| `ERR_DEMON_RITE_DIRECT_DISABLED` | `demon_rite cannot be activated directly by Demon Rite` | Demon Rite cannot be cast directly. | Nghi Lễ Quỷ Dữ không thể tự kích hoạt. | `lib_ability_xena.lua` |
| `ERR_DEMON_RITE_TARGET_MISMATCH` | `demon_rite target must be allied with its triggering card` | Ritual target must be an allied card. | Mục tiêu Nghi Lễ Quỷ Dữ phải là thẻ đồng minh. | `lib_ability_xena.lua` |
| `ERR_DEMON_RITE_MISSING_RITE` | `missing_demon_rite` | Demon Rite is not deployed on your back line. | Demon Rite chưa được triển khai ở hậu tuyến của bạn. | `lib_ability_xena.lua` |
| `ERR_DEMON_RITE_MISSING_ORBS` | `missing_demon_orbs` | Demon Orbs is not deployed on your back line. | Demon Orbs chưa được triển khai ở hậu tuyến của bạn. | `lib_ability_xena.lua` |
| `ERR_XENA4_TARGET_MUST_BE_XENA4` | `xena_awakened4 target must be xena4` | Xena Awakened IV can only target Xena IV. | Thức Tỉnh IV chỉ áp dụng trên Xena IV. | `lib_ability_xena.lua` |
