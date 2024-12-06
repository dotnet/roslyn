// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class LanguageServerTestComposition
{
    /// <summary>
    /// Build places DevKit files to this subdirectory.
    /// </summary>
    private const string DevKitExtensionSubdirectory = "DevKit";

    private const string DevKitAssemblyFileName = "Microsoft.VisualStudio.LanguageServices.DevKit.dll";

    private static string GetDevKitExtensionPath()
        => Path.Combine(AppContext.BaseDirectory, DevKitExtensionSubdirectory, DevKitAssemblyFileName);

    public static Task<ExportProvider> CreateExportProviderAsync(
        ILoggerFactory loggerFactory,
        bool includeDevKitComponents,
        string cacheDirectory,
        out ServerConfiguration serverConfiguration,
        out IAssemblyLoader assemblyLoader)
    {
        var devKitDependencyPath = includeDevKitComponents ? GetDevKitExtensionPath() : null;
        serverConfiguration = new ServerConfiguration(LaunchDebugger: false,
            LogConfiguration: new LogConfiguration(LogLevel.Trace),
            StarredCompletionsPath: null,
            TelemetryLevel: null,
            SessionId: null,
            ExtensionAssemblyPaths: [],
            DevKitDependencyPath: devKitDependencyPath,
            RazorSourceGenerator: null,
            RazorDesignTimePath: null,
            ExtensionLogDirectory: string.Empty);
        var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
        assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);
        return ExportProviderBuilder.CreateExportProviderAsync(extensionManager, assemblyLoader, devKitDependencyPath, cacheDirectory, loggerFactory);
    }
}
