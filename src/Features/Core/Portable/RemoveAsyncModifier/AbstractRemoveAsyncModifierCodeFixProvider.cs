// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider;

namespace Microsoft.CodeAnalysis.RemoveAsyncModifier
{
    internal abstract class AbstractRemoveAsyncModifierCodeFixProvider : CodeFixProvider
    {
        public static readonly string EquivalenceKey = FeaturesResources.Remove_async_modifier;

        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
        protected abstract SyntaxNode RemoveAsyncModifier(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes);
        protected abstract SyntaxNode ChangeReturnStatements(SyntaxNode node, SyntaxGenerator generator, KnownTypes knownTypes);
        protected virtual bool CanFix(KnownTypes knownTypes, IMethodSymbol methodSymbolOpt) => true;

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

            var diagnostic = context.Diagnostics.First();
            var token = diagnostic.Location.FindToken(context.CancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var declaredSymbol = semanticModel.GetDeclaredSymbol(node);

            if (declaredSymbol == null || (declaredSymbol is IMethodSymbol methodSymbolOpt &&
                !methodSymbolOpt.ReturnsVoid &&
                CanFix(knownTypes, methodSymbolOpt)))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixNodeAsync(context.Document, diagnostic, c)),
                    context.Diagnostics);
            }
        }

        private async Task<Document> FixNodeAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

            var newNode = RemoveAsyncModifier(methodSymbolOpt, node, knownTypes);

            var generator = SyntaxGenerator.GetGenerator(document);

            newNode = ChangeReturnStatements(newNode, generator, knownTypes);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = root.ReplaceNode(node, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Remove_async_modifier, createChangedDocument, AbstractRemoveAsyncModifierCodeFixProvider.EquivalenceKey)
            {
            }
        }
    }
}
