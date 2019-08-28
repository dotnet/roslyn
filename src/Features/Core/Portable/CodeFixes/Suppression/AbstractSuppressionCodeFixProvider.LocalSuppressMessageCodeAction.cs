// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal sealed class LocalSuppressMessageCodeAction : AbstractSuppressionCodeAction
        {
            private readonly AbstractSuppressionCodeFixProvider _fixer;
            private readonly ISymbol _targetSymbol;
            private readonly SyntaxNode _targetNode;
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            public LocalSuppressMessageCodeAction(AbstractSuppressionCodeFixProvider fixer, ISymbol targetSymbol, SyntaxNode targetNode, Document document, Diagnostic diagnostic)
                : base(fixer, FeaturesResources.in_Source_attribute)
            {
                _fixer = fixer;
                _targetSymbol = targetSymbol;
                _targetNode = targetNode;
                _document = document;
                _diagnostic = diagnostic;
            }

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newTargetNode = _fixer.AddLocalSuppressMessageAttribute(_targetNode, _targetSymbol, _diagnostic);
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(_targetNode, newTargetNode);
                return _document.WithSyntaxRoot(newRoot);
            }

            protected override string DiagnosticIdForEquivalenceKey => _diagnostic.Id;

            internal SyntaxNode TargetNode_TestOnly => _targetNode;
        }
    }
}
