// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingAnalyzerImpl
    {
        private readonly DiagnosticDescriptor _descriptor;

        protected AbstractFormattingAnalyzerImpl(DiagnosticDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        internal void InitializeWorker(AnalysisContext context)
        {
            var workspace = new AdhocWorkspace();
            var codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

            context.RegisterSyntaxTreeAction(c => AnalyzeSyntaxTree(c, workspace, codingConventionsManager));
        }

        protected abstract OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, Workspace workspace, ICodingConventionsManager codingConventionsManager)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var options = workspace.Options;
            if (File.Exists(tree.FilePath))
            {
                var codingConventionContext = codingConventionsManager.GetConventionContextAsync(tree.FilePath, cancellationToken).GetAwaiter().GetResult();
                options = ApplyFormattingOptions(options, codingConventionContext);
            }

            var oldText = tree.GetText(cancellationToken);
            var formattingChanges = Formatter.GetFormattedTextChanges(tree.GetRoot(cancellationToken), workspace, options, cancellationToken);

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
                    // No actual change
                    throw new InvalidOperationException("This program location is thought to be unreachable.");
                }

                var location = Location.Create(tree, change.Span);
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    _descriptor,
                    location,
                    ReportDiagnostic.Default,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}
