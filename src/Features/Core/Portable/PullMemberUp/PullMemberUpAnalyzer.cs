// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpAnalysisBuilder
    {
        internal static AnalysisResult BuildAnalysisResult(
            INamedTypeSymbol targetSymbol,
            IEnumerable<(ISymbol member, bool makeAbstract)> selectedMembersAndOption)
        {
            var memberResult = selectedMembersAndOption.Select(selection =>
            {
                if (targetSymbol.TypeKind == TypeKind.Interface)
                {
                    return new MemberAnalysisResult(
                        selection.member,
                        changeOriginToPublic: selection.member.DeclaredAccessibility != Accessibility.Public,
                        changeOriginToNonStatic: selection.member.IsStatic);
                }
                else
                {
                    return new MemberAnalysisResult(selection.member, makeAbstract: selection.makeAbstract);
                }
            });

            if (targetSymbol.TypeKind == TypeKind.Interface)
            {
                return new AnalysisResult(false, targetSymbol, memberResult);
            }
            else
            {
                var changeTargetToAbstract = 
                    !targetSymbol.IsAbstract &&
                    selectedMembersAndOption.Aggregate(false, (acc, selection) => acc || selection.member.IsAbstract || selection.makeAbstract);
                return new AnalysisResult(changeTargetToAbstract, targetSymbol, memberResult);
            }
        }
    }

    internal class MemberAnalysisResult
    {
        public ISymbol Member { get; }

        public bool ChangeOriginToPublic { get; }

        public bool ChangeOriginToNonStatic { get; }

        public bool MakeAbstract { get; }

        internal MemberAnalysisResult(ISymbol member, bool changeOriginToPublic = false, bool changeOriginToNonStatic = false, bool makeAbstract = false)
        {
            Member = member;
            ChangeOriginToPublic = changeOriginToPublic;
            ChangeOriginToNonStatic = changeOriginToNonStatic;
            MakeAbstract = makeAbstract;
        }
    }

    /// <summary>
    /// This is class contains all the operations to be done on members and target in order to pull members up to target
    /// </summary>
    internal class AnalysisResult
    {
        public bool ChangeTargetAbstract { get; }

        public INamedTypeSymbol Target { get; }

        public IEnumerable<MemberAnalysisResult> MembersAnalysisResults { get; }

        public bool IsPullUpOperationCauseError { get; }

        internal AnalysisResult(
            bool changeTargetAbstract,
            INamedTypeSymbol target,
            IEnumerable<MemberAnalysisResult> membersAnalysisResults)
        {
            ChangeTargetAbstract = changeTargetAbstract;
            Target = target;
            MembersAnalysisResults = membersAnalysisResults;
            IsPullUpOperationCauseError = !MembersAnalysisResults.Aggregate(
                ChangeTargetAbstract,
                (acc, result) => acc || result.ChangeOriginToPublic || result.ChangeOriginToNonStatic || result.MakeAbstract);
        }
    }
}
