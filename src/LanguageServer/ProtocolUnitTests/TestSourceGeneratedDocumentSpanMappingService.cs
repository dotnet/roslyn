// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[ExportWorkspaceService(typeof(ISourceGeneratedDocumentSpanMappingService))]
[Shared]
[PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestSourceGeneratedDocumentSpanMappingService() : ISourceGeneratedDocumentSpanMappingService
{
    public bool DidMapSpans { get; private set; }
    public bool DidMapEdits { get; private set; }
    public bool Enable { get; set; } = true;

    private readonly Dictionary<string, string> _mappedFileNames = [];

    internal void AddMappedFileName(string filePath, string mappedFilePath)
    {
        _mappedFileNames[filePath] = mappedFilePath;
    }

    public bool CanMapSpans(SourceGeneratedDocument sourceGeneratedDocument)
    {
        return Enable && sourceGeneratedDocument.IsRazorSourceGeneratedDocument();
    }

    public async Task<ImmutableArray<MappedTextChange>> GetMappedTextChangesAsync(SourceGeneratedDocument oldDocument, SourceGeneratedDocument newDocument, CancellationToken cancellationToken)
    {
        if (oldDocument.IsRazorSourceGeneratedDocument())
        {
            DidMapEdits = true;

            var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken);
            return changes.SelectAsArray(c => new MappedTextChange(_mappedFileNames[newDocument.FilePath], c));
        }

        return [];
    }

    public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(SourceGeneratedDocument document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (document.IsRazorSourceGeneratedDocument())
        {
            var sourceText = await document.GetTextAsync(cancellationToken);
            DidMapSpans = true;
            return spans.SelectAsArray(s => new MappedSpanResult(document.FilePath, sourceText.Lines.GetLinePositionSpan(s), s));
        }

        return [];
    }
}
