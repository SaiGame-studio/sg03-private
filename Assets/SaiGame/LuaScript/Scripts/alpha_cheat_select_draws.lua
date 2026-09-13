require "lib_battle_common"

local function validate_payload()
    if type(payload.inventory_item_ids) ~= "table" then
        return "alpha cheat draw selection is invalid"
    end
    if #payload.inventory_item_ids > 2 then
        return "alpha cheat draw selection is invalid"
    end
    return nil
end

local function save_alpha_next_draws(state, inventory_item_ids)
    local source_ids = {}
    for _, card in ipairs(state.alpha_the_source or {}) do
        local inventory_item_id = card.inventory_item_id
        if inventory_item_id ~= nil and inventory_item_id ~= "" then
            source_ids[inventory_item_id] = true
        end
    end

    local selected_ids = {}
    for _, inventory_item_id in ipairs(inventory_item_ids) do
        if type(inventory_item_id) ~= "string" or inventory_item_id == "" then return false end
        if source_ids[inventory_item_id] ~= true or selected_ids[inventory_item_id] == true then
            return false
        end
        selected_ids[inventory_item_id] = true
    end

    state.alpha_draw_priority_ids = inventory_item_ids
    return true
end

local function main()
    if not lib_battle_common.is_development() then
        output.error = "alpha cheat is only available in development"
        return
    end

    local payload_err = validate_payload()
    if payload_err ~= nil then output.error = payload_err ; return end

    local session_id, session_err = lib_battle_common.resolve_session_id()
    if session_err ~= nil then output.error = session_err ; return end

    local state, state_err = lib_battle_common.load_session(session_id)
    if state_err ~= nil then output.error = state_err ; return end
    if state.status == "completed" then output.error = "battle is already completed" ; return end

    if not save_alpha_next_draws(state, payload.inventory_item_ids) then
        output.error = "alpha cheat draw selection is invalid"
        return
    end

    local save_err = game.battle_session_update(session_id, state)
    if save_err ~= nil then output.error = save_err ; return end

    output.alpha_next_draw_count = #payload.inventory_item_ids
end

main()
