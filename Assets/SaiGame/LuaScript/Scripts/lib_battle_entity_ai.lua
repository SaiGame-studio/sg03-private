-- lib_battle_entity_ai  (is_library = true)
-- Resolves the AI library name from the Omega entity instead of maintaining an
-- enemy-key dispatcher in each battle-phase script.

function get_enemy_key(state)
    return state.metadata ~= nil and state.metadata.enemy_entity_key or nil
end

local function load_enemy_ai(state)
    local enemy_key = get_enemy_key(state)
    local enemy, enemy_err = game.get_entity_def_by_key(enemy_key)
    if enemy == nil then return nil, enemy_err end

    local script_name = "enemy_ai_" .. enemy.entity_key
    local enemy_ai = _G[script_name]
    if enemy_ai == nil then
        return nil, "enemy AI library is not loaded: " .. script_name
    end
    return enemy_ai, nil
end

local function get_enemy_handler(state, handler_name)
    local enemy_ai, ai_err = load_enemy_ai(state)
    if enemy_ai == nil then return nil, ai_err end

    local handler = enemy_ai[handler_name]
    if type(handler) ~= "function" then
        return nil, "entity AI handler is missing: " .. tostring(handler_name)
    end
    return handler, nil
end

function run_defend(state)
    local handler, handler_err = get_enemy_handler(state, "defend")
    if handler == nil then return handler_err end
    return handler(state)
end

function run_plan_attack(state)
    local handler, handler_err = get_enemy_handler(state, "plan_attack")
    if handler == nil then return handler_err end
    return handler(state)
end

function deploy_enemy(state)
    local enemy_key = get_enemy_key(state)
    lib_battle_common.dlog("[entity_ai] deploy_enemy enemy_key=" .. tostring(enemy_key))

    local handler, handler_err = get_enemy_handler(state, "deploy")
    if handler == nil then return handler_err end

    local o_front, o_back, o_hand, deploy_err = handler(state)
    if deploy_err ~= nil then return deploy_err end

    state.omega_front_line = o_front
    state.omega_back_line  = o_back
    state.omega_hand       = o_hand
    return nil
end
