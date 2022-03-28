// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Microsoft.RoslynTools.Commands;

internal static class CommonOptions
{
    public static string[] VerbosityLevels => new[] { "q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic" };
    public static readonly Option<string> VerbosityOption = new Option<string>(new[] { "--verbosity", "-v" }, "Set the verbosity level. Allowed values are quiet, minimal, normal, detailed, and diagnostic.").FromAmong(VerbosityLevels);

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

    public static ILogger SetupLogging(this InvocationContext context)
    {
        var minimalLogLevel = context.ParseResult.ParseVerbosity();
        return new SimpleConsoleLogger(context.Console, minimalLogLevel, minimalErrorLevel: LogLevel.Warning);
    }
}
