// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Formatter = Microsoft.CodeAnalysis.Formatting.FormatterHelper;
using FormattingProvider = Microsoft.CodeAnalysis.Formatting.ISyntaxFormatting;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class FormattingAnalyzerHelper
{
    internal static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, FormattingProvider formattingProvider, DiagnosticDescriptor descriptor, SyntaxFormattingOptions options)
    {
        var tree = context.Tree;
        var cancellationToken = context.CancellationToken;

        var oldText = tree.GetText(cancellationToken);
        var root = tree.GetRoot(cancellationToken);
        var span = context.FilterSpan.HasValue ? context.FilterSpan.GetValueOrDefault() : root.FullSpan;
        var spans = SpecializedCollections.SingletonEnumerable(span);
        var formattingChanges = Formatter.GetFormattedTextChanges(root, spans, formattingProvider, options, rules: default, cancellationToken);

        // formattingChanges could include changes that impact a larger section of the original document than
        // necessary. Before reporting diagnostics, process the changes to minimize the span of individual
        // diagnostics.
        foreach (var formattingChange in formattingChanges)
        {
            var change = formattingChange;
            if (change.NewText.Length > 0 && !change.Span.IsEmpty)
            {
                // Handle cases where the change is a substring removal from the beginning. In these cases, we want
                // the diagnostic span to cover the unwanted leading characters (which should be removed), and
                // nothing more.
                var offset = change.Span.Length - change.NewText.Length;
                if (offset >= 0)
                {
                    if (oldText.GetSubText(new TextSpan(change.Span.Start + offset, change.NewText.Length)).ContentEquals(SourceText.From(change.NewText)))
                    {
                        change = new TextChange(new TextSpan(change.Span.Start, offset), "");
                    }
                    else
                    {
                        // Handle cases where the change is a substring removal from the end. In these cases, we want
                        // the diagnostic span to cover the unwanted trailing characters (which should be removed), and
                        // nothing more.
                        if (oldText.GetSubText(new TextSpan(change.Span.Start, change.NewText.Length)).ContentEquals(SourceText.From(change.NewText)))
                        {
                            change = new TextChange(new TextSpan(change.Span.Start + change.NewText.Length, offset), "");
                        }
                    }
                }
            }

            if (change.NewText.Length == 0 && change.Span.IsEmpty)
            {
                // No actual change (allows for the formatter to report a NOP change without triggering a
                // diagnostic that can't be fixed).
                continue;
            }

            var location = Location.Create(tree, change.Span);
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                additionalLocations: null,
                properties: null));
        }
    }
}
