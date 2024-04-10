// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

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
        private readonly AbstractSuppressionCodeFixProvider _fixer = fixer;
        private readonly ISymbol _targetSymbol = targetSymbol;
        private readonly INamedTypeSymbol _suppressMessageAttribute = suppressMessageAttribute;
        private readonly SyntaxNode _targetNode = targetNode;
        private readonly Document _document = document;
        private readonly Diagnostic _diagnostic = diagnostic;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var newTargetNode = _fixer.AddLocalSuppressMessageAttribute(
                _targetNode, _targetSymbol, _suppressMessageAttribute, _diagnostic);
            var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode<SyntaxNode>(_targetNode, newTargetNode);
            return _document.WithSyntaxRoot(newRoot);
        }

        protected override string DiagnosticIdForEquivalenceKey => _diagnostic.Id;

        internal SyntaxNode TargetNode_TestOnly => _targetNode;
    }
}
