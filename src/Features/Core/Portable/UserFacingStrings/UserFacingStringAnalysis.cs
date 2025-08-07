// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UserFacingStrings;

/// <summary>
/// Contains AI analysis results for a string literal.
/// </summary>
internal sealed record UserFacingStringAnalysis
{
    public double ConfidenceScore { get; }
    public string SuggestedResourceKey { get; }
    public string Reasoning { get; }

    public UserFacingStringAnalysis(double confidenceScore, string suggestedResourceKey, string reasoning)
    {
        ConfidenceScore = confidenceScore;
        SuggestedResourceKey = suggestedResourceKey;
        Reasoning = reasoning;
    }
}
