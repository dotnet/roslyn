// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertCast
{
    /// <summary>
    /// Refactor:
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    ///
    /// Or vice versa.
    /// </summary>
    internal abstract class AbstractConvertCastCodeRefactoringProvider<TTypeNode, TFromExpression, TToExpression>
        : CodeRefactoringProvider
        where TTypeNode : SyntaxNode
        where TFromExpression : SyntaxNode
        where TToExpression : SyntaxNode
    {
        protected abstract string GetTitle();

        protected abstract int FromKind { get; }
        protected abstract TToExpression ConvertExpression(TFromExpression from);
        protected abstract TTypeNode GetTypeNode(TFromExpression from);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var fromNodes = await context.GetRelevantNodesAsync<TFromExpression>().ConfigureAwait(false);
            var from = fromNodes.FirstOrDefault(n => n.RawKind == FromKind);
            if (from == null)
                return;

            if (from.GetDiagnostics().Any(d => d.DefaultSeverity == DiagnosticSeverity.Error))
                return;

            var (document, _, cancellationToken) = context;

            var typeNode = GetTypeNode(from);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (type is { TypeKind: not TypeKind.Error, IsReferenceType: true })
            {
                var title = GetTitle();
                context.RegisterRefactoring(
                    CodeAction.Create(
                        title,
                        c => ConvertAsync(document, from, cancellationToken),
                        title),
                    from.Span);
            }
        }

        protected async Task<Document> ConvertAsync(
            Document document,
            TFromExpression from,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(from, ConvertExpression(from));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
