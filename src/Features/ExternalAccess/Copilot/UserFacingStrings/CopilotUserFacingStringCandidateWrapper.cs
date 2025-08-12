// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// Wrapper for UserFacingStringCandidate to expose to external Copilot services.
/// </summary>
internal sealed class CopilotUserFacingStringCandidateWrapper
{
    private readonly UserFacingStringCandidate _candidate;

    public CopilotUserFacingStringCandidateWrapper(UserFacingStringCandidate candidate)
    {
        _candidate = candidate;
    }

    /// <summary>
    /// The location of the string literal in the source code.
    /// </summary>
    public TextSpan Location => _candidate.Location;

    /// <summary>
    /// The actual string value.
    /// </summary>
    public string Value => _candidate.Value;

    /// <summary>
    /// The context in which the string is used (e.g., method argument, assignment, etc.).
    /// </summary>
    public string Context => _candidate.Context;
}
