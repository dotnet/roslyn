// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpAnalysisBuilder
    {
        internal static PullMembersUpAnalysisResult BuildAnalysisResult(
            INamedTypeSymbol destination,
            ImmutableArray<ISymbol> members)
        {
            var membersAnalysisResult = members.SelectAsArray(member =>
            {
                if (destination.TypeKind == TypeKind.Interface)
                {
                    var changeOriginalToPublic = member.DeclaredAccessibility != Accessibility.Public;
                    var changeOriginalToNonStatic = member.IsStatic;
                    return new MemberAnalysisResult(member, changeOriginalToPublic, changeOriginalToNonStatic);
                }
                else
                {
                    return new MemberAnalysisResult(member, changeOriginalToPublic: false, changeOriginalToNonStatic: false);
                }
            });

            return new PullMembersUpAnalysisResult(destination, membersAnalysisResult);
        }
    }
}
