// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.RoslynTools.Authentication;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Microsoft.RoslynTools.Commands;

internal static class CommonOptions
{
    public static string[] VerbosityLevels => ["q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic"];
    public static readonly Option<string> VerbosityOption = new Option<string>(["--verbosity", "-v"], "Set the verbosity level. Allowed values are quiet, minimal, normal, detailed, and diagnostic.").FromAmong(VerbosityLevels);

    public static readonly Option<string> GitHubTokenOption = new(["--github-token"], () => string.Empty, "Token used to authenticate GitHub.");
    public static readonly Option<string> DevDivAzDOTokenOption = new(["--devdiv-azdo-token"], () => string.Empty, "Token used to authenticate to DevDiv Azure DevOps.");
    public static readonly Option<string> DncEngAzDOTokenOption = new(["--dnceng-azdo-token"], () => string.Empty, "Token used to authenticate to DncEng Azure DevOps.");
    public static readonly Option<bool> IsCIOption = new(["--ci"], "Indicate that the command is running in a CI environment.");

    public static RoslynToolsSettings LoadSettings(this ParseResult parseResult, ILogger logger)
    {
        // Both options default to empty string.
        var githubToken = parseResult.GetValueForOption(GitHubTokenOption) ?? string.Empty;
        var devdivAzDOToken = parseResult.GetValueForOption(DevDivAzDOTokenOption) ?? string.Empty;
        var dncengAzDOToken = parseResult.GetValueForOption(DncEngAzDOTokenOption) ?? string.Empty;
        var isCI = parseResult.GetValueForOption(IsCIOption);

        return LocalSettings.GetRoslynToolsSettings(githubToken, devdivAzDOToken, dncengAzDOToken, isCI, logger);
    }

    public static LogLevel ParseVerbosity(this ParseResult parseResult)
    {
        if (parseResult.HasOption(VerbosityOption) &&
            parseResult.GetValueForOption(VerbosityOption) is string { Length: > 0 } verbosity)
        {
            return GetLogLevel(verbosity);
        }

        return LogLevel.Information;
    }

    public static LogLevel GetLogLevel(string? verbosity)
    {
        return verbosity switch
        {
            "q" or "quiet" => LogLevel.Error,
            "m" or "minimal" => LogLevel.Warning,
            "n" or "normal" => LogLevel.Information,
            "d" or "detailed" => LogLevel.Debug,
            "diag" or "diagnostic" => LogLevel.Trace,
            _ => LogLevel.Information,
        };
    }

    public static ILogger<Program> SetupLogging(this InvocationContext context)
    {
        var minimalLogLevel = context.ParseResult.ParseVerbosity();
        using var loggerFactory = LoggerFactory
            .Create(builder => builder.AddSimpleConsole(c => c.ColorBehavior = LoggerColorBehavior.Default)
            .SetMinimumLevel(minimalLogLevel));
        return loggerFactory.CreateLogger<Program>();
    }
}
