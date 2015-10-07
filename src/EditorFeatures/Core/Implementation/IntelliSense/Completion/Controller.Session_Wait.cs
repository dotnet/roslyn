// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            internal ImmutableArray<Model> WaitForModels()
            {
                AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Completion_ModelComputation_WaitForModel, CancellationToken.None))
                {
                    return Computation.ModelTask.WaitAndGetResult(CancellationToken.None);
                }
            }

            internal Model GetSelectedModel()
            {
                AssertIsForeground();

                WaitForModels();
                if (Computation.ModelTask.Result == null)
                {
                    return null;
                }
                else
                {
                    return Computation.ModelTask.Result.SingleOrDefault(m => m.IsSelected);
                }
            }
        }
    }
}
