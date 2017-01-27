﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static class ReferenceLocationExtensions
    {
        public static async Task<Dictionary<ISymbol, List<Location>>> FindReferencingSymbolsAsync(
            this IEnumerable<ReferenceLocation> referenceLocations,
            CancellationToken cancellationToken)
        {
            var documentGroups = referenceLocations.GroupBy(loc => loc.Document);
            var projectGroups = documentGroups.GroupBy(g => g.Key.Project);
            var result = new Dictionary<ISymbol, List<Location>>();

            foreach (var projectGroup in projectGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = projectGroup.Key;
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                foreach (var documentGroup in projectGroup)
                {
                    var document = documentGroup.Key;
                    await AddSymbolsAsync(document, documentGroup, result, cancellationToken).ConfigureAwait(false);
                }

                GC.KeepAlive(compilation);
            }

            return result;
        }

        private static async Task AddSymbolsAsync(
            Document document,
            IEnumerable<ReferenceLocation> references,
            Dictionary<ISymbol, List<Location>> result,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var reference in references)
            {
                var containingSymbol = GetEnclosingMethodOrPropertyOrField(semanticModel, reference);
                if (containingSymbol != null)
                {
                    if (!result.TryGetValue(containingSymbol, out var locations))
                    {
                        locations = new List<Location>();
                        result.Add(containingSymbol, locations);
                    }

                    locations.Add(reference.Location);
                }
            }
        }

        private static ISymbol GetEnclosingMethodOrPropertyOrField(
            SemanticModel semanticModel,
            ReferenceLocation reference)
        {
            var enclosingSymbol = semanticModel.GetEnclosingSymbol(reference.Location.SourceSpan.Start);

            for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
            {
                if (current.Kind == SymbolKind.Field)
                {
                    return current;
                }

                if (current.Kind == SymbolKind.Property)
                {
                    return current;
                }

                if (current.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)current;
                    if (method.IsAccessor())
                    {
                        return method.AssociatedSymbol;
                    }

                    if (method.MethodKind != MethodKind.AnonymousFunction)
                    {
                        return method;
                    }
                }
            }

            return null;
        }
    }
}
