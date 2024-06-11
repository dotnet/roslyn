// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    public class NonLocalDiagnosticTests : AbstractPullDiagnosticTestsBase
    {
        public NonLocalDiagnosticTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5634")]
        internal async Task TestNonLocalDocumentDiagnosticsAreReportedWhenFSAEnabled(bool mutatingLspWorkspace, bool fsaEnabled)
        {
            var markup1 = @"class A { }";
            var markup2 = @"class B { }";
            var scope = fsaEnabled ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.OpenFiles;
            await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                 new[] { markup1, markup2 }, mutatingLspWorkspace, scope, useVSDiagnostics: false);

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.First();

            // Non-local document diagnostics are reported only for open documents by DocumentPullDiagnosticsHandler.
            // For closed documents, non-local document diagnostics are reported by the WorkspacePullDiagnosticsHandler
            // and not reported here.
            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics: false, testNonLocalDiagnostics: true);
            if (fsaEnabled)
            {
                Assert.Equal(1, results.Length);
                Assert.Equal(2, results[0].Diagnostics?.Length);
                var orderedDiagnostics = results[0].Diagnostics.OrderBy(d => d.Code!.Value.Value).ToList();
                Assert.Equal(NonLocalDiagnosticsAnalyzer.NonLocalDescriptor.Id, orderedDiagnostics[0].Code);
                Assert.Equal(NonLocalDiagnosticsAnalyzer.CompilationEndDescriptor.Id, orderedDiagnostics[1].Code);
                Assert.Equal(document.GetURI(), results[0].Uri);

                // Asking again should give us back unchanged diagnostics.
                var results2 = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics: false, previousResultId: results.Single().ResultId, testNonLocalDiagnostics: true);
                Assert.Null(results2[0].Diagnostics);
                Assert.Equal(results[0].ResultId, results2[0].ResultId);
            }
            else
            {
                Assert.Empty(results);

                // Asking again should give us back unchanged diagnostics.
                var results2 = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics: false, testNonLocalDiagnostics: true);
                Assert.Empty(results2);
            }
        }

        protected override TestComposition Composition => base.Composition.AddParts(typeof(NonLocalDiagnosticsAnalyzer));

        private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
            => new(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(
                DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp),
                new NonLocalDiagnosticsAnalyzer())));

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private sealed class NonLocalDiagnosticsAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor NonLocalDescriptor = new("NonLocal0001", "Title1", "NonLocal0001", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            public static readonly DiagnosticDescriptor CompilationEndDescriptor = new("NonLocal0002", "Title2", "NonLocal0002", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true, customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NonLocalDescriptor, CompilationEndDescriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(context =>
                {
                    var compilation = context.Compilation;
                    context.RegisterSyntaxTreeAction(context =>
                    {
                        foreach (var tree in compilation.SyntaxTrees)
                        {
                            if (tree != context.Tree)
                            {
                                var root = tree.GetRoot();
                                var diagnostic = Diagnostic.Create(NonLocalDescriptor, root.GetFirstToken().GetLocation());
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    });

                    context.RegisterCompilationEndAction(context =>
                    {
                        foreach (var tree in compilation.SyntaxTrees)
                        {
                            var root = tree.GetRoot();
                            var diagnostic = Diagnostic.Create(CompilationEndDescriptor, root.GetFirstToken().GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    });
                });
            }
        }
    }
}
