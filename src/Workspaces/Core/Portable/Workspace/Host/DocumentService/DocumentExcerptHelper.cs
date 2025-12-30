// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal static class DocumentExcerptHelper
{
    public static bool CanExcerpt(Document document)
    {
        if (document is SourceGeneratedDocument sourceGeneratedDocument &&
            document.Project.Solution.Services.GetService<ISourceGeneratedDocumentExcerptService>() is { } sourceGeneratedExcerptService)
        {
            return sourceGeneratedExcerptService.CanExcerpt(sourceGeneratedDocument);
        }

        return document.DocumentServiceProvider.GetService<IDocumentExcerptService>() is not null;
    }

    public static Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        if (document is SourceGeneratedDocument sourceGeneratedDocument &&
            document.Project.Solution.Services.GetService<ISourceGeneratedDocumentExcerptService>() is { } sourceGeneratedExcerptService)
        {
            return sourceGeneratedExcerptService.TryExcerptAsync(sourceGeneratedDocument, span, mode, classificationOptions, cancellationToken);
        }

        var excerptService = document.DocumentServiceProvider.GetService<IDocumentExcerptService>();
        if (excerptService == null)
        {
            return SpecializedTasks.Default<ExcerptResult?>();
        }

        return excerptService.TryExcerptAsync(document, span, mode, classificationOptions, cancellationToken);
    }
}
