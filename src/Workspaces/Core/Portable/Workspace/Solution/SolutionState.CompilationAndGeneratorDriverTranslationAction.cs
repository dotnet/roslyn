﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private abstract partial class CompilationAndGeneratorDriverTranslationAction
        {
            public virtual Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                return Task.FromResult(oldCompilation);
            }
        }
    }
}
