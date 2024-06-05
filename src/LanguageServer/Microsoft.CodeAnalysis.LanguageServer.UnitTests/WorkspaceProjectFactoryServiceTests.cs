// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class WorkspaceProjectFactoryServiceTests
{
    [Fact]
    public async Task CreateProjectAndBatch()
    {
        var loggerFactory = new LoggerFactory();
        using var exportProvider = await LanguageServerTestComposition.CreateExportProviderAsync(
            loggerFactory, includeDevKitComponents: false, out var serverConfiguration, out var _);

        exportProvider.GetExportedValue<ServerConfigurationFactory>()
            .InitializeConfiguration(serverConfiguration);
        await exportProvider.GetExportedValue<ServiceBrokerFactory>().CreateAsync();
        var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var workspaceProjectFactoryServiceInstance = (WorkspaceProjectFactoryService)exportProvider
            .GetExportedValues<IExportedBrokeredService>()
            .Single(service => service.Descriptor == WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor);

        await using var brokeredServiceFactory = new BrokeredServiceProxy<IWorkspaceProjectFactoryService>(
            workspaceProjectFactoryServiceInstance);

        var workspaceProjectFactoryService = await brokeredServiceFactory.GetServiceAsync();
        using var workspaceProject = await workspaceProjectFactoryService.CreateAndAddProjectAsync(
            new WorkspaceProjectCreationInfo(LanguageNames.CSharp, "DisplayName", FilePath: null, new Dictionary<string, string>()),
            CancellationToken.None);

        using var batch = await workspaceProject.StartBatchAsync(CancellationToken.None);

        var sourceFilePath = MakeAbsolutePath("SourceFile.cs");
        var additionalFilePath = MakeAbsolutePath("AdditionalFile.txt");

        await workspaceProject.AddSourceFilesAsync([new SourceFileInfo(sourceFilePath, ["Folder"])], CancellationToken.None);
        await workspaceProject.AddAdditionalFilesAsync([new SourceFileInfo(additionalFilePath, FolderNames: ["Folder"])], CancellationToken.None);
        await batch.ApplyAsync(CancellationToken.None);

        // Verify it actually did something; we won't exclusively test each method since those are tested at lower layers
        var project = workspaceFactory.Workspace.CurrentSolution.Projects.Single();

        var document = Assert.Single(project.Documents);
        Assert.Equal(sourceFilePath, document.FilePath);
        Assert.Equal("Folder", Assert.Single(document.Folders));

        var additionalDocument = Assert.Single(project.AdditionalDocuments);
        Assert.Equal(additionalFilePath, additionalDocument.FilePath);
        Assert.Equal("Folder", Assert.Single(additionalDocument.Folders));
    }

    private static string MakeAbsolutePath(string relativePath)
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine("Z:\\", relativePath);
        else
            return Path.Combine("//", relativePath);
    }
}
