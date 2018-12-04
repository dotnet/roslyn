// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
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
        public readonly ImmutableArray<MemberAnalysisResult> MemberAnalysisResults;

        /// <summary>
        /// Indicate whether it would cause error if we directly pull all members in MemberAnalysisResults up to destination.
        /// </summary>
        public readonly bool PullUpOperationCausesError;

        internal PullMembersUpAnalysisResult(
            INamedTypeSymbol destination,
            ImmutableArray<MemberAnalysisResult> memberAnalysisResults)
        {
            Destination = destination;
            MemberAnalysisResults = memberAnalysisResults;
            PullUpOperationCausesError = MemberAnalysisResults.Any(result => result.PullMemberUpCausesError);
        }
    }
}
