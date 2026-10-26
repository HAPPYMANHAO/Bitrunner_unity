# Performance and Testing

Use this reference when the task concerns profiling, allocation, rendering,
test scope, CI, or target-platform evidence.

## Measure first

Capture the scenario, device/editor target, build type, frame timing, and
relevant profiler modules before optimizing. Editor profiling is useful for
direction but is not a substitute for a development build on the target.

Common investigation lanes:

- CPU: script time, physics, animation, rendering submission, and jobs;
- memory: managed allocation, GC, native memory, texture/mesh/audio residency;
- GPU: overdraw, shader cost, bandwidth, shadows, post-processing, resolution;
- loading: synchronous asset work, scene activation, shader warmup, and I/O.

Pooling is not automatically faster. Use it when creation/destruction cost,
allocation, or spikes are measured and define reset semantics for pooled state.

## Rendering

Identify Built-in, URP, or HDRP and its installed version. Inspect actual render
pipeline assets, renderer features, quality tiers, batching/instancing
conditions, materials, lights, shadows, LODs, and platform limits before
recommending a pipeline-specific change.

## Test choice

- **EditMode:** pure rules, serialization helpers, Editor utilities, and fast
  deterministic logic.
- **PlayMode:** lifecycle, scene/prefab wiring, physics, input integration,
  coroutines, time, and rendering-dependent behavior.
- **Target/player:** platform APIs, build stripping, device input, performance,
  graphics, file paths, and services.

Test user-visible contracts and state transitions instead of duplicating method
implementation. Keep scenes and prefabs used by tests explicit and preserve
their `.meta` files.

## CI and builds

Reuse repository-native commands. Pin or derive the matching Unity version,
capture license and platform prerequisites without exposing secrets, and retain
the Editor/player logs that explain failures.

A passing C# analyzer is static evidence. A passing EditMode suite does not
prove PlayMode wiring. A successful editor build does not prove target-device
performance. Report the actual layer reached.
