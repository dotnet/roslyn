﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NewLines.MultipleBlankLines
{
    internal abstract class AbstractMultipleBlankLinesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ISyntaxFacts _syntaxFacts;

        protected AbstractMultipleBlankLinesDiagnosticAnalyzer(ISyntaxFacts syntaxFacts)
            : base(IDEDiagnosticIds.MultipleBlankLinesDiagnosticId,
                   EnforceOnBuildValues.MultipleBlankLines,
                   CodeStyleOptions2.AllowMultipleBlankLines,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(AnalyzersResources.Avoid_multiple_blank_lines), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            _syntaxFacts = syntaxFacts;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetOption(CodeStyleOptions2.AllowMultipleBlankLines, context.Tree.Options.Language);
            if (option.Value)
                return;

            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;
            var root = tree.GetRoot(cancellationToken);

            Recurse(context, option.Notification.Severity, root, cancellationToken);
        }

        private void Recurse(
            SyntaxTreeAnalysisContext context,
            ReportDiagnostic severity,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Don't bother analyzing nodes that have syntax errors in them.
            if (node.ContainsDiagnostics)
                return;

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(context, severity, child.AsNode()!, cancellationToken);
                else if (child.IsToken)
                    CheckToken(context, severity, child.AsToken());
            }
        }

        private void CheckToken(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, SyntaxToken token)
        {
            if (token.ContainsDiagnostics)
                return;

            if (!ContainsMultipleBlankLines(token, out var badTrivia))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                Location.Create(badTrivia.SyntaxTree!, new TextSpan(badTrivia.SpanStart, 0)),
                severity,
                additionalLocations: ImmutableArray.Create(token.GetLocation()),
                properties: null));
        }

        private bool ContainsMultipleBlankLines(SyntaxToken token, out SyntaxTrivia firstBadTrivia)
        {
            var leadingTrivia = token.LeadingTrivia;
            for (var i = 0; i < leadingTrivia.Count; i++)
            {
                if (IsEndOfLine(leadingTrivia, i) &&
                    IsEndOfLine(leadingTrivia, i + 1))
                {
                    // Three cases that end up with two blank lines.
                    //
                    // 1. the token starts with two newlines.  This is definitely something to clean up.
                    // 2. we have two newlines after structured trivia (which itself ends with an newline).
                    // 3. we have three newlines (following non-structured trivia).

                    if (i == 0 ||
                        leadingTrivia[i - 1].HasStructure)
                    {
                        firstBadTrivia = leadingTrivia[i];
                        return true;
                    }

                    if (IsEndOfLine(leadingTrivia, i + 2))
                    {
                        // Report on the second newline.  This is for cases like:
                        //
                        //      // comment
                        //
                        //
                        //      public
                        //
                        // The first newline follows the comment.  But we want to report the issue on the start of the
                        // next line.
                        firstBadTrivia = leadingTrivia[i + 1];
                        return true;
                    }
                }
            }

            firstBadTrivia = default;
            return false;
        }

        private bool IsEndOfLine(SyntaxTriviaList triviaList, int index)
        {
            if (index >= triviaList.Count)
                return false;

            var trivia = triviaList[index];
            return _syntaxFacts.IsEndOfLineTrivia(trivia);
        }
    }
}
