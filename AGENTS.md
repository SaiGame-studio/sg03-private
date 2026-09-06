# Repository Rules

## SaiGame Package Boundary

Never create, modify, move, rename, or delete files under `Assets/SaiGame/`, except files directly inside `Assets/SaiGame/LuaScript/Scripts/`.

Treat `Assets/SaiGame/` as a read-only dependency. Implement project-specific behavior only outside that directory, except for game-specific Lua scripts directly inside `Assets/SaiGame/LuaScript/Scripts/`, unless the user explicitly revokes this rule for a specific change.

## Lua Error Catalog

Whenever a Lua script introduces a new error type, error string, or failure reason, add the corresponding entry to `Assets/SaiGame/LuaScript/Scripts/LuaErrorCatalog.md` as part of the same change.

## Lua Shared Helpers

For basic Lua operations that may be reused (for example definition lookup, card type or race checks, and line or battle-state scans), prefer an existing shared helper. When none exists, add a generic helper to the appropriate shared library rather than a function named for one ability or card. Keep ability-specific functions only for rules that are genuinely unique to that ability.

Keep Lua functions focused on one responsibility. When a function combines collection, state selection, mutation, and action creation, split it into small, clearly named helpers so the logic is readable and maintainable. Reuse generic helpers for shared operations; keep ability-specific helpers only for unique game rules.

## Conditional Complexity (C# and Lua)

In C# and Lua, do not nest `if` statements more than three levels deep. Do not create an `if` / `else if` / `else if` chain with more than three conditional branches. When a rule would exceed either limit, use guard clauses, a clearly named helper, or table-driven dispatch instead.

## Game Content Naming

When creating or editing game content, use English for every official character and skill name. Vietnamese may be used only in descriptive text or explanatory notes, never as an official name, identifier, or card title.

## Unity Lifecycle Methods

Do not place implementation logic directly in Unity lifecycle methods (for example `Awake`, `Start`, `OnEnable`, `Update`, `LateUpdate`, `OnDisable`, or `OnDestroy`). Lifecycle methods may only call clearly named helper methods; put all logic in those helper methods instead.

## Unity Component Loading

Resolve Unity component references through the `LoadComponents()` mechanism. Every `SaiBehaviour` that needs component dependencies must override `LoadComponents()`, call `base.LoadComponents()` first, and invoke clearly named `Load...` helper methods for those dependencies.

Stable scene and prefab dependencies should also have serialized references. A `Load...` helper may use `GetComponent`, `FindFirstObjectByType`, or a similar lookup only as a fallback when its cached or serialized reference is null.

Do not resolve missing component references lazily from gameplay actions, event callbacks, request callbacks, UI handlers, or other runtime execution paths. Those paths must consume references already prepared by `LoadComponents()` and fail clearly if a required reference is unavailable.

## Runtime UI Assets

For any UI Toolkit asset required at runtime (`VisualTreeAsset`, `StyleSheet`, `PanelSettings`, and related assets), assign a serialized reference in the owning scene or prefab. That reference must be present in source control so Unity includes the asset in every player build, including WebGL.

`UnityEditor.AssetDatabase` is editor-only and must never be the only way a runtime UI asset is loaded. It may be used solely inside `#if UNITY_EDITOR` as a convenience fallback; the runtime serialized reference remains mandatory.

Before completing a UI change, inspect every affected scene/prefab serialization and verify that each newly used runtime asset has a non-null reference. Do not rely on an asset loading successfully in the Unity Editor as evidence it will work in a player build.

### WebGL Icon Compatibility

Do not use emoji or non-ASCII Unicode characters as UI icons. UI Toolkit's player fonts, especially in WebGL, may not include their glyphs even when they display in the Unity Editor. Do not custom-build icons by assembling UI Toolkit raw geometry elements in UXML/USS. Do not custom-create or manually generate SVG path code for icons; always use official `.svg` vector icons downloaded directly from FontAwesome or actual serialized sprite or vector image assets (e.g. Sprite/Texture2D/VectorImage) imported from valid asset sources supported in WebGL; these assets must follow the runtime-reference rule above. Plain ASCII text is acceptable for a textual fallback.

## Evidence-Based Explanations (No Guessing)

Always substantiate every technical explanation, root cause analysis, or response with concrete code snippets, line numbers, or empirical log/file evidence. Never guess or speculate on code logic, architecture, or behavior without inspecting the authoritative source.

