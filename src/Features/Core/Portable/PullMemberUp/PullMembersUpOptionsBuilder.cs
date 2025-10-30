// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

internal sealed class PullMembersUpOptionsBuilder
{
    public static PullMembersUpOptions BuildPullMembersUpOptions(
        INamedTypeSymbol destination,
        ImmutableArray<(ISymbol member, bool makeAbstract)> members)
    {
        var membersAnalysisResult = members.SelectAsArray(memberAndMakeAbstract =>
        {
            if (destination.TypeKind == TypeKind.Interface)
            {
                var changeOriginalToPublic = memberAndMakeAbstract.member.DeclaredAccessibility != Accessibility.Public;
                var changeOriginalToNonStatic = memberAndMakeAbstract.member.IsStatic;
                return new MemberAnalysisResult(
                    memberAndMakeAbstract.member,
                    changeOriginalToPublic,
                    changeOriginalToNonStatic,
                    makeMemberDeclarationAbstract: false,
                    changeDestinationTypeToAbstract: false);
            }
            else
            {
                var changeDestinationToAbstract = !destination.IsAbstract && (memberAndMakeAbstract.makeAbstract || memberAndMakeAbstract.member.IsAbstract);
                return new MemberAnalysisResult(memberAndMakeAbstract.member,
                    changeOriginalToPublic: false,
                    changeOriginalToNonStatic: false,
                    memberAndMakeAbstract.makeAbstract,
                    changeDestinationTypeToAbstract: changeDestinationToAbstract);
            }
        });

        return new PullMembersUpOptions(destination, membersAnalysisResult);
    }
}
