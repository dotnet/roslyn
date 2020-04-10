﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Base class for all components that run outside of the Visual Studio process.
    /// </summary>
    public abstract class OutOfProcComponent
    {
        protected readonly VisualStudioInstance VisualStudioInstance;

        protected OutOfProcComponent(VisualStudioInstance visualStudioInstance)
        {
            VisualStudioInstance = visualStudioInstance;
        }

        internal static TInProcComponent CreateInProcComponent<TInProcComponent>(VisualStudioInstance visualStudioInstance)
            where TInProcComponent : InProcComponent
            => visualStudioInstance.ExecuteInHostProcess<TInProcComponent>(type: typeof(TInProcComponent), methodName: "Create");

        protected void WaitForCompletionSet()
            => VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.CompletionSet);

        protected void WaitForSignatureHelp()
            => VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);

        protected void WaitForQuickInfo()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorSquiggles);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.QuickInfo);
        }
    }
}
