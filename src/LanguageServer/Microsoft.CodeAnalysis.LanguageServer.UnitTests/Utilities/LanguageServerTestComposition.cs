// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class LanguageServerTestComposition
{
    public static Task<ExportProvider> CreateExportProviderAsync(
        ILoggerFactory loggerFactory,
        bool includeDevKitComponents,
        string cacheDirectory,
        string[]? extensionPaths,
        out ServerConfiguration serverConfiguration,
        out IAssemblyLoader assemblyLoader)
    {
        var devKitDependencyPath = includeDevKitComponents ? TestPaths.GetDevKitExtensionPath() : null;
        serverConfiguration = new ServerConfiguration(LaunchDebugger: false,
            LogConfiguration: new LogConfiguration(LogLevel.Trace),
            StarredCompletionsPath: null,
            TelemetryLevel: null,
            SessionId: null,
            ExtensionAssemblyPaths: extensionPaths ?? [],
            DevKitDependencyPath: devKitDependencyPath,
            RazorSourceGenerator: null,
            RazorDesignTimePath: null,
            ExtensionLogDirectory: string.Empty,
            ServerPipeName: null,
            UseStdIo: false);
        var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
        assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);
        return ExportProviderBuilder.CreateExportProviderAsync(extensionManager, assemblyLoader, devKitDependencyPath, cacheDirectory, loggerFactory);
    }
}
