// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
/// Gets edits in C# files, and returns edits to Razor files, with nicely formatted Html
/// </summary>
internal sealed class CSharpOnTypeFormattingPass(
    IDocumentMappingService documentMappingService,
    IRazorEditService razorEditService,
    IHostServicesProvider hostServicesProvider,
    ILoggerFactory loggerFactory) : IFormattingPass
{
    private readonly IDocumentMappingService _documentMappingSerivce = documentMappingService;
    private readonly IRazorEditService _razorEditService = razorEditService;
    private readonly IHostServicesProvider _hostServicesProvider = hostServicesProvider;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CSharpOnTypeFormattingPass>();

    public async Task<ImmutableArray<TextChange>> ExecuteAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        using var roslynWorkspaceHelper = new RoslynWorkspaceHelper(_hostServicesProvider);

        // Normalize and re-map the C# edits.
        var codeDocument = context.CodeDocument;
        var csharpText = codeDocument.GetCSharpSourceText();

        if (changes.Length == 0)
        {
            if (!_documentMappingSerivce.TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(), context.HostDocumentIndex, out _, out var projectedIndex))
            {
                _logger.LogWarning($"Failed to map to projected position for document {context.OriginalSnapshot.FilePath}.");
                return [];
            }

            // Ask C# for formatting changes.
            var autoFormattingOptions = new RazorAutoFormattingOptions(
                formatOnReturn: true, formatOnTyping: true, formatOnSemicolon: true, formatOnCloseBrace: true);

            var formattingChanges = await RazorCSharpFormattingInteractionService.GetFormattingChangesAsync(
                roslynWorkspaceHelper.CreateCSharpDocument(context.CodeDocument),
                typedChar: context.TriggerCharacter,
                projectedIndex,
                context.Options.ToIndentationOptions(),
                autoFormattingOptions,
                indentStyle: CodeAnalysis.Formatting.FormattingOptions.IndentStyle.Smart,
                context.Options.CSharpSyntaxFormattingOptions,
                cancellationToken).ConfigureAwait(false);

            if (formattingChanges.IsEmpty)
            {
                _logger.LogInformation($"Received no results.");
                return [];
            }

            changes = formattingChanges;
            _logger.LogInformation($"Received {changes.Length} results from C#.");
        }

        // Sometimes the C# document is out of sync with our document, so Roslyn can return edits to us that will throw when we try
        // to normalize them. Instead of having this flow up and log a NFW, we just capture it here. Since this only happens when typing
        // very quickly, it is a safe assumption that we'll get another chance to do on type formatting, since we know the user is typing.
        // The proper fix for this is https://github.com/dotnet/razor-tooling/issues/6650 at which point this can be removed
        foreach (var edit in changes)
        {
            var startPos = edit.Span.Start;
            var endPos = edit.Span.End;
            var count = csharpText.Length;
            if (startPos > count || endPos > count)
            {
                _logger.LogWarning($"Got a bad edit that couldn't be applied. Edit is {startPos}-{endPos} but there are only {count} characters in C#.");
                return [];
            }
        }

        context.Logger?.LogSourceText("OriginalCSharp", csharpText);

        var normalizedChanges = csharpText.MinimizeTextChanges(changes, out var originalTextWithChanges);

        context.Logger?.LogSourceText("FormattedCSharp", originalTextWithChanges);

        var mappedChanges = await _razorEditService.MapCSharpEditsAsync(
            normalizedChanges.SelectAsArray(static c => c.ToRazorTextChange()),
            context.CurrentSnapshot,
            context.IncludeCSharpLanguageFeatureEdits,
            cancellationToken).ConfigureAwait(false);

        var filteredChanges = FilterCSharpTextChanges(context, mappedChanges.SelectAsArray(static e => e.ToTextChange()));
        if (filteredChanges.Length == 0)
        {
            return [];
        }

        // Find the lines that were affected by these edits.
        var originalText = codeDocument.Source.Text;
        context.Logger?.LogSourceText("OriginalRazor", originalText);

        context.Logger?.LogMessage($"Source Mappings:\r\n{RenderSourceMappings(context.CodeDocument)}");

        // Apply the format on type edits sent over by the client.
        var formattedText = ApplyChangesAndTrackChange(originalText, filteredChanges, out _, out var spanAfterFormatting);
        context.Logger?.LogSourceText("AfterCSharpChanges", formattedText);

        var changedContext = await context.WithTextAsync(formattedText, cancellationToken).ConfigureAwait(false);
        var linePositionSpanAfterFormatting = formattedText.GetLinePositionSpan(spanAfterFormatting);

        cancellationToken.ThrowIfCancellationRequested();

        // We make an optimistic attempt at fixing corner cases.
        var cleanupChanges = CleanupDocument(changedContext, linePositionSpanAfterFormatting);
        var cleanedText = formattedText;

        if (!cleanupChanges.IsEmpty)
        {
            cleanedText = formattedText.WithChanges(cleanupChanges);
            context.Logger?.LogSourceText("AfterCleanupDocument", cleanedText);

            changedContext = await changedContext.WithTextAsync(cleanedText, cancellationToken).ConfigureAwait(false);
        }

        // At this point we should have applied all edits that adds/removes newlines.
        // Let's now ensure the indentation of each of those lines is correct.

        // We only want to adjust the range that was affected.
        // We need to take into account the lines affected by formatting as well as cleanup.
        var lineDelta = LineDelta(formattedText, cleanupChanges, out var firstLine, out var lastLine);

        // Okay hear me out, I know this looks lazy, but it totally makes sense.
        // This method is called with edits that the C# formatter wants to make, and from those edits we work out which
        // other edits to apply etc. Fine, all good so far. BUT its totally possible that the user typed a closing brace
        // in the same position as the C# formatter thought it should be, on the line _after_ the code that the C# formatter
        // reformatted.
        //
        // For example, given:
        // if (true){
        //     }
        //
        // If the C# formatter is happy with the placement of that close brace then this method will get two edits:
        //  * On line 1 to indent the if by 4 spaces
        //  * On line 1 to add a newline and 4 spaces in front of the opening brace
        //
        // We'll happy format lines 1 and 2, and ignore the closing brace altogether. So, by looking one line further
        // we won't have that problem.
        if (linePositionSpanAfterFormatting.End.Line + lineDelta < cleanedText.Lines.Count - 1)
        {
            lineDelta++;
        }

        // Now we know how many lines were affected by the cleanup and formatting, but we don't know where those lines are. For example, given:
        //
        // @if (true)
        // {
        //      }
        // else
        // {
        // $$}
        //
        // When typing that close brace, the changes would fix the previous close brace, but the line delta would be 0, so
        // we'd format line 6 and call it a day, even though the formatter made an edit on line 3. To fix this we use the
        // first and last position of edits made above, and make sure our range encompasses them as well. For convenience
        // we calculate these positions in the LineDelta method called above.
        var startLine = Math.Min(firstLine, linePositionSpanAfterFormatting.Start.Line);
        var endLineInclusive = Math.Max(lastLine, linePositionSpanAfterFormatting.End.Line + lineDelta);

        Debug.Assert(cleanedText.Lines.Count > endLineInclusive, "Invalid range. This is unexpected.");

        var indentationChanges = await AdjustIndentationAsync(changedContext, startLine, endLineInclusive, roslynWorkspaceHelper.HostWorkspaceServices, _logger, cancellationToken).ConfigureAwait(false);
        if (indentationChanges.Length > 0)
        {
            // Apply the edits that modify indentation.
            cleanedText = cleanedText.WithChanges(indentationChanges);

            context.Logger?.LogSourceText("AfterAdjustIndentationAsync", cleanedText);
        }

        // Now that we have made all the necessary changes to the document. Let's diff the original vs final version and return the diff.
        return SourceTextDiffer.GetMinimalTextChanges(originalText, cleanedText, DiffKind.Char);
    }

    // Returns the minimal TextSpan that encompasses all the differences between the old and the new text.
    private static SourceText ApplyChangesAndTrackChange(SourceText oldText, ImmutableArray<TextChange> changes, out TextSpan spanBeforeChange, out TextSpan spanAfterChange)
    {
        var newText = oldText.WithChanges(changes);
        var affectedRange = newText.GetEncompassingTextChangeRange(oldText);

        spanBeforeChange = affectedRange.Span;
        spanAfterChange = new TextSpan(spanBeforeChange.Start, affectedRange.NewLength);

        return newText;
    }

    private static ImmutableArray<TextChange> FilterCSharpTextChanges(FormattingContext context, ImmutableArray<TextChange> changes)
    {
        var indent = context.GetIndentationLevelString(1);

        using var filteredChanges = new PooledArrayBuilder<TextChange>();

        foreach (var change in changes)
        {
            if (!ShouldFormat(context, change.Span, allowImplicitStatements: false))
            {
                continue;
            }

            // One extra bit of filtering we do here, is to guard against quirks in runtime code-gen, where source mappings
            // end after whitespace, rather than design time where they end before. This results in the C# formatter wanting
            // to insert an indent in what ends up being the middle of a line of Razor code. Since there is no reason to ever
            // insert anything but a single space in the middle of a line, it's easy to filter them out.
            if (change.Span.Length == 0 &&
                change.NewText == indent)
            {
                var linePosition = context.SourceText.GetLinePosition(change.Span.Start);
                var first = context.SourceText.Lines[linePosition.Line].GetFirstNonWhitespaceOffset();
                if (linePosition.Character > first)
                {
                    continue;
                }
            }

            filteredChanges.Add(change);
        }

        return filteredChanges.ToImmutable();
    }

    private static int LineDelta(SourceText text, IEnumerable<TextChange> changes, out int firstLine, out int lastLine)
    {
        firstLine = int.MaxValue;
        lastLine = 0;

        // Let's compute the number of newlines added/removed by the incoming changes.
        var delta = 0;

        foreach (var change in changes)
        {
            var newLineCount = change.NewText is null ? 0 : change.NewText.Split('\n').Length - 1;

            // For convenience, since we're already iterating through things, we also find the extremes
            // of the range of edits that were made.
            var range = text.GetLinePositionSpan(change.Span);
            firstLine = Math.Min(firstLine, range.Start.Line);
            lastLine = Math.Max(lastLine, range.End.Line);

            // The number of lines added/removed will be,
            // the number of lines added by the change  - the number of lines the change span represents
            delta += newLineCount - (range.End.Line - range.Start.Line);
        }

        return delta;
    }

    private static ImmutableArray<TextChange> CleanupDocument(FormattingContext context, LinePositionSpan spanAfterFormatting)
    {
        var text = context.SourceText;
        var csharpDocument = context.CodeDocument.GetRequiredCSharpDocument();

        using var changes = new PooledArrayBuilder<TextChange>();
        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var mappingSpan = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
            var mappingLinePositionSpan = text.GetLinePositionSpan(mappingSpan);
            if (!spanAfterFormatting.LineOverlapsWith(mappingLinePositionSpan))
            {
                if (mappingLinePositionSpan.Start > spanAfterFormatting.End)
                {
                    // This span (and all following) are after the area we're interested in
                    break;
                }

                // We don't care about this range. It didn't change.
                continue;
            }

            CleanupSourceMappingStart(context, mappingLinePositionSpan, ref changes.AsRef(), out var newLineAdded);

            CleanupSourceMappingEnd(context, mappingLinePositionSpan, ref changes.AsRef(), newLineAdded);
        }

        return changes.ToImmutable();
    }

    private static void CleanupSourceMappingStart(FormattingContext context, LinePositionSpan sourceMappingRange, ref PooledArrayBuilder<TextChange> changes, out bool newLineAdded)
    {
        newLineAdded = false;

        //
        // We look through every source mapping that intersects with the affected range and
        // bring the first line to its own line and adjust its indentation,
        //
        // E.g,
        //
        // @{   public int x = 0;
        // }
        //
        // becomes,
        //
        // @{
        //    public int x  = 0;
        // }
        //

        var text = context.SourceText;
        var sourceMappingSpan = text.GetTextSpan(sourceMappingRange);
        if (!ShouldFormat(context,
            sourceMappingSpan,
            new ShouldFormatOptions(
                AllowImplicitStatements: false,
                AllowImplicitExpressions: false,
                AllowSingleLineExplicitExpressions: true,
                IsLineRequest: false),
            out var owner))
        {
            // We don't want to run cleanup on this range.
            return;
        }

        if (owner is CSharpStatementLiteralSyntax literal &&
            literal.TryGetPreviousSibling(out var prevNode) &&
            prevNode.FirstAncestorOrSelf<CSharpTemplateBlockSyntax>() is { } template &&
            owner.SpanStart == template.Span.End &&
            IsOnSingleLine(template, text))
        {
            // Special case, we don't want to add a line break after a single line template
            return;
        }

        // Parent.Parent.Parent is because the tree is
        //  ExplicitExpression -> ExplicitExpressionBody -> CSharpCodeBlock -> CSharpExpressionLiteral
        if (owner is CSharpExpressionLiteralSyntax { Parent.Parent.Parent: CSharpExplicitExpressionSyntax explicitExpression } &&
            IsOnSingleLine(explicitExpression, text))
        {
            // Special case, we don't want to add line breaks inside a single line explicit expression (ie @( ... ))
            return;
        }

        if (sourceMappingRange.Start.Character == 0)
        {
            // It already starts on a fresh new line which doesn't need cleanup.
            // E.g, (The mapping starts at | in the below case)
            // @{
            //     @: Some html
            // |   var x = 123;
            // }
            //

            return;
        }

        // @{
        //     if (true)
        //     {
        //         <div></div>|
        //
        //              |}
        // }
        // We want to return the length of the range marked by |...|
        //
        if (!text.TryGetFirstNonWhitespaceOffset(sourceMappingSpan, out var whitespaceLength, out var newLineCount))
        {
            // There was no content after the start of this mapping. Meaning it already is clean.
            // E.g,
            // @{|
            //    ...
            // }

            return;
        }

        var spanToReplace = new TextSpan(sourceMappingSpan.Start, whitespaceLength);
        if (!context.TryGetIndentationLevel(spanToReplace.End, out var contentIndentLevel))
        {
            // Can't find the correct indentation for this content. Leave it alone.
            return;
        }

        if (newLineCount == 0)
        {
            newLineAdded = true;
            newLineCount = 1;
        }

        // At this point, `contentIndentLevel` should contain the correct indentation level for `}` in the above example.
        // Make sure to preserve the same number of blank lines as the original string had
        var replacement = PrependLines(context.GetIndentationLevelString(contentIndentLevel), context.NewLineString, newLineCount);

        // After the below change the above example should look like,
        // @{
        //     if (true)
        //     {
        //         <div></div>
        //     }
        // }
        var change = new TextChange(spanToReplace, replacement);
        changes.Add(change);
    }

    private static string PrependLines(string text, string newLine, int count)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.SetCapacityIfLarger((newLine.Length * count) + text.Length);

        for (var i = 0; i < count; i++)
        {
            builder.Append(newLine);
        }

        builder.Append(text);
        return builder.ToString();
    }

    private static void CleanupSourceMappingEnd(FormattingContext context, LinePositionSpan sourceMappingRange, ref PooledArrayBuilder<TextChange> changes, bool newLineWasAddedAtStart)
    {
        //
        // We look through every source mapping that intersects with the affected range and
        // bring the content after the last line to its own line and adjust its indentation,
        //
        // E.g,
        //
        // @{
        //     if (true)
        //     {  <div></div>
        //     }
        // }
        //
        // becomes,
        //
        // @{
        //    if (true)
        //    {
        //        </div></div>
        //    }
        // }
        //

        var text = context.SourceText;
        var sourceMappingSpan = text.GetTextSpan(sourceMappingRange);
        var mappingEndLineIndex = sourceMappingRange.End.Line;

        var indentations = context.GetIndentations();

        var startsInCSharpContext = indentations[mappingEndLineIndex].StartsInCSharpContext;

        // If the span is on a single line, and we added a line, then end point is now on a line that does start in a C# context.
        if (!startsInCSharpContext && newLineWasAddedAtStart && sourceMappingRange.Start.Line == mappingEndLineIndex)
        {
            startsInCSharpContext = true;
        }

        if (!startsInCSharpContext)
        {
            // For corner cases like (Position marked with |),
            // It is already in a separate line. It doesn't need cleaning up.
            // @{
            //     if (true}
            //     {
            //         |<div></div>
            //     }
            // }
            //
            return;
        }

        var endSpan = TextSpan.FromBounds(sourceMappingSpan.End, sourceMappingSpan.End);
        if (!ShouldFormat(context, endSpan, allowImplicitStatements: false, out var owner))
        {
            // We don't want to run cleanup on this range.
            return;
        }

        if (owner is CSharpStatementLiteralSyntax &&
            owner.NextSpan() is { } nextSpan &&
            nextSpan.AsNode().AssumeNotNull().FirstAncestorOrSelf<CSharpTemplateBlockSyntax>() is { } template &&
            template.SpanStart == owner.Span.End &&
            IsOnSingleLine(template, text))
        {
            // Special case, we don't want to add a line break in front of a single line template
            return;
        }

        if (owner is MarkupTagHelperAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
            MarkupTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
            MarkupMinimizedTagHelperAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
            MarkupMinimizedTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true })
        {
            // Special case, we don't want to add a line break at the end of a component attribute. They are technically
            // C#, for features like GTD and FAR, but we consider them Html for formatting
            return;
        }

        var contentStartOffset = text.Lines[mappingEndLineIndex].GetFirstNonWhitespaceOffset(sourceMappingRange.End.Character);
        if (contentStartOffset is null)
        {
            // There is no content after the end of this source mapping. No need to clean up.
            return;
        }

        var spanToReplace = new TextSpan(sourceMappingSpan.End, 0);
        if (!context.TryGetIndentationLevel(spanToReplace.End, out var contentIndentLevel))
        {
            // Can't find the correct indentation for this content. Leave it alone.
            return;
        }

        // At this point, `contentIndentLevel` should contain the correct indentation level for `}` in the above example.
        var replacement = context.NewLineString + context.GetIndentationLevelString(contentIndentLevel);

        // After the below change the above example should look like,
        // @{
        //     if (true)
        //     {
        //         <div></div>
        //     }
        // }
        var change = new TextChange(spanToReplace, replacement);
        changes.Add(change);
    }

    private static bool IsOnSingleLine(RazorSyntaxNode node, SourceText text)
    {
        var linePositionSpan = text.GetLinePositionSpan(node.Span);

        return linePositionSpan.Start.Line == linePositionSpan.End.Line;
    }

    private async Task<ImmutableArray<TextChange>> AdjustIndentationAsync(FormattingContext context, int startLine, int endLineInclusive, HostWorkspaceServices hostWorkspaceServices, ILogger logger, CancellationToken cancellationToken)
    {
        // In this method, the goal is to make final adjustments to the indentation of each line.
        // We will take into account the following,
        // 1. The indentation due to nested C# structures
        // 2. The indentation due to Razor and HTML constructs

        var text = context.SourceText;
        var csharpDocument = context.CodeDocument.GetRequiredCSharpDocument();

        // To help with figuring out the correct indentation, first we will need the indentation
        // that the C# formatter wants to apply in the following locations,
        // 1. The start and end of each of our source mappings
        // 2. The start of every line that starts in C# context

        // Due to perf concerns, we only want to invoke the real C# formatter once.
        // So, let's collect all the significant locations that we want to obtain the CSharpDesiredIndentations for.

        using var _1 = HashSetPool<int>.GetPooledObject(out var significantLocations);

        // First, collect all the locations at the beginning and end of each source mapping.
        var sourceMappingMap = new Dictionary<int, int>();
        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var mappingSpan = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
#if DEBUG
            var spanText = context.SourceText.ToString(mappingSpan);
#endif

            var options = new ShouldFormatOptions(
                // Implicit expressions and single line explicit expressions don't affect the indentation of anything
                // under them, so we don't want their positions to be "significant".
                AllowImplicitExpressions: false,
                AllowSingleLineExplicitExpressions: false,

                // Implicit statements are @if, @foreach etc. so they do affect indentation
                AllowImplicitStatements: true,

                IsLineRequest: false);

            if (!ShouldFormat(context, mappingSpan, options, out var owner))
            {
                // We don't care about this range as this can potentially lead to incorrect scopes.
                continue;
            }

            var originalStartLocation = mapping.OriginalSpan.AbsoluteIndex;
            var projectedStartLocation = mapping.GeneratedSpan.AbsoluteIndex;
            sourceMappingMap[originalStartLocation] = projectedStartLocation;
            significantLocations.Add(projectedStartLocation);

            var originalEndLocation = mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length + 1;
            var projectedEndLocation = mapping.GeneratedSpan.AbsoluteIndex + mapping.GeneratedSpan.Length + 1;
            sourceMappingMap[originalEndLocation] = projectedEndLocation;
            significantLocations.Add(projectedEndLocation);
        }

        // Next, collect all the line starts that start in C# context
        var indentations = context.GetIndentations();
        var lineStartMap = new Dictionary<int, int>();
        for (var i = startLine; i <= endLineInclusive; i++)
        {
            if (indentations[i].EmptyOrWhitespaceLine)
            {
                // We should remove whitespace on empty lines.
                continue;
            }

            var line = context.SourceText.Lines[i];
            var lineStart = line.GetFirstNonWhitespacePosition() ?? line.Start;

            var lineStartSpan = new TextSpan(lineStart, 0);
            if (!ShouldFormat(context, lineStartSpan, allowImplicitStatements: true, out var owner))
            {
                // We don't care about this range as this can potentially lead to incorrect scopes.
                context.Logger?.LogMessage($"Don't care about line: {line.ToString()}");
                continue;
            }

            if (_documentMappingSerivce.TryMapToCSharpDocumentPosition(csharpDocument, lineStart, out _, out var projectedLineStart))
            {
                lineStartMap[lineStart] = projectedLineStart;
                significantLocations.Add(projectedLineStart);
            }
            else if (owner is CSharpTransitionSyntax &&
                owner.Parent is RazorDirectiveSyntax containingDirective &&
                containingDirective.IsDirective(SectionDirective.Directive))
            {
                // Section directives are a challenge because they have Razor indentation (we want to indent their contents one level)
                // and their contents will have Html indentation, and the generated code for them is indented (contents are in a lambda)
                // but they have no C# mapping themselves to rely on. In there is no C# in a section block at all, everything works great
                // but even simple C# poses a challenge. For example:
                //
                // @section Goo {
                //     @if (true)
                //     {
                //          // some C# content
                //     }
                // }
                //
                // The `if` in the generated code will be indented by virtue of being in a lambda, but with nothing in the @section directive
                // itself that is mapped, the baseline indentation will be whatever happens to be the nearest C# mapping from outside the block
                // which is not helpful. To solve this, we artificially introduce a mapping for the start of the section block, which points to
                // the first C# mapping inside it.
                if (containingDirective.DirectiveBody.CSharpCode.Children is [.., MarkupBlockSyntax block, RazorMetaCodeSyntax /* close brace */])
                {
                    var blockSpan = block.Span;
                    foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
                    {
                        if (blockSpan.Contains(mapping.OriginalSpan.AbsoluteIndex))
                        {
                            var projectedStartLocation = mapping.GeneratedSpan.AbsoluteIndex;
                            lineStartMap[blockSpan.Start] = projectedStartLocation;
                            sourceMappingMap[blockSpan.Start] = projectedStartLocation;
                            significantLocations.Add(projectedStartLocation);
                            break;
                        }
                        else if (mapping.OriginalSpan.AbsoluteIndex > blockSpan.End)
                        {
                            // This span (and all following) are after the area we're interested in
                            break;
                        }
                    }
                }
            }
            else
            {
                context.Logger?.LogMessage($"Couldn't map line: {line.ToString()}");
            }
        }

        // Now, invoke the C# formatter to obtain the CSharpDesiredIndentation for all significant locations.
        var significantLocationIndentation = await CSharpFormatter.GetCSharpIndentationAsync(context, significantLocations, hostWorkspaceServices, cancellationToken).ConfigureAwait(false);

        // Build source mapping indentation scopes.
        var sourceMappingIndentations = new SortedDictionary<int, IndentationData>();
        var root = context.CodeDocument.GetRequiredSyntaxRoot();
        foreach (var originalLocation in sourceMappingMap.Keys)
        {
            var significantLocation = sourceMappingMap[originalLocation];
            if (!significantLocationIndentation.TryGetValue(significantLocation, out var indentation))
            {
                // C# formatter didn't return an indentation for this. Skip.
                continue;
            }

            if (originalLocation > root.EndPosition)
            {
                continue;
            }

            var scopeOwner = root.FindInnermostNode(originalLocation);
            if (!sourceMappingIndentations.ContainsKey(originalLocation))
            {
                sourceMappingIndentations[originalLocation] = new IndentationData(indentation);
            }

            // For @section blocks we have special handling to add a fake source mapping/significant location at the end of the
            // section, to return the indentation back to before the start of the section block.
            if (scopeOwner?.Parent?.Parent?.Parent is RazorDirectiveSyntax containingDirective &&
                containingDirective.IsDirective(SectionDirective.Directive) &&
                !sourceMappingIndentations.ContainsKey(containingDirective.EndPosition - 1))
            {
                // We want the indentation for the end point to be whatever the indentation was before the start point. For
                // performance reasons, and because source mappings could be un-ordered, we defer that calculation until
                // later, when we have all of the information in place. We use a negative number to indicate that there is
                // more processing to do.
                // This is saving repeatedly realising the source mapping indentations keys, then converting them to an array,
                // and then doing binary search here, before we've processed all of the mappings
                sourceMappingIndentations[containingDirective.EndPosition - 1] = new IndentationData(lazyLoad: true, offset: originalLocation - 1);
            }
        }

        var sourceMappingIndentationScopes = sourceMappingIndentations.Keys.ToArray();

        // Build lineStart indentation map.
        var lineStartIndentations = new Dictionary<int, int>();
        foreach (var originalLocation in lineStartMap.Keys)
        {
            var significantLocation = lineStartMap[originalLocation];
            if (!significantLocationIndentation.TryGetValue(significantLocation, out var indentation))
            {
                // C# formatter didn't return an indentation for this. Skip.
                continue;
            }

            lineStartIndentations[originalLocation] = indentation;
        }

        // Now, let's combine the C# desired indentation with the Razor and HTML indentation for each line.
        var newIndentations = new Dictionary<int, int>();
        for (var i = startLine; i <= endLineInclusive; i++)
        {
            if (indentations[i].EmptyOrWhitespaceLine)
            {
                // We should remove whitespace on empty lines.
                newIndentations[i] = 0;
                continue;
            }

            var minCSharpIndentation = context.GetIndentationOffsetForLevel(indentations[i].MinCSharpIndentLevel);
            var line = context.SourceText.Lines[i];
            var lineStart = line.GetFirstNonWhitespacePosition() ?? line.Start;
            var lineStartSpan = new TextSpan(lineStart, 0);
            if (!ShouldFormatLine(context, lineStartSpan, allowImplicitStatements: true))
            {
                // We don't care about this line as it lies in an area we don't want to format.
                continue;
            }

            if (!lineStartIndentations.TryGetValue(lineStart, out var csharpDesiredIndentation))
            {
                // Couldn't remap. This is probably a non-C# location.
                // Use SourceMapping indentations to locate the C# scope of this line.
                // E.g,
                //
                // @if (true) {
                //   <div>
                //  |</div>
                // }
                //
                // We can't find a direct mapping at |, but we can infer its base indentation from the
                // indentation of the latest source mapping prior to this line.
                // We use binary search to find that spot.

                var index = Array.BinarySearch(sourceMappingIndentationScopes, lineStart);

                if (index < 0)
                {
                    // Couldn't find the exact value. Find the index of the element to the left of the searched value.
                    index = (~index) - 1;
                }

                if (index < 0)
                {
                    // If we _still_ couldn't find the right indentation, then it probably means that the text is
                    // before the first source mapping location, so we can just place it in the minimum spot (realistically
                    // at index 0 in the razor file, but we use minCSharpIndentation because we're adjusting based on the
                    // generated file here)
                    csharpDesiredIndentation = minCSharpIndentation;
                }
                else
                {
                    // index will now be set to the same value as the end of the closest source mapping.
                    var absoluteIndex = sourceMappingIndentationScopes[index];
                    csharpDesiredIndentation = sourceMappingIndentations[absoluteIndex].GetIndentation(sourceMappingIndentations, sourceMappingIndentationScopes, minCSharpIndentation);

                    // This means we didn't find an exact match and so we used the indentation of the end of a previous mapping.
                    // So let's use the MinCSharpIndentation of that same location if possible.
                    if (context.TryGetFormattingSpan(absoluteIndex, out var span))
                    {
                        minCSharpIndentation = context.GetIndentationOffsetForLevel(span.MinCSharpIndentLevel);
                    }
                }
            }

            // Now let's use that information to figure out the effective C# indentation.
            // This should be based on context.
            // For instance, lines inside @code/@functions block should be reduced one level
            // and lines inside @{} should be reduced by two levels.

            if (csharpDesiredIndentation < minCSharpIndentation)
            {
                // CSharp formatter doesn't want to indent this. Let's not touch it.
                continue;
            }

            var effectiveCSharpDesiredIndentation = csharpDesiredIndentation - minCSharpIndentation;
            var razorDesiredIndentation = context.GetIndentationOffsetForLevel(indentations[i].IndentationLevel);
            if (indentations[i].StartsInHtmlContext)
            {
                // This is a non-C# line.
                // HTML formatter doesn't run in the case of format on type.
                // Let's stick with our syntax understanding of HTML to figure out the desired indentation.
            }

            var effectiveDesiredIndentation = razorDesiredIndentation + effectiveCSharpDesiredIndentation;

            // This will now contain the indentation we ultimately want to apply to this line.
            newIndentations[i] = effectiveDesiredIndentation;
        }

        // Now that we have collected all the indentations for each line, let's convert them to text edits.
        using var changes = new PooledArrayBuilder<TextChange>(capacity: newIndentations.Count);
        foreach (var item in newIndentations)
        {
            var line = item.Key;
            var indentation = item.Value;
            Debug.Assert(indentation >= 0, "Negative indentation. This is unexpected.");

            var existingIndentationLength = indentations[line].ExistingIndentation;
            var spanToReplace = new TextSpan(context.SourceText.Lines[line].Start, existingIndentationLength);
            var effectiveDesiredIndentation = FormattingUtilities.GetIndentationString(indentation, context.Options.InsertSpaces, context.Options.TabSize);
            changes.Add(new TextChange(spanToReplace, effectiveDesiredIndentation));
        }

        return changes.ToImmutableAndClear();
    }

    private static bool ShouldFormat(FormattingContext context, TextSpan mappingSpan, bool allowImplicitStatements)
        => ShouldFormat(context, mappingSpan, allowImplicitStatements, out _);

    private static bool ShouldFormat(FormattingContext context, TextSpan mappingSpan, bool allowImplicitStatements, out RazorSyntaxNode? foundOwner)
        => ShouldFormat(context, mappingSpan, new ShouldFormatOptions(allowImplicitStatements, isLineRequest: false), out foundOwner);

    private static bool ShouldFormatLine(FormattingContext context, TextSpan mappingSpan, bool allowImplicitStatements)
        => ShouldFormat(context, mappingSpan, new ShouldFormatOptions(allowImplicitStatements, isLineRequest: true), out _);

    private static bool ShouldFormat(FormattingContext context, TextSpan mappingSpan, ShouldFormatOptions options, out RazorSyntaxNode? foundOwner)
    {
        // We should be called with the range of various C# SourceMappings.

        if (mappingSpan.Start == 0)
        {
            // The mapping starts at 0. It can't be anything special but pure C#. Let's format it.
            foundOwner = null;
            return true;
        }

        var root = context.CodeDocument.GetRequiredSyntaxRoot();
        var owner = root.FindInnermostNode(mappingSpan.Start, includeWhitespace: true);
        if (owner is null)
        {
            // Can't determine owner of this position. Optimistically allow formatting.
            foundOwner = null;
            return true;
        }

        foundOwner = owner;

        // Special case: If we're formatting implicit statements, we want to treat the `@attribute` directive and
        // the `@typeparam` directive as one so that the C# content within them is formatted as C#
        if (options.AllowImplicitStatements &&
            (
                IsAttributeDirective() ||
                IsTypeParamDirective()
            ))
        {
            return true;
        }

        if (IsInsideRazorComment())
        {
            return false;
        }

        if (IsInBoundComponentAttributeName())
        {
            return false;
        }

        if (IsComponentStartTagName())
        {
            return false;
        }

        if (IsInHtmlAttributeValue())
        {
            return false;
        }

        if (IsInDirectiveWithNoKind())
        {
            return false;
        }

        if (IsInSingleLineDirective())
        {
            return false;
        }

        if (!options.AllowImplicitExpressions && IsImplicitExpression())
        {
            return false;
        }

        if (!options.AllowSingleLineExplicitExpressions && IsSingleLineExplicitExpression())
        {
            return false;
        }

        if (IsInSectionDirectiveOrBrace())
        {
            return false;
        }

        if (!options.AllowImplicitStatements && IsImplicitStatementStart())
        {
            return false;
        }

        if (IsInTemplateBlock())
        {
            return false;
        }

        return true;

        bool IsInsideRazorComment()
        {
            // We don't want to format _in_ comments, but we do want to move the start `@*` to the right position
            if (owner is RazorCommentBlockSyntax &&
                mappingSpan.Start != owner.SpanStart)
            {
                return true;
            }

            return false;
        }

        bool IsImplicitStatementStart()
        {
            // We will return true if the position points to the start of the C# portion of an implicit statement.
            // `@|for(...)` - true
            // `@|if(...)` - true
            // `@{|...` - false
            // `@code {|...` - false
            //

            if (owner.SpanStart == mappingSpan.Start &&
                owner is CSharpStatementLiteralSyntax { Parent: CSharpCodeBlockSyntax } literal &&
                literal.TryGetPreviousSibling(out var transition) &&
                transition is CSharpTransitionSyntax)
            {
                return true;
            }

            // Not an implicit statement.
            return false;
        }

        bool IsInBoundComponentAttributeName()
        {
            // E.g, (| is position)
            //
            // `<p |csharpattr="Variable">` - true
            //
            // Because we map attributes, so rename and FAR works, there could be C# mapping for them,
            // but only if they're actually bound attributes. We don't want the mapping to throw make the
            // formatting engine think it needs to apply C# indentation rules.
            //
            // The exception here is if we're being asked whether to format the line of code at all,
            // then we want to pretend it's not a component attribute, because we do still want the line
            // formatted. ie, given this:
            //
            // `<p
            //     |csharpattr="Variable">`
            //
            // We want to return false when being asked to format the line, so the line gets indented, but
            // return true if we're just being asked "should we format this according to C# rules".

            return owner is MarkupTextLiteralSyntax
            {
                Parent: MarkupTagHelperAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
                        MarkupTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
                        MarkupMinimizedTagHelperAttributeSyntax { TagHelperAttributeInfo.Bound: true } or
                        MarkupMinimizedTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true }
            } && !options.IsLineRequest;
        }

        bool IsComponentStartTagName()
        {
            // E.g, (| is position)
            //
            // `<|Component>` - true
            //
            // As above, we map component elements, so GTD and FAR works, there could be C# mapping for them.
            // We don't want the mapping to make the formatting engine think it needs to apply C# indentation rules.

            return owner is MarkupTagHelperStartTagSyntax startTag &&
                startTag.Name.Span.Contains(mappingSpan.Start);
        }

        bool IsInHtmlAttributeValue()
        {
            // E.g, (| is position)
            //
            // `<p csharpattr="|Variable">` - true
            //
            return owner.AncestorsAndSelf().Any(
                n => n is MarkupDynamicAttributeValueSyntax or
                          MarkupLiteralAttributeValueSyntax or
                          MarkupTagHelperAttributeValueSyntax);
        }

        bool IsInDirectiveWithNoKind()
        {
            // E.g, (| is position)
            //
            // `@using |System;
            //
            return owner.AncestorsAndSelf().Any(
                n => n is RazorUsingDirectiveSyntax or RazorDirectiveSyntax { HasDirectiveDescriptor: false });
        }

        bool IsAttributeDirective()
        {
            // E.g, (| is position)
            //
            // `@attribute |[System.Obsolete]
            //
            return owner.AncestorsAndSelf().Any(
                static n => n is RazorDirectiveSyntax directive && directive.IsDirective(AttributeDirective.Directive));
        }

        bool IsTypeParamDirective()
        {
            // E.g, (| is position)
            //
            // `@typeparam |T where T : IDisposable
            //
            return owner.AncestorsAndSelf().Any(
               static n => n is RazorDirectiveSyntax directive && directive.IsDirective(ComponentTypeParamDirective.Directive));
        }

        bool IsInSingleLineDirective()
        {
            // E.g, (| is position)
            //
            // `@inject |SomeType SomeName` - true
            //
            return owner.AncestorsAndSelf().Any(
                static n => n is RazorDirectiveSyntax directive && directive.IsDirectiveKind(DirectiveKind.SingleLine));
        }

        bool IsImplicitExpression()
        {
            // E.g, (| is position)
            //
            // `@|foo` - true
            //
            return owner.AncestorsAndSelf().Any(static n => n is CSharpImplicitExpressionSyntax);
        }

        bool IsSingleLineExplicitExpression()
        {
            // E.g, (| is position)
            //
            // `|@{ foo }` - true
            //
            if (owner is { Parent.Parent.Parent: CSharpExplicitExpressionSyntax explicitExpression } &&
                context.SourceText.GetRange(explicitExpression.Span) is { } exprRange &&
                exprRange.IsSingleLine())
            {
                return true;
            }

            return owner.AncestorsAndSelf().Any(n => n is CSharpImplicitExpressionSyntax);
        }

        bool IsInTemplateBlock()
        {
            // E.g, (| is position)
            //
            // `RenderFragment(|@<Component>);` - true
            //
            return owner.AncestorsAndSelf().Any(n => n is CSharpTemplateBlockSyntax);
        }

        bool IsInSectionDirectiveOrBrace()
        {
            // @section Scripts {
            //     <script></script>
            // }

            // In design time there is a source mapping for the section name, but it doesn't appear in runtime, so
            // we effectively pretend it doesn't exist so the formatting engine can handle both forms.
            if (owner is CSharpStatementLiteralSyntax literal &&
                owner.Parent?.Parent?.Parent is RazorDirectiveSyntax directive3 &&
                directive3.IsDirective(SectionDirective.Directive))
            {
                return true;
            }

            // Due to how sections are generated (inside a multi-line lambda), we also want to exclude the braces
            // from being formatted, or it will be indented by one level due to the lambda. The rest we don't
            // need to worry about, because the one level indent is actually desirable.

            // Open brace is the 4th child of the C# code block that is the directive itself
            // and close brace is the last child
            if (owner is RazorMetaCodeSyntax &&
                owner.Parent is CSharpCodeBlockSyntax codeBlock &&
                codeBlock.Children.Count > 3 &&
                (owner == codeBlock.Children[3] || owner == codeBlock.Children[^1]) &&
                // CSharpCodeBlock -> RazorDirectiveBody -> RazorDirective
                codeBlock.Parent?.Parent is RazorDirectiveSyntax directive2 &&
                directive2.IsDirective(SectionDirective.Directive))
            {
                return true;
            }

            return false;
        }
    }

    private static string RenderSourceMappings(RazorCodeDocument codeDocument)
    {
        using var pooledBuilder = StringBuilderPool.GetPooledObject();
        var builder = pooledBuilder.Object;

        var documentText = codeDocument.Source.Text.ToString();
        var lastIndex = 0;
        foreach (var mapping in codeDocument.GetRequiredCSharpDocument().SourceMappingsSortedByOriginal)
        {
            builder.Append(documentText, lastIndex, mapping.OriginalSpan.AbsoluteIndex - lastIndex);
            builder.Append("<#");
            builder.Append(documentText, mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
            builder.Append("#>");

            lastIndex = mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length;
        }

        builder.Append(documentText, lastIndex, documentText.Length - lastIndex);

        return builder.ToString();
    }

    private record struct ShouldFormatOptions(bool AllowImplicitStatements, bool AllowImplicitExpressions, bool AllowSingleLineExplicitExpressions, bool IsLineRequest)
    {
        public ShouldFormatOptions(bool allowImplicitStatements, bool isLineRequest)
            : this(allowImplicitStatements, true, true, isLineRequest)
        {
        }
    }

    private class IndentationData
    {
        private readonly int _offset;
        private int _indentation;
        private bool _lazyLoad;

        public IndentationData(int indentation)
        {
            _indentation = indentation;
        }

        public IndentationData(bool lazyLoad, int offset)
        {
            _lazyLoad = lazyLoad;
            _offset = offset;
        }

        public int GetIndentation(SortedDictionary<int, IndentationData> sourceMappingIndentations, int[] indentationScopes, int minCSharpIndentation)
        {
            // If we're lazy loading, then we need to find the indentation from the source mappings, at the offset,
            // which for whatever reason may not have been available when creating this class.
            if (_lazyLoad)
            {
                _lazyLoad = false;

                var index = Array.BinarySearch(indentationScopes, _offset);
                if (index < 0)
                {
                    index = (~index) - 1;
                }

                // If there is a source mapping to the left of the original start point, then we use its indentation
                // otherwise use the minimum
                _indentation = index < 0
                    ? minCSharpIndentation
                    : sourceMappingIndentations[indentationScopes[index]]._indentation;
            }

            return _indentation;
        }
    }
}
