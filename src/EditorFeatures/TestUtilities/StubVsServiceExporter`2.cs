// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

// Import Roslyn.Utilities with an alias to avoid conflicts with AsyncLazy<T>. This implementation relies on
// AsyncLazy<T> from vs-threading, and not the one from Roslyn.
using RoslynUtilities = Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

[Export(typeof(IVsService<,>))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[PartNotDiscoverable]
internal class StubVsServiceExporter<TService, TInterface> : IVsService<TService, TInterface>
    where TService : class
    where TInterface : class
{
    private readonly AsyncLazy<TInterface> _serviceGetter;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public StubVsServiceExporter(
        [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider2 asyncServiceProvider,
        JoinableTaskContext joinableTaskContext)
    {
        _serviceGetter = new AsyncLazy<TInterface>(() => asyncServiceProvider.GetServiceAsync<TService, TInterface>(joinableTaskContext.Factory, throwOnFailure: true)!, joinableTaskContext.Factory);
    }

    /// <inheritdoc />
    public Task<TInterface> GetValueAsync(CancellationToken cancellationToken)
        => _serviceGetter.GetValueAsync(cancellationToken);

    /// <inheritdoc />
    public Task<TInterface?> GetValueOrNullAsync(CancellationToken cancellationToken)
    {
        var value = GetValueAsync(cancellationToken);
        if (value.IsCompleted)
        {
            return TransformResult(value);
        }

        return value.ContinueWith(
            static t => TransformResult(t),
            CancellationToken.None, // token is already passed to antecedent, and this is a tiny sync continuation, so no need to make it also cancelable.
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();

        static Task<TInterface?> TransformResult(Task<TInterface> task)
        {
            Debug.Assert(task.IsCompleted);
            if (task.Status == TaskStatus.Faulted)
            {
                // Our caller never wants exceptions, so return a cached null value
                return RoslynUtilities::SpecializedTasks.Null<TInterface>();
            }
            else
            {
                // Whether this is cancelled or ran to completion, we return the value as-is
                return RoslynUtilities::SpecializedTasks.AsNullable(task);
            }
        }
    }
}
