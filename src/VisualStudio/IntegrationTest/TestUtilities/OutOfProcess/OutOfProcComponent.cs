// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            => VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);

        protected void WaitForSignatureHelp()
            => VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.SignatureHelp);

        protected void WaitForQuickInfo()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.QuickInfo);
        }
    }
}
