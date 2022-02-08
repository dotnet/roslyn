// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
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
        /// Waits for any in-progress Find Reference operations to complete and returns the set of displayed results.
        /// </summary>
        /// <returns>An array of <see cref="Reference"/> items capturing the current contents of the 
        /// Find References window.</returns>
        public Reference[] GetContents()
        {
            // Wait for any pending FindReferences or Implementations operation to complete.
            // Go to Definition/Go to Base are synchronous so we don't need to wait for them
            // (and currently can't, anyway); if they are made asynchronous we will need to wait for
            // them here as well.
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.FindReferences);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.GoToImplementation);

            return _inProc.GetContents();
        }

        public void NavigateTo(Reference reference, bool isPreview, bool shouldActivate)
        {
            _inProc.NavigateTo(reference, isPreview, shouldActivate);
            WaitForNavigate();
        }

        private void WaitForNavigate()
        {
            // Navigation operations handled by Roslyn are tracked by FeatureAttribute.FindReferences
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.FindReferences);

            // Navigation operations handled by the editor are tracked within its own JoinableTaskFactory instance
            VisualStudioInstance.Editor.WaitForEditorOperations(Helper.HangMitigatingTimeout);
        }
    }
}
