// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal interface IRemoteFixAllProviderService
{
    ValueTask<string> PerformCleanupAsync(
        Checksum solutionChecksum, DocumentId documentId, CodeCleanupOptions codeCleanupOptions, CancellationToken cancellationToken);
}
