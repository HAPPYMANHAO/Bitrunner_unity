# Gameplay Systems

Use this reference for input, movement, camera, UI, audio, save data, and
NavMesh behavior.

## Input

Inspect both package state and active project configuration. The Input System
package can be installed while legacy input remains active, and projects may
use legacy, new, or both backends. Match the existing path unless a migration is
explicitly authorized.

Keep input sampling separate from gameplay intent when rebinding, multiple
devices, AI control, replay, or tests require it. For a small local behavior,
project-consistent direct input may be enough.

## Movement and physics

Identify the single movement owner and whether it uses a `Rigidbody`,
`CharacterController`, transform movement, root motion, or NavMesh. Do not mix
owners without an explicit handoff.

Read input in `Update` when frame-level responsiveness matters; apply
`Rigidbody` physics in `FixedUpdate`. Respect interpolation, collision mode,
constraints, layers, trigger rules, and 2D versus 3D APIs.

## Camera

Inspect the installed camera package and existing rig before choosing APIs.
Keep target selection, camera state, and effects ownership explicit. Verify
blend timing and update mode during the actual movement path.

## UI

Match uGUI, UI Toolkit, TextMesh Pro, or the project's combination. Confirm
EventSystem/input integration, sorting, scaling, safe area, navigation, focus,
localization, and CJK font coverage when relevant.

Inspect the installed Localization package and existing string/asset tables
before choosing keys or loading behavior. Preserve locale fallback, plural and
smart-string rules, font fallbacks, right-to-left needs, and dynamic layout.
Where the Unity version and UI stack support accessibility APIs, verify the
actual screen-reader/focus path; always retain non-color cues, reduced-motion
options, and controller/keyboard navigation as applicable.

Pause UI must distinguish scaled and unscaled time. Decide whether input,
animation, audio, physics, AI, and transitions continue while paused.

## Audio

Use the project's existing audio path. Centralize high-level routing only when
mixing, persistence, pooling, or middleware justifies it. Avoid firing the same
cue from several observers; connect it to one owned event.

## Save data

Version serialized data, define defaults for missing fields, and avoid storing
Unity object references as durable identity. Use stable IDs where cross-session
references matter. Write atomically when corruption risk matters and keep
platform storage constraints in view.

## NavMesh and simple NPC behavior

Confirm the AI Navigation package/API, baked or runtime surface data, agent
type, area mask, links, obstacles, and agent placement. `isOnNavMesh` is a
diagnostic, not a complete explanation.

For a small NPC, explicit states such as idle, patrol, investigate, chase, and
return are usually easier to inspect than a behavior-tree or GOAP framework.
Add a larger decision system only for demonstrated complexity.
