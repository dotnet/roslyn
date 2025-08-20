// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ResxSelection;

/// <summary>
/// Request for AI-powered selection of the best .resx file for a string.
/// </summary>
internal sealed record ResxFileSelectionRequest
{
    public string StringToMove { get; }
    public string StringContext { get; }
    public string CurrentDocumentPath { get; }
    public ImmutableArray<ResxFileInfo> AvailableResxFiles { get; }
    public string? SuggestedResourceKey { get; }
    
    public ResxFileSelectionRequest(
        string stringToMove,
        string stringContext, 
        string currentDocumentPath,
        ImmutableArray<ResxFileInfo> availableResxFiles,
        string? suggestedResourceKey = null)
    {
        StringToMove = stringToMove;
        StringContext = stringContext;
        CurrentDocumentPath = currentDocumentPath;
        AvailableResxFiles = availableResxFiles;
        SuggestedResourceKey = suggestedResourceKey;
    }
}
