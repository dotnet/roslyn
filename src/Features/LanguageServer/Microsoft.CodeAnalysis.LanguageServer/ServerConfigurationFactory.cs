// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export, Shared]
internal class ServerConfigurationFactory
{
    private ServerConfiguration? _serverConfiguration;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServerConfigurationFactory()
    {
    }

    [Export(typeof(ServerConfiguration))]
    public ServerConfiguration ServerConfiguration => _serverConfiguration ?? throw new InvalidOperationException($"{nameof(ServerConfiguration)} has not been initialized");

    public void InitializeConfiguration(ServerConfiguration serverConfiguration) => _serverConfiguration = serverConfiguration;
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
