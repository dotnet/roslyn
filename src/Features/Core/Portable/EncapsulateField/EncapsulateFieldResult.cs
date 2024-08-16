// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField;

internal class EncapsulateFieldResult(string name, Glyph glyph, Func<CancellationToken, Task<Solution>> getSolutionAsync)
{
    public readonly string Name = name;
    public readonly Glyph Glyph = glyph;
    private readonly AsyncLazy<Solution> _lazySolution = AsyncLazy.Create(getSolutionAsync);

    public Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        => _lazySolution.GetValueAsync(cancellationToken);
}
