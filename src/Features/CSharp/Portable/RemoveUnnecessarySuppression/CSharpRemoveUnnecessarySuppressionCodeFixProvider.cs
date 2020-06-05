// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppression
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpRemoveUnnecessarySuppressionCodeFixProvider : CodeFixProvider
    {
        private const string RemoveOperator = nameof(RemoveOperator);
        private const string NegateExpression = nameof(NegateExpression);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRemoveUnnecessarySuppressionCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessarySuppressionForIsExpressionDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
            => new CSharpRemoveUnnecessarySuppressionFixAllProvider();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;

            context.RegisterCodeFix(
                new MyCodeAction(
                    CSharpFeaturesResources.Remove_operator_preserves_semantics,
                    c => FixAllAsync(document, diagnostics, negate: false, c),
                    RemoveOperator),
                context.Diagnostics);

            context.RegisterCodeFix(
                new MyCodeAction(
                    CSharpFeaturesResources.Negate_expression_changes_semantics,
                    c => FixAllAsync(document, diagnostics, negate: true, c),
                    NegateExpression),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private static async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            bool negate, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;
            var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
                var left = node switch
                {
                    BinaryExpressionSyntax binary => binary.Left,
                    IsPatternExpressionSyntax isPattern => isPattern.Expression,
                    _ => throw ExceptionUtilities.UnexpectedValue(node),
                };

                var suppression = (PostfixUnaryExpressionSyntax)left;

                // Remove the suppression operator.
                var newNode = node.ReplaceNode(
                    left, suppression.Operand.WithAppendedTrailingTrivia(suppression.OperatorToken.GetAllTrivia()));

                // Negate the result if requested.
                var final = negate
                    ? generator.Negate(generatorInternal, newNode, semanticModel, cancellationToken)
                    : newNode;

                editor.ReplaceNode(node, final);
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
