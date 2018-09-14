// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class MethodTypeParameterSymbolReferenceFinder : AbstractReferenceFinder<ITypeParameterSymbol>
    {
        protected override bool CanFind(ITypeParameterSymbol symbol)
        {
            return symbol.TypeParameterKind == TypeParameterKind.Method;
        }

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<ITypeParameterSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            var method = (IMethodSymbol)symbol.ContainingSymbol;
            var ordinal = method.TypeParameters.IndexOf(symbol);

            if (ordinal >= 0)
            {
                if (method.PartialDefinitionPart != null && ordinal < method.PartialDefinitionPart.TypeParameters.Length)
                {
                    return Task.FromResult(ImmutableArray.Create(
                        symbolAndProjectId.WithSymbol((ISymbol)method.PartialDefinitionPart.TypeParameters[ordinal])));
                }

                if (method.PartialImplementationPart != null && ordinal < method.PartialImplementationPart.TypeParameters.Length)
                {
                    return Task.FromResult(ImmutableArray.Create(
                        symbolAndProjectId.WithSymbol((ISymbol)method.PartialImplementationPart.TypeParameters[ordinal])));
                }
            }

            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ITypeParameterSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Type parameters are only found in documents that have both their name, and the name
            // of its owning method.  NOTE(cyrusn): We have to check in multiple files because of
            // partial types.  A type parameter can be referenced across all the parts. NOTE(cyrusn):
            // We look for type parameters by name.  This means if the same type parameter has a
            // different name in different parts that we won't find it. However, this only happens
            // in error situations.  It is not legal in C# to use a different name for a type
            // parameter in different parts.
            //
            // Also, we only look for files that have the name of the owning type.  This helps filter
            // down the set considerably.
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name,
                GetMemberNameWithoutInterfaceName(symbol.DeclaringMethod.Name),
                symbol.DeclaringMethod.ContainingType.Name);
        }

        private static string GetMemberNameWithoutInterfaceName(string fullName)
        {
            var index = fullName.LastIndexOf('.');
            return index > 0
                ? fullName.Substring(index + 1)
                : fullName;
        }

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ITypeParameterSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // TODO(cyrusn): Method type parameters are like locals.  They are only in scope in
            // the bounds of the method they're declared within.  We could improve perf by
            // limiting our search by only looking within the method body's span. 
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken);
        }
    }
}
