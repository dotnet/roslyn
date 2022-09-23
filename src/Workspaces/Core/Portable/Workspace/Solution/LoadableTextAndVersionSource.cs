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

internal sealed class LoadableTextAndVersionSource : ValueSource<TextAndVersion>, ITextAndVersionSource
{
    private readonly AsyncLazy<TextAndVersion> _lazy;
    private readonly TextLoader _loader;

    public LoadableTextAndVersionSource(TextLoader loader, bool cacheResult)
    {
        _loader = loader;
        _lazy = new AsyncLazy<TextAndVersion>(LoadAsync, LoadSynchronously, cacheResult);
    }

    private Task<TextAndVersion> LoadAsync(CancellationToken cancellationToken)
        => _loader.LoadTextAsync(cancellationToken);

    private TextAndVersion LoadSynchronously(CancellationToken cancellationToken)
        => _loader.LoadTextSynchronously(cancellationToken);

    public override TextAndVersion GetValue(CancellationToken cancellationToken)
        => _lazy.GetValue(cancellationToken);

    public override bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
        => _lazy.TryGetValue(out value);

    public override Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => _lazy.GetValueAsync(cancellationToken);

    public TextLoader Loader
        => _loader;

    public SourceHashAlgorithm ChecksumAlgorithm
        => _loader.ChecksumAlgorithm;

    public ITextAndVersionSource? TryUpdateChecksumAlgorithm(SourceHashAlgorithm algorithm)
    {
        var newLoader = _loader.TryUpdateChecksumAlgorithm(algorithm);
        if (newLoader == null)
        {
            return null;
        }

        if (newLoader == _loader)
        {
            return this;
        }

        return new LoadableTextAndVersionSource(newLoader, _lazy.CacheResult);
    }
}
