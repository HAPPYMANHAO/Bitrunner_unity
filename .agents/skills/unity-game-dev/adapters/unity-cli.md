# Unity CLI Adapter

Use this optional adapter when a Unity 6.0 or later project needs live Editor
access through Unity's command-line interface (`unity`). Unity deprecated the
MCP server in the Assistant package (`com.unity.ai.assistant`) from 2.18 and
names the Unity CLI as its replacement. The portable skill does not require the
CLI, and repository-only work must remain possible without it.

The Unity CLI is experimental (1.0.0-beta.10 as of 2026-09-23), and it drives
the Editor through the experimental Unity Pipeline package
(`com.unity.pipeline`). Commands, flags, and the Editor command catalog can
change between releases. Treat `unity --help`, `unity commands --format json`,
and the command list of the connected Editor as authoritative over this summary.
Sources checked 2026-09-23: [Unity CLI as the replacement for the in-Editor MCP
server](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli),
[Use the Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli), the
[Unity CLI
reference](https://docs.unity.com/en-us/unity-cli/unity-cli-reference), [Unity
Pipeline
package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package),
and the [Unity CLI release
notes](https://docs.unity.com/en-us/unity-cli/release-notes).

## Contents

- Prerequisite gate
- Setup that needs authority
- Direct commands or MCP mode
- Session gate
- Mutation and verification
- Troubleshooting order

## Prerequisite gate

Before relying on the CLI, verify:

- `ProjectSettings/ProjectVersion.txt` declares Unity 6000.0 or later; the CLI
  can install and manage older Editors but drives a running Editor only from
  Unity 6.0;
- `unity --version` reports an installed CLI and `unity doctor` reports no
  blocking problem;
- `Packages/manifest.json` or the lock file contains `com.unity.pipeline`, and
  `unity pipeline list` reports it installed;
- if the project also uses `com.unity.ai.assistant`, the resolved version is
  2.13 or later, which fixes a conflict between the CLI and the Assistant
  package.

The CLI runs locally, and its pipeline server accepts connections on localhost
only. Unity documents the CLI as free and separate from Unity AI, so a Unity AI
subscription is not a prerequisite.

## Setup that needs authority

Each of these steps changes the machine, the project, or an agent client beyond
the requested source edit. Ask before running any of them:

- installing or updating the CLI through Unity's install script, the `unity-cli`
  Homebrew cask, the `Unity.CLI` winget package, Unity's apt or dnf repository,
  or `unity self-update`;
- `unity auth login`, which the Pipeline setup steps call for; the user
  completes the sign-in;
- `unity pipeline install`, which adds `com.unity.pipeline` and its dependencies
  to the project and triggers a recompile;
- `unity mcp configure <client>`, which writes the agent client's configuration
  file; for Codex it also relaxes the sandbox network policy so shell commands
  can reach the Editor on localhost. Preview the change with `--dry-run`;
- `unity skill install <client>`, which installs Unity's own CLI skill, plus
  skills shipped by installed CLI plugins, into the client's skill or rules
  directory. With `--local` it also copies the skill bundled in the project's
  `com.unity.pipeline` package. That skill complements this one; installing it
  is still a user-level or project-level change.

## Direct commands or MCP mode

When the agent can run shell commands, prefer `unity command` and `unity eval`.
Unity documents them as faster than MCP and using fewer tokens. Use `unity mcp`,
the CLI's own MCP server over stdio, when the harness cannot run shell commands
or its model does not compose command lines reliably. Unity's deprecation covers
only the Assistant package's in-Editor server, not `unity mcp`. Both modes reach
the same Editor through the Pipeline package.

`unity eval '<expr>'` evaluates C# inside the connected Editor. Treat it as able
to mutate the project, and use it for read-only inspection unless the request
authorizes the change it makes.

## Session gate

Before any call that can change the project, observe and record:

- `unity status`: connected Editors with project path, Unity version, process
  ID, and whether each is `ready` or still `starting`;
- the target project: `unity command`, `unity list`, `unity job`, and `unity
  mcp` match the current directory against registered projects, but pass
  `--project-path` explicitly when more than one Editor is open;
- `unity command` without a name, or `unity list`, for the commands and
  parameter schemas this Editor's Pipeline package registers;
- edit/play state, active scene, compilation and console state, and dirty scenes
  or assets, to the extent the listed commands can show them.

Map the portable workflow to the listed commands by capability, as the core
skill describes. Do not assume a fixed command catalog; it comes from the
installed Pipeline package version.

## Mutation and verification

For an Editor-backed change:

1. Inspect the target and serialized references.
2. Identify unrelated dirty state and stop if it cannot be separated safely.
3. Make one bounded mutation with a listed command.
4. Save only the assets authorized by the request.
5. Wait for import or compilation to settle, and confirm that `unity status`
   reports the Editor `ready` before the next command.
6. Re-read the target, references, and console.
7. Run Play Mode or a target build only when that evidence is required.

A `--detach` call returns a job ID immediately; wait with `unity job wait` or
check `unity job status` before reporting its result. `unity close` saves
nothing, even with `--force`, so never use it to end a session that holds
unsaved work.

`unity test` and `unity build` run the project's tests and builds through an
Editor. They count as project-native evidence only when that Editor matches the
project's declared version. Command success is not proof that Unity compiled,
serialized the intended object, or preserved all references. Report each
observed evidence level separately.

## Troubleshooting order

When the CLI cannot reach the Editor or a command fails, check in this order:

1. Unity compilation errors and package resolution;
2. `unity doctor` and `unity --version`;
3. `unity pipeline list` for the Pipeline package state;
4. `unity status` for the target project and its `ready` or `starting` state;
5. explicit `--project-path` targeting when several Editors are open;
6. an Assistant package older than 2.13 in the same project;
7. for MCP mode, `unity mcp configure --list` for the client's configuration
   status;
8. `unity logs`, then restart the Editor and client if needed.

Do not work around a refused client-configuration write by editing security
settings the user has not approved.
