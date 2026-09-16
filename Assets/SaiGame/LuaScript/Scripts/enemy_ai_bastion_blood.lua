-- enemy_ai_bastion_blood  (is_library = true)
-- AI module for Bastion Blood normal enemy.

function defend(state)
    return nil
end

local function find_safe_setup_attacker(state, main_code_name, remaining_def)
    local selected_card = nil
    local selected_damage = 0
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (state.omega_front_line or {})[index]
        local damage = enemy_ai_core.get_omega_character_attack_damage(state, card)
        if enemy_ai_core.is_eligible_omega_attack_planner(state, card)
            and card.item_definition_code_name ~= main_code_name
            and damage > selected_damage
            and damage < remaining_def then
            selected_card = card
            selected_damage = damage
        end
    end
    return selected_card
end

local function try_trigger_blood_mist(state)
    local back_line = state.omega_back_line or {}
    local front_line = state.omega_front_line or {}

    for _, card in ipairs(back_line) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == "blood_mist"
            and card.face_up == true then
            return nil
        end
    end

    local blood_spire = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "blood_spire")
    local mireya = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "mireya")
    if blood_spire == nil or mireya == nil then
        return nil
    end
    if mireya.trigger == true then
        lib_battle_common.dlog("[entity_ai] bastion_blood: skip blood_mist - mireya is triggered")
        return nil
    end

    local backup_mist = nil
    for _, card in ipairs(back_line) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == "blood_mist"
            and card.face_up ~= true then
            backup_mist = card
            break
        end
    end

    if backup_mist == nil then
        return nil
    end

    lib_battle_common.dlog("[entity_ai] bastion_blood: triggering blood_mist on mireya")
    local event_data = {
        defender_card = mireya,
        defender_line_key = "omega_front_line",
    }
    return enemy_ai_core.trigger_ability_and_append_actions(
        state, backup_mist, "blood_mist", "on_attack", event_data
    )
end

-- Preserve enough frontline slots for Crimson Spire to build two Bone Spires.
-- Once Blood Spire exists, Crimson Spire cannot summon and no slots are reserved.
local function can_deploy_character_without_blocking_bone_spires(front_line, slot_count)
    local blood_spire = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "blood_spire")
    if blood_spire ~= nil then
        return enemy_ai_core.count_empty_slots(front_line, slot_count) > 0
    end

    local bone_spire_count = enemy_ai_core.count_line_cards_by_code(front_line, "bone_spire")
    local required_bone_slots = math.max(0, 2 - bone_spire_count)
    return enemy_ai_core.count_empty_slots(front_line, slot_count) > required_bone_slots
end

local function deploy_character_strategy(state, front_line, remaining_cards, slot_count, deployed_ids, front_deployed)
    local blood_spire = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "blood_spire")
    local sythra_on_line = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "sythra")
    local mireya_on_line = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "mireya")
    local can_deploy_character = can_deploy_character_without_blocking_bone_spires(front_line, slot_count)

    if blood_spire ~= nil then
        local mireya_card = enemy_ai_core.find_card_by_code(remaining_cards, "mireya", nil)
        local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
        if can_deploy_character and mireya_card ~= nil and slot_i ~= nil then
            enemy_ai_core.deploy_card(front_line, slot_i, mireya_card, true, front_deployed)
            table.insert(deployed_ids, mireya_card.id)
            return
        end

        local character_cards = lib_battle_ai._split_cards_by_type(remaining_cards, state.item_defs)
        for _, card in ipairs(character_cards) do
            if can_deploy_character and slot_i ~= nil then
                local face_up = card.item_definition_code_name == "sythra" or card.item_definition_code_name == "mireya"
                enemy_ai_core.deploy_card(front_line, slot_i, card, face_up, front_deployed)
                table.insert(deployed_ids, card.id)
                break
            end
        end
        return
    end

    if sythra_on_line ~= nil and mireya_on_line ~= nil then
        local extra_sythra = enemy_ai_core.find_card_by_code(remaining_cards, "sythra", nil)
        local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
        if can_deploy_character and extra_sythra ~= nil and slot_i ~= nil then
            enemy_ai_core.deploy_card(front_line, slot_i, extra_sythra, true, front_deployed)
            table.insert(deployed_ids, extra_sythra.id)
        end
        return
    end

    local character_cards = lib_battle_ai._split_cards_by_type(remaining_cards, state.item_defs)
    for _, card in ipairs(character_cards) do
        local code = card.item_definition_code_name
        local is_key_combo = code == "sythra" or code == "mireya"
        if can_deploy_character then
            local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
            if slot_i ~= nil then
                local face_up = is_key_combo
                enemy_ai_core.deploy_card(front_line, slot_i, card, face_up, front_deployed)
                table.insert(deployed_ids, card.id)
                break
            end
        end
    end
end

local function exclude_characters_from_hand_capacity_cleanup(state, hand, front_line, slot_count)
    if can_deploy_character_without_blocking_bone_spires(front_line, slot_count) then
        return nil
    end

    local excluded_ids = {}
    for _, card in ipairs(hand or {}) do
        if card.id ~= nil and card.id ~= ""
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            excluded_ids[card.id] = true
        end
    end
    lib_battle_common.dlog("[entity_ai] bastion_blood: preserving frontline slots for Bone Spires")
    return excluded_ids
end

function deploy(state)
    lib_battle_common.dlog("[entity_ai] == bastion_blood.deploy ==")

    local slot_count = lib_battle_common.get_hand_size()
    local front_line = state.omega_front_line or {}
    local back_line = state.omega_back_line or {}
    local hand = state.omega_hand or {}
    local hand_cards = lib_battle_ai._collect_cards(hand)
    local deployed_ids = {}
    local front_deployed = {}
    local back_deployed = {}

    for _, card in ipairs(hand_cards) do
        if card.item_definition_code_name == "blood_mist" then
            local slot_i = enemy_ai_core.find_empty_slot(back_line, slot_count)
            if slot_i ~= nil then
                enemy_ai_core.deploy_card(back_line, slot_i, card, false, back_deployed)
                table.insert(deployed_ids, card.id)
            end
        end
    end

    local remaining_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
    local remaining_cards = lib_battle_ai._collect_cards(remaining_hand)
    local bone_spire_count = enemy_ai_core.count_line_cards_by_code(front_line, "bone_spire")
    local priority_character = nil
    if bone_spire_count >= 2 then
        priority_character = enemy_ai_core.find_character_with_base_attack_above(
            remaining_cards, state.item_defs, 0
        )
    end

    local bone_spire_card = enemy_ai_core.find_card_by_code(remaining_cards, "bone_spire", nil)
    local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
    if priority_character ~= nil and slot_i ~= nil then
        enemy_ai_core.deploy_card(front_line, slot_i, priority_character, true, front_deployed)
        table.insert(deployed_ids, priority_character.id)
    elseif bone_spire_card ~= nil then
        if slot_i ~= nil then
            enemy_ai_core.deploy_card(front_line, slot_i, bone_spire_card, false, front_deployed)
            table.insert(deployed_ids, bone_spire_card.id)
        end
    else
        deploy_character_strategy(state, front_line, remaining_cards, slot_count, deployed_ids, front_deployed)
    end

    local excluded_ids = exclude_characters_from_hand_capacity_cleanup(
        state, hand, front_line, slot_count
    )
    lib_battle_ai.ensure_omega_hand_draw_capacity(
        state, front_line, back_line, hand, deployed_ids, front_deployed, back_deployed, excluded_ids
    )

    local new_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)
    return front_line, back_line, new_hand, nil
end

function plan_attack(state)
    lib_battle_common.dlog("[entity_ai] == bastion_blood.plan_attack ==")
    state.omega_planning = {}

    local mist_err = try_trigger_blood_mist(state)
    if mist_err ~= nil then return mist_err end

    local defender = enemy_ai_core.pick_alpha_front_line_character_target(state)
    if defender == nil then
        local attacker = enemy_ai_core.find_eligible_omega_attack_planner(state, false)
        if attacker == nil then
            lib_battle_ai.omega_end_turn(state)
            return nil
        end
        enemy_ai_core.append_omega_attack_plan(state, attacker, nil)
        return nil
    end

    local front_line = state.omega_front_line or {}
    local bone_spire_count = enemy_ai_core.count_line_cards_by_code(front_line, "bone_spire")
    local void_has_bone_spire = enemy_ai_core.find_card_in_zone_by_code(state, "omega_the_void", "bone_spire") ~= nil
    local blood_spire_on_line = enemy_ai_core.find_line_card_by_code_prefer_exposed(front_line, "blood_spire")

    -- Void-spawned Bone Spires are the primary setup. Once Blood Spire exists,
    -- Crimson Spire cannot summon, so retain normal damage priority instead.
    if bone_spire_count < 2 and void_has_bone_spire and blood_spire_on_line == nil then
        local sythra = enemy_ai_core.find_untriggered_line_card_by_code(front_line, "sythra")
        if enemy_ai_core.is_eligible_omega_attack_planner(state, sythra) then
            local remaining_def = (defender.final_def or 0) - (defender.total_damage_received or 0)
            local sythra_damage = enemy_ai_core.get_omega_character_attack_damage(state, sythra)
            if sythra_damage >= remaining_def then
                enemy_ai_core.append_omega_attack_plan(state, sythra, defender)
                return nil
            end

            local setup_attacker = find_safe_setup_attacker(state, "sythra", remaining_def)
            if setup_attacker ~= nil then
                enemy_ai_core.append_omega_attack_plan(state, setup_attacker, defender)
                return nil
            end
        end
    end

    if bone_spire_count >= 2 then
        local mireya = enemy_ai_core.find_untriggered_line_card_by_code(front_line, "mireya")
        if enemy_ai_core.is_eligible_omega_attack_planner(state, mireya) then
            local remaining_def = (defender.final_def or 0) - (defender.total_damage_received or 0)
            local mireya_damage = enemy_ai_core.get_omega_character_attack_damage(state, mireya)
            if mireya_damage >= remaining_def then
                enemy_ai_core.append_omega_attack_plan(state, mireya, defender)
                return nil
            end

            local setup_attacker = find_safe_setup_attacker(state, "mireya", remaining_def)
            if setup_attacker ~= nil then
                enemy_ai_core.append_omega_attack_plan(state, setup_attacker, defender)
                return nil
            end
        end
    end

    local attacker = enemy_ai_core.find_eligible_omega_attack_planner(state, false)
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    enemy_ai_core.append_omega_attack_plan(state, attacker, defender)
    return nil
end
