-- lib_ability_mid_game
-- is_library = true

-- ability: titan_fall
-- Replaces a threatened Human with Titan before the queued opposing attack resolves.
function titan_fall_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "titan_fall requires a Human target card"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "titan_fall source card is not on a battle line"
    end

    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    local target_index = nil
    if target_line ~= nil then
        for i, card in ipairs(target_line) do
            if card.inventory_item_id == target_card.inventory_item_id then
                target_index = i
                break
            end
        end
    end
    if target_index == nil then
        return {}, "titan_fall target must be on a battle line"
    end

    local target_def = helpers.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
    local target_race = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.race or nil
    if target_type ~= "character" or target_race ~= "human" then
        return {}, "titan_fall target must be a Human character"
    end

    local base_def = target_def.base_stats ~= nil and target_def.base_stats.def or 0
    local final_def = target_card.final_def or base_def
    local def_buff = final_def - base_def
    local titan_fall_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local def_buff_required = titan_fall_def ~= nil and titan_fall_def.base_stats ~= nil
        and titan_fall_def.base_stats.def_buff_required or 0
    if def_buff_required <= 0 then
        return {}, "titan_fall requires a positive base_stats.def_buff_required"
    end
    if def_buff < def_buff_required then
        return {}, "titan_fall target requires at least +" .. def_buff_required .. " DEF"
    end

    local opponent_planning = source_side == "alpha" and (state.omega_planning or {}) or (state.alpha_planning or {})
    local attack_plan = nil
    for _, plan in ipairs(opponent_planning) do
        if plan.action == "card_attack_card" and plan.defender_inv_id == target_card.inventory_item_id then
            attack_plan = plan
            break
        end
    end
    if attack_plan == nil then
        return {}, "titan_fall target is not being attacked"
    end

    local attacker_card = nil
    local attacker_def = nil
    local opponent_lines = source_side == "alpha"
        and { state.omega_front_line or {}, state.omega_back_line or {} }
        or { state.alpha_front_line or {}, state.alpha_back_line or {} }
    for _, line in ipairs(opponent_lines) do
        for _, card in ipairs(line) do
            if card.inventory_item_id == attack_plan.attacker_inv_id then
                attacker_card = card
                attacker_def = helpers.find_item_def(state.item_defs, card.item_definition_code_name)
                break
            end
        end
        if attacker_card ~= nil then break end
    end
    if attacker_card == nil or attacker_def == nil then
        return {}, "titan_fall attacking card was not found"
    end

    local attacker_atk = attacker_def.base_stats ~= nil and attacker_def.base_stats.atk or 0
    local accumulated_damage = target_card.total_damage_received or 0
    local total_attack_damage = attacker_atk + accumulated_damage
    if total_attack_damage < final_def then
        local remaining_def = final_def - total_attack_damage
        return {}, "titan_fall cannot trigger: target Human would survive with " .. remaining_def .. " DEF remaining"
    end
    if total_attack_damage == final_def then
        return {}, "titan_fall cannot trigger: attack equals target DEF; Titan Fall requires damage to exceed DEF"
    end

    local ren_card = helpers.find_line_card_by_code(state[source_side .. "_front_line"], "azure_blade")
    if ren_card == nil then
        ren_card = helpers.find_line_card_by_code(state[source_side .. "_back_line"], "azure_blade")
    end
    if ren_card == nil then
        return {}, "titan_fall requires azure_blade on the field"
    end

    local void_key = source_side .. "_the_void"
    local titan_card = nil
    local titan_index = nil
    for i, card in ipairs(state[void_key] or {}) do
        if card.item_definition_code_name == "titan" then
            titan_card = card
            titan_index = i
            break
        end
    end
    if titan_card == nil then
        return {}, "titan_fall requires titan in the_void"
    end
    local summon_turn_err = battle.validate_summon_card_turn(state, state.item_defs, titan_card)
    if summon_turn_err ~= nil then return {}, summon_turn_err end

    local target_slot_index = target_card.slot_index
    table.remove(state[void_key], titan_index)
    table.insert(state[void_key], target_card)
    battle.reset_card_turn_state(state.item_defs, titan_card, state)
    titan_card.slot_index = target_slot_index
    titan_card.face_up = true
    titan_card.expose = true
    titan_card.trigger = true
    target_line[target_index] = titan_card
    attack_plan.defender_inv_id = titan_card.inventory_item_id

    battle.dlog("[ability] titan_fall: target=" .. target_card.inventory_item_id .. " def_buff=" .. def_buff .. " def_buff_required=" .. def_buff_required .. " attacker_atk=" .. attacker_atk .. " accumulated_damage=" .. accumulated_damage .. " titan=" .. titan_card.inventory_item_id)
    local expose_ren = helpers.expose_ability_selected_card(state, ren_card)
    local actions = {}
    if expose_ren ~= nil then table.insert(actions, expose_ren) end
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=titan_fall,target=" .. target_card.inventory_item_id .. ",selected=" .. ren_card.inventory_item_id)
    battle.append_card_sent_to_void_action(actions, source_side, target_card)
    table.insert(actions, source_side .. "_void_to_front_line:" .. titan_card.inventory_item_id .. "," .. tostring(target_slot_index))
    return actions, nil
end

-- ability: titan_spear_sweep
-- Titan deals base_stats.atk to every opposing Character, then
-- base_stats.shockwave_atk to one adjacent ally unless that ally is Ren.
function titan_spear_sweep_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "titan_spear_sweep source card is not on the battlefield"
    end

    local titan_card = nil
    local titan_line = nil
    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line" }) do
        for _, card in ipairs(state[line_key] or {}) do
            if card.inventory_item_id ~= nil and card.inventory_item_id ~= "" and
               card.item_definition_code_name == "titan" then
                titan_card = card
                titan_line = state[line_key]
                break
            end
        end
        if titan_card ~= nil then break end
    end
    if titan_card == nil then
        return {}, "titan_spear_sweep requires Titan on the battlefield"
    end
    if titan_card.trigger == true then
        return {}, "titan_spear_sweep requires Titan to be ready"
    end

    local ability_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local ability_stats = ability_def ~= nil and ability_def.base_stats or nil
    local enemy_damage = ability_stats ~= nil and tonumber(ability_stats.atk) or nil
    if enemy_damage == nil or enemy_damage <= 0 then
        return {}, "titan_spear_sweep requires a positive base_stats.atk"
    end
    local ally_damage = ability_stats ~= nil and tonumber(ability_stats.shockwave_atk) or nil
    if ally_damage == nil or ally_damage <= 0 then
        return {}, "titan_spear_sweep requires a positive base_stats.shockwave_atk"
    end

    titan_card.trigger = true
    titan_card.face_up = true
    titan_card.expose = true

    local ability_actions = {
        source_side .. "_card_expose:" .. titan_card.inventory_item_id,
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=titan_spear_sweep,selected=" .. titan_card.inventory_item_id,
    }
    local target_side = source_side == "alpha" and "omega" or "alpha"
    local target_lines = {
        { side = target_side, line = state[target_side .. "_front_line"] or {} },
    }
    local function deal_sweep_damage(target_card, target_line, target_void_key, damage)
        table.insert(ability_actions, source_side .. "_attack:" ..
            titan_card.inventory_item_id .. "," .. target_card.inventory_item_id)
        local damage_actions, damage_err = helpers.deal_damage_to_character(
            state, titan_card, target_card, damage, target_line, target_void_key
        )
        if damage_err ~= nil then return damage_err end
        for _, action in ipairs(damage_actions) do
            table.insert(ability_actions, action)
        end
        battle.dlog("[ability] titan_spear_sweep: titan=" .. titan_card.inventory_item_id ..
            " target=" .. target_card.inventory_item_id .. " damage=" .. damage)
        return nil
    end

    for _, target_entry in ipairs(target_lines) do
        local void_key = target_entry.side .. "_the_void"
        for _, target_card in ipairs(target_entry.line) do
            if target_card.inventory_item_id ~= nil and target_card.inventory_item_id ~= "" and
               battle.check_card_type(state.item_defs, target_card, "character") then
                local damage_err = deal_sweep_damage(target_card, target_entry.line, void_key, enemy_damage)
                if damage_err ~= nil then return ability_actions, damage_err end
            end
        end
    end

    local adjacent_ally = nil
    local titan_slot = titan_card.slot_index or 0
    for _, adjacent_slot in ipairs({ titan_slot + 1, titan_slot - 1 }) do
        for _, ally_card in ipairs(titan_line or {}) do
            if ally_card.inventory_item_id ~= nil and ally_card.inventory_item_id ~= "" and
               ally_card.inventory_item_id ~= titan_card.inventory_item_id and
               ally_card.slot_index == adjacent_slot and
               battle.check_card_type(state.item_defs, ally_card, "character") then
                local ally_def = helpers.find_item_def(state.item_defs, ally_card.item_definition_code_name)
                local ally_char_code_required = ally_def ~= nil and ally_def.metadata ~= nil
                    and ally_def.metadata.char_code_required or nil
                if ally_card.item_definition_code_name ~= "azure_blade" and
                   ally_char_code_required ~= "azure_blade" then
                    adjacent_ally = ally_card
                else
                    battle.dlog("[ability] titan_spear_sweep: adjacent Ren is immune")
                end
                break
            end
        end
        if adjacent_ally ~= nil then break end
    end

    if adjacent_ally ~= nil then
        local damage_err = deal_sweep_damage(
            adjacent_ally, titan_line, source_side .. "_the_void", ally_damage
        )
        if damage_err ~= nil then return ability_actions, damage_err end
    end

    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    local source_void_key = source_side .. "_the_void"
    if state[source_void_key] == nil then state[source_void_key] = {} end
    table.insert(state[source_void_key], source_card)
    battle.append_card_sent_to_void_action(ability_actions, source_side, source_card)

    return ability_actions, nil
end
