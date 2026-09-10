-- lib_battle_ai  (is_library = true)
-- AI logic for Omega deploying cards into front_line / back_line,
-- similar to card_deploy.lua but runs automatically based on difficulty.
--
-- Usage from a main script:
--   require "lib_battle_ai"
--   local front, back, hand, err = lib_battle_ai.deploy_omega_cards(state, difficulty)
--   if err ~= nil then ... end
--   state.omega_front_line = front
--   state.omega_back_line  = back
--   state.omega_hand       = hand

local SLOT_COUNT = 5 -- number of slots per line (matches card_deploy.lua)

-- ── Helpers ──────────────────────────────────────────────────────────────────

-- Returns real cards from omega_hand, skipping empty slots {}.
function _collect_cards(hand)
    local cards = {}
    for _, slot in ipairs(hand) do
        if slot.item_definition_code_name ~= nil and slot.item_definition_code_name ~= "" then
            table.insert(cards, slot)
        end
    end
    return cards
end

-- Returns the first source index matching Omega's configured draw priority.
-- Chosen cards are considered in choose_card_1 through choose_card_3 order.
function find_omega_source_choice_index(state, source)
    local omega = state.metadata ~= nil and state.metadata.omega or nil
    local preset = omega ~= nil and omega.metadata or nil
    if preset == nil then return nil end

    for choice_index = 1, 3 do
        local code = preset["choose_card_" .. choice_index]
        if code ~= nil and code ~= "" then
            for source_index, card in ipairs(source or {}) do
                if card.item_definition_code_name == code then return source_index end
            end
        end
    end
    return nil
end

-- Finds an item def in state.item_defs (array) by item_code.
function _find_item_def(item_defs, code)
    if item_defs == nil then return nil end
    for _, def in ipairs(item_defs) do
        if def.item_code == code then return def end
    end
    return nil
end

-- Returns the attack value that Alpha is allowed to see while Omega plans.
-- Never expose stats for a hidden card.
function _get_visible_omega_attacker_atk(state, attacker_card)
    if attacker_card == nil or attacker_card.face_up ~= true or attacker_card.expose ~= true then
        return 0
    end

    local attacker_def = _find_item_def(state.item_defs, attacker_card.item_definition_code_name)
    if attacker_def == nil then return 0 end
    if attacker_def.base_stats ~= nil and attacker_def.base_stats.atk ~= nil then
        return attacker_def.base_stats.atk
    end
    if attacker_def.metadata ~= nil and attacker_def.metadata.atk ~= nil then
        return attacker_def.metadata.atk
    end
    return 0
end

-- Builds a labeled client action so each parameter is self-describing.
function build_omega_planning_character_attack_action(state, attacker_card, defender_id)
    local visible_atk = _get_visible_omega_attacker_atk(state, attacker_card)
    return "omega_planing_character_attack:attacker_card_id=" .. attacker_card.inventory_item_id ..
        ",defender_card_id=" .. defender_id .. ",atk=" .. tostring(visible_atk)
end

-- Splits cards into two groups: characters (front-eligible) and others (back only).
-- Game rule: only character-type cards may be placed in the front line.
function _split_cards_by_type(cards, item_defs)
    local characters = {}
    local others     = {}
    for _, checked_card in ipairs(cards) do
        local item_def  = _find_item_def(item_defs, checked_card.item_definition_code_name)
        local card_type = item_def ~= nil and item_def.metadata ~= nil and item_def.metadata.type or nil
        if card_type == "character" then
            table.insert(characters, checked_card)
        else
            table.insert(others, checked_card)
        end
    end
    return characters, others
end

-- Builds a SLOT_COUNT-slot line from card_list, assigning slot_index / face_up / expose / card_action.
function _build_line(card_list, face_up, card_action)
    local line = {}
    for i = 1, SLOT_COUNT do
        line[i] = {}
    end
    for i, card in ipairs(card_list) do
        if i > SLOT_COUNT then break end
        card.slot_index = i - 1
        card.face_up    = face_up
        card.expose     = face_up
        -- card.card_action = card_action
        line[i]         = card
    end
    return line
end

-- Rebuilds omega_hand after deploying some cards (removes deployed cards).
function _rebuild_hand(original_hand, deployed_ids)
    local deployed = {}
    for _, id in ipairs(deployed_ids) do
        deployed[id] = true
    end

    local hand = {}
    for i = 1, SLOT_COUNT do
        hand[i] = {}
    end

    local slot = 1
    for _, card in ipairs(original_hand) do
        if slot > SLOT_COUNT then break end
        local id = card.id
        if id ~= nil and id ~= "" and not deployed[id] then
            card.slot_index = slot - 1
            hand[slot]      = card
            slot            = slot + 1
        end
    end

    return hand
end

-- ── Draw helpers ─────────────────────────────────────────────────────────────

function _gen_id()
    local t = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx"
    return string.gsub(t, "[xy]", function(c)
        local v = (c == "x") and math.random(0, 15) or math.random(8, 11)
        return string.format("%x", v)
    end)
end

function _find_and_remove(list, inventory_item_id)
    for i, item in ipairs(list) do
        if item.inventory_item_id == inventory_item_id then
            table.remove(list, i)
            return item
        end
    end
    return nil
end

function _find_and_remove_by_code(list, code)
    for i, item in ipairs(list) do
        if item.item_definition_code_name == code then
            table.remove(list, i)
            return item
        end
    end
    return nil
end

-- ── alpha_draw_random ─────────────────────────────────────────────────────────
-- Draws up to card_count (default: get_draw_card_count()) random cards from
-- alpha_the_source into the first available empty slots of state.alpha_hand.
-- Hand always maintains get_hand_size() total slots.
-- Returns err or nil.
function alpha_draw_random(state, card_count)
    card_count      = card_count or lib_battle_common.get_draw_card_count()
    local hand_size = lib_battle_common.get_hand_size()
    lib_battle_common.dlog("[lib_battle_ai] == alpha_draw_random == card_count=" .. tostring(card_count))

    local source = state.alpha_the_source
    if source == nil then
        return "alpha_the_source not found in session state"
    end

    if state.alpha_hand == nil then state.alpha_hand = {} end
    while #state.alpha_hand < hand_size do
        table.insert(state.alpha_hand, {})
    end

    math.randomseed(ctx.timestamp)

    local drawn = 0
    for i = 1, hand_size do
        if drawn >= card_count then break end
        if #source == 0 then break end
        local slot = state.alpha_hand[i]
        if slot == nil or slot.inventory_item_id == nil or slot.inventory_item_id == "" then
            local idx  = math.random(1, #source)
            local card = source[idx]
            table.remove(source, idx)
            card.slot_index  = i - 1
            card.trigger     = false
            state.alpha_hand[i] = card
            lib_battle_common.append_client_action(state,
                "alpha_source_to_hand:" .. card.inventory_item_id .. "," .. tostring(card.slot_index))
            drawn = drawn + 1
        end
    end

    lib_battle_common.dlog("[lib_battle_ai] alpha_draw_random: drawn=" .. tostring(drawn))
    return nil
end

-- ── omega_draw_random ─────────────────────────────────────────────────────────
-- Draws card_count random cards from omega_the_source (no preset logic).
-- start_slot (optional, default 0): slot_index offset for the drawn cards.
-- Returns: hand, err

function omega_draw_random(state, card_count, start_slot)
    lib_battle_common.dlog("[lib_battle_ai] == omega_draw_random == card_count: " .. tostring(card_count))
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
        source[idx].id                = _gen_id()
        source[idx].inventory_item_id = _gen_id()
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
    lib_battle_common.dlog("[lib_battle_ai] omega_draw_random client_actions added: " .. tostring(#hand) .. " cards")

    return hand, nil
end

-- ── deploy_omega_cards ────────────────────────────────────────────────────────
-- Deploys cards from omega_hand into existing omega lines (mid-battle, per turn).
-- Characters → front line (face-down); others → back line (face-down).
-- Returns: front_line, back_line, new_hand, err

-- Places cards from card_list into the first available empty slots of target_line.
-- Returns deployed_ids (by card.id) and deployed_list.
function _fill_line_slots(target_line, card_list, face_up)
    local deployed_ids  = {}
    local deployed_list = {}
    for _, deploy_card in ipairs(card_list) do
        local placed = false
        for slot_i = 1, SLOT_COUNT do
            local existing = target_line[slot_i]
            if existing == nil or existing.item_definition_code_name == nil or existing.item_definition_code_name == "" then
                deploy_card.slot_index = slot_i - 1
                deploy_card.face_up    = face_up
                deploy_card.expose     = face_up
                target_line[slot_i]    = deploy_card
                table.insert(deployed_ids, deploy_card.id)
                table.insert(deployed_list, deploy_card)
                placed = true
                break
            end
        end
        if not placed then
            lib_battle_common.dlog("[lib_battle_ai] _fill_line_slots: no empty slot for card " ..
            (deploy_card.inventory_item_id or "?"))
        end
    end
    return deployed_ids, deployed_list
end

-- Appends omega_hand_to_front_line / omega_hand_to_back_line client actions.
function _append_mid_deploy_actions(state, front_deployed, back_deployed)
    for _, front_card in ipairs(front_deployed) do
        if front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= "" then
            lib_battle_common.append_client_action(state,
                "omega_hand_to_front_line:" .. front_card.inventory_item_id .. "," .. (front_card.slot_index or 0))
        end
    end
    for _, back_card in ipairs(back_deployed) do
        if back_card.inventory_item_id ~= nil and back_card.inventory_item_id ~= "" then
            lib_battle_common.append_client_action(state,
                "omega_hand_to_back_line:" .. back_card.inventory_item_id .. "," .. (back_card.slot_index or 0))
        end
    end
end

-- Calls reset_card_turn_state on all newly deployed front and back cards.
function _reset_deployed_cards(item_defs, front_deployed, back_deployed)
    for _, front_card in ipairs(front_deployed) do
        lib_battle_common.reset_card_turn_state(item_defs, front_card)
    end
    for _, back_card in ipairs(back_deployed) do
        lib_battle_common.reset_card_turn_state(item_defs, back_card)
    end
end

function deploy_omega_cards(state)
    lib_battle_common.dlog("[lib_battle_ai] == deploy_omega_cards ==")
    local omega_front_line             = state.omega_front_line or {}
    local omega_back_line              = state.omega_back_line or {}

    local hand_cards                   = _collect_cards(state.omega_hand or {})
    local character_cards, other_cards = _split_cards_by_type(hand_cards, state.item_defs)

    local front_ids, front_deployed    = _fill_line_slots(omega_front_line, character_cards, false)
    local back_ids, back_deployed      = _fill_line_slots(omega_back_line, other_cards, false)

    local all_deployed_ids             = {}
    for _, dep_id in ipairs(front_ids) do table.insert(all_deployed_ids, dep_id) end
    for _, dep_id in ipairs(back_ids) do table.insert(all_deployed_ids, dep_id) end

    local new_hand = _rebuild_hand(state.omega_hand or {}, all_deployed_ids)
    _append_mid_deploy_actions(state, front_deployed, back_deployed)
    _reset_deployed_cards(state.item_defs, front_deployed, back_deployed)

    lib_battle_common.dlog("[lib_battle_ai] deploy_omega_cards: deployed=" .. #all_deployed_ids)
    return omega_front_line, omega_back_line, new_hand, nil
end

-- ── omega_end_turn ───────────────────────────────────────────────────────────
-- Ends omega's attacking turn: clears alpha_defending, sets omega_defending,
-- and advances next_move to "alpha_turn" so the state machine returns control to alpha.
function omega_end_turn(state)
    lib_battle_common.dlog("[lib_battle_ai] == omega_end_turn ==")
    lib_battle_common.append_client_action(state, "omega_no_available_attacker")
    lib_battle_common.reset_turn_cards(state, "alpha")
    state.alpha_defending = false
    state.turn = (state.turn or 0) + 1
    lib_battle_common.dlog("[lib_battle_ai] omega_end_turn: alpha_defending=false, turn=" ..
    tostring(state.turn))
    lib_battle_common.append_client_action(state, "omega_turn_end:" .. tostring(state.turn))
    alpha_draw_random(state)
    state.metadata.next_move = "alpha_turn"
    lib_battle_common.append_client_action(state, "next_move:alpha_turn")
    lib_battle_common.append_client_action(state, "alpha_take_lamp")
    state.omega_defending = true
    lib_battle_common.append_client_action(state, "omega_defending")
    lib_battle_common.dlog("[lib_battle_ai] omega_end_turn: next_move=alpha_turn, omega_defending=true")
end
-- Builds state.omega_planning for the current turn.
-- Picks ONE untriggered character from omega_front_line to attack the weakest
-- face-up Alpha Character, then a face-down target (fallback to alpha back-line).
-- Appends a labeled omega planning action with attacker, defender, and visible ATK.
-- If no untriggered attacker exists, calls omega_end_turn directly.
-- Returns err or nil.

-- Returns the first real (non-empty-slot) card from line, or nil.
function _find_first_real_card_in_line(line)
    for _, line_card in ipairs(line or {}) do
        if line_card.inventory_item_id ~= nil and line_card.inventory_item_id ~= "" then
            return line_card
        end
    end
    return nil
end

-- Returns the first untriggered character card in omega_front_line, or nil.
-- When require_face_up is true, hidden Characters are not eligible to attack.
function _find_omega_attacker(state, require_face_up)
    local omega_front_line = state.omega_front_line or {}
    for _, front_card in ipairs(omega_front_line) do
        local attacker_id = front_card.inventory_item_id or ""
        if attacker_id == "" then
            -- empty slot, skip
        elseif front_card.trigger == true then
            lib_battle_common.dlog("[lib_battle_ai] _find_omega_attacker: already triggered: " .. attacker_id)
        elseif require_face_up and front_card.face_up ~= true then
            lib_battle_common.dlog("[lib_battle_ai] _find_omega_attacker: face-down attacker skipped: " .. attacker_id)
        else
            local attacker_def = _find_item_def(state.item_defs, front_card.item_definition_code_name)
            local card_type    = attacker_def ~= nil and attacker_def.metadata ~= nil and attacker_def.metadata.type or
            nil
            if card_type == "character" then
                lib_battle_common.dlog("[lib_battle_ai] _find_omega_attacker: selected=" .. attacker_id)
                return front_card
            end
        end
    end
    return nil
end

-- Returns the face-up Alpha front-line Character with the least remaining DEF.
-- If none is face-up, returns the first face-down Character in slot order.
-- Falls back to the first real Alpha back-line card if no front target exists.
function _pick_alpha_attack_target(state)
    local alpha_front_line = state.alpha_front_line or {}
    local selected_face_up = nil
    local lowest_remaining_def = math.huge
    local first_face_down = nil
    for _, front_card in ipairs(alpha_front_line) do
        local has_card = front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
        if has_card and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            if front_card.face_up == true then
                local remaining_def = (front_card.final_def or 0) -
                    (front_card.total_damage_received or 0)
                lib_battle_common.dlog("[lib_battle_ai] _pick_alpha_attack_target: face-up candidate id=" ..
                    front_card.inventory_item_id .. " remaining_def=" .. remaining_def)
                if remaining_def < lowest_remaining_def then
                    lowest_remaining_def = remaining_def
                    selected_face_up = front_card
                end
            elseif first_face_down == nil then
                first_face_down = front_card
            end
        end
    end
    if selected_face_up ~= nil then
        lib_battle_common.dlog("[lib_battle_ai] _pick_alpha_attack_target: chose face-up id=" ..
            selected_face_up.inventory_item_id .. " remaining_def=" .. lowest_remaining_def)
        return selected_face_up
    end
    if first_face_down ~= nil then
        lib_battle_common.dlog("[lib_battle_ai] _pick_alpha_attack_target: chose face-down id=" ..
            first_face_down.inventory_item_id)
        return first_face_down
    end
    local back_card = _find_first_real_card_in_line(state.alpha_back_line)
    if back_card ~= nil then
        lib_battle_common.dlog("[lib_battle_ai] _pick_alpha_attack_target: front empty, chose back id=" ..
        back_card.inventory_item_id)
    end
    return back_card
end

function omega_planning_to_attack(state, hide_attackers_while_alpha_front)
    lib_battle_common.dlog("[lib_battle_ai] == omega_planning_to_attack ==")
    state.omega_planning = {}

    -- Hidden Shaman Characters may be preserved while Alpha still has a
    -- front-line Character. Other callers retain the default behavior.
    local has_alpha_front_character = false
    for _, card in ipairs(state.alpha_front_line or {}) do
        if lib_battle_common.check_card_type(state.item_defs, card, "character") then
            has_alpha_front_character = true
            break
        end
    end

    local attacker_card = _find_omega_attacker(
        state,
        hide_attackers_while_alpha_front and has_alpha_front_character
    )
    if attacker_card == nil then
        lib_battle_common.dlog(
        "[lib_battle_ai] omega_planning_to_attack: no untriggered character attacker -> calling omega_end_turn")
        omega_end_turn(state)
        return nil
    end

    -- Check if we can attack alpha_hp directly (no character cards on alpha front line)
    if not has_alpha_front_character then
        local plan_entry           = {}
        plan_entry.action          = "omega_attack_alpha_hp"
        plan_entry.attacker_inv_id = attacker_card.inventory_item_id
        plan_entry.defender_inv_id = "alpha_hp"
        table.insert(state.omega_planning, plan_entry)

        lib_battle_common.append_client_action(state,
            build_omega_planning_character_attack_action(state, attacker_card, "alpha_hp"))
        lib_battle_common.dlog("[lib_battle_ai] omega_planning_to_attack: planned direct attack " ..
        attacker_card.inventory_item_id .. " -> alpha_hp")
        return nil
    end

    local defender_card = _pick_alpha_attack_target(state)
    if defender_card == nil then
        lib_battle_common.dlog("[lib_battle_ai] omega_planning_to_attack: no alpha target available")
        return nil
    end

    local plan_entry           = {}
    plan_entry.action          = "card_attack_card"
    plan_entry.attacker_inv_id = attacker_card.inventory_item_id
    plan_entry.defender_inv_id = defender_card.inventory_item_id
    table.insert(state.omega_planning, plan_entry)

    lib_battle_common.append_client_action(state,
        build_omega_planning_character_attack_action(state, attacker_card, defender_card.inventory_item_id))
    lib_battle_common.dlog("[lib_battle_ai] omega_planning_to_attack: planned " ..
    attacker_card.inventory_item_id .. " -> " .. defender_card.inventory_item_id)
    return nil
end
