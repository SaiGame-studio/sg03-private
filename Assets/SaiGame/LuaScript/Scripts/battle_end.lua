-- Usage: create or update this file as a backend Lua script, then run it through the script API.
-- Endpoint: POST /api/v1/games/{game_id}/scripts/{script_name}/run
-- Headers:
--   Authorization: Bearer {access_token}
--   Content-Type: application/json
-- Example request body:
-- {
--   "payload": {
--     "session_id": "battle-session-uuid"  (optional, defaults to current active session)
--   }
-- }

local validate_payload   -- forward declaration
local resolve_session_id -- forward declaration
local load_session       -- forward declaration
local determine_winner -- forward declaration
local end_session      -- forward declaration
local open_drop_packs  -- forward declaration

-- Add granted items to the reward list, combining matching item definitions.
local function add_granted_items(item_list, item_map, items)
    if items == nil then return end

    for _, item in ipairs(items) do
        local def_id = item.item_definition_id
        if def_id then
            if item_map[def_id] then
                item_map[def_id].quantity = item_map[def_id].quantity + (item.quantity or 1)
            else
                local entry = {
                    definition_id = def_id,
                    name = item.name or "Unknown item",
                    category = item.category,
                    quantity = item.quantity or 1
                }
                table.insert(item_list, entry)
                item_map[def_id] = entry
            end
        end
    end
end

-- Flatten successful entity drop pack results into a reward list.
local function process_drops(pack_results, item_list, item_map)
    item_list = item_list or {}
    item_map = item_map or {}

    for _, pack in ipairs(pack_results) do
        if pack.success and pack.items then
            add_granted_items(item_list, item_map, pack.items)
        end
    end
    return item_list, item_map
end

local function is_main_inventory_capacity_error(err)
    return string.find(tostring(err), "maximum owned item quantity reached", 1, true) ~= nil
end

local function main()
    local err = validate_payload()
    if err ~= nil then output.error = err ; return end

    local session_id, session_err = resolve_session_id()
    if session_err ~= nil then output.error = session_err ; return end

    local state, load_err = load_session(session_id)
    if load_err ~= nil then output.error = load_err ; return end

    local winner, alpha_hp, omega_hp = determine_winner(state)

    local end_err = end_session(session_id, state, winner)
    if end_err ~= nil then output.error = end_err ; return end

    output.session_id = session_id
    output.status     = "ended"
    output.winner     = winner
    output.turn       = state.turn
    output.alpha_hp   = alpha_hp
    output.omega_hp   = omega_hp

    -- Log battle completion
    game.log("Battle ended. Winner: " .. winner .. ", Turn: " .. tostring(state.turn or 1) .. ", Alpha HP: " .. tostring(alpha_hp) .. ", Omega HP: " .. tostring(omega_hp))

    if winner == "alpha" then
        local drops, drop_err = open_drop_packs(session_id, state)
        if drop_err ~= nil then output.error = drop_err ; return end

        -- Return only rewards declared by the defeated enemy's drop_pack_ids.
        local flat_drops = process_drops(drops)
        output.drops = flat_drops

        -- Log drops/rewards
        if flat_drops ~= nil and #flat_drops > 0 then
            for _, drop in ipairs(flat_drops) do
                game.log("Reward obtained: " .. tostring(drop.name) .. " x" .. tostring(drop.quantity))
            end
        else
            game.log("No rewards obtained.")
        end
    end
end

-- ─── Functions ───────────────────────────────────────────────────────────────

validate_payload = function()
    return nil
end

resolve_session_id = function()
    if payload.session_id ~= nil and payload.session_id ~= "" then
        return payload.session_id, nil
    end
    local session_id, err = game.battle_session_current_id()
    if err ~= nil then return nil, err end
    if session_id == nil or session_id == "" then return nil, "current battle session not found" end
    return session_id, nil
end

load_session = function(session_id)
    local state, err = game.battle_session_get(session_id)
    if err ~= nil then return nil, err end
    if state == nil then return nil, "battle session not found" end
    return state, nil
end

determine_winner = function(state)
    local alpha_hp = state.alpha_hp or 0
    local omega_hp = state.omega_hp or 0
    local winner
    if alpha_hp > omega_hp then
        winner = "alpha"
    else
        winner = "omega"
    end
    return winner, alpha_hp, omega_hp
end

end_session = function(session_id, state, winner)
    local end_data = {
        winner   = winner,
        reason   = "completed",
        turn     = state.turn or 1,
        ended_at = ctx.timestamp,
    }
    return game.battle_session_end(session_id, end_data)
end

open_drop_packs = function(session_id, state)
    local enemy = state.metadata and state.metadata.omega
    if enemy == nil then return {}, nil end

    local pack_ids = enemy.metadata and enemy.metadata.drop_pack_ids
    if pack_ids == nil or #pack_ids == 0 then return {}, nil end

    local drops = {}
    for _, pack_id in ipairs(pack_ids) do
        local pack, pack_err = game.get_gacha_pack_by_id(pack_id)
        if pack_err ~= nil then
            return nil, "gacha pack not found (id: " .. tostring(pack_id) .. "): " .. tostring(pack_err)
        end

        local pack_code = pack.code_name or "unknown"
        local result, err = game.open_reward_gacha_pack(
            pack_id,
            "battle-entity-drop:" .. session_id .. ":" .. pack_id
        )
        if err ~= nil then
            if is_main_inventory_capacity_error(err) then
                game.log("Skipped battle reward pack '" .. tostring(pack.name or pack_code) .. "' because the main inventory is full.")
            else
                return nil, "failed to open gacha pack '" .. tostring(pack.name or pack_code) .. "' (code: " .. tostring(pack_code) .. ", id: " .. tostring(pack_id) .. "): " .. tostring(err)
            end
        end

        if result ~= nil then
            drops[#drops + 1] = {
                pack_id = pack_id,
                success = true,
                items = result.items or {},
            }
        end
    end
    return drops, nil
end

main()
