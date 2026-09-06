-- lib_ability_aura
-- is_library = true
--
-- Owns lifecycle refresh, activation, stacking, and target rules for
-- persistent battlefield auras.

local function get_aura_keys()
    return { "abyssal_mist" }
end

local function get_aura_refresh_events(ability_key)
    local events_by_aura = {
        abyssal_mist = { "alpha_deploy", "alpha_end_turn", "omega_end_turn" },
    }
    return events_by_aura[ability_key] or {}
end

local function aura_refreshes_on_event(ability_key, lifecycle_event)
    for _, allowed_event in ipairs(get_aura_refresh_events(ability_key)) do
        if allowed_event == lifecycle_event then return true end
    end
    return false
end

-- Reconciles every registered aura for one lifecycle event. Callers do not
-- know aura keys or their stacking behavior.
function refresh_active_auras(state, lifecycle_event)
    local all_actions = {}
    for _, ability_key in ipairs(get_aura_keys()) do
        if aura_refreshes_on_event(ability_key, lifecycle_event) then
            local refresh_handler = lib_ability_aura[ability_key .. "_refresh_aura"]
            if type(refresh_handler) == "function" then
                local aura_actions = refresh_handler(state, lifecycle_event)
                for _, action in ipairs(aura_actions or {}) do
                    table.insert(all_actions, action)
                end
            end
        end
    end
    return all_actions
end

local function abyssal_mist_field_lines(state)
    return {
        { side = "alpha", line = state.alpha_front_line or {} },
        { side = "alpha", line = state.alpha_back_line or {} },
        { side = "omega", line = state.omega_front_line or {} },
        { side = "omega", line = state.omega_back_line or {} },
    }
end

local function create_aura_effect_action(side, source_id, target_id, ability_key)
    return side .. "_card_aura:source=" .. source_id ..
        ",ability=" .. ability_key .. ",target=" .. target_id
end

-- Several active Mists may stay on the battlefield, but only the primary
-- source contributes. If it leaves, the next active Mist takes over without
-- increasing the bonus.
function abyssal_mist_refresh_aura(state, lifecycle_event)
    state.abyssal_mist_source_ids = state.abyssal_mist_source_ids or {}
    local source_ids = state.abyssal_mist_source_ids
    local active_sources = {}
    local source_by_id = {}
    for _, line_data in ipairs(abyssal_mist_field_lines(state)) do
        for _, card in ipairs(line_data.line) do
            local has_id = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            if has_id and card.item_definition_code_name == "abyssal_mist" then
                source_ids[card.inventory_item_id] = true
                if card.abyssal_mist_active == true then
                    table.insert(active_sources, card)
                    source_by_id[card.inventory_item_id] = card
                end
            end
        end
    end

    local primary_source = source_by_id[state.abyssal_mist_primary_source_id]
    if primary_source == nil then
        primary_source = active_sources[1]
        state.abyssal_mist_primary_source_id = primary_source ~= nil and primary_source.inventory_item_id or nil
    end

    local primary_id = primary_source ~= nil and primary_source.inventory_item_id or nil
    local def_added = primary_source ~= nil and (tonumber(primary_source.abyssal_mist_def_added) or 0) or 0
    local atk_added = primary_source ~= nil and (tonumber(primary_source.abyssal_mist_atk_added) or 0) or 0
    local misthy_id = primary_source ~= nil and primary_source.abyssal_mist_misthy_id or nil
    local actions = {}

    for _, line_data in ipairs(abyssal_mist_field_lines(state)) do
        for _, target_card in ipairs(line_data.line) do
            local old_def_bonus = 0
            if target_card.persistent_def_bonuses ~= nil then
                for source_id, _ in pairs(source_ids) do
                    old_def_bonus = old_def_bonus + (tonumber(target_card.persistent_def_bonuses[source_id]) or 0)
                    target_card.persistent_def_bonuses[source_id] = nil
                end
            end

            local new_def_bonus = 0
            if primary_id ~= nil and lib_battle_common.is_character_of_races(
                state.item_defs, target_card, { "darkborn", "natureborn" }) then
                target_card.persistent_def_bonuses = target_card.persistent_def_bonuses or {}
                target_card.persistent_def_bonuses[primary_id] = def_added
                new_def_bonus = def_added
            end
            local def_bonus_changed = old_def_bonus ~= new_def_bonus
            if def_bonus_changed then
                target_card.final_def = math.max(0, (target_card.final_def or 0) - old_def_bonus + new_def_bonus)
            end

            local old_atk_bonus = 0
            if target_card.persistent_atk_bonuses ~= nil then
                for source_id, _ in pairs(source_ids) do
                    old_atk_bonus = old_atk_bonus + (tonumber(target_card.persistent_atk_bonuses[source_id]) or 0)
                    target_card.persistent_atk_bonuses[source_id] = nil
                end
            end
            local new_atk_bonus = target_card.inventory_item_id == misthy_id and atk_added or 0
            if new_atk_bonus > 0 then
                target_card.persistent_atk_bonuses = target_card.persistent_atk_bonuses or {}
                target_card.persistent_atk_bonuses[primary_id] = new_atk_bonus
            end
            local atk_bonus_changed = old_atk_bonus ~= new_atk_bonus
            if primary_id ~= nil and (def_bonus_changed or atk_bonus_changed) then
                table.insert(actions, create_aura_effect_action(
                    line_data.side, primary_id, target_card.inventory_item_id, "abyssal_mist"))
            end
        end
    end
    return actions
end

function abyssal_mist_execute(state, source_card, event_data, helpers)
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "abyssal_mist source card is not on the battlefield"
    end
    if source_card.abyssal_mist_active == true then
        return {}, "abyssal_mist is already active"
    end

    local is_misthy = function(card)
        return card.item_definition_code_name == "misthy"
    end
    local misthy_card = helpers.find_untriggered_card(state[source_side .. "_front_line"], is_misthy)
    if misthy_card == nil then
        misthy_card = helpers.find_untriggered_card(state[source_side .. "_back_line"], is_misthy)
    end
    if misthy_card == nil then
        return {}, "abyssal_mist requires untriggered misthy on the battlefield"
    end
    local atk_added = tonumber(helpers.get_card_stat(state, source_card, "atk_added"))
    local def_added = tonumber(helpers.get_card_stat(state, source_card, "def_added"))
    if atk_added == nil or atk_added <= 0 or def_added == nil or def_added <= 0 then
        return {}, "abyssal_mist requires positive base_stats.atk_added and base_stats.def_added"
    end

    local expose_action = helpers.expose_ability_selected_card(state, misthy_card)
    misthy_card.trigger = true
    source_card.abyssal_mist_active = true
    source_card.abyssal_mist_atk_added = atk_added
    source_card.abyssal_mist_def_added = def_added
    source_card.abyssal_mist_misthy_id = misthy_card.inventory_item_id
    state.abyssal_mist_source_ids = state.abyssal_mist_source_ids or {}
    state.abyssal_mist_source_ids[source_card.inventory_item_id] = true
    if state.abyssal_mist_primary_source_id == nil then
        state.abyssal_mist_primary_source_id = source_card.inventory_item_id
    end

    local actions = abyssal_mist_refresh_aura(state)
    if expose_action ~= nil then table.insert(actions, 1, expose_action) end
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
        ",ability=abyssal_mist,selected=" .. misthy_card.inventory_item_id)
    return actions, nil
end
