// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
/// Gets edits in Html files, and returns edits to Razor files, with nicely formatted Html
/// </summary>
internal sealed class HtmlOnTypeFormattingPass : IFormattingPass
{
    public async Task<ImmutableArray<TextChange>> ExecuteAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        if (changes.Length == 0)
        {
            // There are no HTML edits for us to apply. No op.
            return [];
        }

        var originalText = context.SourceText;

        var changedText = originalText;
        var changedContext = context;

        context.Logger?.LogSourceText("BeforeHtmlFormatter", changedText);

        if (changes.Length > 0)
        {
            var filteredChanges = FilterIncomingChanges(changedContext, changes);

            changedText = originalText.WithChanges(filteredChanges);
            // Create a new formatting context for the changed razor document.
            changedContext = await context.WithTextAsync(changedText, cancellationToken).ConfigureAwait(false);

            context.Logger?.LogSourceText("AfterNormalizedEdits", changedText);
        }

        var indentationChanges = AdjustRazorIndentation(changedContext);
        if (indentationChanges.Length > 0)
        {
            // Apply the edits that adjust indentation.
            changedText = changedText.WithChanges(indentationChanges);
            context.Logger?.LogSourceText("AfterAdjustRazorIndentation", changedText);
        }

        return SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, DiffKind.Char);
    }

    private static ImmutableArray<TextChange> FilterIncomingChanges(FormattingContext context, ImmutableArray<TextChange> changes)
    {
        var root = context.CodeDocument.GetRequiredSyntaxRoot();

        using var changesToKeep = new PooledArrayBuilder<TextChange>(capacity: changes.Length);

        foreach (var change in changes)
        {
            // Don't keep changes that start inside of a razor comment block.
            var comment = root.FindInnermostNode(change.Span.Start)?.FirstAncestorOrSelf<RazorCommentBlockSyntax>();
            if (comment is not null)
            {
                continue;
            }

            changesToKeep.Add(change);
        }

        return changesToKeep.ToImmutableAndClear();
    }

    private static ImmutableArray<TextChange> AdjustRazorIndentation(FormattingContext context)
    {
        // Assume HTML formatter has already run at this point and HTML is relatively indented correctly.
        // But HTML doesn't know about Razor blocks.
        // Our goal here is to indent each line according to the surrounding Razor blocks.
        var sourceText = context.SourceText;
        var indentations = context.GetIndentations();

        using var editsToApply = new PooledArrayBuilder<TextChange>(capacity: sourceText.Lines.Count);
        for (var i = 0; i < sourceText.Lines.Count; i++)
        {
            var line = sourceText.Lines[i];
            if (line.Span.Length == 0)
            {
                // Empty line.
                continue;
            }

            if (indentations[i].StartsInCSharpContext)
            {
                // Normally we don't do HTML things in C# contexts but there is one
                // edge case when including render fragments in a C# code block, eg:
                //
                // @code {
                //      void Foo()
                //      {
                //          Render(@<SurveyPrompt />);
                //      {
                // }
                //
                // This is popular in some libraries, like bUnit. The issue here is that
                // the HTML formatter sees ~~~~~<SurveyPrompt /> and puts a newline before
                // the tag, but obviously that breaks things.
                //
                // It's straight forward enough to just check for this situation and special case
                // it by removing the newline again.

                // There needs to be at least one more line, and the current line needs to end with
                // an @ sign, and have an open angle bracket at the start of the next line.
                if (sourceText.Lines.Count >= i + 1 &&
                    line.Text?.Length > 1 &&
                    line.Text?[line.End - 1] == '@')
                {
                    var nextLine = sourceText.Lines[i + 1];
                    var firstChar = nextLine.GetFirstNonWhitespaceOffset().GetValueOrDefault();

                    // When the HTML formatter inserts the newline in this scenario, it doesn't
                    // indent the component tag, so we use that as another signal that this is
                    // the scenario we think it is.
                    if (firstChar == 0 &&
                        nextLine.Text?[nextLine.Start] == '<')
                    {
                        var lineBreakLength = line.EndIncludingLineBreak - line.End;
                        var spanToReplace = new TextSpan(line.End, lineBreakLength);
                        var change = new TextChange(spanToReplace, string.Empty);
                        editsToApply.Add(change);

                        // Skip the next line because we've essentially just removed it.
                        i++;
                    }
                }

                continue;
            }

            var razorDesiredIndentationLevel = indentations[i].RazorIndentationLevel;
            if (razorDesiredIndentationLevel == 0)
            {
                // This line isn't under any Razor specific constructs. Trust the HTML formatter.
                continue;
            }

            var htmlDesiredIndentationLevel = indentations[i].HtmlIndentationLevel;
            if (htmlDesiredIndentationLevel == 0 && !IsPartOfHtmlTag(context, indentations[i].FirstSpan.Span.Start))
            {
                // This line is under some Razor specific constructs but not under any HTML tag.
                // E.g,
                // @{
                //          @* comment *@ <----
                // }
                //
                // In this case, the HTML formatter wouldn't touch it but we should format it correctly.
                // So, let's use our syntax understanding to rewrite the indentation.
                // Note: This case doesn't apply for HTML tags (HTML formatter will touch it even if it is in the root).
                // Hence the second part of the if condition.
                //
                var desiredIndentationLevel = indentations[i].IndentationLevel;
                var desiredIndentationString = context.GetIndentationLevelString(desiredIndentationLevel);
                var spanToReplace = new TextSpan(line.Start, indentations[i].ExistingIndentation);
                var change = new TextChange(spanToReplace, desiredIndentationString);
                editsToApply.Add(change);
            }
            else
            {
                // This line is under some Razor specific constructs and HTML tags.
                // E.g,
                // @{
                //    <div class="foo"
                //         id="oof">  <----
                //    </div>
                // }
                //
                // In this case, the HTML formatter would've formatted it correctly. Let's not use our syntax understanding.
                // Instead, we should just add to the existing indentation.
                //
                var razorDesiredIndentationString = context.GetIndentationLevelString(razorDesiredIndentationLevel);
                var existingIndentationString = FormattingUtilities.GetIndentationString(indentations[i].ExistingIndentationSize, context.Options.InsertSpaces, context.Options.TabSize);
                var desiredIndentationString = existingIndentationString + razorDesiredIndentationString;
                var spanToReplace = new TextSpan(line.Start, indentations[i].ExistingIndentation);
                var change = new TextChange(spanToReplace, desiredIndentationString);
                editsToApply.Add(change);
            }
        }

        return editsToApply.ToImmutableAndClear();
    }

    private static bool IsPartOfHtmlTag(FormattingContext context, int position)
    {
        var root = context.CodeDocument.GetRequiredSyntaxRoot();
        var owner = root.FindInnermostNode(position, includeWhitespace: true);
        if (owner is null)
        {
            // Can't determine owner of this position.
            return false;
        }

        // E.g, (| is position)
        //
        // `<p csharpattr="|Variable">` - true
        //
        return owner.AncestorsAndSelf().Any(
            n => n is MarkupStartTagSyntax or MarkupTagHelperStartTagSyntax or MarkupEndTagSyntax or MarkupTagHelperEndTagSyntax);
    }
}
