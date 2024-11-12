// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal abstract class AbstractFormattingAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    protected AbstractFormattingAnalyzer()
        : base(
            IDEDiagnosticIds.FormattingDiagnosticId,
            EnforceOnBuildValues.Formatting,
            option: null,
            new LocalizableResourceString(nameof(AnalyzersResources.Fix_formatting), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Fix_formatting), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected abstract ISyntaxFormatting SyntaxFormatting { get; }

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterSyntaxTreeAction(treeContext => AnalyzeSyntaxTree(treeContext, context.Compilation.Options)));

    /// <summary>
    /// Fixing formatting is high priority.  It's something the user wants to be able to fix quickly, is driven by
    /// them acting on an error reported in code, and can be computed fast as it only uses syntax not semantics.
    /// It's also the 8th most common fix that people use, and is picked almost all the times it is shown.
    /// </summary>
    public override bool IsHighPriority => true;

    private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        if (ShouldSkipAnalysis(context, compilationOptions, notification: null))
            return;

        var tree = context.Tree;
        var cancellationToken = context.CancellationToken;

        var oldText = tree.GetText(cancellationToken);
        var root = tree.GetRoot(cancellationToken);
        var span = context.FilterSpan.HasValue ? context.FilterSpan.GetValueOrDefault() : root.FullSpan;
        var spans = SpecializedCollections.SingletonEnumerable(span);
        var formattingOptions = context.GetAnalyzerOptions().GetSyntaxFormattingOptions(SyntaxFormatting);
        var formattingChanges = SyntaxFormatting.GetFormattingResult(root, spans, formattingOptions, rules: default, cancellationToken).GetTextChanges(cancellationToken);

        // formattingChanges could include changes that impact a larger section of the original document than
        // necessary. Before reporting diagnostics, process the changes to minimize the span of individual
        // diagnostics.
        foreach (var formattingChange in formattingChanges)
        {
            var change = formattingChange;
            Contract.ThrowIfNull(change.NewText);

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

            Contract.ThrowIfNull(change.NewText);
            if (change.NewText.Length == 0 && change.Span.IsEmpty)
            {
                // No actual change (allows for the formatter to report a NOP change without triggering a
                // diagnostic that can't be fixed).
                continue;
            }

            var location = Location.Create(tree, change.Span);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                location,
                additionalLocations: null,
                properties: null));
        }
    }
}
