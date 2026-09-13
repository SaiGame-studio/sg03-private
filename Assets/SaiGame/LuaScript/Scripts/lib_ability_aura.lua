-- lib_ability_aura
-- is_library = true
--
-- Owns lifecycle refresh, activation, stacking, and target rules for
-- persistent battlefield auras.

local function get_aura_keys()
    return { "abyssal_mist", "bloodmight" }
end

local function get_aura_refresh_events(ability_key)
    local events_by_aura = {
        abyssal_mist = { "alpha_end_turn", "omega_end_turn", "aura_source_deployed" },
        bloodmight = { "alpha_end_turn", "omega_end_turn", "aura_source_deployed" },
    }
    return events_by_aura[ability_key] or {}
end

local function aura_refreshes_on_event(ability_key, lifecycle_event)
    for _, allowed_event in ipairs(get_aura_refresh_events(ability_key)) do
        if allowed_event == lifecycle_event then return true end
    end
    return false
end

-- Reconciles every registered Aura for a shared lifecycle event. A removal
-- dispatches only the explicitly supplied source keys, never every Aura.
function refresh_active_auras(state, lifecycle_event, removed_sources)
    local all_actions = {}
    if lifecycle_event == "aura_removed" then
        for ability_key, removed_source in pairs(removed_sources or {}) do
            local aura_actions = refresh_removed_aura(state, ability_key, removed_source)
            for _, action in ipairs(aura_actions or {}) do
                table.insert(all_actions, action)
            end
        end
        return all_actions
    end
    for _, ability_key in ipairs(get_aura_keys()) do
        if aura_refreshes_on_event(ability_key, lifecycle_event) then
            local refresh_handler = lib_ability_aura[ability_key .. "_refresh_aura"]
            if type(refresh_handler) == "function" then
                local aura_actions = refresh_handler(state, lifecycle_event, nil)
                for _, action in ipairs(aura_actions or {}) do
                    table.insert(all_actions, action)
                end
            end
        end
    end
    return all_actions
end

-- A source-removal event has exactly one Aura owner. Route directly to that
-- Aura's reconciler instead of refreshing every registered Aura.
function refresh_removed_aura(state, ability_key, removed_source)
    if #get_aura_refresh_events(ability_key) == 0 then return {} end
    local refresh_handler = lib_ability_aura[ability_key .. "_refresh_aura"]
    if type(refresh_handler) ~= "function" then return {} end
    return refresh_handler(state, "aura_removed", removed_source)
end

-- All battlefield Aura scans use this one ordered view. The stable ordering
-- also makes primary-source selection deterministic.
local function get_battlefield_lines(state)
    return {
        { side = "alpha", line = state.alpha_front_line or {} },
        { side = "alpha", line = state.alpha_back_line or {} },
        { side = "omega", line = state.omega_front_line or {} },
        { side = "omega", line = state.omega_back_line or {} },
    }
end

local function collect_aura_sources(state, ability_key, active_flag_key)
    local source_ids_key = ability_key .. "_source_ids"
    state[source_ids_key] = state[source_ids_key] or {}
    local sources = { ids = state[source_ids_key], active = {}, by_id = {}, side_by_id = {} }
    for _, line_data in ipairs(get_battlefield_lines(state)) do
        for _, card in ipairs(line_data.line) do
            local source_id = card.inventory_item_id
            if source_id ~= nil and source_id ~= ""
                and card.item_definition_code_name == ability_key then
                sources.ids[source_id] = true
                if card[active_flag_key] == true then
                    table.insert(sources.active, card)
                    sources.by_id[source_id] = card
                    sources.side_by_id[source_id] = line_data.side
                end
            end
        end
    end
    return sources
end

local function get_aura_context(state, sources, removed_source, primary_state_key, stat_fields)
    local primary_source = sources.by_id[state[primary_state_key]]
    if primary_source == nil then
        primary_source = sources.active[1]
        state[primary_state_key] = primary_source ~= nil and primary_source.inventory_item_id or nil
    end
    local source_id = primary_source ~= nil and primary_source.inventory_item_id or nil
    local removed_id = removed_source ~= nil and removed_source.id or nil
    local context = {
        source_id = source_id,
        action_source_id = source_id or removed_id,
        source_side = source_id ~= nil and sources.side_by_id[source_id]
            or (removed_source ~= nil and removed_source.side or nil),
    }
    for context_key, source_field in pairs(stat_fields or {}) do
        context[context_key] = primary_source ~= nil
            and (tonumber(primary_source[source_field]) or 0) or 0
    end
    return context
end

-- Abyssal Mist remains on the battlefield only while its owner has a Misthy
-- in the front line. This is side-symmetric: callers supply Alpha or Omega.
function reconcile_abyssal_mist_frontline_requirement(state, side)
    if lib_battle_common.has_front_line_card_code(state, side, "misthy") then return {} end

    local mists = lib_battle_common.collect_side_cards_by_code(state, side, "abyssal_mist")
    if #mists == 0 then return {} end

    local void_key = side .. "_the_void"
    state[void_key] = state[void_key] or {}
    local actions = {}
    for _, match in ipairs(mists) do
        lib_battle_common.remove_card_from_line(match.line, match.card.inventory_item_id)
        table.insert(state[void_key], match.card)
        lib_battle_common.append_card_sent_to_void_action(actions, side, match.card)
    end

    local aura_actions = refresh_removed_aura(state, "abyssal_mist", {
        id = mists[1].card.inventory_item_id,
        side = side,
    })
    for _, action in ipairs(aura_actions) do table.insert(actions, action) end
    return actions
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
    for _, line_data in ipairs(get_battlefield_lines(state)) do
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

local function refresh_aura_targets(state, ability_key, context, source_ids, refresh_target)
    local actions = {}
    local checked_card_count = 0
    local eligible_card_count = 0
    local affected_card_count = 0
    for _, line_data in ipairs(get_battlefield_lines(state)) do
        for _, target_card in ipairs(line_data.line) do
            checked_card_count = checked_card_count + 1
            local is_eligible, bonus_changed = refresh_target(
                state, target_card, line_data.side, context, source_ids)
            if is_eligible then eligible_card_count = eligible_card_count + 1 end
            if bonus_changed and context.action_source_id ~= nil then
                table.insert(actions, create_aura_effect_action(
                    line_data.side, context.action_source_id, target_card, ability_key))
                affected_card_count = affected_card_count + 1
            end
        end
    end
    if context.action_source_id ~= nil then
        table.insert(actions, create_aura_result_action(
            context.source_side, context.action_source_id, ability_key, checked_card_count,
            eligible_card_count, affected_card_count))
    end
    return actions
end

-- Returns whether the specified side already has an active Abyssal Mist in
-- its back line. Ability activation and enemy planners use this to enforce
-- the one-active-Abyssal-Mist-per-side rule.
function has_active_abyssal_mist(state, side)
    return lib_battle_common.has_active_back_line_card_code(state, side, "abyssal_mist", "abyssal_mist_active")
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

local function apply_persistent_stat_bonus(state, target_card, stat_key, source_id, new_bonus, source_ids)
    local bonus_key = "persistent_" .. stat_key .. "_bonuses"
    local final_key = "final_" .. stat_key
    local old_bonus = clear_persistent_bonus(target_card, bonus_key, source_ids)
    if new_bonus > 0 then
        target_card[bonus_key] = target_card[bonus_key] or {}
        target_card[bonus_key][source_id] = new_bonus
    end
    if old_bonus == new_bonus then return false end
    local item_def = lib_battle_common.find_item_def(state.item_defs, target_card.item_definition_code_name)
    local base_stat = item_def ~= nil and tonumber((item_def.base_stats or {})[stat_key]) or 0
    target_card[final_key] = math.max(0, (target_card[final_key] or base_stat) - old_bonus + new_bonus)
    return true
end

local function refresh_abyssal_mist_target(state, target_card, target_side, context, source_ids)
    local is_eligible = context.source_id ~= nil and lib_battle_common.is_character_of_races(
        state.item_defs, target_card, { "darkborn", "natureborn" })
    local def_changed = apply_persistent_stat_bonus(state, target_card, "def", context.source_id,
        is_eligible and context.def_added or 0, source_ids)
    local atk_changed = apply_persistent_stat_bonus(state, target_card, "atk", context.source_id,
        target_card.item_definition_code_name == "misthy" and context.atk_added or 0, source_ids)
    return is_eligible, def_changed or atk_changed
end

-- Several active Mists may stay on the battlefield, but only the primary
-- source contributes. If it leaves, the next active Mist takes over without
-- increasing the bonus.
function abyssal_mist_refresh_aura(state, lifecycle_event, removed_source)
    local sources = collect_aura_sources(state, "abyssal_mist", "abyssal_mist_active")
    local context = get_aura_context(state, sources, removed_source, "abyssal_mist_primary_source_id", {
        atk_added = "abyssal_mist_atk_added",
        def_added = "abyssal_mist_def_added",
    })
    return refresh_aura_targets(state, "abyssal_mist", context, sources.ids,
        refresh_abyssal_mist_target)
end

function abyssal_mist_execute(state, source_card, event_data, helpers)
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "abyssal_mist source card is not on the battlefield"
    end
    local source_is_in_backline = false
    for _, card in ipairs(state[source_side .. "_back_line"] or {}) do
        if card.inventory_item_id == source_card.inventory_item_id then
            source_is_in_backline = true
            break
        end
    end
    if not source_is_in_backline then
        return {}, "abyssal_mist requires source card in own backline"
    end
    if source_card.abyssal_mist_active == true then
        return {}, "abyssal_mist is already active"
    end

    local misthy_card = event_data ~= nil and event_data.misthy_card or nil
    local defeated_enemy = event_data ~= nil and event_data.defeated_enemy or nil
    if misthy_card == nil or misthy_card.item_definition_code_name ~= "misthy"
        or defeated_enemy == nil or defeated_enemy.defeated_from_line_key == nil then
        return {}, "abyssal_mist requires misthy to defeat an enemy"
    end
    local atk_added = tonumber(helpers.get_card_stat(state, source_card, "atk_added"))
    local def_added = tonumber(helpers.get_card_stat(state, source_card, "def_added"))
    if atk_added == nil or atk_added <= 0 or def_added == nil or def_added <= 0 then
        return {}, "abyssal_mist requires positive base_stats.atk_added and base_stats.def_added"
    end

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
    table.insert(actions, source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
        ",ability=abyssal_mist,selected=" .. misthy_card.inventory_item_id)
    return actions, nil
end

local function refresh_bloodmight_target(state, target_card, target_side, context, source_ids)
    local is_eligible = context.source_id ~= nil and target_side == context.source_side
        and lib_battle_common.is_character_of_races(state.item_defs, target_card, { "darkborn" })
    local changed = apply_persistent_stat_bonus(state, target_card, "atk", context.source_id,
        is_eligible and context.atk_added or 0, source_ids)
    return is_eligible, changed
end

function bloodmight_refresh_aura(state, lifecycle_event, removed_source)
    local sources = collect_aura_sources(state, "bloodmight", "bloodmight_active")
    local context = get_aura_context(state, sources, removed_source, "bloodmight_primary_source_id", {
        atk_added = "bloodmight_atk_added",
    })
    return refresh_aura_targets(state, "bloodmight", context, sources.ids,
        refresh_bloodmight_target)
end

-- Bloodmight consumes exactly three allied Bone Spires from Sythra's front
-- line, then persists as an Aura that affects allied Darkborn only.
function bloodmight_execute(state, source_card, event_data, helpers)
    local source_side = helpers.find_card_side(state, source_card)
    if source_side == nil or source_side == "unknown" then
        return {}, "bloodmight source card is not on the battlefield"
    end
    local source_is_in_backline = false
    for _, card in ipairs(state[source_side .. "_back_line"] or {}) do
        if card.inventory_item_id == source_card.inventory_item_id then
            source_is_in_backline = true
            break
        end
    end
    if not source_is_in_backline then
        return {}, "bloodmight requires source card in own backline"
    end
    if source_card.bloodmight_active == true then
        return {}, "bloodmight is already active"
    end

    local front_line = state[source_side .. "_front_line"] or {}
    local sythra_card = lib_battle_common.find_card_in_line_by_code(front_line, "sythra")
    if sythra_card == nil then
        return {}, "bloodmight requires sythra in own front_line"
    end
    local bone_spires = {}
    for _, card in ipairs(front_line) do
        if card.item_definition_code_name == "bone_spire" then
            table.insert(bone_spires, card)
        end
    end
    if #bone_spires < 3 then
        return {}, "bloodmight requires 3 bone_spire in own front_line"
    end
    local atk_added = tonumber(helpers.get_card_stat(state, source_card, "atk_added"))
    if atk_added == nil or atk_added <= 0 then
        return {}, "bloodmight requires positive base_stats.atk_added"
    end

    source_card.bloodmight_active = true
    source_card.bloodmight_atk_added = atk_added
    state.bloodmight_source_ids = state.bloodmight_source_ids or {}
    state.bloodmight_source_ids[source_card.inventory_item_id] = true

    local actions = {
        source_side .. "_card_ability:source=" .. source_card.inventory_item_id ..
            ",ability=bloodmight,selected=" .. sythra_card.inventory_item_id .. ",bone_spire_count=3",
    }
    local void_key = source_side .. "_the_void"
    state[void_key] = state[void_key] or {}
    for index = 1, 3 do
        local bone_spire = bone_spires[index]
        lib_battle_common.remove_card_from_line(front_line, bone_spire.inventory_item_id)
        table.insert(state[void_key], bone_spire)
        lib_battle_common.append_card_sent_to_void_action(actions, source_side, bone_spire)
    end
    local aura_actions = bloodmight_refresh_aura(state)
    for _, action in ipairs(aura_actions) do table.insert(actions, action) end
    return actions, nil
end
