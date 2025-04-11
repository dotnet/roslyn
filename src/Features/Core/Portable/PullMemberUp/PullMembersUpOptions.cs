// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.PullMemberUp;

/// <summary>
/// This class contains all the operations needs to be done on members and destination to complete the pull up operation.
/// If user clicked the cancel button, it will be null.
/// </summary>
internal sealed class PullMembersUpOptions
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
    public readonly bool PullUpOperationNeedsToDoExtraChanges;

    public PullMembersUpOptions(
        INamedTypeSymbol destination,
        ImmutableArray<MemberAnalysisResult> memberAnalysisResults)
    {
        Destination = destination;
        MemberAnalysisResults = memberAnalysisResults;
        PullUpOperationNeedsToDoExtraChanges = MemberAnalysisResults.Any(static result => result.PullMemberUpNeedsToDoExtraChanges);
    }
}
