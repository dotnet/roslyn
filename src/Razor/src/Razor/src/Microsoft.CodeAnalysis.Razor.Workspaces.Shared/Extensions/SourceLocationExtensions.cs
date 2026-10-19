// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

using SourceLocation = Microsoft.AspNetCore.Razor.Language.SourceLocation;

internal static class SourceLocationExtensions
{
    public static LinePosition ToLinePosition(this SourceLocation location)
        => new(location.LineIndex, location.CharacterIndex);
}
