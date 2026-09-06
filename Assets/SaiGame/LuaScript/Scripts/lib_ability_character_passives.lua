-- lib_ability_character_passives
-- is_library = true

-- ability: twin_reaper
function twin_reaper_execute(state, attacker_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    battle.dlog("== [ability] twin_reaper ====================")

    local defender = (event_data or {}).defender_card
    if defender == nil then
        battle.dlog("[ability] twin_reaper: skip - defender_card is nil in event_data")
        return {}, nil
    end

    local line_key = (event_data or {}).defender_line_key
    local void_key = (event_data or {}).defender_side_void
    local defender_line = line_key ~= nil and state[line_key] or nil
    if defender_line == nil then
        battle.dlog("[ability] twin_reaper: skip - defender_line_key missing or line is nil (line_key=" .. tostring(line_key) .. ")")
        return {}, nil
    end

    local defender_slot = defender.slot_index or 0
    battle.dlog("[ability] twin_reaper: defender=" .. defender.inventory_item_id .. " slot=" .. defender_slot)

    local target
    for _, slot_card in ipairs(defender_line) do
        if slot_card.inventory_item_id ~= nil and slot_card.inventory_item_id ~= ""
           and slot_card.inventory_item_id ~= defender.inventory_item_id
           and (slot_card.slot_index or 0) == defender_slot + 1 then
            target = slot_card
            break
        end
    end
    if target == nil then
        for _, slot_card in ipairs(defender_line) do
            if slot_card.inventory_item_id ~= nil and slot_card.inventory_item_id ~= ""
               and slot_card.inventory_item_id ~= defender.inventory_item_id
               and (slot_card.slot_index or 0) == defender_slot - 1 then
                target = slot_card
                break
            end
        end
    end
    if target == nil then
        battle.dlog("[ability] twin_reaper: no adjacent card found, skip")
        return {}, nil
    end

    local attacker_def = (event_data or {}).attacker_def
    local damage = (attacker_def ~= nil and attacker_def.base_stats and attacker_def.base_stats.atk) or 1
    battle.dlog("[ability] twin_reaper: target=" .. target.inventory_item_id .. " slot=" .. (target.slot_index or 0) .. " damage=" .. damage)

    local attacker_side = helpers.find_card_side(state, attacker_card)
    local ability_actions = { attacker_side .. "_card_ability:source=" .. attacker_card.inventory_item_id .. ",ability=twin_reaper,target=" .. target.inventory_item_id }
    local damage_actions, dmg_err = helpers.deal_damage_to_character(state, attacker_card, target, damage, defender_line, void_key)
    if dmg_err ~= nil then return ability_actions, dmg_err end
    for _, action in ipairs(damage_actions) do
        table.insert(ability_actions, action)
    end
    return ability_actions, nil
end

-- passive: scout_strike (Lyra)
-- After Lyra attacks, expose one face-down card directly adjacent to the
-- attacked target on the same battle line. Prefer the left neighbour so the
-- result is deterministic when both neighbours are eligible.
function scout_strike_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local defender = (event_data or {}).defender_card
    local defender_line_key = (event_data or {}).defender_line_key
    local defender_line = defender_line_key ~= nil and state[defender_line_key] or nil

    if defender == nil or defender_line == nil then
        battle.dlog("[ability] scout_strike: skip - target or target line is unavailable")
        return {}, nil
    end

    local defender_slot = defender.slot_index
    if defender_slot == nil then
        battle.dlog("[ability] scout_strike: skip - target has no slot_index")
        return {}, nil
    end

    local target = nil
    for _, adjacent_slot in ipairs({ defender_slot - 1, defender_slot + 1 }) do
        for _, line_card in ipairs(defender_line) do
            if line_card.inventory_item_id ~= nil and line_card.inventory_item_id ~= ""
                and line_card.slot_index == adjacent_slot
                and line_card.face_up ~= true
                and line_card.expose ~= true then
                target = line_card
                break
            end
        end
        if target ~= nil then break end
    end

    if target == nil then
        battle.dlog("[ability] scout_strike: no face-down adjacent card, skip")
        return {}, nil
    end

    target.face_up = true
    target.expose = true
    local target_side = helpers.find_card_side(state, target)
    local source_side = helpers.find_card_side(state, source_card)
    battle.dlog("[ability] scout_strike: exposed target=" .. target.inventory_item_id)
    return {
        target_side .. "_card_expose:" .. target.inventory_item_id,
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id .. ",ability=scout_strike,target=" .. target.inventory_item_id
    }, nil
end

-- passive: mist_execution (Misthy)
-- After Misthy defeats her attack target, move one Abyssal Mist from the
-- owning side's void into its first empty back-line slot. The primary attack
-- resolves before this handler runs, so the defeated target must already be in
-- the defender void recorded by the attack event.
function mist_execution_execute(state, source_card, event_data, helpers)
    local battle = helpers.lib_battle_common
    local defender_card = (event_data or {}).defender_card
    local defender_void_key = (event_data or {}).defender_side_void
    if defender_card == nil or defender_void_key == nil then
        battle.dlog("[ability] mist_execution: skip - attack target or target void is unavailable")
        return {}, nil
    end

    local target_was_defeated = false
    for _, void_card in ipairs(state[defender_void_key] or {}) do
        if void_card.inventory_item_id == defender_card.inventory_item_id then
            target_was_defeated = true
            break
        end
    end
    if not target_was_defeated then
        battle.dlog("[ability] mist_execution: skip - attack target survived")
        return {}, nil
    end

    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        battle.dlog("[ability] mist_execution: skip - Misthy is not on the battlefield")
        return {}, nil
    end

    local back_line_key = source_side .. "_back_line"
    local back_line = state[back_line_key] or {}
    state[back_line_key] = back_line
    local empty_slot_index = nil
    for index = 1, 5 do
        local line_card = back_line[index]
        if line_card ~= nil and line_card.item_definition_code_name == "abyssal_mist" then
            battle.dlog("[ability] mist_execution: skip - Abyssal Mist is already in back_line")
            return {}, nil
        end
        if empty_slot_index == nil and (line_card == nil or line_card.inventory_item_id == nil or line_card.inventory_item_id == "") then
            empty_slot_index = index
        end
    end
    if empty_slot_index == nil then
        battle.dlog("[ability] mist_execution: skip - back_line has no free slots")
        return {}, nil
    end

    local own_void_key = source_side .. "_the_void"
    local own_void = state[own_void_key] or {}
    state[own_void_key] = own_void
    local abyssal_mist_card = nil
    local abyssal_mist_index = nil
    for index, void_card in ipairs(own_void) do
        if void_card.item_definition_code_name == "abyssal_mist" then
            abyssal_mist_card = void_card
            abyssal_mist_index = index
            break
        end
    end
    if abyssal_mist_card == nil then
        battle.dlog("[ability] mist_execution: skip - no Abyssal Mist in " .. own_void_key)
        return {}, nil
    end

    table.remove(own_void, abyssal_mist_index)
    abyssal_mist_card.slot_index = empty_slot_index - 1
    abyssal_mist_card.trigger = false
    abyssal_mist_card.face_up = true
    abyssal_mist_card.expose = true
    abyssal_mist_card.defeated_from_line_key = nil
    local atk_added = tonumber(helpers.get_card_stat(state, abyssal_mist_card, "atk_added"))
    local def_added = tonumber(helpers.get_card_stat(state, abyssal_mist_card, "def_added"))
    if atk_added ~= nil and atk_added > 0 and def_added ~= nil and def_added > 0 then
        abyssal_mist_card.abyssal_mist_active = true
        abyssal_mist_card.abyssal_mist_atk_added = atk_added
        abyssal_mist_card.abyssal_mist_def_added = def_added
        abyssal_mist_card.abyssal_mist_misthy_id = source_card.inventory_item_id
        state.aura_refresh_requested = true
    else
        battle.dlog("[ability] mist_execution: Abyssal Mist has invalid aura stats")
    end
    back_line[empty_slot_index] = abyssal_mist_card

    battle.dlog("[ability] mist_execution: Misthy=" .. source_card.inventory_item_id ..
        " summoned Abyssal Mist=" .. abyssal_mist_card.inventory_item_id ..
        " to " .. back_line_key .. " slot=" .. abyssal_mist_card.slot_index)
    return {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=mist_execution,target=" .. abyssal_mist_card.inventory_item_id,
        source_side .. "_void_to_back_line:" .. abyssal_mist_card.inventory_item_id ..
            "," .. tostring(abyssal_mist_card.slot_index),
    }, nil
end
