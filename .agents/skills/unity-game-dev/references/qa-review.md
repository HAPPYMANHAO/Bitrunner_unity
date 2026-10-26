# QA Review Brief

Use for an independent verification pass after implementation or for a focused
bug investigation.

Build a compact evidence matrix:

| Scenario | Setup | Action | Expected | Observed | Evidence |
| --- | --- | --- | --- | --- | --- |

Cover the primary path, one important boundary, and the reproduced failure.
Include scene/prefab wiring, input backend, time state, play/edit lifecycle,
console output, and target platform only when relevant.

Report:

1. blockers and reproducible defects;
2. passed scenarios and their evidence level;
3. untested scenarios and the missing prerequisite;
4. the smallest next diagnostic or fix.

Do not claim PlayMode, device, or package-resolution results from static
inspection. A QA pass does not authorize unrelated fixes.
