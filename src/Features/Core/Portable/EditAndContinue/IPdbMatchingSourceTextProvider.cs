// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface IPdbMatchingSourceTextProvider
{
    // TODO: Return SourceText (https://github.com/dotnet/roslyn/issues/64504) or text changes (if we can maintain baseline)
    ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken);
}

internal sealed class NullPdbMatchingSourceTextProvider : IPdbMatchingSourceTextProvider
{
    public static readonly NullPdbMatchingSourceTextProvider Instance = new();

    private NullPdbMatchingSourceTextProvider()
    {
    }

    public ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        => ValueTask.FromResult<string?>(null);
}
