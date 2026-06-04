// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportWorkspaceService(typeof(ISourceGeneratedDocumentExcerptService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class RazorSourceGeneratedDocumentExcerptServiceWrapper(
    [Import(AllowDefault = true)] Lazy<IRazorSourceGeneratedDocumentExcerptService>? implementation) : ISourceGeneratedDocumentExcerptService
#pragma warning restore RS0030 // Do not use banned APIs
{
    private readonly Lazy<IRazorSourceGeneratedDocumentExcerptService>? _implementation = implementation;

    public bool CanExcerpt(SourceGeneratedDocument document)
        => _implementation is not null && document.IsRazorSourceGeneratedDocument();

    public Task<ExcerptResult?> TryExcerptAsync(SourceGeneratedDocument document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        if (_implementation is null || !document.IsRazorSourceGeneratedDocument())
        {
            return Task.FromResult<ExcerptResult?>(null);
        }

        return _implementation.Value.TryExcerptAsync(document, span, mode, classificationOptions, cancellationToken);
    }
}
