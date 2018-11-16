// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal class PullMembersUpAnalysisBuilder
    {
        internal static AnalysisResult BuildAnalysisResult(
            INamedTypeSymbol targetSymbol,
            IEnumerable<ISymbol> selectedMembersAndOption)
        {
            var memberResult = selectedMembersAndOption.Select(member =>
            {
                if (targetSymbol.TypeKind == TypeKind.Interface)
                {
                    return new MemberAnalysisResult(
                        member, member.DeclaredAccessibility != Accessibility.Public,
                        member.IsStatic);
                }
                else
                {
                    var changeOriginToPublic = false;
                    var changeOriginToNonStatic = false;
                    return new MemberAnalysisResult(member, changeOriginToPublic, changeOriginToNonStatic);
                }
            });

            return new AnalysisResult(targetSymbol, memberResult);
        }
    }

    internal class MemberAnalysisResult
    {
        public readonly ISymbol _member;

        public readonly bool _changeOriginToPublic;

        public readonly bool _changeOriginToNonStatic;

        internal MemberAnalysisResult(ISymbol member, bool changeOriginToPublic, bool changeOriginToNonStatic)
        {
            _member = member;
            _changeOriginToPublic = changeOriginToPublic;
            _changeOriginToNonStatic = changeOriginToNonStatic;
        }
    }

    /// <summary>
    /// This is class contains all the operations to be done on members and target in order to pull members up to target
    /// </summary>
    internal class AnalysisResult
    {
        public readonly INamedTypeSymbol _target;

        public readonly ImmutableArray<MemberAnalysisResult> _membersAnalysisResults;

        public readonly bool _pullUpOperationCauseError;

        internal AnalysisResult(
            INamedTypeSymbol target,
            IEnumerable<MemberAnalysisResult> membersAnalysisResults)
        {
            _target = target;
            _membersAnalysisResults = membersAnalysisResults.ToImmutableArray();
            _pullUpOperationCauseError = _membersAnalysisResults.All(result => result._changeOriginToNonStatic || result._changeOriginToPublic);
        }
    }
}
