// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConstructorInitializerPlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConstructorInitializerPlacementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId,
                   EnforceOnBuildValues.ConstructorInitializerPlacement,
                   CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Blank_line_not_allowed_after_constructor_initializer_colon), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().AllowBlankLineAfterColonInConstructorInitializer;
            if (option.Value)
                return;

            Recurse(context, option.Notification.Severity, context.GetAnalysisRoot(findInTrivia: false));
        }

        private void Recurse(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, SyntaxNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // Don't bother analyzing nodes that have syntax errors in them.
            if (node.ContainsDiagnostics)
                return;

            if (node is ConstructorInitializerSyntax initializer)
                ProcessConstructorInitializer(context, severity, initializer);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (!context.ShouldAnalyzeSpan(child.Span))
                    continue;

                if (child.IsNode)
                    Recurse(context, severity, child.AsNode()!);
            }
        }

        private void ProcessConstructorInitializer(
            SyntaxTreeAnalysisContext context, ReportDiagnostic severity, ConstructorInitializerSyntax initializer)
        {
            var sourceText = context.Tree.GetText(context.CancellationToken);

            var colonToken = initializer.ColonToken;
            var thisOrBaseKeyword = initializer.ThisOrBaseKeyword;

            var colonLine = sourceText.Lines.GetLineFromPosition(colonToken.SpanStart);
            var thisBaseLine = sourceText.Lines.GetLineFromPosition(thisOrBaseKeyword.SpanStart);
            if (colonLine == thisBaseLine)
                return;

            if (colonToken.TrailingTrivia.Count == 0)
                return;

            if (colonToken.TrailingTrivia.Last().Kind() != SyntaxKind.EndOfLineTrivia)
                return;

            if (colonToken.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine()))
                return;

            if (thisOrBaseKeyword.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine() && !t.IsSingleOrMultiLineComment()))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                colonToken.GetLocation(),
                severity,
                additionalLocations: ImmutableArray.Create(initializer.GetLocation()),
                properties: null));
        }
    }
}
