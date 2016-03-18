// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
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
            await analyzer.AnalyzeSyntaxAsync(document, CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeDocumentAsync(document, bodyOpt: null, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeProjectAsync(document.Project, semanticsChanged: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWhenFileOpened()
        {
            var workspace = new AdhocWorkspace();
            var document = GetDocumentFromIncompleteProject(workspace);

            // open document
            workspace.OpenDocument(document.Id);

            // create listener/service/analyzer
            var listener = new AsynchronousOperationListener();
            var service = new MyDiagnosticAnalyzerService(new Analyzer(), listener);
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            bool syntax = false;
            bool semantic = false;

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                switch (a.Diagnostics.Length)
                {
                    case 0:
                        return;
                    case 1:
                        syntax |= a.Diagnostics[0].Id == Analyzer.s_syntaxRule.Id;
                        semantic |= a.Diagnostics[0].Id == Analyzer.s_semanticRule.Id;
                        return;
                    default:
                        AssertEx.Fail("shouldn't reach here");
                        return;
                }
            };

            // now call each analyze method. none of them should run.
            await analyzer.AnalyzeSyntaxAsync(document, CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeDocumentAsync(document, bodyOpt: null, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await analyzer.AnalyzeProjectAsync(document.Project, semanticsChanged: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            // wait for all events to raised
            await listener.CreateWaitTask().ConfigureAwait(false);

            // two should have been called.
            Assert.True(syntax);
            Assert.True(semantic);
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

            return workspace.AddDocument(project.Id, "Empty.cs", SourceText.From(""));
        }

        private class MyDiagnosticAnalyzerService : DiagnosticAnalyzerService
        {
            internal MyDiagnosticAnalyzerService(DiagnosticAnalyzer analyzer, IAsynchronousOperationListener listener)
                : base(new HostAnalyzerManager(
                            ImmutableArray.Create<AnalyzerReference>(
                                new TestAnalyzerReferenceByLanguage(
                                    ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(analyzer)))),
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
    }
}
