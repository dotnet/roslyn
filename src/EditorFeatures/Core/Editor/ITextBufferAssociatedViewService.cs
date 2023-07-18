﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        event EventHandler<SubjectBuffersConnectedEventArgs> SubjectBuffersDisconnected;
    }

    internal class SubjectBuffersConnectedEventArgs(ITextView textView, ReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        public ReadOnlyCollection<ITextBuffer> SubjectBuffers { get; } = subjectBuffers;
        public ITextView TextView { get; } = textView;
    }
}
