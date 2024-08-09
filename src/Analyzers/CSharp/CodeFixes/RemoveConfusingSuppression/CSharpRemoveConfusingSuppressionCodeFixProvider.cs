// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveConfusingSuppression), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpRemoveConfusingSuppressionCodeFixProvider() : CodeFixProvider
{
    public const string RemoveOperator = nameof(RemoveOperator);
    public const string NegateExpression = nameof(NegateExpression);

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.RemoveConfusingSuppressionForIsExpressionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostics = context.Diagnostics;
        var cancellationToken = context.CancellationToken;

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpAnalyzersResources.Remove_operator_preserves_semantics,
                c => FixAllAsync(document, diagnostics, negate: false, c),
                RemoveOperator),
            context.Diagnostics);

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpAnalyzersResources.Negate_expression_changes_semantics,
                c => FixAllAsync(document, diagnostics, negate: true, c),
                NegateExpression),
            context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        bool negate, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        var generator = editor.Generator;
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        foreach (var diagnostic in diagnostics)
        {
            var node = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            Debug.Assert(node.Kind() is SyntaxKind.IsExpression or SyntaxKind.IsPatternExpression);

            // Negate the result if requested.
            var updatedNode = negate
                ? generator.Negate(generatorInternal, node, semanticModel, cancellationToken)
                : node;

            var isNode = updatedNode.DescendantNodesAndSelf().First(
                n => n.Kind() is SyntaxKind.IsExpression or SyntaxKind.IsPatternExpression);
            var left = isNode switch
            {
                BinaryExpressionSyntax binary => binary.Left,
                IsPatternExpressionSyntax isPattern => isPattern.Expression,
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };

            // Remove the suppression operator.
            var suppression = (PostfixUnaryExpressionSyntax)left;
            var withoutSuppression = suppression.Operand.WithAppendedTrailingTrivia(suppression.OperatorToken.GetAllTrivia());
            var isWithoutSuppression = updatedNode.ReplaceNode(suppression, withoutSuppression);

            editor.ReplaceNode(node, isWithoutSuppression);
        }

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    public override FixAllProvider GetFixAllProvider()
        => FixAllProvider.Create(async (context, document, diagnostics) =>
            await FixAllAsync(
                document, diagnostics,
                context.CodeActionEquivalenceKey == NegateExpression,
                context.CancellationToken).ConfigureAwait(false));
}
