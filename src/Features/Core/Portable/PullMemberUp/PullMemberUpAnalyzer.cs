// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpAnalysisBuilder
    {
        private static bool IsAbstractModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return !selectedMember.IsAbstract || targetSymbol.IsAbstract;
        }

        private static bool IsAccessiblityModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return selectedMember.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool IsStaticModifiersMatch(INamedTypeSymbol targetSymbol, ISymbol selectedMember)
        {
            return !selectedMember.IsStatic;
        }

        internal static AnalysisResult BuildAnalysisResult(
            INamedTypeSymbol targetSymbol,
            IEnumerable<(ISymbol member, bool makeAbstract)> selectedMembersAndOption)
        {
            var memberResult = selectedMembersAndOption.Select(selection =>
            {
                if (targetSymbol.TypeKind == TypeKind.Interface)
                {
                    return new MemberAnalysisResult(selection.member, !IsAccessiblityModifiersMatch(targetSymbol, selection.member), !IsStaticModifiersMatch(targetSymbol, selection.member));
                }
                else
                {
                    return new MemberAnalysisResult(selection.member);
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

        public bool ChangeOriginToNonPublic { get; }

        public bool ChangeOriginToNonStatic { get; }

        internal MemberAnalysisResult(ISymbol member, bool changeOriginToNonPublic = false, bool changeOriginToNonStatic = false)
        {
            Member = member;
            ChangeOriginToNonPublic = changeOriginToNonPublic;
            ChangeOriginToNonStatic = changeOriginToNonStatic;
        }
    }

    internal class AnalysisResult
    {
        public bool ChangeTargetAbstract { get; }

        public INamedTypeSymbol Target { get; }

        public IEnumerable<MemberAnalysisResult> MembersAnalysisResults { get; }

        public bool IsValid { get; }

        internal AnalysisResult(
            bool changeTargetAbstract,
            INamedTypeSymbol target,
            IEnumerable<MemberAnalysisResult> membersAnalysisResults)
        {
            ChangeTargetAbstract = changeTargetAbstract;
            Target = target;
            MembersAnalysisResults = membersAnalysisResults;
            IsValid = !MembersAnalysisResults.Aggregate(
                ChangeTargetAbstract,
                (acc, result) => acc || result.ChangeOriginToNonPublic || result.ChangeOriginToNonStatic);
        }
    }
}
