// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Debugging;

internal readonly struct DebugLocationInfo
{
    public readonly string Name;
    public readonly int LineOffset;

    public DebugLocationInfo(string name, int lineOffset)
    {
        RoslynDebug.Assert(name != null);
        Name = name;
        LineOffset = lineOffset;
    }

    public bool IsDefault
        => Name == null;
}
