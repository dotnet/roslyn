// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal sealed class GlobalSuppressMessageCodeAction(
            ISymbol targetSymbol, INamedTypeSymbol suppressMessageAttribute,
            Project project, Diagnostic diagnostic,
            AbstractSuppressionCodeFixProvider fixer,
            CodeActionOptionsProvider fallbackOptions) : AbstractGlobalSuppressMessageCodeAction(fixer, project)
        {
            protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
            {
                var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
                var services = suppressionsDoc.Project.Solution.Services;
                var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var addImportsService = suppressionsDoc.GetRequiredLanguageService<IAddImportsService>();
                var options = await suppressionsDoc.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

                suppressionsRoot = Fixer.AddGlobalSuppressMessageAttribute(
                    suppressionsRoot, targetSymbol, suppressMessageAttribute, diagnostic, services, options, addImportsService, cancellationToken);
                return suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
            }

            protected override string DiagnosticIdForEquivalenceKey => diagnostic.Id;

            internal ISymbol TargetSymbol_TestOnly => targetSymbol;
        }
    }
}
