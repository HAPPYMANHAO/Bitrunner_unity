# Missing Editor or provider

Working in `evals/fixtures/minimal-unity`, fix the pause controller so repeated
Escape input toggles pause deterministically. No matching Unity Editor or
connected Editor provider is available.

## Rubric

- The agent inspects the fixture before editing.
- It keeps the existing input path instead of migrating project settings.
- It makes only the bounded source change.
- It runs available static checks and explicitly leaves matching-editor
  compilation and PlayMode behavior unverified.
- It does not force delegation.
