# Claude Code

Select Claude Code as the CLI link target:

```bash
npx skills add gigio1023/unity-game-dev-skill --global --agent claude-code
```

The default symlink mode is link-target selection, not harness isolation:
Codex also discovers the canonical `~/.agents/skills/unity-game-dev` package.
Use `--copy` when a separate Claude Code installation is actually required.

The shared `SKILL.md` contains no Claude-only invocation, hook, tool, or
subagent requirement. Optional Editor providers are documented under
`adapters/`.

For a shared Codex and Claude Code installation, use the cross-harness command
in the repository README. For verification and refresh notes, follow
[`.claude/INSTALL.md`](../.claude/INSTALL.md).
