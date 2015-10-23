// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal partial class VisualStudioWaitContext
    {
        /// <summary>
        /// Note: this is a COM interface, however it is also free threaded.  This is necessary and
        /// by design so that we can hear about cancellation happening from the wait dialog (which
        /// will happen on the background).
        /// </summary>
        private class Callback : IVsThreadedWaitDialogCallback
        {
            private readonly VisualStudioWaitContext _waitContext;

            public Callback(VisualStudioWaitContext waitContext)
            {
                _waitContext = waitContext;
            }

            public void OnCanceled()
            {
                _waitContext.OnCanceled();
            }
        }
    }
}
