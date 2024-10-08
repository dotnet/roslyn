// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.DocumentHighlighting;

[ExportLanguageService(typeof(IDocumentHighlightsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CSharpDocumentHighlightsService(
    [ImportMany] IEnumerable<Lazy<IEmbeddedLanguageDocumentHighlighter, EmbeddedLanguageMetadata>> services)
    : AbstractDocumentHighlightsService(
        LanguageNames.CSharp,
        CSharpEmbeddedLanguagesProvider.Info,
        CSharpSyntaxKinds.Instance,
        services)
{
    protected override async Task<ImmutableArray<Location>> GetAdditionalReferencesAsync(
        Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        // The FindRefs engine won't find references through 'var' for performance reasons.
        // Also, they are not needed for things like rename/sig change, and the normal find refs
        // feature.  However, we would like the results to be highlighted to get a good experience
        // while editing (especially since highlighting may have been invoked off of 'var' in
        // the first place).
        //
        // So we look for the references through 'var' directly in this file and add them to the
        // results found by the engine.
        using var _ = ArrayBuilder<Location>.GetInstance(out var results);

        if (symbol is INamedTypeSymbol && symbol.Name != "var")
        {
            var originalSymbol = symbol.OriginalDefinition;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var descendants = root.DescendantNodes();
            var semanticModel = (SemanticModel?)null;

            foreach (var type in descendants.OfType<IdentifierNameSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (type.IsVar)
                {
                    // Document highlights are not impacted by nullable analysis.  Get a semantic model with nullability
                    // disabled to lower the amount of work we need to do here.
                    semanticModel ??= await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    var boundSymbol = semanticModel.GetSymbolInfo(type, cancellationToken).Symbol;
                    boundSymbol = boundSymbol?.OriginalDefinition;

                    if (originalSymbol.Equals(boundSymbol))
                        results.Add(type.GetLocation());
                }
            }
        }

        return results.ToImmutableAndClear();
    }
}
