// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SimpleNameSyntaxExtensions
    {
        public static ExpressionSyntax GetLeftSideOfDot(this SimpleNameSyntax name)
        {
            Debug.Assert(name.IsSimpleMemberAccessExpressionName() || name.IsMemberBindingExpressionName() || name.IsRightSideOfQualifiedName() || name.IsParentKind(SyntaxKind.NameMemberCref));
            if (name.IsSimpleMemberAccessExpressionName())
            {
                return ((MemberAccessExpressionSyntax)name.Parent).Expression;
            }
            else if (name.IsMemberBindingExpressionName())
            {
                return name.GetParentConditionalAccessExpression().Expression;
            }
            else if (name.IsRightSideOfQualifiedName())
            {
                return ((QualifiedNameSyntax)name.Parent).Left;
            }
            else
            {
                return ((QualifiedCrefSyntax)name.Parent.Parent).Container;
            }
        }

        // Returns true if this looks like a possible type name that is on it's own (i.e. not after
        // a dot).  This function is not exhaustive and additional checks may be added if they are
        // believed to be valuable.
        public static bool LooksLikeStandaloneTypeName(this SimpleNameSyntax simpleName)
        {
            if (simpleName == null)
            {
                return false;
            }

            // Isn't stand-alone if it's on the right of a dot/arrow
            if (simpleName.IsRightSideOfDotOrArrow())
            {
                return false;
            }

            // type names can't be invoked.
            if (simpleName?.Parent is InvocationExpressionSyntax invocation &&
                invocation.Expression == simpleName)
            {
                return false;
            }

            // type names can't be indexed into.
            if (simpleName?.Parent is ElementAccessExpressionSyntax elementAccess &&
                elementAccess.Expression == simpleName)
            {
                return false;
            }

            if (simpleName.Identifier.CouldBeKeyword())
            {
                // Something that looks like a keyword is almost certainly not intended to be a
                // "Standalone type name".  
                //
                // 1. Users are not going to name types the same name as C# keywords (contextual or otherwise).
                // 2. Types in .NET are virtually always start with a Uppercase. While keywords are lowercase)
                //
                // Having a lowercase identifier which matches a c# keyword is enough of a signal 
                // to just not treat this as a standalone type name (even though for some identifiers
                // it could be according to the language).
                return false;
            }

            // Looks good.  However, feel free to add additional checks if this function is too
            // lenient in some circumstances.
            return true;
        }
    }
}
