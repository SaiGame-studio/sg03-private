-- lib_battle_common
-- Shared helpers used across battle scripts.
-- is_library = true

-- ─── Helpers ─────────────────────────────────────────────────────────────────

-- Returns true if the card's item definition metadata.type matches card_type.
-- Looks up the definition from item_defs using card.item_definition_code_name.
function check_card_type(item_defs, card, card_type)
    dlog("== check_card_type ==")
    if card == nil then return false end
    if item_defs == nil then return false end
    local code = card.item_definition_code_name
    if code == nil then return false end
    for _, item_def in ipairs(item_defs) do
        if item_def.item_code == code then
            return item_def.metadata ~= nil and item_def.metadata.type == card_type
        end
    end
    return false
end

-- Returns the item definition with the given item_code, or nil when absent.
function find_item_def(item_defs, item_code)
    if item_defs == nil or item_code == nil then return nil end
    for _, item_def in ipairs(item_defs) do
        if item_def.item_code == item_code then return item_def end
    end
    return nil
end

-- Returns true when card is a Character whose metadata.race is one of the
-- supplied allowed_races values.
function is_character_of_races(item_defs, card, allowed_races)
    if card == nil or type(allowed_races) ~= "table" then return false end
    local item_def = find_item_def(item_defs, card.item_definition_code_name)
    local metadata = item_def ~= nil and item_def.metadata or nil
    if metadata == nil or metadata.type ~= "character" then return false end
    for _, allowed_race in ipairs(allowed_races) do
        if metadata.race == allowed_race then return true end
    end
    return false
end

-- Clears the first slot with matching inventory_item_id from a fixed-size line.
-- Preserves the line length by replacing the slot with {} instead of removing it.
-- Returns true if a card was cleared, false otherwise.
function remove_card_from_line(line, inventory_item_id)
    dlog("== remove_card_from_line ==")
    if line == nil then return false end
    for i, card in ipairs(line) do
        if card.inventory_item_id == inventory_item_id then
            line[i] = {}
            return true
        end
    end
    return false
end

-- ─── reset_card_turn_state ──────────────────────────────────────────────────
-- Resets per-turn state for a single card using item_defs to restore final_def.
--   trigger              → false
--   final_def            → base_stats.def from item_defs (0 if not found)
--   total_damage_received → 0
-- No-ops if card has no item_definition_code_name.
local function is_card_on_battlefield(state, inventory_item_id)
    if state == nil or inventory_item_id == nil or inventory_item_id == "" then return false end
    for _, line in ipairs({
        state.alpha_front_line or {}, state.alpha_back_line or {},
        state.omega_front_line or {}, state.omega_back_line or {},
    }) do
        for _, card in ipairs(line) do
            if card.inventory_item_id == inventory_item_id then return true end
        end
    end
    return false
end

local function get_active_persistent_bonus(state, bonuses)
    local total = 0
    if type(bonuses) ~= "table" then return total end
    for source_id, bonus in pairs(bonuses) do
        if is_card_on_battlefield(state, source_id) then
            total = total + (tonumber(bonus) or 0)
        else
            bonuses[source_id] = nil
        end
    end
    return total
end

function reset_card_turn_state(item_defs, reset_card, state)
    if reset_card == nil then return end
    if reset_card.item_definition_code_name == nil or reset_card.item_definition_code_name == "" then return end
    local base_atk = 0
    local base_def = 0
    if item_defs ~= nil then
        for _, item_def in ipairs(item_defs) do
            if item_def.item_code == reset_card.item_definition_code_name then
                base_atk = (item_def.base_stats and item_def.base_stats.atk) or 0
                base_def = (item_def.base_stats and item_def.base_stats.def) or 0
                break
            end
        end
    end
    reset_card.final_atk             = base_atk + get_active_persistent_bonus(state, reset_card.persistent_atk_bonuses)
    reset_card.final_def             = base_def + get_active_persistent_bonus(state, reset_card.persistent_def_bonuses)
    reset_card.total_damage_received = 0
end

-- Returns an attacker's planned damage. An ability with base_stats.atk_added
-- borrows the ATK of metadata.char_code_required only when that character is
-- present in the same side's Front Line.
function get_attack_damage(state, attacker_def, attacker_line_key, attacker_card)
    if attacker_def == nil then return 0 end

    local base_stats = attacker_def.base_stats or {}
    local base_atk = tonumber(base_stats.atk)
        or tonumber(attacker_def.metadata ~= nil and attacker_def.metadata.atk or nil)
        or 0
    local persistent_atk_added = get_active_persistent_bonus(
        state,
        attacker_card ~= nil and attacker_card.persistent_atk_bonuses or nil
    )
    if base_stats.atk_added == nil then return base_atk + persistent_atk_added end

    local required_character_code = attacker_def.metadata ~= nil and attacker_def.metadata.char_code_required
    local added_atk = tonumber(base_stats.atk_added) or 0
    if required_character_code == nil or required_character_code == "" then return 0 end

    local attacker_side = string.sub(attacker_line_key or "", 1, 5) == "alpha"
        and "alpha"
        or "omega"
    local front_line = attacker_side == "alpha"
        and (state.alpha_front_line or {})
        or (state.omega_front_line or {})
    for _, card in ipairs(front_line) do
        local character_def = nil
        for _, item_def in ipairs(state.item_defs or {}) do
            if item_def.item_code == card.item_definition_code_name then
                character_def = item_def
                break
            end
        end
        local character_code_required = character_def ~= nil and character_def.metadata ~= nil
            and character_def.metadata.char_code_required or nil
        if character_def ~= nil and character_def.metadata ~= nil
            and character_def.metadata.type == "character"
            and (character_def.item_code == required_character_code or character_code_required == required_character_code) then
            local character_stats = character_def.base_stats or {}
            local character_atk = tonumber(character_stats.atk)
                or tonumber(character_def.metadata.atk)
                or 0
            return character_atk + added_atk + persistent_atk_added
        end
    end

    return 0
end

-- Returns an error when a 4-star-or-higher card is summoned before turn 4.
-- Cards with 1-3 stars are not turn-restricted.
function validate_summon_card_turn(state, item_defs, summon_card)
    if summon_card == nil then return "summon requires a card" end

    local summon_def = nil
    for _, item_def in ipairs(item_defs or {}) do
        if item_def.item_code == summon_card.item_definition_code_name then
            summon_def = item_def
            break
        end
    end
    if summon_def == nil then
        return "summon card definition not found: " .. tostring(summon_card.item_definition_code_name)
    end

    local stars = summon_def.base_stats ~= nil and tonumber(summon_def.base_stats.star) or nil
    if stars == nil then
        return "summon card star is missing: " .. tostring(summon_card.item_definition_code_name)
    end
    if stars < 4 then return nil end

    local current_turn = tonumber(state ~= nil and state.turn or nil) or 0
    if current_turn < 4 then
        return tostring(stars) .. "-star card can only be summoned from turn 4: " ..
            tostring(summon_card.item_definition_code_name)
    end
    return nil
end

-- ─── append_client_action ───────────────────────────────────────────────────
-- Appends a client action with an auto-incremented index prefix.
-- Format: [index]:[action_name] or [index]:[action_name]:[params]
function append_client_action(state, action)
    local index = #state.client_actions + 1
    table.insert(state.client_actions, index .. ":" .. action)
end

-- Reveals a card and appends its expose action before its move-to-void action.
-- This is the single ordering rule for every path that removes a card from a
-- battle line, so the client never animates a hidden card directly into void.
function append_card_sent_to_void_action(actions, side, card)
    if card == nil or card.inventory_item_id == nil or card.inventory_item_id == "" then return end
    card.face_up = true
    card.expose = true
    table.insert(actions, side .. "_card_expose:" .. card.inventory_item_id)
    table.insert(actions, side .. "_card_sent_to_void:" .. card.inventory_item_id)
end

-- State-backed variant used by scripts which append actions directly to the
-- session queue instead of returning an ability action list.
function append_card_sent_to_void_client_action(state, side, card)
    if card == nil or card.inventory_item_id == nil or card.inventory_item_id == "" then return end
    local is_already_exposed = card.face_up == true and card.expose == true
    card.face_up = true
    card.expose = true
    if not is_already_exposed then
        append_client_action(state, side .. "_card_expose:" .. card.inventory_item_id)
    end
    append_client_action(state, side .. "_card_sent_to_void:" .. card.inventory_item_id)
end

-- ─── reset_turn_cards ───────────────────────────────────────────────────────
-- Marks a Character to be unavailable for its next turn. The flag is kept
-- separately from trigger because trigger is normally reset at every handoff.
function mark_card_skip_next_turn(card)
    if card == nil then return end
    card.skip_next_turn = true
    card.trigger = true
end

-- Resets per-turn state only for the side whose turn is about to begin. A card
-- used during the previous side's turn therefore remains triggered throughout
-- the opponent's turn. A card marked skip_next_turn remains triggered when its
-- owner's next turn begins; the marker is then consumed, so the following
-- handoff readies it normally.
function reset_turn_cards(state, next_active_side)
    dlog("== reset_turn_cards next_active_side=" .. tostring(next_active_side) .. " ==")

    local lines = {
        { side = "alpha", line = state.alpha_front_line or {} },
        { side = "alpha", line = state.alpha_back_line  or {} },
        { side = "omega", line = state.omega_front_line or {} },
        { side = "omega", line = state.omega_back_line  or {} },
    }
    for _, line_data in ipairs(lines) do
        if line_data.side == next_active_side then
            for _, reset_card in ipairs(line_data.line) do
                reset_card_turn_state(state.item_defs, reset_card, state)
                if reset_card.skip_next_turn == true then
                    reset_card.trigger = true
                    reset_card.skip_next_turn = nil
                else
                    reset_card.trigger = false
                end
            end
        end
    end
end

-- ─── get_draw_card_count ────────────────────────────────────────────────────
-- Returns the number of cards a player draws at the start of their turn.
function get_draw_card_count()
    dlog("== get_draw_card_count ==")
    return 2
end

-- ─── get_hand_size ───────────────────────────────────────────────────────────
-- Returns the maximum number of card slots in a player's hand.
function get_hand_size()
    return 5
end

-- ─── dlog ────────────────────────────────────────────────────────────────────
-- Appends msg to output.debug_log only when ctx.game.status == "development".
-- Safe to call unconditionally; no-ops in production.
function dlog(msg)
    if ctx.game == nil or ctx.game.status ~= "development" then return end
    if output.debug_log == nil then output.debug_log = {} end
    table.insert(output.debug_log, msg)
end

-- ─── hide_unrevealed_omega_cards ─────────────────────────────────────────────
-- Strips sensitive fields from omega cards that are not yet revealed to alpha.
-- Clears item_definition_code_name and final_def for any card where
-- face_up == false or expose == false.
function hide_unrevealed_omega_cards(state)
    local omega_battle_lines = { state.omega_front_line or {}, state.omega_back_line or {} }
    for _, omega_line in ipairs(omega_battle_lines) do
        for _, omega_card in ipairs(omega_line) do
            if omega_card.face_up == false or omega_card.expose == false then
                omega_card.item_definition_code_name = nil
                omega_card.final_def                 = nil
            end
        end
    end
end

-- ─── Session helpers ──────────────────────────────────────────────────────────

-- Returns session_id from payload.session_id if provided, otherwise fetches the current active session.
function resolve_session_id()
    if payload.session_id ~= nil and payload.session_id ~= "" then
        return payload.session_id, nil
    end
    local sid, sid_err = game.battle_session_current_id()
    if sid_err ~= nil then return nil, sid_err end
    if sid == nil or sid == "" then return nil, "no active battle session found" end
    return sid, nil
end

-- Loads and returns battle session state for the given session_id.
function load_session(session_id)
    local state, err = game.battle_session_get(session_id)
    if err ~= nil then return nil, err end
    if state == nil then return nil, "battle session not found" end
    return state, nil
end

-- ─── battle_status helpers ────────────────────────────────────────────────────

local function resolve_battle_session()
    local session_id, id_err = game.battle_session_current_id()
    if id_err ~= nil then return nil, nil, id_err end
    if session_id == nil or session_id == "" then
        return nil, nil, "no active battle session"
    end
    local state, get_err = game.battle_session_get(session_id)
    if get_err ~= nil then return nil, nil, get_err end
    if state == nil then return nil, nil, "battle session not found" end
    return session_id, state, nil
end

local function mask_omega_hand(state, is_development)
    output.omega_hand = state.omega_hand
    if not is_development and state.omega_hand ~= nil then
        for _, omega_hand_slot in ipairs(state.omega_hand) do
            omega_hand_slot.item_definition_code_name = nil
        end
    end
end

local function write_alpha_state_output(state, session_id)
    output.session_id             = session_id
    output.alpha_hp               = state.alpha_hp
    output.alpha_max_hp           = state.alpha_max_hp
    output.alpha_the_source       = state.alpha_the_source
    output.alpha_the_source_count = state.alpha_the_source ~= nil and #state.alpha_the_source or 0
    output.alpha_the_void         = state.alpha_the_void
    output.alpha_the_void_count   = state.alpha_the_void ~= nil and #state.alpha_the_void or 0
    output.alpha_hand             = state.alpha_hand
    output.alpha_front_line       = state.alpha_front_line
    output.alpha_back_line        = state.alpha_back_line
end

local function write_omega_state_output(state)
    output.omega_hp               = state.omega_hp
    output.omega_max_hp           = state.omega_max_hp
    output.omega_the_source_count = state.omega_the_source ~= nil and #state.omega_the_source or 0
    output.omega_the_void         = state.omega_the_void
    output.omega_the_void_count   = state.omega_the_void ~= nil and #state.omega_the_void or 0
    output.omega_front_line       = state.omega_front_line
    output.omega_back_line        = state.omega_back_line
end

local function build_card_action_list(state)
    local card_action_list = {}
    if state.item_defs == nil then return card_action_list end
    for _, item_def in ipairs(state.item_defs) do
        if item_def.item_code ~= nil and item_def.metadata ~= nil and item_def.metadata.action ~= nil then
            local action_entry = {}
            action_entry.item_code = item_def.item_code
            card_action_list[#card_action_list + 1] = action_entry
        end
    end
    return card_action_list
end

local function write_battle_meta_output(state)
    output.turn            = state.turn
    output.action          = state.action
    output.status          = state.status
    output.metadata        = state.metadata
    output.omega_planning  = state.omega_planning
    output.alpha_defending = state.alpha_defending
    output.omega_defending = state.omega_defending
    output.client_actions  = state.client_actions
end

-- ─── battle_status ───────────────────────────────────────────────────────────
-- Reads the current battle session and writes its full state into output.

function battle_status()
    dlog("== battle_status ==")
    local session_id, state, resolve_err = resolve_battle_session()
    if resolve_err ~= nil then output.error = resolve_err ; return end

    local is_development  = ctx.game ~= nil and ctx.game.status == "development"
    output.is_development = is_development
    output.game_status    = ctx.game ~= nil and ctx.game.status or nil

    mask_omega_hand(state, is_development)
    write_alpha_state_output(state, session_id)
    hide_unrevealed_omega_cards(state)
    write_omega_state_output(state)
    output.item_defs         = is_development and state.item_defs or nil
    -- output.item_defs_actions = build_card_action_list(state)
    write_battle_meta_output(state)
end

-- ─── card_attack_card helpers ────────────────────────────────────────────────

local function side_from_line_key(line_key)
    if line_key == nil then return "alpha" end
    if string.sub(line_key, 1, 5) == "alpha" then return "alpha" end
    return "omega"
end

-- Reveal both cards before combat resolution. The matching client actions are
-- appended here (rather than after damage resolution) so the client animates
-- the reveal before attack, damage, and any move to the void.
local function expose_attack_pair(state, attacker_side, defender_side, attacker_card, defender_card)
    attacker_card.face_up = true
    attacker_card.expose  = true
    defender_card.face_up = true
    defender_card.expose  = true
    append_client_action(state, attacker_side .. "_card_expose:" .. attacker_card.inventory_item_id)
    append_client_action(state, defender_side .. "_card_expose:" .. defender_card.inventory_item_id)
end

local function fire_on_attack(state, attacker_card, attacker_def, defender_card, defender_def, defender_line_key, defender_side_void, damage_dealt)
    local atk_event_data = {}
    atk_event_data.defender_card      = defender_card
    atk_event_data.damage_dealt       = damage_dealt
    atk_event_data.attacker_def       = attacker_def
    atk_event_data.defender_def       = defender_def
    atk_event_data.defender_line_key  = defender_line_key
    atk_event_data.defender_side_void = defender_side_void

    if attacker_def.metadata ~= nil and attacker_def.metadata.type == "ability" then
        local ability_key = attacker_card.item_definition_code_name
        return lib_ability_core.trigger_ability_by_key(state, attacker_card, ability_key, "on_attack", atk_event_data)
    end
    return lib_ability_core.trigger_card_ability(state, attacker_card, "on_attack", atk_event_data)
end

local function fire_on_damaged(state, attacker_card, attacker_def, defender_card, defender_def, damage_dealt)
    local def_event_data = {}
    def_event_data.attacker_card   = attacker_card
    def_event_data.damage_received = damage_dealt
    def_event_data.attacker_def    = attacker_def
    def_event_data.defender_def    = defender_def
    return lib_ability_core.trigger_card_ability(state, defender_card, "on_damaged", def_event_data)
end

local function fire_pending_aura_refresh(state)
    if state.aura_refresh_requested ~= true then return {} end
    state.aura_refresh_requested = nil
    return lib_ability_aura.refresh_active_auras(state, "aura_source_deployed")
end

local function append_attack_client_actions(state, attacker_side, defender_side, attacker_card, defender_card, dmg_actions, atk_actions, def_actions, aura_actions)
    append_client_action(state, attacker_side .. "_attack:" .. attacker_card.inventory_item_id .. "," .. defender_card.inventory_item_id)
    for _, action in ipairs(dmg_actions) do append_client_action(state, action) end
    for _, action in ipairs(atk_actions) do append_client_action(state, action) end
    for _, action in ipairs(def_actions) do append_client_action(state, action) end
    for _, action in ipairs(aura_actions) do append_client_action(state, action) end
end

local function send_ability_attacker_to_void(state, attacker_card, attacker_line_key, attacker_def, attacker_side)
    if attacker_def.metadata == nil or attacker_def.metadata.type ~= "ability" then return end
    local attacker_void_key = attacker_side .. "_the_void"
    remove_card_from_line(state[attacker_line_key], attacker_card.inventory_item_id)
    if state[attacker_void_key] == nil then state[attacker_void_key] = {} end
    table.insert(state[attacker_void_key], attacker_card)
    append_card_sent_to_void_client_action(state, attacker_side, attacker_card)
    dlog("attacker is ability-type, sent to " .. attacker_void_key .. ": " .. attacker_card.inventory_item_id)
end

-- ─── card_attack_card ────────────────────────────────────────────────────────
-- Reusable: makes one card attack another. Handles expose, damage application,
-- attacker.trigger flag, on_attack / on_damaged ability triggers, client action
-- accumulation, and moves an ability-type attacker to its side's void.
-- Sides are inferred from attacker_line_key and defender_side_void.
-- Returns: err (nil on success).
function card_attack_card(state, attacker_card, attacker_def, attacker_line_key, defender_card, defender_def, defender_line_key, defender_side_void, damage_dealt)
    dlog("== card_attack_card ==")
    local attacker_side = side_from_line_key(attacker_line_key)
    local defender_side = (defender_side_void == "alpha_the_void") and "alpha" or "omega"

    expose_attack_pair(state, attacker_side, defender_side, attacker_card, defender_card)

    local dmg_actions, dmg_err = lib_ability_core.deal_damage_to_character(
        state, attacker_card, defender_card, damage_dealt, state[defender_line_key], defender_side_void
    )
    if dmg_err ~= nil then return dmg_err end

    attacker_card.trigger = true

    local atk_actions, atk_err = fire_on_attack(state, attacker_card, attacker_def, defender_card, defender_def, defender_line_key, defender_side_void, damage_dealt)
    if atk_err ~= nil then return atk_err end

    local def_actions, def_err = fire_on_damaged(state, attacker_card, attacker_def, defender_card, defender_def, damage_dealt)
    if def_err ~= nil then return def_err end

    local aura_actions = fire_pending_aura_refresh(state)
    append_attack_client_actions(
        state, attacker_side, defender_side, attacker_card, defender_card,
        dmg_actions, atk_actions, def_actions, aura_actions)

    send_ability_attacker_to_void(state, attacker_card, attacker_line_key, attacker_def, attacker_side)

    return nil
end
