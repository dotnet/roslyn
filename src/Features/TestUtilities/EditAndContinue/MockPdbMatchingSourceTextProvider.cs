// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class MockPdbMatchingSourceTextProvider : IPdbMatchingSourceTextProvider
{
    public Func<string, ImmutableArray<byte>, SourceHashAlgorithm, string?>? TryGetMatchingSourceTextImpl { get; set; }

    public ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        => ValueTask.FromResult(TryGetMatchingSourceTextImpl?.Invoke(filePath, requiredChecksum, checksumAlgorithm));
}
