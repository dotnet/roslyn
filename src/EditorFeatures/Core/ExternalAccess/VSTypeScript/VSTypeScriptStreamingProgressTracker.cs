// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

internal sealed class VSTypeScriptStreamingProgressTracker(IStreamingProgressTracker progressTracker) : IVSTypeScriptStreamingProgressTracker
{
    private readonly IStreamingProgressTracker _progressTracker = progressTracker;

    public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
        => _progressTracker.AddItemsAsync(count, cancellationToken);

    public ValueTask ItemCompletedAsync(CancellationToken cancellationToken)
        => _progressTracker.ItemCompletedAsync(cancellationToken);
}
