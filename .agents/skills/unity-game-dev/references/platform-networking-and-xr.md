# Platform, Networking, Services, and XR

Use this reference for player builds, multiplayer, Unity Gaming Services, XR,
and other package-heavy target integrations.

## Platform builds

Read the exact Unity version's build UI and command-line documentation. Unity 6
lines can use Build Profiles and newer platform tooling, while older projects
may use legacy build settings or custom pipelines. Reuse project-native build
entry points instead of silently migrating them.

For each target, inspect:

- scripting backend, API compatibility, architecture, stripping, and symbols;
- graphics APIs, color space, quality tier, resolution, orientation, and safe
  area;
- permissions, entitlements, signing, keystores, capabilities, and store rules;
- filesystem, persistent-data, locale, input, networking, and suspend/resume;
- native plug-ins, AOT/reflection requirements, link metadata, and package
  compatibility;
- development versus release build flags and target-device profiling.

Credentials, signing assets, store submissions, and cloud project changes
require explicit authority. Do not expose secrets in logs or generated files.

## Multiplayer

Identify the actual stack and version: Netcode for GameObjects/Entities,
Unity Transport, Relay/Lobby, Photon, Mirror, a platform SDK, or custom
networking. Do not combine lifecycle, RPC, ownership, serialization, prefab, or
scene-management rules from different stacks or major versions.

Define:

- authority and trust boundary;
- connection, approval, reconnect, and shutdown lifecycle;
- tick/snapshot model and physics relationship;
- ownership, prediction, interpolation, reconciliation, and lag handling;
- spawnable content and scene synchronization;
- serialization limits, bandwidth budget, rate limits, and abuse protection;
- host migration, save/persistence, and version compatibility when required.

Use installed package source and exact-version docs before writing attributes or
callbacks. A local single-player PlayMode pass does not validate multiplayer
authority or transport behavior.

## Unity Gaming Services and external services

Inspect package versions, linked Unity project/environment, authentication
flow, consent requirements, quotas, region/platform availability, dashboard
configuration, and error/retry guidance. Keep service API calls behind a clear
boundary and make offline/degraded behavior explicit.

Creating environments, changing dashboard settings, uploading content, or
using production credentials are external mutations and need user authority.

## XR

Identify the target device, provider plug-in, OpenXR features, XR Plug-in
Management configuration, XR Interaction Toolkit version, Input System setup,
render pipeline, and performance budget.

XRI 2.x and 3.x differ in namespaces, locomotion architecture, and component
details. Inspect installed package APIs before configuring `XROrigin`,
interactors, interactables, locomotion, haptics, or UI. Keep physics layers
distinct from interaction-layer masks.

Validate:

- tracking origin and camera hierarchy;
- controller/action bindings and device loss;
- direct, ray, socket, grab, teleport, and UI collider requirements;
- comfort options, snap/smooth turn, movement, height, handedness, and seated
  modes;
- target refresh rate, GPU time, resolution scaling, overdraw, and thermal
  behavior;
- focus, pause, recenter, permissions, and platform lifecycle.

XR scene setup and device behavior require Editor and target-device evidence;
serialized inspection alone is insufficient.
