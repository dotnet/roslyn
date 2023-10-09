// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeMapper;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// A mapper used for Mapping C# Code from a suggestion onto existing code in the codebase.
/// This is the backbone of previews, as this allows us to know where we should place the preview.
/// </summary>
[ExportLanguageService(typeof(ICodeMapper), language: LanguageNames.CSharp), Shared]
internal sealed partial class CSharpCodeMapper : ICodeMapper
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeMapper(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    private static async Task<DocumentSpan> GetMappingTargetAsync(Document document, ImmutableArray<DocumentSpan> focusLocations, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        foreach (var focusLocation in focusLocations)
        {
            if (document.Id != focusLocation.Document.Id)
                continue;

            if (root.FullSpan.Contains(focusLocation.SourceSpan))
                return focusLocation;
        }

        return new DocumentSpan(document, sourceSpan: root.FullSpan);
    }

    public async Task<ImmutableArray<TextChange>> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<DocumentSpan> focusLocations, bool formatMappedCode, CancellationToken cancellationToken)
    {
        var target = await GetMappingTargetAsync(document, focusLocations, cancellationToken).ConfigureAwait(false);

        if ((await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))?.Options is not CSharpParseOptions options)
            return ImmutableArray<TextChange>.Empty;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var result);
        foreach (var code in contents)
        {
            var mapHelper = await GetMapperHelperAsync(target, code, options, cancellationToken).ConfigureAwait(false);
            if (mapHelper is not null)
            {
                var edits = await mapHelper.MapCodeAsync(cancellationToken).ConfigureAwait(false);
                result.AddRange(edits);
            }
        }

        var changes = result.ToImmutable();
        if (formatMappedCode)
            changes = await GetFormattedChangesAsync(document, changes, cancellationToken).ConfigureAwait(false);

        return changes;
    }

    private static async Task<AbstractMappingHelper?> GetMapperHelperAsync(DocumentSpan target, string code, CSharpParseOptions options, CancellationToken cancellationToken)
    {
        var sourceNodes = await CSharpSourceNode.ExtractSourceNodesAsync(code, options, cancellationToken).ConfigureAwait(false);
        if (sourceNodes.IsEmpty)
            return null;

        Debug.Assert(sourceNodes.Any(sn => sn.Scope is Scope.None) != sourceNodes.Any(sn => sn.Scope is not Scope.None));

        var documentRoot = await target.Document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (sourceNodes.Any(static (sourceNode, syntaxRoot) => sourceNode.ExistsOnTarget(syntaxRoot, out _), arg: documentRoot))
        {
            // Replace does not support more than one node.
            if (sourceNodes.Length > 1)
                return null;

            return new ReplaceHelper(target, sourceNodes);
        }

        return new InsertionHelper(target, sourceNodes);
    }

    private async Task<ImmutableArray<TextChange>> GetFormattedChangesAsync(Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken)
    {
        var cleanupOptions = await document.GetCodeCleanupOptionsAsync(
            _globalOptions.GetCodeCleanupOptions(document.Project.Services, allowImportsInHiddenRegions: null, fallbackOptions: null),
            cancellationToken).ConfigureAwait(false);
        var formattingOptions = cleanupOptions.FormattingOptions;

        var adjustedChanges = await AdjustTextChangesAsync(document, textChanges, formattingOptions.NewLine, cancellationToken).ConfigureAwait(false);
        var spansToFormat = GetChangedSpansInNewText(adjustedChanges);

        var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = oldText.WithChanges(adjustedChanges);
        var newDocument = document.WithText(newText);

        var formattedDocument = await Formatter.FormatAsync(newDocument, spansToFormat, formattingOptions, rules: null, cancellationToken).ConfigureAwait(false);
        var changesWithFormatting = await formattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

        return changesWithFormatting.ToImmutableArray();
    }

    private static ImmutableArray<TextSpan> GetChangedSpansInNewText(ImmutableArray<TextChange> textChanges)
    {
        var currentAdjustment = 0;
        using var _ = ArrayBuilder<TextSpan>.GetInstance(textChanges.Length, out var builder);
        var sortedChanges = textChanges.Sort((x, y) => x.Span.CompareTo(y.Span));

        foreach (var change in sortedChanges)
        {
            builder.Add(new TextSpan(start: change.Span.Start + currentAdjustment, length: change.NewText!.Length));
            var additionalAdjustment = change.NewText!.Length - change.Span.Length;
            currentAdjustment += additionalAdjustment;
        }

        return builder.ToImmutable();
    }

    private static async Task<ImmutableArray<TextChange>> AdjustTextChangesAsync(Document document, ImmutableArray<TextChange> changes, string newlineString, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(changes.Length, out var builder);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var change in changes)
        {
            var adjustedInsertion = TryAdjustNewlines(change.Span, change.NewText!);
            builder.Add(new TextChange(change.Span, adjustedInsertion));
        }

        return builder.ToImmutable();

        string TryAdjustNewlines(TextSpan mapSpan, string mappedText)
        {
            var adjustedText = " " + mappedText.Trim();

            var startLine = text.Lines.GetLineFromPosition(mapSpan.Start);
            var offset = mapSpan.Start - startLine.Start;
            if (startLine.GetFirstNonWhitespaceOffset() is { } firstNonWhitespace && firstNonWhitespace < offset)
            {
                adjustedText = newlineString + adjustedText;
            }

            var endLine = text.Lines.GetLineFromPosition(mapSpan.End);
            offset = mapSpan.End - endLine.Start;
            if (GetFirstNonWhitespacePositionStartFromOffSet(endLine, offset) is { } lastNonWhitespace && lastNonWhitespace >= offset)
            {
                adjustedText = adjustedText + newlineString;
            }

            return adjustedText;
        }

        static int? GetFirstNonWhitespacePositionStartFromOffSet(TextLine line, int offset)
        {
            var text = line.Text;
            if (text != null)
            {
                for (var i = line.Start + offset; i < line.End; i++)
                {
                    if (!char.IsWhiteSpace(text[i]))
                        return i - line.Start;
                }
            }

            return null;
        }
    }
}
