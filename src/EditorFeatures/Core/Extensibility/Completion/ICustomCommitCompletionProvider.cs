// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Interface to implement for a completion provider that wants to provide customized commit
    /// behavior.
    /// </summary>
    internal interface ICustomCommitCompletionProvider
    {
        void Commit(CompletionItem completionItem, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar);
    }
}
