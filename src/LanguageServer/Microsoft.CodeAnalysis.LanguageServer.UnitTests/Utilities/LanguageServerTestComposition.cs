// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class LanguageServerTestComposition
{
    public static async Task<(ExportProvider exportProvider, IAssemblyLoader assemblyLoader)> CreateExportProviderAsync(
        ILoggerFactory loggerFactory,
        bool includeDevKitComponents,
        string cacheDirectory,
        string[]? extensionPaths)
    {
        var devKitDependencyPath = includeDevKitComponents ? TestPaths.GetDevKitExtensionPath() : null;
        var serverConfiguration = new ServerConfiguration(LaunchDebugger: false,
            LogConfiguration: new LogConfiguration(LogLevel.Trace),
            StarredCompletionsPath: null,
            TelemetryLevel: null,
            SessionId: null,
            ExtensionAssemblyPaths: extensionPaths ?? [],
            DevKitDependencyPath: devKitDependencyPath,
            RazorDesignTimePath: null,
            CSharpDesignTimePath: null,
            ExtensionLogDirectory: string.Empty,
            ServerPipeName: null,
            UseStdIo: false,
            AutoLoadProjects: false,
            SourceGeneratorExecutionPreference: SourceGeneratorExecutionPreference.Balanced,
            ParentProcessId: null);
        var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
        var assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);

        var exportProvider = await LanguageServerExportProviderBuilder.CreateExportProviderAsync(TestPaths.GetLanguageServerDirectory(), extensionManager, assemblyLoader, devKitDependencyPath, cacheDirectory, loggerFactory, CancellationToken.None);
        exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(serverConfiguration);
        return (exportProvider, assemblyLoader);
    }
}
