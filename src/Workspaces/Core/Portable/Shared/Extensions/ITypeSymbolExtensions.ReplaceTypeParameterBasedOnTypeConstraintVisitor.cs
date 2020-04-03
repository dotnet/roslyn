// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class ReplaceTypeParameterBasedOnTypeConstraintVisitor : AsyncSymbolVisitor<ITypeSymbol>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Compilation _compilation;
            private readonly ISet<string> _availableTypeParameterNames;
            private readonly Solution _solution;

            public ReplaceTypeParameterBasedOnTypeConstraintVisitor(Compilation compilation, ISet<string> availableTypeParameterNames, Solution solution, CancellationToken cancellationToken)
            {
                _compilation = compilation;
                _availableTypeParameterNames = availableTypeParameterNames;
                _solution = solution;
                _cancellationToken = cancellationToken;
            }

            protected override ITypeSymbol DefaultResult => throw new NotImplementedException();

            public override ValueTask<ITypeSymbol> VisitDynamicType(IDynamicTypeSymbol symbol)
                => new ValueTask<ITypeSymbol>(symbol);

            public override async ValueTask<ITypeSymbol> VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = await symbol.ElementType.Accept(this).ConfigureAwait(false);
                if (elementType != null && elementType.Equals(symbol.ElementType))
                {
                    return symbol;
                }

                return _compilation.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override async ValueTask<ITypeSymbol> VisitNamedType(INamedTypeSymbol symbol)
            {
                var arguments = await SpecializedTasks.WhenAll(symbol.TypeArguments.Select(t => t.Accept(this))).ConfigureAwait(false);
                if (arguments.SequenceEqual(symbol.TypeArguments))
                {
                    return symbol;
                }

                return symbol.ConstructedFrom.Construct(arguments.ToArray());
            }

            public override async ValueTask<ITypeSymbol> VisitPointerType(IPointerTypeSymbol symbol)
            {
                var elementType = await symbol.PointedAtType.Accept(this).ConfigureAwait(false);
                if (elementType != null && elementType.Equals(symbol.PointedAtType))
                {
                    return symbol;
                }

                return _compilation.CreatePointerTypeSymbol(elementType);
            }

            public override async ValueTask<ITypeSymbol> VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_availableTypeParameterNames.Contains(symbol.Name))
                {
                    return symbol;
                }

                switch (symbol.ConstraintTypes.Length)
                {
                    case 0:
                        // If there are no constraint then there is no replacement required
                        // Just return the symbol
                        return symbol;

                    case 1:
                        // If there is one constraint which is a INamedTypeSymbol then return the INamedTypeSymbol
                        // because the TypeParameter is expected to be of that type
                        // else return the original symbol
                        return symbol.ConstraintTypes.ElementAt(0) as INamedTypeSymbol ?? (ITypeSymbol)symbol;

                    // More than one
                    default:
                        if (symbol.ConstraintTypes.All(t => t is INamedTypeSymbol))
                        {
                            var immutableProjects = _solution.Projects.ToImmutableHashSet();
                            var derivedImplementedTypesOfEachConstraintType = await Task.WhenAll(symbol.ConstraintTypes.Select(async ct =>
                            {
                                var derivedAndImplementedTypes = new List<INamedTypeSymbol>();
                                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync((INamedTypeSymbol)ct, _solution, immutableProjects, _cancellationToken).ConfigureAwait(false);
                                var implementedTypes = await DependentTypeFinder.FindTransitivelyImplementingStructuresAndClassesAsync((INamedTypeSymbol)ct, _solution, immutableProjects, _cancellationToken).ConfigureAwait(false);
                                return derivedClasses.Concat(implementedTypes.Select(s => s.Symbol)).ToList();
                            })).ConfigureAwait(false);

                            var intersectingTypes = derivedImplementedTypesOfEachConstraintType.Aggregate((x, y) => x.Intersect(y).ToList());

                            // If there was any intersecting derived type among the constraint types then pick the first of the lot.
                            if (intersectingTypes.Any())
                            {
                                var resultantIntersectingType = intersectingTypes.First();

                                // If the resultant intersecting type contains any Type arguments that could be replaced 
                                // using the type constraints then recursively update the type until all constraints are appropriately handled
                                var typeConstraintConvertedType = await resultantIntersectingType.Accept(this).ConfigureAwait(false);
                                var knownSimilarTypesInCompilation = SymbolFinder.FindSimilarSymbols(typeConstraintConvertedType, _compilation, _cancellationToken);
                                if (knownSimilarTypesInCompilation.Any())
                                {
                                    return knownSimilarTypesInCompilation.First();
                                }

                                var resultantSimilarKnownTypes = SymbolFinder.FindSimilarSymbols(resultantIntersectingType, _compilation, _cancellationToken);
                                return resultantSimilarKnownTypes.FirstOrDefault() ?? (ITypeSymbol)symbol;
                            }
                        }

                        return symbol;
                }
            }
        }
    }
}
