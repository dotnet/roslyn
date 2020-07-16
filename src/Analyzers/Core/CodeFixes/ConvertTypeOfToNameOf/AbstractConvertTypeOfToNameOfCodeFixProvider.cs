// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
{
    internal abstract class AbstractConvertTypeOfToNameOfCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId);
        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
               context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                ConvertTypeOfToNameOf(editor, node, semanticModel);
            }
        }

        /// <Summary>
        ///  Method converts typeof(...).Name to nameof(...)
        /// </Summary>
        public void ConvertTypeOfToNameOf(SyntaxEditor editor,
                                                      SyntaxNode nodeToReplace,
                                                      SemanticModel semanticModel)
        {
            var symbolType = GetSymbolType(nodeToReplace, semanticModel);

            if (symbolType != null)
            {
                var typeExpression = editor.Generator.TypeExpression(symbolType);
                var nameOfSyntax = editor.Generator.NameOfExpression(typeExpression);
                editor.ReplaceNode(nodeToReplace, nameOfSyntax);
            }
        }

        /// <Summary>
        ///  Method gets the ITypeSymbol of the node
        /// </Summary>
        protected abstract ITypeSymbol GetSymbolType(SyntaxNode node, SemanticModel model);

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Convert_typeof_to_nameof, createChangedDocument, AnalyzersResources.Convert_typeof_to_nameof)
            {
            }
        }
    }
}
