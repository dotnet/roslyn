// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal class DestinationAnalysisResult
    {
        public INamedTypeSymbol Destination { get; }
        public ImmutableArray<MemberAnalysisResult> MemberAnalysisResults { get; }
        public bool PullUpOperationNeedsToDoExtraChanges { get; }
        public DestinationAnalysisResult(
            INamedTypeSymbol destination,
            ImmutableArray<MemberAnalysisResult> memberAnalysisResults)
        {
            Destination = destination;
            MemberAnalysisResults = memberAnalysisResults;
            PullUpOperationNeedsToDoExtraChanges = MemberAnalysisResults.Any(result => result.MoveMemberNeedsToDoExtraChanges);
        }
    }
}
