// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

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
                => _waitContext = waitContext;

            public void OnCanceled()
                => _waitContext.OnCanceled();
        }
    }
}
