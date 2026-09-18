-- ability: eagle_eye
-- Expose one face-down enemy Character while Lyra is on the caster's front line.
-- Eagle Eye reveals Lyra, but does not trigger her.
function eagle_eye_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] eagle_eye ====================")

    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        return {}, "eagle_eye requires a target card"
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "eagle_eye source card is not on a battle line"
    end

    local target_def = helpers.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local target_type = target_def ~= nil and target_def.metadata ~= nil and target_def.metadata.type or nil
    if target_type ~= "character" then
        return {}, "eagle_eye can target only a character card"
    end
    if target_card.face_up == true or target_card.expose == true then
        return {}, "eagle_eye requires a face-down character target"
    end

    local lyra_card = nil
    local front_line = state[source_side .. "_front_line"] or {}
    for _, line_card in ipairs(front_line) do
        local has_id = line_card.inventory_item_id ~= nil and line_card.inventory_item_id ~= ""
        if has_id then
            local line_def = helpers.find_item_def(state.item_defs, line_card.item_definition_code_name)
            local line_type = line_def ~= nil and line_def.metadata ~= nil and line_def.metadata.type or nil
            local char_code_required = line_def ~= nil and line_def.metadata ~= nil and line_def.metadata.char_code_required or nil
            if line_type == "character" and (line_card.item_definition_code_name == "lyra" or char_code_required == "lyra") then
                lyra_card = line_card
                break
            end
        end
    end
    if lyra_card == nil then
        return {}, "eagle_eye requires Lyra on the caster's front_line"
    end

    source_card.face_up = true
    source_card.expose = true
    lyra_card.face_up = true
    lyra_card.expose = true
    target_card.face_up = true
    target_card.expose = true
    local target_side = helpers.find_card_side(state, target_card)
    local ability_actions = {
        helpers.lib_battle_common.build_card_expose_action(source_side, source_card),
        helpers.lib_battle_common.build_card_expose_action(source_side, lyra_card),
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=eagle_eye,target=" .. target_card.inventory_item_id .. ",required=" .. lyra_card.inventory_item_id,
        helpers.lib_battle_common.build_card_expose_action(target_side, target_card)
    }

    -- Ability cards are consumed after resolving an ability-only action.
    for _, line_key in ipairs({ source_side .. "_front_line", source_side .. "_back_line", source_side .. "_hand" }) do
        local line = state[line_key]
        if line ~= nil then
            battle.remove_card_from_line(line, source_card.inventory_item_id)
        end
    end
    local void_key = source_side .. "_the_void"
    if state[void_key] == nil then state[void_key] = {} end
    table.insert(state[void_key], source_card)
    battle.append_card_sent_to_void_action(ability_actions, source_side, source_card)

    battle.dlog("[ability] eagle_eye: exposed Lyra=" .. lyra_card.inventory_item_id .. " and target=" .. target_card.inventory_item_id)
    return ability_actions, nil
end


-- ability: spinning_slash
function spinning_slash_execute(state, attacker_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] spinning_slash ====================")

    local defender = (event_data or {}).defender_card
    if defender == nil then
        battle.dlog("[ability] spinning_slash: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local line_key = (event_data or {}).defender_line_key
    local void_key = (event_data or {}).defender_side_void

    local attacker_side = helpers.find_card_side(state, attacker_card)
    local front_line_key = attacker_side .. "_front_line"
    local front_line = state[front_line_key] or {}
    battle.dlog("[ability] spinning_slash: attacker=" .. attacker_card.inventory_item_id .. " side=" .. attacker_side .. " selecting from " .. front_line_key)

    local azure_blade_card = helpers.find_untriggered_card(front_line, function(c) return c.item_definition_code_name == "azure_blade" end)
    if azure_blade_card == nil then
        battle.dlog("[ability] spinning_slash: error - no untriggered azure_blade in " .. front_line_key)
        return {}, "spinning_slash requires untriggered azure_blade in front_line"
    end
    azure_blade_card.trigger = true

    local ability_item_def = helpers.find_item_def(state.item_defs, "spinning_slash")
    local atk_added = 0
    if ability_item_def ~= nil then
        if ability_item_def.base_stats ~= nil and ability_item_def.base_stats.atk_added then
            atk_added = ability_item_def.base_stats.atk_added
        elseif ability_item_def.metadata ~= nil and ability_item_def.metadata.atk_added then
            atk_added = ability_item_def.metadata.atk_added
        end
    end

    local azure_blade_item_def = helpers.find_item_def(state.item_defs, azure_blade_card.item_definition_code_name)
    local char_atk = (azure_blade_item_def ~= nil and azure_blade_item_def.base_stats ~= nil and azure_blade_item_def.base_stats.atk) or 0

    local damage = atk_added + char_atk
    battle.dlog("[ability] spinning_slash: azure_blade=" .. azure_blade_card.inventory_item_id .. " total_damage=" .. damage)

    local defender_line = line_key ~= nil and state[line_key] or nil
    local expose_action = helpers.expose_ability_selected_card(state, azure_blade_card)
    local ability_actions = {
        expose_action,
        attacker_side .. "_card_ability:source=" .. attacker_card.inventory_item_id .. ",ability=spinning_slash,target=" .. defender.inventory_item_id .. ",selected=" .. azure_blade_card.inventory_item_id
    }
    local damage_actions, dmg_err = helpers.deal_damage_to_character(state, azure_blade_card, defender, damage, defender_line, void_key)
    if dmg_err ~= nil then return ability_actions, dmg_err end
    for _, action in ipairs(damage_actions) do
        table.insert(ability_actions, action)
    end
    return ability_actions, nil
end
-- ability: cross_guard
function cross_guard_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] cross_guard ====================")

    local target_card = (event_data or {}).defender_card
    if target_card == nil then
        battle.dlog("[ability] cross_guard: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local source_side = helpers.find_card_side(state, source_card)
    local source_front_line_key = source_side .. "_front_line"
    local azure_blade_card = helpers.find_untriggered_card(state[source_front_line_key], function(c) return c.item_definition_code_name == "azure_blade" end)
    if azure_blade_card == nil then
        battle.dlog("[ability] cross_guard: error - no untriggered azure_blade in " .. source_front_line_key)
        return {}, "cross_guard requires untriggered azure_blade in front_line"
    end
    azure_blade_card.trigger = true

    local source_item_def = helpers.find_item_def(state.item_defs, source_card.item_definition_code_name)
    local guard_bonus = 0
    if source_item_def ~= nil then
        if source_item_def.metadata ~= nil and source_item_def.metadata.def_added ~= nil then
            guard_bonus = source_item_def.metadata.def_added
        elseif source_item_def.base_stats ~= nil and source_item_def.base_stats.def_added ~= nil then
            guard_bonus = source_item_def.base_stats.def_added
        end
    end
    local prev_def = target_card.final_def or 0
    target_card.final_def = prev_def + guard_bonus
    local expose_action = helpers.expose_ability_selected_card(state, azure_blade_card)
    battle.dlog("[ability] cross_guard: target=" .. target_card.inventory_item_id .. " def_added=" .. guard_bonus .. " final_def " .. prev_def .. " -> " .. target_card.final_def)
    local guard_actions = {
        expose_action,
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=cross_guard,target=" .. target_card.inventory_item_id .. ",selected=" .. azure_blade_card.inventory_item_id,
        source_side .. "_card_guarded:" .. target_card.inventory_item_id
    }
    return guard_actions, nil
end
-- ability: totem_pulse
