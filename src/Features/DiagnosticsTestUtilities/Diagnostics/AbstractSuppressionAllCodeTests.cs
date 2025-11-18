// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;

[UseExportProvider]
public abstract class AbstractSuppressionAllCodeTests : IEqualityComparer<Diagnostic>
{
    protected abstract TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions);

    internal abstract Tuple<Analyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

    protected Task TestPragmaAsync(string code, ParseOptions options, Func<string, bool> verifier)
    {
        var set = new HashSet<ValueTuple<SyntaxToken, SyntaxToken>>();
        return TestPragmaOrAttributeAsync(code, options, pragma: true, digInto: n => true, verifier: verifier, fixChecker: c =>
        {
            var fix = (AbstractSuppressionCodeFixProvider.PragmaWarningCodeAction)c;
            var tuple = ValueTuple.Create(fix.StartToken_TestOnly, fix.EndToken_TestOnly);
            if (set.Contains(tuple))
            {
                return true;
            }

            set.Add(tuple);
            return false;
        });
    }

    protected Task TestSuppressionWithAttributeAsync(string code, ParseOptions options, Func<SyntaxNode, bool> digInto, Func<string, bool> verifier)
    {
        var set = new HashSet<ISymbol>();
        return TestPragmaOrAttributeAsync(code, options, pragma: false, digInto: digInto, verifier: verifier, fixChecker: c =>
        {
            var fix = (AbstractSuppressionCodeFixProvider.GlobalSuppressMessageCodeAction)c;
            if (set.Contains(fix.TargetSymbol_TestOnly))
            {
                return true;
            }

            set.Add(fix.TargetSymbol_TestOnly);
            return false;
        });
    }

    protected async Task TestPragmaOrAttributeAsync(
        string code, ParseOptions options, bool pragma, Func<SyntaxNode, bool> digInto, Func<string, bool> verifier, Func<CodeAction, bool> fixChecker)
    {
        using var workspace = CreateWorkspaceFromFile(code, options);
        var (analyzer, fixer) = CreateDiagnosticProviderAndFixer(workspace);

        var analyzerReference = new AnalyzerImageReference([analyzer]);
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));

        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var root = document.GetSyntaxRootAsync().GetAwaiter().GetResult();
        var existingDiagnostics = root.GetDiagnostics().ToArray();

        var descendants = root.DescendantNodesAndSelf(digInto).ToImmutableArray();
        analyzer.AllNodes = descendants;
        var diagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, root.FullSpan);

        foreach (var diagnostic in diagnostics)
        {
            if (!fixer.IsFixableDiagnostic(diagnostic))
            {
                continue;
            }

            var fixes = fixer.GetFixesAsync(document, diagnostic.Location.SourceSpan, [diagnostic], CancellationToken.None).GetAwaiter().GetResult();
            if (fixes == null || fixes.Length <= 0)
            {
                continue;
            }

            var fix = GetFix(fixes.Select(f => f.Action), pragma);
            if (fix == null)
            {
                continue;
            }

            // already same fix has been tested
            if (fixChecker(fix))
            {
                continue;
            }

            var operations = fix.GetOperationsAsync(
                document.Project.Solution, CodeAnalysisProgress.None, CancellationToken.None).GetAwaiter().GetResult();

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().Single();
            var newDocument = applyChangesOperation.ChangedSolution.Projects.Single().Documents.Single();
            var newTree = newDocument.GetSyntaxTreeAsync().GetAwaiter().GetResult();

            var newText = newTree.GetText().ToString();
            Assert.True(verifier(newText));

            var newDiagnostics = newTree.GetDiagnostics();
            Assert.Equal(0, existingDiagnostics.Except(newDiagnostics, this).Count());
        }
    }

    private static CodeAction GetFix(IEnumerable<CodeAction> fixes, bool pragma)
    {
        if (pragma)
        {
            return fixes.FirstOrDefault(f => f is AbstractSuppressionCodeFixProvider.PragmaWarningCodeAction);
        }

        return fixes.OfType<AbstractSuppressionCodeFixProvider.GlobalSuppressMessageCodeAction>().FirstOrDefault();
    }

    public bool Equals(Diagnostic x, Diagnostic y)
        => x.Id == y.Id && x.Descriptor.Category == y.Descriptor.Category;

    public int GetHashCode(Diagnostic obj)
        => Hash.Combine(obj.Id, obj.Descriptor.Category.GetHashCode());

    internal sealed class Analyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly DiagnosticDescriptor _descriptor =
            new("TestId", "Test", "Test", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public bool IsHighPriority => false;

        public ImmutableArray<SyntaxNode> AllNodes { get; set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return [_descriptor];
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSyntaxTreeAction(
                context =>
                {
                    foreach (var node in AllNodes)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, node.GetLocation()));
                    }
                });
        }
    }
}
