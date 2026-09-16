require "lib_battle_common"
require "lib_ability_aura"

local function validate_payload()
    if type(payload.source_inventory_item_id) ~= "string" or payload.source_inventory_item_id == "" then
        return "missing source_inventory_item_id"
    end
    if type(payload.target_inventory_item_id) ~= "string" or payload.target_inventory_item_id == "" then
        return "missing target_inventory_item_id"
    end
    return nil
end

local function find_seppuku_source(state, inventory_item_id)
    local source_card = lib_battle_common.find_card_in_line_by_id(state.alpha_back_line, inventory_item_id)
    if source_card == nil then return nil, "seppuku source must be deployed on alpha backline" end
    if source_card.item_definition_code_name ~= "seppuku" then return nil, "seppuku source must be the seppuku card" end
    return source_card, nil
end

local function build_battle_lines(state)
    return {
        { cards = state.alpha_front_line, side = "alpha" },
        { cards = state.alpha_back_line, side = "alpha" },
        { cards = state.omega_front_line, side = "omega" },
        { cards = state.omega_back_line, side = "omega" },
    }
end

local function find_target_character(state, inventory_item_id)
    for _, entry in ipairs(build_battle_lines(state)) do
        for _, card in ipairs(entry.cards or {}) do
            if card.inventory_item_id == inventory_item_id then
                return card, entry.cards, entry.side
            end
        end
    end
    return nil, nil, nil
end

local function move_target_to_owner_void(state, target_card, target_line, target_side)
    if not lib_battle_common.check_card_type(state.item_defs, target_card, "character") then
        return "seppuku target must be a character"
    end

    local removed = lib_battle_common.remove_card_from_line(target_line, target_card.inventory_item_id)
    if not removed then return "seppuku target character not found in battle lines" end

    local void_key = target_side .. "_the_void"
    if state[void_key] == nil then state[void_key] = {} end
    table.insert(state[void_key], target_card)
    lib_battle_common.append_card_sent_to_void_client_action(state, target_side, target_card)
    local aura_actions = lib_ability_aura.reconcile_frontline_aura_requirements(state, target_side)
    for _, action in ipairs(aura_actions) do
        lib_battle_common.append_client_action(state, action)
    end
    return nil
end

local function main()
    if not lib_battle_common.is_development() then
        output.error = "seppuku cheat is only available in development"
        return
    end

    local payload_err = validate_payload()
    if payload_err ~= nil then output.error = payload_err ; return end

    local session_id, session_err = lib_battle_common.resolve_session_id()
    if session_err ~= nil then output.error = session_err ; return end

    local state, state_err = lib_battle_common.load_session(session_id)
    if state_err ~= nil then output.error = state_err ; return end
    if state.status == "completed" then output.error = "battle is already completed" ; return end

    local source_card, source_err = find_seppuku_source(state, payload.source_inventory_item_id)
    if source_err ~= nil then output.error = source_err ; return end

    local target_card, target_line, target_side = find_target_character(state, payload.target_inventory_item_id)
    if target_card == nil then
        output.error = "seppuku target character not found in battle lines"
        return
    end

    local move_err = move_target_to_owner_void(state, target_card, target_line, target_side)
    if move_err ~= nil then output.error = move_err ; return end

    state.action = (state.action or 0) + 1
    state.updated_at = ctx.timestamp
    local save_err = game.battle_session_update(session_id, state)
    if save_err ~= nil then output.error = save_err ; return end

    lib_battle_common.battle_status()
    output.source_inventory_item_id = source_card.inventory_item_id
    output.target_inventory_item_id = target_card.inventory_item_id
    output.target_side = target_side
end

main()
