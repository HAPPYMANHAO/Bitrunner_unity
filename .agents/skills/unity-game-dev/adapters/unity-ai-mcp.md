# Unity Assistant MCP Adapter (Deprecated)

Use this optional adapter only when the project is already connected through the
Unity MCP server in Unity's `com.unity.ai.assistant` package and an external MCP
client needs live Editor access. Unity deprecated this server from Assistant
2.18 and names the Unity CLI as its replacement, so set up new connections with
the [Unity CLI adapter](unity-cli.md) instead. The portable skill does not
require either path, and repository-only work must remain possible without them.

The package is pre-release and its tools can change between versions. Inspect
the installed package, current Project Settings, and live MCP schema instead of
treating examples in this file as a stable API catalog. The notes below were
rechecked against the 2.19 documentation on 2026-09-23.

## Contents

- Prerequisite gate
- Architecture and client setup
- Session gate
- Capability mapping
- Mutation and verification
- Custom tools
- Troubleshooting order

## Prerequisite gate

Before configuring a client, verify:

- `ProjectSettings/ProjectVersion.txt` declares Unity 6000.0 or later;
- `Packages/manifest.json` contains `com.unity.ai.assistant`, and the lock file
  identifies the resolved version;
- the Editor opens the intended project without compilation errors;
- **Edit > Project Settings > AI > Unity MCP Server** shows the bridge as
  running;
- the current account, subscription or trial, accepted terms, and Cloud-project
  state satisfy the package UI's current requirements.

Do not add or upgrade the package, accept terms, link a Cloud project, or start
a paid trial unless the user has authorized that change. Unity documents the
current baseline in [Get started with MCP
server](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/unity-mcp-get-started.html)
and lists product-level requirements on the [Unity AI page](https://unity.com/features/ai).

## Architecture and client setup

Unity's MCP bridge runs inside the Editor and opens a local IPC channel. The MCP
client starts Unity's relay as a stdio server; the relay connects to the bridge:

```text
external MCP client -> stdio relay -> local IPC -> Unity Editor bridge
```

The Editor installs relay executables under `~/.unity/relay/`. Use the
platform-specific path shown by the installed package's Integrations panel and
pass `--mcp`. Prefer the panel's generated configuration because executable
names and client configuration formats may change. Some clients do not expand
`~`, so use an absolute home path when required.

If several Editors are open, target the intended instance explicitly. The 2.19
package, like 2.11, supports `--project-path` / `UNITY_PROJECT_PATH` and
`--instance-id` / `UNITY_INSTANCE_ID`; command-line targeting takes precedence
over environment variables. Confirm the connected project after launch instead
of assuming the first discovered Editor is correct.

Direct external clients require approval on first connection. Review the
pending client in Unity MCP Server settings and accept only the expected client.
Unity remembers approved clients. Connections routed from Assistant through AI
Gateway are approved automatically, which is a different trust path. See the
[architecture and connection
model](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/unity-mcp-overview.html).

## Session gate

Before any tool call that can change the project, observe and record:

- connected client identity and the loaded project path;
- Editor process or instance when more than one is running;
- current edit/play state and active scene;
- compilation and console state;
- enabled tools and their live input/output schemas;
- dirty scenes, assets, and unrelated unsaved work to the extent the current
  tool surface can show them.

Approval proves client identity, not user intent for every mutation. Apply the
portable skill's authority and save gates after the connection is approved.

## Capability mapping

Discover the current tool list, then map the core workflow to capabilities that
can:

- inspect scenes, hierarchy, GameObjects, components, assets, scripts, and
  console output;
- make a bounded scene, asset, or script change;
- report compilation, import, or console results;
- save only the intended scene or asset;
- enter and leave Play Mode when runtime evidence is necessary;
- register or invoke project-specific custom tools when the project owns them.

Unity's 2.19 documentation uses names such as `Unity.ManageScene`,
`Unity.ManageGameObject`, and `Unity_ReadConsole` as examples, where the 2.11
documentation wrote `Unity_ManageScene` and `Unity_ManageGameObject`. They are
examples, not a contract for another package version. Do not invent a call when
the live client does not expose the matching schema.

## Mutation and verification

For an Editor-backed change:

1. Inspect the target and serialized references.
2. Identify unrelated dirty state and stop if it cannot be separated safely.
3. Make one bounded mutation using the live schema.
4. Register Undo or use the package's current reversible path when available.
5. Save only the assets authorized by the request.
6. Wait for import or compilation to settle.
7. Re-read the target, references, and console.
8. Run Play Mode or a target build only when that evidence is required.

Tool success is not proof that Unity compiled, serialized the intended object,
or preserved all references. Report each observed evidence level separately.

## Custom tools

Prefer a small, typed custom tool when a repeated project operation is too
fragile for open-ended scene or C# manipulation. The current package can
discover tools from `[McpTool]`, typed parameter schemas, class implementations,
or runtime registration. These APIs belong to the installed Assistant package;
verify its local documentation and assembly references before adding code.

Custom tools can mutate the project with the caller's authority. Give them
narrow inputs, structured results, explicit failures, Undo support where
applicable, and no implicit save or package installation. Unity's current API is
documented in [Register custom MCP
tools](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/unity-mcp-tool-registration.html).

## Troubleshooting order

When the bridge or tools fail, check in this order:

1. Unity compilation errors and package resolution;
2. bridge status in Unity MCP Server settings;
3. relay existence, executable path, and the required `--mcp` flag;
4. explicit project/PID targeting when multiple Editors are open;
5. pending client approval;
6. disabled tools or custom-tool discovery errors;
7. console/debug logs, then restart the Editor and client if needed.

Do not bypass approval or replace the relay with an unverified binary. The
[official troubleshooting
guide](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/troubleshoot/unity-mcp-troubleshooting.html)
is versioned with the package and should outrank this summary.

### UI inspection hazard in 2.11 through 2.19

Unity's troubleshooting guide, from 2.11.0-pre.2 through 2.19.0-pre.2, warns
that calling the documented `get_components` operation on Canvas objects or UI
hierarchies can freeze or crash the Editor because some UI properties cause
invalid transform state or unbounded serialization. For those package versions,
avoid the operation on `Canvas`, `CanvasScaler`, `GraphicRaycaster`, and
`RectTransform` targets. Check the installed package's troubleshooting page
before assuming the limitation is fixed or applies unchanged to another version.
