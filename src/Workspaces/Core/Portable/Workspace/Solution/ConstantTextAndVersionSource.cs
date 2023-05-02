// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// This value source keeps a strong reference to a value.
/// </summary>
internal sealed class ConstantTextAndVersionSource : ValueSource<TextAndVersion>, ITextAndVersionSource
{
    private readonly TextAndVersion _value;

    public ConstantTextAndVersionSource(TextAndVersion value)
    {
        _value = value;
    }

    public bool CanReloadText
        => false;

    public override TextAndVersion GetValue(CancellationToken cancellationToken)
        => _value;

    public override Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => Task.FromResult(_value);

    public override bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value)
    {
        value = _value;
        return true;
    }

    public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
        => GetValue(cancellationToken);

    public Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => GetValueAsync(cancellationToken);

    public bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value)
        => TryGetValue(out value);

    public bool TryGetVersion(LoadTextOptions options, out VersionStamp version)
    {
        version = _value.Version;
        return true;
    }

    public ValueTask<VersionStamp> GetVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => new(_value.Version);
}
