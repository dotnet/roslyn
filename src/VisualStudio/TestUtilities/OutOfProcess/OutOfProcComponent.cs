// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    /// <summary>
    /// Base class for all components that run outside of the Visual Studio process.
    /// </summary>
    public abstract class OutOfProcComponent<TInProcComponent>
        where TInProcComponent : InProcComponent
    {
        protected readonly VisualStudioInstance VisualStudioInstance;
        protected readonly TInProcComponent InProc;

        protected OutOfProcComponent(VisualStudioInstance visualStudioInstance)
        {
            VisualStudioInstance = visualStudioInstance;

            InProc = CreateInProcComponent();
        }

        private TInProcComponent CreateInProcComponent()
        {
            // Create MarshalByRefObject that can be used to execute code in the VS process.
            return VisualStudioInstance.ExecuteInHostProcess<TInProcComponent>(
                type: typeof(TInProcComponent),
                methodName: "Create");
        }
    }
}
