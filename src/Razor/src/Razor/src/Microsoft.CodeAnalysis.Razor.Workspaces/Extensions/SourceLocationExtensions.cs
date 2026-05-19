// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

using RazorSourceLocation = Microsoft.AspNetCore.Razor.Language.SourceLocation;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class SourceLocationExtensions
{
    public static LinePosition ToLinePosition(this RazorSourceLocation location)
        => new(location.LineIndex, location.CharacterIndex);
}
