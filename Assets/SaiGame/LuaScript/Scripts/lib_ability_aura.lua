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
        abyssal_mist = { "alpha_end_turn", "omega_end_turn", "aura_source_deployed" },
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
function refresh_active_auras(state, lifecycle_event, removed_sources)
    local all_actions = {}
    for _, ability_key in ipairs(get_aura_keys()) do
        if lifecycle_event == "aura_removed" or aura_refreshes_on_event(ability_key, lifecycle_event) then
            local refresh_handler = lib_ability_aura[ability_key .. "_refresh_aura"]
            if type(refresh_handler) == "function" then
                local removed_source = removed_sources ~= nil and removed_sources[ability_key] or nil
                local aura_actions = refresh_handler(state, lifecycle_event, removed_source)
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

-- Returns whether card is a configured Darkborn Aura. Consumers supply the
-- configured code-name set so each counter ability can extend its own list.
function is_configured_darkborn_aura(state, card, allowed_codes)
    if card == nil or type(allowed_codes) ~= "table" then return false end
    if allowed_codes[card.item_definition_code_name] ~= true then return false end
    local item_def = lib_battle_common.find_item_def(state.item_defs, card.item_definition_code_name)
    local metadata = item_def ~= nil and item_def.metadata or nil
    return metadata ~= nil and metadata.race == "darkborn" and metadata.type == "ability"
end

-- Collects every configured Darkborn Aura on the battlefield. Each result
-- retains the source line and owner so callers can move all matches safely.
function find_configured_darkborn_auras(state, allowed_codes, required_code)
    local matches = {}
    for _, line_data in ipairs(abyssal_mist_field_lines(state)) do
        for _, card in ipairs(line_data.line) do
            local matches_required_code = required_code == nil
                or card.item_definition_code_name == required_code
            if matches_required_code
                and is_configured_darkborn_aura(state, card, allowed_codes) then
                table.insert(matches, { card = card, line = line_data.line, side = line_data.side })
            end
        end
    end
    return matches
end

local function create_aura_effect_action(side, source_id, target_card, ability_key)
    return side .. "_card_aura:source=" .. source_id ..
        ",ability=" .. ability_key .. ",target=" .. target_card.inventory_item_id ..
        ",final_atk=" .. tostring(target_card.final_atk or 0) ..
        ",final_def=" .. tostring(target_card.final_def or 0)
end

local function create_aura_result_action(side, source_id, ability_key, checked_cards, eligible_cards, affected_cards)
    return side .. "_card_aura:source=" .. source_id .. ",ability=" .. ability_key ..
        ",checked_cards=" .. checked_cards .. ",eligible_cards=" .. eligible_cards ..
        ",affected_cards=" .. affected_cards
end

local function collect_abyssal_mist_sources(state)
    state.abyssal_mist_source_ids = state.abyssal_mist_source_ids or {}
    local sources = {
        ids = state.abyssal_mist_source_ids,
        active = {},
        by_id = {},
        side_by_id = {},
    }
    for _, line_data in ipairs(abyssal_mist_field_lines(state)) do
        for _, card in ipairs(line_data.line) do
            local source_id = card.inventory_item_id
            local is_mist = card.item_definition_code_name == "abyssal_mist"
            if source_id ~= nil and source_id ~= "" and is_mist then
                sources.ids[source_id] = true
                if card.abyssal_mist_active == true then
                    table.insert(sources.active, card)
                    sources.by_id[source_id] = card
                    sources.side_by_id[source_id] = line_data.side
                end
            end
        end
    end
    return sources
end

local function get_abyssal_mist_context(state, sources, removed_source)
    local primary_source = sources.by_id[state.abyssal_mist_primary_source_id]
    if primary_source == nil then
        primary_source = sources.active[1]
        state.abyssal_mist_primary_source_id = primary_source ~= nil
            and primary_source.inventory_item_id or nil
    end

    local primary_id = primary_source ~= nil and primary_source.inventory_item_id or nil
    local removed_id = removed_source ~= nil and removed_source.id or nil
    return {
        primary_id = primary_id,
        action_source_id = primary_id or removed_id,
        action_source_side = primary_id ~= nil and sources.side_by_id[primary_id]
            or (removed_source ~= nil and removed_source.side or nil),
        def_added = primary_source ~= nil
            and (tonumber(primary_source.abyssal_mist_def_added) or 0) or 0,
        atk_added = primary_source ~= nil
            and (tonumber(primary_source.abyssal_mist_atk_added) or 0) or 0,
    }
end

local function clear_persistent_bonus(card, bonus_key, source_ids)
    local bonuses = card[bonus_key]
    local total = 0
    if bonuses == nil then return total end
    for source_id, _ in pairs(source_ids) do
        total = total + (tonumber(bonuses[source_id]) or 0)
        bonuses[source_id] = nil
    end
    return total
end

local function apply_abyssal_mist_def_bonus(target_card, context, is_eligible, source_ids)
    local old_bonus = clear_persistent_bonus(target_card, "persistent_def_bonuses", source_ids)
    local new_bonus = is_eligible and context.def_added or 0
    if new_bonus > 0 then
        target_card.persistent_def_bonuses = target_card.persistent_def_bonuses or {}
        target_card.persistent_def_bonuses[context.primary_id] = new_bonus
    end
    if old_bonus == new_bonus then return false end
    target_card.final_def = math.max(0, (target_card.final_def or 0) - old_bonus + new_bonus)
    return true
end

local function apply_abyssal_mist_atk_bonus(state, target_card, context, source_ids)
    local old_bonus = clear_persistent_bonus(target_card, "persistent_atk_bonuses", source_ids)
    local is_misthy = target_card.item_definition_code_name == "misthy"
    local new_bonus = is_misthy and context.atk_added or 0
    if new_bonus > 0 then
        target_card.persistent_atk_bonuses = target_card.persistent_atk_bonuses or {}
        target_card.persistent_atk_bonuses[context.primary_id] = new_bonus
    end
    if old_bonus == new_bonus then return false end
    local item_def = lib_battle_common.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local base_atk = item_def ~= nil and tonumber((item_def.base_stats or {}).atk) or 0
    target_card.final_atk = math.max(0, (target_card.final_atk or base_atk) - old_bonus + new_bonus)
    return true
end

local function refresh_abyssal_mist_target(state, target_card, context, source_ids)
    local is_eligible = context.primary_id ~= nil and lib_battle_common.is_character_of_races(
        state.item_defs, target_card, { "darkborn", "natureborn" })
    local def_changed = apply_abyssal_mist_def_bonus(
        target_card, context, is_eligible, source_ids)
    local atk_changed = apply_abyssal_mist_atk_bonus(state, target_card, context, source_ids)
    return is_eligible, def_changed or atk_changed
end

-- Several active Mists may stay on the battlefield, but only the primary
-- source contributes. If it leaves, the next active Mist takes over without
-- increasing the bonus.
function abyssal_mist_refresh_aura(state, lifecycle_event, removed_source)
    local sources = collect_abyssal_mist_sources(state)
    local context = get_abyssal_mist_context(state, sources, removed_source)
    local actions = {}
    local checked_card_count = 0
    local eligible_card_count = 0
    local affected_card_count = 0

    for _, line_data in ipairs(abyssal_mist_field_lines(state)) do
        for _, target_card in ipairs(line_data.line) do
            checked_card_count = checked_card_count + 1
            local is_eligible, bonuses_changed = refresh_abyssal_mist_target(
                state, target_card, context, sources.ids)
            if is_eligible then eligible_card_count = eligible_card_count + 1 end
            if context.action_source_id ~= nil and bonuses_changed then
                table.insert(actions, create_aura_effect_action(
                    line_data.side, context.action_source_id, target_card, "abyssal_mist"))
                affected_card_count = affected_card_count + 1
            end
        end
    end
    if context.action_source_id ~= nil then
        table.insert(actions, create_aura_result_action(
            context.action_source_side, context.action_source_id, "abyssal_mist", checked_card_count,
            eligible_card_count, affected_card_count))
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
