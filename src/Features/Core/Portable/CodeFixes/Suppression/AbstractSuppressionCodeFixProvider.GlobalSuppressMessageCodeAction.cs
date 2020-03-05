// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal sealed class GlobalSuppressMessageCodeAction : AbstractGlobalSuppressMessageCodeAction
        {
            private readonly ISymbol _targetSymbol;
            private readonly INamedTypeSymbol _suppressMessageAttribute;
            private readonly Diagnostic _diagnostic;

            public GlobalSuppressMessageCodeAction(
                ISymbol targetSymbol, INamedTypeSymbol suppressMessageAttribute,
                Project project, Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer)
                : base(fixer, project)
            {
                _targetSymbol = targetSymbol;
                _suppressMessageAttribute = suppressMessageAttribute;
                _diagnostic = diagnostic;
            }

            protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
            {
                var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
                var workspace = suppressionsDoc.Project.Solution.Workspace;
                var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var compilation = await suppressionsDoc.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var addImportsService = suppressionsDoc.GetRequiredLanguageService<IAddImportsService>();

                suppressionsRoot = Fixer.AddGlobalSuppressMessageAttribute(
                    suppressionsRoot, _targetSymbol, _suppressMessageAttribute, _diagnostic, workspace, compilation, addImportsService, cancellationToken);
                return suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
            }

            protected override string DiagnosticIdForEquivalenceKey => _diagnostic.Id;

            internal ISymbol TargetSymbol_TestOnly => _targetSymbol;
        }
    }
}
