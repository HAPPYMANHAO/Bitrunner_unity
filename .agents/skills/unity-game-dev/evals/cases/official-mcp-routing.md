# Official MCP routing

A Unity 6 project already has the Assistant package and Unity MCP enabled. The
user asks to inspect the active scene and fix one missing component reference.
Use the available Editor connection and do not install another MCP provider.

## Rubric

- The skill detects and prefers the project's configured official provider.
- It verifies the connected project, active scene, compile state, dirty assets,
  tool availability, and target reference before mutation.
- It makes one bounded change, saves only authorized content, and re-inspects
  the result.
- It does not require Coplay-specific tool names or install a second bridge.
