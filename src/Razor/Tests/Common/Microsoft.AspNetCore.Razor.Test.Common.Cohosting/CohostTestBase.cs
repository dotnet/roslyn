// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public abstract class CohostTestBase(ITestOutputHelper testOutputHelper) : ToolingTestBase(testOutputHelper)
{
    private ExportProvider? _exportProvider;
    private TestIncompatibleProjectService _incompatibleProjectService = null!;
    private RemoteClientInitializationOptions _clientInitializationOptions;
    private RemoteClientLSPInitializationOptions _clientLSPInitializationOptions;
    private CodeAnalysis.Workspace? _localWorkspace;
    private ExportProvider? _localExportProvider;
    private IClientSettingsManager? _clientSettingsManager;

    private protected abstract IRemoteServiceInvoker RemoteServiceInvoker { get; }
    private protected abstract IFilePathService FilePathService { get; }
    private protected abstract TestComposition LocalComposition { get; }

    private protected TestIncompatibleProjectService IncompatibleProjectService => _incompatibleProjectService.AssumeNotNull();
    private protected RemoteLanguageServerFeatureOptions FeatureOptions => OOPExportProvider.GetExportedValue<RemoteLanguageServerFeatureOptions>();
    private protected RemoteClientCapabilitiesService ClientCapabilitiesService => (RemoteClientCapabilitiesService)OOPExportProvider.GetExportedValue<IClientCapabilitiesService>();
    private protected CodeAnalysis.Workspace LocalWorkspace => _localWorkspace.AssumeNotNull();
    private protected IClientSettingsManager ClientSettingsManager => _clientSettingsManager.AssumeNotNull();

    /// <summary>
    /// The export provider for client services (Roslyn)
    /// </summary>
    private protected ExportProvider LocalExportProvider => _localExportProvider.AssumeNotNull();

    /// <summary>
    /// The export provider for Razor OOP services (not Roslyn)
    /// </summary>
    private protected ExportProvider OOPExportProvider => _exportProvider.AssumeNotNull();

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Create a new isolated MEF composition.
        // Note that this uses a cached catalog and configuration for performance.
        try
        {
            _exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory: null, DisposalToken);
        }
        catch (CompositionFailedException ex) when (ex.Errors is not null)
        {
            Assert.Fail($"""
                Errors in the Remote MEF composition:

                {string.Join(Environment.NewLine, ex.Errors.SelectMany(e => e).Select(e => e.Message))}
                """);
        }

        AddDisposable(_exportProvider);

        _incompatibleProjectService = new TestIncompatibleProjectService();

        var remoteLogger = _exportProvider.GetExportedValue<RemoteLoggerFactory>();
        remoteLogger.SetTargetLoggerFactory(LoggerFactory);
        remoteLogger.AddLoggerProvider(new ThrowingErrorLoggerProvider());

        _clientInitializationOptions = new()
        {
            ReturnCodeActionAndRenamePathsWithPrefixedSlash = false,
            SupportsFileManipulation = true,
            ShowAllCSharpCodeActions = false,
        };
        UpdateClientInitializationOptions(c => c);

        _clientLSPInitializationOptions = GetRemoteClientLSPInitializationOptions();
        UpdateClientLSPInitializationOptions(c => c);

        // Force initialization and creation of the remote workspace. It will be filled in later.
        var traceSource = new TraceSource("Cohost test remote initialization");
        traceSource.Listeners.Add(new XunitTraceListener(TestOutputHelper));
        await RemoteWorkspaceProvider.TestAccessor.InitializeRemoteExportProviderBuilderAsync(Path.GetTempPath(), traceSource, DisposalToken);
        _ = RemoteWorkspaceProvider.Instance.GetWorkspace();

        _localWorkspace = CreateLocalWorkspace();

        _clientSettingsManager = CreateClientSettingsManager();
        _clientSettingsManager.ClientSettingsChanged += ClientSettingsManager_ClientSettingsChanged;
    }

    private protected abstract IClientSettingsManager CreateClientSettingsManager();

    private void ClientSettingsManager_ClientSettingsChanged(object? sender, EventArgs e)
    {
        var remoteClientManager = OOPExportProvider.GetExportedValue<RemoteClientSettingsManager>();
        remoteClientManager.Update(_clientSettingsManager.AssumeNotNull().GetClientSettings());
    }

    protected override Task DisposeAsync()
    {
        _clientSettingsManager?.ClientSettingsChanged -= ClientSettingsManager_ClientSettingsChanged;

        return base.DisposeAsync();
    }

    private AdhocWorkspace CreateLocalWorkspace()
    {
        var composition = ConfigureLocalComposition(LocalComposition);

        // We can't enforce that the composition is entirely valid, because we don't have a full MEF catalog, but we
        // can assume there should be no errors related to Razor, and having this array makes debugging failures a lot
        // easier.
        var errors = composition.GetCompositionErrors().ToArray();
        // RazorInProcLanguageClient is a Roslyn type, which we don't care about, so no need to worry about false positives there,
        // but command line builds fail to compose it correctly.
        AssertEx.EqualOrDiff("", string.Join(Environment.NewLine, errors.Where(e => e.Contains("Razor") && !e.Contains("RazorInProcLanguageClient"))));

        _localExportProvider = composition.ExportProviderFactory.CreateExportProvider();
        AddDisposable(_localExportProvider);
        var workspace = TestWorkspace.CreateWithDiagnosticAnalyzers(_localExportProvider);
        AddDisposable(workspace);
        return workspace;
    }

    private protected virtual TestComposition ConfigureLocalComposition(TestComposition composition)
        => composition;

    private protected abstract RemoteClientLSPInitializationOptions GetRemoteClientLSPInitializationOptions();

    private protected void UpdateClientInitializationOptions(Func<RemoteClientInitializationOptions, RemoteClientInitializationOptions> mutation)
    {
        _clientInitializationOptions = mutation(_clientInitializationOptions);
        FeatureOptions.SetOptions(_clientInitializationOptions);
    }

    private protected void UpdateClientLSPInitializationOptions(Func<RemoteClientLSPInitializationOptions, RemoteClientLSPInitializationOptions> mutation)
    {
        _clientLSPInitializationOptions = mutation(_clientLSPInitializationOptions);

        var lifetimeServices = OOPExportProvider.GetExportedValues<ILspLifetimeService>();
        foreach (var service in lifetimeServices)
        {
            service.OnLspInitialized(_clientLSPInitializationOptions);
        }
    }

    private protected abstract TextDocument CreateProjectAndRazorDocument(
        string contents,
        RazorFileKind? fileKind = null,
        string? documentFilePath = null,
        (string fileName, string contents)[]? additionalFiles = null,
        bool inGlobalNamespace = false,
        bool miscellaneousFile = false,
        bool addDefaultImports = true,
        Action<RazorProjectBuilder>? projectConfigure = null);

    private protected TextDocument CreateProjectAndRazorDocument(
        CodeAnalysis.Workspace remoteWorkspace,
        string contents,
        RazorFileKind? fileKind = null,
        string? documentFilePath = null,
        (string fileName, string contents)[]? additionalFiles = null,
        bool inGlobalNamespace = false,
        bool miscellaneousFile = false,
        bool addDefaultImports = true,
        Action<RazorProjectBuilder>? projectConfigure = null)
    {
        // Using IsLegacy means null == component, so easier for test authors
        var isComponent = fileKind != RazorFileKind.Legacy;

        documentFilePath ??= isComponent
            ? TestProjectData.SomeProjectComponentFile1.FilePath
            : TestProjectData.SomeProjectFile1.FilePath;

        var projectId = ProjectId.CreateNewId(debugName: TestProjectData.SomeProject.DisplayName);
        var documentId = DocumentId.CreateNewId(projectId, debugName: documentFilePath);

        return CreateProjectAndRazorDocument(remoteWorkspace, projectId, miscellaneousFile, documentId, documentFilePath, contents, additionalFiles, inGlobalNamespace, addDefaultImports, projectConfigure);
    }

    private protected static TextDocument CreateProjectAndRazorDocument(CodeAnalysis.Workspace workspace, ProjectId projectId, bool miscellaneousFile, DocumentId documentId, string documentFilePath, string contents, (string fileName, string contents)[]? additionalFiles, bool inGlobalNamespace, bool addDefaultImports, Action<RazorProjectBuilder>? projectConfigure)
    {
        return AddProjectAndRazorDocument(workspace.CurrentSolution, TestProjectData.SomeProject.FilePath, projectId, documentId, documentFilePath, contents, miscellaneousFile, additionalFiles, inGlobalNamespace, addDefaultImports, projectConfigure);
    }

    private protected static TextDocument AddProjectAndRazorDocument(
        Solution solution,
        [DisallowNull] string? projectFilePath,
        ProjectId projectId,
        DocumentId documentId,
        string documentFilePath,
        string contents,
        bool miscellaneousFile = false,
        (string fileName, string contents)[]? additionalFiles = null,
        bool inGlobalNamespace = false,
        bool addDefaultImports = true,
        Action<RazorProjectBuilder>? projectConfigure = null)
    {
        var builder = new RazorProjectBuilder(projectId);

        if (projectConfigure is not null)
        {
            projectConfigure(builder);
        }

        builder.AddReferences(miscellaneousFile
            ? Net461.ReferenceInfos.All.Select(r => r.Reference) // This isn't quite what Roslyn does, but its close enough for our tests
            : AspNet80.ReferenceInfos.All.Select(r => r.Reference));
        builder.GenerateGlobalConfigFile = !miscellaneousFile;
        builder.RootNamespace = null;

        builder.AddAdditionalDocument(documentId, documentFilePath, SourceText.From(contents));

        if (!miscellaneousFile)
        {
            builder.ProjectFilePath = projectFilePath;

            if (!inGlobalNamespace)
            {
                builder.RootNamespace = TestProjectData.SomeProject.RootNamespace;
            }

            if (addDefaultImports)
            {
                builder.AddAdditionalDocument(
                    filePath: TestProjectData.SomeProjectComponentImportFile1.FilePath,
                    text: SourceText.From("""
                        @using Microsoft.AspNetCore.Components
                        @using Microsoft.AspNetCore.Components.Authorization
                        @using Microsoft.AspNetCore.Components.Forms
                        @using Microsoft.AspNetCore.Components.Routing
                        @using Microsoft.AspNetCore.Components.Web
                        """));
                builder.AddAdditionalDocument(
                    filePath: TestProjectData.SomeProjectImportFile.FilePath,
                    text: SourceText.From("""
                        @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
                        """));
            }

            if (additionalFiles is not null)
            {
                foreach (var file in additionalFiles)
                {
                    if (Path.GetExtension(file.fileName) == ".cs")
                    {
                        builder.AddDocument(filePath: file.fileName, text: SourceText.From(file.contents));
                    }
                    else
                    {
                        builder.AddAdditionalDocument(filePath: file.fileName, text: SourceText.From(file.contents));
                    }
                }
            }
        }

        return builder.Build(solution).GetAdditionalDocument(documentId).AssumeNotNull();
    }

    protected static Uri FileUri(string projectRelativeFileName)
        => new(FilePath(projectRelativeFileName));

    protected static string FilePath(string projectRelativeFileName)
        => Path.GetFullPath(Path.Combine(TestProjectData.SomeProjectPath, projectRelativeFileName));
}
