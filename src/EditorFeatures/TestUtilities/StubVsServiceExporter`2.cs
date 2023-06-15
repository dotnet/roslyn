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
        _nullableGetter ??= _serviceGetter.GetValueAsync(cancellationToken).ContinueWith(
            static t =>
            {
                return t.Status switch
                {
                    // Our caller never wants exceptions
                    TaskStatus.Faulted => null,

                    // In cancellation cases, this will rethrow the OCE, which we want.
                    // Otherwise it will return the result, which they also want.
                    _ => t.GetAwaiter().GetResult(),
                };
            },
            CancellationToken.None, // token is already passed to antecedent, and this is a tiny sync continuation, so no need to make it also cancelable.
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return _nullableGetter;
    }
}
