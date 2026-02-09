// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export, Shared]
internal sealed class ServerConfigurationFactory
{
    private readonly IGlobalOptionService _globalOptionService;

    private ServerConfiguration? _serverConfiguration;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServerConfigurationFactory(IGlobalOptionService globalOptionService)
    {
        _globalOptionService = globalOptionService;
    }

    [Export(typeof(ServerConfiguration))]
    public ServerConfiguration ServerConfiguration => _serverConfiguration ?? throw new InvalidOperationException($"{nameof(ServerConfiguration)} has not been initialized");

    public void InitializeConfiguration(ServerConfiguration serverConfiguration)
    {
        Contract.ThrowIfFalse(_serverConfiguration == null);
        _serverConfiguration = serverConfiguration;

        // Update any other global options based on the configuration the server was started with.

        // Check if the devkit extension is included to see if devkit is enabled.
        var isDevkitEnabled = serverConfiguration.DevKitDependencyPath != null;
        // Set the standalone option so other features know whether devkit is running.
        _globalOptionService.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, isDevkitEnabled);
    }
}

internal sealed record class ServerConfiguration(
    bool LaunchDebugger,
    LogConfiguration LogConfiguration,
    string? StarredCompletionsPath,
    string? TelemetryLevel,
    string? SessionId,
    IEnumerable<string> ExtensionAssemblyPaths,
    string? DevKitDependencyPath,
    string? RazorDesignTimePath,
    string? CSharpDesignTimePath,
    string? ServerPipeName,
    bool UseStdIo,
    string? ExtensionLogDirectory,
    bool AutoLoadProjects,
    SourceGeneratorExecutionPreference SourceGeneratorExecutionPreference);

internal sealed class LogConfiguration
{
    private int _currentLogLevel;

    public LogConfiguration(LogLevel initialLogLevel)
    {
        _currentLogLevel = (int)(initialLogLevel);
    }

    public void UpdateLogLevel(LogLevel level)
    {
        Interlocked.Exchange(ref _currentLogLevel, (int)level);
    }

    public LogLevel GetLogLevel()
    {
        return (LogLevel)_currentLogLevel;
    }
}
