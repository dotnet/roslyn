// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal interface IRemoteFixAllProviderService
{
    ValueTask<string> PerformCleanupAsync(
        Checksum solutionChecksum, DocumentId documentId, CodeCleanupOptions codeCleanupOptions,
        Dictionary<TextSpan, List<string>> nodeAnnotations, Dictionary<TextSpan, List<string>> tokenAnnotations,
        CancellationToken cancellationToken);
}
