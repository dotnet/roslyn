// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;

internal static class CSharpTestLspServerHelpers
{
    private const string EditRangeSetting = "editRange";

    public static Task<CSharpTestLspServer> CreateCSharpLspServerAsync(
        SourceText csharpSourceText,
        Uri csharpDocumentUri,
        VSInternalServerCapabilities serverCapabilities,
        CancellationToken cancellationToken) =>
        CreateCSharpLspServerAsync(csharpSourceText, csharpDocumentUri, serverCapabilities, new EmptyMappingService(), capabilitiesUpdater: null, cancellationToken);

    public static Task<CSharpTestLspServer> CreateCSharpLspServerAsync(
        SourceText csharpSourceText,
        Uri csharpDocumentUri,
        VSInternalServerCapabilities serverCapabilities,
        Action<VSInternalClientCapabilities> capabilitiesUpdater,
        CancellationToken cancellationToken) =>
        CreateCSharpLspServerAsync(csharpSourceText, csharpDocumentUri, serverCapabilities, new EmptyMappingService(), capabilitiesUpdater, cancellationToken);

    public static Task<CSharpTestLspServer> CreateCSharpLspServerAsync(
        SourceText csharpSourceText,
        Uri csharpDocumentUri,
        VSInternalServerCapabilities serverCapabilities,
        IRazorMappingService razorMappingService,
        Action<VSInternalClientCapabilities> capabilitiesUpdater,
        CancellationToken cancellationToken)
    {
        var files = new[]
        {
            (csharpDocumentUri, csharpSourceText)
        };

        return CreateCSharpLspServerAsync(files, serverCapabilities, razorMappingService, multiTargetProject: true, capabilitiesUpdater, cancellationToken);
    }

    public static async Task<CSharpTestLspServer> CreateCSharpLspServerAsync(
        IEnumerable<(Uri Uri, SourceText SourceText)> files,
        VSInternalServerCapabilities serverCapabilities,
        IRazorMappingService razorMappingService,
        bool multiTargetProject,
        Action<VSInternalClientCapabilities> capabilitiesUpdater,
        CancellationToken cancellationToken)
    {
        var csharpFiles = files.Select(f => new CSharpFile(f.Uri, f.SourceText));

        var exportProvider = TestComposition.RoslynFeatures
            .AddParts(typeof(RazorTestLanguageServerFactory))
            .ExportProviderFactory.CreateExportProvider();

        var metadataReferences = await ReferenceAssemblies.Default.ResolveAsync(language: LanguageNames.CSharp, cancellationToken);
        metadataReferences = metadataReferences.Add(ReferenceUtil.AspNetLatestComponents);

        var workspace = CreateCSharpTestWorkspace(csharpFiles, exportProvider, metadataReferences, razorMappingService, multiTargetProject);

        var clientCapabilities = new VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            TextDocument = new TextDocumentClientCapabilities
            {
                Completion = new VSInternalCompletionSetting
                {
                    CompletionListSetting = new()
                    {
                        ItemDefaults = [EditRangeSetting]
                    },
                    CompletionItem = new()
                    {
                        SnippetSupport = true
                    }
                },
                InlayHint = new()
                {
                    ResolveSupport = new InlayHintResolveSupportSetting { Properties = ["tooltip"] }
                }
            },
            SupportsDiagnosticRequests = true,
            Workspace = new()
            {
                Configuration = true
            }
        };

        capabilitiesUpdater?.Invoke(clientCapabilities);

        return await CSharpTestLspServer.CreateAsync(
            workspace, exportProvider, clientCapabilities, serverCapabilities, cancellationToken);
    }

    private static AdhocWorkspace CreateCSharpTestWorkspace(
        IEnumerable<CSharpFile> files,
        ExportProvider exportProvider,
        ImmutableArray<MetadataReference> metadataReferences,
        IRazorMappingService razorMappingService,
        bool multiTargetProject)
    {
        var workspace = TestWorkspace.CreateWithDiagnosticAnalyzers(exportProvider);

        // Add project and solution to workspace
        var projectInfoNet60 = ProjectInfo.Create(
            id: ProjectId.CreateNewId("TestProject (net6.0)"),
            version: VersionStamp.Default,
            name: "TestProject (net6.0)",
            assemblyName: "TestProject.dll",
            language: LanguageNames.CSharp,
            filePath: @"C:\TestSolution\TestProject.csproj",
            metadataReferences: metadataReferences).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath(@"C:\TestSolution\obj\TestProject.dll"));

        var projectInfoNet100 = ProjectInfo.Create(
            id: ProjectId.CreateNewId("TestProject (net10.0)"),
            version: VersionStamp.Default,
            name: "TestProject (net10.0)",
            assemblyName: "TestProject.dll",
            language: LanguageNames.CSharp,
            filePath: @"C:\TestSolution\TestProject.csproj",
            metadataReferences: metadataReferences);

        ProjectInfo[] projectInfos = multiTargetProject
            ? [projectInfoNet60, projectInfoNet100]
            : [projectInfoNet100];

        foreach (var projectInfo in projectInfos)
        {
            workspace.AddProject(projectInfo);
        }

        // Add document to workspace. We use an IVT method to create the DocumentInfo variable because there's
        // a special constructor in Roslyn that will help identify the document as belonging to Razor.
        var languageServerFactory = exportProvider.GetExportedValue<AbstractRazorLanguageServerFactoryWrapper>();

        var documentCount = 0;
        foreach (var (documentUri, csharpSourceText) in files)
        {
            var documentFilePath = documentUri.GetDocumentFilePath();
            var textAndVersion = TextAndVersion.Create(csharpSourceText, VersionStamp.Default, documentFilePath);

            foreach (var projectInfo in projectInfos)
            {
                var documentInfo = languageServerFactory.CreateDocumentInfo(
                    id: DocumentId.CreateNewId(projectInfo.Id),
                    name: "TestDocument" + documentCount,
                    filePath: documentFilePath,
                    loader: TextLoader.From(textAndVersion),
                    razorDocumentServiceProvider: new TestRazorDocumentServiceProvider(razorMappingService));

                workspace.AddDocument(documentInfo);
            }

            documentCount++;
        }

        return workspace;
    }

    private record CSharpFile(Uri DocumentUri, SourceText CSharpSourceText);

    private class EmptyMappingService : IRazorMappingService
    {
        public Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorMappedSpanResult>();
        }

        public Task<ImmutableArray<RazorMappedEditResult>> MapTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorMappedEditResult>();
        }
    }
}
