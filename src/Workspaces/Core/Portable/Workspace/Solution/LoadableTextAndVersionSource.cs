// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public readonly AsyncLazy<TextAndVersion> LazyValue;
        public readonly LoadTextOptions Options;

        public LazyValueWithOptions(LoadableTextAndVersionSource source, LoadTextOptions options)
        {
            LazyValue = new AsyncLazy<TextAndVersion>(LoadAsync, LoadSynchronously, source.CacheResult);
            Source = source;
            Options = options;
        }

        private Task<TextAndVersion> LoadAsync(CancellationToken cancellationToken)
            => Source.Loader.LoadTextAsync(Options, cancellationToken);

        private TextAndVersion LoadSynchronously(CancellationToken cancellationToken)
            => Source.Loader.LoadTextSynchronously(Options, cancellationToken);
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

    private AsyncLazy<TextAndVersion> GetLazyValue(LoadTextOptions options)
    {
        var lazy = _lazyValue;

        if (lazy == null || lazy.Options != options)
        {
            // drop previous value and replace it with the one that has current options:
            _lazyValue = lazy = new LazyValueWithOptions(this, options);
        }

        return lazy.LazyValue;
    }

    public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
        => GetLazyValue(options).GetValue(cancellationToken);

    public bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value)
        => GetLazyValue(options).TryGetValue(out value);

    public Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => GetLazyValue(options).GetValueAsync(cancellationToken);
}
