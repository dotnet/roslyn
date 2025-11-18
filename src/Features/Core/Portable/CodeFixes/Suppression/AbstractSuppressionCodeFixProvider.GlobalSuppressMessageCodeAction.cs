// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
{
    internal sealed class GlobalSuppressMessageCodeAction(
        ISymbol targetSymbol,
        INamedTypeSymbol suppressMessageAttribute,
        Project project, Diagnostic diagnostic,
        AbstractSuppressionCodeFixProvider fixer) : AbstractGlobalSuppressMessageCodeAction(fixer, project)
    {
        private readonly INamedTypeSymbol _suppressMessageAttribute = suppressMessageAttribute;
        private readonly Diagnostic _diagnostic = diagnostic;

        protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
        {
            var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
            var services = suppressionsDoc.Project.Solution.Services;
            var suppressionsRoot = await suppressionsDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportsService = suppressionsDoc.GetRequiredLanguageService<IAddImportsService>();
            var options = await suppressionsDoc.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            suppressionsRoot = Fixer.AddGlobalSuppressMessageAttribute(
                suppressionsRoot, TargetSymbol_TestOnly, _suppressMessageAttribute, _diagnostic, services, options, addImportsService, cancellationToken);
            return suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
        }

        protected override string DiagnosticIdForEquivalenceKey => _diagnostic.Id;

        internal ISymbol TargetSymbol_TestOnly { get; } = targetSymbol;
    }
}
