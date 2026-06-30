#!/usr/bin/env dotnet
#:property ImportDirectoryBuildProps=false
#:property ImportDirectoryBuildTargets=false
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

var options = ParseArguments(args);
var workItemId = GetRequiredInt(options, "WorkItemId");
var organizationUrl = GetString(options, "OrganizationUrl", "https://devdiv.visualstudio.com");
var project = GetString(options, "Project", "DevDiv");
var timeoutSeconds = GetInt(options, "TimeoutSeconds", 300);
var remoteDebuggingPort = GetInt(options, "RemoteDebuggingPort", 0);
var userDataDirectory = GetString(options, "UserDataDirectory", Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FeedbackHandler",
    "browser-auth-profile"));
var vssTokenCachePath = GetString(options, "VssTokenCachePath", Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FeedbackHandler",
    "vss_token_cache.txt"));
var devComCookieCachePath = GetString(options, "DevComCookieCachePath", Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FeedbackHandler",
    "devcom_cookie_cache.txt"));
var devComAccessTokenCachePath = GetString(options, "DevComAccessTokenCachePath", Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FeedbackHandler",
    "devcom_access_token_cache.txt"));
var skipDevComAuth = GetSwitch(options, "SkipDevComCookies") || GetSwitch(options, "SkipDevComAuth");
var keepBrowserOpen = GetSwitch(options, "KeepBrowserOpen");

if (remoteDebuggingPort == 0)
{
    remoteDebuggingPort = GetFreeTcpPort();
}

var workItemUrl = $"{organizationUrl.TrimEnd('/')}/{project}/_workitems/edit/{workItemId}";
var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
var edgeProcess = StartEdge(workItemUrl, remoteDebuggingPort, userDataDirectory);

try
{
    await WaitForDevToolsEndpointAsync(remoteDebuggingPort, deadline);

    var vssToken = await WaitForVssTokenAsync(remoteDebuggingPort, workItemUrl, deadline);
    WriteTextNoBom(vssTokenCachePath, vssToken);
    Console.WriteLine($"Saved VSS app token to {vssTokenCachePath}");

    if (!skipDevComAuth)
    {
        await CaptureDeveloperCommunityAuthAsync(remoteDebuggingPort, deadline, devComCookieCachePath, devComAccessTokenCachePath);
    }

    Console.WriteLine("[VS_FEEDBACK_AUTH_SUMMARY] {\"vssTokenCached\":true}");
}
finally
{
    if (!keepBrowserOpen && !edgeProcess.HasExited)
    {
        edgeProcess.CloseMainWindow();
        if (!edgeProcess.WaitForExit(5000))
        {
            edgeProcess.Kill(entireProcessTree: true);
        }
    }
}

Dictionary<string, string?> ParseArguments(string[] arguments)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < arguments.Length; i++)
    {
        var argument = arguments[i];
        if (!argument.StartsWith("-", StringComparison.Ordinal))
        {
            continue;
        }

        var name = argument.TrimStart('-');
        string? value = "true";
        var equalsIndex = name.IndexOf('=');
        if (equalsIndex >= 0)
        {
            value = name[(equalsIndex + 1)..];
            name = name[..equalsIndex];
        }
        else if (i + 1 < arguments.Length && !arguments[i + 1].StartsWith("-", StringComparison.Ordinal))
        {
            value = arguments[++i];
        }

        result[name] = value;
    }

    return result;
}

int GetRequiredInt(Dictionary<string, string?> values, string name)
{
    if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var result))
    {
        throw new ArgumentException($"Missing or invalid -{name}.");
    }

    return result;
}

int GetInt(Dictionary<string, string?> values, string name, int defaultValue)
    => values.TryGetValue(name, out var value) && int.TryParse(value, out var result) ? result : defaultValue;

string GetString(Dictionary<string, string?> values, string name, string defaultValue)
    => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

bool GetSwitch(Dictionary<string, string?> values, string name)
    => values.TryGetValue(name, out var value) &&
        (string.IsNullOrWhiteSpace(value) ||
        bool.TryParse(value, out var boolValue) && boolValue ||
        value.Equals("1", StringComparison.OrdinalIgnoreCase));

int GetFreeTcpPort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    try
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
    finally
    {
        listener.Stop();
    }
}

string FindEdgeBinary()
{
    var paths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe"),
    };

    foreach (var path in paths)
    {
        if (File.Exists(path))
        {
            return path;
        }
    }

    throw new FileNotFoundException("Microsoft Edge was not found.");
}

Process StartEdge(string url, int port, string profileDirectory)
{
    Directory.CreateDirectory(profileDirectory);
    var startInfo = new ProcessStartInfo(FindEdgeBinary())
    {
        UseShellExecute = false,
    };

    startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
    startInfo.ArgumentList.Add($"--remote-debugging-port={port}");
    startInfo.ArgumentList.Add("--no-first-run");
    startInfo.ArgumentList.Add("--no-default-browser-check");
    startInfo.ArgumentList.Add("--new-window");
    startInfo.ArgumentList.Add(url);

    Console.WriteLine($"Opening Edge for VS Feedback auth with profile: {profileDirectory}");
    Console.WriteLine("If prompted, sign in to Azure DevOps in the browser window.");
    return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Edge.");
}

async Task WaitForDevToolsEndpointAsync(int port, DateTimeOffset deadline)
{
    using var httpClient = new HttpClient();
    while (DateTimeOffset.UtcNow < deadline)
    {
        try
        {
            using var response = await httpClient.GetAsync($"http://127.0.0.1:{port}/json/version");
            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (HttpRequestException)
        {
        }

        await Task.Delay(500);
    }

    throw new TimeoutException($"Timed out waiting for Edge DevTools endpoint on port {port}.");
}

async Task<string> WaitForVssTokenAsync(int port, string preferredUrl, DateTimeOffset deadline)
{
    string? lastReason = null;
    var lastStatus = DateTimeOffset.MinValue;
    while (DateTimeOffset.UtcNow < deadline)
    {
        if (DateTimeOffset.UtcNow - lastStatus > TimeSpan.FromSeconds(15))
        {
            Console.WriteLine(string.IsNullOrWhiteSpace(lastReason)
                ? "Waiting for VS Feedback Diagnostics iframe and VSS token..."
                : $"Waiting for VS Feedback Diagnostics iframe and VSS token... Last check: {lastReason}");
            lastStatus = DateTimeOffset.UtcNow;
        }

        var targets = await GetTargetsAsync(port);
        var pageTarget = GetPageTarget(targets, preferredUrl);
        if (pageTarget is not null)
        {
            await using var pageClient = await CdpClient.ConnectAsync(pageTarget.WebSocketDebuggerUrl);
            await pageClient.SendAsync("Page.enable");
            await pageClient.SendAsync("Runtime.enable");
            await pageClient.SendAsync("Network.enable");

            await TryClickDiagnosticsTabAsync(pageClient);
            await EnsureDiagnosticsFrameAsync(pageClient);

            var token = await TryGetVssTokenFromAuthCookieAsync(pageClient);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            lastReason = "VS Feedback auth cookie is not available yet.";
        }

        targets = await GetTargetsAsync(port);
        var diagnosticsTarget = targets.FirstOrDefault(target =>
            !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl) &&
            target.Url.Contains("vsfeedback.azurewebsites.net", StringComparison.OrdinalIgnoreCase) &&
            target.Url.Contains("Diagnostics.html", StringComparison.OrdinalIgnoreCase));

        if (diagnosticsTarget is not null)
        {
            await using var diagnosticsClient = await CdpClient.ConnectAsync(diagnosticsTarget.WebSocketDebuggerUrl);
            await diagnosticsClient.SendAsync("Network.enable");

            var token = await TryGetVssTokenFromAuthCookieAsync(diagnosticsClient);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            token = await TryGetVssTokenFromScriptAsync(diagnosticsClient);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            lastReason = "Diagnostics iframe loaded, but no token is available yet.";
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new TimeoutException($"Timed out before acquiring a VSS app token. Last check: {lastReason}");
}

async Task TryClickDiagnosticsTabAsync(CdpClient client)
{
    const string expression = """
(() => {
  const describe = e => `${e.innerText || e.textContent || ''} ${e.getAttribute?.('aria-label') || ''} ${e.title || ''}`.trim();
  const elements = [];
  const visit = root => {
    for (const element of root.querySelectorAll('*')) {
      elements.push(element);
      if (element.shadowRoot) {
        visit(element.shadowRoot);
      }
    }
  };
  visit(document);

  const matches = elements
    .map(e => ({ element: e, text: describe(e) }))
    .filter(x => /\bDiagnostics\b/i.test(x.text) && x.text.length <= 200)
    .sort((left, right) => left.text.length - right.text.length);

  for (const { element, text } of matches) {
    const clickable = element.closest?.('[role="tab"], button, a, [role="button"], li') || element;
    clickable.scrollIntoView({ block: 'center', inline: 'center' });
    clickable.click();
    return text || 'Diagnostics';
  }

  return '';
})()
""";

    var response = await client.EvaluateAsync(expression, timeout: TimeSpan.FromSeconds(5));
    var clicked = response?["result"]?["result"]?["value"]?.GetValue<string>();
    if (!string.IsNullOrWhiteSpace(clicked))
    {
        Console.WriteLine($"Clicked Diagnostics tab candidate: {clicked}");
    }
}

async Task EnsureDiagnosticsFrameAsync(CdpClient client)
{
    var frameTreeResponse = await client.SendAsync("Page.getFrameTree", timeout: TimeSpan.FromSeconds(5));
    var frameTree = frameTreeResponse?["result"]?["frameTree"];
    var diagnosticsFrame = FindFrameByUrlPart(frameTree, "Diagnostics.html");
    if (diagnosticsFrame is not null)
    {
        return;
    }

    var overviewFrame = FindFrameByUrlPart(frameTree, "Overview.html");
    if (overviewFrame is null)
    {
        return;
    }

    var overviewUrl = overviewFrame["url"]?.GetValue<string>();
    var overviewFrameId = overviewFrame["id"]?.GetValue<string>();
    if (string.IsNullOrWhiteSpace(overviewUrl) || string.IsNullOrWhiteSpace(overviewFrameId))
    {
        return;
    }

    var diagnosticsUrl = overviewUrl.Replace("Overview.html", "Diagnostics.html", StringComparison.OrdinalIgnoreCase);
    await client.SendAsync("Page.navigate", new JsonObject
    {
        ["url"] = diagnosticsUrl,
        ["frameId"] = overviewFrameId,
    }, timeout: TimeSpan.FromSeconds(10));
    Console.WriteLine("Navigated VS Feedback iframe to Diagnostics.html.");
}

JsonNode? FindFrameByUrlPart(JsonNode? frameTree, string urlPart)
{
    if (frameTree is null)
    {
        return null;
    }

    var frame = frameTree["frame"];
    var url = frame?["url"]?.GetValue<string>();
    if (!string.IsNullOrWhiteSpace(url) &&
        url.Contains(urlPart, StringComparison.OrdinalIgnoreCase))
    {
        return frame;
    }

    if (frameTree["childFrames"] is JsonArray children)
    {
        foreach (var child in children)
        {
            var result = FindFrameByUrlPart(child, urlPart);
            if (result is not null)
            {
                return result;
            }
        }
    }

    return null;
}

async Task<string?> TryGetVssTokenFromAuthCookieAsync(CdpClient client)
{
    var response = await client.SendAsync("Network.getAllCookies", timeout: TimeSpan.FromSeconds(5));
    if (response?["result"]?["cookies"] is not JsonArray cookies)
    {
        return null;
    }

    foreach (var cookie in cookies.OfType<JsonObject>())
    {
        var domain = cookie["domain"]?.GetValue<string>();
        var name = cookie["name"]?.GetValue<string>();
        if (!string.Equals(name, "Auth", StringComparison.Ordinal) ||
            domain?.Contains("vsfeedback.azurewebsites.net", StringComparison.OrdinalIgnoreCase) != true)
        {
            continue;
        }

        var rawValue = cookie["value"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            continue;
        }

        var value = WebUtility.UrlDecode(rawValue);
        return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value["Bearer ".Length..]
            : null;
    }

    return null;
}

async Task<string?> TryGetVssTokenFromScriptAsync(CdpClient client)
{
    const string expression = """
(async () => {
  if (typeof VSS === 'undefined' || typeof VSS.getAppToken !== 'function') {
    return '';
  }

  const token = await VSS.getAppToken();
  return token && (token.token || token) || '';
})()
""";

    var response = await client.EvaluateAsync(expression, timeout: TimeSpan.FromSeconds(20));
    return response?["result"]?["result"]?["value"]?.GetValue<string>();
}

async Task CaptureDeveloperCommunityAuthAsync(
    int port,
    DateTimeOffset deadline,
    string devComCookieCachePath,
    string devComAccessTokenCachePath)
{
    Console.WriteLine("Capturing Developer Community authentication...");
    var targets = await GetTargetsAsync(port);
    var pageTarget = targets.FirstOrDefault(target =>
        target.Type.Equals("page", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl));
    if (pageTarget is null)
    {
        Console.WriteLine("No page target was available for Developer Community auth.");
        return;
    }

    await using (var client = await CdpClient.ConnectAsync(pageTarget.WebSocketDebuggerUrl))
    {
        await client.SendAsync("Page.enable");
        await client.SendAsync("Runtime.enable");
        await client.SendAsync("Network.enable");
        await client.SendAsync("Page.navigate", new JsonObject
        {
            ["url"] = "https://developercommunity.visualstudio.com/switch?ru=https%3A%2F%2Fdevelopercommunity.visualstudio.com%2Fhome",
        });
    }

    CdpTarget? devComTarget = null;
    while (DateTimeOffset.UtcNow < deadline)
    {
        targets = await GetTargetsAsync(port);
        devComTarget = targets.FirstOrDefault(target =>
            target.Type.Equals("page", StringComparison.OrdinalIgnoreCase) &&
            target.Url.StartsWith("https://developercommunity.visualstudio.com/", StringComparison.OrdinalIgnoreCase) &&
            !target.Url.Contains("/switch", StringComparison.OrdinalIgnoreCase) &&
            !target.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) &&
            !target.Url.Contains("microsoftonline.com", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl));
        if (devComTarget is not null)
        {
            break;
        }

        Console.WriteLine("Waiting for Developer Community sign-in to complete...");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    if (devComTarget is null)
    {
        Console.WriteLine("Developer Community auth did not reach an authenticated page.");
        return;
    }

    await using var devComClient = await CdpClient.ConnectAsync(devComTarget.WebSocketDebuggerUrl);
    await devComClient.SendAsync("Runtime.enable");
    await devComClient.SendAsync("Network.enable");

    var token = await TryGetDeveloperCommunityAccessTokenAsync(devComClient);
    if (!string.IsNullOrWhiteSpace(token))
    {
        WriteTextNoBom(devComAccessTokenCachePath, token);
        Console.WriteLine($"Saved Developer Community access token to {devComAccessTokenCachePath}");
    }
    else
    {
        Console.WriteLine("No Developer Community access token was captured.");
    }

    var cookies = await GetDeveloperCommunityCookiesAsync(devComClient);
    if (!string.IsNullOrWhiteSpace(cookies))
    {
        WriteTextNoBom(devComCookieCachePath, cookies);
        Console.WriteLine($"Saved Developer Community cookies to {devComCookieCachePath}");
    }
    else
    {
        Console.WriteLine("No Developer Community cookies were captured.");
    }
}

async Task<string?> TryGetDeveloperCommunityAccessTokenAsync(CdpClient client)
{
    const string expression = """
(async () => {
  try {
    const response = await fetch('/token?refresh=true');
    if (!response.ok) {
      return '';
    }

    const data = await response.json();
    return data && data.access_token || '';
  } catch {
    return '';
  }
})()
""";

    var response = await client.EvaluateAsync(expression, timeout: TimeSpan.FromSeconds(20));
    return response?["result"]?["result"]?["value"]?.GetValue<string>();
}

async Task<string?> GetDeveloperCommunityCookiesAsync(CdpClient client)
{
    var response = await client.SendAsync("Network.getAllCookies", timeout: TimeSpan.FromSeconds(5));
    if (response?["result"]?["cookies"] is not JsonArray cookies)
    {
        return null;
    }

    var pairs = cookies.OfType<JsonObject>()
        .Where(cookie => cookie["domain"]?.GetValue<string>()?.Contains("developercommunity.visualstudio.com", StringComparison.OrdinalIgnoreCase) == true)
        .Select(cookie => $"{cookie["name"]?.GetValue<string>()}={cookie["value"]?.GetValue<string>()}")
        .ToArray();

    return pairs.Length == 0 ? null : string.Join("; ", pairs);
}

async Task<List<CdpTarget>> GetTargetsAsync(int port)
{
    using var httpClient = new HttpClient();
    var json = await httpClient.GetStringAsync($"http://127.0.0.1:{port}/json/list");
    var array = JsonNode.Parse(json)?.AsArray() ?? [];
    var result = new List<CdpTarget>();
    foreach (var item in array.OfType<JsonObject>())
    {
        result.Add(new CdpTarget(
            item["type"]?.GetValue<string>() ?? "",
            item["url"]?.GetValue<string>() ?? "",
            item["title"]?.GetValue<string>() ?? "",
            item["webSocketDebuggerUrl"]?.GetValue<string>() ?? ""));
    }

    return result;
}

CdpTarget? GetPageTarget(List<CdpTarget> targets, string preferredUrl)
{
    var pageTargets = targets
        .Where(target => target.Type.Equals("page", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl))
        .ToArray();

    return pageTargets.FirstOrDefault(target => target.Url.StartsWith(preferredUrl, StringComparison.OrdinalIgnoreCase)) ??
        pageTargets.FirstOrDefault(target => target.Url.Contains("/_workitems/edit/", StringComparison.OrdinalIgnoreCase)) ??
        pageTargets.FirstOrDefault();
}

void WriteTextNoBom(string path, string content)
{
    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

sealed record CdpTarget(string Type, string Url, string Title, string WebSocketDebuggerUrl);

sealed class CdpClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket;
    private int _nextId;

    private CdpClient(ClientWebSocket socket)
        => _socket = socket;

    public static async Task<CdpClient> ConnectAsync(string webSocketDebuggerUrl)
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketDebuggerUrl), CancellationToken.None);
        return new CdpClient(socket);
    }

    public Task<JsonObject?> EvaluateAsync(string expression, TimeSpan? timeout = null)
        => SendAsync("Runtime.evaluate", new JsonObject
        {
            ["expression"] = expression,
            ["awaitPromise"] = true,
            ["returnByValue"] = true,
        }, timeout);

    public async Task<JsonObject?> SendAsync(string method, JsonObject? parameters = null, TimeSpan? timeout = null)
    {
        var id = Interlocked.Increment(ref _nextId);
        var payload = new JsonObject
        {
            ["id"] = id,
            ["method"] = method,
            ["params"] = parameters ?? new JsonObject(),
        };

        var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        await _socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        while (true)
        {
            var message = await ReceiveMessageAsync(cancellationTokenSource.Token);
            if (message?["id"]?.GetValue<int>() == id)
            {
                if (message["error"] is not null)
                {
                    throw new InvalidOperationException($"CDP {method} failed: {message["error"]}");
                }

                return message;
            }
        }
    }

    private async Task<JsonObject?> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        using var stream = new MemoryStream();
        while (true)
        {
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("Edge DevTools websocket closed.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        return JsonNode.Parse(json) as JsonObject;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
        }
        catch
        {
        }

        _socket.Dispose();
    }
}
