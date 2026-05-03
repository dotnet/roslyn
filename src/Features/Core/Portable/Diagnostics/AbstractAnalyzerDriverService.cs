// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract class AbstractAnalyzerDriverService : IAnalyzerDriverService
{
    public ImmutableArray<DeclarationInfo> ComputeDeclarationsInSpan(
        SemanticModel model, TextSpan span, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<DeclarationInfo>.GetInstance(out var infos);
        ComputeDeclarationsInSpan(model, span, infos, cancellationToken);
        return infos.ToImmutableAndClear();
    }

    protected abstract void ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, ArrayBuilder<DeclarationInfo> infos, CancellationToken cancellationToken);
}
