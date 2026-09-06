-- lib_ability_config
-- is_library = true

-- Valid target_positions:
--   own_frontline, own_backline, own_hand, own_void, own_source
--   enemy_frontline, enemy_backline, enemy_hand, enemy_void, enemy_source
-- handler_group is required and selects the library containing `<ability_key>_execute`.
--
-- Các trường cấu hình Ability:
--   event (string, không bắt buộc): Sự kiện mà dispatcher được phép kích hoạt.
--   target_positions (string[], không bắt buộc): Các vùng mục tiêu hợp lệ.
--   requires_target_card (boolean, không bắt buộc): true khi Ability phải có
--       một lá bài mục tiêu cụ thể, không thể dùng lên người chơi/HP.
--   resolves_without_attack (boolean, không bắt buộc): true khi lá Ability
--       chỉ thực hiện hiệu ứng, không đi qua luồng tấn công và gây sát thương.
function get_ability_config(ability_key)
    local configs = {
        twin_reaper = {
            handler_group = "character_passives",
            event = "on_attack",
            target_positions = { "enemy_frontline" },
        },
        scout_strike = {
            handler_group = "character_passives",
            event = "on_attack",
            target_positions = { "enemy_frontline" },
        },
        mist_execution = {
            handler_group = "character_passives",
            event = "on_attack",
            target_positions = { "enemy_frontline", "enemy_backline" },
        },
        eagle_eye = {
            handler_group = "human",
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        spinning_slash = {
            handler_group = "human",
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
        },
        cross_guard = {
            handler_group = "human",
            target_positions = { "own_frontline" },
            requires_target_card = true,
        },
        totem_pulse = {
            handler_group = "natureborn",
            target_positions = { "own_frontline" },
        },
        back_stab = {
            handler_group = "natureborn",
            target_positions = { "enemy_frontline" },
            requires_target_card = true,
        },
        brute_call = {
            handler_group = "natureborn",
            target_positions = { "own_frontline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        holy_glow = {
            handler_group = "lightborn",
            target_positions = { "own_frontline", "own_backline", "own_source", "own_void" },
            can_target_player_hp = true,
        },
        static_bind = {
            handler_group = "lightborn",
            target_positions = { "enemy_frontline", "enemy_backline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        lightning_strike = {
            handler_group = "lightborn",
            target_positions = { "enemy_frontline", "enemy_backline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        skeleton_shield = {
            handler_group = "darkborn",
            target_positions = { "own_frontline" },
            requires_target_card = true,
        },
        abyssal_mist = {
            handler_group = "aura",
            target_positions = { "own_frontline", "own_backline" },
            resolves_without_attack = true,
        },
        animate_dead = {
            handler_group = "advanced",
            target_positions = { "own_frontline", "own_backline", "own_source", "own_void" },
            resolves_without_attack = true,
        },
        king_return = {
            handler_group = "advanced",
            target_positions = { "own_frontline" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        titan_fall = {
            handler_group = "mid_game",
            target_positions = { "own_frontline" },
            requires_target_card = true,
        },
        titan_spear_sweep = {
            handler_group = "mid_game",
            target_positions = {
                "enemy_frontline",
                "own_source", "enemy_source",
            },
            resolves_without_attack = true,
        },
        xena_awakened1 = {
            handler_group = "xena",
            target_positions = { "own_frontline", "own_backline", "own_void" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        xena_awakened2 = {
            handler_group = "xena",
            target_positions = { "own_frontline", "own_backline", "own_void" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        xena_awakened3 = {
            handler_group = "xena",
            target_positions = { "own_frontline", "own_backline", "own_void" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
        xena_awakened4 = {
            handler_group = "xena",
            target_positions = { "own_frontline", "own_backline", "own_void" },
            requires_target_card = true,
            resolves_without_attack = true,
        },
    }
    return configs[ability_key]
end
