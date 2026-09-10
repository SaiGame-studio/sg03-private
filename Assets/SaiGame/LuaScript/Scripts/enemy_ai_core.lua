-- enemy_ai_core  (is_library = true)
-- Shared battle-state helpers for enemy AI modules.

function is_empty_slot(card)
    return card == nil or card.inventory_item_id == nil or card.inventory_item_id == ""
end

function find_empty_slot(line, slot_count)
    for slot_i = 1, slot_count do
        if is_empty_slot(line[slot_i]) then return slot_i end
    end
    return nil
end

function find_adjacent_empty_slots(line, slot_count, required_count)
    required_count = required_count or 2
    for slot_i = 1, slot_count - required_count + 1 do
        local all_empty = true
        for offset = 0, required_count - 1 do
            if not is_empty_slot(line[slot_i + offset]) then
                all_empty = false
                break
            end
        end
        if all_empty then return slot_i end
    end
    return nil
end

function find_card_by_code(cards, code_name, excluded_id)
    for _, card in ipairs(cards or {}) do
        if card.item_definition_code_name == code_name and card.inventory_item_id ~= excluded_id then
            return card
        end
    end
    return nil
end

function find_line_card_by_code_prefer_exposed(line, code_name)
    local unexposed_fallback = nil
    for _, card in ipairs(line or {}) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == code_name then
            if card.expose == true then return card end
            if unexposed_fallback == nil then unexposed_fallback = card end
        end
    end
    return unexposed_fallback
end

function find_untriggered_line_card_by_code(line, code_name)
    for _, card in ipairs(line or {}) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == code_name
            and card.trigger ~= true then
            return card
        end
    end
    return nil
end

function find_card_in_zone_by_code(state, zone_key, code_name)
    return find_card_by_code(state[zone_key] or {}, code_name, nil)
end

function filter_cards_by_code(cards, code_name)
    local matched_cards = {}
    for _, card in ipairs(cards or {}) do
        if card.item_definition_code_name == code_name then
            table.insert(matched_cards, card)
        end
    end
    return matched_cards
end

function deploy_card(line, slot_i, card, face_up, deployed_cards)
    card.slot_index = slot_i - 1
    card.face_up = face_up
    card.expose = face_up
    line[slot_i] = card
    table.insert(deployed_cards, card)
end

function append_client_actions(state, actions)
    for _, action in ipairs(actions or {}) do
        lib_battle_common.append_client_action(state, action)
    end
end

function trigger_ability_and_append_actions(state, source_card, ability_key, trigger_event, event_data)
    local actions, ability_err = lib_ability_core.trigger_ability_by_key(
        state, source_card, ability_key, trigger_event, event_data
    )
    if ability_err ~= nil then return ability_err end
    append_client_actions(state, actions)
    return nil
end

function is_omega_front_line_taking_damage(state)
    local pending_attack = state.pending_attack
    if pending_attack == nil or (pending_attack.damage_dealt or 0) <= 0 then return false end
    local defender_id = pending_attack.defender_inventory_item_id or ""
    for _, card in ipairs(state.omega_front_line or {}) do
        if card.inventory_item_id == defender_id then return true end
    end
    return false
end

function defend_with_back_line_ability_when_front_line_takes_damage(state, ability_code, required_front_code)
    if not is_omega_front_line_taking_damage(state) then return nil end

    local source_card = find_line_card_by_code_prefer_exposed(state.omega_back_line, ability_code)
    if source_card == nil then return nil end
    if find_untriggered_line_card_by_code(state.omega_front_line, required_front_code) == nil then return nil end

    return trigger_ability_and_append_actions(state, source_card, ability_code, "on_defend", {
        pending_attack = state.pending_attack,
    })
end

-- Returns the face-up Alpha Character with the least remaining DEF.
function pick_alpha_face_up_front_line_character_target(state)
    local selected_card = nil
    local lowest_remaining_def = math.huge
    for _, card in ipairs(state.alpha_front_line or {}) do
        local has_card = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_card and card.face_up == true
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            local remaining_def = (card.final_def or 0) - (card.total_damage_received or 0)
            if remaining_def < lowest_remaining_def then
                selected_card = card
                lowest_remaining_def = remaining_def
            end
        end
    end
    return selected_card
end

-- Prefers the weakest face-up Alpha Character on the front line. If none is
-- face-up, returns the first face-down Character in slot order. Back-line cards,
-- including Ability cards, are intentionally not valid combat targets here.
function pick_alpha_front_line_character_target(state)
    local face_up_target = pick_alpha_face_up_front_line_character_target(state)
    if face_up_target ~= nil then return face_up_target end

    for _, card in ipairs(state.alpha_front_line or {}) do
        local has_card = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_card and card.face_up ~= true
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            return card
        end
    end
    return nil
end

-- Plans one Omega Character attack against defender, or Alpha HP when defender is nil.
function plan_omega_attack_with_target(state, defender)
    state.omega_planning = {}
    local attacker = lib_battle_ai._find_omega_attacker(state, true)
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    local defender_id = defender ~= nil and defender.inventory_item_id or "alpha_hp"
    table.insert(state.omega_planning, {
        action = defender ~= nil and "card_attack_card" or "omega_attack_alpha_hp",
        attacker_inv_id = attacker.inventory_item_id,
        defender_inv_id = defender_id,
    })
    lib_battle_common.append_client_action(
        state, lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, defender_id)
    )
    return nil
end

function plan_basic_omega_attack(state)
    local defender = pick_alpha_front_line_character_target(state)
    return plan_omega_attack_with_target(state, defender)
end
