// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            /// <summary>
            /// Should only be called from <see cref="Controller.WaitForModel"/>.
            /// </summary>
            internal Model WaitForModel_DoNotCallDirectly(bool blockForInitialItems)
            {
                AssertIsForeground();

                if (!blockForInitialItems && this.InitialUnfilteredModel == null)
                {
                    // If we don't have our initial completion items, and the caller doesn't want
                    // us to block, then just return nothing here.
                    return null;
                }

                // Otherwise, if we have at least computed items, or our all caller is ok with 
                // blocking, then wait until all work is done.

                using (Logger.LogBlock(FunctionId.Completion_ModelComputation_WaitForModel, CancellationToken.None))
                {
                    return Computation.ModelTask.WaitAndGetResult(CancellationToken.None);
                }
            }
        }
    }
}
