---
name: unity-game-dev
description: >
  Use when planning, implementing, debugging, or reviewing gameplay in an
  existing Unity project, including C# under Assets, scenes, prefabs, packages,
  input, UI, physics, NavMesh, Editor automation, Unity AI/MCP, Sentis,
  ML-Agents, tests, and performance. Adapts to the project's Unity version and
  available Editor-control capability. NOT for engine-neutral game design,
  ordinary non-Unity C#, standalone asset creation, generic ML work, or
  contributing to the Unity engine source.
---

# Unity Game Development

Deliver the smallest project-aligned result that satisfies the request. Read at
most one primary reference for a bounded task unless it genuinely spans domains.

## Authority Boundary

- Review, diagnosis, and status requests do not authorize edits, package
  changes, Editor mutations, or saves. Implementation requests authorize
  in-scope local edits and non-destructive validation.
- Ask before downloading or importing assets, adding or upgrading packages,
  changing project-wide settings, deleting assets or scenes, overwriting
  authored content, or performing external writes.
- Preserve unrelated worktree and unsaved Editor changes. Stop before a risky
  save or mutation if the control path cannot distinguish them.

## Establish the Project Baseline

Read the smallest relevant set:

- `ProjectSettings/ProjectVersion.txt` for the Editor version;
- `Packages/manifest.json` and, when needed, `Packages/packages-lock.json`;
- relevant input, rendering, physics, quality, and build settings;
- nearby scripts, asmdefs, tests, prefabs, scenes, and their `.meta` files;
- repository instructions and current worktree state.

Local configuration outranks assumptions. A manifest entry does not prove a
package is configured, and an asset name does not prove its version or API.
Preserve the active input backend unless migration is authorized.

From Unity 6.2 onward (6.4 makes int-based object identity obsolete and 6.5
turns obsolete InstanceID APIs into compile errors), treat EntityId as current object identity, not a durable reference. Route by the
exact patch, inspect exact APIs, reacquire handles after reload/session, and use
documented persistent locators where needed.

Use [version and package checks](references/version-and-package-checks.md) when
an API depends on Unity, package, render-pipeline, or input configuration.

## Choose an Execution Path

### Repository-only

Use source and serialized-asset edits only when safe without live Editor state.
Treat scene and prefab YAML as authored data: inspect ownership and references,
preserve `.meta` files, and avoid speculative serialization changes.

### Matching Unity Editor or project-native automation

Prefer the declared Editor and existing build, test, batchmode, or CI entry
points. Never present an ad-hoc compile against another Unity installation,
template, or project as Unity validation.

### Connected Editor control

Prefer a provider already approved and configured by the project. A project
already connected through the Assistant package's Unity MCP server can keep
using it, but Unity deprecated that server from Assistant 2.18. Otherwise, for
Unity 6.0 or later projects consider the official, still experimental Unity CLI
before a new community integration. Verify current Unity access requirements and
project policy before installing or enabling any agentic bridge; do not infer a
third-party provider's authorization or package-signing status from popularity.

First verify the connection, project, active scene, play/edit and console state,
dirty assets, and available capabilities. Name capabilities—inspect hierarchy,
read components, mutate, save, read logs—not assumed tool names.

For scene or prefab work, inspect, make one bounded mutation, save only intended
assets, re-inspect references and console, and use Play Mode only when needed.

Provider-specific setup lives in the [Unity CLI adapter](adapters/unity-cli.md),
the [deprecated Unity Assistant MCP adapter](adapters/unity-ai-mcp.md), and the
[Coplay Unity MCP adapter](adapters/coplay-unity-mcp.md). The core workflow must
still work when no provider is available.

## Implementation Rules

- Match existing namespaces, folder boundaries, naming, serialization patterns,
  assembly definitions, and test conventions.
- Keep one authoritative state owner where practical; add abstractions only for
  a visible dependency, lifecycle, authoring, or testing need.
- Respect Unity object lifetime and main-thread constraints. Do not treat a
  destroyed `UnityEngine.Object` like ordinary managed null.
- Decide whether time-dependent behavior uses scaled or unscaled time.
- For physics, distinguish `Update` input sampling from `FixedUpdate` physics
  work and avoid competing movement owners.
- Profile before adding pooling, custom update loops, jobs, or render complexity.

Read [architecture](references/architecture.md) for dependency and state choices,
or [gameplay systems](references/gameplay-systems.md) for input, movement,
camera, UI, audio, saving, and NavMesh guidance.

Use [content and rendering](references/content-rendering-and-media.md),
[platform/networking/XR](references/platform-networking-and-xr.md), or
[Editor and serialization](references/editor-serialization-and-source-control.md)
for those domains.

Use [AI and agent workflows](references/ai-and-agent-workflows.md) when the task
involves Assistant, Gateway, MCP, generated assets, Sentis runtime inference, or
ML-Agents training. Keep authoring AI, runtime inference, and training workflows
separate; their packages, data boundaries, evidence, and failure modes differ.

## Validation

Use the lowest evidence level that proves the requested result, and state the
level reached:

1. static inspection of source and serialized references;
2. project-native compile or script reload in the matching Unity version;
3. EditMode tests;
4. PlayMode or connected-Editor scenario;
5. target-platform build or runtime profiling.

Do not claim compile, save, package resolution, Play Mode, or build results
without observing them. Without the exact Editor or provider, finish safe work,
run static checks, and name the smallest remaining Editor verification.

Read [project workflow](references/project-workflow.md) for evidence levels and
scene mutation, or [performance and testing](references/performance-and-testing.md)
for profiling, EditMode, PlayMode, CI, and target-device checks.

## Design and Review

For feedback work, tie cues to one owned state or event. See
[game feel](references/game-feel.md).

Use independent review only for separate judgment, specialized inspection, or
fresh verification. Delegation never expands authority.

When a focused review would help, use these compact briefs:

- [game-design review](references/game-design-review.md)
- [implementation review](references/implementation-review.md)
- [QA review](references/qa-review.md)

For third-party content, verify the exact asset, current source, and license;
do not describe an asset as “risk-free.” See
[assets and licensing](references/assets-and-licensing.md).

## Output

Lead with the user-visible result. Name changed files/assets or review findings,
the validation and evidence level observed, material assumptions, unresolved
Editor/runtime checks, and the smallest next action when something remains.

## Gotchas

- Moving or regenerating a Unity asset without its `.meta` file changes GUID
  identity and can break serialized references.
- `Time.timeScale = 0` does not pause unscaled time, animation modes, audio, or
  every custom system.
- Text edits to scene or prefab YAML can create valid-looking but broken object
  references; prefer Editor-backed mutation when relationships are non-trivial.
- NavMesh behavior depends on the installed AI Navigation package, baked data,
  agent type, area mask, and runtime placement—not merely a `NavMeshAgent`.
- Sentis may appear under the package ID `com.unity.ai.inference` while runtime
  code uses the `Unity.InferenceEngine` namespace; inspect the installed version
  instead of renaming from product display text.
- Unity Assistant skills, Codex skills, and Claude Code skills can share a
  `SKILL.md` shape but use different discovery locations and optional metadata.
  Do not claim one installation covers all three runtimes.
- A clean static review is not evidence that the Editor compiled or that the
  runtime behavior works.
