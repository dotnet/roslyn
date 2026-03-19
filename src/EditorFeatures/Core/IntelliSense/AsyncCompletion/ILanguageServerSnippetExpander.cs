// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;

internal interface ILanguageServerSnippetExpander
{
    bool TryExpand(string lspSnippetText, SnapshotSpan snapshotSpan, ITextView textView);
}
