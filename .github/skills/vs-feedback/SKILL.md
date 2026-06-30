---
name: vs-feedback
description: Download and investigate Visual Studio Developer Community / Azure DevOps feedback items, including work item fields, Developer Community conversation, VS Feedback diagnostics, all attachments, logs, binlogs, dumps, ETL traces, screenshots, and repro archives.
---

# Visual Studio Feedback Investigation

Use this skill when:
- given a Visual Studio Developer Community URL, DevDiv Azure DevOps feedback work item, or VS Feedback ticket
- asked to download feedback attachments, logs, dumps, telemetry, diagnostics, or repro files
- investigating a Roslyn issue reported through Visual Studio feedback

Do **not** use this for ordinary CI failures unless the feedback item links to a CI build. Use `ci-analysis` for CI build/test status.

## What the TypeScript feedback handler does

The working pattern from `TypeScript-FeedbackHandler` is:

1. Fetch the Azure DevOps work item with `WorkItemExpand.Relations`.
2. Read `Microsoft.DevDiv.DeveloperCommunityLink` and parse the Developer Community ID from `/t/<slug>/<id>`.
3. Fetch the Developer Community thread from `https://sendvsfeedback2.azurewebsites.net/api/detailsV3/rootPost?id=<devComId>`.
4. Fetch diagnostics and attachments from `https://vsfeedback.azurewebsites.net/api/diagnostics?areaPath=<areaPath>&developerCommunityId=<devComId>&developerCommunityUrl=<devComUrl>`.
5. Authorize the diagnostics API with a **VSS app token** from `VSS.getAppToken()` in the feedback work item's Diagnostics iframe. This is different from the Azure DevOps AAD bearer token used by the normal work-item REST API.
6. Treat every diagnostics `items[]` entry as potentially relevant. Each entry can include `groupName`, `friendlyName` or `fileName`, `url`, and sometimes a Prism telemetry session link.
7. Download Azure DevOps attachment URLs with the AAD bearer token. Download Developer Community / Azure Edge attachment URLs with authenticated Developer Community cookies.

## Download everything

Store downloaded artifacts under the session artifacts folder, not in the repo. For example:

```powershell
.\.github\skills\vs-feedback\scripts\Get-VSFeedback.ps1 `
  -WorkItemId 1234567 `
  -OutputDirectory D:\.copilot\session-state\<session-id>\files\vs-feedback\1234567 `
  -AcquireVssToken `
  -IncludeHistory `
  -ExtractArchives
```

The script writes:
- `work-item.json`
- `comments.json`
- `history.json` when `-IncludeHistory` is set
- `devcom-conversation.json` and `devcom-conversation.md`
- `diagnostics.json`
- `attachments-index.json`
- downloaded files under `attachments\`
- extracted zip contents under `extracted\` when `-ExtractArchives` is set
- `analysis\artifact-inventory.md`
- `analysis\log-findings.md`
- `summary.md`

### Authentication

The script tries to obtain an Azure DevOps AAD token from `-AzdoBearerToken`, `AZDO_BEARER_TOKEN`, or `az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798`.

For VS Feedback diagnostics attachments, pass `-VssToken`, set `VSS_TOKEN`, let the script read `%LOCALAPPDATA%\FeedbackHandler\vss_token_cache.txt`, or run with `-AcquireVssToken`. The acquisition flow runs the file-based C# helper `Get-VSFeedbackAuth.cs`, opens Edge with the reusable `%LOCALAPPDATA%\FeedbackHandler\browser-auth-profile` auth profile, loads the Diagnostics iframe through the browser DevTools endpoint, captures the VS Feedback bearer token, and caches it for later runs. If the browser prompts for sign-in during the first run, complete sign-in; later skill runs reuse that profile.

During `-AcquireVssToken`, the helper also tries to cache a Developer Community access token in `%LOCALAPPDATA%\FeedbackHandler\devcom_access_token_cache.txt` and cookies in `%LOCALAPPDATA%\FeedbackHandler\devcom_cookie_cache.txt`. If Developer Community attachment downloads still return 401/403, rerun the main download with `-AcquireVssToken` or provide auth with `-DevComAccessToken` / `DEVCOM_ACCESS_TOKEN` or `-DevComCookie` / `DEVCOM_COOKIE`. Do not report the investigation as complete when attachments failed to download; include the exact failed files and auth requirement.

The browser auth flow is intentionally interactive: if the browser prompts for sign-in, complete it as the user, then make sure the work item's Diagnostics tab is open. Do not try to replace browser auth with an Azure CLI token; the diagnostics service does not accept the normal Azure DevOps AAD token.

## Investigation workflow

1. Run the download script with `-AcquireVssToken -IncludeHistory -ExtractArchives` unless a valid cached VSS token already exists.
2. Read `summary.md`, `devcom-conversation.md`, `attachments-index.json`, and the generated analysis files.
3. Build a concise issue theory from the title, description, Developer Community conversation, telemetry session ID, and attachment names before reading large logs.
4. Inspect artifacts by category:
   - **ActivityLog / VS logs**: search for `Error`, `Exception`, `Microsoft.CodeAnalysis`, `Roslyn`, `ServiceHub`, `LanguageServer`, `OOP`, `devenv`, `Watson`, `crash`, `hang`, and the feature area named in the report.
   - **Roslyn/Razor logs**: identify request names, document paths, project names, exception stacks, and timings. If the attachment is a `Full_*_razor.zip` or `Range_*_razor.zip`, invoke the `formatting-log` skill after downloading it.
   - **MSBuild binlogs**: use binlog tools on `.binlog` files and correlate errors or project state with the reported behavior.
   - **Dumps**: record dump paths and process names. Analyze with an already available debugger (`dotnet-dump`, WinDbg/cdb, VS) only if present; do not install new dump tooling just for the investigation.
   - **ETL/perf traces**: record the trace and use existing PerfView/WPA tooling if present. Otherwise summarize that trace analysis requires local perf tooling.
   - **Screenshots/images**: inspect for error text, UI state, project context, and Visual Studio version.
   - **Repro archives/projects**: extract to the artifact folder, inspect project files and source, and try to reduce to a minimal Roslyn test when appropriate.
5. If logs point to Roslyn source, search this repo for the feature area or stack frame and continue with a normal product investigation.

## Reporting

Return:
- the feedback work item title/ID and Developer Community link
- where artifacts were saved
- which attachments downloaded successfully and which failed
- the strongest evidence from logs/dumps/binlogs/screenshots
- a likely root cause or "not enough evidence" with the missing artifact called out
- repro steps or a minimal repro plan when one is derivable
- recommended next action: fix, ask reporter for more data, route to another team, or import a specific log/repro into tests

Never commit raw feedback attachments, dumps, logs, or extracted customer projects. They may contain private data.
