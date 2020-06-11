// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal class EncapsulateFieldResult
    {
        public readonly string Name;
        public readonly Glyph Glyph;
        private readonly AsyncLazy<Solution> _lazySolution;

        public EncapsulateFieldResult(string name, Glyph glyph, Func<CancellationToken, Task<Solution>> getSolutionAsync)
        {
            Name = name;
            Glyph = glyph;
            _lazySolution = new AsyncLazy<Solution>(getSolutionAsync, cacheResult: true);
        }

        public Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
            => _lazySolution.GetValueAsync(cancellationToken);
    }
}
