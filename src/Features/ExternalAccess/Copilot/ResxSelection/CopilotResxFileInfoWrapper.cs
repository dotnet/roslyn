// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ResxSelection;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// External wrapper for ResxFileInfo to expose to external AI services.
/// </summary>
internal readonly struct CopilotResxFileInfoWrapper
{
    internal ResxFileInfo UnderlyingObject { get; }

    internal CopilotResxFileInfoWrapper(ResxFileInfo underlyingObject)
        => UnderlyingObject = underlyingObject;

    public string FilePath => UnderlyingObject.FilePath;
    public string RelativePathFromDocument => UnderlyingObject.RelativePathFromDocument;
    public string? Namespace => UnderlyingObject.Namespace;
    public DateTime LastModified => UnderlyingObject.LastModified;
    
    public ImmutableArray<CopilotResxEntryWrapper> ExistingEntries
        => UnderlyingObject.ExistingEntries.SelectAsArray(e => new CopilotResxEntryWrapper(e));
}
