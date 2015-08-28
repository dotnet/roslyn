// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        internal partial class ImplementInterfaceCodeAction
        {
            private bool HasConflictingMember(ISymbol member, List<ISymbol> implementedVisibleMembers)
            {
                // Checks if this member conflicts with an existing member in classOrStructType or with
                // a method we've already implemented.  If so, we'll need to implement this one
                // explicitly.

                var allMembers = State.ClassOrStructType.GetAccessibleMembersInThisAndBaseTypes<ISymbol>(State.ClassOrStructType).Concat(implementedVisibleMembers);

                var conflict1 = allMembers.Any(m => HasConflict(m, member));
                var conflict2 = IsReservedName(member.Name);

                return conflict1 || conflict2;
            }

            private bool HasConflict(ISymbol member1, ISymbol member2)
            {
                // If either of these members are invisible explicit, then there is no conflict.
                if (Service.HasHiddenExplicitImplementation)
                {
                    if (member1.ExplicitInterfaceImplementations().Any() || member2.ExplicitInterfaceImplementations().Any())
                    {
                        // explicit methods don't conflict with anything.
                        return false;
                    }
                }

                // Members normally conflict if they have the same name.  The exceptions are methods
                // and parameterized properties (which conflict if they have the same signature).
                if (!IdentifiersMatch(member1.Name, member2.Name))
                {
                    return false;
                }

                // If they differ in type, then it's almost always a conflict.  There may be
                // exceptions to this, but i don't know of any.
                if (member1.Kind != member2.Kind)
                {
                    return true;
                }

                // At this point, we have two members of the same type with the same name.  If they
                // have a different signature (for example, methods, or parameterized properties),
                // then they do not conflict.
                if (!SignatureComparer.Instance.HaveSameSignature(member1, member2, this.IsCaseSensitive))
                {
                    return false;
                }

                // Now we have to members with the same name, type and signature. If the language
                // doesn't support implicit implementation, then these members are definitely in
                // conflict.
                if (!Service.CanImplementImplicitly)
                {
                    return true;
                }

                // two members conflict if they have the same signature and have
                //
                // a) different return types
                // b) different accessibility
                // c) different constraints
                if (member1.DeclaredAccessibility != member2.DeclaredAccessibility ||
                    !SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(member1, member2, this.IsCaseSensitive))
                {
                    return true;
                }

                // Same name, type, accessibility, return type, *and* the services can implement
                // implicitly.  These are not in conflict.
                return false;
            }
        }
    }
}
