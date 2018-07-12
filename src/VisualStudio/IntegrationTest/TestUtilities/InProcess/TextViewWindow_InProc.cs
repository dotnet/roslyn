// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal abstract class TextViewWindow_InProc : InProcComponent
    {
        protected void ExecuteOnActiveView(Action<IWpfTextView> action)
            => InvokeOnUIThread(GetExecuteOnActionViewCallback(action));

        protected Action GetExecuteOnActionViewCallback(Action<IWpfTextView> action)
            => () =>
            {
                var view = GetActiveTextView();
                action(view);
            };

        protected abstract IWpfTextView GetActiveTextView();
    }
}
