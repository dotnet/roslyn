---
name: validate-sdk
description: Install and validate a .NET SDK from an Azure DevOps internal build. Use this when asked to install, validate, or set up a .NET SDK from a dnceng/internal Azure DevOps build link or build ID.
---

# Install and Validate .NET SDK from Azure DevOps Internal Build

Follow these steps to install and validate a .NET SDK from an internal Azure DevOps build (typically from `dnceng/internal`).

This skill can be run from **any directory**, including inside a repo like dotnet/roslyn. The SDK is installed to a temp folder and validation runs in a separate temp folder, so nothing in the user's working directory is created, modified, or depended upon.

This skill requires the **ADO MCP server** configured for the `dnceng` organization to be available in your session.

## How to Invoke This Skill

In a copilot session with an ADO MCP server configured for the `dnceng` organization, ask something like:
- "Install the .NET SDK from this build: https://dev.azure.com/dnceng/internal/_build/results?buildId=2919304"
- "Install .NET SDK from ADO build 2919304"
- "Use /validate-sdk to set up the SDK from build 2919304"

### Required Input

The user must provide **one** of the following:
- An **Azure DevOps build URL** from `dev.azure.com/dnceng/internal` containing a `buildId` query parameter
- A **numeric build ID** (e.g., `2919304`) from the dnceng/internal project

### What Does NOT Work as Input

- **SharePoint/OneNote links** (e.g., `microsoft.sharepoint.com/...`) — these are documentation pages, not build links. If the user provides one, ask them for the Azure DevOps build URL or build ID instead.
- **GitHub links** — this skill is for Azure DevOps builds, not GitHub Actions.
- **NuGet package versions** — this skill installs the full SDK, not individual packages.

If the user doesn't have the build URL or ID, suggest they look for a link like:
`https://dev.azure.com/dnceng/internal/_build/results?buildId=XXXXXXX`
in their signoff document or validation instructions.

## Step 1: Get the Build ID

Extract the `buildId` from the Azure DevOps URL. For example, from:
`https://dev.azure.com/dnceng/internal/_build/results?buildId=2919304&view=artifacts`
The build ID is `2919304`.

## Step 2: Get the SDK Version from the Build

Use the ADO MCP tool `ado-pipelines_get_build_status` with `project: "internal"` and the `buildId` to retrieve the build report. The build number in the report contains the SDK version. Parse out the version portion (e.g., `11.0.100-preview.2.26154.117` from build number `20260305.1-11.0.100-preview.2.26154.117-304630`).

The SDK version follows the pattern `X.Y.Z-preview.N.NNNNN.NNN` and appears between the date prefix and the trailing BAR build ID.

## Step 3: Install Using dotnet-install Script

Download and run the official dotnet-install script with the exact SDK version. Install into a **temp folder** so there is no dependency on the user's working directory or any repo:

```powershell
$sdkDir = "$env:TEMP\validate-sdk-dotnet"
$installScript = "$env:TEMP\dotnet-install.ps1"
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript

# Install the specific SDK version to a temp directory
& $installScript -Version "<SDK_VERSION>" -InstallDir $sdkDir
```

The script will automatically try multiple feeds:
- `https://builds.dotnet.microsoft.com/dotnet` (primary public feed)
- `https://ci.dot.net/public` (CI public feed — internal preview builds are often available here)

If both feeds return 404, the SDK may not yet be published. Check with the user.

For Linux/macOS, use the bash version instead:
```bash
sdkDir="/tmp/validate-sdk-dotnet"
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --version <SDK_VERSION> --install-dir $sdkDir
```

## Step 4: Verify Installation

Run the installed dotnet to confirm:
```powershell
& "$env:TEMP\validate-sdk-dotnet\dotnet.exe" --version
```

This should print the exact SDK version that was requested.

**Troubleshooting:** If `dotnet.exe` is not found in the install directory, the dotnet host may not have been downloaded (the directory will only contain `host/`, `sdk/`, `shared/` subdirectories without a `dotnet.exe`). In this case, remove the directory and reinstall:
```powershell
Remove-Item -Recurse -Force "$env:TEMP\validate-sdk-dotnet" -ErrorAction SilentlyContinue
& $installScript -Version "<SDK_VERSION>" -InstallDir "$env:TEMP\validate-sdk-dotnet"
```

## Step 5: Configure for Use

The SDK is installed to a **temp directory**, not system-wide. This means `dotnet --list-sdks` using the system dotnet will NOT show it. This is by design — it avoids affecting the user's system .NET setup.

To use the locally installed SDK:
- Run directly: `$env:TEMP\validate-sdk-dotnet\dotnet.exe`
- Or prepend to PATH for the current session: `$env:PATH = "$env:TEMP\validate-sdk-dotnet;$env:PATH"`

## Step 6: Validate with `#error version`

Use a **file-based app** (a .NET 10+ feature) to extract compiler details. This **must** run in an isolated temp folder to avoid interference from `global.json`, `.sln`, or `.csproj` files in the user's working directory (e.g., a roslyn repo checkout).

1. Create a temp directory with `app.cs` and a pinned `global.json`:
   ```powershell
   $testDir = "$env:TEMP\validate-sdk-test"
   New-Item -ItemType Directory -Path $testDir -Force | Out-Null
   Set-Content -Path "$testDir\app.cs" -Value '#error version'
   # Pin the SDK version to prevent any parent global.json from overriding it
   Set-Content -Path "$testDir\global.json" -Value '{"sdk":{"version":"<SDK_VERSION>","allowPrerelease":true}}'
   ```

2. Run the file-based app from the temp directory using the installed SDK. You **must `cd` into the temp directory** and set `DOTNET_MULTILEVEL_LOOKUP=0` to ensure full isolation from the user's environment:
   ```powershell
   Push-Location $testDir
   $env:DOTNET_MULTILEVEL_LOOKUP = "0"
   & "$env:TEMP\validate-sdk-dotnet\dotnet.exe" run app.cs 2>&1 | Select-String -Pattern "CS8304"
   Pop-Location
   ```

   **Why this isolation matters:** Without `cd` + `global.json` + `DOTNET_MULTILEVEL_LOOKUP=0`, the dotnet CLI walks up the directory tree looking for `global.json`. If run from inside a repo like dotnet/roslyn, it would find the repo's `global.json` and try to use a different SDK version, causing the validation to fail or test the wrong compiler.

3. **The build is expected to fail** (exit code non-zero) because `#error version` is a deliberate compile error. The success criterion is seeing the `CS8304` diagnostic in the output. Look for the `CS8304` line which contains:
   - **Compiler version** (e.g., `5.6.0-2.26154.117`)
   - **Compiler commit SHA** in parentheses (e.g., `6dbf4ee311820b91535cc405fb9f72f3e1ec85fc`)
   - **Language version** (e.g., `preview`)

4. Clean up:
   ```powershell
   Remove-Item -Recurse -Force "$env:TEMP\validate-sdk-test" -ErrorAction SilentlyContinue
   ```

## Step 7: Trace the Roslyn SHA

The compiler commit SHA from Step 6 is a commit in the **dotnet/dotnet** VMR (Virtual Mono Repo), not dotnet/roslyn directly.

1. Look up the compiler SHA in dotnet/dotnet using `github-mcp-server-get_commit` (owner: `dotnet`, repo: `dotnet`).
2. Fetch `src/source-manifest.json` at that commit using `github-mcp-server-get_file_contents` (owner: `dotnet`, repo: `dotnet`, path: `src/source-manifest.json`, sha: `<COMPILER_SHA>`).
3. In the JSON, find the entry with `"path": "roslyn"` and extract its `commitSha` — this is the actual dotnet/roslyn commit.
4. Look up that Roslyn SHA using `github-mcp-server-get_commit` (owner: `dotnet`, repo: `roslyn`) to get the commit summary and date.

## Step 8: Output Validation Report

Output a report in this format:

```
## .NET SDK Validation Report

- **SDK Version:** `<SDK_VERSION>`
- **Compiler Version:** `<COMPILER_VERSION>`
- **dotnet/dotnet commit:** https://github.com/dotnet/dotnet/commit/<DOTNET_DOTNET_SHA>
- **dotnet/roslyn commit:** https://github.com/dotnet/roslyn/commit/<ROSLYN_SHA>

**Last Roslyn commit:** "<COMMIT_SUMMARY>" by <AUTHOR> — merged <DATE>.
```

Use full SHAs (not shortened) and plain URLs (not markdown link syntax).

## Notes

- Build artifacts can also be explored via `ado-pipelines_get_build_log` and `ado-pipelines_get_build_log_by_id` if you need to inspect specific asset names.
- The dotnet-install script approach is preferred over downloading artifacts directly, as the SDK zip is typically published to the CI feeds even for preview builds.

