// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CommandLine;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal static class LanguageServerCommandLine
{
    /// <summary>
    /// Creates the root command for the language server, configuring all command-line options and binding
    /// the action that converts a successful parse into a <see cref="ServerConfiguration"/>.
    /// </summary>
    /// <param name="onParsedAsync">
    /// Callback invoked with the parsed <see cref="ServerConfiguration"/> when the command is invoked.
    /// Tests can pass a callback that simply captures the configuration to validate parsing behavior.
    /// </param>
    public static RootCommand CreateCommand(Func<ServerConfiguration, CancellationToken, Task<int>> onParsedAsync)
    {
        var debugOption = new Option<bool>("--debug")
        {
            Description = "Flag indicating if the debugger should be launched on startup.",
            Required = false,
            DefaultValueFactory = _ => false,
        };
        var brokeredServicePipeNameOption = new Option<string?>("--brokeredServicePipeName")
        {
            Description = "The name of the pipe used to connect to a remote process (if one exists).",
            Required = false,
        };

        var logLevelOption = new Option<LogLevel?>("--logLevel")
        {
            Description = "The minimum log verbosity.",
            Required = false,
        };

        var telemetryLevelOption = new Option<string?>("--telemetryLevel")
        {
            Description = "Telemetry level, Defaults to 'off'. Example values: 'all', 'crash', 'error', or 'off'.",
            Required = false,
        };
        var extensionLogDirectoryOption = new Option<string?>("--extensionLogDirectory")
        {
            Description = "The directory where we should write log files to",
            Required = false,
        };

        var sessionIdOption = new Option<string?>("--sessionId")
        {
            Description = "Session Id to use for telemetry",
            Required = false
        };

        var extensionAssemblyPathsOption = new Option<string[]?>("--extension")
        {
            Description = "Full paths of extension assemblies to load (optional).",
            Required = false
        };

        var devKitDependencyPathOption = new Option<string?>("--devKitDependencyPath")
        {
            Description = "Full path to the Roslyn dependency used with DevKit (optional).",
            Required = false
        };

        var csharpDesignTimePathOption = new Option<string?>("--csharpDesignTimePath")
        {
            Description = "Full path to the C# design time target path (optional).",
            Required = false
        };

        var serverPipeNameOption = new Option<string?>("--pipe")
        {
            Description = "The name of the pipe the server will connect to.",
            Required = false
        };

        var useStdIoOption = new Option<bool>("--stdio")
        {
            Description = "Use stdio for communication with the client.",
            Required = false,
            DefaultValueFactory = _ => false,
        };

        var autoLoadProjectsOption = new Option<int?>("--autoLoadProjects")
        {
            Description = $"The server should automatically discover and load projects based on the workspace folders. " +
                          $"Optionally accepts an integer specifying the maximum number of projects to auto-load; " +
                          $"if no value is supplied, defaults to a server-recommended limit.",
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            CustomParser = argumentResult =>
            {
                // Flag specified with no value, use an internal default
                if (argumentResult.Tokens.Count == 0)
                    return 500;

                if (int.TryParse(argumentResult.Tokens[0].Value, out var value) && value > 0)
                    return value;

                argumentResult.AddError($"Invalid integer value '{argumentResult.Tokens[0].Value}' for --autoLoadProjects.");
                return null;
            },
        };

        var sourceGeneratorExecutionOption = new Option<SourceGeneratorExecutionPreference>("--sourceGeneratorExecutionPreference")
        {
            Description = "Controls when source generators are executed.",
            Required = false,
            // Balanced mode requires additional client side support (to trigger refreshes), so by default run in automatic to ensure tool scenarios without client support run generators.
            DefaultValueFactory = _ => SourceGeneratorExecutionPreference.Automatic,
        };

        var clientProcessIdOption = new Option<int?>("--clientProcessId")
        {
            Description = "The process ID of the client process. The server will terminate when the client process exits.",
            Required = false,
        };

        var daemonOption = new Option<bool>("--daemon")
        {
            Description = "Run as a multi-client daemon that listens for connections on the pipe named by --pipe. " +
                          "This is an internal option used by the roslyn-language-server thin client; end users and editors should not pass it directly.",
            Required = false,
            DefaultValueFactory = _ => false,
        };

        var daemonKeepAliveOption = new Option<int?>("--daemonKeepAlive")
        {
            Description = "Seconds the daemon stays alive after its last client disconnects before exiting. " +
                          "A value of zero or less keeps the daemon alive indefinitely. Defaults to the env var " +
                          $"'{DaemonKeepAliveEnvironmentVariable}' if set, otherwise {DefaultDaemonKeepAliveSeconds} seconds.",
            Required = false,
        };

        var rootCommand = new RootCommand()
        {
            debugOption,
            brokeredServicePipeNameOption,
            logLevelOption,
            telemetryLevelOption,
            sessionIdOption,
            extensionAssemblyPathsOption,
            devKitDependencyPathOption,
            csharpDesignTimePathOption,
            extensionLogDirectoryOption,
            serverPipeNameOption,
            useStdIoOption,
            autoLoadProjectsOption,
            sourceGeneratorExecutionOption,
            clientProcessIdOption,
            daemonOption,
            daemonKeepAliveOption,
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var launchDebugger = parseResult.GetValue(debugOption);
            var logLevel = parseResult.GetValue(logLevelOption);
            var telemetryLevel = parseResult.GetValue(telemetryLevelOption);
            var sessionId = parseResult.GetValue(sessionIdOption);
            var extensionAssemblyPaths = parseResult.GetValue(extensionAssemblyPathsOption) ?? [];
            var devKitDependencyPath = parseResult.GetValue(devKitDependencyPathOption);
            var csharpDesignTimePath = parseResult.GetValue(csharpDesignTimePathOption);
            var extensionLogDirectory = parseResult.GetValue(extensionLogDirectoryOption);
            var serverPipeName = parseResult.GetValue(serverPipeNameOption);
            var useStdIo = parseResult.GetValue(useStdIoOption);
            var autoLoadProjects = parseResult.GetValue(autoLoadProjectsOption);
            var sourceGeneratorExecutionPreference = parseResult.GetValue(sourceGeneratorExecutionOption);
            var clientProcessId = parseResult.GetValue(clientProcessIdOption);
            var isDaemon = parseResult.GetValue(daemonOption);
            var daemonKeepAlive = ResolveDaemonKeepAlive(parseResult.GetValue(daemonKeepAliveOption));

            var serverConfiguration = new ServerConfiguration(
                LaunchDebugger: launchDebugger,
                InitialLogLevel: logLevel ?? LogLevel.Information,
                TelemetryLevel: telemetryLevel,
                SessionId: sessionId,
                ExtensionAssemblyPaths: extensionAssemblyPaths,
                DevKitDependencyPath: devKitDependencyPath,
                CSharpDesignTimePath: csharpDesignTimePath,
                ServerPipeName: serverPipeName,
                UseStdIo: useStdIo,
                ExtensionLogDirectory: extensionLogDirectory,
                AutoLoadProjects: autoLoadProjects,
                SourceGeneratorExecutionPreference: sourceGeneratorExecutionPreference,
                ClientProcessId: clientProcessId,
                IsDaemon: isDaemon,
                DaemonKeepAlive: daemonKeepAlive);

            return await onParsedAsync(serverConfiguration, cancellationToken);
        });

        return rootCommand;
    }

    /// <summary>
    /// The default amount of time a daemon stays alive after its last client disconnects.
    /// </summary>
    internal const int DefaultDaemonKeepAliveSeconds = 15 * 60;

    /// <summary>
    /// Environment variable that can override the daemon keepalive (in seconds) when the
    /// <c>--daemonKeepAlive</c> option is not specified.
    /// </summary>
    internal const string DaemonKeepAliveEnvironmentVariable = "ROSLYN_LANGUAGE_SERVER_DAEMON_KEEPALIVE";

    private static TimeSpan ResolveDaemonKeepAlive(int? optionSeconds)
    {
        var seconds = optionSeconds;
        if (seconds is null &&
            int.TryParse(Environment.GetEnvironmentVariable(DaemonKeepAliveEnvironmentVariable), out var envSeconds))
        {
            seconds = envSeconds;
        }

        seconds ??= DefaultDaemonKeepAliveSeconds;

        // Zero or negative means stay alive indefinitely.
        return seconds <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(seconds.Value);
    }
}
