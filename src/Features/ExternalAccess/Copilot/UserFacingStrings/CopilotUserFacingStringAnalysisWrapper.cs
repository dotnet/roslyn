// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// Wrapper for UserFacingStringAnalysis to expose to external Copilot services.
/// </summary>
internal sealed class CopilotUserFacingStringAnalysisWrapper
{
    public CopilotUserFacingStringAnalysisWrapper(double confidenceScore, string suggestedResourceKey, string reasoning)
    {
        ConfidenceScore = confidenceScore;
        SuggestedResourceKey = suggestedResourceKey;
        Reasoning = reasoning;
    }

    /// <summary>
    /// AI confidence score between 0.0 and 1.0 indicating how likely this string is user-facing.
    /// </summary>
    public double ConfidenceScore { get; }

    /// <summary>
    /// AI-suggested resource key name for this string.
    /// </summary>
    public string SuggestedResourceKey { get; }

    /// <summary>
    /// AI reasoning for the confidence score and classification.
    /// </summary>
    public string Reasoning { get; }

    /// <summary>
    /// Converts this wrapper to the internal UserFacingStringAnalysis type.
    /// </summary>
    internal UserFacingStringAnalysis ToInternal()
        => new(ConfidenceScore, SuggestedResourceKey, Reasoning);
}
