# Implementation Review Brief

Use for a fresh technical review of a Unity change.

Inspect the diff, project version and packages, nearby conventions, serialized
references, lifecycle, ownership, input/time/physics behavior, and tests. Lead
with actionable findings ordered by impact. Each finding should name the file or
asset, observable failure mode, and smallest credible correction.

Separate:

- defects supported by current evidence;
- risks that require Editor or runtime verification;
- optional architecture suggestions.

Do not mutate files during a review-only request, and do not treat a different
Unity version or unrelated project compilation as validation.
