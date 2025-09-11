// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes;

[UseExportProvider]
public sealed class CodeFixServiceTests
{
    private static readonly TestComposition s_compositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures;

    [Fact]
    public async Task TestGetFirstDiagnosticWithFixAsync()
    {
        var fixers = CreateFixers();
        var code = """
            a
            """;
        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);

        var diagnosticService = workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();

        var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));

        var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetRequiredService<IErrorLoggerService>()));
        var fixService = new CodeFixService(
            logger, fixers, configurationProviders: []);

        var reference = new MockAnalyzerReference();
        var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
        var document = project.Documents.Single();
        var unused = await fixService.GetMostSevereFixAsync(
            document, TextSpan.FromBounds(0, 0), priority: null, CancellationToken.None);

        var fixer1 = (MockFixer)fixers.Single().Value;
        var fixer2 = (MockFixer)reference.Fixers.Single();

        // check to make sure both of them are called.
        Assert.True(fixer1.Called);
        Assert.True(fixer2.Called);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41116")]
    public async Task TestGetFixesAsyncWithDuplicateDiagnostics()
    {
        var codeFix = new MockFixer();

        // Add duplicate analyzers to get duplicate diagnostics.
        var analyzerReference = new MockAnalyzerReference(
                codeFix,
                [new MockAnalyzerReference.MockDiagnosticAnalyzer(), new MockAnalyzerReference.MockDiagnosticAnalyzer()]);

        var tuple = ServiceSetup(codeFix);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager, analyzerReference);

        // Verify that we do not crash when computing fixes.
        _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);

        // Verify that code fix is invoked with both the diagnostics in the context,
        // i.e. duplicate diagnostics are not silently discarded by the CodeFixService.
        Assert.Equal(2, codeFix.ContextDiagnosticsCount);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45779")]
    public async Task TestGetFixesAsyncHasNoDuplicateConfigurationActions()
    {
        var codeFix = new MockFixer();

        // Add analyzers with duplicate ID and/or category to get duplicate diagnostics.
        var analyzerReference = new MockAnalyzerReference(
                codeFix,
                [
                    new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category1"),
                    new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category1"),
                    new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category2"),
                    new MockAnalyzerReference.MockDiagnosticAnalyzer("ID2", "Category2"),
                ]);

        var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: true);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager, analyzerReference);

        // Verify registered configuration code actions do not have duplicates.
        var fixCollections = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);
        var codeActions = fixCollections.SelectManyAsArray(c => c.Fixes.Select(f => f.Action));
        Assert.Equal(7, codeActions.Length);
        var uniqueTitles = new HashSet<string>();
        foreach (var codeAction in codeActions)
        {
            Assert.True(codeAction is AbstractConfigurationActionWithNestedActions);
            Assert.True(uniqueTitles.Add(codeAction.Title));
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56843")]
    public async Task TestGetFixesAsyncForFixableAndNonFixableAnalyzersAsync()
    {
        var codeFix = new MockFixer();
        var analyzerWithFix = new MockAnalyzerReference.MockDiagnosticAnalyzer();
        Assert.Equal(codeFix.FixableDiagnosticIds.Single(), analyzerWithFix.SupportedDiagnostics.Single().Id);

        var analyzerWithoutFix = new MockAnalyzerReference.MockDiagnosticAnalyzer("AnalyzerWithoutFixId", "Category");
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzerWithFix, analyzerWithoutFix);
        var analyzerReference = new MockAnalyzerReference(codeFix, analyzers);

        // Verify no callbacks received at initialization.
        Assert.False(analyzerWithFix.ReceivedCallback);
        Assert.False(analyzerWithoutFix.ReceivedCallback);

        var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: true);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager, analyzerReference);

        // Verify only analyzerWithFix is executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Normal'.
        _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
            CodeActionRequestPriority.Default,
            cancellationToken: CancellationToken.None);
        Assert.True(analyzerWithFix.ReceivedCallback);
        Assert.False(analyzerWithoutFix.ReceivedCallback);

        // Verify both analyzerWithFix and analyzerWithoutFix are executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Lowest'.
        _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
            CodeActionRequestPriority.Lowest,
            cancellationToken: CancellationToken.None);
        Assert.True(analyzerWithFix.ReceivedCallback);
        Assert.True(analyzerWithoutFix.ReceivedCallback);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1450689")]
    public async Task TestGetFixesAsyncForDocumentDiagnosticAnalyzerAsync()
    {
        // TS has special DocumentDiagnosticAnalyzer that report 0 SupportedDiagnostics.
        // We need to ensure that we don't skip these document analyzers
        // when computing the diagnostics/code fixes for "Normal" priority bucket, which
        // normally only execute those analyzers which report at least one fixable supported diagnostic.
        var documentDiagnosticAnalyzer = new MockAnalyzerReference.MockDocumentDiagnosticAnalyzer(reportedDiagnosticIds: []);
        Assert.Empty(documentDiagnosticAnalyzer.SupportedDiagnostics);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(documentDiagnosticAnalyzer);
        var codeFix = new MockFixer();
        var analyzerReference = new MockAnalyzerReference(codeFix, analyzers);

        // Verify no callbacks received at initialization.
        Assert.False(documentDiagnosticAnalyzer.ReceivedCallback);

        var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: false);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager, analyzerReference);

        // Verify both analyzers are executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Normal'.
        _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
            CodeActionRequestPriority.Default,
            cancellationToken: CancellationToken.None);
        Assert.True(documentDiagnosticAnalyzer.ReceivedCallback);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67354")]
    public async Task TestGetFixesAsyncForGeneratorDiagnosticAsync()
    {
        // We have a special GeneratorDiagnosticsPlaceholderAnalyzer that report 0 SupportedDiagnostics.
        // We need to ensure that we don't skip this special analyzer
        // when computing the diagnostics/code fixes for "Normal" priority bucket, which
        // normally only execute those analyzers which report at least one fixable supported diagnostic.
        // Note that this special placeholder analyzer instance is always included for the project,
        // we do not need to include it in the passed in analyzers.
        Assert.Empty(GeneratorDiagnosticsPlaceholderAnalyzer.Instance.SupportedDiagnostics);

        var analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
        var generator = new MockAnalyzerReference.MockGenerator();
        var generators = ImmutableArray.Create<ISourceGenerator>(generator);
        var fixTitle = "Fix Title";
        var codeFix = new MockFixer(fixTitle);
        var codeFixes = ImmutableArray.Create<CodeFixProvider>(codeFix);
        var analyzerReference = new MockAnalyzerReference(codeFixes, analyzers, generators);

        var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: false);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager, analyzerReference);

        Assert.False(codeFix.Called);
        var fixCollectionSet = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
            CodeActionRequestPriority.Default,
            cancellationToken: CancellationToken.None);
        Assert.True(codeFix.Called);
        var fixCollection = Assert.Single(fixCollectionSet);
        Assert.Equal(MockFixer.Id, fixCollection.Diagnostics.First().Id);
        var fix = Assert.Single(fixCollection.Fixes);
        Assert.Equal(fixTitle, fix.Action.Title);
    }

    [Fact]
    public Task TestGetCodeFixWithExceptionInRegisterMethod_Diagnostic()
        => GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethod());

    [Fact]
    public Task TestGetCodeFixWithExceptionInRegisterMethod_Fixes()
        => GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethod());

    [Fact]
    public Task TestGetCodeFixWithExceptionInRegisterMethodAsync_Diagnostic()
        => GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethodAsync());

    [Fact]
    public Task TestGetCodeFixWithExceptionInRegisterMethodAsync_Fixes()
        => GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethodAsync());

    [Fact]
    public Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Diagnostic()
        => GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());

    [Fact]
    public Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Fixes()
        => GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21533")]
    public Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Diagnostic2()
        => GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21533")]
    public Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Fixes2()
        => GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());

    [Fact]
    public async Task TestGetCodeFixWithExceptionInGetFixAllProvider()
        => await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInGetFixAllProvider());

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45851")]
    public async Task TestGetCodeFixWithExceptionOnCodeFixProviderCreation()
        => await GetAddedFixesAsync(
            new MockFixer(),
            new MockAnalyzerReference.MockDiagnosticAnalyzer(),
            throwExceptionInFixerCreation: true);

    private static Task<ImmutableArray<CodeFixCollection>> GetAddedFixesWithExceptionValidationAsync(CodeFixProvider codefix)
        => GetAddedFixesAsync(codefix, diagnosticAnalyzer: new MockAnalyzerReference.MockDiagnosticAnalyzer(), exception: true);

    private static async Task<ImmutableArray<CodeFixCollection>> GetAddedFixesAsync(CodeFixProvider codefix, DiagnosticAnalyzer diagnosticAnalyzer, bool exception = false, bool throwExceptionInFixerCreation = false)
    {
        var tuple = ServiceSetup(codefix, throwExceptionInFixerCreation: throwExceptionInFixerCreation);

        using var workspace = tuple.workspace;

        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();

        var errorReported = false;
        errorReportingService.OnError = message => errorReported = true;

        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager);
        var reference = new MockAnalyzerReference(codefix, [diagnosticAnalyzer]);
        var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
        document = project.Documents.Single();
        var fixes = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);

        if (exception)
        {
            Assert.True(extensionManager.IsDisabled(codefix));
            Assert.False(extensionManager.IsIgnored(codefix));
        }

        Assert.Equal(exception || throwExceptionInFixerCreation, errorReported);

        return fixes;
    }

    private static async Task GetFirstDiagnosticWithFixWithExceptionValidationAsync(CodeFixProvider codefix)
    {
        var tuple = ServiceSetup(codefix);
        using var workspace = tuple.workspace;

        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();

        var errorReported = false;
        errorReportingService.OnError = message => errorReported = true;

        GetDocumentAndExtensionManager(workspace, out var document, out var extensionManager);
        var unused = await tuple.codeFixService.GetMostSevereFixAsync(
            document, TextSpan.FromBounds(0, 0), priority: null, CancellationToken.None);
        Assert.True(extensionManager.IsDisabled(codefix));
        Assert.False(extensionManager.IsIgnored(codefix));
        Assert.True(errorReported);
    }

    private static (EditorTestWorkspace workspace, CodeFixService codeFixService, IErrorLoggerService errorLogger) ServiceSetup(
        CodeFixProvider codefix,
        bool includeConfigurationFixProviders = false,
        bool throwExceptionInFixerCreation = false,
        EditorTestHostDocument? additionalDocument = null,
        string code = "class Program { }")
        => ServiceSetup([codefix], includeConfigurationFixProviders, throwExceptionInFixerCreation, additionalDocument, code);

    private static (EditorTestWorkspace workspace, CodeFixService codeFixService, IErrorLoggerService errorLogger) ServiceSetup(
        ImmutableArray<CodeFixProvider> codefixers,
        bool includeConfigurationFixProviders = false,
        bool throwExceptionInFixerCreation = false,
        EditorTestHostDocument? additionalDocument = null,
        string code = "class Program { }")
    {
        var fixers = codefixers.Select(codefix =>
            new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(
            () => throwExceptionInFixerCreation ? throw new Exception() : codefix,
            new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));

        var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);

        if (additionalDocument != null)
        {
            workspace.Projects.Single().AddAdditionalDocument(additionalDocument);
            workspace.AdditionalDocuments.Add(additionalDocument);
            workspace.OnAdditionalDocumentAdded(additionalDocument.ToDocumentInfo());
        }

        var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));

        var diagnosticService = workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => new TestErrorLogger()));
        var errorLogger = logger.First().Value;

        var configurationFixProviders = includeConfigurationFixProviders
            ? workspace.ExportProvider.GetExports<IConfigurationFixProvider, CodeChangeProviderMetadata>()
            : [];

        var fixService = new CodeFixService(logger, fixers, configurationFixProviders);
        return (workspace, fixService, errorLogger);
    }

    private static void GetDocumentAndExtensionManager(
        EditorTestWorkspace workspace,
        out TextDocument document,
        out EditorLayerExtensionManager.ExtensionManager extensionManager,
        MockAnalyzerReference? analyzerReference = null,
        TextDocumentKind documentKind = TextDocumentKind.Document)
    {
        // register diagnostic engine to solution crawler

        var reference = analyzerReference ?? new MockAnalyzerReference();
        var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
        document = documentKind switch
        {
            TextDocumentKind.Document => project.Documents.Single(),
            TextDocumentKind.AdditionalDocument => project.AdditionalDocuments.Single(),
            TextDocumentKind.AnalyzerConfigDocument => project.AnalyzerConfigDocuments.Single(),
            _ => throw new NotImplementedException(),
        };
        extensionManager = (EditorLayerExtensionManager.ExtensionManager)document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
    }

    private static IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> CreateFixers()
        => [new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => new MockFixer(), new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp))];

    internal sealed class MockFixer : CodeFixProvider
    {
        public const string Id = "MyDiagnostic";
        private readonly string? _registerFixWithTitle;
        public bool Called;
        public int ContextDiagnosticsCount;

        public MockFixer(string? registerFixWithTitle = null)
        {
            _registerFixWithTitle = registerFixWithTitle;
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return [Id]; }
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Called = true;
            ContextDiagnosticsCount = context.Diagnostics.Length;
            if (_registerFixWithTitle != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        _registerFixWithTitle,
                        createChangedDocument: _ => Task.FromResult(context.Document)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MockAnalyzerReference
        : AnalyzerReference, ICodeFixProviderFactory, SerializerService.TestAccessor.IAnalyzerReferenceWithGuid
    {
        public readonly ImmutableArray<CodeFixProvider> Fixers;
        public readonly ImmutableArray<DiagnosticAnalyzer> Analyzers;
        public readonly ImmutableArray<ISourceGenerator> Generators;

        private static readonly ImmutableArray<CodeFixProvider> s_defaultFixers = [new MockFixer()];
        private static readonly ImmutableArray<DiagnosticAnalyzer> s_defaultAnalyzers = [new MockDiagnosticAnalyzer()];

        public MockAnalyzerReference(ImmutableArray<CodeFixProvider> fixers, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<ISourceGenerator> generators)
        {
            Fixers = fixers;
            Analyzers = analyzers;
            Generators = generators;
        }

        public MockAnalyzerReference(ImmutableArray<CodeFixProvider> fixers, ImmutableArray<DiagnosticAnalyzer> analyzers)
            : this(fixers, analyzers, [])
        {
        }

        public MockAnalyzerReference(CodeFixProvider? fixer, ImmutableArray<DiagnosticAnalyzer> analyzers)
            : this(fixer != null ? [fixer] : [],
                   analyzers)
        {
        }

        public MockAnalyzerReference()
            : this(s_defaultFixers, s_defaultAnalyzers)
        {
        }

        public MockAnalyzerReference(CodeFixProvider? fixer)
            : this(fixer, s_defaultAnalyzers)
        {
        }

        public override string Display
        {
            get
            {
                return "MockAnalyzerReference";
            }
        }

        public override string FullPath
        {
            get
            {
                return string.Empty;
            }
        }

        public override object Id
        {
            get
            {
                return "MockAnalyzerReference";
            }
        }

        public Guid Guid { get; } = Guid.NewGuid();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => Analyzers;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => [];

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
            => Generators;

        public ImmutableArray<CodeFixProvider> GetFixers()
            => Fixers;

        private static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics(ImmutableArray<(string id, string category)> reportedDiagnosticIdsWithCategories)
        {
            var builder = ArrayBuilder<DiagnosticDescriptor>.GetInstance();
            foreach (var (diagnosticId, category) in reportedDiagnosticIdsWithCategories)
            {
                var descriptor = new DiagnosticDescriptor(diagnosticId, "MockDiagnostic", "MockDiagnostic", category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
                builder.Add(descriptor);
            }

            return builder.ToImmutableAndFree();
        }

        public sealed class MockDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public MockDiagnosticAnalyzer(ImmutableArray<(string id, string category)> reportedDiagnosticIdsWithCategories)
                => SupportedDiagnostics = CreateSupportedDiagnostics(reportedDiagnosticIdsWithCategories);

            public MockDiagnosticAnalyzer(string diagnosticId, string category)
                : this(ImmutableArray.Create((diagnosticId, category)))
            {
            }

            public MockDiagnosticAnalyzer(ImmutableArray<string> reportedDiagnosticIds)
                : this(reportedDiagnosticIds.SelectAsArray(id => (id, "InternalCategory")))
            {
            }

            public MockDiagnosticAnalyzer()
                : this(ImmutableArray.Create(MockFixer.Id))
            {
            }

            public bool ReceivedCallback { get; private set; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    this.ReceivedCallback = true;

                    foreach (var descriptor in SupportedDiagnostics)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(descriptor, c.Tree.GetLocation(TextSpan.FromBounds(0, 0))));
                    }
                });
            }
        }

        public sealed class MockDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
        {
            public MockDocumentDiagnosticAnalyzer(ImmutableArray<(string id, string category)> reportedDiagnosticIdsWithCategories)
                => SupportedDiagnostics = CreateSupportedDiagnostics(reportedDiagnosticIdsWithCategories);

            public MockDocumentDiagnosticAnalyzer(ImmutableArray<string> reportedDiagnosticIds)
                : this(reportedDiagnosticIds.SelectAsArray(id => (id, "InternalCategory")))
            {
            }

            public MockDocumentDiagnosticAnalyzer()
                : this(ImmutableArray.Create(MockFixer.Id))
            {
            }

            public bool ReceivedCallback { get; private set; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument document, SyntaxTree? tree, CancellationToken cancellationToken)
            {
                ReceivedCallback = true;
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument document, SyntaxTree? tree, CancellationToken cancellationToken)
            {
                ReceivedCallback = true;
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }
        }

#pragma warning disable RS1042 // Do not implement
        public sealed class MockGenerator : ISourceGenerator
#pragma warning restore RS1042 // Do not implement
        {
            private readonly DiagnosticDescriptor s_descriptor = new(MockFixer.Id, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public void Initialize(GeneratorInitializationContext context)
            {
            }

            public void Execute(GeneratorExecutionContext context)
            {
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_descriptor, tree.GetLocation(new TextSpan(0, 1))));
                }
            }
        }
    }

    internal sealed class TestErrorLogger : IErrorLoggerService
    {
        public Dictionary<string, string> Messages = [];

        public void LogException(object source, Exception exception)
            => Messages.Add(source.GetType().Name, ToLogFormat(exception));

        private static string ToLogFormat(Exception exception)
            => exception.Message + Environment.NewLine + exception.StackTrace;
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixCodeFixersAsync()
    {
        // No NuGet or VSIX code fix provider
        // Verify no code action registered
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: null,
            expectedNuGetFixerCodeActionWasRegistered: false,
            vsixFixer: null,
            expectedVsixFixerCodeActionWasRegistered: false);

        // Only NuGet code fix provider
        // Verify only NuGet fixer's code action registered
        var fixableDiagnosticIds = ImmutableArray.Create(MockFixer.Id);
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider(fixableDiagnosticIds),
            expectedNuGetFixerCodeActionWasRegistered: true,
            vsixFixer: null,
            expectedVsixFixerCodeActionWasRegistered: false);

        // Only Vsix code fix provider
        // Verify only Vsix fixer's code action registered
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: null,
            expectedNuGetFixerCodeActionWasRegistered: false,
            vsixFixer: new VsixCodeFixProvider(fixableDiagnosticIds),
            expectedVsixFixerCodeActionWasRegistered: true);

        // Both NuGet and Vsix code fix provider
        // Verify only NuGet fixer's code action registered
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider(fixableDiagnosticIds),
            expectedNuGetFixerCodeActionWasRegistered: true,
            vsixFixer: new VsixCodeFixProvider(fixableDiagnosticIds),
            expectedVsixFixerCodeActionWasRegistered: false);
    }

    private static async Task TestNuGetAndVsixCodeFixersCoreAsync(
        NuGetCodeFixProvider? nugetFixer,
        bool expectedNuGetFixerCodeActionWasRegistered,
        VsixCodeFixProvider? vsixFixer,
        bool expectedVsixFixerCodeActionWasRegistered,
        MockAnalyzerReference.MockDiagnosticAnalyzer? diagnosticAnalyzer = null)
    {
        var fixes = await GetNuGetAndVsixCodeFixersCoreAsync(nugetFixer, vsixFixer, diagnosticAnalyzer);

        var fixTitles = fixes.SelectMany(fixCollection => fixCollection.Fixes).Select(f => f.Action.Title).ToHashSet();
        Assert.Equal(expectedNuGetFixerCodeActionWasRegistered, fixTitles.Contains(nameof(NuGetCodeFixProvider)));
        Assert.Equal(expectedVsixFixerCodeActionWasRegistered, fixTitles.Contains(nameof(VsixCodeFixProvider)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18818")]
    public async Task TestNuGetAndVsixCodeFixersWithMultipleFixableDiagnosticIdsAsync()
    {
        const string id1 = "ID1";
        const string id2 = "ID2";
        var reportedDiagnosticIds = ImmutableArray.Create(id1, id2);
        var diagnosticAnalyzer = new MockAnalyzerReference.MockDiagnosticAnalyzer(reportedDiagnosticIds);

        // Only NuGet code fix provider which fixes both reported diagnostic IDs.
        // Verify only NuGet fixer's code actions registered and they fix all IDs.
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider(reportedDiagnosticIds),
            expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: reportedDiagnosticIds,
            vsixFixer: null,
            expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: [],
            diagnosticAnalyzer);

        // Only Vsix code fix provider which fixes both reported diagnostic IDs.
        // Verify only Vsix fixer's code action registered and they fix all IDs.
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: null,
            expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: [],
            vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
            expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: reportedDiagnosticIds,
            diagnosticAnalyzer);

        // Both NuGet and Vsix code fix provider register same fixable IDs.
        // Verify only NuGet fixer's code actions registered.
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider(reportedDiagnosticIds),
            expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: reportedDiagnosticIds,
            vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
            expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: [],
            diagnosticAnalyzer);

        // Both NuGet and Vsix code fix provider register different fixable IDs.
        // Verify both NuGet and Vsix fixer's code actions registered.
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider([id1]),
            expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: [id1],
            vsixFixer: new VsixCodeFixProvider([id2]),
            expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: [id2],
            diagnosticAnalyzer);

        // NuGet code fix provider registers subset of Vsix code fix provider fixable IDs.
        // Verify both NuGet and Vsix fixer's code actions registered,
        // there are no duplicates and NuGet ones are preferred for duplicates.
        await TestNuGetAndVsixCodeFixersCoreAsync(
            nugetFixer: new NuGetCodeFixProvider([id1]),
            expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: [id1],
            vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
            expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: [id2],
            diagnosticAnalyzer);
    }

    private static async Task TestNuGetAndVsixCodeFixersCoreAsync(
        NuGetCodeFixProvider? nugetFixer,
        ImmutableArray<string> expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer,
        VsixCodeFixProvider? vsixFixer,
        ImmutableArray<string> expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer,
        MockAnalyzerReference.MockDiagnosticAnalyzer diagnosticAnalyzer)
    {
        var fixes = (await GetNuGetAndVsixCodeFixersCoreAsync(nugetFixer, vsixFixer, diagnosticAnalyzer))
            .SelectMany(fixCollection => fixCollection.Fixes);

        var nugetFixerRegisteredActions = fixes.Where(f => f.Action.Title == nameof(NuGetCodeFixProvider));
        var actualDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer = nugetFixerRegisteredActions.SelectMany(a => a.Diagnostics).Select(d => d.Id);
        Assert.True(actualDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer.SetEquals(expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer));

        var vsixFixerRegisteredActions = fixes.Where(f => f.Action.Title == nameof(VsixCodeFixProvider));
        var actualDiagnosticIdsWithRegisteredCodeActionsByVsixFixer = vsixFixerRegisteredActions.SelectMany(a => a.Diagnostics).Select(d => d.Id);
        Assert.True(actualDiagnosticIdsWithRegisteredCodeActionsByVsixFixer.SetEquals(expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer));
    }

    private static async Task<ImmutableArray<CodeFixCollection>> GetNuGetAndVsixCodeFixersCoreAsync(
        NuGetCodeFixProvider? nugetFixer,
        VsixCodeFixProvider? vsixFixer,
        MockAnalyzerReference.MockDiagnosticAnalyzer? diagnosticAnalyzer = null)
    {
        var code = @"class C { }";

        var vsixFixers = vsixFixer != null
            ? SpecializedCollections.SingletonEnumerable(new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => vsixFixer, new CodeChangeProviderMetadata(name: nameof(VsixCodeFixProvider), languages: LanguageNames.CSharp)))
            : [];

        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);

        var diagnosticService = workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();

        var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetRequiredService<IErrorLoggerService>()));
        var fixService = new CodeFixService(logger, vsixFixers, configurationProviders: []);

        diagnosticAnalyzer ??= new MockAnalyzerReference.MockDiagnosticAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(diagnosticAnalyzer);
        var reference = new MockAnalyzerReference(nugetFixer, analyzers);
        var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);

        var document = project.Documents.Single();

        return await fixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);
    }

    private sealed class NuGetCodeFixProvider : AbstractNuGetOrVsixCodeFixProvider
    {
        public NuGetCodeFixProvider(ImmutableArray<string> fixableDiagnsoticIds)
            : base(fixableDiagnsoticIds, nameof(NuGetCodeFixProvider))
        {
        }
    }

    private sealed class VsixCodeFixProvider : AbstractNuGetOrVsixCodeFixProvider
    {
        public VsixCodeFixProvider(ImmutableArray<string> fixableDiagnsoticIds)
            : base(fixableDiagnsoticIds, nameof(VsixCodeFixProvider))
        {
        }
    }

    private abstract class AbstractNuGetOrVsixCodeFixProvider : CodeFixProvider
    {
        private readonly string _name;

        protected AbstractNuGetOrVsixCodeFixProvider(ImmutableArray<string> fixableDiagnsoticIds, string name)
        {
            FixableDiagnosticIds = fixableDiagnsoticIds;
            _name = name;
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var fixableDiagnostics = context.Diagnostics.WhereAsArray(d => FixableDiagnosticIds.Contains(d.Id));
            context.RegisterCodeFix(CodeAction.Create(_name, ct => Task.FromResult(context.Document)), fixableDiagnostics);
            return Task.CompletedTask;
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44553")]
    [InlineData(null)]
    [InlineData("CodeFixProviderWithDuplicateEquivalenceKeyActions")]
    public async Task TestRegisteredCodeActionsWithSameEquivalenceKey(string? equivalenceKey)
    {
        var diagnosticId = "ID1";
        var analyzer = new MockAnalyzerReference.MockDiagnosticAnalyzer(ImmutableArray.Create(diagnosticId));
        var fixer = new CodeFixProviderWithDuplicateEquivalenceKeyActions(diagnosticId, equivalenceKey);

        // Verify multiple code actions registered with same equivalence key are not de-duped.
        var fixes = (await GetAddedFixesAsync(fixer, analyzer)).SelectMany(fixCollection => fixCollection.Fixes).ToList();
        Assert.Equal(2, fixes.Count);
    }

    private sealed class CodeFixProviderWithDuplicateEquivalenceKeyActions : CodeFixProvider
    {
        private readonly string _diagnosticId;
        private readonly string? _equivalenceKey;

        public CodeFixProviderWithDuplicateEquivalenceKeyActions(string diagnosticId, string? equivalenceKey)
        {
            _diagnosticId = diagnosticId;
            _equivalenceKey = equivalenceKey;
        }

        public override ImmutableArray<string> FixableDiagnosticIds => [_diagnosticId];

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Register duplicate code actions with same equivalence key, but different title.
            RegisterCodeFix(context, titleSuffix: "1");
            RegisterCodeFix(context, titleSuffix: "2");

            return Task.CompletedTask;
        }

        private void RegisterCodeFix(CodeFixContext context, string titleSuffix)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    nameof(CodeFixProviderWithDuplicateEquivalenceKeyActions) + titleSuffix,
                    ct => Task.FromResult(context.Document),
                    _equivalenceKey),
                context.Diagnostics);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62877")]
    public async Task TestAdditionalDocumentCodeFixAsync()
    {
        var analyzer = new AdditionalFileAnalyzer();
        var fixer1 = new AdditionalFileFixerWithDocumentKindsAndExtensions();
        var fixer2 = new AdditionalFileFixerWithDocumentKinds();
        var fixer3 = new AdditionalFileFixerWithDocumentExtensions();
        var fixer4 = new AdditionalFileFixerWithoutDocumentKindsAndExtensions();
        var fixers = ImmutableArray.Create<CodeFixProvider>(fixer1, fixer2, fixer3, fixer4);
        var analyzerReference = new MockAnalyzerReference(fixers, [analyzer]);

        // Verify available code fixes for .txt additional document
        var tuple = ServiceSetup(fixers, additionalDocument: new EditorTestHostDocument("Additional Document", filePath: "test.txt"));
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var txtDocument, out var extensionManager, analyzerReference, documentKind: TextDocumentKind.AdditionalDocument);
        var txtDocumentCodeFixes = await tuple.codeFixService.GetFixesAsync(txtDocument, TextSpan.FromBounds(0, 1), CancellationToken.None);
        Assert.Equal(2, txtDocumentCodeFixes.Length);
        var txtDocumentCodeFixTitles = txtDocumentCodeFixes.SelectAsArray(s => s.Fixes.Single().Action.Title);
        Assert.Contains(fixer1.Title, txtDocumentCodeFixTitles);
        Assert.Contains(fixer2.Title, txtDocumentCodeFixTitles);

        // Verify code fix application
        var codeAction = txtDocumentCodeFixes.Single(s => s.Fixes.Single().Action.Title == fixer1.Title).Fixes.Single().Action;
        var solution = await codeAction.GetChangedSolutionInternalAsync(txtDocument.Project.Solution, CodeAnalysisProgress.None, CancellationToken.None);
        var changedtxtDocument = solution!.Projects.Single().AdditionalDocuments.Single(t => t.Id == txtDocument.Id);
        Assert.Equal("Additional Document", txtDocument.GetTextSynchronously(CancellationToken.None).ToString());
        Assert.Equal($"Additional Document{fixer1.Title}", changedtxtDocument.GetTextSynchronously(CancellationToken.None).ToString());

        // Verify available code fixes for .log additional document
        tuple = ServiceSetup(fixers, additionalDocument: new EditorTestHostDocument("Additional Document", filePath: "test.log"));
        using var workspace2 = tuple.workspace;
        GetDocumentAndExtensionManager(workspace2, out var logDocument, out extensionManager, analyzerReference, documentKind: TextDocumentKind.AdditionalDocument);
        var logDocumentCodeFixes = await tuple.codeFixService.GetFixesAsync(logDocument, TextSpan.FromBounds(0, 1), CancellationToken.None);
        var logDocumentCodeFix = Assert.Single(logDocumentCodeFixes);
        var logDocumentCodeFixTitle = logDocumentCodeFix.Fixes.Single().Action.Title;
        Assert.Equal(fixer2.Title, logDocumentCodeFixTitle);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class AdditionalFileAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AFA0001";
        private readonly DiagnosticDescriptor _descriptor = new(DiagnosticId, "AdditionalFileAnalyzer", "AdditionalFileAnalyzer", "AdditionalFileAnalyzer", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterAdditionalFileAction(context =>
            {
                var text = context.AdditionalFile.GetText(context.CancellationToken);
                if (text == null || text.Lines.Count == 0)
                    return;
                var line = text.Lines[0];
                var span = new TextSpan(line.Start, line.End);
                var location = Location.Create(context.AdditionalFile.Path, span, text.Lines.GetLinePositionSpan(span));
                context.ReportDiagnostic(Diagnostic.Create(_descriptor, location));
            });
        }
    }

    internal abstract class AbstractAdditionalFileCodeFixProvider : CodeFixProvider
    {
        public string Title { get; }

        protected AbstractAdditionalFileCodeFixProvider(string title)
            => Title = title;

        public sealed override ImmutableArray<string> FixableDiagnosticIds => [AdditionalFileAnalyzer.DiagnosticId];

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create(Title,
                createChangedSolution: async ct =>
                {
                    var document = context.TextDocument;
                    var text = await document.GetTextAsync(ct).ConfigureAwait(false);
                    var newText = SourceText.From(text.ToString() + Title);
                    return document.Project.Solution.WithAdditionalDocumentText(document.Id, newText);
                },
                equivalenceKey: Title),
                context.Diagnostics[0]);

            return Task.CompletedTask;
        }
    }

#pragma warning disable RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'
    [ExportCodeFixProvider(
        LanguageNames.CSharp,
        DocumentKinds = [nameof(TextDocumentKind.AdditionalDocument)],
        DocumentExtensions = [".txt"])]
    [Shared]
    internal sealed class AdditionalFileFixerWithDocumentKindsAndExtensions : AbstractAdditionalFileCodeFixProvider
    {
        public AdditionalFileFixerWithDocumentKindsAndExtensions() : base(nameof(AdditionalFileFixerWithDocumentKindsAndExtensions)) { }
    }

    [ExportCodeFixProvider(
        LanguageNames.CSharp,
        DocumentKinds = [nameof(TextDocumentKind.AdditionalDocument)])]
    [Shared]
    internal sealed class AdditionalFileFixerWithDocumentKinds : AbstractAdditionalFileCodeFixProvider
    {
        public AdditionalFileFixerWithDocumentKinds() : base(nameof(AdditionalFileFixerWithDocumentKinds)) { }
    }

    [ExportCodeFixProvider(
        LanguageNames.CSharp,
        DocumentExtensions = [".txt"])]
    [Shared]
    internal sealed class AdditionalFileFixerWithDocumentExtensions : AbstractAdditionalFileCodeFixProvider
    {
        public AdditionalFileFixerWithDocumentExtensions() : base(nameof(AdditionalFileFixerWithDocumentExtensions)) { }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    internal sealed class AdditionalFileFixerWithoutDocumentKindsAndExtensions : AbstractAdditionalFileCodeFixProvider
    {
        public AdditionalFileFixerWithoutDocumentKindsAndExtensions() : base(nameof(AdditionalFileFixerWithoutDocumentKindsAndExtensions)) { }
    }
#pragma warning restore RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'

    [Theory, CombinatorialData]
    public async Task TestGetFixesWithDeprioritizedAnalyzerAsync(
        DeprioritizedAnalyzer.ActionKind actionKind,
        bool diagnosticOnFixLineInPriorSnapshot,
        bool editOnFixLine,
        bool addNewLineWithEdit)
    {
        // Disable these cases due to:
        // https://github.com/dotnet/roslyn/issues/77036
        if (actionKind is DeprioritizedAnalyzer.ActionKind.SemanticModel or DeprioritizedAnalyzer.ActionKind.SymbolStartEnd &&
            diagnosticOnFixLineInPriorSnapshot &&
            !addNewLineWithEdit)
        {
            return;
        }

        // This test validates analyzer de-prioritization logic in diagnostic service for lightbulb code path.
        // Basically, we have a certain set of heuristics (detailed in the next comment below), under which an analyzer
        // which is deemed to be an expensive analyzer is moved down from 'Normal' priority code fix bucket to
        // 'Low' priority bucket to improve lightbulb performance. This test validates this logic by performing
        // the following steps:
        //  1. Use 2 snapshots of document, such that the analyzer has a reported diagnostic on the code fix trigger
        //     line in the earlier snapshot based on the flag 'diagnosticOnFixLineInPriorSnapshot'
        //  2. For the second snapshot, mimic whether or not background analysis has computed and cached full document
        //     diagnostics for the document based on the flag 'testWithCachedDiagnostics'.
        //  3. Apply an edit in the second snapshot on the code fix trigger line based on the flag 'editOnFixLine'.
        //     If this flag is false, edit is apply at a different line in the document.
        //  4. Add a new line edit in the second snapshot based on the flag 'addNewLineWithEdit'. This tests the part
        //     of the heuristic where we compare intersecting diagnostics across document snapshots only if both
        //     snapshots have the same number of lines.

        var expectDeprioritization = GetExpectDeprioritization(actionKind, diagnosticOnFixLineInPriorSnapshot, addNewLineWithEdit);

        var priorSnapshotFixLine = diagnosticOnFixLineInPriorSnapshot ? "int x1 = 0;" : "System.Console.WriteLine();";
        var code = $$"""
            #pragma warning disable CS0219
            class C
            {
                void M()
                {
                    {{priorSnapshotFixLine}}
                }
            }
            """;

        var codeFix = new FixerForDeprioritizedAnalyzer();
        var analyzer = new DeprioritizedAnalyzer(actionKind);
        var analyzerReference = new MockAnalyzerReference(codeFix, [analyzer]);

        var tuple = ServiceSetup(codeFix, code: code);
        using var workspace = tuple.workspace;
        GetDocumentAndExtensionManager(workspace, out var document,
            out var extensionManager, analyzerReference);

        var sourceDocument = (Document)document;
        var root = await sourceDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var testSpan = diagnosticOnFixLineInPriorSnapshot
            ? root.DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax>().First().Span
            : root.DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().First().Span;

        // Trigger background analysis to ensure analyzer diagnostics are computed and cached. 
        // We enable full solution analysis so the 'AnalyzeDocumentAsync' doesn't skip analysis based on whether the document is active/open.
        workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

        var analyzerService = (DiagnosticAnalyzerService)workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        var diagnostics = await analyzerService.ForceRunCodeAnalysisDiagnosticsAsync(sourceDocument.Project, CancellationToken.None);
        await VerifyCachedDiagnosticsAsync(
            sourceDocument, expectedCachedDiagnostic: diagnosticOnFixLineInPriorSnapshot, testSpan, diagnostics);

        // Compute and apply code edit
        if (editOnFixLine)
        {
            code = code.Replace(priorSnapshotFixLine, "int x2 = 0;");
        }
        else
        {
            code += " // Comment at end of file";
        }

        if (addNewLineWithEdit)
        {
            // Add a new line at the start of the document to ensure the line with diagnostic in prior snapshot moved.
            code = Environment.NewLine + code;
        }

        sourceDocument = sourceDocument.WithText(SourceText.From(code));
        var appliedChanges = workspace.TryApplyChanges(sourceDocument.Project.Solution);
        Assert.True(appliedChanges);
        sourceDocument = workspace.CurrentSolution.Projects.Single().Documents.Single();

        root = await sourceDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var expectedNoFixes = !diagnosticOnFixLineInPriorSnapshot && !editOnFixLine;
        testSpan = !expectedNoFixes
            ? root.DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax>().First().Span
            : root.DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().First().Span;

        await analyzerService.GetDiagnosticsForIdsAsync(
            sourceDocument.Project, [sourceDocument.Id], diagnosticIds: null, AnalyzerFilter.All,
            includeLocalDocumentDiagnostics: true, CancellationToken.None);
        // await diagnosticIncrementalAnalyzer.GetTestAccessor().TextDocumentOpenAsync(sourceDocument);

        var normalPriFixes = await tuple.codeFixService.GetFixesAsync(sourceDocument, testSpan, CodeActionRequestPriority.Default, CancellationToken.None);
        var lowPriFixes = await tuple.codeFixService.GetFixesAsync(sourceDocument, testSpan, CodeActionRequestPriority.Low, CancellationToken.None);

        if (expectedNoFixes)
        {
            Assert.Empty(normalPriFixes);
            Assert.Empty(lowPriFixes);
            return;
        }

        var deprioritizedAnalyzers = await analyzerService.GetTestAccessor().GetDeprioritizedAnalyzersAsync(sourceDocument.Project);
        var deprioritizedIds = await analyzerService.GetTestAccessor().GetDeprioritizedDiagnosticIdsAsync(sourceDocument.Project);

        CodeFixCollection expectedFixCollection;
        if (expectDeprioritization)
        {
            Assert.Empty(normalPriFixes);
            expectedFixCollection = Assert.Single(lowPriFixes);
            var lowPriorityAnalyzer = Assert.Single(deprioritizedAnalyzers);
            Assert.Same(analyzer, lowPriorityAnalyzer);
            AssertEx.SetEqual(analyzer.SupportedDiagnostics.Select(d => d.Id), deprioritizedIds);
        }
        else
        {
            expectedFixCollection = Assert.Single(normalPriFixes);
            Assert.Empty(lowPriFixes);
            Assert.Empty(deprioritizedAnalyzers);
            Assert.Empty(deprioritizedIds);
        }

        var fix = expectedFixCollection.Fixes.Single();
        Assert.Equal(FixerForDeprioritizedAnalyzer.Title, fix.Action.Title);
        return;

        static bool GetExpectDeprioritization(
            DeprioritizedAnalyzer.ActionKind actionKind,
            bool diagnosticOnFixLineInPriorSnapshot,
            bool addNewLineWithEdit)
        {
            // We expect de-prioritization of analyzer from 'Normal' to 'Low' bucket only if following conditions are met:
            //  1. We have an expensive analyzer that registers SymbolStart/End or SemanticModel actions, both of which have a broad analysis scope.
            //  2. Either of the below is true:
            //     a. We do not have an analyzer diagnostic reported in the prior document snapshot on the edited line OR
            //     b. Number of lines in the prior document snapshot differs from number of lines in the current document snapshot,
            //        i.e. we added a new line with the edit and 'addNewLineWithEdit = true'.

            // Condition 1
            if (actionKind is not (DeprioritizedAnalyzer.ActionKind.SymbolStartEnd or DeprioritizedAnalyzer.ActionKind.SemanticModel))
                return false;

            // Condition 2(a)
            if (!diagnosticOnFixLineInPriorSnapshot)
                return true;

            // Condition 2(b)
            return addNewLineWithEdit;
        }

        static async Task VerifyCachedDiagnosticsAsync(
            Document sourceDocument,
            bool expectedCachedDiagnostic,
            TextSpan testSpan,
            ImmutableArray<DiagnosticData> cachedDiagnostics)
        {
            cachedDiagnostics = cachedDiagnostics.WhereAsArray(d => !d.IsSuppressed);

            if (!expectedCachedDiagnostic)
            {
                Assert.Empty(cachedDiagnostics);
            }
            else
            {
                var diagnostic = Assert.Single(cachedDiagnostics);
                Assert.Equal(DeprioritizedAnalyzer.Descriptor.Id, diagnostic.Id);
                var text = await sourceDocument.GetTextAsync();
                Assert.Equal(testSpan, diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text));
            }
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DeprioritizedAnalyzer : DiagnosticAnalyzer
    {
        public enum ActionKind
        {
            SymbolStartEnd,
            SemanticModel,
            Operation
        }

        public static readonly DiagnosticDescriptor Descriptor = new("ID0001", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private readonly ActionKind _actionKind;

        public DeprioritizedAnalyzer(ActionKind actionKind)
        {
            _actionKind = actionKind;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

        public override void Initialize(AnalysisContext context)
        {
            switch (_actionKind)
            {
                case ActionKind.SymbolStartEnd:
                    context.RegisterSymbolStartAction(context =>
                    {
                        var variableDeclarations = new HashSet<SyntaxNode>();
                        context.RegisterOperationAction(context
                            => variableDeclarations.Add(context.Operation.Syntax), OperationKind.VariableDeclaration);
                        context.RegisterSymbolEndAction(context =>
                        {
                            foreach (var decl in variableDeclarations)
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, decl.GetLocation()));
                        });
                    }, SymbolKind.NamedType);
                    break;

                case ActionKind.SemanticModel:
                    context.RegisterSemanticModelAction(context =>
                    {
                        var variableDeclarations = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax>();
                        foreach (var decl in variableDeclarations)
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, decl.GetLocation()));
                    });
                    break;

                case ActionKind.Operation:
                    context.RegisterOperationAction(context =>
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Operation.Syntax.GetLocation())),
                        OperationKind.VariableDeclaration);
                    break;
            }
        }
    }

    private sealed class FixerForDeprioritizedAnalyzer : CodeFixProvider
    {
        public static readonly string Title = $"Fix {DeprioritizedAnalyzer.Descriptor.Id}";
        public override ImmutableArray<string> FixableDiagnosticIds => [DeprioritizedAnalyzer.Descriptor.Id];

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(Title,
                    createChangedDocument: _ => Task.FromResult(context.Document),
                    equivalenceKey: nameof(FixerForDeprioritizedAnalyzer)),
                context.Diagnostics);
            return Task.CompletedTask;
        }
    }
}
