// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
{
    internal abstract class AbstractConvertTypeOfToNameOfCodeFixProvider<
        TMemberAccessExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TMemberAccessExpressionSyntax : SyntaxNode
    {
        protected abstract string GetCodeFixTitle();

        protected abstract SyntaxNode GetSymbolTypeExpression(SemanticModel model, TMemberAccessExpressionSyntax node, CancellationToken cancellationToken);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var title = GetCodeFixTitle();
            RegisterCodeFix(context, title, title);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                if (editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not TMemberAccessExpressionSyntax node)
                    continue;

                ConvertTypeOfToNameOf(semanticModel, editor, node, cancellationToken);
            }
        }

        /// <Summary>
        ///  Method converts typeof(...).Name to nameof(...)
        /// </Summary>
        public void ConvertTypeOfToNameOf(SemanticModel semanticModel, SyntaxEditor editor, TMemberAccessExpressionSyntax nodeToReplace, CancellationToken cancellationToken)
        {
            var typeExpression = GetSymbolTypeExpression(semanticModel, nodeToReplace, cancellationToken);
            var nameOfSyntax = editor.Generator.NameOfExpression(typeExpression);
            editor.ReplaceNode(nodeToReplace, nameOfSyntax);
        }
    }
}
