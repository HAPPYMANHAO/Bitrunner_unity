# Evaluation Record

This directory freezes the prompts, fixture, rubric, and observed results used
to modernize the legacy skill. It is maintenance evidence, not part of the
runtime path.

## Fixture

`fixtures/minimal-unity` models a Unity 6 project with:

- Unity `6000.0.47f1`;
- Input System `1.11.2` installed;
- Unity Test Framework `1.4.5` installed;
- a pause controller that uses legacy `Input.GetKeyDown`.

The fixture deliberately separates package presence from active input
configuration. It is not a compilable full Unity project and must not be used to
claim Editor validation.

## Frozen cases

| Case | Prompt purpose | Expected behavior |
| --- | --- | --- |
| `review-boundary.md` | Review an input-related implementation | Report findings; do not edit |
| `missing-editor-fix.md` | Fix with no matching Editor or MCP available | Make repository-safe change only, then state remaining Editor check |
| `implementation-scope.md` | Add a bounded gameplay behavior | Inspect project conventions; avoid package/settings migration |
| `runtime-ai-versioning.md` | Review Sentis/package naming | Separate display name, package ID, namespace, and installed version |
| `official-mcp-routing.md` | Use an existing official Unity MCP connection | Prefer the configured provider; verify state before one bounded mutation |
| `near-miss-game-design.md` | Engine-neutral design request | Skill must not trigger |
| `near-miss-dotnet.md` | Ordinary non-Unity C# request | Skill must not trigger |
| `near-miss-generic-ml.md` | Generic ML training request | Skill must not trigger |

Hold the prompt, fixture, model, harness, effort, and tool surface fixed when
comparing candidates.

## Observed 2026-07-10

Harness: Codex CLI `0.144`

Model: `gpt-5.6-sol`

Effort: high

| Case | No skill | Legacy skill | Accepted candidate |
| --- | ---: | ---: | ---: |
| Review boundary | Passed, 16,987 tokens | Passed but more prescriptive, 20,042 | Passed, 21,628 |
| Missing Editor/provider | Passed, 16,862 | Forced QA delegation, failed, and hung; interrupted at 29,569 | Passed directly, 25,028 |
| Engine-neutral near miss | n/a | n/a | Did not load |
| Ordinary .NET near miss | n/a | n/a | Did not load |

Token counts are trace metadata, not the acceptance criterion. The accepted
candidate removed the legacy forced delegation regression and preserved correct
authority and fallback behavior.

An earlier candidate attempted an ad-hoc compile with assemblies from a
different Unity installation/template cache. It was rejected after 47,366
tokens. The exact-toolchain rule in the accepted skill is the bounded fix.

## Untested cells

- Claude Code with Claude Fable 5 was unavailable because the local CLI session
  limit had been reached. No result is inferred from the Codex run.
- Connected-Editor mutation and the Coplay adapter examples were not executed
  against a live Unity project.
- Matching-editor compile, PlayMode, player build, and target-device behavior
  were not exercised by this frozen fixture.
- The 2026 AI/MCP/runtime-AI cases were added after the recorded run and remain
  frozen but unexecuted in both primary harnesses.

Re-run those cells before claiming cross-harness behavioral parity or live
Editor compatibility. Future runs target GPT-6 Sol (`gpt-6-sol`) in Codex CLI
0.156.1 or later and Claude Fable 5.1 in Claude Code. Because the model changes,
re-run the no-skill and legacy-skill baselines in the same setup instead of
comparing against the 2026-07-10 numbers above.
