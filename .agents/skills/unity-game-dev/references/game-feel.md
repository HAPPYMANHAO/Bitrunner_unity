# Game Feel

Use this reference when feedback, responsiveness, clarity, tension, or polish is
part of the requested outcome.

Start from one gameplay event or authoritative state. Define:

- what the player should notice;
- when the cue begins, peaks, and ends;
- which system owns the event;
- how repeated or overlapping cues behave;
- what remains perceivable with reduced motion, low audio, color-vision
  differences, or controller-only play.

Layer cues deliberately:

- motion: anticipation, follow-through, recoil, shake, hit stop;
- visual: animation, particles, lighting, material response, UI;
- audio: transient, loop, ambience, mix change;
- timing: delay, easing, cadence, recovery;
- control: buffering, forgiveness, dead zones, aim or camera response.

Do not let several scripts independently trigger the same effect. Route from one
owned event and let presentation systems observe it.

Prefer one measurable before/after scenario over adding every effect at once.
Verify that feedback improves legibility and does not obscure state, cause
motion discomfort, or create performance spikes.
