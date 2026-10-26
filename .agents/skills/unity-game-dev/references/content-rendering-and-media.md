# Content, Rendering, and Media

Use this reference for assets, scenes, rendering, shaders, 2D, animation,
camera, audio, and content loading. These areas change substantially across
Unity and package versions; inspect the project before choosing an API.

## Contents

- Content ownership and loading
- Scenes and prefabs
- Rendering and shaders
- 2D
- Animation, Timeline, and camera
- Audio and feedback

## Content ownership and loading

Identify how the project currently owns and loads content:

- direct serialized references for known, always-present assets;
- scene or prefab references for authored composition;
- `Resources` only when its global path-based loading and build inclusion are
  intentional;
- Addressables for catalogued asynchronous content, remote delivery, or
  dependency-aware lifetime management;
- AssetBundles when the project already owns the lower-level build, catalog,
  dependency, cache, and release workflow.

Do not migrate an existing content system as a side effect of adding one asset.
For Addressables or AssetBundles, inspect the installed package/source and
existing wrappers. Track operation handles, scene activation, reference counts,
release ownership, failure state, cancellation limits, catalog version, and
offline behavior. Exact overloads and release semantics are version-sensitive.

Preserve GUIDs when moving assets. Review importer settings, platform overrides,
sprite atlases, texture compression, mesh read/write state, audio load type,
and generated artifacts before changing size or memory behavior.

## Scenes and prefabs

Determine whether the project uses a single scene, additive composition,
bootstrap/persistent scenes, Addressable scenes, or networked scene management.
Define who owns initialization, unloading, and cross-scene state. Avoid duplicate
`DontDestroyOnLoad` services and hidden runtime searches.

For prefabs, preserve variant inheritance and distinguish an instance override
from an asset change. Use the Editor for structural relationship changes when
possible, then verify references and overrides after saving.

## Rendering and shaders

First identify Built-in, URP, or HDRP, the package version, renderer assets, and
quality-level assignments. A manifest entry alone does not prove the active
pipeline. Profile CPU submission, GPU cost, bandwidth, overdraw, memory, and
shader compilation on the relevant target before changing the pipeline.

For SRP customization, inspect the installed package implementation and samples.
Renderer Features, Render Graph, compatibility paths, `RTHandle` usage, blit
helpers, volume integration, and pass timing differ between package versions.
Do not copy a custom pass from another major version without checking its API
and resource-lifetime model.

For shaders and Shader Graph:

- match the active pipeline and target shader model;
- inspect material properties, keywords, render queue, passes, and variants;
- preserve SRP Batcher and instancing compatibility where relevant;
- budget variants and stripping using build evidence;
- check precision, texture sampling, branching, transparency, and overdraw on
  the target GPU;
- treat serialized Shader Graph editing as version-sensitive authored data.

Use package source or exact-version documentation for node, target, block, and
custom-function behavior. A current development branch is change awareness, not
proof that a released project supports an API.

## 2D

Inspect the 2D package versions and whether the project uses the Built-in or URP
2D Renderer. Preserve pixels-per-unit, filtering, compression, sprite atlas,
sorting layer/group, pivot, and camera conventions.

For Tilemaps, identify grid layout, palette/tile assets, renderer mode, chunking,
animation, collider generation, and streaming needs. For physics, keep 2D and
3D APIs separate; consider CompositeCollider2D and Rigidbody2D body types based
on measured collision behavior rather than generic rules.

## Animation, Timeline, and camera

Identify Animator, Animation, Timeline/Playable, Animation Rigging, root-motion,
or code-driven ownership. Keep state transitions, parameter hashes, layer
weights, avatar masks, animation events, and root-motion authority explicit.
Timeline bindings and signals are serialized scene/asset relationships and need
Editor verification.

Do not require Cinemachine for every camera. If it is installed, distinguish
Cinemachine 2.x from 3.x before naming components: 3.x contains breaking
architecture and terminology changes. Match the project's existing rig, update
mode, target ownership, blending, damping, collision, input, and Timeline use.

## Audio and feedback

Match the existing native Audio, FMOD, Wwise, or custom path. Inspect import
compression, streaming/decompression choice, sample rate, mixer groups,
snapshots, spatialization, voice limits, pooling, and bank/bundle lifetime.
Middleware licensing and package versions are external facts; verify them
before recommending adoption.

Connect feedback to one owned gameplay event. Layer visual, motion, audio, and
timing cues, then test clarity, accessibility, repeated triggering, pause
behavior, and performance instead of hard-coding a universal effect stack.
