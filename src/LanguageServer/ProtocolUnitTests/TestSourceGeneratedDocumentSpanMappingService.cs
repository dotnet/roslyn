﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[ExportWorkspaceService(typeof(ISourceGeneratedDocumentSpanMappingService))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestSourceGeneratedDocumentSpanMappingService() : ISourceGeneratedDocumentSpanMappingService
{
    public bool DidMapSpans { get; private set; }

    public bool CanMapSpans(SourceGeneratedDocument sourceGeneratedDocument)
    {
        throw new NotImplementedException();
    }

    public Task<ImmutableArray<MappedTextChange>> GetMappedTextChangesAsync(SourceGeneratedDocument oldDocument, SourceGeneratedDocument newDocument, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(SourceGeneratedDocument document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (document.IsRazorSourceGeneratedDocument())
        {
            DidMapSpans = true;
        }

        return Task.FromResult(ImmutableArray<MappedSpanResult>.Empty);
    }
}
