-- enemy_ai_goblin_shaman  (is_library = true)
-- AI module for the goblin_shaman enemy.

-- Logs the final_def of every card in the given front line (for post-buff inspection).
function goblin_shaman_log_front_line_def(front_line, label)
    lib_battle_common.dlog("[entity_ai] " .. label .. " front_line def (count=" .. #front_line .. "):")
    for _, front_card in ipairs(front_line) do
        local front_id   = front_card.inventory_item_id or ""
        local front_code = front_card.item_definition_code_name or ""
        local front_def  = front_card.final_def or 0
        lib_battle_common.dlog("[entity_ai]   id=" .. front_id .. " code=" .. front_code .. " final_def=" .. front_def)
    end
end

-- Defend reaction: trigger totem_pulse from back_line if omega front-line is taking damage.
-- Returns err or nil.
function defend(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.defend ==")
    local ability_err = enemy_ai_core.defend_with_back_line_ability_when_front_line_takes_damage(
        state, "totem_pulse", "goblin_shaman"
    )
    if ability_err ~= nil then return ability_err end
    goblin_shaman_log_front_line_def(state.omega_front_line or {}, "omega")
    lib_battle_common.dlog("[entity_ai] goblin_shaman.defend done")
    return nil
end

-- Deploy strategy: deploy one character per turn. While Alpha still has a
-- Character on its front line, keep Shaman's next character face-down once one is face-up.
-- Resets newly deployed cards. Returns: front_line, back_line, hand, err.
function deploy(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.deploy ==")

    local slot_count       = lib_battle_common.get_hand_size()
    local omega_front_line = state.omega_front_line or {}
    local omega_back_line  = state.omega_back_line or {}
    local deployed_ids     = {}
    local front_deployed   = {}
    local back_deployed    = {}
    local has_face_up_character = false
    for _, front_card in ipairs(omega_front_line) do
        if front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
            and front_card.face_up == true
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            has_face_up_character = true
            break
        end
    end

    local alpha_front_line_character_count = 0
    for _, front_card in ipairs(state.alpha_front_line or {}) do
        if front_card.inventory_item_id ~= nil and front_card.inventory_item_id ~= ""
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            alpha_front_line_character_count = alpha_front_line_character_count + 1
        end
    end

    local hand_cards                   = lib_battle_ai._collect_cards(state.omega_hand or {})
    local character_cards, other_cards = lib_battle_ai._split_cards_by_type(hand_cards, state.item_defs)
    local totem_pulse_cards            = enemy_ai_core.filter_cards_by_code(other_cards, "totem_pulse")
    lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: characters=" .. #character_cards .. " totem_pulse=" .. #totem_pulse_cards)

    -- Keep the one-character-per-turn rule. Retain hidden information while
    -- Alpha still has a Character in its front line.
    if #character_cards >= 1 then
        local deploy_card = character_cards[1]
        local face_up = not (has_face_up_character and alpha_front_line_character_count > 0)
        for slot_i = 1, slot_count do
            local existing = omega_front_line[slot_i]
            if existing == nil or existing.item_definition_code_name == nil or existing.item_definition_code_name == "" then
                deploy_card.slot_index   = slot_i - 1
                deploy_card.face_up      = face_up
                deploy_card.expose       = face_up
                omega_front_line[slot_i] = deploy_card
                table.insert(deployed_ids, deploy_card.id)
                table.insert(front_deployed, deploy_card)
                lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: front slot=" .. (slot_i - 1) .. " card=" .. (deploy_card.inventory_item_id or "?"))
                break
            end
        end
    end

    -- Deploy totem_pulse cards to back (always face down).
    for _, deploy_card in ipairs(totem_pulse_cards) do
        local face_up = false
        for slot_i = 1, slot_count do
            local existing = omega_back_line[slot_i]
            if existing == nil or existing.item_definition_code_name == nil or existing.item_definition_code_name == "" then
                deploy_card.slot_index  = slot_i - 1
                deploy_card.face_up     = face_up
                deploy_card.expose      = face_up
                omega_back_line[slot_i] = deploy_card
                table.insert(deployed_ids, deploy_card.id)
                table.insert(back_deployed, deploy_card)
                lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: back slot=" .. (slot_i - 1) .. " card=" .. (deploy_card.inventory_item_id or "?"))
                break
            end
        end
    end

    lib_battle_ai.ensure_omega_hand_draw_capacity(
        state, omega_front_line, omega_back_line, state.omega_hand or {},
        deployed_ids, front_deployed, back_deployed
    )

    local new_hand = lib_battle_ai._rebuild_hand(state.omega_hand or {}, deployed_ids)
    lib_battle_ai._append_mid_deploy_actions(state, front_deployed, back_deployed)
    lib_battle_ai._reset_deployed_cards(state.item_defs, front_deployed, back_deployed)
    lib_battle_common.dlog("[entity_ai] goblin_shaman.deploy: deployed=" .. #deployed_ids)

    return omega_front_line, omega_back_line, new_hand, nil
end

-- Counts hidden Characters in omega_front_line and returns the first one that
-- may attack. The returned card is intentionally face-down: attacking exposes it.
function goblin_shaman_find_extra_face_down_attacker(state)
    local face_down_count = 0
    local first_face_down_attacker = nil
    for _, front_card in ipairs(state.omega_front_line or {}) do
        local card_id = front_card.inventory_item_id or ""
        if card_id ~= ""
            and front_card.trigger ~= true
            and front_card.face_up ~= true
            and lib_battle_common.check_card_type(state.item_defs, front_card, "character") then
            face_down_count = face_down_count + 1
            if first_face_down_attacker == nil then
                first_face_down_attacker = front_card
            end
        end
    end
    return face_down_count, first_face_down_attacker
end

-- Use the shared face-up-first target priority.
function goblin_shaman_pick_attack_target(state)
    return enemy_ai_core.pick_alpha_front_line_character_target(state)
end

-- Attack planning: keep one hidden Character. When more than one hidden
-- Character is on the front line, attack with a hidden one to reveal it.
-- Returns err or nil.
function plan_attack(state)
    lib_battle_common.dlog("[entity_ai] == goblin_shaman.plan_attack ==")
    state.omega_planning = {}
    local defender = goblin_shaman_pick_attack_target(state)
    local face_down_count, face_down_attacker = goblin_shaman_find_extra_face_down_attacker(state)
    local attacker = nil
    if face_down_count > 1 then
        attacker = face_down_attacker
        lib_battle_common.dlog("[entity_ai] goblin_shaman.plan_attack: face-down count=" .. face_down_count .. ", attacking with hidden card=" .. attacker.inventory_item_id)
    else
        attacker = lib_battle_ai._find_omega_attacker(state, true)
    end
    if attacker == nil then
        lib_battle_ai.omega_end_turn(state)
        return nil
    end

    if defender == nil then
        table.insert(state.omega_planning, {
            action = "omega_attack_alpha_hp",
            attacker_inv_id = attacker.inventory_item_id,
            defender_inv_id = "alpha_hp"
        })
        lib_battle_common.append_client_action(
            state,
            lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, "alpha_hp")
        )
        return nil
    end

    table.insert(state.omega_planning, {
        action = "card_attack_card",
        attacker_inv_id = attacker.inventory_item_id,
        defender_inv_id = defender.inventory_item_id
    })
    lib_battle_common.append_client_action(
        state,
        lib_battle_ai.build_omega_planning_character_attack_action(state, attacker, defender.inventory_item_id)
    )
    return nil
end
