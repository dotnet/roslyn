// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal sealed class DocumentationCommentGreyText
{
    public TextSpan SpanToReplace { get; }

    public string SnippetText { get; }

    public string SymbolString { get; }

    internal DocumentationCommentGreyText(TextSpan spanToReplace, string snippetText, string symbolString)
    {
        SpanToReplace = spanToReplace;
        SnippetText = snippetText;
        SymbolString = symbolString;
    }
}
