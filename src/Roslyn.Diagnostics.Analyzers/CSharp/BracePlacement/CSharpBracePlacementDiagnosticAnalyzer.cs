// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BracePlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpBracePlacementDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(
            nameof(RoslynDiagnosticsAnalyzersResources.BracePlacementMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.BracePlacementRuleId,
            s_localizableMessage,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableMessage,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var stack = ArrayBuilder<SyntaxNode>.GetInstance();
            try
            {
                Recurse(context, stack);
            }
            finally
            {
                stack.Free();
            }
        }

        private static void Recurse(SyntaxTreeAnalysisContext context, ArrayBuilder<SyntaxNode> stack)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var root = tree.GetRoot(cancellationToken);
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
                    if (child.IsNode)
                        stack.Add(child.AsNode());
                    else if (child.IsToken)
                        ProcessToken(context, text, child.AsToken());
                }
            }
        }

        private static void ProcessToken(SyntaxTreeAnalysisContext context, SourceText text, SyntaxToken token)
        {
            if (!HasExcessBlankLinesAfter(text, token, out var secondBrace, out _))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                secondBrace.GetLocation()));
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
            for (var currentLine = firstBraceLine + 1; currentLine < secondBraceLine - 1; currentLine++)
            {
                if (!IsAllWhitespace(lines[currentLine]))
                    return false;
            }

            endOfLineTrivia = secondBrace.LeadingTrivia.Last(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            return endOfLineTrivia != default;
        }

        private static bool IsAllWhitespace(TextLine textLine)
        {
            var text = textLine.Text;
            for (var i = textLine.Start; i < textLine.End; i++)
            {
                if (!SyntaxFacts.IsWhitespace(text[i]))
                    return false;
            }

            return true;
        }
    }
}
