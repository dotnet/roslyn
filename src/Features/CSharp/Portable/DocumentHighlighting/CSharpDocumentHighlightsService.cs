// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.DocumentHighlighting
{
    [ExportLanguageService(typeof(IDocumentHighlightsService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentHighlightsService : AbstractDocumentHighlightsService
    {
        [ImportingConstructor]
        public CSharpDocumentHighlightsService()
        {
        }

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
            var results = ArrayBuilder<Location>.GetInstance();

            if (symbol is INamedTypeSymbol && symbol.Name != "var")
            {
                var originalSymbol = symbol.OriginalDefinition;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var descendents = root.DescendantNodes();
                var semanticModel = default(SemanticModel);

                foreach (var type in descendents.OfType<IdentifierNameSyntax>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (type.IsVar)
                    {
                        if (semanticModel == null)
                        {
                            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        }

                        var boundSymbol = semanticModel.GetSymbolInfo(type, cancellationToken).Symbol;
                        boundSymbol = boundSymbol?.OriginalDefinition;

                        if (originalSymbol.Equals(boundSymbol))
                        {
                            results.Add(type.GetLocation());
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }
    }
}
