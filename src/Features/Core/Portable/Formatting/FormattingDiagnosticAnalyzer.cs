// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class FormattingDiagnosticAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        public static readonly string ReplaceTextKey = nameof(ReplaceTextKey);

        public static readonly ImmutableDictionary<string, string> RemoveTextProperties =
            ImmutableDictionary.Create<string, string>().Add(ReplaceTextKey, "");

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

            var options = context.Options.GetDocumentOptionSetAsync(context.Tree, context.CancellationToken).GetAwaiter().GetResult();
            if (options == null)
            {
                return;
            }

            var workspace = workspaceAnalyzerOptions.Services.Workspace;
            var formattingChanges = Formatter.GetFormattedTextChanges(context.Tree.GetRoot(context.CancellationToken), workspace, options, context.CancellationToken);
            foreach (var formattingChange in formattingChanges)
            {
                var change = formattingChange;
                if (change.NewText.Length > 0 && !change.Span.IsEmpty)
                {
                    var oldText = context.Tree.GetText(context.CancellationToken);

                    // Handle cases where the change is a substring removal from the beginning
                    var offset = change.Span.Length - change.NewText.Length;
                    if (offset >= 0 && oldText.GetSubText(new TextSpan(change.Span.Start + offset, change.NewText.Length)).ContentEquals(SourceText.From(change.NewText)))
                    {
                        change = new TextChange(new TextSpan(change.Span.Start, offset), "");
                    }
                    else
                    {
                        // Handle cases where the change is a substring removal from the end
                        if (change.NewText.Length < change.Span.Length
                            && oldText.GetSubText(new TextSpan(change.Span.Start, change.NewText.Length)).ContentEquals(SourceText.From(change.NewText)))
                        {
                            change = new TextChange(new TextSpan(change.Span.Start + change.NewText.Length, change.Span.Length - change.NewText.Length), "");
                        }
                    }
                }

                if (change.NewText.Length == 0 && change.Span.IsEmpty)
                {
                    // No actual change
                    continue;
                }

                ImmutableDictionary<string, string> properties;
                if (change.NewText.Length == 0)
                {
                    properties = RemoveTextProperties;
                }
                else
                {
                    properties = ImmutableDictionary.Create<string, string>().Add(ReplaceTextKey, change.NewText);
                }

                var location = Location.Create(context.Tree, change.Span);
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    location,
                    ReportDiagnostic.Default,
                    additionalLocations: null,
                    properties));
            }
        }
    }
}
