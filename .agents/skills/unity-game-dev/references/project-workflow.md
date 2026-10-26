# Project Workflow and Evidence

Use this reference when deciding how to inspect, mutate, save, or validate a
Unity project.

## Baseline inspection

Start with the request boundary and the files that determine local behavior:

- repository instructions and `git status`;
- `ProjectSettings/ProjectVersion.txt`;
- `Packages/manifest.json` and relevant lock-file entries;
- relevant `ProjectSettings` assets;
- nearby scripts, asmdefs, tests, scenes, prefabs, and `.meta` files.

Search for the project's established implementation before inventing a new
pattern. Identify generated files, vendored packages, and authored assets that
should not be reformatted or replaced.

## Execution routes

### Repository edits

Best for C#, tests, asmdefs, configuration that is intentionally text-managed,
and small serialized changes whose reference graph is understood. Do not claim
that source inspection proves Unity compilation.

### Batch or CI automation

Use an existing project command when available. A typical Unity batch invocation
must use the project's exact editor version and project-defined entry point.
Capture the exit code and the relevant Editor log. Avoid creating a new build
pipeline merely to validate a local change.

### Connected Editor

Confirm the provider is connected to the intended project and establish active
scene, edit/play state, selection or prefab stage when relevant, dirty assets,
and current console errors. Then:

1. inspect hierarchy, components, and serialized references;
2. mutate one bounded unit;
3. save only intended assets;
4. re-inspect the saved result;
5. observe compile and console state;
6. run a focused scenario if runtime behavior matters.

Do not assume that a provider command succeeded because the call returned.
Re-query the resulting object, file, or log.

## Exact-toolchain rule

Unity assemblies are coupled to editor and package versions. Do not assemble a
one-off C# compiler command from another Unity installation, a package cache, or
a template project and label that as project compilation. If the declared
editor is unavailable, use static checks and report the remaining exact-editor
check.

## Evidence levels

Use the lowest level that proves the claim:

1. **Static:** source, YAML, GUID, manifest, and reference inspection.
2. **Compile:** matching-editor script compilation or project-native build step.
3. **EditMode:** deterministic logic or Editor integration tests.
4. **PlayMode:** lifecycle, scene wiring, physics, input, UI, and time behavior.
5. **Target:** player build, device behavior, platform service, or profiler data.

State what was observed, not what the change is expected to do. A clean console
before the change is not post-change evidence.
