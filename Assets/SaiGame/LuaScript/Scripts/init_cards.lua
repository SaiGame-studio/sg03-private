require "lib_battle_common"
require "lib_battle_ai"
require "lib_ability_config"
require "lib_ability_core"
require "lib_battle_entity_ai"
require "enemy_ai_core"
require "enemy_ai_goblin_shaman"
require "enemy_ai_silas"
require "lib_ability_human"
require "lib_ability_darkborn"
require "lib_ability_lightborn"
require "lib_ability_natureborn"

-- init_cards
-- Draws opening hands for both alpha and omega.
--   Alpha (5 cards):
--     Cards 1-3 : matched from alpha_preset_metadata by inventory_item_id in alpha_the_source.
--     Cards 4-5 : drawn randomly from alpha_the_source.
--   Omega (N cards):
--     Each choose_card_X in omega_preset_metadata holds an item_definition_code_name.
--     One matching card is picked per slot from omega_the_source.
-- Drawn cards are removed from their respective source pools.
--
-- Endpoint: POST /api/v1/games/{game_id}/scripts/init_cards/run
-- Example payload (session_id is optional; omit to use the current active session):
-- {
--   "session_id": "battle-session-uuid"
-- }

-- ── Inlined helpers (from lib_battle_ai) ────────────────────────────────────
local function gen_id()
    local t = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"
    return string.gsub(t, "[xy]", function(c)
        local v = (c == "x") and math.random(0, 15) or math.random(8, 11)
        return string.format("%x", v)
    end)
end

local function find_and_remove(list, inventory_item_id)
    for i, item in ipairs(list) do
        if item.inventory_item_id == inventory_item_id then
            table.remove(list, i)
            return item
        end
    end
    return nil
end

local function find_and_remove_by_code(list, code)
    for i, item in ipairs(list) do
        if item.item_definition_code_name == code then
            table.remove(list, i)
            return item
        end
    end
    return nil
end

local function find_by_definition_code(list, code)
    for _, item in ipairs(list or {}) do
        if item.item_definition_code_name == code then return item end
    end
    return nil
end

local function find_by_inventory_item_id(list, inventory_item_id)
    for _, item in ipairs(list or {}) do
        if item.inventory_item_id == inventory_item_id then return item end
    end
    return nil
end

local function alpha_draw_random(state, card_count, start_slot)
    lib_battle_common.dlog("[init_cards] == alpha_draw_random == card_count: " .. tostring(card_count))
    start_slot = start_slot or 0
    local source = state.alpha_the_source
    if source == nil then
        return nil, "alpha_the_source not found in session state"
    end
    math.randomseed(ctx.timestamp)
    local hand = {}
    for _ = 1, card_count do
        if #source == 0 then break end
        local idx = math.random(1, #source)
        table.insert(hand, source[idx])
        table.remove(source, idx)
    end
    for i = #hand + 1, card_count do hand[i] = {} end
    for i, hand_card in ipairs(hand) do
        if hand_card.item_definition_code_name ~= nil and hand_card.item_definition_code_name ~= "" then
            hand_card.slot_index  = start_slot + i - 1
            hand_card.trigger     = false
        end
    end
    for _, hand_card in ipairs(hand) do
        if hand_card.inventory_item_id ~= nil and hand_card.inventory_item_id ~= "" then
            lib_battle_common.append_client_action(state,
                "alpha_source_to_hand:" .. hand_card.inventory_item_id .. "," .. (hand_card.slot_index or 0))
        end
    end
    return hand, nil
end

local function omega_draw_random(state, card_count, start_slot)
    lib_battle_common.dlog("[init_cards] == omega_draw_random == card_count: " .. tostring(card_count))
    start_slot = start_slot or 0
    local source = state.omega_the_source
    if source == nil then
        return nil, "omega_the_source not found in session state"
    end
    math.randomseed(ctx.timestamp)
    local hand = {}
    for _ = 1, card_count do
        if #source == 0 then break end
        local idx                     = math.random(1, #source)
        source[idx].id                = gen_id()
        source[idx].inventory_item_id = gen_id()
        table.insert(hand, source[idx])
        table.remove(source, idx)
    end
    for i = #hand + 1, card_count do hand[i] = {} end
    for i, hand_card in ipairs(hand) do
        if hand_card.item_definition_code_name ~= nil and hand_card.item_definition_code_name ~= "" then
            hand_card.slot_index  = start_slot + i - 1
            hand_card.trigger     = false
        end
    end
    for _, hand_card in ipairs(hand) do
        if hand_card.inventory_item_id ~= nil and hand_card.inventory_item_id ~= "" then
            lib_battle_common.append_client_action(state,
                "omega_source_to_hand:" .. hand_card.inventory_item_id .. "," .. (hand_card.slot_index or 0))
        end
    end
    return hand, nil
end
-- ─────────────────────────────────────────────────────────────────────────────

-- Draws Alpha's opening hand from the preset cards chosen by the player
-- (choose_card_1/2/3 from alpha_preset_metadata). Empty preset slots are skipped.
-- Returns: hand, err
local function alpha_choose_cards(state)
    lib_battle_common.dlog("[init_cards] == alpha_choose_cards ==")
    local preset = state.alpha_preset_metadata
    if preset == nil then
        return nil, "alpha_preset_metadata not found in session state"
    end

    local source = state.alpha_the_source
    if source == nil then
        return nil, "alpha_the_source not found in session state"
    end

    local slot_names   = { "choose_card_1", "choose_card_2", "choose_card_3" }
    local preset_uuids = { preset.choose_card_1, preset.choose_card_2, preset.choose_card_3 }
    local hand = {}
    for i, uid in ipairs(preset_uuids) do
        if uid ~= nil and uid ~= "" then
            local card = find_and_remove(source, uid)
            if card == nil then
                -- Void takes precedence over a hand choice, including cards
                -- automatically moved because they have at least four stars.
                if find_by_inventory_item_id(state.alpha_the_void, uid) ~= nil then
                    lib_battle_common.dlog("[init_cards] Skipped hand choice " .. slot_names[i] .. " (" .. uid .. ") because the card is in alpha_the_void")
                else
                    return nil, "preset card " .. slot_names[i] .. " (" .. uid .. ") not found in alpha_the_source"
                end
            else
                card.slot_index  = #hand
                card.trigger     = false
                table.insert(hand, card)
            end
        end
    end

    for _, hand_card in ipairs(hand) do
        lib_battle_common.append_client_action(state,
            "alpha_source_to_hand:" .. hand_card.inventory_item_id .. "," .. (hand_card.slot_index or 0))
    end
    lib_battle_common.dlog("[init_cards] alpha_choose_cards: " .. tostring(#hand) .. " cards")

    return hand, nil
end

-- Draws Omega's opening hand: exactly the preset cards from choose_card_1/2/3
-- in metadata.omega.metadata. No random fill.
-- Returns: hand, err
local function omega_choose_cards(state)
    lib_battle_common.dlog("[init_cards] == omega_choose_cards ==")
    if state.metadata == nil or state.metadata.omega == nil then
        return nil, "metadata.omega not found in session state"
    end
    local preset = state.metadata.omega.metadata
    if preset == nil then
        return nil, "metadata.omega.metadata not found in session state"
    end

    local source = state.omega_the_source
    if source == nil then
        return nil, "omega_the_source not found in session state"
    end

    local slot_keys = { "choose_card_1", "choose_card_2", "choose_card_3" }
    local slots     = {}
    for _, key in ipairs(slot_keys) do
        local code = preset[key]
        if code ~= nil and code ~= "" then
            table.insert(slots, { key = key, code = code })
        end
    end

    if #slots == 0 then
        return nil, "metadata.omega.metadata has no choose_card slots"
    end

    local hand = {}
    for i, slot in ipairs(slots) do
        local card = find_and_remove_by_code(source, slot.code)
        if card == nil then
            -- Automatic Void rules take precedence over the opening-hand preset,
            -- matching Alpha's behavior for a selected card already in the Void.
            if find_by_definition_code(state.omega_the_void, slot.code) ~= nil then
                lib_battle_common.dlog("[init_cards] Skipped omega hand choice " .. slot.key .. " (" .. slot.code .. ") because the card is in omega_the_void")
            else
                return nil, "omega preset " .. slot.key .. " (" .. slot.code .. ") not found in omega_the_source"
            end
        else
            card.id                = gen_id()
            card.inventory_item_id = gen_id()
            card.slot_index        = #hand
            card.trigger           = false
            table.insert(hand, card)
        end
    end

    for _, hand_card in ipairs(hand) do
        lib_battle_common.append_client_action(state,
            "omega_source_to_hand:" .. hand_card.inventory_item_id .. "," .. (hand_card.slot_index or 0))
    end
    lib_battle_common.dlog("[init_cards] omega_choose_cards: " .. tostring(#hand) .. " cards")

    return hand, nil
end

-- Draws alpha's opening hand: preset chosen cards plus random cards to reach 5.
-- Returns err or nil.
local function alpha_init_cards(state)
    local alpha_hand, alpha_err = alpha_choose_cards(state)
    if alpha_err ~= nil then return alpha_err end

    local random_hand, random_err = alpha_draw_random(state, 5 - #alpha_hand, #alpha_hand)
    if random_err ~= nil then return random_err end
    for _, card in ipairs(random_hand) do table.insert(alpha_hand, card) end

    state.alpha_hand = alpha_hand
    return nil
end

-- Moves cards whose definition is assigned to the_void or Character with at
-- least four stars out of a side's source before that side draws its opening hand.
local function move_auto_void_cards(state, side)
    local source_key = side .. "_the_source"
    local void_key = side .. "_the_void"
    local source = state[source_key]
    if source == nil then
        return source_key .. " not found in session state"
    end
    if state[void_key] == nil then
        state[void_key] = {}
    end

    local function move_to_void(card, reason)
        -- Enemy source cards are given their inventory ID only when they leave
        -- the source. Void actions require that ID just like hand actions do.
        if card.inventory_item_id == nil or card.inventory_item_id == "" then
            card.inventory_item_id = gen_id()
        end
        table.insert(state[void_key], card)
        lib_battle_common.append_card_sent_to_void_client_action(state, side, card)
        lib_battle_common.dlog("[init_cards] Moved " .. side .. " card to void (" .. reason .. "): " .. card.inventory_item_id)
    end

    local defs_by_code = {}
    for _, item_def in ipairs(state.item_defs or {}) do
        if item_def.item_code ~= nil and item_def.item_code ~= "" then
            defs_by_code[item_def.item_code] = item_def
        end
    end

    local function get_card_stars(card)
        local item_def = defs_by_code[card.item_definition_code_name]
        local stars = item_def ~= nil and item_def.base_stats ~= nil and item_def.base_stats.star or nil
        return tonumber(stars) or 0
    end

    local function get_auto_void_reason(card)
        local item_def = defs_by_code[card.item_definition_code_name]
        local metadata = item_def ~= nil and item_def.metadata or nil
        if metadata ~= nil and metadata.location == "the_void" then
            return "metadata.location=the_void"
        end

        local card_type = metadata ~= nil and metadata.type or nil
        if card_type == "character" and get_card_stars(card) >= 4 then
            return tostring(get_card_stars(card)) .. "-star character"
        end

        return nil
    end

    -- Iterate backwards while removing so every remaining source card is checked.
    for i = #source, 1, -1 do
        local card = source[i]
        local reason = get_auto_void_reason(card)
        if reason ~= nil then
            table.remove(source, i)
            move_to_void(card, reason)
        end
    end

    return nil
end

-- Moves explicitly selected Alpha void cards, then lets the shared automatic
-- Void rules process every remaining Alpha source card.
local function alpha_init_void(state)
    lib_battle_common.dlog("[init_cards] == alpha_init_void ==")
    local preset = state.alpha_preset_metadata
    if preset == nil then
        return "alpha_preset_metadata not found in session state"
    end

    local source = state.alpha_the_source
    if source == nil then
        return "alpha_the_source not found in session state"
    end
    if state.alpha_the_void == nil then
        state.alpha_the_void = {}
    end

    local function move_to_void(card, reason)
        table.insert(state.alpha_the_void, card)
        lib_battle_common.append_card_sent_to_void_client_action(state, "alpha", card)
        lib_battle_common.dlog("[init_cards] Moved alpha card to void (" .. reason .. "): " .. card.inventory_item_id)
    end

    -- Explicit void choices keep preset order and may contain any card type
    -- or star level. They take precedence over opening-hand choices.
    local slot_names = { "void_card_1", "void_card_2", "void_card_3", "void_card_4", "void_card_5", "void_card_6", "void_card_7" }
    for _, key in ipairs(slot_names) do
        local uid = preset[key]
        if uid ~= nil and uid ~= "" then
            local card = find_by_inventory_item_id(source, uid)
            if card ~= nil then
                find_and_remove(source, uid)
                move_to_void(card, key)
            else
                lib_battle_common.dlog("[init_cards] Warning: void card " .. key .. " (" .. uid .. ") not found in alpha_the_source")
            end
        end
    end

    return move_auto_void_cards(state, "alpha")
end

-- Draws omega's opening hand: selected cards plus random cards to reach five.
-- Returns err or nil.
local function omega_init_cards(state)
    local omega_hand, omega_err = omega_choose_cards(state)
    if omega_err ~= nil then return omega_err end

    local random_hand, random_err = omega_draw_random(state, 5 - #omega_hand, #omega_hand)
    if random_err ~= nil then return random_err end
    for _, card in ipairs(random_hand) do table.insert(omega_hand, card) end

    state.omega_hand = omega_hand
    return nil
end

-- ─── Omega deploy dispatch ──────────────────────────────────────────────────

local function run_omega_deploy(state)
    return lib_battle_entity_ai.deploy_enemy(state)
end

local function main()
    local session_id, sid_err = lib_battle_common.resolve_session_id()
    if sid_err ~= nil then
        output.error = sid_err; return
    end
    lib_battle_common.dlog("[init_cards] session resolved: " .. tostring(session_id))

    local state, load_err = lib_battle_common.load_session(session_id)
    if load_err ~= nil then
        output.error = load_err; return
    end

    local void_err = alpha_init_void(state)
    if void_err ~= nil then
        output.error = void_err; return
    end

    local omega_void_err = move_auto_void_cards(state, "omega")
    if omega_void_err ~= nil then
        output.error = omega_void_err; return
    end

    local alpha_err = alpha_init_cards(state)
    if alpha_err ~= nil then
        output.error = alpha_err; return
    end

    local omega_err = omega_init_cards(state)
    if omega_err ~= nil then
        output.error = omega_err; return
    end

    local deploy_err = run_omega_deploy(state)
    if deploy_err ~= nil then
        output.error = deploy_err; return
    end

    lib_battle_common.append_client_action(state, "alpha_take_lamp")

    state.action     = (state.action or 0) + 1
    state.updated_at = ctx.timestamp
    if state.metadata == nil then state.metadata = {} end
    state.metadata.next_move = "alpha_turn"
    lib_battle_common.append_client_action(state, "next_move:alpha_turn")
    state.omega_defending = true
    lib_battle_common.append_client_action(state, "omega_defending")

    local save_err = game.battle_session_update(session_id, state)
    if save_err ~= nil then
        output.error = save_err; return
    end
    lib_battle_common.dlog("[init_cards] session persisted, next_move = alpha_turn, omega_defending = true")

    lib_battle_common.battle_status()
end

-- ─── Functions ───────────────────────────────────────────────────────────────

main()
