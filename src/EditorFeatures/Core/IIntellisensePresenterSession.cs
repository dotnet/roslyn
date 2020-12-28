// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
