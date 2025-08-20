// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ResxSelection;

/// <summary>
/// Result of AI-powered .resx file selection with confidence and reasoning.
/// </summary>
internal sealed record ResxFileSelectionResult
{
    public string SelectedResxFilePath { get; }
    public double ConfidenceScore { get; }
    public string Reasoning { get; }
    public string SuggestedResourceKey { get; }
    
    public ResxFileSelectionResult(
        string selectedResxFilePath,
        double confidenceScore,
        string reasoning,
        string suggestedResourceKey)
    {
        SelectedResxFilePath = selectedResxFilePath;
        ConfidenceScore = confidenceScore;
        Reasoning = reasoning;
        SuggestedResourceKey = suggestedResourceKey;
    }
}
