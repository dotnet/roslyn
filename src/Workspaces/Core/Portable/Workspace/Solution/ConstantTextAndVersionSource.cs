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
internal sealed class ConstantTextAndVersionSource(TextAndVersion value) : ITextAndVersionSource
{
    private readonly TextAndVersion _value = value;

    public bool CanReloadText
        => false;

    public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
        => _value;

    public Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => Task.FromResult(_value);

    public bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value)
    {
        value = _value;
        return true;
    }

    public bool TryGetVersion(LoadTextOptions options, out VersionStamp version)
    {
        version = _value.Version;
        return true;
    }

    public ValueTask<VersionStamp> GetVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => new(_value.Version);
}
