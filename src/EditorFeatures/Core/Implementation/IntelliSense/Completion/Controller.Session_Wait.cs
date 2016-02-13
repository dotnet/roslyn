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
            internal Model WaitForModel()
            {
                AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Completion_ModelComputation_WaitForModel, CancellationToken.None))
                {
                    return Computation.ModelTask.WaitAndGetResult(CancellationToken.None);
                }
            }
        }
    }
}
