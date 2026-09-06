function skeleton_shield_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] skeleton_shield ====================")

    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        battle.dlog("[ability] skeleton_shield: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local source_side = helpers.find_card_side(state, source_card)
    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}

    -- Requirement 1: Ria must be present in front_line, even if triggered.
    local ria_card = nil
    for _, card in ipairs(front_line) do
        local has_id = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_id and card.item_definition_code_name == "ria" then
            ria_card = card
            break
        end
    end
    if ria_card == nil then
        battle.dlog("[ability] skeleton_shield: error - no ria in " .. front_line_key)
        return {}, "skeleton_shield requires ria in front_line"
    end

    -- Requirement 2: Must have a skeleton card in front_line (different from target_card)
    local skeleton_card = nil
    local skel_idx = nil
    for i, c in ipairs(front_line) do
        local has_id = c.inventory_item_id ~= nil and c.inventory_item_id ~= ""
        if has_id and c.item_definition_code_name == "skeleton" and c.inventory_item_id ~= target_card.inventory_item_id then
            skeleton_card = c
            skel_idx = i
            break
        end
    end
    if skeleton_card == nil or skel_idx == nil then
        battle.dlog("[ability] skeleton_shield: error - no distinct skeleton in " .. front_line_key)
        return {}, "skeleton_shield requires skeleton in front_line different from target_card"
    end

    -- Requirement 3: Target card must be currently targeted by an opponent planning attack
    local opponent_planning = (source_side == "alpha") and (state.omega_planning or {}) or (state.alpha_planning or {})
    local target_plan_entry = nil
    for _, plan_entry in ipairs(opponent_planning) do
        if plan_entry.defender_inv_id == target_card.inventory_item_id then
            target_plan_entry = plan_entry
            break
        end
    end

    if target_plan_entry == nil then
        battle.dlog("[ability] skeleton_shield: error - target_card is not targeted by opponent planning attack")
        return {}, "skeleton_shield requires target card to be targeted by opponent planning attack"
    end

    -- Find target_card line and slot index
    local target_line_key = (event_data or {}).defender_line_key
    if target_line_key == nil or target_line_key == "" or state[target_line_key] == nil then
        if state.alpha_front_line then
            for _, c in ipairs(state.alpha_front_line) do
                if c.inventory_item_id == target_card.inventory_item_id then
                    target_line_key = "alpha_front_line"
                    break
                end
            end
        end
        if target_line_key == nil and state.omega_front_line then
            for _, c in ipairs(state.omega_front_line) do
                if c.inventory_item_id == target_card.inventory_item_id then
                    target_line_key = "omega_front_line"
                    break
                end
            end
        end
    end

    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    local target_idx = nil
    if target_line ~= nil then
        for i, c in ipairs(target_line) do
            if c.inventory_item_id == target_card.inventory_item_id then
                target_idx = i
                break
            end
        end
    end

    if target_line == nil or target_idx == nil then
        battle.dlog("[ability] skeleton_shield: error - target_card position not found")
        return {}, "skeleton_shield target_card position not found"
    end

    -- Swap position of skeleton_card and target_card
    local temp_slot = skeleton_card.slot_index
    skeleton_card.slot_index = target_card.slot_index
    target_card.slot_index = temp_slot

    if front_line_key == target_line_key then
        front_line[skel_idx] = target_card
        front_line[target_idx] = skeleton_card
    else
        front_line[skel_idx] = target_card
        target_line[target_idx] = skeleton_card
    end

    -- Redirect the opponent's planned attack to the skeleton card (as a substitute shield)
    target_plan_entry.defender_inv_id = skeleton_card.inventory_item_id

    ria_card.trigger = true


    local expose_action = helpers.expose_ability_selected_card(state, ria_card)
    battle.dlog("[ability] skeleton_shield: swapped skeleton=" .. skeleton_card.inventory_item_id .. " and target=" .. target_card.inventory_item_id)

    local shield_actions = {}
    if expose_action ~= nil then
        table.insert(shield_actions, expose_action)
    end
    table.insert(shield_actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=skeleton_shield,target=" .. target_card.inventory_item_id .. ",selected=" .. ria_card.inventory_item_id .. ",swapped=" .. skeleton_card.inventory_item_id)
    table.insert(shield_actions, source_side .. "_card_swapped:card1=" .. skeleton_card.inventory_item_id .. ",card2=" .. target_card.inventory_item_id)
    table.insert(shield_actions, source_side .. "_card_guarded:" .. target_card.inventory_item_id)

    return shield_actions, nil
end
