// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax> : SyntaxEditorBasedCodeRefactoringProvider
    {
        private const string SwitchStatementEquivalenceKey = "SwitchStatement";
        private const string SwitchExpressionEquivalenceKey = "SwitchExpression";

        public abstract string GetTitle(bool forSwitchExpression);
        public abstract Analyzer CreateAnalyzer(ISyntaxFacts syntaxFacts, ParseOptions options);

        protected sealed override ImmutableArray<FixAllScope> SupportedFixAllScopes => AllFixAllScopes;

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>().ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (!ShouldOfferRefactoring(ifStatement, semanticModel, syntaxFactsService, out var analyzer, out var sections, out var target))
            {
                return;
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    GetTitle(forSwitchExpression: false),
                    c => UpdateDocumentAsync(document, target, ifStatement, sections, analyzer.Features, convertToSwitchExpression: false, c),
                    SwitchStatementEquivalenceKey),
                ifStatement.Span);

            if (analyzer.Supports(Feature.SwitchExpression) &&
                CanConvertToSwitchExpression(analyzer.Supports(Feature.OrPattern), sections))
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        GetTitle(forSwitchExpression: true),
                        c => UpdateDocumentAsync(document, target, ifStatement, sections, analyzer.Features, convertToSwitchExpression: true, c),
                        SwitchExpressionEquivalenceKey),
                    ifStatement.Span);
            }
        }

        private bool ShouldOfferRefactoring(
            TIfStatementSyntax? ifStatement,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            [NotNullWhen(true)] out Analyzer? analyzer,
            [NotNullWhen(true)] out ImmutableArray<AnalyzedSwitchSection> sections,
            [NotNullWhen(true)] out SyntaxNode? target)
        {
            analyzer = null;
            sections = default;
            target = null;

            if (ifStatement == null || ifStatement.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return false;
            }

            var ifOperation = semanticModel.GetOperation(ifStatement);
            if (ifOperation is not IConditionalOperation { Parent: IBlockOperation parentBlock })
            {
                return false;
            }

            var operations = parentBlock.Operations;
            var index = operations.IndexOf(ifOperation);
            analyzer = CreateAnalyzer(syntaxFactsService, ifStatement.SyntaxTree.Options);
            (sections, target) = analyzer.AnalyzeIfStatementSequence(operations.AsSpan()[index..]);
            if (sections.IsDefaultOrEmpty)
            {
                return false;
            }

            // To prevent noisiness we don't offer this unless we're going to generate at least
            // two switch labels.  It can be quite annoying to basically have this offered
            // on pretty much any simple 'if' like "if (a == 0)" or "if (x == null)".  In these
            // cases, the converted code just looks and feels worse, and it ends up causing the
            // lightbulb to appear too much.
            //
            // This does mean that if someone has a simple if, and is about to add a lot more
            // cases, and says to themselves "let me convert this to a switch first!", then they'll
            // be out of luck.  However, I believe the core value here is in taking existing large
            // if-chains/checks and easily converting them over to a switch.  So not offering the
            // feature on simple if-statements seems like an acceptable compromise to take to ensure
            // the overall user experience isn't degraded.
            var labelCount = sections.Sum(section => section.Labels.IsDefault ? 1 : section.Labels.Length);
            if (labelCount < 2)
            {
                return false;
            }

            return true;
        }

        private static bool CanConvertToSwitchExpression(
            bool supportsOrPattern, ImmutableArray<AnalyzedSwitchSection> sections)
        {
            // There must be a default case for an exhaustive switch expression
            if (!sections.Any(static section => section.Labels.IsDefault))
                return false;

            // There must be at least one return statement
            if (!sections.Any(static section => GetSwitchArmKind(section.Body) == OperationKind.Return))
                return false;

            if (!sections.All(section => CanConvertSectionForSwitchExpression(supportsOrPattern, section)))
                return false;

            return true;

            static OperationKind GetSwitchArmKind(IOperation op)
            {
                switch (op)
                {
                    case IReturnOperation { ReturnedValue: { } }:
                    case IThrowOperation { Exception: { } }:
                        return op.Kind;

                    case IBlockOperation { Operations: { Length: 1 } statements }:
                        return GetSwitchArmKind(statements[0]);
                }

                return default;
            }

            static bool CanConvertSectionForSwitchExpression(bool supportsOrPattern, AnalyzedSwitchSection section)
            {
                // All arms must be convertible to a switch arm
                if (GetSwitchArmKind(section.Body) == default)
                    return false;

                // Default label can trivially be converted to a switch arm.
                if (section.Labels.IsDefault)
                    return true;

                // Single label case can trivially be converted to a switch arm.
                if (section.Labels.Length == 1)
                    return true;

                if (section.Labels.Length == 0)
                {
                    Debug.Fail("How did we not get any labels?");
                    return false;
                }

                // If there are two or more labels, we can support this as long as the language supports 'or' patterns
                // and as long as no label has any guards.
                return supportsOrPattern && section.Labels.All(label => label.Guards.IsDefaultOrEmpty);
            }
        }

        protected sealed override async Task FixAllAsync(
            Document document,
            ImmutableArray<TextSpan> fixAllSpans,
            SyntaxEditor editor,
            CodeActionOptionsProvider optionsProvider,
            string? equivalenceKey,
            CancellationToken cancellationToken)
        {
            var convertToSwitchExpression = equivalenceKey == SwitchExpressionEquivalenceKey;
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Get all the descendant if statements to process.
            var ifStatements = editor.OriginalRoot.DescendantNodes().OfType<TIfStatementSyntax>();

            // We're going to be continually editing this tree. Track all the nodes we
            // care about so we can find them across each edit.
            document = document.WithSyntaxRoot(editor.OriginalRoot.TrackNodes(ifStatements));

            // Process the if statements.
            foreach (var originalIfStatement in ifStatements)
            {
                // Only process if statements fully within a fixAllSpan
                if (!fixAllSpans.Any(fixAllSpan => fixAllSpan.Contains(originalIfStatement.Span)))
                    continue;

                // Get current root, if statement and semantic model.
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var ifStatement = root.GetCurrentNodes(originalIfStatement).SingleOrDefault();
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Check if the refactoring is applicable for this if statement.
                if (!ShouldOfferRefactoring(ifStatement, semanticModel, syntaxFactsService, out var analyzer, out var sections, out var target))
                    continue;

                // When converting to switch expression, we need to perform an additional check to ensure this conversion is possible.
                if (convertToSwitchExpression && !CanConvertToSwitchExpression(analyzer.Supports(Feature.OrPattern), sections))
                    continue;

                document = await UpdateDocumentAsync(document, target, ifStatement, sections,
                    analyzer.Features, convertToSwitchExpression, cancellationToken).ConfigureAwait(false);
            }

            var updatedRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
        }
    }
}
