# Codex

Install the skill globally for Codex:

```bash
npx skills add gigio1023/unity-game-dev-skill --global --agent codex
```

The `skills` CLI manages the canonical package. Codex discovers the installed
user skill automatically, so no `AGENTS.md` declaration or manual symlink is
required. The shared `SKILL.md` contains no Codex-only tool or subagent
requirement; optional Editor providers are under `adapters/`.

For verification and refresh notes, follow
[`.codex/INSTALL.md`](../.codex/INSTALL.md).
