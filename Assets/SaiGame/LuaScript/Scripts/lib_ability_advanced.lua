-- lib_ability_advanced
-- is_library = true

local function advanced_find_card_index_by_id(line, inventory_item_id)
    for index, card in ipairs(line or {}) do
        if card.inventory_item_id == inventory_item_id then
            return index
        end
    end
    return nil
end

local function advanced_find_card_by_code(cards, code)
    for index, card in ipairs(cards or {}) do
        if card.item_definition_code_name == code then
            return card, index
        end
    end
    return nil, nil
end

local function advanced_find_nearest_card_indexes_by_code(line, code, center_index, count)
    local indexes = {}
    for index, card in ipairs(line or {}) do
        if card.inventory_item_id ~= nil and card.inventory_item_id ~= "" and
           card.item_definition_code_name == code then
            table.insert(indexes, index)
        end
    end
    table.sort(indexes, function(left, right)
        local left_distance = math.abs(left - center_index)
        local right_distance = math.abs(right - center_index)
        if left_distance ~= right_distance then return left_distance < right_distance end
        return left < right
    end)
    while #indexes > count do
        table.remove(indexes)
    end
    return indexes
end

local function advanced_find_empty_adjacent_slot(line, center_index)
    for _, index in ipairs({ center_index - 1, center_index + 1 }) do
        if index >= 1 and index <= #line then
            local slot = line[index]
            if slot == nil or slot.inventory_item_id == nil or slot.inventory_item_id == "" then
                return index
            end
        end
    end
    return nil
end

local function advanced_send_source_to_void(state, source_side, source_card, void_zone, battle, actions)
    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    table.insert(void_zone, source_card)
    battle.append_card_sent_to_void_action(actions, source_side, source_card)
end

local function advanced_get_ability_atk(state, source_card, helpers, ability_key)
    local ability_atk = tonumber(helpers.get_card_stat(state, source_card, "atk"))
    if ability_atk == nil or ability_atk <= 0 then
        return nil, ability_key .. " requires a positive base_stats.atk"
    end
    return ability_atk, nil
end

local function advanced_apply_ability_damage_to_ria(state, source_card, ria_card, ability_atk,
    front_line, void_key, helpers, actions)
    local damage_actions, damage_err = helpers.deal_damage_to_character(
        state, source_card, ria_card, ability_atk, front_line, void_key)
    if damage_err ~= nil then return damage_err end
    for _, action in ipairs(damage_actions) do
        table.insert(actions, action)
    end
    return nil
end

-- ability: animate_dead
function animate_dead_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] animate_dead ====================")

    local caster_side = helpers.find_card_side(state, source_card)
    if caster_side == "unknown" or caster_side == nil then
        local function find_side_in_all_zones(card_id)
            local alpha_keys = { "alpha_front_line", "alpha_back_line", "alpha_hand", "alpha_the_void", "alpha_the_source" }
            for _, key in ipairs(alpha_keys) do
                if state[key] then
                    for _, c in ipairs(state[key]) do
                        if c.inventory_item_id == card_id then return "alpha" end
                    end
                end
            end
            local omega_keys = { "omega_front_line", "omega_back_line", "omega_hand", "omega_the_void", "omega_the_source" }
            for _, key in ipairs(omega_keys) do
                if state[key] then
                    for _, c in ipairs(state[key]) do
                        if c.inventory_item_id == card_id then return "omega" end
                    end
                end
            end
            return "alpha"
        end
        caster_side = find_side_in_all_zones(source_card.inventory_item_id)
    end

    local front_line_key = caster_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local ria_card = lib_battle_common.find_card_in_line_by_code(front_line, "ria")
    if ria_card == nil then
        battle.dlog("[ability] animate_dead: error - no ria in " .. front_line_key)
        return {}, "animate_dead requires ria in front_line"
    end
    local ability_atk, ability_atk_err = advanced_get_ability_atk(state, source_card, helpers, "animate_dead")
    if ability_atk_err ~= nil then return {}, ability_atk_err end

    local expose_action = helpers.expose_ability_selected_card(state, ria_card)
    local ability_actions = {}
    if expose_action ~= nil then table.insert(ability_actions, expose_action) end
    table.insert(ability_actions, caster_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=animate_dead,selected=" .. ria_card.inventory_item_id)

    local void_key = caster_side .. "_the_void"
    local void_zone = state[void_key] or {}
    state[void_key] = void_zone

    -- 1. Perform all skeleton summons first
    for _ = 1, 3 do
        local skeleton_card, skeleton_idx = advanced_find_card_by_code(void_zone, "skeleton")
        if skeleton_card == nil then
            battle.dlog("[ability] animate_dead: no more skeleton in " .. void_key)
            break
        end
        local summon_turn_err = battle.validate_summon_card_turn(state, state.item_defs, skeleton_card)
        if summon_turn_err ~= nil then return ability_actions, summon_turn_err end

        local free_slots = {}
        for i = 1, 5 do
            local slot = front_line[i]
            if slot == nil or slot.inventory_item_id == nil or slot.inventory_item_id == "" then
                table.insert(free_slots, i)
            end
        end
        if #free_slots == 0 then
            battle.dlog("[ability] animate_dead: front_line has no free slots, stopping summon")
            break
        end

        local chosen_slot_idx = free_slots[math.random(1, #free_slots)]
        table.remove(void_zone, skeleton_idx)
        skeleton_card.slot_index = chosen_slot_idx - 1
        battle.reset_card_turn_state(state.item_defs, skeleton_card)
        skeleton_card.trigger = false
        skeleton_card.face_up = true
        skeleton_card.expose = true
        front_line[chosen_slot_idx] = skeleton_card
        battle.dlog("[ability] animate_dead: summoned skeleton=" .. skeleton_card.inventory_item_id .. " to slot=" .. skeleton_card.slot_index)
        table.insert(ability_actions, caster_side .. "_void_to_front_line:" .. skeleton_card.inventory_item_id .. "," .. skeleton_card.slot_index)
    end

    -- 2. Apply damage to Ria once after summoning completes (do not deal damage prior to summoning)
    local ria_damage_err = advanced_apply_ability_damage_to_ria(
        state, source_card, ria_card, ability_atk, front_line, void_key, helpers, ability_actions)
    if ria_damage_err ~= nil then return ability_actions, ria_damage_err end

    advanced_send_source_to_void(state, caster_side, source_card, void_zone, battle, ability_actions)
    battle.dlog("[ability] animate_dead: source card sent to void=" .. void_key .. " id=" .. source_card.inventory_item_id)

    return ability_actions, nil
end

-- ability: king_return
-- Sacrifices the two or three nearest Skeletons to Ria to summon
-- Skeleton King beside the selected Ria. The left adjacent slot
-- is preferred. If neither adjacent slot is free after the sacrifices, the
-- ability is still consumed but Skeleton King remains in the void.
function king_return_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] king_return ====================")

    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "king_return requires a Ria target"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "king_return source card is not on a battle line"
    end

    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local ria_index = advanced_find_card_index_by_id(front_line, target_card.inventory_item_id)
    if ria_index == nil then
        return {}, "king_return target must be on own front_line"
    end

    local ria_card = front_line[ria_index]
    if ria_card.item_definition_code_name ~= "ria" then
        return {}, "king_return target must be Ria"
    end
    local ability_atk, ability_atk_err = advanced_get_ability_atk(state, source_card, helpers, "king_return")
    if ability_atk_err ~= nil then return {}, ability_atk_err end

    local void_key = source_side .. "_the_void"
    local void_zone = state[void_key] or {}
    state[void_key] = void_zone
    local king_card, king_index = advanced_find_card_by_code(void_zone, "skeleton_king")
    if king_card == nil then
        return {}, "king_return requires Skeleton King in own the_void"
    end
    local summon_turn_err = battle.validate_summon_card_turn(state, state.item_defs, king_card)
    if summon_turn_err ~= nil then return {}, summon_turn_err end

    local sacrifice_indexes = advanced_find_nearest_card_indexes_by_code(front_line, "skeleton", ria_index, 3)
    if #sacrifice_indexes < 2 then
        return {}, "king_return requires at least 2 Skeleton in own front_line"
    end

    local actions = {}
    local expose_ria = helpers.expose_ability_selected_card(state, ria_card)
    if expose_ria ~= nil then table.insert(actions, expose_ria) end
    ria_card.trigger = true
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
        ",ability=king_return,target=" .. ria_card.inventory_item_id ..
        ",selected=" .. ria_card.inventory_item_id)

    -- 1. Perform sacrifices and summon Skeleton King first
    for _, index in ipairs(sacrifice_indexes) do
        local skeleton_card = front_line[index]
        front_line[index] = {}
        table.insert(void_zone, skeleton_card)
        battle.append_card_sent_to_void_action(actions, source_side, skeleton_card)
    end

    local chosen_index = advanced_find_empty_adjacent_slot(front_line, ria_index)

    if chosen_index == nil then
        table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=king_return,target=" .. ria_card.inventory_item_id ..
            ",selected=" .. ria_card.inventory_item_id ..
            ",result=failed,reason=no_adjacent_position")
    else
        table.remove(void_zone, king_index)
        battle.reset_card_turn_state(state.item_defs, king_card)
        king_card.slot_index = chosen_index - 1
        king_card.face_up = true
        king_card.expose = true
        king_card.trigger = true
        king_card.defeated_from_line_key = nil
        front_line[chosen_index] = king_card
        table.insert(actions, source_side .. "_void_to_front_line:" ..
            king_card.inventory_item_id .. "," .. tostring(king_card.slot_index))
        battle.dlog("[ability] king_return: summoned=" .. king_card.inventory_item_id ..
            " beside ria=" .. ria_card.inventory_item_id ..
            " slot=" .. tostring(king_card.slot_index))
    end

    -- 2. Apply damage to Ria once after summoning completes (do not deal damage prior to summoning)
    local ria_damage_err = advanced_apply_ability_damage_to_ria(
        state, source_card, ria_card, ability_atk, front_line, void_key, helpers, actions)
    if ria_damage_err ~= nil then return actions, ria_damage_err end

    advanced_send_source_to_void(state, source_side, source_card, void_zone, battle, actions)

    return actions, nil
end
