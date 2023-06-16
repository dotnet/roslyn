// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
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
    private Task<TInterface?>? _nullableGetter;

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
        // If we already have a cached result for this call, return it immediately. Otherwise, calculate it on first
        // use.
        return _nullableGetter ?? GetValueOrNullSlowAsync(cancellationToken);
    }

    private Task<TInterface?> GetValueOrNullSlowAsync(CancellationToken cancellationToken)
    {
        return _serviceGetter.GetValueAsync(cancellationToken).ContinueWith(
            static (t, state) =>
            {
                var self = (StubVsServiceExporter<TService, TInterface>)state;
                var result = self._nullableGetter;
                if (result is not null)
                    return result;

                switch (t.Status)
                {
                    case TaskStatus.Faulted:
                        // Our caller never wants exceptions
                        result = RoslynUtilities::SpecializedTasks.Null<TInterface>();
                        break;

                    case TaskStatus.Canceled:
                        // In cancellation cases, this will rethrow the OCE, which we want. However, we avoid caching
                        // this result by returning immediately.
                        return RoslynUtilities::SpecializedTasks.AsNullable(t);

                    default:
                        result = RoslynUtilities::SpecializedTasks.AsNullable(t);
                        break;
                }

                result = Interlocked.CompareExchange(ref self._nullableGetter, result, null) ?? result;
                return result;
            },
            state: this,
            CancellationToken.None, // token is already passed to antecedent, and this is a tiny sync continuation, so no need to make it also cancelable.
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }
}
