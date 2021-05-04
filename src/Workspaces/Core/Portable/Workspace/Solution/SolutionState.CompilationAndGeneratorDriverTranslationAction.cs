// Licensed to the .NET Foundation under one or more agreements.
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

            /// <summary>
            /// Whether or not <see cref="TransformCompilationAsync" /> can be called on Compilations that may contain
            /// generated documents.
            /// </summary>
            /// <remarks>
            /// Most translation actions add or remove a single syntax tree which means we can do the "same" change
            /// to a compilation that contains the generated files and one that doesn't; however some translation actions
            /// (like <see cref="ReplaceAllSyntaxTreesAction"/>) will unilaterally remove all trees, and would have unexpected
            /// side effects. This opts those out of operating on ones with generated documents where there would be side effects.
            /// </remarks>
            public abstract bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput { get; }
        }
    }
}
