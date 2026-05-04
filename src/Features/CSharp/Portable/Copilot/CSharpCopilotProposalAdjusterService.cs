// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportLanguageService(typeof(ICopilotProposalAdjusterService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCopilotProposalAdjusterService(IGlobalOptionService globalOptions)
    : AbstractCopilotProposalAdjusterService(globalOptions)
{
    private const string CS1513 = nameof(CS1513); // } expected

    protected override async Task<Document> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken)
    {
        var newRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var changes = await forkedDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
        if (changes.IsEmpty())
            return forkedDocument;

        // Check if we introduced a missing close-brace error by getting
        // missing closing brace diagnostics that are after the last text change.
        var lastTextChange = changes.Last();
        var newDiagnostics = newRoot.GetDiagnostics();
        var lastChangeEndPos = lastTextChange.Span.End + lastTextChange.NewText?.Length ?? 0;

        var closeBraceDiagnostics = newDiagnostics.WhereAsArray(d => d.Id == CS1513 && d.Location.SourceSpan.Start >= lastChangeEndPos);
        if (closeBraceDiagnostics.IsEmpty)
            return forkedDocument;

        // Insert a close brace at each qualifying diagnostic position
        var insertCloseBraceTextChanges = closeBraceDiagnostics.SelectAsArray(
            d => new TextChange(new TextSpan(d.Location.SourceSpan.Start, 0), "}"));

        // return a new document with the inserted close braces
        var newText = await forkedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var fixedDocument = forkedDocument.WithText(newText.WithChanges(insertCloseBraceTextChanges));
        return fixedDocument;
    }
}
