// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Supports test interaction with the new (in Dev15) Find References/Find Implementation window.
    /// </summary>
    public class FindReferencesWindow_OutOfProc : OutOfProcComponent
    {
        private readonly FindReferencesWindow_InProc _inProc;

        public FindReferencesWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<FindReferencesWindow_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// Returns the set of currently-displayed results.
        /// </summary>
        /// <param name="windowCaption">The name of the window. Generally this will be something like
        /// "'Alpha' references" or "'Beta' implementations".</param>
        /// <returns>An array of <see cref="Reference"/> items capturing the current contents of the 
        /// Find References window.</returns>
        public Reference[] GetContents(string windowCaption)
            => _inProc.GetContents(windowCaption);
    }
}
