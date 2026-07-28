---
coverage: Razor-layer (src/Razor) known issues, quirks & workarounds
---

# Razor — Known Issues

Layer-specific quirks for Razor tooling/compiler. Load when working under
`src/Razor`. Cross-cutting issues live in `.github/memory/KNOWN_ISSUES.md`.

## Build wrappers can mangle a semicolon-delimited `-projects` list

**Affected area:** `build.cmd` / PowerShell build wrappers
**Description:** Passing a semicolon-delimited project list through a nested
PowerShell invocation can make PowerShell treat `;` as a statement separator and
even open `.csproj` files in Visual Studio.
**Workaround:** Build a single project at a time, or invoke the underlying script
so the full `-projects` value is preserved as one argument.

## Shared projects need `.projitems` entries

**Affected area:** `Microsoft.CodeAnalysis.Razor.CohostingShared` (and its
`.UnitTests`) shared projects
**Description:** Files under
`src\Razor\src\Razor\src\Microsoft.CodeAnalysis.Razor.CohostingShared\` and
`src\Razor\src\Razor\test\Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests\`
are compiled through their `.projitems` files. Adding a new `.cs` file to the
shared tree is not enough on its own.
**Workaround:** Also add a matching `<Compile Include="...">` entry to
`Microsoft.CodeAnalysis.Razor.CohostingShared.projitems` or
`Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests.projitems`, or the file
won't be built/tested by the importing projects.

## Duplicate global analyzer config keys (`MultipleGlobalAnalyzerKeys`)

**Affected area:** Razor projects, which receive global configs from both
`eng/config/globalconfigs/*.globalconfig` and `src/Razor/*.globalconfig` overlays
**Description:** The same key in two global configs causes compiler error
`MultipleGlobalAnalyzerKeys`, and the key is left unset.
**Workaround:** Don't redefine a key already present in the base config.
