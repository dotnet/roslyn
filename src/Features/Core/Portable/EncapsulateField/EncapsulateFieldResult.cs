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
        private readonly AsyncLazy<AbstractEncapsulateFieldService.Result> _resultGetter;

        public EncapsulateFieldResult(Func<CancellationToken, Task<AbstractEncapsulateFieldService.Result>> resultGetter)
            => _resultGetter = new AsyncLazy<AbstractEncapsulateFieldService.Result>(c => resultGetter(c), cacheResult: true);

        public async Task<string> GetNameAsync(CancellationToken cancellationToken)
        {
            var result = await _resultGetter.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return result.Name;
        }

        public async Task<Glyph> GetGlyphAsync(CancellationToken cancellationToken)
        {
            var result = await _resultGetter.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return result.Glyph;
        }

        public async Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        {
            var result = await _resultGetter.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return result.Solution;
        }
    }
}
