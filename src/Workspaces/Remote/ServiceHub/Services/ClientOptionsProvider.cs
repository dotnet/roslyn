// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class ClientOptionsProvider<TOptions, TCallback>(RemoteCallback<TCallback> callback, RemoteServiceCallbackId callbackId) : OptionsProvider<TOptions>
    where TCallback : class, IRemoteOptionsCallback<TOptions>
{
    private ImmutableDictionary<string, AsyncLazy<TOptions>> _cache = ImmutableDictionary<string, AsyncLazy<TOptions>>.Empty;

    public async ValueTask<TOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
    {
        var lazyOptions = ImmutableInterlocked.GetOrAdd(ref _cache, languageServices.Language, _ => AsyncLazy.Create(GetRemoteOptionsAsync));
        return await lazyOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);

        Task<TOptions> GetRemoteOptionsAsync(CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.GetOptionsAsync(callbackId, languageServices.Language, cancellationToken), cancellationToken).AsTask();
    }
}
