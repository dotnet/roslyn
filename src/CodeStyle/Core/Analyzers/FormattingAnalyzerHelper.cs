// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

#if CODE_STYLE
using FormatterState = Microsoft.CodeAnalysis.Formatting.ISyntaxFormattingService;
#else
using Microsoft.CodeAnalysis.Options;
using FormatterState = Microsoft.CodeAnalysis.Workspace;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class FormattingAnalyzerHelper
    {
        internal static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, FormatterState formatterState, DiagnosticDescriptor descriptor, OptionSet options)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var oldText = tree.GetText(cancellationToken);
            var formattingChanges = Formatter.GetFormattedTextChanges(tree.GetRoot(cancellationToken), formatterState, options, cancellationToken);

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

                if (change is { NewText: { Length: 0 }, Span: { IsEmpty: true } })
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
}
