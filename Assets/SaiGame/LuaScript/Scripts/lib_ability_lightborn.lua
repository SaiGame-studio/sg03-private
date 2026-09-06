function holy_glow_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] holy_glow ====================")

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
            return "alpha" -- fallback
        end
        caster_side = find_side_in_all_zones(source_card.inventory_item_id)
    end

    local frontline_key = caster_side .. "_front_line"
    local front_line = state[frontline_key] or {}
    local lightborn_female_card = helpers.find_untriggered_card(front_line, function(c)
        local def = helpers.find_item_def(state.item_defs, c.item_definition_code_name)
        local is_lightborn = def ~= nil and def.metadata ~= nil and
            (def.metadata.race == "lightborn" or def.metadata.race == "light_elf")
        return def ~= nil and def.metadata ~= nil and
            def.metadata.type == "character" and
            is_lightborn and
            def.metadata.gender == "female"
    end)

    if lightborn_female_card == nil then
        battle.dlog("[ability] holy_glow: error - no untriggered female Lightborn character in " .. frontline_key)
        return {}, "holy_glow requires an untriggered female Lightborn character in front_line"
    end

    lightborn_female_card.trigger = true
    local expose_action = helpers.expose_ability_selected_card(state, lightborn_female_card)

    local target_side = event_data ~= nil and event_data.target_player_side or caster_side
    if target_side ~= "alpha" and target_side ~= "omega" then
        target_side = caster_side
    end
    local hp_key = target_side .. "_hp"
    local max_hp_key = target_side .. "_max_hp"

    -- Ensure max HP is declared for the battle
    if state.alpha_max_hp == nil then
        state.alpha_max_hp = state.alpha_hp
    end
    if state.omega_max_hp == nil then
        state.omega_max_hp = state.omega_hp
    end

    local card_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local hp_restore = 0
    if card_def ~= nil and card_def.base_stats ~= nil and card_def.base_stats.hp_restore ~= nil then
        hp_restore = card_def.base_stats.hp_restore
    end

    local current_hp = state[hp_key] or 0
    local max_hp = state[max_hp_key] or current_hp
    local new_hp = current_hp + hp_restore
    if new_hp > max_hp then
        new_hp = max_hp
    end
    local actual_restored = new_hp - current_hp
    state[hp_key] = new_hp

    local is_ability_card = (card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.type == "ability")
    local will_system_send_to_void = false
    if is_ability_card then
        local defender_line_key = event_data ~= nil and event_data.defender_line_key or nil
        if defender_line_key == "alpha_front_line" or
           defender_line_key == "alpha_back_line"  or
           defender_line_key == "omega_front_line" or
           defender_line_key == "omega_back_line" then
            will_system_send_to_void = true
        end
    end

    battle.dlog("[ability] holy_glow: caster=" .. source_card.inventory_item_id .. " side=" .. caster_side .. " target_side=" .. target_side .. " lightborn_female=" .. lightborn_female_card.inventory_item_id .. " restore=" .. hp_restore .. " actual_restored=" .. actual_restored .. " new_hp=" .. new_hp .. "/" .. max_hp .. " will_system_send_to_void=" .. tostring(will_system_send_to_void))

    if not will_system_send_to_void then
        -- Move source card to the void
        local lines_to_check = {
            caster_side .. "_front_line",
            caster_side .. "_back_line",
            caster_side .. "_hand"
        }
        for _, line_key in ipairs(lines_to_check) do
            local line = state[line_key]
            if line ~= nil then
                battle.remove_card_from_line(line, source_card.inventory_item_id)
            end
        end

        local void_key = caster_side .. "_the_void"
        if state[void_key] == nil then state[void_key] = {} end
        table.insert(state[void_key], source_card)
        battle.dlog("[ability] holy_glow: source card sent to void=" .. void_key .. " id=" .. source_card.inventory_item_id)
    end

    local ability_actions = {}
    if expose_action ~= nil then
        table.insert(ability_actions, expose_action)
    end
    table.insert(ability_actions, caster_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=holy_glow,target=" .. target_side .. ",hp_restore=" .. hp_restore .. ",actual_restored=" .. actual_restored .. "," .. hp_key .. "=" .. state[hp_key] .. ",caster=" .. lightborn_female_card.inventory_item_id .. ",selected=" .. lightborn_female_card.inventory_item_id)
    
    if not will_system_send_to_void then
        battle.append_card_sent_to_void_action(ability_actions, caster_side, source_card)
    end

    return ability_actions, nil
end

-- ability: static_bind
-- Stuns an enemy Character. A stunned Character cannot execute a queued attack;
-- every queued plan it owns is removed and its planning lunge is returned to
-- its holder. Damage comes only from the ability definition's atk stat.
function static_bind_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "static_bind requires a target character"
    end
    if not battle.check_card_type(state.item_defs, target_card, "character") then
        return {}, "static_bind can target only a character card"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "static_bind source card is not on a battle line"
    end

    local azura_card = helpers.find_untriggered_card(state[source_side .. "_front_line"], function(card)
        local card_def = helpers.find_item_def(state.item_defs, card.item_definition_code_name)
        local char_code_required = card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.char_code_required or nil
        return card.item_definition_code_name == "volt_heart" or char_code_required == "volt_heart"
    end)
    if azura_card == nil then
        return {}, "static_bind requires an untriggered Azura in front_line"
    end
    azura_card.trigger = true
    azura_card.face_up = true
    azura_card.expose = true

    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    if target_line == nil then
        return {}, "static_bind target must be on a battle line"
    end

    local ability_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local ability_stats = ability_def ~= nil and ability_def.base_stats or nil
    local damage = ability_stats ~= nil and tonumber(ability_stats.atk) or nil
    if damage == nil or damage <= 0 then
        return {}, "static_bind requires a positive base_stats.atk"
    end

    battle.mark_card_skip_next_turn(target_card)
    target_card.face_up = true
    target_card.expose = true

    local target_id = target_card.inventory_item_id
    local cancelled_plan = false
    for _, planning_key in ipairs({ "alpha_planning", "omega_planning" }) do
        local planning = state[planning_key]
        if planning ~= nil then
            for index = #planning, 1, -1 do
                local plan = planning[index]
                if plan ~= nil and plan.attacker_inv_id == target_id then
                    table.remove(planning, index)
                    cancelled_plan = true
                end
            end
        end
    end

    if state.pending_attack ~= nil and state.pending_attack.attacker_inventory_item_id == target_id then
        state.pending_attack.cancelled = true
        cancelled_plan = true
    end

    local target_side = target_line_key:sub(1, 5) == "alpha" and "alpha" or "omega"
    local actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=static_bind,target=" .. target_id .. ",damage=" .. tostring(damage) ..
            ",cancelled_plan=" .. tostring(cancelled_plan) .. ",skip_next_turn=true,selected=" .. azura_card.inventory_item_id,
        source_side .. "_card_expose:" .. azura_card.inventory_item_id,
        target_side .. "_card_expose:" .. target_id,
    }

    local damage_actions, damage_err = helpers.deal_damage_to_character(
        state, source_card, target_card, damage, target_line, target_side .. "_the_void"
    )
    if damage_err ~= nil then return actions, damage_err end
    for _, action in ipairs(damage_actions) do
        table.insert(actions, action)
    end

    if cancelled_plan and target_card.total_damage_received < (target_card.final_def or 0) then
        table.insert(actions, "card_move_back_to_holder:" .. target_id)
    end

    -- Static Bind is an ability card, so consume it after its effect resolves.
    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    local source_void_key = source_side .. "_the_void"
    if state[source_void_key] == nil then state[source_void_key] = {} end
    table.insert(state[source_void_key], source_card)
    battle.append_card_sent_to_void_action(actions, source_side, source_card)

    battle.dlog("[ability] static_bind: azura=" .. azura_card.inventory_item_id .. " target=" .. target_id .. " damage=" .. tostring(damage) .. " cancelled_plan=" .. tostring(cancelled_plan))
    return actions, nil
end

-- ability: lux_maxima
-- The selected Aura decides the one configured Darkborn Aura code to remove.
-- Every on-field Aura with that exact code is then removed and reconciled.
function lux_maxima_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "lux_maxima requires a target Darkborn Aura"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "lux_maxima source card is not on a battle line"
    end

    local diana_card = helpers.find_line_card_by_code(
        state[source_side .. "_front_line"], "diana")
    if diana_card == nil then
        diana_card = helpers.find_line_card_by_code(
            state[source_side .. "_back_line"], "diana")
    end
    if diana_card == nil then
        return {}, "lux_maxima requires Diana on the battlefield"
    end

    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    if target_line == nil then
        return {}, "lux_maxima target must be on a battle line"
    end

    local config = lib_ability_config.get_ability_config("lux_maxima") or {}
    local allowed_codes = config.counterable_darkborn_aura_codes or {}
    if not lib_ability_aura.is_configured_darkborn_aura(state, target_card, allowed_codes) then
        return {}, "lux_maxima target must be a configured Darkborn Aura"
    end

    local selected_aura_code = target_card.item_definition_code_name
    local aura_targets = lib_ability_aura.find_configured_darkborn_auras(
        state, allowed_codes, selected_aura_code)
    if #aura_targets == 0 then
        return {}, "lux_maxima target must be a configured Darkborn Aura"
    end

    local actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=lux_maxima,target=" .. target_card.inventory_item_id ..
            ",selected=" .. diana_card.inventory_item_id ..
            ",target_code=" .. selected_aura_code ..
            ",target_count=" .. tostring(#aura_targets),
    }
    local diana_expose_action = helpers.expose_ability_selected_card(state, diana_card)
    if diana_expose_action ~= nil then table.insert(actions, diana_expose_action) end

    local removed_sources = {}
    for _, aura_target in ipairs(aura_targets) do
        local aura_code = aura_target.card.item_definition_code_name
        if removed_sources[aura_code] == nil then
            removed_sources[aura_code] = {
                id = aura_target.card.inventory_item_id,
                side = aura_target.side,
            }
        end
        battle.remove_card_from_line(aura_target.line, aura_target.card.inventory_item_id)
        local target_void_key = aura_target.side .. "_the_void"
        if state[target_void_key] == nil then state[target_void_key] = {} end
        table.insert(state[target_void_key], aura_target.card)
    end

    local aura_actions = lib_ability_aura.refresh_active_auras(
        state, "aura_removed", removed_sources)
    for _, aura_action in ipairs(aura_actions) do
        table.insert(actions, aura_action)
    end
    for _, aura_target in ipairs(aura_targets) do
        battle.append_card_sent_to_void_action(actions, aura_target.side, aura_target.card)
    end

    battle.remove_card_from_line(state[source_side .. "_front_line"], source_card.inventory_item_id)
    battle.remove_card_from_line(state[source_side .. "_back_line"], source_card.inventory_item_id)
    local source_void_key = source_side .. "_the_void"
    if state[source_void_key] == nil then state[source_void_key] = {} end
    table.insert(state[source_void_key], source_card)
    battle.append_card_sent_to_void_action(actions, source_side, source_card)
    return actions, nil
end

-- ability: lightning_strike
-- Azura strikes the selected enemy and, when available, one adjacent enemy on
-- the same battle line. Only 1- and 2-star targets are stunned: their queued attack is
-- cancelled and a pending lunge returns to its holder if they survive.
function lightning_strike_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "lightning_strike requires a target character"
    end
    if not battle.check_card_type(state.item_defs, target_card, "character") then
        return {}, "lightning_strike can target only a character card"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "lightning_strike source card is not on a battle line"
    end

    local azura_card = helpers.find_untriggered_card(state[source_side .. "_front_line"], function(card)
        local card_def = helpers.find_item_def(state.item_defs, card.item_definition_code_name)
        local char_code_required = card_def ~= nil and card_def.metadata ~= nil and card_def.metadata.char_code_required or nil
        return card.item_definition_code_name == "volt_heart" or char_code_required == "volt_heart"
    end)
    if azura_card == nil then
        return {}, "lightning_strike requires an untriggered Azura in front_line"
    end

    local target_line_key = (event_data or {}).defender_line_key
    local target_line = target_line_key ~= nil and state[target_line_key] or nil
    if target_line == nil then
        return {}, "lightning_strike target must be on a battle line"
    end

    local target_slot = target_card.slot_index
    if target_slot == nil then
        return {}, "lightning_strike target requires a slot_index"
    end

    local adjacent_target = nil
    for _, adjacent_slot in ipairs({ target_slot - 1, target_slot + 1 }) do
        for _, line_card in ipairs(target_line) do
            if line_card.inventory_item_id ~= nil and line_card.inventory_item_id ~= "" and
               line_card.inventory_item_id ~= target_card.inventory_item_id and
               line_card.slot_index == adjacent_slot and
               battle.check_card_type(state.item_defs, line_card, "character") then
                adjacent_target = line_card
                break
            end
        end
        if adjacent_target ~= nil then break end
    end
    local ability_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local ability_stats = ability_def ~= nil and ability_def.base_stats or nil
    local damage = ability_stats ~= nil and tonumber(ability_stats.atk) or nil
    if damage == nil or damage <= 0 then
        return {}, "lightning_strike requires a positive base_stats.atk"
    end

    local targets = { target_card }
    if adjacent_target ~= nil then
        table.insert(targets, adjacent_target)
    end
    local target_stars = {}
    for index, target in ipairs(targets) do
        local target_def = helpers.find_item_def(state.item_defs, target.item_definition_code_name)
        local stars = target_def ~= nil and target_def.base_stats ~= nil and tonumber(target_def.base_stats.star) or nil
        if stars == nil then
            return {}, "lightning_strike target star is missing: " .. tostring(target.item_definition_code_name)
        end
        target_stars[index] = stars
    end

    azura_card.trigger = true
    azura_card.face_up = true
    azura_card.expose = true

    local target_side = target_line_key:sub(1, 5) == "alpha" and "alpha" or "omega"
    local ability_action = source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
        ",ability=lightning_strike,target=" .. target_card.inventory_item_id
    if adjacent_target ~= nil then
        ability_action = ability_action .. ",adjacent_target=" .. adjacent_target.inventory_item_id
    end
    ability_action = ability_action .. ",damage=" .. tostring(damage) .. ",selected=" .. azura_card.inventory_item_id
    local actions = {
        ability_action,
        source_side .. "_card_expose:" .. azura_card.inventory_item_id,
    }

    local function cancel_target_attack(target)
        local target_id = target.inventory_item_id
        local cancelled_plan = false
        for _, planning_key in ipairs({ "alpha_planning", "omega_planning" }) do
            local planning = state[planning_key]
            if planning ~= nil then
                for plan_index = #planning, 1, -1 do
                    local plan = planning[plan_index]
                    if plan ~= nil and plan.attacker_inv_id == target_id then
                        table.remove(planning, plan_index)
                        cancelled_plan = true
                    end
                end
            end
        end
        if state.pending_attack ~= nil and state.pending_attack.attacker_inventory_item_id == target_id then
            state.pending_attack.cancelled = true
            cancelled_plan = true
        end
        return cancelled_plan
    end

    for index, target in ipairs(targets) do
        local cancelled_plan = false
        if target_stars[index] <= 2 then
            battle.mark_card_skip_next_turn(target)
            cancelled_plan = cancel_target_attack(target)
        end

        local damage_actions, damage_err = helpers.deal_damage_to_character(
            state, source_card, target, damage, target_line, target_side .. "_the_void"
        )
        if damage_err ~= nil then return actions, damage_err end
        for _, action in ipairs(damage_actions) do
            table.insert(actions, action)
        end

        if cancelled_plan and target.total_damage_received < (target.final_def or 0) then
            table.insert(actions, "card_move_back_to_holder:" .. target.inventory_item_id)
        end
    end

    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    local source_void_key = source_side .. "_the_void"
    if state[source_void_key] == nil then state[source_void_key] = {} end
    table.insert(state[source_void_key], source_card)
    battle.append_card_sent_to_void_action(actions, source_side, source_card)

    battle.dlog("[ability] lightning_strike: azura=" .. azura_card.inventory_item_id ..
        " target=" .. target_card.inventory_item_id ..
        " adjacent_target=" .. (adjacent_target ~= nil and adjacent_target.inventory_item_id or "none") ..
        " damage=" .. tostring(damage))
    return actions, nil
end

-- ability: skeleton_shield
