// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal interface IRemoteEmbeddedLanguageBraceMatcherService
    {
        ValueTask<BraceMatchingResult?> FindBracesAsync(
            Checksum solutionInfo, DocumentId documentId, int position, BraceMatchingOptions options, CancellationToken cancellationToken);
    }
}
