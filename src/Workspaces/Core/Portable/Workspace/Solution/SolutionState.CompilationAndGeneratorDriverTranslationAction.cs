// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionCompilationState
    {
        /// <summary>
        /// Represents a change that needs to be made to a <see cref="Compilation"/>, <see cref="GeneratorDriver"/>, or both in response to
        /// some user edit.
        /// </summary>
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

            public virtual GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver) => generatorDriver;

            /// <summary>
            /// When changes are made to a solution, we make a list of translation actions. If multiple similar changes happen in rapid
            /// succession, we may be able to merge them without holding onto intermediate state.
            /// </summary>
            /// <param name="priorAction">The action prior to this one. May be a different type.</param>
            /// <returns>A non-null <see cref="CompilationAndGeneratorDriverTranslationAction" /> if we could create a merged one, null otherwise.</returns>
            public virtual CompilationAndGeneratorDriverTranslationAction? TryMergeWithPrior(CompilationAndGeneratorDriverTranslationAction priorAction)
                => null;
        }
    }
}
