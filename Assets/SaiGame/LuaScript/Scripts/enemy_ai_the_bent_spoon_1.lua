-- enemy_ai_the_bent_spoon_1  (is_library = true)
-- AI module for The Bent Spoon #1 normal enemy.

local function find_untriggered_omega_misthy(state)
    return enemy_ai_core.find_untriggered_line_card_by_code(state.omega_front_line, "misthy")
        or enemy_ai_core.find_untriggered_line_card_by_code(state.omega_back_line, "misthy")
end

local function has_active_abyssal_mist(state)
    for _, line in ipairs({ state.omega_front_line or {}, state.omega_back_line or {} }) do
        for _, card in ipairs(line) do
            if card.item_definition_code_name == "abyssal_mist" and card.abyssal_mist_active == true then
                return true
            end
        end
    end
    return false
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

local function deploy_character_cards(state, front_line, hand_cards, slot_count, deployed_ids, front_deployed)
    local priority_codes = { "misthy", "lyra" }
    for _, code_name in ipairs(priority_codes) do
        for _, card in ipairs(hand_cards) do
            if card.item_definition_code_name == code_name then
                local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
                if slot_i == nil then return end
                enemy_ai_core.deploy_card(front_line, slot_i, card, true, front_deployed)
                table.insert(deployed_ids, card.id)
            end
        end
    end

    local character_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
    for _, card in ipairs(character_cards) do
        if card.item_definition_code_name ~= "misthy" and card.item_definition_code_name ~= "lyra" then
            local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
            if slot_i == nil then return end
            enemy_ai_core.deploy_card(front_line, slot_i, card, true, front_deployed)
            table.insert(deployed_ids, card.id)
        end
    end
end

local function deploy_one_ability_from_hand(back_line, hand_cards, code_name, slot_count, deployed_ids, back_deployed)
    local card = enemy_ai_core.find_card_by_code(hand_cards, code_name, nil)
    if card == nil then return nil end

    local slot_i = enemy_ai_core.find_empty_slot(back_line, slot_count)
    if slot_i == nil then return nil end

    enemy_ai_core.deploy_card(back_line, slot_i, card, true, back_deployed)
    table.insert(deployed_ids, card.id)
    return card
end

local function find_omega_line_card(state, code_name)
    return enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_back_line, code_name)
        or enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_front_line, code_name)
end

local function stage_abyssal_mist(state, hand_cards, back_line, slot_count, deployed_ids, back_deployed)
    if has_active_abyssal_mist(state) or find_untriggered_omega_misthy(state) == nil then return nil end

    local source_card = find_omega_line_card(state, "abyssal_mist")
    if source_card == nil then
        source_card = deploy_one_ability_from_hand(
            back_line, hand_cards, "abyssal_mist", slot_count, deployed_ids, back_deployed
        )
    end
    return source_card
end

local function stage_eagle_eye(state, hand_cards, back_line, slot_count, deployed_ids, back_deployed)
    local target_card = find_face_down_alpha_character(state)
    if target_card == nil or enemy_ai_core.find_line_card_by_code_prefer_exposed(state.omega_front_line, "lyra") == nil then
        return nil, nil
    end

    local source_card = find_omega_line_card(state, "eagle_eye")
    if source_card == nil then
        source_card = deploy_one_ability_from_hand(
            back_line, hand_cards, "eagle_eye", slot_count, deployed_ids, back_deployed
        )
    end
    return source_card, target_card
end

function defend(state)
    return nil
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

    deploy_character_cards(state, front_line, hand_cards, slot_count, deployed_ids, front_deployed)

    local abyssal_mist_card = stage_abyssal_mist(
        state, hand_cards, back_line, slot_count, deployed_ids, back_deployed
    )
    local eagle_eye_card, eagle_eye_target = stage_eagle_eye(
        state, hand_cards, back_line, slot_count, deployed_ids, back_deployed
    )

    local new_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)

    if abyssal_mist_card ~= nil then
        local abyssal_mist_err = enemy_ai_core.trigger_ability_and_append_actions(
            state, abyssal_mist_card, "abyssal_mist", "on_attack", {}
        )
        if abyssal_mist_err ~= nil then return front_line, back_line, new_hand, abyssal_mist_err end
    end

    if eagle_eye_card ~= nil then
        local eagle_eye_err = enemy_ai_core.trigger_ability_and_append_actions(
            state, eagle_eye_card, "eagle_eye", "on_attack", { defender_card = eagle_eye_target }
        )
        if eagle_eye_err ~= nil then return front_line, back_line, new_hand, eagle_eye_err end
    end

    return front_line, back_line, new_hand, nil
end

function plan_attack(state)
    return enemy_ai_core.plan_basic_omega_attack(state)
end
