// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
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
