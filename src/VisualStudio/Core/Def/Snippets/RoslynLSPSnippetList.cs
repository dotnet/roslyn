// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

internal sealed class RoslynLSPSnippetList(string input, IReadOnlyList<RoslynLSPSnippetSyntax> children)
    : IReadOnlyList<RoslynLSPSnippetSyntax>
{
    private readonly IReadOnlyList<RoslynLSPSnippetSyntax> _children = children;

    public string Input { get; } = input;

    public int Count => _children.Count;

    public RoslynLSPSnippetSyntax this[int index] => _children[index];

    public IEnumerator<RoslynLSPSnippetSyntax> GetEnumerator()
        => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _children.GetEnumerator();

    public override string ToString()
        => Input;
}
