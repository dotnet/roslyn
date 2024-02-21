// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

// TODO: Use generic IRemoteOptionsCallback<TOptions> once https://github.com/microsoft/vs-streamjsonrpc/issues/789 is fixed

internal sealed class RemoteOptionsProviderCache<TOptions>
{
    private readonly Func<RemoteServiceCallbackId, string, CancellationToken, ValueTask<TOptions>> _callback;
    private readonly RemoteServiceCallbackId _callbackId;

    private ImmutableDictionary<string, AsyncLazy<TOptions>> _cache = ImmutableDictionary<string, AsyncLazy<TOptions>>.Empty;

    public RemoteOptionsProviderCache(Func<RemoteServiceCallbackId, string, CancellationToken, ValueTask<TOptions>> callback, RemoteServiceCallbackId callbackId)
    {
        _callback = callback;
        _callbackId = callbackId;
    }

    public async ValueTask<TOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
    {
        var lazyOptions = ImmutableInterlocked.GetOrAdd(ref _cache, languageServices.Language, _ => AsyncLazy.Create(GetRemoteOptionsAsync));
        return await lazyOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);

        Task<TOptions> GetRemoteOptionsAsync(CancellationToken cancellationToken)
            => _callback(_callbackId, languageServices.Language, cancellationToken).AsTask();
    }
}

internal sealed class ClientCleanCodeGenerationOptionsProvider : AbstractCleanCodeGenerationOptionsProvider
{
    private readonly RemoteOptionsProviderCache<CleanCodeGenerationOptions> _cache;

    public ClientCleanCodeGenerationOptionsProvider(Func<RemoteServiceCallbackId, string, CancellationToken, ValueTask<CleanCodeGenerationOptions>> callback, RemoteServiceCallbackId callbackId)
        => _cache = new RemoteOptionsProviderCache<CleanCodeGenerationOptions>(callback, callbackId);

    public override ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => _cache.GetOptionsAsync(languageServices, cancellationToken);
}

internal sealed class ClientCodeCleanupOptionsProvider : AbstractCodeCleanupOptionsProvider
{
    private readonly RemoteOptionsProviderCache<CodeCleanupOptions> _cache;

    public ClientCodeCleanupOptionsProvider(Func<RemoteServiceCallbackId, string, CancellationToken, ValueTask<CodeCleanupOptions>> callback, RemoteServiceCallbackId callbackId)
        => _cache = new RemoteOptionsProviderCache<CodeCleanupOptions>(callback, callbackId);

    public override ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => _cache.GetOptionsAsync(languageServices, cancellationToken);
}

