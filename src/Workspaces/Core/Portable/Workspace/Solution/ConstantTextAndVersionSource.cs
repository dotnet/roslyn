// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// This value source keeps a strong reference to a value.
/// </summary>
internal sealed class ConstantTextAndVersionSource : ConstantValueSource<TextAndVersion>, ITextAndVersionSource
{
    public ConstantTextAndVersionSource(TextAndVersion value)
        : base(value)
    {
    }

    public TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken)
        => GetValue(cancellationToken);

    public Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => GetValueAsync(cancellationToken);

    public bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value)
        => TryGetValue(out value);

    public bool TryGetTextVersion(out VersionStamp version)
    {
        version = Value.Version;
        return true;
    }
}
