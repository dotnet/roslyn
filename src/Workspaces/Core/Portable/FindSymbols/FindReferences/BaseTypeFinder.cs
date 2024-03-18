// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.FindReferences;

internal static partial class BaseTypeFinder
{
    public static ImmutableArray<INamedTypeSymbol> FindBaseTypesAndInterfaces(INamedTypeSymbol type)
        => FindBaseTypes(type).AddRange(type.AllInterfaces);

    public static async ValueTask<ImmutableArray<ISymbol>> FindOverriddenAndImplementedMembersAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var results = ArrayBuilder<ISymbol>.GetInstance();

        // This is called for all: class, struct or interface member.
        results.AddRange(symbol.ExplicitOrImplicitInterfaceImplementations());

        // The type scenario. Iterate over all base classes to find overridden and hidden (new/Shadows) methods.
        foreach (var type in FindBaseTypes(symbol.ContainingType))
        {
            foreach (var member in type.GetMembers(symbol.Name))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Add to results overridden members only. Do not add hidden members.
                if (await SymbolFinder.IsOverrideAsync(solution, symbol, member, cancellationToken).ConfigureAwait(false))
                {
                    results.Add(member);

                    // We should add implementations only for overridden members but not for hidden ones.
                    // In the following example:
                    //
                    // interface I { void M(); }
                    // class A : I { public void M(); }
                    // class B : A { public new void M(); }
                    //
                    // we should not find anything for B.M() because it does not implement the interface:
                    //
                    // I i = new B(); i.M(); 
                    //
                    // will call the method from A.
                    // However, if we change the code to 
                    //
                    // class B : A, I { public new void M(); }
                    //
                    // then
                    //
                    // I i = new B(); i.M(); 
                    //
                    // will call the method from B. We should find the base for B.M in this case.
                    // And if we change 'new' to 'override' in the original code and add 'virtual' where needed, 
                    // we should find I.M as a base for B.M(). And the next line helps with this scenario.
                    results.AddRange(member.ExplicitOrImplicitInterfaceImplementations());
                }
            }
        }

        // Remove duplicates from interface implementations before adding their projects.
        return results.ToImmutableAndFree().Distinct();
    }

    private static ImmutableArray<INamedTypeSymbol> FindBaseTypes(INamedTypeSymbol type)
    {
        var typesBuilder = ArrayBuilder<INamedTypeSymbol>.GetInstance();

        var currentType = type.BaseType;
        while (currentType != null)
        {
            typesBuilder.Add(currentType);
            currentType = currentType.BaseType;
        }

        return typesBuilder.ToImmutableAndFree();
    }
}
