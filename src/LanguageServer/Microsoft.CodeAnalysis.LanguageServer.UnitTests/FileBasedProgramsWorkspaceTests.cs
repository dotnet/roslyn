// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class FileBasedProgramsWorkspaceTests : AbstractLspMiscellaneousFilesWorkspaceTests, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly TempRoot _tempRoot;
    private readonly TempDirectory _mefCacheDirectory;

    public FileBasedProgramsWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        _tempRoot = new();
        _mefCacheDirectory = _tempRoot.CreateDirectory();
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerProvider.Dispose();
    }

    protected override async ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            _loggerFactory,
            includeDevKitComponents: false,
            cacheDirectory: _mefCacheDirectory.Path,
            extensionPaths: []);

        return exportProvider;
    }

    private protected override async ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath, string content)
    {
        // For the file-based programs, we want to put them in the real workspace via the real host service
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var project = await workspaceFactory.HostProjectFactory.CreateAndAddToWorkspaceAsync(
            Guid.NewGuid().ToString(),
            LanguageNames.CSharp,
            new ProjectSystemProjectCreationInfo { AssemblyName = Guid.NewGuid().ToString() },
            workspaceFactory.ProjectSystemHostInfo);

        project.AddSourceFile(filePath);

        return workspaceFactory.HostWorkspace.CurrentSolution.GetRequiredProject(project.Id).Documents.Single();
    }

    private protected override Workspace GetHostWorkspace(TestLspServer testLspServer)
    {
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        return workspaceFactory.HostWorkspace;
    }
}
