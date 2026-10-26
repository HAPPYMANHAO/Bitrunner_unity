# unity-game-dev

`unity-game-dev` is one cross-harness skill for planning, implementing,
debugging, and reviewing work in an existing Unity project. It teaches the
agent to read the project before choosing an API, preserve the project's
architecture and package choices, and report only the validation it actually
observed.

The repository root is the installable package. The same portable `SKILL.md` is
designed for Codex and Claude Code; live Unity Editor control is optional and
isolated in provider adapters.

## Install

Install the skill at user scope with the
[`skills` CLI](https://github.com/vercel-labs/skills):

```bash
npx skills add gigio1023/unity-game-dev-skill --global
```

The interactive flow lets you choose Codex, Claude Code, or both. Keep the
default symlink mode when installing for both harnesses so they share one
managed package. The repository root contains exactly one installable skill, so
no subdirectory or `--skill` argument is required.

For a non-interactive cross-harness install:

```bash
npx skills add gigio1023/unity-game-dev-skill \
  --global \
  --agent codex \
  --agent claude-code \
  --yes
```

The CLI manages the canonical package and harness links. With the current CLI,
Codex uses `~/.agents/skills/unity-game-dev`; Claude Code receives its supported
user-level installation or symlink. Follow the paths printed by the command
rather than creating links by hand.

To select a harness link target, pass either `--agent codex` or
`--agent claude-code`. In the default symlink mode, that selection does not
isolate the package: `--agent claude-code` installs the canonical package under
`~/.agents/skills`, which Codex also discovers. Use `--copy` when separate
harness installations are actually required. See the
[Codex installation notes](.codex/INSTALL.md) and [Claude Code installation
notes](.claude/INSTALL.md).

Verify or update the managed installation with:

```bash
npx skills list --global
npx skills update unity-game-dev --global
```

Start a new task or refresh skill discovery if a harness built its skill list
before installation. Codex does not require an `AGENTS.md` declaration for this
user-level skill.

Unity Assistant uses its own skill locations and permission model. It does not
discover the Codex or Claude Code installation above. Supporting Unity
Assistant is an optional, separate installation target—not part of the normal
Codex or Claude Code setup. See
[Unity Assistant skills as an optional runtime](references/ai-and-agent-workflows.md#unity-assistant-skills-as-an-optional-runtime).

## What the skill does

For each task, the skill first establishes the local baseline from
`ProjectSettings/ProjectVersion.txt`, package manifests and locks, project
settings, nearby assets and scripts, repository instructions, and worktree
state. It then chooses the least invasive execution path:

| Path | When it fits | Evidence it can establish |
| --- | --- | --- |
| Repository-only | Source or serialized-asset work that is safe without live Editor state | Static inspection and repository-level checks |
| Matching Editor or project automation | The exact project Editor, tests, batchmode, build scripts, or CI is available | Compile, EditMode, PlayMode, build, or profiling evidence actually run |
| Connected Editor provider | A configured provider can inspect or mutate the open project | Bounded scene or prefab changes, console state, and runtime checks supported by that provider |

Repository-only operation is a complete supported path, not a degraded mode.
The agent should finish safe work there and name the smallest remaining Editor
check instead of inventing compile or Play Mode results.

The domain guidance covers gameplay C#, architecture, input, physics, cameras,
UI, audio, animation, scenes and prefabs, serialization, rendering pipelines,
2D and 3D content, NavMesh, networking, services, XR, performance, testing,
builds, source control, and asset licensing. It also distinguishes three Unity
AI concerns that require different workflows:

- authoring agents: Unity Assistant, the Unity CLI, MCP, AI Gateway, and
  connected Editor automation;
- runtime inference: the project's installed Sentis/Inference Engine package
  and model formats;
- training gameplay agents: ML-Agents' Unity environment, Python trainer, and
  exported inference model.

The skill is version-aware through Unity 6 release lines, including Unity 6.6,
the current Supported Update release as of 2026-09-23, but never treats “latest”
as an instruction to upgrade. The target project's exact Editor patch, package
versions, settings, and supported release line remain authoritative. See
[version and package checks](references/version-and-package-checks.md) and [AI
and agent workflows](references/ai-and-agent-workflows.md).

It is not a general game-design advisor, an ordinary C# or machine-learning
skill, a standalone asset generator, or guidance for contributing to Unity's
engine source. It also does not install, authorize, or replace an Editor
provider.

## Optional Editor providers

The portable core describes capabilities—inspect hierarchy, read components,
mutate objects, save intended assets, read logs—rather than hard-coding one
provider's tool names. Use an adapter only when that provider is already
configured or the user has authorized its setup.

- [Unity CLI](adapters/unity-cli.md) is Unity's official, still experimental
  replacement for the Assistant package's MCP server. It drives Unity 6.0+
  Editors through the Unity Pipeline package, either with direct `unity command`
  and `unity eval` calls or as an MCP server through `unity mcp`. Installing the
  CLI or the Pipeline package, configuring a client, and installing the CLI's
  own agent skill each need authorization.
- [Unity Assistant MCP](adapters/unity-ai-mcp.md) covers projects already
  connected through the MCP server in `com.unity.ai.assistant`, which Unity
  deprecated from Assistant 2.18. A direct MCP client starts Unity's relay, and
  the first connection requires user approval. Discover the live tool surface
  instead of assuming built-in tool names remain fixed.
- [Coplay Unity MCP](adapters/coplay-unity-mcp.md) documents the current
  provider-specific setup, capability discovery, and diagnostic workflow for
  projects that use Coplay.

Provider availability does not expand task authority. The agent still inspects
before mutation, preserves unsaved or unrelated work, saves only intended
assets, and distinguishes static inspection from observed Editor evidence.
Third-party integrations must also satisfy the project's organizational,
security, and current Unity access requirements.

## Why one skill

The legacy package exposed 22 independently discoverable skills, copied large
undated manual snapshots, assumed one Editor provider, and forced a
designer-to-programmer-to-QA agent chain. That made activation noisy and could
override better project-local judgment.

This package intentionally keeps one judgment and workflow skill instead of
generating a wrapper skill for every Unity topic or MCP tool. Focused references
load only when needed, provider contracts remain replaceable, and optional
review roles do not control execution. The underlying topic-coverage review is
recorded in the [legacy content audit](docs/legacy-content-audit.md).

## Repository map

- [`SKILL.md`](SKILL.md) — portable task contract and normal execution path
- [`references/`](references/) — focused, version-aware Unity decision guidance
- [`adapters/`](adapters/) — optional Editor-provider and runtime integration
  details
- [`evals/`](evals/) — frozen cases, fixture, rubric, observed results, and
  untested cells
- [`docs/research-sources.md`](docs/research-sources.md) — source priority and
  ignored maintenance checkouts

The installable package remains at the repository root. Research clones live
under the ignored `.research/build/` directory so upstream `SKILL.md` files do
not become package entries.

## Validate the package

Maintainers can verify package discovery from the repository root:

```bash
npx skills add . --list
npx skills add . --list --full-depth
```

Both commands must report exactly one skill named `unity-game-dev`.

Review [the evaluation record](evals/README.md) before making compatibility
claims. It records the harness, model, fixture, observed cases, and untested
cells. In particular, repository fixtures cannot prove a matching-Editor
compile, connected-Editor mutation, Play Mode behavior, player build, or target
device performance.

## Maintain

When Unity or a package changes, update guidance from the narrowest matching
source in this order: the target project, official documentation for its exact
released version, a matching package release, matching reference source, then
development branches for upcoming changes. Community repositories are useful
for comparison and discovery, not as authority for Unity behavior.

Keep raw documentation snapshots and research clones out of the runtime
package. A removed topic must remain covered by the portable contract, a
focused reference, a provider adapter, deterministic validation, or an explicit
evidence-backed rejection. Refresh procedures and pinned research provenance
live in [the research-source record](docs/research-sources.md).
