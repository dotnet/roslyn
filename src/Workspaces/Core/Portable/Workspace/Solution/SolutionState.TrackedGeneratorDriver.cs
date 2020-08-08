// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        // TODO: this mostly exists as a way to track if a GeneratorDriver has been mutated in a way
        // which TryApplyEdit can no longer be used. We should just revise GeneratorDriver to better handle
        // this pattern. https://github.com/dotnet/roslyn/issues/42815 tracks updating an API to allow this.
        public readonly struct TrackedGeneratorDriver
        {
            public TrackedGeneratorDriver(GeneratorDriver? generatorDriver)
            {
                GeneratorDriver = generatorDriver;
                NeedsFullGeneration = true;
            }

            internal TrackedGeneratorDriver(GeneratorDriver generatorDriver, bool needsFullGeneration)
            {
                GeneratorDriver = generatorDriver;
                NeedsFullGeneration = needsFullGeneration;
            }

            public GeneratorDriver? GeneratorDriver { get; }
            public bool NeedsFullGeneration { get; }

            // TODO: re-enable when PendingEdit is public again
            // https://github.com/dotnet/roslyn/issues/46419
#if false
            
            public TrackedGeneratorDriver WithPendingEdit(PendingEdit pendingEdit)
            {
                return WithPendingEdits(ImmutableArray.Create(pendingEdit));
            }

            public TrackedGeneratorDriver WithPendingEdits(ImmutableArray<PendingEdit> pendingEdits)
            {
                if (GeneratorDriver == null)
                {
                    return this;
                }

                // We are able to incrementally update the generator driver then
                return new TrackedGeneratorDriver(
                    GeneratorDriver.WithPendingEdits(pendingEdits),
                    needsFullGeneration: false);
            }

#endif
        }
    }
}
