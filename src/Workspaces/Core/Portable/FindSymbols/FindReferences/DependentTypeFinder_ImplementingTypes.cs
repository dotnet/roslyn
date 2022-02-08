// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        private static async Task<ImmutableArray<INamedTypeSymbol>> FindImplementingTypesInCurrentProcessAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project>? projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type?.TypeKind == TypeKind.Interface)
            {
                // Note: it is intentional that we do TypeHasBaseTypeInSet even though we're searching for interfaces
                // (which are never in the BaseType chain of a type).  The reason for this is the following case:
                //
                //  interface IGoo { }
                //
                //  class Base : IGoo { }
                //
                //  class Derived : Base { }
                //
                // In this case, IGoo has two implementations (Base and Derived).  When searching we'll first find the
                // 'Base' match and will add to the set.  Then, we'll look for types that have 'Base' in their
                // inheritance chain, and we need to match that by looking in the .BaseType inheritance chain when
                // looking at 'Derived'.
                static bool TypeMatches(INamedTypeSymbol type, HashSet<INamedTypeSymbol> set)
                    => TypeHasBaseTypeInSet(type, set) || TypeHasInterfaceInSet(type, set);

                // As long as we keep hitting derived interfaces or implementing non-sealed classes we need to keep
                // looking (as those types themselves may have more derived classes that would b implementations of this
                // interface).  If we hit structs/sealed-classes though we can stop as they can't have any more types
                // that inherit from them.
                var allTypes = await DescendInheritanceTreeAsync(type, solution, projects,
                    typeMatches: TypeMatches,
                    shouldContinueSearching: s_isInterfaceOrNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                // Only classes/struct/delegates/enums implement interface types.  Derived interfaces can be found with
                // FindDerivedInterfacesAsync.  Delegates/Enums only happen in a few corner cases.  For example, enums
                // implement IComparable, and delegates implement ICloneable.
                return allTypes.WhereAsArray(
                    t => t.TypeKind is TypeKind.Class or
                         TypeKind.Struct or
                         TypeKind.Delegate or
                         TypeKind.Enum);
            }

            return ImmutableArray<INamedTypeSymbol>.Empty;
        }
    }
}
