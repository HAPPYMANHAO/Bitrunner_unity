# Editor, Serialization, and Source Control

Use this reference for Editor tooling, YAML, importers, source control, CI, and
repository hygiene.

## Serialization identity

Unity asset identity is the GUID in the asset's `.meta` file. Serialized
references also use local `fileID` values for sub-objects. Moving an asset with
its `.meta` preserves identity; regenerating or guessing a GUID can break every
reference.

Treat manual edits to `.unity`, `.prefab`, `.asset`, `.meta`, and
`ProjectSettings/*.asset` as a last resort. Prefer the matching Editor when
relationships or hidden serialized fields are involved. If text editing is
necessary:

- require text serialization and a clean diff;
- understand each object block, owner, GUID, and fileID reference;
- change the smallest field or complete relationship;
- never leave an object block and component list out of sync;
- reopen/reimport in the matching Editor and inspect the console and asset.

Do not transfer serialized fields from a different Unity version without
checking the exact format.

## Editor scripting

Keep Editor code in an Editor-only assembly/folder and runtime code free of
`UnityEditor` references. For mutations, use the project's established Undo,
dirty-state, prefab modification, asset database, and save patterns.

Batch asset editing must have bounded scope and cleanup on failure. Avoid
expensive `OnValidate` work, recursive imports, unconditional asset refresh,
or automatic scene saves. Custom inspectors and property drawers should improve
authoring without hiding runtime ownership.

## Source control

Preserve the repository's existing settings. Typical Unity repositories:

- commit `Assets`, `Packages`, `ProjectSettings`, and all required `.meta`;
- ignore generated `Library`, `Temp`, `Logs`, `obj`, build outputs, and local
  IDE/UserSettings artifacts according to team policy;
- use Git LFS for large binary formats selected by the project, not every asset;
- use UnityYAMLMerge/Smart Merge only when it is configured and compatible with
  the team's merge workflow;
- avoid formatting or regenerating unrelated serialized assets.

Before resolving a scene/prefab conflict, understand both sides' object
identity and intent. A syntactically merged YAML file can still contain broken
references or lost overrides.

## CI and build automation

Reuse the repository's pinned Unity version, package lock state, build methods,
test commands, cache policy, and license setup. Capture Editor logs and return
codes. Keep credentials in the CI secret store and avoid printing activation,
signing, or service tokens.

Separate static checks, EditMode tests, PlayMode tests, player builds, and
target-device checks. Cache restoration does not prove package resolution, and
a successful batchmode exit does not prove the expected player artifact unless
the pipeline verifies it.

For version upgrades, use a separate branch/worktree, back up authored assets,
read upgrade guides and package changelogs, let the exact Editor perform
serialization migrations, and review the resulting serialized diff before
acceptance.
