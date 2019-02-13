// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpOptionsBuilder
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
}
