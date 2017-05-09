// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ReferenceHighlighting
{
    [ExportLanguageService(typeof(IReferenceHighlightingAdditionalReferenceProvider), LanguageNames.CSharp), Shared]
    internal class ReferenceHighlightingAdditionalReferenceProvider : IReferenceHighlightingAdditionalReferenceProvider
    {
        public async Task<IEnumerable<Location>> GetAdditionalReferencesAsync(
            Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            // The FindRefs engine won't find references through 'var' for performance reasons.
            // Also, they are not needed for things like rename/sig change, and the normal find refs
            // feature.  However, we would lke the results to be highlighted to get a good experience
            // while editing (especially since highlighting may have been invoked off of 'var' in
            // the first place).
            //
            // So we look for the references through 'var' directly in this file and add them to the
            // results found by the engine.
            List<Location> results = null;

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
                        boundSymbol = boundSymbol == null ? null : boundSymbol.OriginalDefinition;

                        if (originalSymbol.Equals(boundSymbol))
                        {
                            if (results == null)
                            {
                                results = new List<Location>();
                            }

                            results.Add(type.GetLocation());
                        }
                    }
                }
            }

            return results ?? SpecializedCollections.EmptyEnumerable<Location>();
        }
    }
}
