// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ResxSelection;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// External wrapper for ResxEntry to expose to external AI services.
/// </summary>
internal readonly struct CopilotResxEntryWrapper
{
    internal ResxEntry UnderlyingObject { get; }

    internal CopilotResxEntryWrapper(ResxEntry underlyingObject)
        => UnderlyingObject = underlyingObject;

    public string Key => UnderlyingObject.Key;
    public string Value => UnderlyingObject.Value;
    public string? Comment => UnderlyingObject.Comment;
}
