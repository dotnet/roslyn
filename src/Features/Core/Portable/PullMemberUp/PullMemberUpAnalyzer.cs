// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

    internal readonly struct MemberAnalysisResult
    {
        /// <summary>
        /// The member needs to be pulled up.
        /// </summary>
        public readonly ISymbol Member;

        /// <summary>
        /// Indicate whether this member needs to be changed to public so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToPublic;

        /// <summary>
        /// Indicate whether this member needs to be changed to non-static so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToNonStatic;

        internal MemberAnalysisResult(ISymbol member, bool changeOriginalToPublic, bool changeOriginalToNonStatic)
        {
            Member = member;
            ChangeOriginalToPublic = changeOriginalToPublic;
            ChangeOriginalToNonStatic = changeOriginalToNonStatic;
        }
    }

    /// <summary>
    /// This is struct contains all the operations needs to be done on members and destination to complete the pull up operation.
    /// </summary>
    internal readonly struct PullMembersUpAnalysisResult
    {
        /// <summary>
        /// Destination of where members should be pulled up to.
        /// </summary>
        public readonly INamedTypeSymbol Destination;

        /// <summary>
        /// All the members involved in this pull up operation,
        /// and the other changes (in adddition to pull up) needed so that this pull up operation won't cause error.
        /// </summary>
        public readonly ImmutableArray<MemberAnalysisResult> MembersAnalysisResults;

        /// <summary>
        /// Indicate whether it would cause error if we directly pull the members up to destination.
        /// </summary>
        public readonly bool PullUpOperationCausesError;

        internal PullMembersUpAnalysisResult(
            INamedTypeSymbol destination,
            ImmutableArray<MemberAnalysisResult> membersAnalysisResults)
        {
            Destination = destination;
            MembersAnalysisResults = membersAnalysisResults;
            PullUpOperationCausesError = MembersAnalysisResults.Any(result => result.ChangeOriginalToNonStatic || result.ChangeOriginalToPublic);
        }
    }
}
