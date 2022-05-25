// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    // Code for the CodeFixProvider ("Fixing") portion of the feature.

    internal partial class UseExpressionBodyForLambdaCodeStyleProvider
    {
        protected override Task<ImmutableArray<CodeAction>> ComputeCodeActionsAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var title = diagnostic.GetMessage();
            var codeAction = CodeAction.Create(
                title,
                c => FixWithSyntaxEditorAsync(document, diagnostic, c),
                title);

            return Task.FromResult(ImmutableArray.Create<CodeAction>(codeAction));
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, semanticModel, diagnostic, cancellationToken);
            }
        }

        private static void AddEdits(
            SyntaxEditor editor, SemanticModel semanticModel,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var originalDeclaration = (LambdaExpressionSyntax)declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.ReplaceNode(
                originalDeclaration,
                (current, _) => Update(semanticModel, originalDeclaration, (LambdaExpressionSyntax)current));
        }
    }
}
