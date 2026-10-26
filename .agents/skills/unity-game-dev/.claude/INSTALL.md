# Install for Claude Code

Select Claude Code as the link target with the `skills` CLI:

```bash
npx skills add gigio1023/unity-game-dev-skill --global --agent claude-code
```

The default symlink mode selects a link target; it does not isolate the
package. Codex also discovers the canonical
`~/.agents/skills/unity-game-dev` package. Use `--copy` when a separate Claude
Code installation is actually required.

Confirm the managed installation with:

```bash
npx skills list --global --agent claude-code
```

For a shared Codex and Claude Code installation, use the cross-harness command
in the repository README and keep the CLI's default symlink mode. Use `--yes`
only for an intentionally non-interactive install.
