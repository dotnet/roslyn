// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Loader;
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

    public static Task<ExportProvider> CreateExportProviderAsync(ILoggerFactory loggerFactory, bool includeDevKitComponents)
    {
        var devKitDependencyPath = includeDevKitComponents ? GetDevKitExtensionPath() : null;
        // Preload the devkit assemblies - the normal process hooks the resolving event, but that doesn't run at this entrypoint.
        if (devKitDependencyPath != null)
        {
            foreach (var assembly in Directory.EnumerateFiles(Path.GetDirectoryName(devKitDependencyPath)!, "*.dll"))
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(assembly);
            }
        }

        var serverConfiguration = new ServerConfiguration(LaunchDebugger: false,
            MinimumLogLevel: LogLevel.Trace,
            StarredCompletionsPath: null,
            TelemetryLevel: null,
            SessionId: null,
            ExtensionAssemblyPaths: [],
            DevKitDependencyPath: devKitDependencyPath,
            ExtensionLogDirectory: string.Empty);
        var extensionAssemblyManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
        return ExportProviderBuilder.CreateExportProviderAsync(extensionAssemblyManager, devKitDependencyPath, loggerFactory: loggerFactory);
    }
}
