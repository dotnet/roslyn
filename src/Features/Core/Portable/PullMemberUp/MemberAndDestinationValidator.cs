// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PullMemberUp;

internal static class MemberAndDestinationValidator
{
    public static bool IsDestinationValid(Solution solution, INamedTypeSymbol destination, CancellationToken cancellationToken)
    {
        // Make sure destination is class or interface since it could be ErrorTypeSymbol
        if (destination.TypeKind is not TypeKind.Interface and not TypeKind.Class)
        {
            return false;
        }

        // Don't provide any refactoring option if the destination is not in source.
        // If the destination is generated code, also don't provide refactoring since we can't make sure if we won't break it.
        var isDestinationInSourceAndNotGeneratedCode =
            destination.Locations.Any(static (location, arg) => location.IsInSource && !arg.solution.GetRequiredDocument(location.SourceTree).IsGeneratedCode(arg.cancellationToken), (solution, cancellationToken));
        return isDestinationInSourceAndNotGeneratedCode;
    }

    public static bool IsMemberValid([NotNullWhen(true)] ISymbol? member)
    {
        if (member is null)
        {
            return false;
        }

        if (member.IsImplicitlyDeclared)
        {
            return false;
        }

        // Static, abstract and accessiblity are not checked here but in PullMembersUpOptionsBuilder.cs since there are
        // two refactoring options provided for pull members up,
        // 1. Quick Action (Only allow members that don't cause error)
        // 2. Dialog box (Allow modifers may cause error and will provide fixing)
        return member switch
        {
            IMethodSymbol { MethodKind: MethodKind.Ordinary } => true,
            IPropertySymbol or IEventSymbol or IFieldSymbol => true,
            _ => false,
        };
    }
}
