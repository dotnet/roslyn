// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal interface ITextAndVersionSource
{
    bool TryGetValue([MaybeNullWhen(false)] out TextAndVersion value);
    TextAndVersion GetValue(CancellationToken cancellationToken);
    Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken);

    SourceHashAlgorithm ChecksumAlgorithm { get; }
    ITextAndVersionSource? TryUpdateChecksumAlgorithm(SourceHashAlgorithm algorithm);
}
