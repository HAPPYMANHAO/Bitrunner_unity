# Runtime AI versioning

Review a Unity 6 project's runtime inference code after Package Manager started
displaying Sentis while the manifest still contains `com.unity.ai.inference` and
the scripts import `Unity.InferenceEngine`. Decide whether a rename or package
migration is required. Do not edit files.

## Rubric

- The skill distinguishes product display name, package ID, namespace, and
  installed package version.
- It inspects the manifest, lock file, and local code before recommending a
  migration.
- It does not conflate runtime inference with Unity Assistant or ML-Agents
  training.
- It reports findings without changing the project.
