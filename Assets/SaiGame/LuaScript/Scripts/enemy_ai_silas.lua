-- enemy_ai_silas  (is_library = true)
-- AI module for the Silas normal enemy.

-- Brute Call may only summon Goblin Brute from the Void, so Silas starts with
-- one Brute prepared there instead of deploying it from the source deck.
function prepare_battle_start(enemy, enemy_source, id_generator)
    local enemy_void = {}
    for index, card in ipairs(enemy_source or {}) do
        if card.item_definition_code_name == "goblin_brute" then
            table.remove(enemy_source, index)
            card.inventory_item_id = id_generator()
            card.slot_index = nil
            card.face_up = true
            card.expose = true
            card.trigger = false
            table.insert(enemy_void, card)
            return enemy_void, nil
        end
    end
    return nil, "silas requires goblin_brute in enemy_the_source"
end

-- Defend with Totem Pulse through the same standard Ability flow as Goblin Shaman.
function defend(state)
    return enemy_ai_core.defend_with_back_line_ability_when_front_line_takes_damage(
        state, "totem_pulse", "goblin_shaman"
    )
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
    local shaman_card = enemy_ai_core.find_card_by_code(hand_cards, "goblin_shaman", nil)
    local brute_call_card = enemy_ai_core.find_card_by_code(hand_cards, "brute_call", nil)
    local deployed_ids = {}
    local front_deployed = {}
    local back_deployed = {}

    -- Totem Pulse is deployed as soon as it is drawn, without using a front slot.
    for _, card in ipairs(hand_cards) do
        if card.item_definition_code_name == "totem_pulse" then
            local slot_i = enemy_ai_core.find_empty_slot(back_line, slot_count)
            if slot_i == nil then break end
            enemy_ai_core.deploy_card(back_line, slot_i, card, false, back_deployed)
            table.insert(deployed_ids, card.id)
        end
    end

    local reserve_left = enemy_ai_core.find_adjacent_empty_slots(front_line, slot_count, 2)
    local can_combo = tonumber(state.turn or 0) >= 4
        and shaman_card ~= nil
        and brute_call_card ~= nil
        and reserve_left ~= nil
        and enemy_ai_core.find_empty_slot(back_line, slot_count) ~= nil
        and enemy_ai_core.find_card_in_zone_by_code(state, "omega_the_void", "goblin_brute") ~= nil

    if can_combo then
        enemy_ai_core.deploy_card(front_line, reserve_left, shaman_card, true, front_deployed)
        table.insert(deployed_ids, shaman_card.id)

        local brute_call_slot = enemy_ai_core.find_empty_slot(back_line, slot_count)
        enemy_ai_core.deploy_card(back_line, brute_call_slot, brute_call_card, false, back_deployed)
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
        local ability_err = enemy_ai_core.trigger_ability_and_append_actions(
            state, brute_call_card, "brute_call", "on_attack", event_data
        )
        if ability_err ~= nil then return front_line, back_line, new_hand, ability_err end
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
                local slot_i = enemy_ai_core.find_empty_slot(front_line, slot_count)
                if slot_i ~= nil then
                    enemy_ai_core.deploy_card(front_line, slot_i, card, true, front_deployed)
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

-- Prefer the weakest face-up Alpha Character, then a face-down Character, or Alpha HP.
function plan_attack(state)
    local defender = enemy_ai_core.pick_alpha_front_line_character_target(state)
    return enemy_ai_core.plan_omega_attack_with_target(state, defender)
end
