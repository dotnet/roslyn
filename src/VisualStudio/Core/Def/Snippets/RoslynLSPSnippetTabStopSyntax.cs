// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class RoslynLSPSnippetTabStopSyntax(int tabStopIndex, int startIndex, int length)
    : RoslynLSPSnippetSyntax(startIndex, length)
{
    public int TabStopIndex { get; } = tabStopIndex;

    private string GetDebuggerDisplay()
        => $"${TabStopIndex} - ({StartIndex}, {Length})";
}
