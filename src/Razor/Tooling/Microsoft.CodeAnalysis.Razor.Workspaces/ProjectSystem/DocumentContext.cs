// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class DocumentContext(Uri uri, IDocumentSnapshot snapshot)
{
    private RazorCodeDocument? _codeDocument;
    private SourceText? _sourceText;

    public Uri Uri { get; } = uri;
    public IDocumentSnapshot Snapshot { get; } = snapshot;

    private bool TryGetCodeDocument([NotNullWhen(true)] out RazorCodeDocument? codeDocument)
    {
        codeDocument = _codeDocument;
        return codeDocument is not null;
    }

    public ValueTask<RazorCodeDocument> GetCodeDocumentAsync(CancellationToken cancellationToken)
    {
        return TryGetCodeDocument(out var codeDocument)
            ? new(codeDocument)
            : GetCodeDocumentCoreAsync(cancellationToken);

        async ValueTask<RazorCodeDocument> GetCodeDocumentCoreAsync(CancellationToken cancellationToken)
        {
            var codeDocument = await Snapshot
                .GetGeneratedOutputAsync(cancellationToken)
                .ConfigureAwait(false);

            // Interlock to ensure that we only ever return one instance of RazorCodeDocument.
            // In race scenarios, when more than one RazorCodeDocument is produced, we want to
            // return whichever RazorCodeDocument is cached.
            return InterlockedOperations.Initialize(ref _codeDocument, codeDocument);
        }
    }

    public ValueTask<SourceText> GetSourceTextAsync(CancellationToken cancellationToken)
    {
        return _sourceText is SourceText sourceText
            ? new(sourceText)
            : GetSourceTextCoreAsync(cancellationToken);

        async ValueTask<SourceText> GetSourceTextCoreAsync(CancellationToken cancellationToken)
        {
            var sourceText = await Snapshot.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Interlock to ensure that we only ever return one instance of RazorCodeDocument.
            // In race scenarios, when more than one RazorCodeDocument is produced, we want to
            // return whichever RazorCodeDocument is cached.
            return InterlockedOperations.Initialize(ref _sourceText, sourceText);
        }
    }

    public ValueTask<RazorSyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        return TryGetCodeDocument(out var codeDocument)
            ? new(GetSyntaxTreeCore(codeDocument))
            : GetSyntaxTreeCoreAsync(cancellationToken);

        static RazorSyntaxTree GetSyntaxTreeCore(RazorCodeDocument codeDocument)
        {
            return codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        }

        async ValueTask<RazorSyntaxTree> GetSyntaxTreeCoreAsync(CancellationToken cancellationToken)
        {
            var codeDocument = await GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
            return GetSyntaxTreeCore(codeDocument);
        }
    }

    public ValueTask<SourceText> GetCSharpSourceTextAsync(CancellationToken cancellationToken)
    {
        return TryGetCodeDocument(out var codeDocument)
            ? new(GetCSharpSourceTextCore(codeDocument))
            : GetCSharpSourceTextCoreAsync(cancellationToken);

        static SourceText GetCSharpSourceTextCore(RazorCodeDocument codeDocument)
        {
            return codeDocument.GetCSharpSourceText();
        }

        async ValueTask<SourceText> GetCSharpSourceTextCoreAsync(CancellationToken cancellationToken)
        {
            var codeDocument = await GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
            return GetCSharpSourceTextCore(codeDocument);
        }
    }
}
