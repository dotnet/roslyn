// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Debugging;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#endif

internal readonly struct FSharpDebugLocationInfo
{
    internal readonly DebugLocationInfo UnderlyingObject;

    public FSharpDebugLocationInfo(string name, int lineOffset)
        => UnderlyingObject = new DebugLocationInfo(name, lineOffset);

    public readonly string Name => UnderlyingObject.Name;
    public readonly int LineOffset => UnderlyingObject.LineOffset;
    internal bool IsDefault => UnderlyingObject.IsDefault;
}
