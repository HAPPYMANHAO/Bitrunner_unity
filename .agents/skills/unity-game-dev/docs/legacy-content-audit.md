# Legacy Content Audit

Audited 2026-07-10 against the original `main` package, the ignored research
checkouts in [research-sources.md](research-sources.md), and current Unity 6
release guidance.

## Result

The 23,000-line deletion was too broad in topic coverage but correct in storage
format. The removed `references/*.md` files were mostly unversioned, duplicated
Unity Manual scrapes. Restoring those snapshots would make the skill larger and
more likely to select an API from the wrong Unity/package version.

The fix is to keep those dumps deleted while restoring the useful decision
knowledge as concise, version-aware references.

## Current-change findings

- Unity's current release model separates LTS from Supported Update releases;
  the newest available editor is not automatically the right upgrade target for
  a live project.
- The local UnityCsReference checkout currently identifies itself as
  `6000.7.0a2`. It is useful for upcoming API/source inspection, not as proof of
  stable 6.0/6.3/6.5 behavior.
- The current Input System development package reports `1.20.0` and documents
  several supported workflows: project-wide Actions, `PlayerInput`, and direct
  device reads. This confirms that “always use one input style” is wrong even
  when the package is active.
- The current Cinemachine development package reports `3.1.8-pre.1` and its
  documentation explicitly calls the 2.x → 3.x architecture change breaking.
  Legacy component recipes cannot be kept as universal guidance.
- The current Graphics development packages report `17.6.0`. Render pipeline,
  Render Graph, resource, custom-pass, Shader Graph, and VFX APIs must be checked
  against the project's installed package rather than a copied manual page.
- Besty0728/Unity-Skills has advanced from the locally installed old `1.6.4`
  snapshot to a `2.1.0`-era REST/provider package. It remains useful for topic
  discovery, but its tool contract must not leak into this provider-neutral
  skill.
- Unity now ships an Assistant package with its own skills, AI Gateway, and
  official Unity MCP path. Authoring AI must be kept separate from Sentis
  runtime inference and ML-Agents training; restoring one old catch-all AI page
  would collapse three different version and evidence boundaries.
- Unity 6.2–6.5 stages the transition from InstanceID assumptions toward
  EntityId. Long-lived Editor automation must reacquire session handles and use
  documented persistent locators rather than preserving old integer-ID recipes.
- Coplay v10 replaced the legacy `IRunCommand` diagnostic shape with an
  in-memory method-body execution surface. The previous adapter examples were
  therefore repaired instead of being retained merely because they were local.

## Coverage disposition

| Legacy content | Disposition | Current home |
| --- | --- | --- |
| `references/2d.md` | Raw scrape remains deleted; 2D renderer, sprites, Tilemaps, sorting, and Physics2D checks restored | [content-rendering-and-media.md](../references/content-rendering-and-media.md) |
| `references/3d.md`, `rendering.md`, `shaders.md` | Raw scrapes remain deleted; pipeline, quality, Render Graph/custom pass, shader variant, GPU, lighting, and VFX decisions restored | [content-rendering-and-media.md](../references/content-rendering-and-media.md), [performance-and-testing.md](../references/performance-and-testing.md) |
| `references/animation.md`, `audio.md`, `ui.md` | Raw scrapes remain deleted; animation/Timeline, camera, audio, feedback, UI, localization, and accessibility decisions retained | [content-rendering-and-media.md](../references/content-rendering-and-media.md), [gameplay-systems.md](../references/gameplay-systems.md) |
| `references/assets.md`, `scene_management.md` | Raw scrapes remain deleted; importer, GUID, prefab, scene composition, Addressables/AssetBundle, and release ownership restored | [content-rendering-and-media.md](../references/content-rendering-and-media.md), [editor-serialization-and-source-control.md](../references/editor-serialization-and-source-control.md) |
| `references/physics.md` | Raw scrape remains deleted; movement ownership, cadence, colliders, 2D/3D separation, and NavMesh diagnostics retained | [gameplay-systems.md](../references/gameplay-systems.md), [performance-and-testing.md](../references/performance-and-testing.md) |
| `references/platform.md`, `networking.md`, `services.md`, `xr.md` | Misgrouped/raw snapshots remain deleted; platform builds, multiplayer authority, services, and XR version boundaries restored | [platform-networking-and-xr.md](../references/platform-networking-and-xr.md) |
| `references/scripting.md`, `editor.md`, `getting_started.md` | Raw snapshots remain deleted; lifecycle, async, asmdef, Editor/source-control, and exact-toolchain rules retained | [architecture.md](../references/architecture.md), [project-workflow.md](../references/project-workflow.md), [editor-serialization-and-source-control.md](../references/editor-serialization-and-source-control.md) |
| `references/optimization.md` | Raw snapshot remains deleted; profiling, memory, rendering, test, CI, and target evidence retained | [performance-and-testing.md](../references/performance-and-testing.md) |
| Legacy AI/advisory material | Undated catch-all guidance remains deleted; Assistant/Gateway/MCP, Sentis, and ML-Agents are separated by package and lifecycle | [ai-and-agent-workflows.md](../references/ai-and-agent-workflows.md), [version-and-package-checks.md](../references/version-and-package-checks.md) |
| `references/index.md`, `other.md` | Duplicated catch-all indexes remain deleted; `SKILL.md` is the bounded router | [SKILL.md](../SKILL.md) |

## Advisory modules

The 21 nested `advisory/*/SKILL.md` files remain removed as independently
discoverable skills. Their durable content was merged as follows:

- architecture, ADR, patterns, asmdef, async, script roles/design, inspector,
  scene contracts, testability, and project scouting:
  [architecture.md](../references/architecture.md) and
  [project-workflow.md](../references/project-workflow.md);
- performance, rendering, physics, NavMesh, AI, camera, UI, audio, saves, and
  game feel: the domain references listed above;
- workflow, testing, CI, Git, prefabs, and build automation:
  [performance-and-testing.md](../references/performance-and-testing.md) and
  [editor-serialization-and-source-control.md](../references/editor-serialization-and-source-control.md);
- asset/license sourcing: [assets-and-licensing.md](../references/assets-and-licensing.md);
- optional design, implementation, and QA perspectives: the three review briefs
  under `references/`.

## Intentionally rejected legacy rules

These were not preserved because they were universal claims, stale APIs, or
unsafe side effects:

- always use Cinemachine, FMOD, the new Input System, or one UI system;
- force designer → programmer → QA delegation for every implementation;
- switch Active Input Handling to Both as a generic fix;
- add `NavMeshSurface` and bake automatically during diagnosis;
- hard-code Cinemachine 2 component names in a 2026-wide skill;
- hard-code URP post-processing scripts and package assumptions;
- prescribe binary saves, AES, fixed CJK glyph counts, or memory savings without
  project evidence;
- claim current licenses or pricing from an old asset-source table;
- treat a community MCP provider as an official Unity tool;
- treat static or cross-version compilation as matching-Editor validation.

## Acceptance rule

Future removals must map every affected topic to one of:

1. retained portable contract;
2. current focused reference;
3. provider adapter;
4. deterministic script;
5. explicit rejection with evidence.

Do not restore raw documentation dumps merely to reduce the deletion count.
