// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using AspNet80 = Basic.Reference.Assemblies.AspNet80;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Formatting;

public class DocumentFormattingBenchmark
{
    private const int FormatOperationCount = 100;
    private const string BenchmarkRootPath = @"C:\Benchmark";
    private const string ProjectName = "DocumentFormattingBenchmark";
    private const string ProjectFilePath = @"C:\Benchmark\FormattingBenchmark.csproj";
    private const string GlobalConfigFilePath = @"C:\Benchmark\.globalconfig";
    private const string GeneratedAssemblyPath = @"C:\Benchmark\obj\DocumentFormattingBenchmark.dll";
    private const string DocumentFilePath = @"C:\Benchmark\DocumentFormattingBenchmark.cshtml";
    private const string DocumentRelativePath = "DocumentFormattingBenchmark.cshtml";
    private const string RootNamespace = "Benchmark";

    private static readonly Uri s_documentUri = new(DocumentFilePath);
    private static readonly AnalyzerFileReference s_razorSourceGeneratorReference = new(
        typeof(RazorSourceGenerator).Assembly.Location,
        AnalyzerAssemblyLoader.Instance);

    private AdhocWorkspace? _workspace;
    private DocumentContext? _documentContext;
    private SourceText? _sourceText;
    private ImmutableArray<TextChange> _htmlChanges;
    private RazorFormattingService? _formattingService;
    private RazorFormattingOptions _options;

    [GlobalSetup]
    public void Setup()
    {
        _sourceText = SourceText.From(Resources.GetResourceText("DocumentFormattingBenchmark.cshtml", folder: "Formatting"));
        var htmlFormattedText = SourceText.From(Resources.GetResourceText("DocumentFormattingBenchmark.htmlformatted.cshtml", folder: "Formatting"));
        _htmlChanges = SourceTextDiffer.GetMinimalTextChanges(_sourceText, htmlFormattedText, DiffKind.Line);

        _workspace = new AdhocWorkspace();

        var solution = CreateBenchmarkSolution(_workspace.CurrentSolution, _sourceText, out var documentId);
        if (!_workspace.TryApplyChanges(solution))
        {
            throw new InvalidOperationException("Could not apply the benchmark solution to the Roslyn workspace.");
        }

        var document = _workspace.CurrentSolution.GetAdditionalDocument(documentId).AssumeNotNull();
        var filePathService = new RemoteFilePathService();
        var snapshotManager = new RemoteSnapshotManager(filePathService, NoOpTelemetryReporter.Instance);
        var documentSnapshot = snapshotManager.GetSnapshot(document);
        _documentContext = new DocumentContext(s_documentUri, documentSnapshot);

        var hostServicesProvider = new RemoteHostServicesProvider();
        hostServicesProvider.SetWorkspaceProvider(new WorkspaceProvider(_workspace));

        var clientSettingsManager = new RemoteClientSettingsManager();
        var documentMappingService = new RemoteDocumentMappingService(filePathService, snapshotManager, EmptyLoggerFactory.Instance);
        var razorEditService = new RemoteRazorEditService(documentMappingService, clientSettingsManager, filePathService, snapshotManager, NoOpTelemetryReporter.Instance);

        _formattingService = new RemoteRazorFormattingService(
            documentMappingService,
            razorEditService,
            hostServicesProvider,
            new FormattingLoggerFactory(),
            EmptyLoggerFactory.Instance);

        _options = new RazorFormattingOptions
        {
            InsertSpaces = false,
            TabSize = 4,
            CSharpSyntaxFormattingOptions = RazorCSharpSyntaxFormattingOptions.Default,
        };

        var changeCount = FormatDocumentCore();
        if (changeCount == 0)
        {
            throw new InvalidOperationException("The document formatting benchmark setup produced no formatting changes.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "100x full document formatting of Razor file")]
    public int FormatDocument()
    {
        var totalChangeCount = 0;
        for (var i = 0; i < FormatOperationCount; i++)
        {
            totalChangeCount += FormatDocumentCore();
        }

        return totalChangeCount;
    }

    private int FormatDocumentCore()
    {
        var changes = _formattingService.AssumeNotNull().GetDocumentFormattingChangesAsync(
            _documentContext.AssumeNotNull(),
            _htmlChanges,
            range: null,
            _options,
            CancellationToken.None).GetAwaiter().GetResult();

        return changes.Length;
    }

    private static Solution CreateBenchmarkSolution(Solution solution, SourceText sourceText, out DocumentId documentId)
    {
        var projectId = ProjectId.CreateNewId(debugName: ProjectName);
        documentId = DocumentId.CreateNewId(projectId, debugName: DocumentRelativePath);

        var projectInfo = ProjectInfo.Create(
            id: projectId,
            version: VersionStamp.Create(),
            name: ProjectName,
            assemblyName: ProjectName,
            language: LanguageNames.CSharp,
            filePath: ProjectFilePath,
            parseOptions: CSharpParseOptions.Default.WithFeatures([new("use-roslyn-tokenizer", "true")]),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: AspNet80.ReferenceInfos.All.Select(static referenceInfo => referenceInfo.Reference))
            .WithDefaultNamespace(RootNamespace)
            .WithAnalyzerReferences([s_razorSourceGeneratorReference])
            .WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath(GeneratedAssemblyPath));

        solution = solution.AddProject(projectInfo);
        solution = solution.AddAdditionalDocument(documentId, Path.GetFileName(DocumentFilePath), sourceText, filePath: DocumentFilePath);
        solution = solution.AddAnalyzerConfigDocument(
            DocumentId.CreateNewId(projectId),
            name: ".globalconfig",
            text: SourceText.From(CreateGlobalConfigText()),
            filePath: GlobalConfigFilePath);

        return solution;
    }

    private static string CreateGlobalConfigText()
    {
        var encodedTargetPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(DocumentRelativePath));

        return $$"""
            is_global = true

            build_property.RazorLangVersion = {{RazorLanguageVersion.Preview}}
            build_property.RazorConfiguration = {{FallbackRazorConfiguration.Latest.ConfigurationName}}
            build_property.RootNamespace = {{RootNamespace}}

            # This mirrors the Razor SDK setup used by the Roslyn-based test project shape.
            build_property.SuppressRazorSourceGenerator = true
            build_property.MSBuildProjectDirectory = {{BenchmarkRootPath}}

            [{{DocumentFilePath.Replace('\\', '/')}}]
            build_metadata.AdditionalFiles.TargetPath = {{encodedTargetPath}}
            """;
    }

    private sealed class WorkspaceProvider(Workspace workspace) : IWorkspaceProvider
    {
        public Workspace GetWorkspace() => workspace;
    }

    private sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static readonly AnalyzerAssemblyLoader Instance = new();

        private AnalyzerAssemblyLoader()
        {
        }

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
            => Assembly.LoadFrom(fullPath);
    }
}
