// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
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
        {
            // Create MarshalByRefObject that can be used to execute code in the VS process.
            return visualStudioInstance.ExecuteInHostProcess<TInProcComponent>(
                type: typeof(TInProcComponent),
                methodName: "Create");
        }

        protected void WaitForCompletionSet()
        {
            VisualStudioInstance.VisualStudioWorkspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }

        protected void WaitForSignatureHelp()
        {
            VisualStudioInstance.VisualStudioWorkspace.WaitForAsyncOperations(FeatureAttribute.SignatureHelp);
        }
    }
}
