// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.Completion
{
    internal abstract class SnippetCompletionProvider : CompletionListProvider, ISnippetCompletionProvider, ICustomCommitCompletionProvider
    {
        public abstract void Commit(CompletionItem completionItem, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar);
    }
}
