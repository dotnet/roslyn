// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Logging;

namespace Microsoft.RoslynTools.Commands;

internal static class CommonOptions
{
    public static string[] VerbosityLevels => ["q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic"];
    public static readonly Option<string> VerbosityOption = new("--verbosity", "-v")
    {
        Description = "Set the verbosity level. Allowed values are quiet, minimal, normal, detailed, and diagnostic.",
        DefaultValueFactory = _ => "n"
    };
    public static readonly Option<string> GitHubTokenOption = new("--github-token")
    {
        Description = "Token used to authenticate GitHub.",
        DefaultValueFactory = _ => string.Empty,
    };
    public static readonly Option<string> DevDivAzDOTokenOption = new("--devdiv-azdo-token")
    {
        Description = "Token used to authenticate to DevDiv Azure DevOps.",
        DefaultValueFactory = _ => string.Empty,
    };
    public static readonly Option<string> DncEngAzDOTokenOption = new("--dnceng-azdo-token")
    {
        Description = "Token used to authenticate to DncEng Azure DevOps.",
        DefaultValueFactory = _ => string.Empty,
    };
    public static readonly Option<bool> IsCIOption = new("--ci")
    {
        Description = "Indicate that the command is running in a CI environment.",
    };

    static CommonOptions()
    {
        VerbosityOption.AcceptOnlyFromAmong(VerbosityLevels);
    }

    public static RoslynToolsSettings LoadSettings(this ParseResult parseResult, ILogger logger)
    {
        // Both options default to empty string.
        var githubToken = parseResult.GetValue(GitHubTokenOption) ?? string.Empty;
        var devdivAzDOToken = parseResult.GetValue(DevDivAzDOTokenOption) ?? string.Empty;
        var dncengAzDOToken = parseResult.GetValue(DncEngAzDOTokenOption) ?? string.Empty;
        var isCI = parseResult.GetValue(IsCIOption);

        return LocalSettings.GetRoslynToolsSettings(githubToken, devdivAzDOToken, dncengAzDOToken, isCI, logger);
    }

    public static LogLevel ParseVerbosity(this ParseResult parseResult)
    {
        return GetLogLevel(parseResult.GetValue(VerbosityOption));
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

    public static ILogger<Program> SetupLogging(this ParseResult parseResult)
    {
        var minimalLogLevel = parseResult.ParseVerbosity();
        using var loggerFactory = LoggerFactory
            .Create(builder => builder.AddSimplerFormatter(o => { })
            .SetMinimumLevel(minimalLogLevel));
        return loggerFactory.CreateLogger<Program>();
    }
}
