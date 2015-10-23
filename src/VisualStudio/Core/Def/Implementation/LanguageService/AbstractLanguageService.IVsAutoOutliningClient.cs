// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService : IVsAutoOutliningClient
    {
        public int QueryWaitForAutoOutliningCallback(out int fWait)
        {
            // Normally, the editor automatically loads outlining information immediately. By saying we want to wait, we get to
            // control the load point during our view setup.
            fWait = 1;
            return VSConstants.S_OK;
        }
    }
}
