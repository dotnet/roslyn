
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed partial class CSharpFormattingPass(
    IHostServicesProvider hostServicesProvider,
    IDocumentMappingService documentMappingService,
    ILoggerFactory loggerFactory) : IFormattingPass
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CSharpFormattingPass>();
    private readonly IHostServicesProvider _hostServicesProvider = hostServicesProvider;
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;

    public async Task<ImmutableArray<TextChange>> ExecuteAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        // Process changes from previous passes
        var changedText = context.SourceText.WithChanges(changes);
        var changedContext = await context.WithTextAsync(changedText, cancellationToken).ConfigureAwait(false);
        context.Logger?.LogObject("SourceMappings", changedContext.CodeDocument.GetRequiredCSharpDocument().SourceMappingsSortedByGenerated);

        var csharpSyntaxTrue = await changedContext.CurrentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var csharpSyntaxRoot = await csharpSyntaxTrue.GetRootAsync(cancellationToken).ConfigureAwait(false);

        // To format C# code we generate a C# document that represents the indentation semantics the user would be
        // expecting in their Razor file. See the doc comments on CSharpDocumentGenerator for more info
        var generatedDocument = CSharpDocumentGenerator.Generate(changedContext.CodeDocument, csharpSyntaxRoot, context.Options, _documentMappingService);

        var generatedCSharpText = generatedDocument.SourceText;
        context.Logger?.LogSourceText("FormattingDocument", generatedCSharpText);
        context.Logger?.LogObject("FormattingDocumentLineInfo", generatedDocument.LineInfo);
        var formattedCSharpText = await FormatCSharpAsync(generatedCSharpText, context.Options, cancellationToken).ConfigureAwait(false);
        context.Logger?.LogSourceText("FormattedFormattingDocument", formattedCSharpText);

        // We now have a formatted C# document, and an original document, but we can't just apply the changes to the original
        // document as they come from very different places. What we want to do is go through each line of the generated document,
        // take the indentation that is in it, and apply it to the original document, and then take any formatting changes
        // on that line, and translate them across to the original document.
        // Essentially each line is split in two, with indentation on the left of the first non-whitespace char, and formatting
        // changes on the right. Sometimes we need to skip parts of the right (eg, skip the `@` in `@if`), and sometimes we skip
        // one side entirely.

        using var formattingChanges = new PooledArrayBuilder<TextChange>();
        FormattingUtilities.GetOriginalDocumentChangesFromLineInfo(context, changedText, generatedDocument.LineInfo, formattedCSharpText, _logger, shouldKeepInsertedNewlineAtPosition: null, ref formattingChanges.AsRef(), out var lastFormattedTextLine);

        // We're finished processing the original file, which means we've done all of the indentation for the file, and we've done
        // the formatting changes for lines that are entirely C#, or start with C#, and lines that are Html or Razor. Now we process
        // the "additional changes", which is formatting for C# that is inside Html, via implicit or explicit expressions.

        // Previous to this step, all of our changes will have been in order by definition of how we go through the document, so
        // we haven't had to worry about overlaps, but now we do. In order to not loop constantly, we keep track of an extra index
        // variable for where we are in the changes, to check for overlaps.
        var iChanges = 0;
        for (var iFormatted = lastFormattedTextLine; iFormatted < formattedCSharpText.Lines.Count; iFormatted++)
        {
            // Any C# that is in the middle of a line of Html/Razor will be emitted at the end of the generated document, with a
            // comment above it that encodes where it came from in the original file. We just look for the comment, and then apply
            // the next line as formatted content.
            if (CSharpDocumentGenerator.TryParseAdditionalLineComment(formattedCSharpText.Lines[iFormatted], out var start, out var length))
            {
                iFormatted++;

                // Skip ahead to where changes are likely to become relevant, to save looping the whole set every time
                while (iChanges < formattingChanges.Count)
                {
                    if (formattingChanges[iChanges].Span.End > start)
                    {
                        break;
                    }

                    iChanges++;
                }

                if (iChanges < formattingChanges.Count &&
                    formattingChanges[iChanges].Span.Contains(start))
                {
                    // To avoid overlapping changes, which Roslyn will throw on, we just have to drop this change. It gives the user
                    // something at least, and hopefully they'll report a bug for this case so we can find it.
                    context.Logger?.LogMessage($"Skipping a change that would have overlapped an existing change, starting at {start} for {length} chars, overlapping a change at {formattingChanges[iChanges].Span}. iFormatted={iFormatted}, iChanges={iChanges}");
                    continue;
                }

                formattingChanges.Add(new TextChange(new TextSpan(start, length), formattedCSharpText.Lines[iFormatted].ToString()));
            }
        }

        var finalFormattingChanges = formattingChanges.ToArray();
        context.Logger?.LogObject("FinalFormattingChanges", finalFormattingChanges);
        changedText = changedText.WithChanges(finalFormattingChanges);
        context.Logger?.LogSourceText("FinalFormattedDocument", changedText);

        // And we're done, we have a final set of changes to apply. BUT these are changes to the document after Html and Razor
        // formatting, and the return from this method must be changes relative to the original passed in document. The algorithm
        // above is fairly naive anyway, and a lot of them will be no-ops, so it's nice to have this final step as a filter.
        return SourceTextDiffer.GetMinimalTextChanges(context.SourceText, changedText, DiffKind.Char);
    }

    private async Task<SourceText> FormatCSharpAsync(SourceText generatedCSharpText, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        using var helper = new RoslynWorkspaceHelper(_hostServicesProvider);

        var tree = CSharpSyntaxTree.ParseText(generatedCSharpText, cancellationToken: cancellationToken);
        var csharpRoot = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var csharpSyntaxFormattingOptions = options.CSharpSyntaxFormattingOptions;

        if (csharpSyntaxFormattingOptions is not null)
        {
            // Roslyn can be configured to insert a space after a method call, or a dot, but that can break Razor. eg:
            //
            // <div>@PrintHello()</div>
            // @DateTime.Now.ToString()
            //
            // Would become:
            //
            // <div>@PrintHello ()</div>
            // @DateTime. Now. ToString()
            //
            // In Razor, that's not a method call, its a method group (ie C# compile error) followed by Html, and
            // the dot after DateTime is also just Html, as is the rest of the line.
            // We're not smart enough (yet?) to ignore this change when its inline in Razor, but allow it when
            // in a code block, so we just force these options to off.
            csharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions with
            {
                Spacing = csharpSyntaxFormattingOptions.Spacing
                    & ~RazorSpacePlacement.AfterMethodCallName
                    & ~RazorSpacePlacement.AfterDot
            };
        }

        var csharpChanges = RazorCSharpFormattingInteractionService.GetFormattedTextChanges(helper.HostWorkspaceServices, csharpRoot, csharpRoot.FullSpan, options.ToIndentationOptions(), csharpSyntaxFormattingOptions, cancellationToken);

        return generatedCSharpText.WithChanges(csharpChanges);
    }

    [Obsolete("Only for the syntax visualizer, do not call")]
    internal static string GetFormattingDocumentContentsForSyntaxVisualizer(RazorCodeDocument codeDocument, SyntaxNode csharpSyntaxRoot, IDocumentMappingService documentMappingService)
        => CSharpDocumentGenerator.Generate(codeDocument, csharpSyntaxRoot, new(), documentMappingService).SourceText.ToString();
}
