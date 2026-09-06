function totem_pulse_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] totem_pulse ====================")

    local source_side = helpers.find_card_side(state, source_card)
    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local totem_item_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local def_added = (totem_item_def ~= nil and totem_item_def.base_stats ~= nil and totem_item_def.base_stats.def_added) or 0
    battle.dlog("[ability] totem_pulse: source=" .. source_card.inventory_item_id .. " side=" .. source_side .. " def_added=" .. def_added)

    local shaman_card = helpers.find_untriggered_card(front_line, function(c) return c.item_definition_code_name == "goblin_shaman" end)
    if shaman_card == nil then
        battle.dlog("[ability] totem_pulse: error - no untriggered goblin_shaman in " .. front_line_key)
        return {}, "totem_pulse requires untriggered goblin_shaman in front_line"
    end

    battle.dlog("[ability] totem_pulse: untriggered goblin_shaman found: " .. shaman_card.inventory_item_id)
    shaman_card.trigger = true
    local ability_actions = {}
    local expose_action = helpers.expose_ability_selected_card(state, shaman_card)
    if expose_action ~= nil then table.insert(ability_actions, expose_action) end
    for _, front_card in ipairs(front_line) do
        local has_id = front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
        if has_id then
            local prev_def = front_card.final_def or 0
            front_card.final_def = prev_def + def_added
            battle.dlog("[ability] totem_pulse: buffed card=" .. front_card.inventory_item_id .. " final_def " .. prev_def .. " -> " .. front_card.final_def)
            local buff_action = source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=totem_pulse,target=" .. front_card.inventory_item_id .. ",selected=" .. shaman_card.inventory_item_id
            table.insert(ability_actions, buff_action)
            table.insert(ability_actions, source_side .. "_card_guarded:" .. front_card.inventory_item_id)
        end
    end

    local back_line_key = source_side .. "_back_line"
    local back_line = state[back_line_key] or {}
    battle.remove_card_from_line(back_line, source_card.inventory_item_id)
    local void_key = source_side .. "_the_void"
    if state[void_key] == nil then state[void_key] = {} end
    table.insert(state[void_key], source_card)
    battle.dlog("[ability] totem_pulse: source card sent to void=" .. void_key .. " id=" .. source_card.inventory_item_id)
    battle.append_card_sent_to_void_action(ability_actions, source_side, source_card)

    return ability_actions, nil
end
-- ability: back_stab
function back_stab_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] back_stab ====================")

    local defender = (event_data or {}).defender_card
    if defender == nil then
        battle.dlog("[ability] back_stab: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local source_side = helpers.find_card_side(state, source_card)
    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local grunt_card = helpers.find_untriggered_card(front_line, function(c)
        return c.item_definition_code_name == "goblin_grunt"
    end)
    if grunt_card == nil then
        battle.dlog("[ability] back_stab: error - no untriggered goblin_grunt in " .. front_line_key)
        return {}, "back_stab requires untriggered goblin_grunt in front_line"
    end
    grunt_card.trigger = true

    if defender.inventory_item_id == grunt_card.inventory_item_id then
        battle.dlog("[ability] back_stab: error - defender matches selected goblin_grunt id=" .. tostring(grunt_card.inventory_item_id))
        return {}, "back_stab cannot target the selected goblin_grunt"
    end

    local line_key = (event_data or {}).defender_line_key
    local void_key = (event_data or {}).defender_side_void
    local defender_line = line_key ~= nil and state[line_key] or nil

    -- Use the shared calculation so Back Stab always matches the client preview:
    -- Goblin Grunt's base_stats.atk plus Back Stab's base_stats.atk_added.
    local source_item_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local damage = battle.get_attack_damage(state, source_item_def, source_side .. "_back_line", source_card)
    battle.dlog("[ability] back_stab: goblin_grunt=" .. grunt_card.inventory_item_id .. " target=" .. defender.inventory_item_id .. " damage=" .. damage)

    local expose_action = helpers.expose_ability_selected_card(state, grunt_card)
    local ability_actions = {
        expose_action,
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=back_stab,target=" .. defender.inventory_item_id .. ",selected=" .. grunt_card.inventory_item_id .. ",damage=" .. tostring(damage)
    }
    local damage_actions, dmg_err = helpers.deal_damage_to_character(state, grunt_card, defender, damage, defender_line, void_key)
    if dmg_err ~= nil then return ability_actions, dmg_err end
    for _, action in ipairs(damage_actions) do
        table.insert(ability_actions, action)
    end
    return ability_actions, nil
end

-- ability: brute_call
-- Summons Goblin Brute from the void beside the selected Goblin Shaman.
-- An adjacent 1- or 2-star Goblin is trampled; otherwise an empty adjacent
-- slot is used. The Ability is consumed even when neither slot is valid.
function brute_call_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] brute_call ====================")

    local shaman_card = (event_data or {}).defender_card
    if shaman_card == nil then
        return {}, "brute_call requires a Goblin Shaman target"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "brute_call source card is not on a battle line"
    end

    local front_line_key = source_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    local shaman_index = nil
    for index, card in ipairs(front_line) do
        if card.inventory_item_id == shaman_card.inventory_item_id then
            shaman_index = index
            break
        end
    end
    if shaman_index == nil then
        return {}, "brute_call target must be on own front_line"
    end

    local shaman_def = helpers.find_item_def(state.item_defs, shaman_card.item_definition_code_name)
    local shaman_type = shaman_def ~= nil and shaman_def.metadata ~= nil and shaman_def.metadata.type or nil
    local shaman_code_required = shaman_def ~= nil and shaman_def.metadata ~= nil and shaman_def.metadata.char_code_required or nil
    if shaman_type ~= "character" or
       (shaman_card.item_definition_code_name ~= "goblin_shaman" and shaman_code_required ~= "goblin_shaman") then
        return {}, "brute_call target must be Goblin Shaman"
    end

    local live_shaman_card = front_line[shaman_index]
    if live_shaman_card.trigger == true then
        return {}, "brute_call requires an untriggered Goblin Shaman in own front_line"
    end

    local void_key = source_side .. "_the_void"
    local void_zone = state[void_key] or {}
    local brute_card = nil
    local brute_index = nil
    for index, card in ipairs(void_zone) do
        if card.item_definition_code_name == "goblin_brute" then
            brute_card = card
            brute_index = index
            break
        end
    end
    if brute_card == nil then
        return {}, "brute_call requires Goblin Brute in own the_void"
    end
    local summon_turn_err = battle.validate_summon_card_turn(state, state.item_defs, brute_card)
    if summon_turn_err ~= nil then return {}, summon_turn_err end

    local adjacent_indexes = { shaman_index - 1, shaman_index + 1 }
    local chosen_index = nil
    local chosen_sacrifice = nil
    local chosen_stars = nil

    -- Brute Call has exactly two valid sacrifices. Do not infer eligibility
    -- from race, type, or star metadata: those fields are unrelated to this
    -- ability's card-specific requirement.
    local function get_adjacent_goblin_sacrifice(card)
        if card == nil or card.inventory_item_id == nil or card.inventory_item_id == "" then
            return nil
        end

        local code = card.item_definition_code_name
        if code == "goblin_grunt" then
            return 1
        end
        if code == "goblin_saboteur" then
            return 2
        end
        return nil
    end

    for _, index in ipairs(adjacent_indexes) do
        local card = front_line[index]
        local card_stars = get_adjacent_goblin_sacrifice(card)
        if card_stars ~= nil and (chosen_stars == nil or card_stars < chosen_stars) then
            chosen_index = index
            chosen_sacrifice = card
            chosen_stars = card_stars
        end
    end

    if chosen_index == nil then
        for _, index in ipairs(adjacent_indexes) do
            if index >= 1 and index <= #front_line then
                local card = front_line[index]
                if card == nil or card.inventory_item_id == nil or card.inventory_item_id == "" then
                    chosen_index = index
                    break
                end
            end
        end
    end

    -- event_data may carry a detached target snapshot. Mutate the card stored
    -- in the battle line so the consumed Shaman state is persisted.
    local ability_actions = {}
    local expose_action = helpers.expose_ability_selected_card(state, live_shaman_card)
    if expose_action ~= nil then table.insert(ability_actions, expose_action) end
    live_shaman_card.trigger = true

    if chosen_index ~= nil then
        if chosen_sacrifice ~= nil then
            front_line[chosen_index] = {}
            table.insert(void_zone, chosen_sacrifice)
        end

        table.remove(void_zone, brute_index)
        battle.reset_card_turn_state(state.item_defs, brute_card)
        brute_card.slot_index = chosen_index - 1
        brute_card.face_up = true
        brute_card.expose = true
        brute_card.trigger = true
        brute_card.defeated_from_line_key = nil
        front_line[chosen_index] = brute_card
        -- Write through the line slot as well, matching the persisted state
        -- consumed by the attack validator.
        front_line[chosen_index].trigger = true

        local success_action = source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=brute_call,target=" .. shaman_card.inventory_item_id ..
            ",selected=" .. shaman_card.inventory_item_id ..
            ",result=success,summoned=" .. brute_card.inventory_item_id
        if chosen_sacrifice ~= nil then
            success_action = success_action .. ",sacrificed=" .. chosen_sacrifice.inventory_item_id
        end
        table.insert(ability_actions, success_action)
        if chosen_sacrifice ~= nil then
            battle.append_card_sent_to_void_action(ability_actions, source_side, chosen_sacrifice)
        end
        table.insert(ability_actions, source_side .. "_void_to_front_line:" ..
            brute_card.inventory_item_id .. "," .. tostring(brute_card.slot_index))
        battle.dlog("[ability] brute_call: summoned=" .. brute_card.inventory_item_id ..
            " beside shaman=" .. shaman_card.inventory_item_id ..
            " slot=" .. tostring(brute_card.slot_index))
    else
        table.insert(ability_actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=brute_call,target=" .. shaman_card.inventory_item_id ..
            ",selected=" .. shaman_card.inventory_item_id ..
            ",result=failed,reason=no_adjacent_position")
        battle.dlog("[ability] brute_call: failed - no valid adjacent position beside shaman=" ..
            shaman_card.inventory_item_id)
    end

    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        battle.remove_card_from_line(state[line_key], source_card.inventory_item_id)
    end
    table.insert(void_zone, source_card)
    battle.append_card_sent_to_void_action(ability_actions, source_side, source_card)

    return ability_actions, nil
end

-- ability: holy_glow
