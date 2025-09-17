// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportLanguageService(typeof(ICopilotProposalAdjusterService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCopilotProposalAdjusterService() : AbstractCopilotProposalAdjusterService
{
    private const string CS1513 = nameof(CS1513); // } expected

    protected override async Task<ImmutableArray<TextChange>> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        var root = await originalDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var newText = originalText.WithChanges(normalizedChanges);
        var newDocument = originalDocument.WithText(newText);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // The amount to adjust things when comparing between the original text and the new text.
        var delta = newText.Length - originalText.Length;

        // Check if we introduced a missing close-brace error.
        var lastTextChange = normalizedChanges.Last();

        var lastTextChangePositionInNewText = lastTextChange.Span.End + delta;
        if (lastTextChangePositionInNewText < 0)
            return default;

        var newDiagnostics = newRoot.GetDiagnostics();
        var closeBraceDiagnostics = newDiagnostics.WhereAsArray(
            d => d.Id == CS1513 && d.Location.SourceSpan.Start >= lastTextChangePositionInNewText);
        if (closeBraceDiagnostics.IsEmpty)
            return default;

        var insertCloseBraceTextChanges = closeBraceDiagnostics.SelectAsArray(
            d => new TextChange(new TextSpan(d.Location.SourceSpan.Start - delta, 0), "}"));

        using var _ = ArrayBuilder<TextChange>.GetInstance(normalizedChanges.Length + 1 + insertCloseBraceTextChanges.Length, out var builder);
        builder.AddRange(normalizedChanges);

        // Add in a text edit between the last actual copilot edit and the first close brace edit we're adding. That way
        // we ensure that the code in between those sections is properly formatted (similar to if the user had
        // explicitly typed the close brace themselves.
        if (lastTextChange.Span.End < insertCloseBraceTextChanges.First().Span.Start)
        {
            var interstitialSpan = TextSpan.FromBounds(lastTextChange.Span.End, insertCloseBraceTextChanges.First().Span.Start);
            builder.Add(new TextChange(interstitialSpan, originalText.ToString(interstitialSpan)));
        }

        builder.AddRange(insertCloseBraceTextChanges);

        var finalTextChanges = builder.ToImmutableAndClear();
        return finalTextChanges;
    }
}
