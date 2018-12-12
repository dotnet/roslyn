// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpAnalysisBuilder
    {
        internal static PullMembersUpAnalysisResult BuildAnalysisResult(
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
                        makeDestinationDeclarationAbstract: false,
                        changeDestinationToAbstract: false);
                }
                else
                {
                    var changeDestinationToAbstract = !destination.IsAbstract && (memberAndMakeAbstract.makeAbstract || memberAndMakeAbstract.member.IsAbstract);
                    return new MemberAnalysisResult(memberAndMakeAbstract.member,
                        changeOriginalToPublic: false,
                        changeOriginalToNonStatic: false,
                        memberAndMakeAbstract.makeAbstract,
                        changeDestinationToAbstract: changeDestinationToAbstract);
                }
            });

            return new PullMembersUpAnalysisResult(destination, membersAnalysisResult);
        }
    }
}
