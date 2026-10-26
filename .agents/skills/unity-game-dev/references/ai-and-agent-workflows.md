# AI and Agent Workflows

Use this reference when a Unity task involves authoring agents, Editor MCP,
generated assets, on-device neural inference, or ML-Agents training. These are
different systems with different package, data, runtime, and validation
boundaries.

Sources reviewed 2026-07-10; the authoring-agent, Assistant-skill, and ML-Agents
sources were rechecked on 2026-09-23 against Unity Assistant 2.19, Unity CLI,
and ML-Agents 4.1 documentation. Assistant documentation is pre-release and the
Unity CLI is experimental, so the project's manifest, lock file, package-local
documentation, and current terms outrank the versions summarized here.

## Contents

- Choose the correct AI path
- Establish exact versions and authority
- Authoring AI: Assistant, Gateway, MCP, and Generators
- Unity Assistant skills as an optional runtime
- Runtime inference with Sentis
- Training gameplay agents with ML-Agents
- Generated assets, model provenance, and data
- Agentic access and third-party integrations
- Validation and reporting

## Choose the correct AI path

| User outcome | Unity system | Runs where | Do not confuse with |
| --- | --- | --- | --- |
| Inspect or modify a project with an authoring agent | Assistant, AI Gateway, Unity CLI, or the deprecated Unity MCP server | Editor and optional external client/service | Runtime game AI |
| Generate prototype sprites, textures, audio, animation, or layouts | Unity AI Generators | Editor plus generation service | A cleared shipping asset |
| Run a trained neural network in a player | Sentis | Unity runtime on the target device | Model training |
| Train a policy from observations, actions, and rewards | ML-Agents Toolkit | Unity environment plus Python trainer | Assistant or MCP automation |

Route by the requested outcome. Installing an authoring assistant does not add
runtime inference, and adding Sentis does not create an ML-Agents training
pipeline.

## Establish exact versions and authority

Inspect before recommending code or configuration:

- `ProjectSettings/ProjectVersion.txt`;
- `Packages/manifest.json` and `Packages/packages-lock.json`;
- installed package `Documentation~`, samples, changelog, and assemblies;
- existing model assets, training configuration, generated-asset records, and
  project-specific Editor integrations;
- organization rules for external services, source disclosure, generated
  content, credentials, and player data.

Adding an AI package, model file, Python environment, Cloud link, external
service, or generated asset changes project or external state. Obtain authority
for those actions. Never upload proprietary code, art, credentials, personal
data, or restricted assets to a model service merely because an Editor feature
can attach them.

Use exact installed documentation. Product pages describe the current service;
package documentation describes one package line; neither proves compatibility
with another project version.

## Authoring AI: Assistant, Gateway, MCP, and Generators

Unity AI's authoring surfaces are related but not interchangeable:

- **Assistant** is Unity's in-Editor agent surface. Current package docs cover
  project context, screenshots, Editor actions, custom tools, skills, and an API
  for windowed or headless runs.
- **AI Gateway** runs supported third-party coding agents from the Assistant
  window using the provider's own authentication. It is a hosted/in-Editor
  routing experience, not the same session as an independently launched Codex
  or Claude Code client.
- **Unity CLI** (`unity`) drives a running Unity 6.0+ Editor through the
  experimental Unity Pipeline package, either with direct `unity command` and
  `unity eval` calls or as an MCP server through `unity mcp`. Unity names it the
  replacement for the Assistant package's MCP server; apply the setup and
  mutation gates in the [Unity CLI adapter](../adapters/unity-cli.md).
- **Unity MCP server** in the Assistant package exposes the Editor to an
  external MCP client through Unity's local relay and bridge. Unity deprecated
  it from Assistant 2.18; keep it only for projects already connected through
  it, and apply the connection and mutation gates in the [Assistant MCP
  adapter](../adapters/unity-ai-mcp.md).
- **Generators** create prototype assets from prompts or references. They have
  service, model, provenance, rights, and disclosure considerations beyond a
  normal local asset import.

The current integration map is in [Integrate models, skills, and
tools](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/integration-landing.html).
The [AI Gateway
overview](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/ai-gateway-intro.html)
and [Unity MCP
overview](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/integration/unity-mcp-overview.html)
define the Assistant package's two connection paths, and [Unity CLI as the
replacement for the in-Editor MCP
server](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
covers the move to the CLI.

Do not promise that a particular provider, model, command, built-in tool name,
credit policy, or entitlement remains available. Inspect the current package UI
and provider configuration. Keep secrets in the provider's supported credential
store rather than project files, prompts, logs, or skill resources.

## Unity Assistant skills as an optional runtime

Unity Assistant 2.19, like 2.11, can discover filesystem skills built around
`SKILL.md` and progressive disclosure. This makes Unity Assistant a possible
third runtime for some skill content, but it does not make a Codex/Claude Code
installation automatically visible to Unity. The Unity CLI's `unity skill
install <client>` is a different mechanism: it installs Unity's own CLI skill
into an external client such as Codex or Claude Code, not into Assistant.

Assistant scans:

- any `SKILL.md` below a project's `Assets/` directory;
- the user folder documented for each OS, including
  `~/Library/Application Support/Unity/AIAssistantSkills` on macOS;
- `Packages/<package>/AIAssistantSkills/<skill>/SKILL.md` in installed packages.

Newly discovered skills default to **Deny** until the user reviews and changes
them to **Allow** in Unity's AI Skills preferences. Assistant requires `name` and
`description` and also supports Unity-only frontmatter such as
`required_packages`, `required_editor_version`, `enabled`, and `tools`.

Keep this repository's shared skill at the portable Codex/Claude Code
intersection. Do not add Unity-only fields to the common frontmatter or assume
Unity's built-in actions, custom-tool attributes, installation paths, or
permission UI exist in either primary harness. If Unity Assistant support is
packaged later, treat it as a separate adapter/install target and validate it in
the Editor. See [About
skills](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/skills/skills-overview.html)
and [Create skills from the
filesystem](https://docs.unity3d.com/Packages/com.unity.ai.assistant%402.19/manual/skills/skills-filesystem.html).

## Runtime inference with Sentis

Sentis runs already-trained neural networks in Unity players. It does not train
models. In the current 2.6 line:

- the package ID is `com.unity.ai.inference`;
- the Package Manager display name is **Sentis**; it changed from **Inference
  Engine** in 2.4 without changing the package identity;
- runtime APIs use the `Unity.InferenceEngine` namespace;
- 2.6.1 documentation covers ONNX opsets 7-25, most LiteRT models, and PyTorch
  exported programs decomposed to supported Core ATen operators;
- workers can use CPU, GPU compute, or GPU pixel backends, subject to model,
  operator, platform, and device support.

Read the [Sentis 2.6 overview](https://docs.unity3d.com/Packages/com.unity.ai.inference%402.6/manual/index.html),
[supported-model constraints](https://docs.unity3d.com/Packages/com.unity.ai.inference%402.6/manual/supported-models.html),
and [worker/backend guidance](https://docs.unity3d.com/Packages/com.unity.ai.inference%402.6/manual/create-an-engine.html)
only when they match the project's resolved line. Older Barracuda, Sentis 1.x,
and early Inference Engine examples can use different types and namespaces.

For an inference change, verify:

1. model source, exact version or hash, license, and redistribution terms;
2. import success and unsupported or converted operators;
3. input names, shapes, layouts, ranges, tokenization or preprocessing;
4. output names, shapes, decoding, and postprocessing;
5. selected backend and target-device support;
6. worker/tensor lifetime and memory ownership for the installed API;
7. frame scheduling, synchronization, readback cost, allocation, and warm-up;
8. deterministic fixtures or tolerance-based expected outputs;
9. profiling on the target device, not only in the Editor.

Do not select `GPUCompute` solely because it is usually fast. Small models or
CPU-resident data can favor CPU, while some targets lack compute-shader support.
An imported model is not evidence that every layer executes correctly on the
chosen backend.

## Training gameplay agents with ML-Agents

ML-Agents turns a Unity scene into a learning environment. Its Unity C# package
defines agents, observations, actions, rewards, behaviors, demonstrations, and
the environment connection. The companion Python package contains the training
algorithms. A trained policy can then be embedded for inference through Sentis.

The current 4.1 documentation describes `com.unity.ml-agents` 4.1.0 for Unity
6000.0 or later and pairs its package-install path with Python 3.10.12 and
`mlagents==1.1.0`. Those numbers are not a universal 4.x rule: use the project's
resolved package and its release compatibility table before creating an
environment. The [ML-Agents
overview](https://docs.unity3d.com/Packages/com.unity.ml-agents%404.1/manual/index.html)
defines the C#/Python boundary; the [installation
guide](https://docs.unity3d.com/Packages/com.unity.ml-agents%404.1/manual/Installation.html)
records the current version pair.

For a training change, preserve and validate:

- observation shape, normalization, stacking, and sensor order;
- discrete or continuous action semantics and masks;
- reward scale, timing, terminal conditions, and episode reset completeness;
- `BehaviorName`, behavior parameters, trainer config, and checkpoint pairing;
- reproducible seeds where supported and several-run variance where it matters;
- headless/build execution separately from Editor-only success;
- exported model compatibility and runtime behavior after embedding.

Reward improvement is not proof of useful behavior. Inspect learning curves,
evaluation episodes, exploit strategies, reset leakage, generalization, and the
player/runtime performance of the exported policy.

## Generated assets, model provenance, and data

Unity states that Generator outputs carry embedded metadata marking them as
AI-generated. Treat that tag as a discovery aid, not proof of authorship,
license clearance, storefront compliance, or suitability for release. Keep a
separate provenance record containing the generator, model/provider, date,
prompt/reference sources when allowed, applicable terms, edits, and review
decision; conversions and exports can lose embedded metadata.

Unity's current guidance says users own their input and output data while also
remaining responsible for third-party rights, acceptable use, and generated
asset suitability. Partner models and their terms can change. Verify the
selected generator's current model terms, reference-asset rights, organization
policy, and destination store's disclosure rules before shipping. During beta,
also review the current Evaluation Version and click-through terms rather than
assuming prototype output is cleared for commercial use.

Primary maintenance sources:

- [Unity AI product requirements and generated-asset FAQ](https://unity.com/features/ai)
- [Unity AI Guiding Principles](https://unity.com/legal/unityai-guiding-principles)
- [Unity AI open beta overview](https://unity.com/blog/unity-ai-how-to-get-started)

For third-party neural models used with Sentis or ML-Agents, preserve the model
card, weights license, dataset restrictions when relevant, source URL, version
or commit, conversion steps, and notices. “Open source,” “downloadable,” and
“validated for Sentis” do not by themselves grant redistribution rights.

## Agentic access and third-party integrations

Unity revised its Terms of Service on 2026-06-30 to define **Authorized Agentic
Access** and to address access by AI agents, automated systems, CLIs, and MCP
clients or servers. Unity's Core Standards page also says third-party AI tools
and MCPs are subject to those standards even when distributed outside the Asset
Store or Package Manager.

This reference does not decide whether a particular local tool, package,
workflow, or organization agreement is authorized. Before installing or using
a third-party Editor integration:

- review the current [Unity Terms of Service](https://unity.com/legal/terms-of-service),
  especially the agentic-access definition and use restrictions;
- review [Unity Core Standards](https://unity.com/core-standards) and any current
  package signing, distribution, allowlist, documentation, or authentication
  requirements;
- check applicable Additional, Commercial, enterprise, package, and provider
  terms;
- confirm the tool's present authorization and security posture through a
  Unity-published source or the organization's responsible reviewer.

A public GitHub repository, Asset Store listing, package signature, or previous
successful connection does not alone establish every required permission. Do
not describe a third-party integration as approved, prohibited, compliant, or
illegal without current authoritative evidence and qualified review.

## Validation and reporting

Report the AI path used, exact Editor and package versions, external provider or
model when material, data sent outside the project, assets or settings changed,
and the highest evidence actually observed.

- For authoring agents, verify the connected project, live tool schema, intended
  mutation, compile/import state, saved assets, and runtime behavior when needed.
- For Sentis, verify import, expected outputs, backend support, allocations, and
  target-device performance.
- For ML-Agents, verify environment resets, training compatibility, evaluation
  behavior, exported-model inference, and run-to-run uncertainty.
- For generated assets, verify provenance, rights review status, metadata or
  project labels, and required destination disclosures.

If exact package access, target hardware, provider authorization, model license,
or Editor evidence is unavailable, finish repository-safe work when possible
and state the smallest unresolved check. Do not turn a documentation match into
a claim that the project compiled, trained, generated, or ran successfully.
