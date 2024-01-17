// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class RoslynLSPSnippetPlaceholderSyntax(int tabStopIndex, string placeholder, int startIndex, int length)
    : RoslynLSPSnippetSyntax(startIndex, length)
{
    public int TabStopIndex { get; } = tabStopIndex;

    public string Placeholder { get; } = placeholder;

    private string GetDebuggerDisplay()
        => $"${TabStopIndex}:{Placeholder} - ({StartIndex}, {Length})";
}
