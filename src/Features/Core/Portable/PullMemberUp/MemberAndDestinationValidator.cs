// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal static class MemberAndDestinationValidator
    {
        public static bool IsDestinationValid(Solution solution, INamedTypeSymbol destination, CancellationToken cancellationToken)
        {
            // Make sure destination is class or interface since it could be ErrorTypeSymbol
            if (destination.TypeKind != TypeKind.Interface && destination.TypeKind != TypeKind.Class)
            {
                return false;
            }

            // Don't provide any refactoring option if the destination is not in source.
            // If the destination is generated code, also don't provide refactoring since we can't make sure if we won't break it.
            var isDestinationInSourceAndNotGeneratedCode =
                destination.Locations.Any(location => location.IsInSource && !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken));
            return isDestinationInSourceAndNotGeneratedCode;
        }

        public static bool IsMemberValid(ISymbol member)
        {
            // Static, abstract and accessiblity are not checked here but in PullMembersUpOptionsBuilder.cs since there are
            // two refactoring options provided for pull members up,
            // 1. Quick Action (Only allow members that don't cause error)
            // 2. Dialog box (Allow modifers may cause error and will provide fixing)
            switch (member)
            {
                case IMethodSymbol methodSymbol:
                    return methodSymbol.MethodKind == MethodKind.Ordinary;
                case IFieldSymbol fieldSymbol:
                    return !fieldSymbol.IsImplicitlyDeclared;
                default:
                    return member.IsKind(SymbolKind.Property) || member.IsKind(SymbolKind.Event);
            }
        }
    }
}
