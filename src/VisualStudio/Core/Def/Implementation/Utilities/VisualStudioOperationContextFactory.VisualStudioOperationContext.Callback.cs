// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal partial class VisualStudioOperationContextFactory
    {
        private partial class VisualStudioOperationContext
        {
            private class Callback : IVsThreadedWaitDialogCallback
            {
                private readonly VisualStudioOperationContext _context;

                public Callback(VisualStudioOperationContext context)
                    => _context = context;

                public void OnCanceled()
                    => _context.OnCanceled();
            }
        }
    }
}
