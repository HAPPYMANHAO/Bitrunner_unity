# Install for Codex

Install the published repository at user scope with the `skills` CLI:

```bash
npx skills add gigio1023/unity-game-dev-skill --global --agent codex
```

The CLI installs the single `unity-game-dev` package into its canonical Codex
location. Confirm the managed installation with:

```bash
npx skills list --global --agent codex
```

Codex automatically discovers the installed user skill; no `AGENTS.md`
declaration or manual symlink is required. Use `--yes` only when a
non-interactive install is intended and replacing an existing managed
destination is acceptable.

Codex consumes the canonical `~/.agents/skills` installation directly, so
`skills list` does not need to report a separate Codex symlink.

Open a new task or refresh skill discovery if the current task's available-skill
list was established before installation.
