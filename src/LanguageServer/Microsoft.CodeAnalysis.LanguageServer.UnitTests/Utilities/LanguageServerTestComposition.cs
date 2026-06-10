// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class LanguageServerTestComposition
{
    public static Task<ExportProvider> CreateLanguageServerExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader,
        string cacheDirectory)
    {
        return LanguageServerExportProviderBuilder.CreateExportProviderAsync(TestPaths.GetLanguageServerDirectory(), extensionManager, assemblyLoader, serverConfiguration, cacheDirectory, loggerFactory, CancellationToken.None);
    }

    public static ExportProvider GetSharedExportProvider(ServerConfiguration serverConfiguration, ILoggerFactory loggerFactory)
    {
        Contract.ThrowIfTrue(serverConfiguration.ExtensionAssemblyPaths.Any(), "Tests that require extension assemblies should use AbstractLanguageServerMefHost instead");
        var exportProvider = serverConfiguration.DevKitDependencyPath != null
            ? s_devKit.ExportProviderFactory.CreateExportProvider()
            : s_languageServer.ExportProviderFactory.CreateExportProvider();

        LanguageServerExportProviderBuilder.TestAccessor.InitializeManualExports(exportProvider, new ExtensionAssemblyManager([], [], []), loggerFactory, serverConfiguration);
        return exportProvider;
    }

    private static readonly TestComposition s_languageServer = CreateBaseComposition();
    private static readonly TestComposition s_devKit = CreateDevKitComposition();

    private static TestComposition CreateBaseComposition()
    {
        var languageServerDirectory = TestPaths.GetLanguageServerDirectory();
        var languageServerDlls = LanguageServerExportProviderBuilder.TestAccessor.FindMefAssemblies(languageServerDirectory);

        return TestComposition.Empty.AddAssemblies(languageServerDlls.Select(Assembly.LoadFrom));
    }

    private static TestComposition CreateDevKitComposition()
    {
        var devKitDirectory = TestPaths.GetDevKitExtensionPath();
        return s_languageServer.AddAssemblies(Assembly.LoadFrom(devKitDirectory));
    }
}
