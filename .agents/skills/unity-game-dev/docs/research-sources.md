# Unity Research Sources

The local `.research/` directory is ignored by Git and is not part of the skill
payload. Checkouts live under `.research/build/` because the package validator
and skills CLI already exclude `build` directories from recursive discovery;
upstream `SKILL.md` files therefore cannot become installable entries.

## Local checkout

The following shallow clones were created on 2026-07-10:

| Directory | Source | Checked commit | What it proves |
| --- | --- | --- | --- |
| `.research/build/UnityCsReference` | [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference) | `979bc204a0c6506d87595a02fc89452687ed820d` | Unity 6000.7.0a2 C# reference source; API/source inspection only |
| `.research/build/InputSystem` | [InputSystem](https://github.com/Unity-Technologies/InputSystem) | `e4920593b2fb89543390b9d4d1a7fabb88fc2af1` | Current development source and `Documentation~`; package reports 1.20.0 |
| `.research/build/Cinemachine` | [Cinemachine](https://github.com/Unity-Technologies/com.unity.cinemachine) | `cecef108f04547f216c5c81f866c9c8c8e2c8157` | Current development source and `Documentation~`; package reports 3.1.8-pre.1 |
| `.research/build/Graphics` | [Graphics](https://github.com/Unity-Technologies/Graphics) | `a7e4c051d256a781ab362c64316b125a1e104694` | Sparse checkout of Core/URP/HDRP/Shader Graph/VFX docs; packages report 17.6.0 |
| `.research/build/Unity-Skills` | [Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills) | `f6f09e6bb001aaf0c16dc2309062edfd10d124b7` | Community upstream and legacy-content comparison only |
| `.research/build/coplay-unity-mcp` | [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | `484937c4a75709f4c632b15d933e79556c3c3a2d` | Current v10-era provider code, transport/reload behavior, tool groups, tests, and bundled skill |
| `.research/build/ivan-unity-mcp` | [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | `dae6da830de3cda2a6aa80b123d2ef7483a3ebd4` | Generated per-tool skill approach, CLI setup, async completion, and multi-version tests |
| `.research/build/akiojin-unity-cli` | [akiojin/unity-cli](https://github.com/akiojin/unity-cli) | `8daf7fa8cee861686b3ade87fa965f89f43f99fe` | Cross-harness workflow-skill layout and executable 22-rule skill linter |
| `.research/build/signal-loop-unity-code-mcp` | [Signal-Loop/UnityCodeMCPServer](https://github.com/Signal-Loop/UnityCodeMCPServer) | `1bdc94ffda6b6f7af5adb5b5f2eb4b6ee44ec2e4` | Arbitrary-C# and closed-loop game-playing skill comparison |
| `.research/build/ml-agents` | [Unity-Technologies/ml-agents](https://github.com/Unity-Technologies/ml-agents) | `a2777719560e4676be99e2ea128c5eb1fbeb3dbb` | Sparse Release 23 checkout; Unity package 4.0.0 and Inference Engine dependency boundary |
| `.research/build/sentis-samples` | [Unity-Technologies/sentis-samples](https://github.com/Unity-Technologies/sentis-samples) | `d44c130a802dad351426d58c0931319e1731ca0a` | Official sample code for `com.unity.ai.inference` and `Unity.InferenceEngine` runtime usage |
| `.research/build/unity-ar-skills` | [janifica/unity-ar-skills](https://github.com/janifica/unity-ar-skills) | `c4a7ba7df46110512aa3bb1c9663f697099b3156` | Focused Unity AR skill structure and version-sensitive reference comparison |

These are moving development branches, not compatibility guarantees for a
project. In particular, UnityCsReference HEAD is an alpha editor line and
Cinemachine HEAD is a pre-release package. Match the project's exact editor and
package versions before using an API.

`UnityCsReference` here means only Unity's public reference repository under its
current license. Never copy subscription, paid, or confidential Unity source
into these checkouts or expose it to an agent unless current Editor Source Code
[Terms](https://unity.com/legal/editor-source-code-terms) and organization
authorization explicitly permit that use.

Additional comparison-only checkouts include AnkleBreaker Unity MCP,
isuzu-shiranui UnityMCP, Jahro's Unity agent skills, and wshobson/agents. They
inform ecosystem comparison but are not technical authorities for this skill.

## Recreate the checkout

Run from this repository root:

```bash
mkdir -p .research/build
git clone --depth 1 --filter=blob:none https://github.com/Unity-Technologies/UnityCsReference.git .research/build/UnityCsReference
git clone --depth 1 --filter=blob:none https://github.com/Unity-Technologies/InputSystem.git .research/build/InputSystem
git clone --depth 1 --filter=blob:none https://github.com/Unity-Technologies/com.unity.cinemachine.git .research/build/Cinemachine
git clone --depth 1 --filter=blob:none --sparse https://github.com/Unity-Technologies/Graphics.git .research/build/Graphics
git clone --depth 1 --filter=blob:none https://github.com/Besty0728/Unity-Skills.git .research/build/Unity-Skills
git clone --depth 1 --filter=blob:none https://github.com/CoplayDev/unity-mcp.git .research/build/coplay-unity-mcp
git clone --depth 1 --filter=blob:none https://github.com/IvanMurzak/Unity-MCP.git .research/build/ivan-unity-mcp
git clone --depth 1 --filter=blob:none https://github.com/akiojin/unity-cli.git .research/build/akiojin-unity-cli
git clone --depth 1 --filter=blob:none https://github.com/Signal-Loop/UnityCodeMCPServer.git .research/build/signal-loop-unity-code-mcp
git clone --depth 1 --filter=blob:none --branch release_23 --sparse https://github.com/Unity-Technologies/ml-agents.git .research/build/ml-agents
git clone --depth 1 --filter=blob:none --sparse https://github.com/Unity-Technologies/sentis-samples.git .research/build/sentis-samples
git clone --depth 1 --filter=blob:none https://github.com/janifica/unity-ar-skills.git .research/build/unity-ar-skills
```

Then select only the Graphics documentation trees:

```bash
git -C .research/build/Graphics sparse-checkout set \
  Packages/com.unity.render-pipelines.core/Documentation~ \
  Packages/com.unity.render-pipelines.universal/Documentation~ \
  Packages/com.unity.render-pipelines.high-definition/Documentation~ \
  Packages/com.unity.shadergraph/Documentation~ \
  Packages/com.unity.visualeffectgraph/Documentation~
git -C .research/build/ml-agents sparse-checkout set com.unity.ml-agents docs
git -C .research/build/sentis-samples sparse-checkout set \
  Documentation ChatSample BoardGameAISample TextToSpeechSample
```

## Source priority

Use sources in this order:

1. the target project's `ProjectVersion.txt`, manifest, lock file, settings,
   package cache, local source, and existing code;
2. Unity Manual and package documentation for that exact released version;
3. a matching release tag/branch in the official package repository;
4. matching UnityCsReference source when public;
5. current development branches only to understand upcoming changes;
6. community repositories as comparison or discovery, never as authority.

Unity 6 uses LTS and Supported Update release lines. “Latest” is not a universal
upgrade instruction: choose the line appropriate to the project, then verify its
exact patch and package set. See Unity's
[release support policy](https://unity.com/releases/unity-6/support).

## Refresh

Because the clones are shallow and ignored, update them explicitly:

```bash
git -C .research/build/UnityCsReference pull --ff-only
git -C .research/build/InputSystem pull --ff-only
git -C .research/build/Cinemachine pull --ff-only
git -C .research/build/Graphics pull --ff-only
git -C .research/build/Unity-Skills pull --ff-only
git -C .research/build/coplay-unity-mcp pull --ff-only
git -C .research/build/ivan-unity-mcp pull --ff-only
git -C .research/build/akiojin-unity-cli pull --ff-only
git -C .research/build/signal-loop-unity-code-mcp pull --ff-only
git -C .research/build/ml-agents pull --ff-only
git -C .research/build/sentis-samples pull --ff-only
git -C .research/build/unity-ar-skills pull --ff-only
```

After refreshing, record new commit IDs and dates here only when a new skill
audit actually relies on them.
