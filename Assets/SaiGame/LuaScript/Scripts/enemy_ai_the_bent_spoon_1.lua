-- enemy_ai_the_bent_spoon_1  (is_library = true)
-- AI module for The Bent Spoon #1 normal enemy.

local function find_untriggered_omega_misthy(state)
    return enemy_ai_core.find_untriggered_line_card_by_code(state.omega_front_line, "misthy")
        or enemy_ai_core.find_untriggered_line_card_by_code(state.omega_back_line, "misthy")
end

local function has_positive_abyssal_mist_bonuses(state, source_card)
    local item_def = lib_battle_ai._find_item_def(state.item_defs, source_card.item_definition_code_name)
    local stats = item_def ~= nil and item_def.base_stats or nil
    local atk_added = stats ~= nil and tonumber(stats.atk_added) or nil
    local def_added = stats ~= nil and tonumber(stats.def_added) or nil
    return atk_added ~= nil and atk_added > 0 and def_added ~= nil and def_added > 0
end

local function find_face_down_alpha_character(state)
    for _, card in ipairs(state.alpha_front_line or {}) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.face_up ~= true
            and card.expose ~= true
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            return card
        end
    end
    return nil
end

local function has_omega_deployed_card(front_line, back_line)
    for _, line in ipairs({ front_line, back_line }) do
        for _, card in ipairs(line) do
            if not enemy_ai_core.is_empty_slot(card) then return true end
        end
    end
    return false
end

local function deploy_priority_character(state, front_line, hand_cards, slot_count, deployed_ids, front_deployed, face_up)
    local priority_codes = { "misthy", "lyra" }
    for _, code_name in ipairs(priority_codes) do
        local card = enemy_ai_core.find_card_by_code(hand_cards, code_name, nil)
        local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
        if card ~= nil and slot_i ~= nil then
            enemy_ai_core.deploy_card(front_line, slot_i, card, face_up, front_deployed)
            table.insert(deployed_ids, card.id)
            return card
        end
    end

    local character_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
    for _, card in ipairs(character_cards) do
        local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
        if slot_i == nil then return nil end
        enemy_ai_core.deploy_card(front_line, slot_i, card, face_up, front_deployed)
        table.insert(deployed_ids, card.id)
        return card
    end
    return nil
end

local function deploy_one_ability_from_hand(back_line, hand_cards, code_name, slot_count, deployed_ids, back_deployed, face_up)
    local card = enemy_ai_core.find_card_by_code(hand_cards, code_name, nil)
    if card == nil then return nil end

    local slot_i = enemy_ai_core.find_empty_slot(back_line, slot_count)
    if slot_i == nil then return nil end

    enemy_ai_core.deploy_card(back_line, slot_i, card, face_up, back_deployed)
    table.insert(deployed_ids, card.id)
    return card
end

local function find_omega_line_card(state, code_name)
    return enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_back_line, code_name)
        or enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_front_line, code_name)
end

local function deploy_abyssal_mist_to_backline(state, hand_cards, back_line, slot_count, deployed_ids, back_deployed)
    if find_untriggered_omega_misthy(state) == nil then return nil end

    local source_card = find_omega_line_card(state, "abyssal_mist")
    if source_card ~= nil then return nil end

    local hand_card = enemy_ai_core.find_card_by_code(hand_cards, "abyssal_mist", nil)
    if hand_card == nil or not has_positive_abyssal_mist_bonuses(state, hand_card) then return nil end

    return deploy_one_ability_from_hand(
        back_line, hand_cards, "abyssal_mist", slot_count, deployed_ids, back_deployed, false
    )
end

local function stage_eagle_eye(state, hand_cards, back_line, slot_count, deployed_ids, back_deployed)
    local target_card = find_face_down_alpha_character(state)
    if target_card == nil or enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_front_line, "lyra") == nil then
        return nil, nil
    end

    local source_card = find_omega_line_card(state, "eagle_eye")
    if source_card == nil then
        source_card = deploy_one_ability_from_hand(
            back_line, hand_cards, "eagle_eye", slot_count, deployed_ids, back_deployed, true
        )
    end
    return source_card, target_card
end

function defend(state)
    return nil
end

local function get_omega_character_attack_damage(state, card)
    local item_def = lib_battle_ai._find_item_def(state.item_defs, card.item_definition_code_name)
    return lib_battle_common.get_attack_damage(state, item_def, "omega_front_line", card)
end

local function find_untriggered_front_line_misthy(state)
    return enemy_ai_core.find_untriggered_line_card_by_code(state.omega_front_line, "misthy")
end

local function find_untriggered_non_misthy_attacker(state)
    for _, card in ipairs(state.omega_front_line or {}) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.trigger ~= true
            and card.item_definition_code_name ~= "misthy"
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            return card
        end
    end
    return nil
end

local function find_safe_misthy_setup_attacker(state, remaining_def)
    local selected_card = nil
    local selected_damage = 0
    for _, card in ipairs(state.omega_front_line or {}) do
        local is_character = lib_battle_common.check_card_type(state.item_defs, card, "character")
        local damage = get_omega_character_attack_damage(state, card)
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.trigger ~= true
            and card.item_definition_code_name ~= "misthy"
            and is_character
            and damage > selected_damage
            and damage < remaining_def then
            selected_card = card
            selected_damage = damage
        end
    end
    return selected_card
end

local function append_omega_attack_plan(state, attacker, defender)
    local defender_id = defender ~= nil and defender.inventory_item_id or "alpha_hp"
    table.insert(state.omega_planning, {
        action = defender ~= nil and "card_attack_card" or "omega_attack_alpha_hp",
        attacker_inv_id = attacker.inventory_item_id,
        defender_inv_id = defender_id,
    })
    lib_battle_common.append_client_action(
        state,
        lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, defender_id)
    )
end

function deploy(state)
    lib_battle_common.dlog("[entity_ai] == the_bent_spoon_1.deploy ==")

    local slot_count = lib_battle_common.get_hand_size()
    local front_line = state.omega_front_line or {}
    local back_line = state.omega_back_line or {}
    local hand = state.omega_hand or {}
    local hand_cards = lib_battle_ai._collect_cards(hand)
    local deployed_ids = {}
    local front_deployed = {}
    local back_deployed = {}

    local abyssal_mist_card = deploy_abyssal_mist_to_backline(
        state, hand_cards, back_line, slot_count, deployed_ids, back_deployed
    )
    local eagle_eye_card = nil
    local eagle_eye_target = nil
    if abyssal_mist_card == nil then
        eagle_eye_card, eagle_eye_target = stage_eagle_eye(
            state, hand_cards, back_line, slot_count, deployed_ids, back_deployed
        )
    end
    if abyssal_mist_card == nil and eagle_eye_card == nil then
        local face_up = has_omega_deployed_card(front_line, back_line)
        deploy_priority_character(state, front_line, hand_cards, slot_count, deployed_ids, front_deployed, face_up)
    end

    lib_battle_ai.ensure_omega_hand_draw_capacity(
        state, front_line, back_line, hand, deployed_ids, front_deployed, back_deployed
    )

    local new_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)

    if eagle_eye_card ~= nil then
        local eagle_eye_err = enemy_ai_core.trigger_ability_and_append_actions(
            state, eagle_eye_card, "eagle_eye", "on_attack", { defender_card = eagle_eye_target }
        )
        if eagle_eye_err ~= nil then return front_line, back_line, new_hand, eagle_eye_err end
    end

    return front_line, back_line, new_hand, nil
end

function plan_attack(state)
    lib_battle_common.dlog("[entity_ai] == the_bent_spoon_1.plan_attack ==")
    state.omega_planning = {}

    local defender = enemy_ai_core.pick_alpha_front_line_character_target(state)
    if defender == nil then
        local attacker = lib_battle_ai._find_omega_attacker(state, false)
        if attacker == nil then
            lib_battle_ai.omega_end_turn(state)
            return nil
        end
        lib_battle_common.dlog("[entity_ai] the_bent_spoon_1.plan_attack: Alpha front line is clear, attacking Alpha HP")
        append_omega_attack_plan(state, attacker, nil)
        return nil
    end

    local misthy = find_untriggered_front_line_misthy(state)
    if misthy == nil then
        local attacker = lib_battle_ai._find_omega_attacker(state, false)
        if attacker == nil then
            lib_battle_ai.omega_end_turn(state)
            return nil
        end
        append_omega_attack_plan(state, attacker, defender)
        return nil
    end

    if defender.face_up ~= true or defender.expose ~= true then
        local attacker = find_untriggered_non_misthy_attacker(state) or misthy
        lib_battle_common.dlog("[entity_ai] the_bent_spoon_1.plan_attack: reveal target before Misthy calculation")
        append_omega_attack_plan(state, attacker, defender)
        return nil
    end

    local remaining_def = (defender.final_def or 0) - (defender.total_damage_received or 0)
    local setup_attacker = find_safe_misthy_setup_attacker(state, remaining_def)
    if setup_attacker ~= nil then
        lib_battle_common.dlog("[entity_ai] the_bent_spoon_1.plan_attack: setup Misthy kill with " ..
            setup_attacker.inventory_item_id .. " damage=" .. get_omega_character_attack_damage(state, setup_attacker))
        append_omega_attack_plan(state, setup_attacker, defender)
        return nil
    end

    if get_omega_character_attack_damage(state, misthy) >= remaining_def then
        lib_battle_common.dlog("[entity_ai] the_bent_spoon_1.plan_attack: Misthy finishes " .. defender.inventory_item_id)
        append_omega_attack_plan(state, misthy, defender)
        return nil
    end

    local attacker = lib_battle_ai._find_omega_attacker(state, false)
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    lib_battle_common.dlog("[entity_ai] the_bent_spoon_1.plan_attack: Misthy finish unavailable, using normal attack")
    append_omega_attack_plan(state, attacker, defender)
    return nil
end
