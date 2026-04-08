using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace LspVGrepTool.Infrastructure;

internal sealed class ExternalSearchRunner
{
    public async Task<ExternalSearchResult> SearchTypeDefinitionPwshAsync(
        string directoryPath,
        string typeName,
        CancellationToken cancellationToken)
    {
        var pattern = $@"\b(class|record|struct|interface|enum)\s+(class\s+|struct\s+)?{Regex.Escape(typeName)}\b";

        // Build a one-liner that uses Get-ChildItem + Select-String, excluding bin/obj,
        // and formats output as  file:line: matchedLine  (grep-style).
        var script = string.Join(" ",
            $"Get-ChildItem -Path '{EscapePwshString(directoryPath)}' -Recurse -Filter '*.cs'",
            "| Where-Object { $_.FullName -notmatch '\\\\(bin|obj)\\\\' }",
            $"| Select-String -Pattern '{EscapePwshString(pattern)}'",
            "| ForEach-Object { \"$($_.Path):$($_.LineNumber): $($_.Line.TrimStart())\" }");

        return await TryRunAsync(
            fileName: "pwsh",
            directoryPath,
            cancellationToken,
            argumentBuilder: arguments =>
            {
                arguments.Add("-NoProfile");
                arguments.Add("-NonInteractive");
                arguments.Add("-Command");
                arguments.Add(script);
            });
    }

    public async Task<ExternalSearchResult> SearchTypeNamePwshAsync(
        string directoryPath,
        string typeName,
        CancellationToken cancellationToken)
    {
        var pattern = $@"\b{Regex.Escape(typeName)}\b";

        var script = string.Join(" ",
            $"Get-ChildItem -Path '{EscapePwshString(directoryPath)}' -Recurse -Filter '*.cs'",
            "| Where-Object { $_.FullName -notmatch '\\\\(bin|obj)\\\\' }",
            $"| Select-String -Pattern '{EscapePwshString(pattern)}'",
            "| ForEach-Object { \"$($_.Path):$($_.LineNumber): $($_.Line.TrimStart())\" }");

        return await TryRunAsync(
            fileName: "pwsh",
            directoryPath,
            cancellationToken,
            argumentBuilder: arguments =>
            {
                arguments.Add("-NoProfile");
                arguments.Add("-NonInteractive");
                arguments.Add("-Command");
                arguments.Add(script);
            });
    }

    private static string EscapePwshString(string value) =>
        value.Replace("'", "''");

    public async Task<ExternalSearchResult> SearchImplementationPwshAsync(
        string directoryPath,
        string typeName,
        CancellationToken cancellationToken)
    {
        var pattern = $@":\s.*\b{Regex.Escape(typeName)}\b";

        var script = string.Join(" ",
            $"Get-ChildItem -Path '{EscapePwshString(directoryPath)}' -Recurse -Filter '*.cs'",
            "| Where-Object { $_.FullName -notmatch '\\\\(bin|obj)\\\\' }",
            $"| Select-String -Pattern '{EscapePwshString(pattern)}'",
            "| ForEach-Object { \"$($_.Path):$($_.LineNumber): $($_.Line.TrimStart())\" }");

        return await TryRunAsync(
            fileName: "pwsh",
            directoryPath,
            cancellationToken,
            argumentBuilder: arguments =>
            {
                arguments.Add("-NoProfile");
                arguments.Add("-NonInteractive");
                arguments.Add("-Command");
                arguments.Add(script);
            });
    }

    public async Task<ExternalSearchResult> SearchDerivedTypesPwshAsync(
        string directoryPath,
        string typeName,
        CancellationToken cancellationToken)
    {
        var pattern = $@"\b(class|record|struct)\s+\w+.*\b{Regex.Escape(typeName)}\b";

        var script = string.Join(" ",
            $"Get-ChildItem -Path '{EscapePwshString(directoryPath)}' -Recurse -Filter '*.cs'",
            "| Where-Object { $_.FullName -notmatch '\\\\(bin|obj)\\\\' }",
            $"| Select-String -Pattern '{EscapePwshString(pattern)}'",
            "| ForEach-Object { \"$($_.Path):$($_.LineNumber): $($_.Line.TrimStart())\" }");

        return await TryRunAsync(
            fileName: "pwsh",
            directoryPath,
            cancellationToken,
            argumentBuilder: arguments =>
            {
                arguments.Add("-NoProfile");
                arguments.Add("-NonInteractive");
                arguments.Add("-Command");
                arguments.Add(script);
            });
    }

    private static async Task<ExternalSearchResult> TryRunAsync(
        string fileName,
        string directoryPath,
        CancellationToken cancellationToken,
        Action<System.Collections.ObjectModel.Collection<string>> argumentBuilder)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = directoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        argumentBuilder(startInfo.ArgumentList);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return ExternalSearchResult.CommandNotFound(fileName);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        return new ExternalSearchResult(fileName, process.ExitCode, standardOutput, standardError, CommandMissing: false);
    }
}

internal sealed record ExternalSearchResult(
    string ToolName,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool CommandMissing)
{
    public static ExternalSearchResult CommandNotFound(string toolName) =>
        new(toolName, -1, string.Empty, string.Empty, CommandMissing: true);
}
