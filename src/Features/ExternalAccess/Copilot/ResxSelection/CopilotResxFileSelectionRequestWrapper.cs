// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ResxSelection;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// External wrapper for ResxFileSelectionRequest to expose to external AI services.
/// </summary>
internal readonly struct CopilotResxFileSelectionRequestWrapper
{
    internal ResxFileSelectionRequest UnderlyingObject { get; }

    internal CopilotResxFileSelectionRequestWrapper(ResxFileSelectionRequest underlyingObject)
        => UnderlyingObject = underlyingObject;

    public string StringToMove => UnderlyingObject.StringToMove;
    public string StringContext => UnderlyingObject.StringContext;
    public string CurrentDocumentPath => UnderlyingObject.CurrentDocumentPath;
    public string? SuggestedResourceKey => UnderlyingObject.SuggestedResourceKey;
    
    public ImmutableArray<CopilotResxFileInfoWrapper> AvailableResxFiles
        => UnderlyingObject.AvailableResxFiles.SelectAsArray(f => new CopilotResxFileInfoWrapper(f));
}
