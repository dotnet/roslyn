// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptDebugLocationInfoWrapper(string name, int lineOffset)
{
    internal readonly DebugLocationInfo UnderlyingObject = new(name, lineOffset);

    public readonly string Name => UnderlyingObject.Name;
    public readonly int LineOffset => UnderlyingObject.LineOffset;
    internal bool IsDefault => UnderlyingObject.IsDefault;
}
