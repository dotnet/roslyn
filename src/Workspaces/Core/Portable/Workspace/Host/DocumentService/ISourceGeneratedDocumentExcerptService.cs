// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

internal interface ISourceGeneratedDocumentExcerptService : IWorkspaceService
{
    bool CanExcerpt(SourceGeneratedDocument document);

    Task<ExcerptResult?> TryExcerptAsync(SourceGeneratedDocument document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken);
}
