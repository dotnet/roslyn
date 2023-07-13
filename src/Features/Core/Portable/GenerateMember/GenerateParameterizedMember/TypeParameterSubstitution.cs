// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        private static async ValueTask<ITypeSymbol> ReplaceTypeParametersBasedOnTypeConstraintsAsync(
            Project project,
            ITypeSymbol type,
            Compilation compilation,
            ISet<string> availableTypeParameterNames,
            CancellationToken cancellationToken)
        {
            var visitor = new DetermineSubstitutionsVisitor(
                compilation, availableTypeParameterNames, project, cancellationToken);

            await visitor.Visit(type).ConfigureAwait(false);
            return type.SubstituteTypes(visitor.Substitutions, compilation);
        }

        private sealed class DetermineSubstitutionsVisitor(
            Compilation compilation, ISet<string> availableTypeParameterNames, Project project, CancellationToken cancellationToken) : AsyncSymbolVisitor
        {
            public readonly Dictionary<ITypeSymbol, ITypeSymbol> Substitutions =
                new();
            private readonly CancellationToken _cancellationToken = cancellationToken;
            private readonly Compilation _compilation = compilation;
            private readonly ISet<string> _availableTypeParameterNames = availableTypeParameterNames;
            private readonly Project _project = project;

            public override ValueTask VisitDynamicType(IDynamicTypeSymbol symbol)
                => default;

            public override ValueTask VisitArrayType(IArrayTypeSymbol symbol)
                => symbol.ElementType.Accept(this);

            public override async ValueTask VisitNamedType(INamedTypeSymbol symbol)
            {
                foreach (var typeArg in symbol.TypeArguments)
                    await typeArg.Accept(this).ConfigureAwait(false);
            }

            public override ValueTask VisitPointerType(IPointerTypeSymbol symbol)
                => symbol.PointedAtType.Accept(this);

            public override async ValueTask VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_availableTypeParameterNames.Contains(symbol.Name))
                    return;

                switch (symbol.ConstraintTypes.Length)
                {
                    case 0:
                        // If there are no constraint then there is no replacement required.
                        return;

                    case 1:
                        // If there is one constraint which is a INamedTypeSymbol then return the INamedTypeSymbol
                        // because the TypeParameter is expected to be of that type
                        // else return the original symbol
                        if (symbol.ConstraintTypes.ElementAt(0) is INamedTypeSymbol namedType)
                            Substitutions.Add(symbol, namedType);

                        return;
                }

                var commonDerivedType = await DetermineCommonDerivedTypeAsync(symbol).ConfigureAwait(false);
                if (commonDerivedType != null)
                    Substitutions.Add(symbol, commonDerivedType);
            }

            private async ValueTask<ITypeSymbol> DetermineCommonDerivedTypeAsync(ITypeParameterSymbol symbol)
            {
                if (!symbol.ConstraintTypes.All(t => t is INamedTypeSymbol))
                    return null;

                var solution = _project.Solution;
                var projects = solution.Projects.ToImmutableHashSet();

                var commonTypes = await GetDerivedAndImplementedTypesAsync(
                    (INamedTypeSymbol)symbol.ConstraintTypes[0], projects).ConfigureAwait(false);

                for (var i = 1; i < symbol.ConstraintTypes.Length; i++)
                {
                    var currentTypes = await GetDerivedAndImplementedTypesAsync(
                        (INamedTypeSymbol)symbol.ConstraintTypes[i], projects).ConfigureAwait(false);
                    commonTypes.IntersectWith(currentTypes);

                    if (commonTypes.Count == 0)
                        return null;
                }

                // If there was any intersecting derived type among the constraint types then pick the first of the lot.
                if (commonTypes.Count == 0)
                    return null;

                var commonType = commonTypes.First();

                // If the resultant intersecting type contains any Type arguments that could be replaced 
                // using the type constraints then recursively update the type until all constraints are appropriately handled
                var substitutedType = await ReplaceTypeParametersBasedOnTypeConstraintsAsync(
                    _project, commonType, _compilation, _availableTypeParameterNames, _cancellationToken).ConfigureAwait(false);

                var similarTypes = SymbolFinder.FindSimilarSymbols(substitutedType, _compilation, _cancellationToken);
                if (similarTypes.Any())
                    return similarTypes.First();

                similarTypes = SymbolFinder.FindSimilarSymbols(commonType, _compilation, _cancellationToken);
                return similarTypes.FirstOrDefault() ?? symbol;
            }

            private async Task<ISet<INamedTypeSymbol>> GetDerivedAndImplementedTypesAsync(
                INamedTypeSymbol constraintType, IImmutableSet<Project> projects)
            {
                var solution = _project.Solution;

                var symbol = constraintType;
                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                    symbol, solution, transitive: true, projects, _cancellationToken).ConfigureAwait(false);

                var implementedTypes = await SymbolFinder.FindImplementationsAsync(
                    symbol, solution, transitive: true, projects, _cancellationToken).ConfigureAwait(false);

                return derivedClasses.Concat(implementedTypes).ToSet();
            }
        }
    }
}
