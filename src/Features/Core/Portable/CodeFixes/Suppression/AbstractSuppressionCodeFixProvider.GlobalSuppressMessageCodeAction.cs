// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class GlobalSuppressMessageCodeAction : AbstractGlobalSuppressMessageCodeAction
        {
            private readonly ISymbol _targetSymbol;
            private readonly Diagnostic _diagnostic;

            public GlobalSuppressMessageCodeAction(ISymbol targetSymbol, Project project, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer)
                : base(fixer, project)
            {
                _targetSymbol = targetSymbol;
                _diagnostic = diagnostic;
            }

            protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
            {
                var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
                var workspace = suppressionsDoc.Project.Solution.Workspace;
                var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await suppressionsDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                suppressionsRoot = await Fixer.AddGlobalSuppressMessageAttributeAsync(suppressionsRoot, _targetSymbol, _diagnostic, workspace, cancellationToken).ConfigureAwait(false);
                return suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
            }

            protected override string DiagnosticIdForEquivalenceKey => _diagnostic.Id;

            internal ISymbol TargetSymbol_TestOnly => _targetSymbol;
        }
    }
}
