// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal sealed class LocalSuppressMessageCodeAction(
            AbstractSuppressionCodeFixProvider fixer,
            ISymbol targetSymbol,
            INamedTypeSymbol suppressMessageAttribute,
            SyntaxNode targetNode,
            Document document,
            Diagnostic diagnostic) : AbstractSuppressionCodeAction(fixer, FeaturesResources.in_Source_attribute)
        {
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newTargetNode = this.Fixer.AddLocalSuppressMessageAttribute(
                    targetNode, targetSymbol, suppressMessageAttribute, diagnostic);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode<SyntaxNode>(targetNode, newTargetNode);
                return document.WithSyntaxRoot(newRoot);
            }

            protected override string DiagnosticIdForEquivalenceKey => diagnostic.Id;

            internal SyntaxNode TargetNode_TestOnly => targetNode;
        }
    }
}
