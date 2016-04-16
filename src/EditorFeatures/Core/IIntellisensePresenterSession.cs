// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IIntelliSensePresenterSession
    {
        void Dismiss();
        event EventHandler<EventArgs> Dismissed;
    }

    internal interface IIntelliSensePresenter<TPresenter, TEditorSessionOpt> where TPresenter : IIntelliSensePresenterSession
    {
        TPresenter CreateSession(ITextView textView, ITextBuffer subjectBuffer, TEditorSessionOpt sessionOpt);
    }
}
