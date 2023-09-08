// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class LanguageServerProjectSystemServiceTests
{
    [Fact]
    public async Task FetchServiceAndLoadProject()
    {
        AsynchronousOperationListenerProvider.Enable(true);

        using var exportProvider = await LanguageServerTestComposition.CreateExportProviderAsync(new LoggerFactory(), includeDevKitComponents: false);
        await exportProvider.GetExportedValue<ServiceBrokerFactory>().CreateAsync();

        // Right now the language service indirectly relies on having an initialized configuration
        // TODO: we shouldn't have to do this in unit tests
        exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(new ServerConfiguration(
            LaunchDebugger: false,
            MinimumLogLevel: LogLevel.Trace,
            StarredCompletionsPath: null,
            TelemetryLevel: null,
            SessionId: null,
            SharedDependenciesPath: null,
            ExtensionAssemblyPaths: SpecializedCollections.EmptyEnumerable<string>(),
            ExtensionLogDirectory: ""));

        var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();

        await using var brokeredServiceFactory = new BrokeredServiceProxy<ILanguageServerProjectSystemService>(exportProvider, LanguageServerProjectSystemServiceDescriptor.ServiceDescriptor);
        var languageServerProjectSystemService = await brokeredServiceFactory.GetServiceAsync();

        // Try loading a trivial project
        using var tempRoot = new TempRoot();
        var tempFile = tempRoot.CreateFile("TestProject", ".csproj");
        tempFile.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net7.0</TargetFramework></PropertyGroup></Project>");

        using var loadedProject = await languageServerProjectSystemService.LoadProjectAsync(tempFile.Path, ImmutableDictionary<string, string>.Empty, CancellationToken.None);

        await exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        Assert.NotEmpty(workspaceFactory.Workspace.CurrentSolution.Projects);
    }
}
