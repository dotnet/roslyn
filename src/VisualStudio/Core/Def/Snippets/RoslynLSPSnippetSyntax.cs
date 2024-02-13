// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

internal abstract class RoslynLSPSnippetSyntax(int startIndex, int length)
{
    public int End => StartIndex + Length;

    public int StartIndex { get; } = startIndex;

    public int Length { get; } = length;
}
