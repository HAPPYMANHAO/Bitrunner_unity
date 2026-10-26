# Coplay Unity MCP adapter

This optional adapter maps the portable skill's connected-Editor capabilities
to the community [Coplay Unity MCP](https://github.com/CoplayDev/unity-mcp)
provider. It is not an official Unity package, and the portable skill does not
require it.

The notes below were checked against Coplay v10 source at commit
`484937c4a75709f4c632b15d933e79556c3c3a2d`. Treat the installed provider's
live resources and tool schemas as authoritative: groups, parameters, and
behavior can change between releases.

## Connect by capability

Before using the Editor connection:

1. list the connected Unity instances and select the exact project;
2. read project information, Unity version, packages, active scene, Play/Edit
   state, and console state;
3. inspect the provider's live tool groups and enable only the capability needed;
4. inspect loaded scenes and prefab stage for dirty or unsaved work;
5. confirm the target object or asset through a read-only resource before any
   mutation.

Coplay v10 supports HTTP and stdio transports and can route to multiple Editor
instances. Do not assume that the sole or previously active connection is the
project named by the user. After a script compile or domain reload, re-read
Editor readiness, reacquire object identifiers, and confirm the target instance
before continuing.

Prefer stdio or loopback-only HTTP for local work. Do not expose a LAN or remote
endpoint without explicit authority, TLS, the provider's current authentication
or API-key validation, and verification of the live transport schema.

Map the portable workflow to whatever the current provider exposes for these
capabilities:

- hierarchy, object, component, scene, prefab, package, and console inspection;
- bounded object, asset, and script changes with undo/dirty tracking;
- explicit save of only the authorized scene or asset;
- Play Mode control, screenshots, and console reinspection;
- focused Edit Mode or Play Mode tests with completion polling.

Do not memorize a broad tool catalog. Use read-only resources for observation
where possible, and consult the live schema immediately before a mutating call.

## `execute_code` diagnostics

In Coplay v10, `execute_code` is in the optional `scripting_ext` group. It
accepts a C# **method body**, not an `IRunCommand` class. The provider wraps the
body in a static `object Execute()` method with common `System`, `UnityEngine`,
and `UnityEditor` namespaces, compiles it in memory, and serializes the returned
object. Its `auto` compiler uses Roslyn when available and otherwise falls back
to CodeDom; CodeDom is limited to C# 6 syntax.

The snippets under `adapters/coplay-unity-mcp/scripts/` are read-only method
bodies compatible with that wrapper:

- `check-input-system.cs` reports input handling, installed Input System state,
  loaded `PlayerInput` components, action assets, and likely legacy API calls;
- `diagnose-scene.cs` summarizes common scene, physics, UI, camera, controller,
  input, and missing-component evidence;
- `inspect-navmesh.cs` reports NavMesh triangulation and loaded agent state
  without adding a surface or baking data.

Read the snippet before submitting it as the `code` value with action
`execute`. Keep `safety_checks` enabled and prefer compiler `auto`; adapt the
body if the installed schema or project version differs. The snippets return
compact serializable dictionaries and cap detail lists, but project-wide asset
searches can still take time.

`execute_code` runs arbitrary code with the Unity Editor process's authority.
Its blocked-pattern checks are a basic guard, **not a security sandbox**. Use it
only when the user and project authorize that access, never run untrusted code,
and do not disable safety checks merely to force a snippet through.

## Mutation and save gate

Before an Editor mutation:

1. identify the selected instance, target, serialized references, and prefab
   context;
2. record dirty scenes/assets and unrelated unsaved work;
3. make one bounded, undo-aware change using the provider's current schema;
4. wait through compilation or domain reload and inspect console errors;
5. save only the assets authorized by the request;
6. re-read the affected object and run the smallest relevant test.

If the provider cannot expose unsaved state or target the intended save, stop
before overwriting authored work. Do not turn a diagnostic into an automatic
NavMesh bake, package install, input migration, or project-settings change.
