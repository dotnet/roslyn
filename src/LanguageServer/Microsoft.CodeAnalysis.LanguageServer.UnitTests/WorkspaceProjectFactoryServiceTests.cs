// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceProjectFactoryServiceTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task CreateProjectAndBatch()
    {
        var loggerFactory = new LoggerFactory();
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            loggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var _ = exportProvider;

        await exportProvider.GetExportedValue<ServiceBrokerFactory>().CreateAsync();

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
        var project = workspaceFactory.HostWorkspace.CurrentSolution.Projects.Single();

        var document = Assert.Single(project.Documents);
        Assert.Equal(sourceFilePath, document.FilePath);
        Assert.Equal("Folder", Assert.Single(document.Folders));

        var additionalDocument = Assert.Single(project.AdditionalDocuments);
        Assert.Equal(additionalFilePath, additionalDocument.FilePath);
        Assert.Equal("Folder", Assert.Single(additionalDocument.Folders));
    }

    [Fact]
    public async Task LogProjectLoadWithCapabilities()
    {
        var capturingLogger = new CapturingLoggerProvider();
        var loggerFactory = new LoggerFactory([capturingLogger]);
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            loggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var _ = exportProvider;

        await exportProvider.GetExportedValue<ServiceBrokerFactory>().CreateAsync();

        var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var workspaceProjectFactoryServiceInstance = (WorkspaceProjectFactoryService)exportProvider
            .GetExportedValues<IExportedBrokeredService>()
            .Single(service => service.Descriptor == WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor);

        await using var brokeredServiceFactory = new BrokeredServiceProxy<IWorkspaceProjectFactoryService>(
            workspaceProjectFactoryServiceInstance);

        var workspaceProjectFactoryService = await brokeredServiceFactory.GetServiceAsync();
        
        // Test with capabilities
        var projectPath = MakeAbsolutePath("TestProject.csproj");
        using var workspaceProject = await workspaceProjectFactoryService.CreateAndAddProjectAsync(
            new WorkspaceProjectCreationInfo(
                LanguageNames.CSharp,
                "DisplayName",
                FilePath: projectPath,
                new Dictionary<string, string>(),
                ProjectCapabilities: ImmutableArray.Create("CSharp", "Test", "Managed")),
            CancellationToken.None);

        // Verify the log message includes capabilities
        var logMessage = Assert.Single(capturingLogger.LogMessages, m => m.Contains(projectPath));
        Assert.Contains("with capabilities: CSharp, Test, Managed", logMessage);
        
        // Dispose project so we can create another one
        workspaceProject.Dispose();
        capturingLogger.LogMessages.Clear();
        
        // Test without capabilities (backward compatibility)
        var projectPath2 = MakeAbsolutePath("TestProject2.csproj");
        using var workspaceProject2 = await workspaceProjectFactoryService.CreateAndAddProjectAsync(
            new WorkspaceProjectCreationInfo(
                LanguageNames.CSharp,
                "DisplayName2",
                FilePath: projectPath2,
                new Dictionary<string, string>()),
            CancellationToken.None);

        // Verify the log message does NOT include capabilities
        var logMessage2 = Assert.Single(capturingLogger.LogMessages, m => m.Contains(projectPath2));
        Assert.DoesNotContain("with capabilities:", logMessage2);
        Assert.Contains("loaded by C# Dev Kit", logMessage2);
    }

    private static string MakeAbsolutePath(string relativePath)
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine("Z:\\", relativePath);
        else
            return Path.Combine("//", relativePath);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> LogMessages { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(this);
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return new NoOpDisposable();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                provider.LogMessages.Add(formatter(state, exception));
            }

            private sealed class NoOpDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
