-- lib_cheat (is_library = true)
-- Development-only battle cheats. They are enabled solely by game status.

function mark_alpha_next_draw(state, inventory_item_id)
    if not lib_battle_common.is_development() then return false end
    if inventory_item_id == nil or inventory_item_id == "" then return false end

    for _, card in ipairs(state.alpha_the_source or {}) do
        if card.inventory_item_id == inventory_item_id then
            state.alpha_next_draw_inventory_item_id = inventory_item_id
            return true
        end
    end
    return false
end

-- Returns the marked source index once, then clears the marker. A card that
-- has left the Source is not carried over to a later draw.
function consume_alpha_next_draw_index(state, source)
    if not lib_battle_common.is_development() then return nil end

    local inventory_item_id = state.alpha_next_draw_inventory_item_id
    if inventory_item_id == nil or inventory_item_id == "" then return nil end

    state.alpha_next_draw_inventory_item_id = nil
    for index, card in ipairs(source or {}) do
        if card.inventory_item_id == inventory_item_id then return index end
    end
    return nil
end
