// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Roslyn.Test.Utilities;

[Export(typeof(IDispatcherTaskJoiner)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WpfDispatcherTaskJoiner() : IDispatcherTaskJoiner
{
    public bool IsDispatcherSynchronizationContext(SynchronizationContext context)
        => context is DispatcherSynchronizationContext;

    public void JoinUsingDispatcher(Task task, CancellationToken cancellationToken)
        => task.JoinUsingDispatcher(cancellationToken);
}
