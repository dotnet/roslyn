// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class FormattingDiagnosticAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        public FormattingDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.FormattingDiagnosticId,
                new LocalizableResourceString(nameof(FeaturesResources.Formatting_analyzer_title), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.Formatting_analyzer_message), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            if (!(context.Options is WorkspaceAnalyzerOptions workspaceAnalyzerOptions))
            {
                return;
            }

            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var options = context.Options.GetDocumentOptionSetAsync(tree, cancellationToken).GetAwaiter().GetResult();
            if (options == null)
            {
                return;
            }

            var workspace = workspaceAnalyzerOptions.Services.Workspace;
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
                    throw ExceptionUtilities.Unreachable;
                }

                var location = Location.Create(tree, change.Span);
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    location,
                    ReportDiagnostic.Default,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}
