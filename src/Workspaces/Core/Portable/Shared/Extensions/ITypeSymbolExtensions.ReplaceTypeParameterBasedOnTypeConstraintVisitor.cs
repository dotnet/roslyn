// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class ReplaceTypeParameterBasedOnTypeConstraintVisitor : SymbolVisitor<ITypeSymbol>
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

            public override ITypeSymbol DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override ITypeSymbol VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return symbol;
            }

            public override ITypeSymbol VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = symbol.ElementType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.ElementType))
                {
                    return symbol;
                }

                return _compilation.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override ITypeSymbol VisitNamedType(INamedTypeSymbol symbol)
            {
                var arguments = symbol.TypeArguments.Select(t => t.Accept(this)).ToArray();
                if (arguments.SequenceEqual(symbol.TypeArguments))
                {
                    return symbol;
                }

                return symbol.ConstructedFrom.Construct(arguments.ToArray());
            }

            public override ITypeSymbol VisitPointerType(IPointerTypeSymbol symbol)
            {
                var elementType = symbol.PointedAtType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.PointedAtType))
                {
                    return symbol;
                }

                return _compilation.CreatePointerTypeSymbol(elementType);
            }

            public override ITypeSymbol VisitTypeParameter(ITypeParameterSymbol symbol)
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
                            var derivedImplementedTypesOfEachConstraintType = symbol.ConstraintTypes.Select(ct =>
                            {
                                var derivedAndImplementedTypes = new List<INamedTypeSymbol>();
                                return SymbolFinder.FindDerivedClassesAsync((INamedTypeSymbol)ct, _solution, immutableProjects, _cancellationToken).WaitAndGetResult(_cancellationToken)
                                       .Concat(DependentTypeFinder.FindImplementingTypesAsync((INamedTypeSymbol)ct, _solution, immutableProjects, _cancellationToken).WaitAndGetResult(_cancellationToken))
                                       .ToList();
                            });

                            var intersectingTypes = derivedImplementedTypesOfEachConstraintType.Aggregate((x, y) => x.Intersect(y).ToList());

                            // If there was any intersecting derived type among the constraint types then pick the first of the lot.
                            if (intersectingTypes.Any())
                            {
                                var resultantIntersectingType = intersectingTypes.First();

                                // If the resultant intersecting type contains any Type arguments that could be replaced 
                                // using the type constraints then recursively update the type until all constraints are appropriately handled
                                var typeConstraintConvertedType = resultantIntersectingType.Accept(this);
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
