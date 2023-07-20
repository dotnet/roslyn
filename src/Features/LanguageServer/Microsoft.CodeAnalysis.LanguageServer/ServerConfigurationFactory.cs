// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export, Shared]
internal class ServerConfigurationFactory
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

        // Use the SharedDependenciesPath option as a proxy for whether or not devkit is running.
        var isDevkitEnabled = !string.IsNullOrEmpty(serverConfiguration.SharedDependenciesPath);
        // Set the standalone option so other features know whether devkit is running.
        _globalOptionService.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, isDevkitEnabled);
    }
}

internal record class ServerConfiguration(
    bool LaunchDebugger,
    LogLevel MinimumLogLevel,
    string? StarredCompletionsPath,
    string? TelemetryLevel,
    string? SessionId,
    string? SharedDependenciesPath,
    IEnumerable<string> ExtensionAssemblyPaths,
    string ExtensionLogDirectory);
