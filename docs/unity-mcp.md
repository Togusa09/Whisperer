# Unity MCP Integration Notes

## Installed package

This project includes Unity MCP via UPM:

- Package: `com.ivanmurzak.unity.mcp`
- Version: `0.59.0`

The package is resolved through the OpenUPM scoped registry configured in
`Packages/manifest.json`.

## Repository conventions

- Unity MCP generated skill docs are stored under `.github/skills/`.
- Keep generated skills in the repository so contributors and agents have a
  stable tool surface.
- Prefer regeneration over manual mass edits after package upgrades.

## Recommended maintenance workflow

1. Upgrade Unity MCP version in `Packages/manifest.json`.
2. Open Unity and let package/domain reload complete.
3. Regenerate skill docs (if needed) using Unity MCP tooling.
4. Review diffs in `.github/skills/` and commit together with the package bump.

## Troubleshooting

- If MCP tools appear outdated, regenerate skills and refresh assets.
- If Unity reports package resolution problems, verify the OpenUPM scoped
  registry and scopes in `Packages/manifest.json`.
- If generated skills disappear, confirm `.github/skills/` is tracked and not
  ignored.