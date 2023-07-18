// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveBracePlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConsecutiveBracePlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConsecutiveBracePlacementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConsecutiveBracePlacementDiagnosticId,
                   EnforceOnBuildValues.ConsecutiveBracePlacement,
                   CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Consecutive_braces_must_not_have_a_blank_between_them), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().AllowBlankLinesBetweenConsecutiveBraces;
            if (option.Value)
                return;

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
            Recurse(context, option.Notification.Severity, stack);
        }

        private void Recurse(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, ArrayBuilder<SyntaxNode> stack)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var root = context.GetAnalysisRoot(findInTrivia: false);
            var text = tree.GetText(cancellationToken);

            stack.Add(root);
            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = stack.Last();
                stack.RemoveLast();

                // Don't bother analyzing nodes that have syntax errors in them.
                if (current.ContainsDiagnostics && current.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                    continue;

                foreach (var child in current.ChildNodesAndTokens())
                {
                    if (!context.ShouldAnalyzeSpan(child.FullSpan))
                        continue;

                    if (child.IsNode)
                        stack.Add(child.AsNode()!);
                    else if (child.IsToken)
                        ProcessToken(context, severity, text, child.AsToken());
                }
            }
        }

        private void ProcessToken(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, SourceText text, SyntaxToken token)
        {
            if (!HasExcessBlankLinesAfter(text, token, out var secondBrace, out _))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                secondBrace.GetLocation(),
                severity,
                additionalLocations: null,
                properties: null));
        }

        public static bool HasExcessBlankLinesAfter(
            SourceText text, SyntaxToken token,
            out SyntaxToken secondBrace,
            out SyntaxTrivia endOfLineTrivia)
        {
            secondBrace = default;
            endOfLineTrivia = default;
            if (!token.IsKind(SyntaxKind.CloseBraceToken))
                return false;

            var nextToken = token.GetNextToken();
            if (!nextToken.IsKind(SyntaxKind.CloseBraceToken))
                return false;

            var firstBrace = token;
            secondBrace = nextToken;

            // two } tokens.  They need to be on the same line, or if they are not on subsequent lines, then there needs
            // to be more than whitespace between them.
            var lines = text.Lines;
            var firstBraceLine = lines.GetLineFromPosition(firstBrace.SpanStart).LineNumber;
            var secondBraceLine = lines.GetLineFromPosition(secondBrace.SpanStart).LineNumber;

            var lineCount = secondBraceLine - firstBraceLine;

            // if they're both on the same line, or one line apart, then there's no problem.
            if (lineCount <= 1)
                return false;

            // they're multiple lines apart.  This i not ok if those lines are all whitespace.
            for (var currentLine = firstBraceLine + 1; currentLine < secondBraceLine; currentLine++)
            {
                if (!IsAllWhitespace(lines[currentLine]))
                    return false;
            }

            endOfLineTrivia = secondBrace.LeadingTrivia.Last(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            return endOfLineTrivia != default;
        }

        private static bool IsAllWhitespace(TextLine textLine)
        {
            var text = textLine.Text!;
            for (var i = textLine.Start; i < textLine.End; i++)
            {
                if (!SyntaxFacts.IsWhitespace(text[i]))
                    return false;
            }

            return true;
        }
    }
}
