---
name: validate-sdk
description: Install and validate a .NET SDK from an Azure DevOps internal build. Use this when asked to install, validate, or set up a .NET SDK from a dnceng/internal Azure DevOps build link or build ID.
---

# Install and Validate .NET SDK from Azure DevOps Internal Build

Follow these steps to install and validate a .NET SDK from an internal Azure DevOps build (typically from `dnceng/internal`).

> **Important:** Do not run this skill from within a Roslyn enlistment. Run it from a separate scratch directory (e.g., `c:\repos\test-sdk`). The skill installs an SDK and creates temporary files in the current working directory.

This skill is designed for use with **agency copilot** (install from https://aka.ms/agency). Run `agency copilot` in your terminal to start.

## How to Invoke This Skill

In an `agency copilot` session, ask something like:
- "Install the .NET SDK from this build: https://dev.azure.com/dnceng/internal/_build/results?buildId=2919304"
- "Install .NET SDK from ADO build 2919304"
- "Use /validate-sdk to set up the SDK from build 2919304"

### Working Directory

Run `agency copilot` from any folder you want to use as your working directory. The SDK will be installed into a `.dotnet` subdirectory of the current working directory (e.g., `c:\repos\test-sdk\.dotnet`). A temporary `app.cs` file will also be created in this directory for validation.

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

## Prerequisites: Azure DevOps MCP Plugin

This skill needs the Azure DevOps MCP plugin to query the ADO build and extract the SDK version number from it. Without this plugin, the agent cannot access the `dnceng/internal` build metadata.

1. Check if `~/.copilot/mcp-config.json` exists and contains an `"ado"` server entry.
2. If not, create or update `~/.copilot/mcp-config.json` with:

```json
{
  "mcpServers": {
    "ado": {
      "command": "npx",
      "args": ["-y", "@azure-devops/mcp", "dnceng", "-d", "core", "pipelines"],
      "tools": ["*"]
    }
  }
}
```

3. If you just added the MCP config, the CLI must be restarted for the new tools to become available. Tell the user:
   - Note the current session ID (shown in the prompt or via `/session`)
   - Run `/exit` to quit the CLI
   - Restart with: `agency copilot --resume=<SESSION_ID>`
   - Then tell the agent: "Proceed with validation"

   The ADO MCP server requires Node.js 20+ and will authenticate via the browser on first use.

## Step 1: Get the Build ID

Extract the `buildId` from the Azure DevOps URL. For example, from:
`https://dev.azure.com/dnceng/internal/_build/results?buildId=2919304&view=artifacts`
The build ID is `2919304`.

## Step 2: Get the SDK Version from the Build

Use the ADO MCP tool `ado-pipelines_get_build_status` with `project: "internal"` and the `buildId` to retrieve the build report. The build number in the report contains the SDK version. Parse out the version portion (e.g., `11.0.100-preview.2.26154.117` from build number `20260305.1-11.0.100-preview.2.26154.117-304630`).

The SDK version follows the pattern `X.Y.Z-preview.N.NNNNN.NNN` and appears between the date prefix and the trailing BAR build ID.

## Step 3: Install Using dotnet-install Script

Download and run the official dotnet-install script with the exact SDK version:

```powershell
# Download the installer script
$installScript = "$env:TEMP\dotnet-install.ps1"
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript

# Install the specific SDK version to a local directory
& $installScript -Version "<SDK_VERSION>" -InstallDir "<PROJECT_DIR>\.dotnet"
```

The script will automatically try multiple feeds:
- `https://builds.dotnet.microsoft.com/dotnet` (primary public feed)
- `https://ci.dot.net/public` (CI public feed — internal preview builds are often available here)

If both feeds return 404, the SDK may not yet be published. Check with the user.

For Linux/macOS, use the bash version instead:
```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version <SDK_VERSION> --install-dir <PROJECT_DIR>/.dotnet
```

## Step 4: Verify Installation

Run the installed dotnet to confirm:
```powershell
& "<PROJECT_DIR>\.dotnet\dotnet.exe" --version
```

This should print the exact SDK version that was requested.

## Step 5: Configure for Use

The SDK is installed to a **local directory**, not system-wide. This means `dotnet --list-sdks` using the system dotnet will NOT show it. This is by design — it avoids affecting the user's system .NET setup.

To use the locally installed SDK:
- Run directly: `<PROJECT_DIR>\.dotnet\dotnet.exe`
- Or prepend to PATH for the current session: `$env:PATH = "<PROJECT_DIR>\.dotnet;$env:PATH"`

## Step 6: Validate with `#error version`

Use `dotnet run` with a single-file app to extract compiler details — no project file needed:

1. Create a file called `app.cs` containing just `#error version`.
2. Run it using the installed SDK and look for the `CS8304` error in the output:
   ```powershell
   & "<PROJECT_DIR>\.dotnet\dotnet.exe" run app.cs 2>&1 | Select-String -Pattern "CS8304"
   ```
3. The CS8304 output contains:
   - **Compiler version** (e.g., `5.6.0-2.26154.117`)
   - **Compiler commit SHA** in parentheses (e.g., `6dbf4ee311820b91535cc405fb9f72f3e1ec85fc`)
   - **Language version** (e.g., `preview`)

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

- The `dnceng/internal` project hosts .NET SDK staging builds. The org name is `dnceng`.
- Build artifacts can also be explored via `ado-pipelines_get_build_log` and `ado-pipelines_get_build_log_by_id` if you need to inspect specific asset names.
- The dotnet-install script approach is preferred over downloading artifacts directly, as the SDK zip is typically published to the CI feeds even for preview builds.

