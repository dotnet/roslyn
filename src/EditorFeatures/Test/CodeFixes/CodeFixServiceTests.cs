// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes
{
    [UseExportProvider]
    public class CodeFixServiceTests
    {
        private static readonly TestComposition s_compositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures
            .AddExcludedPartTypes(typeof(IDiagnosticUpdateSourceRegistrationService))
            .AddParts(typeof(MockDiagnosticUpdateSourceRegistrationService));

        [Fact]
        public async Task TestGetFirstDiagnosticWithFixAsync()
        {
            var fixers = CreateFixers();
            var code = @"
    a
";
            using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var diagnosticService = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetRequiredService<IErrorLoggerService>()));
            var fixService = new CodeFixService(
                diagnosticService, logger, fixers, SpecializedCollections.EmptyEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>());

            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

            // register diagnostic engine to solution crawler
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

            var reference = new MockAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            var document = project.Documents.Single();
            var unused = await fixService.GetMostSevereFixableDiagnosticAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.Default, CancellationToken.None);

            var fixer1 = (MockFixer)fixers.Single().Value;
            var fixer2 = (MockFixer)reference.Fixer!;

            // check to make sure both of them are called.
            Assert.True(fixer1.Called);
            Assert.True(fixer2.Called);
        }

        [Fact, WorkItem(41116, "https://github.com/dotnet/roslyn/issues/41116")]
        public async Task TestGetFixesAsyncWithDuplicateDiagnostics()
        {
            var codeFix = new MockFixer();

            // Add duplicate analyzers to get duplicate diagnostics.
            var analyzerReference = new MockAnalyzerReference(
                    codeFix,
                    ImmutableArray.Create<DiagnosticAnalyzer>(
                        new MockAnalyzerReference.MockDiagnosticAnalyzer(),
                        new MockAnalyzerReference.MockDiagnosticAnalyzer()));

            var tuple = ServiceSetup(codeFix);
            using var workspace = tuple.workspace;
            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager, analyzerReference);

            // Verify that we do not crash when computing fixes.
            var options = CodeActionOptions.Default;

            _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), options, CancellationToken.None);

            // Verify that code fix is invoked with both the diagnostics in the context,
            // i.e. duplicate diagnostics are not silently discarded by the CodeFixService.
            Assert.Equal(2, codeFix.ContextDiagnosticsCount);
        }

        [Fact, WorkItem(45779, "https://github.com/dotnet/roslyn/issues/45779")]
        public async Task TestGetFixesAsyncHasNoDuplicateConfigurationActions()
        {
            var codeFix = new MockFixer();

            // Add analyzers with duplicate ID and/or category to get duplicate diagnostics.
            var analyzerReference = new MockAnalyzerReference(
                    codeFix,
                    ImmutableArray.Create<DiagnosticAnalyzer>(
                        new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category1"),
                        new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category1"),
                        new MockAnalyzerReference.MockDiagnosticAnalyzer("ID1", "Category2"),
                        new MockAnalyzerReference.MockDiagnosticAnalyzer("ID2", "Category2")));

            var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: true);
            using var workspace = tuple.workspace;
            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager, analyzerReference);

            // Verify registered configuration code actions do not have duplicates.
            var options = CodeActionOptions.Default;
            var fixCollections = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), options, CancellationToken.None);
            var codeActions = fixCollections.SelectMany(c => c.Fixes.Select(f => f.Action)).ToImmutableArray();
            Assert.Equal(7, codeActions.Length);
            var uniqueTitles = new HashSet<string>();
            foreach (var codeAction in codeActions)
            {
                Assert.True(codeAction is AbstractConfigurationActionWithNestedActions);
                Assert.True(uniqueTitles.Add(codeAction.Title));
            }
        }

        [Fact, WorkItem(56843, "https://github.com/dotnet/roslyn/issues/56843")]
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
            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager, analyzerReference);

            var options = CodeActionOptions.Default;

            // Verify only analyzerWithFix is executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Normal'.
            _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
                priority: CodeActionRequestPriority.Normal, options,
                addOperationScope: _ => null, cancellationToken: CancellationToken.None);
            Assert.True(analyzerWithFix.ReceivedCallback);
            Assert.False(analyzerWithoutFix.ReceivedCallback);

            // Verify both analyzerWithFix and analyzerWithoutFix are executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Lowest'.
            _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
                priority: CodeActionRequestPriority.Lowest, options,
                addOperationScope: _ => null, cancellationToken: CancellationToken.None);
            Assert.True(analyzerWithFix.ReceivedCallback);
            Assert.True(analyzerWithoutFix.ReceivedCallback);
        }

        [Fact, WorkItem(1450689, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1450689")]
        public async Task TestGetFixesAsyncForDocumentDiagnosticAnalyzerAsync()
        {
            // TS has special DocumentDiagnosticAnalyzer that report 0 SupportedDiagnostics.
            // We need to ensure that we don't skip these document analyzers
            // when computing the diagnostics/code fixes for "Normal" priority bucket, which
            // normally only execute those analyzers which report at least one fixable supported diagnostic.
            var documentDiagnosticAnalyzer = new MockAnalyzerReference.MockDocumentDiagnosticAnalyzer(reportedDiagnosticIds: ImmutableArray<string>.Empty);
            Assert.Empty(documentDiagnosticAnalyzer.SupportedDiagnostics);

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(documentDiagnosticAnalyzer);
            var codeFix = new MockFixer();
            var analyzerReference = new MockAnalyzerReference(codeFix, analyzers);

            // Verify no callbacks received at initialization.
            Assert.False(documentDiagnosticAnalyzer.ReceivedCallback);

            var tuple = ServiceSetup(codeFix, includeConfigurationFixProviders: false);
            using var workspace = tuple.workspace;
            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager, analyzerReference);

            // Verify both analyzers are executed when GetFixesAsync is invoked with 'CodeActionRequestPriority.Normal'.
            var options = CodeActionOptions.Default;

            _ = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0),
                priority: CodeActionRequestPriority.Normal, options,
                addOperationScope: _ => null, cancellationToken: CancellationToken.None);
            Assert.True(documentDiagnosticAnalyzer.ReceivedCallback);
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethod_Diagnostic()
        {
            await GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethod());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethod_Fixes()
        {
            await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethod());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethodAsync_Diagnostic()
        {
            await GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethodAsync());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethodAsync_Fixes()
        {
            await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInRegisterMethodAsync());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Diagnostic()
        {
            await GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Fixes()
        {
            await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21533")]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Diagnostic2()
        {
            await GetFirstDiagnosticWithFixWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21533")]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds_Fixes2()
        {
            await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInGetFixAllProvider()
            => await GetAddedFixesWithExceptionValidationAsync(new ErrorCases.ExceptionInGetFixAllProvider());

        [Fact, WorkItem(45851, "https://github.com/dotnet/roslyn/issues/45851")]
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

            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager);
            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)tuple.analyzerService;
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);
            var reference = new MockAnalyzerReference(codefix, ImmutableArray.Create(diagnosticAnalyzer));
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            document = project.Documents.Single();
            var options = CodeActionOptions.Default;
            var fixes = await tuple.codeFixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), options, CancellationToken.None);

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

            GetDocumentAndExtensionManager(tuple.analyzerService, workspace, out var document, out var extensionManager);
            var unused = await tuple.codeFixService.GetMostSevereFixableDiagnosticAsync(document, TextSpan.FromBounds(0, 0), CodeActionOptions.Default, CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(codefix));
            Assert.False(extensionManager.IsIgnored(codefix));
            Assert.True(errorReported);
        }

        private static (TestWorkspace workspace, DiagnosticAnalyzerService analyzerService, CodeFixService codeFixService, IErrorLoggerService errorLogger) ServiceSetup(
            CodeFixProvider codefix,
            bool includeConfigurationFixProviders = false,
            bool throwExceptionInFixerCreation = false)
        {
            var fixers = SpecializedCollections.SingletonEnumerable(
                new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(
                () => throwExceptionInFixerCreation ? throw new Exception() : codefix,
                new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));

            var code = @"class Program { }";

            var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);
            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var diagnosticService = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());
            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => new TestErrorLogger()));
            var errorLogger = logger.First().Value;

            var configurationFixProviders = includeConfigurationFixProviders
                ? workspace.ExportProvider.GetExports<IConfigurationFixProvider, CodeChangeProviderMetadata>()
                : SpecializedCollections.EmptyEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>();

            var fixService = new CodeFixService(
                diagnosticService,
                logger,
                fixers,
                configurationFixProviders);

            return (workspace, diagnosticService, fixService, errorLogger);
        }

        private static void GetDocumentAndExtensionManager(
            DiagnosticAnalyzerService diagnosticService,
            TestWorkspace workspace,
            out Document document,
            out EditorLayerExtensionManager.ExtensionManager extensionManager,
            MockAnalyzerReference? analyzerReference = null)
        {
            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

            // register diagnostic engine to solution crawler
            _ = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

            var reference = analyzerReference ?? new MockAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            document = project.Documents.Single();
            extensionManager = (EditorLayerExtensionManager.ExtensionManager)document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
        }

        private static IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> CreateFixers()
        {
            return SpecializedCollections.SingletonEnumerable(
                new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => new MockFixer(), new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));
        }

        internal class MockFixer : CodeFixProvider
        {
            public const string Id = "MyDiagnostic";
            public bool Called;
            public int ContextDiagnosticsCount;

            public sealed override ImmutableArray<string> FixableDiagnosticIds
            {
                get { return ImmutableArray.Create(Id); }
            }

            public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                Called = true;
                ContextDiagnosticsCount = context.Diagnostics.Length;
                return Task.CompletedTask;
            }
        }

        private class MockAnalyzerReference : AnalyzerReference, ICodeFixProviderFactory
        {
            public readonly CodeFixProvider? Fixer;
            public readonly ImmutableArray<DiagnosticAnalyzer> Analyzers;

            private static readonly CodeFixProvider s_defaultFixer = new MockFixer();
            private static readonly ImmutableArray<DiagnosticAnalyzer> s_defaultAnalyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new MockDiagnosticAnalyzer());

            public MockAnalyzerReference(CodeFixProvider? fixer, ImmutableArray<DiagnosticAnalyzer> analyzers)
            {
                Fixer = fixer;
                Analyzers = analyzers;
            }

            public MockAnalyzerReference()
                : this(s_defaultFixer, s_defaultAnalyzers)
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

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
                => Analyzers;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public ImmutableArray<CodeFixProvider> GetFixers()
                => Fixer != null ? ImmutableArray.Create(Fixer) : ImmutableArray<CodeFixProvider>.Empty;

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

            public class MockDiagnosticAnalyzer : DiagnosticAnalyzer
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

            public class MockDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
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

                public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
                {
                    ReceivedCallback = true;
                    return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
                }

                public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                {
                    ReceivedCallback = true;
                    return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
                }
            }
        }

        internal class TestErrorLogger : IErrorLoggerService
        {
            public Dictionary<string, string> Messages = new Dictionary<string, string>();

            public void LogException(object source, Exception exception)
                => Messages.Add(source.GetType().Name, ToLogFormat(exception));

            private static string ToLogFormat(Exception exception)
                => exception.Message + Environment.NewLine + exception.StackTrace;
        }

        [Fact, WorkItem(18818, "https://github.com/dotnet/roslyn/issues/18818")]
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

        [Fact, WorkItem(18818, "https://github.com/dotnet/roslyn/issues/18818")]
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
                expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: ImmutableArray<string>.Empty,
                diagnosticAnalyzer);

            // Only Vsix code fix provider which fixes both reported diagnostic IDs.
            // Verify only Vsix fixer's code action registered and they fix all IDs.
            await TestNuGetAndVsixCodeFixersCoreAsync(
                nugetFixer: null,
                expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: ImmutableArray<string>.Empty,
                vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
                expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: reportedDiagnosticIds,
                diagnosticAnalyzer);

            // Both NuGet and Vsix code fix provider register same fixable IDs.
            // Verify only NuGet fixer's code actions registered.
            await TestNuGetAndVsixCodeFixersCoreAsync(
                nugetFixer: new NuGetCodeFixProvider(reportedDiagnosticIds),
                expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: reportedDiagnosticIds,
                vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
                expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: ImmutableArray<string>.Empty,
                diagnosticAnalyzer);

            // Both NuGet and Vsix code fix provider register different fixable IDs.
            // Verify both NuGet and Vsix fixer's code actions registered.
            await TestNuGetAndVsixCodeFixersCoreAsync(
                nugetFixer: new NuGetCodeFixProvider(ImmutableArray.Create(id1)),
                expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: ImmutableArray.Create(id1),
                vsixFixer: new VsixCodeFixProvider(ImmutableArray.Create(id2)),
                expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: ImmutableArray.Create(id2),
                diagnosticAnalyzer);

            // NuGet code fix provider registers subset of Vsix code fix provider fixable IDs.
            // Verify both NuGet and Vsix fixer's code actions registered,
            // there are no duplicates and NuGet ones are preferred for duplicates.
            await TestNuGetAndVsixCodeFixersCoreAsync(
                nugetFixer: new NuGetCodeFixProvider(ImmutableArray.Create(id1)),
                expectedDiagnosticIdsWithRegisteredCodeActionsByNuGetFixer: ImmutableArray.Create(id1),
                vsixFixer: new VsixCodeFixProvider(reportedDiagnosticIds),
                expectedDiagnosticIdsWithRegisteredCodeActionsByVsixFixer: ImmutableArray.Create(id2),
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
                : SpecializedCollections.EmptyEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>();

            using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments: true);

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var diagnosticService = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());

            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetRequiredService<IErrorLoggerService>()));
            var fixService = new CodeFixService(
                diagnosticService, logger, vsixFixers, SpecializedCollections.EmptyEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>());

            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

            // register diagnostic engine to solution crawler
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

            diagnosticAnalyzer ??= new MockAnalyzerReference.MockDiagnosticAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(diagnosticAnalyzer);
            var reference = new MockAnalyzerReference(nugetFixer, analyzers);
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);

            var document = project.Documents.Single();
            var options = CodeActionOptions.Default;

            return await fixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), options, CancellationToken.None);
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

        [Theory, WorkItem(44553, "https://github.com/dotnet/roslyn/issues/44553")]
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

            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(_diagnosticId);

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
    }
}
