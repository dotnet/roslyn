// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Represents a proposal containing all string literals found in code for AI analysis.
/// </summary>
internal sealed record UserFacingStringProposal
{
    public string SourceCode { get; }
    public ImmutableArray<UserFacingStringCandidate> Candidates { get; }

    public UserFacingStringProposal(string sourceCode, ImmutableArray<UserFacingStringCandidate> candidates)
    {
        SourceCode = sourceCode;
        Candidates = candidates;
    }
}
