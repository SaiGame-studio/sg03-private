# Agent Rules

- **No Vietnamese Comments in Code**: Always write comments and documentation in code files in English, even if the user interacts with you in Vietnamese. Do not use Vietnamese comments in the codebase.
- **English UI Text Only**: All user-facing UI text, including labels, buttons, tooltips, empty states, and error messages, must be written in English. Do not add Vietnamese text to UI assets or code.
- **Maximum File Length**: A single code file should never exceed 700 lines. If a file is approaching this limit, suggest refactoring or splitting it into multiple files.
- **Prioritize Event Actions**: Always prioritize using event actions over `Update()` or similar continuous loops, unless it is impossible to implement the required logic with event actions.
- **VisualElement Identification**: All `VisualElement` instances must have a `name` or `id` to uniquely identify them.
- **_sg03-Only Changes**: Agents assigned to _sg03-scoped UI work may only create, edit, move, or delete files under `Assets/_sg03`. They must not modify files elsewhere in the repository.
- **Verify C# Compilation**: After writing or modifying C# code, always check and verify that the code compiles without build errors.
- **WebGL Icon Assets**: Do not use emoji or custom USS geometry elements to build UI icons. Do not manually generate custom SVG icons; always use official `.svg` vector icons downloaded directly from FontAwesome or WebGL-compatible serialized sprite/vector image assets from valid project asset sources.
- **Evidence-Based Explanations (No Guessing)**: Always substantiate every technical explanation, root cause analysis, or response with concrete code snippets, line numbers, or empirical log/file evidence. Never guess or speculate on code logic, architecture, or behavior without inspecting the authoritative source.

## Enemy AI Draw Priority

- Every Omega draw must use `lib_battle_ai.find_omega_source_choice_index(state, source)` before random selection. The helper prioritizes remaining source cards by `metadata.omega.metadata.choose_card_1`, then `choose_card_2`, then `choose_card_3`; random selection is allowed only when none of those configured cards remain in `omega_the_source`.
- During battle setup, guarantee that `omega_the_source` contains at least one copy of each configured `choose_card_X`, and do not move an Omega chosen card to `the_void` before `omega_choose_cards` runs.
- Do not duplicate this lookup in an individual enemy AI module. New enemy AIs configure their draw priority through `choose_card_X` metadata and rely on the shared draw pipeline.

## Enemy AI Hand Capacity

- An Omega AI must, whenever legal battlefield slots exist, deploy enough eligible hand cards to leave at least `lib_battle_common.get_draw_card_count()` empty hand slots before its next draw. Use `lib_battle_ai.ensure_omega_hand_draw_capacity(...)` after enemy-specific deployment; do not reimplement this loop in an individual AI.
- An AI may exclude a card only for an explicit enemy-specific game rule, such as reserving a required combo. The exception must be passed as `excluded_ids` and documented next to that AI's deployment logic.

## Enemy AI Direct Damage Priority

- When Alpha has no Character on `alpha_front_line` and Omega has an eligible Character, the AI must plan `omega_attack_alpha_hp` immediately. Check this before card-target combo logic; face-down Omega Characters remain eligible because the direct-attack executor reveals them before damage.


