// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class OnDemandProjectLoadOperation(Task completion)
{
    public static OnDemandProjectLoadOperation Completed { get; } = new(Task.CompletedTask);

    public Task WaitAsync(CancellationToken cancellationToken)
        => completion.WithCancellation(cancellationToken);
}
