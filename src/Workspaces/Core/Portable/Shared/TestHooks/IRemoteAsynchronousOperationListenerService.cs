// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IRemoteAsynchronousOperationListenerService
    {
        ValueTask EnableAsync(bool enable, bool diagnostics, CancellationToken cancellationToken);

        ValueTask<bool> IsCompletedAsync(ImmutableArray<string> featureNames, CancellationToken cancellationToken);

        ValueTask ExpeditedWaitAsync(ImmutableArray<string> featureNames, CancellationToken cancellationToken);
    }
}
