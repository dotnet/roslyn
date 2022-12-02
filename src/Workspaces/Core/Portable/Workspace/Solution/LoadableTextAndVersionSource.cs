// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class LoadableTextAndVersionSource : ITextAndVersionSource
{
    private sealed class LazyValueWithOptions
    {
        public readonly LoadableTextAndVersionSource Source;
        public readonly LoadTextOptions Options;

        private readonly SemaphoreSlim _gate = new(initialCount: 1);
        private TextAndVersion? _instance;
        private WeakReference<TextAndVersion>? _weakInstance;

        public LazyValueWithOptions(LoadableTextAndVersionSource source, LoadTextOptions options)
        {
            Source = source;
            Options = options;
        }

        private Task<TextAndVersion> LoadAsync(CancellationToken cancellationToken)
            => Source.Loader.LoadTextAsync(Options, cancellationToken);

        private TextAndVersion LoadSynchronously(CancellationToken cancellationToken)
            => Source.Loader.LoadTextSynchronously(Options, cancellationToken);

        public bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
        {
            value = _instance;
            if (value != null)
                return true;

            if (_weakInstance != null && _weakInstance.TryGetTarget(out value) && value != null)
                return true;

            return false;
        }

        public TextAndVersion GetValue(CancellationToken cancellationToken)
        {
            if (!TryGetValue(out var textAndVersion))
            {
                using (_gate.DisposableWait(cancellationToken))
                {
                    if (!TryGetValue(out textAndVersion))
                    {
                        textAndVersion = LoadSynchronously(cancellationToken);
                        Save_NoLock(textAndVersion);
                    }
                }
            }

            return textAndVersion;
        }

        public async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken)
        {
            if (!TryGetValue(out var textAndVersion))
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!TryGetValue(out textAndVersion))
                    {
                        textAndVersion = await LoadAsync(cancellationToken).ConfigureAwait(false);
                        Save_NoLock(textAndVersion);
                    }
                }
            }

            return textAndVersion;
        }

        private void Save_NoLock(TextAndVersion textAndVersion)
        {
            Contract.ThrowIfTrue(_gate.CurrentCount != 0);

            _weakInstance ??= new WeakReference<TextAndVersion>(textAndVersion);
            _weakInstance.SetTarget(textAndVersion);

            // if our source wants us to hold on strongly, do so.
            if (this.Source.CacheResult)
                _instance = textAndVersion;
        }
    }

    public readonly TextLoader Loader;
    public readonly bool CacheResult;

    private LazyValueWithOptions? _lazyValue;

    public LoadableTextAndVersionSource(TextLoader loader, bool cacheResult)
    {
        Loader = loader;
        CacheResult = cacheResult;
    }

    public bool CanReloadText
        => Loader.CanReloadText;

    private LazyValueWithOptions GetLazyValue(LoadTextOptions options)
    {
        var lazy = _lazyValue;

        if (lazy == null || lazy.Options != options)
        {
            // drop previous value and replace it with the one that has current options:
            _lazyValue = lazy = new LazyValueWithOptions(this, options);
        }

        return lazy;
    }

    public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
        => GetLazyValue(options).GetValue(cancellationToken);

    public bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value)
        => GetLazyValue(options).TryGetValue(out value);

    public Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => GetLazyValue(options).GetValueAsync(cancellationToken);
}
