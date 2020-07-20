// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

            /// <summary>
            /// Returns a new <see cref="TrackedGeneratorDriver" /> that can be used for future generator invocations.
            /// </summary>
            public virtual TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
            {
                // Our default behavior is that any edit requires us to re-run a full generation pass, since anything
                // could have changed.
                return new TrackedGeneratorDriver(generatorDriver: null);
            }
        }
    }
}
