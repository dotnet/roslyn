// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ITextBufferAssociatedViewService
    {
        IEnumerable<ITextView> GetAssociatedTextViews(ITextBuffer textBuffer);

        event EventHandler<SubjectBuffersConnectedEventArgs> SubjectBuffersConnected;
    }

    internal class SubjectBuffersConnectedEventArgs
    {
        public ReadOnlyCollection<ITextBuffer> SubjectBuffers { get; }
        public ITextView TextView { get; }

        public SubjectBuffersConnectedEventArgs(ITextView textView, ReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            this.TextView = textView;
            this.SubjectBuffers = subjectBuffers;
        }
    }
}
