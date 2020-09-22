// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SymbolDisplayVisitorHelpers
    {
        public static IMethodSymbol FindValidCloneMethod(ITypeSymbol containingType)
        {
            if (containingType.SpecialType == SpecialType.System_Object)
            {
                return null;
            }

            IMethodSymbol candidate = null;

            // TO-DO: Change hard-coded '<Clone>$' to WellKnownMemberNames.CloneMethodName once it is made public
            foreach (var member in containingType.GetMembers("<Clone>$"))
            {
                if (member is IMethodSymbol
                    {
                        DeclaredAccessibility: Accessibility.Public,
                        IsStatic: false,
                        Parameters: { Length: 0 },
                        Arity: 0
                    } method)
                {
                    if (candidate is object)
                    {
                        // An ambiguity case, can come from metadata, treat as an error for simplicity.
                        return null;
                    }

                    candidate = method;
                }
            }

            if (candidate is null ||
                !(containingType.IsSealed || candidate.IsOverride || candidate.IsVirtual || candidate.IsAbstract) ||
                !isEqualToOrDerivedFrom(
                    containingType,
                    candidate.ReturnType))
            {
                return null;
            }

            return candidate;

            static bool isEqualToOrDerivedFrom(ITypeSymbol one, ITypeSymbol other)
            {
                do
                {
                    if (one.Equals(other, SymbolEqualityComparer.IgnoreAll))
                    {
                        return true;
                    }

                    one = one.BaseType;
                }
                while (one != null);

                return false;
            }
        }
    }
}
