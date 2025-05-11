// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class LoadableTextAndVersionSource(TextLoader loader, bool cacheResult) : ITextAndVersionSource
{
    private sealed class LazyValueWithOptions(LoadableTextAndVersionSource source, LoadTextOptions options)
    {
        public readonly LoadableTextAndVersionSource Source = source;
        public readonly LoadTextOptions Options = options;

        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Strong reference to the loaded text and version.  Only held onto once computed if <see cref="Source"/>.<see
        /// cref="CacheResult"/> is <see langword="true"/>.  Once held onto, this will be returned from all calls to
        /// <see cref="TryGetValue"/>, <see cref="GetValue"/> or <see cref="GetValueAsync"/>.  Once non-null will always
        /// remain non-null.
        /// </summary>
        private TextAndVersion? _instance;

        /// <summary>
        /// Weak reference to the loaded text and version that we create whenever the value is computed.  We will
        /// attempt to return from this if still alive when clients call back into this.  If neither this, nor <see
        /// cref="_instance"/> are available, the value will be reloaded.  Once non-null, this will always be non-null.
        /// </summary>
        private WeakReference<TextAndVersion>? _weakInstance;

        private Task<TextAndVersion> LoadAsync(CancellationToken cancellationToken)
            => Source.TextLoader.LoadTextAsync(Options, cancellationToken);

        private TextAndVersion LoadSynchronously(CancellationToken cancellationToken)
            => Source.TextLoader.LoadTextSynchronously(Options, cancellationToken);

        public bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
        {
            value = _instance;
            if (value != null)
                return true;

            return _weakInstance?.TryGetTarget(out value) == true && value != null;
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
                        UpdateWeakAndStrongReferences_NoLock(textAndVersion);
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
                        UpdateWeakAndStrongReferences_NoLock(textAndVersion);
                    }
                }
            }

            return textAndVersion;
        }

        private void UpdateWeakAndStrongReferences_NoLock(TextAndVersion textAndVersion)
        {
            Contract.ThrowIfTrue(_gate.CurrentCount != 0);

            if (this.Source.CacheResult)
            {
                // if our source wants us to hold onto the value strongly, do so.  No need to involve the weak-refs as
                // this will now hold onto the value forever.
                _instance = textAndVersion;
            }
            else
            {
                // Update the weak ref, so we can return the same instance if anything else is holding onto it.
                _weakInstance ??= new WeakReference<TextAndVersion>(textAndVersion);
                _weakInstance.SetTarget(textAndVersion);
            }
        }
    }

    public TextLoader TextLoader { get; } = loader;
    public readonly bool CacheResult = cacheResult;

    private LazyValueWithOptions? _lazyValue;

    public bool CanReloadText
        => TextLoader.CanReloadText;

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

    public bool TryGetVersion(LoadTextOptions options, out VersionStamp version)
    {
        if (!TryGetValue(options, out var value))
        {
            version = default;
            return false;
        }

        version = value.Version;
        return true;
    }

    public async ValueTask<VersionStamp> GetVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        var value = await GetValueAsync(options, cancellationToken).ConfigureAwait(false);
        return value.Version;
    }
}
