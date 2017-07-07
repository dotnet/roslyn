// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class DiagnosticAnalyzerServiceTests
    {
        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalse()
        {
            var workspace = new AdhocWorkspace();
            var document = GetDocumentFromIncompleteProject(workspace);

            // create listener/service/analyzer
            var listener = new AsynchronousOperationListener();
            var service = new MyDiagnosticAnalyzerService(new Analyzer(), listener);
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            // listen to events
            // check empty since this could be called to clear up existing diagnostics
            service.DiagnosticsUpdated += (s, a) => Assert.Empty(a.Diagnostics);

            // now call each analyze method. none of them should run.
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseFSAOn()
        {
            var workspace = new AdhocWorkspace();
            workspace.Options = workspace.Options.WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true);
            var document = GetDocumentFromIncompleteProject(workspace);

            // open document
            workspace.OpenDocument(document.Id);

            await TestAnalyzerAsync(workspace, document, new Analyzer(), AnalyzerResultSetter, expectedSyntax: true, expectedSemantic: true);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWhenFileOpened()
        {
            var workspace = new AdhocWorkspace();
            var document = GetDocumentFromIncompleteProject(workspace);

            // open document
            workspace.OpenDocument(document.Id);

            await TestAnalyzerAsync(workspace, document, new Analyzer(), AnalyzerResultSetter, expectedSyntax: true, expectedSemantic: true);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWhenFileOpenedWithCompilerAnalyzer()
        {
            var workspace = new AdhocWorkspace();
            var document = GetDocumentFromIncompleteProject(workspace);

            // open document
            workspace.OpenDocument(document.Id);

            await TestAnalyzerAsync(workspace, document, new CSharpCompilerDiagnosticAnalyzer(), CompilerAnalyzerResultSetter, expectedSyntax: true, expectedSemantic: false);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWithCompilerAnalyzerFSAOn()
        {
            var workspace = new AdhocWorkspace();
            workspace.Options = workspace.Options.WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true);
            var document = GetDocumentFromIncompleteProject(workspace);

            await TestAnalyzerAsync(workspace, document, new CSharpCompilerDiagnosticAnalyzer(), CompilerAnalyzerResultSetter, expectedSyntax: true, expectedSemantic: false);
        }

        private static async Task TestAnalyzerAsync(
            AdhocWorkspace workspace,
            Document document,
            DiagnosticAnalyzer diagnosticAnalyzer,
            Func<bool, bool, ImmutableArray<DiagnosticData>, (bool, bool)> resultSetter,
            bool expectedSyntax, bool expectedSemantic)
        {
            // create listener/service/analyzer
            var listener = new AsynchronousOperationListener();
            var service = new MyDiagnosticAnalyzerService(diagnosticAnalyzer, listener);
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            bool syntax = false;
            bool semantic = false;

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                (syntax, semantic) = resultSetter(syntax, semantic, a.Diagnostics);
            };

            // now call each analyze method. none of them should run.
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);

            // two should have been called.
            Assert.Equal(expectedSyntax, syntax);
            Assert.Equal(expectedSemantic, semantic);
        }

        [Fact]
        public async Task TestOpenFileOnlyAnalyzerDiagnostics()
        {
            var workspace = new AdhocWorkspace();

            var project = workspace.AddProject(
                           ProjectInfo.Create(
                               ProjectId.CreateNewId(),
                               VersionStamp.Create(),
                               "CSharpProject",
                               "CSharpProject",
                               LanguageNames.CSharp));

            var document = workspace.AddDocument(project.Id, "Empty.cs", SourceText.From(""));

            // create listener/service/analyzer
            var listener = new AsynchronousOperationListener();
            var service = new MyDiagnosticAnalyzerService(new OpenFileOnlyAnalyzer(), listener);
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                if (workspace.IsDocumentOpen(a.DocumentId))
                {
                    // check the diagnostics are reported
                    Assert.Equal(document.Id, a.DocumentId);
                    Assert.Equal(1, a.Diagnostics.Length);
                    Assert.Equal(OpenFileOnlyAnalyzer.s_syntaxRule.Id, a.Diagnostics[0].Id);
                }

                if (a.DocumentId == document.Id && !workspace.IsDocumentOpen(a.DocumentId))
                {
                    // check the diagnostics reported are cleared
                    Assert.Equal(0, a.Diagnostics.Length);
                }
            };

            // open document
            workspace.OpenDocument(document.Id);
            await analyzer.DocumentOpenAsync(document, CancellationToken.None).ConfigureAwait(false);

            // cause analysis
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // close document
            workspace.CloseDocument(document.Id);
            await analyzer.DocumentCloseAsync(document, CancellationToken.None).ConfigureAwait(false);

            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSynchronizeWithBuild()
        {
            var workspace = new AdhocWorkspace(MefV1HostServices.Create(TestExportProvider.ExportProviderWithCSharpAndVisualBasic.AsExportProvider()));

            var language = Workspaces.NoCompilationConstants.LanguageName;

            var project = workspace.AddProject(
                           ProjectInfo.Create(
                               ProjectId.CreateNewId(),
                               VersionStamp.Create(),
                               "NoNameProject",
                               "NoNameProject",
                               language));

            var filePath = "NoNameDoc.other";
            var document = workspace.AddDocument(
                DocumentInfo.Create(
                    DocumentId.CreateNewId(project.Id),
                    "Empty",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create(), filePath)),
                    filePath: filePath));

            // create listener/service/analyzer
            var listener = new AsynchronousOperationListener();
            var service = new MyDiagnosticAnalyzerService(new NoNameAnalyzer(), listener, language);
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            bool syntax = false;

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                switch (a.Diagnostics.Length)
                {
                    case 0:
                        return;
                    case 1:
                        syntax |= a.Diagnostics[0].Id == NoNameAnalyzer.s_syntaxRule.Id;
                        return;
                    default:
                        AssertEx.Fail("shouldn't reach here");
                        return;
                }
            };

            // cause analysis
            await service.SynchronizeWithBuildAsync(
                workspace,
                ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>>.Empty.Add(
                    document.Project.Id,
                    ImmutableArray.Create(
                        Diagnostic.Create(
                            NoNameAnalyzer.s_syntaxRule,
                            Location.Create(document.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)))).ToDiagnosticData(project))));

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);

            // two should have been called.
            Assert.True(syntax);

            // we should reach here without crashing
        }

        private static Document GetDocumentFromIncompleteProject(AdhocWorkspace workspace)
        {
            var project = workspace.AddProject(
                            ProjectInfo.Create(
                                ProjectId.CreateNewId(),
                                VersionStamp.Create(),
                                "CSharpProject",
                                "CSharpProject",
                                LanguageNames.CSharp).WithHasAllInformation(hasAllInformation: false));

            return workspace.AddDocument(project.Id, "Empty.cs", SourceText.From("class A { B B {get} }"));
        }

        private static (bool, bool) AnalyzerResultSetter(bool syntax, bool semantic, ImmutableArray<DiagnosticData> diagnostics)
        {
            switch (diagnostics.Length)
            {
                case 0:
                    break;
                case 1:
                    syntax |= diagnostics[0].Id == Analyzer.s_syntaxRule.Id;
                    semantic |= diagnostics[0].Id == Analyzer.s_semanticRule.Id;
                    break;
                default:
                    AssertEx.Fail("shouldn't reach here");
                    break;
            }

            return (syntax, semantic);
        }

        private static (bool, bool) CompilerAnalyzerResultSetter(bool syntax, bool semantic, ImmutableArray<DiagnosticData> diagnostics)
        {
            syntax |= diagnostics.Any(d => d.Properties["Origin"] == "Syntactic");
            semantic |= diagnostics.Any(d => d.Properties["Origin"] != "Syntactic");

            return (syntax, semantic);
        }

        private static async Task RunAllAnalysisAsync(IIncrementalAnalyzer analyzer, Document document)
        {
            await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeDocumentAsync(document, bodyOpt: null, reasons: InvocationReasons.Empty, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeProjectAsync(document.Project, semanticsChanged: true, reasons: InvocationReasons.Empty, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        private class MyDiagnosticAnalyzerService : DiagnosticAnalyzerService
        {
            internal MyDiagnosticAnalyzerService(DiagnosticAnalyzer analyzer, IAsynchronousOperationListener listener, string language = LanguageNames.CSharp)
                : base(new HostAnalyzerManager(
                            ImmutableArray.Create<AnalyzerReference>(
                                new TestAnalyzerReferenceByLanguage(
                                    ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(language, ImmutableArray.Create(analyzer)))),
                            hostDiagnosticUpdateSource: null),
                      hostDiagnosticUpdateSource: null,
                      registrationService: new MockDiagnosticUpdateSourceRegistrationService(),
                      listener: listener)
            {
            }
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);
            internal static readonly DiagnosticDescriptor s_semanticRule = new DiagnosticDescriptor("semantic", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);
            internal static readonly DiagnosticDescriptor s_compilationRule = new DiagnosticDescriptor("compilation", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule, s_semanticRule, s_compilationRule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));
                context.RegisterSemanticModelAction(c => c.ReportDiagnostic(Diagnostic.Create(s_semanticRule, c.SemanticModel.SyntaxTree.GetRoot().GetLocation())));
                context.RegisterCompilationAction(c => c.ReportDiagnostic(Diagnostic.Create(s_compilationRule, c.Compilation.SyntaxTrees.First().GetRoot().GetLocation())));
            }
        }

        private class OpenFileOnlyAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));
            }

            public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            {
                return DiagnosticAnalyzerCategory.SyntaxAnalysis;
            }

            public bool OpenFileOnly(Workspace workspace)
            {
                return true;
            }
        }

        private class NoNameAnalyzer : DocumentDiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

            public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return ImmutableArray.Create(Diagnostic.Create(s_syntaxRule, Location.Create(document.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)))));
            }

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.Default<ImmutableArray<Diagnostic>>();
            }
        }
    }
}
