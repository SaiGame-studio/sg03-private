-- enemy_ai_silas  (is_library = true)
-- AI module for the Silas normal enemy.

local function is_empty_slot(card)
    return card == nil or card.inventory_item_id == nil or card.inventory_item_id == ""
end

local function find_empty_slot(line, slot_count)
    for slot_i = 1, slot_count do
        if is_empty_slot(line[slot_i]) then return slot_i end
    end
    return nil
end

local function count_empty_slots(line, slot_count)
    local count = 0
    for slot_i = 1, slot_count do
        if is_empty_slot(line[slot_i]) then
            count = count + 1
        end
    end
    return count
end

local function find_adjacent_empty_slots(line, slot_count, required_count)
    required_count = required_count or 2
    for slot_i = 1, slot_count - required_count + 1 do
        local all_empty = true
        for offset = 0, required_count - 1 do
            if not is_empty_slot(line[slot_i + offset]) then
                all_empty = false
                break
            end
        end
        if all_empty then return slot_i end
    end
    return nil
end

local function find_unreserved_empty_slot(line, slot_count, reserve_start, reserve_count)
    reserve_count = reserve_count or 2
    for slot_i = 1, slot_count do
        local is_reserved_slot = reserve_start ~= nil
            and slot_i >= reserve_start
            and slot_i < reserve_start + reserve_count
        if not is_reserved_slot and is_empty_slot(line[slot_i]) then
            return slot_i
        end
    end
    return nil
end

local function find_card_by_code(cards, code_name, excluded_id)
    for _, card in ipairs(cards or {}) do
        if card.item_definition_code_name == code_name and card.inventory_item_id ~= excluded_id then
            return card
        end
    end
    return nil
end

local function find_card_in_zone_by_code(state, zone_key, code_name)
    return find_card_by_code(state[zone_key] or {}, code_name, nil)
end

local function find_line_card_by_code_prefer_exposed(line, code_name)
    local unexposed_fallback = nil
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (line or {})[index]
        if card ~= nil and card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == code_name then
            if card.expose == true then return card end
            if unexposed_fallback == nil then unexposed_fallback = card end
        end
    end
    return unexposed_fallback
end

local function find_untriggered_line_card_by_code(line, code_name)
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (line or {})[index]
        if card ~= nil and card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
            and card.item_definition_code_name == code_name
            and card.trigger ~= true then
            return card
        end
    end
    return nil
end

local function deploy_card(line, slot_i, card, face_up, deployed_cards)
    card.slot_index = slot_i - 1
    card.face_up = face_up
    card.expose = face_up
    line[slot_i] = card
    table.insert(deployed_cards, card)
end

local function is_omega_front_line_taking_damage(state)
    local pending_attack = state.pending_attack
    if pending_attack == nil or (pending_attack.damage_dealt or 0) <= 0 then return false end
    local defender_id = pending_attack.defender_inventory_item_id or ""
    for _, card in ipairs(state.omega_front_line or {}) do
        if card.inventory_item_id == defender_id then return true end
    end
    return false
end

-- Defend reaction: trigger Totem Pulse from back_line when front_line takes damage.
function defend(state)
    lib_battle_common.dlog("[entity_ai] == silas.defend ==")
    if not is_omega_front_line_taking_damage(state) then return nil end

    local source_card = find_line_card_by_code_prefer_exposed(state.omega_back_line, "totem_pulse")
    if source_card == nil then return nil end
    if find_untriggered_line_card_by_code(state.omega_front_line, "goblin_shaman") == nil then return nil end

    local actions, ability_err = lib_ability_core.trigger_ability_by_key(
        state, source_card, "totem_pulse", "on_defend", {
            pending_attack = state.pending_attack,
        }
    )
    if ability_err ~= nil then return ability_err end
    for _, action in ipairs(actions or {}) do
        lib_battle_common.append_client_action(state, action)
    end
    return nil
end

-- Deploy Totem Pulse immediately, reserve one Shaman plus one Brute Call, and
-- retain two adjacent front-line slots until the combo is resolved.
function deploy(state)
    lib_battle_common.dlog("[entity_ai] == silas.deploy ==")

    local slot_count = lib_battle_common.get_hand_size()
    local front_line = state.omega_front_line or {}
    local back_line = state.omega_back_line or {}
    local hand = state.omega_hand or {}
    local hand_cards = lib_battle_ai._collect_cards(hand)
    local shaman_card = find_card_by_code(hand_cards, "goblin_shaman", nil)
    local brute_call_card = find_card_by_code(hand_cards, "brute_call", nil)
    local deployed_ids = {}
    local front_deployed = {}
    local back_deployed = {}

    -- Totem Pulse is deployed as soon as it is drawn, reserving 1 back slot for Brute Call if held.
    local min_back_slots = brute_call_card ~= nil and 2 or 1
    for _, card in ipairs(hand_cards) do
        if card.item_definition_code_name == "totem_pulse" then
            if count_empty_slots(back_line, slot_count) >= min_back_slots then
                local slot_i = find_empty_slot(back_line, slot_count)
                if slot_i ~= nil then
                    deploy_card(back_line, slot_i, card, false, back_deployed)
                    table.insert(deployed_ids, card.id)
                end
            end
        end
    end

    local reserve_left = find_adjacent_empty_slots(front_line, slot_count, 2)
    local can_combo = tonumber(state.turn or 0) >= 4
        and shaman_card ~= nil
        and brute_call_card ~= nil
        and reserve_left ~= nil
        and find_empty_slot(back_line, slot_count) ~= nil
        and find_card_in_zone_by_code(state, "omega_the_void", "goblin_brute") ~= nil

    if can_combo then
        deploy_card(front_line, reserve_left, shaman_card, false, front_deployed)
        table.insert(deployed_ids, shaman_card.id)

        local brute_call_slot = find_empty_slot(back_line, slot_count)
        deploy_card(back_line, brute_call_slot, brute_call_card, false, back_deployed)
        table.insert(deployed_ids, brute_call_card.id)

        local new_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
        lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
        lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)

        -- Use the standard player Ability pipeline. Brute Call owns all summon effects.
        local event_data = {
            defender_card = shaman_card,
            defender_line_key = "omega_front_line",
            damage_dealt = 0,
        }
        local actions, ability_err = lib_ability_core.trigger_ability_by_key(
            state, brute_call_card, "brute_call", "on_attack", event_data
        )
        if ability_err ~= nil then return front_line, back_line, new_hand, ability_err end
        for _, action in ipairs(actions or {}) do
            lib_battle_common.append_client_action(state, action)
        end
        return front_line, back_line, new_hand, nil
    end

    -- Before the combo, leave two adjacent front slots empty. Do not deploy the
    -- reserved Shaman, Brute Call, or Goblin Brute through normal deployment.
    if reserve_left ~= nil then
        local reserved_shaman_id = shaman_card ~= nil and shaman_card.inventory_item_id or nil
        local reserved_call_id = brute_call_card ~= nil and brute_call_card.inventory_item_id or nil
        local character_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
        for _, card in ipairs(character_cards) do
            local is_reserved = card.inventory_item_id == reserved_shaman_id
                or card.inventory_item_id == reserved_call_id
                or card.item_definition_code_name == "goblin_brute"
            if not is_reserved then
                local slot_i = find_unreserved_empty_slot(front_line, slot_count, reserve_left, 2)
                if slot_i ~= nil then
                    deploy_card(front_line, slot_i, card, false, front_deployed)
                    table.insert(deployed_ids, card.id)
                end
                break
            end
        end
    end

    -- This explicit combo rule reserves front-line capacity, so its remaining
    -- Characters are excluded from the shared hand-capacity deployment.
    local excluded_ids = {}
    if reserve_left ~= nil then
        local character_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
        for _, card in ipairs(character_cards) do
            excluded_ids[card.id] = true
        end
    end
    lib_battle_ai.ensure_omega_hand_draw_capacity(
        state, front_line, back_line, hand, deployed_ids, front_deployed, back_deployed, excluded_ids
    )

    local new_hand = lib_battle_ai._rebuild_hand(hand, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)
    return front_line, back_line, new_hand, nil
end

local function get_omega_character_attack_damage(state, card)
    if card == nil then return 0 end
    local item_def = lib_battle_ai._find_item_def(state.item_defs, card.item_definition_code_name)
    return lib_battle_common.get_attack_damage(state, item_def, "omega_front_line", card)
end

local function is_eligible_omega_attacker(state, card)
    if card == nil or card.inventory_item_id == nil or card.inventory_item_id == "" then return false end
    if card.trigger == true then return false end
    if not lib_battle_common.check_card_type(state.item_defs, card, "character") then return false end
    return get_omega_character_attack_damage(state, card) > 1
end

-- Finds an eligible attacker on omega_front_line.
-- Both face-up and face-down characters are eligible; attacking reveals face-down characters.
local function find_omega_attacker(state)
    -- Prefer already face-up attacker first if one exists
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (state.omega_front_line or {})[index]
        if is_eligible_omega_attacker(state, card) and card.face_up == true then
            return card
        end
    end
    -- Fall back to any eligible attacker (including face-down)
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (state.omega_front_line or {})[index]
        if is_eligible_omega_attacker(state, card) then
            return card
        end
    end
    return nil
end

local function pick_alpha_face_up_front_line_character_target(state)
    local selected_card = nil
    local lowest_remaining_def = math.huge
    for _, card in ipairs(state.alpha_front_line or {}) do
        local has_card = card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_card and card.face_up == true
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            local remaining_def = (card.final_def or 0) - (card.total_damage_received or 0)
            if remaining_def < lowest_remaining_def then
                selected_card = card
                lowest_remaining_def = remaining_def
            end
        end
    end
    return selected_card
end

local function pick_alpha_face_down_front_line_character_target(state)
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (state.alpha_front_line or {})[index]
        local has_card = card ~= nil and card.inventory_item_id ~= nil and card.inventory_item_id ~= ""
        if has_card and card.face_up ~= true
            and lib_battle_common.check_card_type(state.item_defs, card, "character") then
            return card
        end
    end
    return nil
end

local function get_total_eligible_omega_attack_damage(state)
    local total_damage = 0
    for index = 1, lib_battle_common.get_hand_size() do
        local card = (state.omega_front_line or {})[index]
        if is_eligible_omega_attacker(state, card) then
            total_damage = total_damage + get_omega_character_attack_damage(state, card)
        end
    end
    return total_damage
end

-- Prefer the weakest face-up Alpha Character while the available Omega damage
-- can defeat it. Otherwise, reveal a face-down Alpha Character if possible.
-- If Alpha has no face-down Character, attack the weakest face-up target anyway.
local function pick_alpha_front_line_character_target(state)
    local face_up_target = pick_alpha_face_up_front_line_character_target(state)
    if face_up_target == nil then
        return pick_alpha_face_down_front_line_character_target(state)
    end

    local remaining_def = (face_up_target.final_def or 0) - (face_up_target.total_damage_received or 0)
    local total_damage = get_total_eligible_omega_attack_damage(state)
    if total_damage >= remaining_def then
        return face_up_target
    end

    local face_down_target = pick_alpha_face_down_front_line_character_target(state)
    if face_down_target ~= nil then
        lib_battle_common.dlog("[entity_ai] silas target fallback: face-up remaining_def=" ..
            tostring(remaining_def) .. " exceeds total_damage=" .. tostring(total_damage) ..
            "; choosing face-down target=" .. face_down_target.inventory_item_id)
        return face_down_target
    end

    return face_up_target
end

local function append_omega_attack_plan(state, attacker, defender)
    if attacker == nil then return end
    state.omega_planning = state.omega_planning or {}
    local defender_id = defender ~= nil and defender.inventory_item_id or "alpha_hp"
    table.insert(state.omega_planning, {
        action = defender ~= nil and "card_attack_card" or "omega_attack_alpha_hp",
        attacker_inv_id = attacker.inventory_item_id,
        defender_inv_id = defender_id,
    })
    lib_battle_common.append_client_action(
        state,
        lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, defender_id)
    )
end

-- Plan Omega Character attack against Alpha front line, or Alpha HP if front line has no Character.
function plan_attack(state)
    lib_battle_common.dlog("[entity_ai] == silas.plan_attack ==")
    state.omega_planning = {}

    local attacker = find_omega_attacker(state)
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    local defender = pick_alpha_front_line_character_target(state)
    append_omega_attack_plan(state, attacker, defender)
    return nil
end
