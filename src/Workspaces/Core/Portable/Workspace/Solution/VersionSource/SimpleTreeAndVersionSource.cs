// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Simple implementation of <see cref="ITreeAndVersionSource"/> backed by an opaque <see
/// cref="AsyncLazy{TreeAndVersion}"/>."/>
/// </summary>
internal sealed class SimpleTreeAndVersionSource : ITreeAndVersionSource
{
    private readonly AsyncLazy<TreeAndVersion> _source;

    // Only full-parse sources have a tree version that is identical to the initial text version.
    private readonly AsyncLazy<TextAndVersion>? _initialTextSource;

    private SimpleTreeAndVersionSource(AsyncLazy<TreeAndVersion> source, AsyncLazy<TextAndVersion>? initialTextSource = null)
    {
        _source = source;
        _initialTextSource = initialTextSource;
    }

    public Task<TreeAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => _source.GetValueAsync(cancellationToken);

    public TreeAndVersion GetValue(CancellationToken cancellationToken)
        => _source.GetValue(cancellationToken);

    public bool TryGetValue([NotNullWhen(true)] out TreeAndVersion? value)
        => _source.TryGetValue(out value);

    public bool TryGetVersion(out VersionStamp version)
    {
        if (_initialTextSource != null && _initialTextSource.TryGetValue(out var textAndVersion))
        {
            version = textAndVersion.Version;
            return true;
        }

        if (TryGetValue(out var treeAndVersion))
        {
            version = treeAndVersion.Version;
            return true;
        }

        version = default;
        return false;
    }

    public async ValueTask<VersionStamp> GetVersionAsync(CancellationToken cancellationToken)
    {
        // A full parse uses the text's version unchanged. Share the same text snapshot with the eventual parse,
        // rather than parsing just to obtain that version or allowing a reloadable source to select different text.
        if (_initialTextSource != null)
            return (await _initialTextSource.GetValueAsync(cancellationToken).ConfigureAwait(false)).Version;

        return (await GetValueAsync(cancellationToken).ConfigureAwait(false)).Version;
    }

    public static SimpleTreeAndVersionSource Create<TArg>(
        Func<TArg, CancellationToken, Task<TreeAndVersion>> asynchronousComputeFunction,
        Func<TArg, CancellationToken, TreeAndVersion>? synchronousComputeFunction, TArg arg,
        AsyncLazy<TextAndVersion>? initialTextSource = null)
    {
        return new(AsyncLazy<TreeAndVersion>.Create(asynchronousComputeFunction, synchronousComputeFunction, arg), initialTextSource);
    }

    public static SimpleTreeAndVersionSource Create(TreeAndVersion source)
        => new(AsyncLazy.Create(source));
}
