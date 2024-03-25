// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor;

/// <summary>
/// Interface to implement for a completion provider that wants to provide customized commit
/// behavior.
/// </summary>
internal interface ICustomCommitCompletionProvider
{
    void Commit(CompletionItem completionItem, Document document, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar);
}
