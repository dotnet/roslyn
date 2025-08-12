// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// Wrapper for UserFacingStringProposal to expose to external Copilot services.
/// </summary>
internal sealed class CopilotUserFacingStringProposalWrapper
{
    private readonly UserFacingStringProposal _proposal;
    private readonly ImmutableArray<CopilotUserFacingStringCandidateWrapper> _wrappedCandidates;

    public CopilotUserFacingStringProposalWrapper(UserFacingStringProposal proposal)
    {
        _proposal = proposal;
        _wrappedCandidates = _proposal.Candidates.SelectAsArray(c => new CopilotUserFacingStringCandidateWrapper(c));
    }

    /// <summary>
    /// The source code context where the strings were found.
    /// </summary>
    public string SourceCode => _proposal.SourceCode;

    /// <summary>
    /// All string literal candidates found in the code.
    /// </summary>
    public ImmutableArray<CopilotUserFacingStringCandidateWrapper> Candidates => _wrappedCandidates;
}
