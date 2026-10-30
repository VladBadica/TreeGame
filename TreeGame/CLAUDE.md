# TreeGame

A cozy 2.5D tree-growing game. The player cares for a tree by playing short, skill-based minigames that earn resources such as water and nutrients. Over time the tree grows: its trunk gets taller and thicker, its branches lengthen and its foliage fills in.

The project is at an early prototype stage. Design so the game can still change direction: avoid choices that lock it into one approach, and avoid building for features that don't exist yet.

## Tools

- **Unity 6** with the **Universal Render Pipeline (URP)**, written in **C#**.
- **Input System package** for input. The legacy Input Manager is disabled, so do not use `UnityEngine.Input` or `StandaloneInputModule`.
- **uGUI** (`UnityEngine.UI`) for UI, with **TextMeshPro** (`TMPro`) for all text. Do not use the legacy `UnityEngine.UI.Text`.
- No third-party dependencies unless explicitly agreed.

## Code principles

**Readable first.** Prefer clear, plain code over clever or compact code. Use descriptive names and standard C# conventions. Comments explain *why* (design decisions, non-obvious behavior), not *what*.

**No duplication.** Before writing new logic, look for an existing component or helper that already does it and reuse or extend it. Share repeated setup through a small helper instead of copying it.

**Stay flexible.** We are early, so:
- Prefer small, single-purpose components over large controllers.
- Keep game state separate from presentation. State components own data and raise events; visuals and UI listen and render. Visuals or UI can then be replaced without touching game logic.
- Keep systems loosely coupled. Communicate through C# events and small public APIs, not direct reach-ins. Avoid singletons and global state.
- Keep systems independent of who drives them. For example, growth does not know about resources and resources do not know about minigames. Connect systems at a higher level instead of inside each other.
- Don't add abstraction layers, frameworks or "future-proof" systems until a real need appears. Simple code is easier to change direction with than an elaborate design.

**Unity conventions**
- Use `[SerializeField] private` fields for values tuned in the Inspector. Make members public only when another component needs them.
- Validate and clamp public inputs (reject NaN, infinity and out-of-range values) at the boundary of each state component.
- Avoid per-frame work when nothing changes. Never create or destroy objects every frame.
- Use deterministic placement (fixed seeds) for procedural visuals, so the same state always looks the same.
- Namespaces start with `TreeGame.` Do not name a namespace `Editor` or `Tree`, because they clash with Unity types.
- Group scripts under `Assets/Scripts/` by feature or responsibility. Editor-only code goes in an `Editor` folder.
- Never name a folder `Resources` unless you really want Unity's Resources loading behavior, which bundles everything in it into builds.

## Working in this project

- Don't hand-edit `.unity`, `.prefab` or `.meta` files. Build scenes and assets with Editor scripts, using the Unity API, or describe the Editor steps for the user to do.
- When deleting or moving assets, keep each `.meta` file with its asset, or ask the user to do it in the Unity Editor.
- Claude cannot press Play in the Editor. After changes, check that the code compiles, then give the user clear steps to verify the change in the Editor.
- Keep development and debug tools (test panels, self-checks) separate from gameplay code, so they can be removed easily.
