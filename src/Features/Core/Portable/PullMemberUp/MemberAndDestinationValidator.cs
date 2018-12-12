// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal static class MemberAndDestinationValidator
    {
        internal static bool IsDestinationValid(INamedTypeSymbol destination, Solution solution, CancellationToken cancellationToken)
        {
            return destination != null &&
                destination.DeclaringSyntaxReferences.Length != 0 &&
                (destination.TypeKind == TypeKind.Interface || destination.TypeKind == TypeKind.Class) &&
                destination.Locations.Any(location => location.IsInSource &&
                !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken));
        }

        internal static bool IsMemeberValid(ISymbol member)
        {
            // Static, abstract and accessiblity are not checked here but in PullMemberUpAnalyzer.cs since there are
            // two refactoring options provided for pull members up,
            // 1. Quick Action (Only allow members that don't cause error)
            // 2. Dialog box (Allow modifers may cause errors and will provide fixing)
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
