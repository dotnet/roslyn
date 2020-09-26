// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    using static SyntaxFactory;
    using static AnalyzedPattern;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUsePatternCombinatorsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUsePatternCombinatorsCodeFixProvider()
        {
        }

        private static SyntaxKind MapToSyntaxKind(BinaryOperatorKind kind)
        {
            return kind switch
            {
                BinaryOperatorKind.LessThan => SyntaxKind.LessThanToken,
                BinaryOperatorKind.GreaterThan => SyntaxKind.GreaterThanToken,
                BinaryOperatorKind.LessThanOrEqual => SyntaxKind.LessThanEqualsToken,
                BinaryOperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanEqualsToken,
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var location = diagnostic.Location;
                var expression = editor.OriginalRoot.FindNode(location.SourceSpan, getInnermostNodeForTie: true);
                var operation = semanticModel.GetOperation(expression, cancellationToken);
                RoslynDebug.AssertNotNull(operation);
                var pattern = CSharpUsePatternCombinatorsAnalyzer.Analyze(operation);
                RoslynDebug.AssertNotNull(pattern);
                var patternSyntax = AsPatternSyntax(pattern).WithAdditionalAnnotations(Formatter.Annotation);
                editor.ReplaceNode(expression, IsPatternExpression((ExpressionSyntax)pattern.Target.Syntax, patternSyntax));
            }
        }

        private static PatternSyntax AsPatternSyntax(AnalyzedPattern pattern)
        {
            return pattern switch
            {
                Binary p => BinaryPattern(
                    p.IsDisjunctive ? SyntaxKind.OrPattern : SyntaxKind.AndPattern,
                    AsPatternSyntax(p.Left).Parenthesize(),
                    Token(p.Token.LeadingTrivia, p.IsDisjunctive ? SyntaxKind.OrKeyword : SyntaxKind.AndKeyword,
                        TriviaList(p.Token.GetAllTrailingTrivia())),
                    AsPatternSyntax(p.Right).Parenthesize()),
                Constant p => ConstantPattern(AsExpressionSyntax(p)),
                Source p => p.PatternSyntax,
                Type p => TypePattern(p.TypeSyntax),
                Relational p => RelationalPattern(Token(MapToSyntaxKind(p.OperatorKind)), p.Value.Parenthesize()),
                Not p => UnaryPattern(AsPatternSyntax(p.Pattern).Parenthesize()),
                var p => throw ExceptionUtilities.UnexpectedValue(p)
            };
        }

        private static ExpressionSyntax AsExpressionSyntax(Constant constant)
        {
            var expr = constant.ExpressionSyntax;
            if (expr.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                // default literals are not permitted in patterns
                var convertedType = constant.Target.SemanticModel.GetTypeInfo(expr).ConvertedType;
                if (convertedType != null)
                {
                    return DefaultExpression(convertedType.GenerateTypeSyntax());
                }
            }

            return expr.Parenthesize();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_pattern_matching, createChangedDocument)
            {
            }

            internal override CodeActionPriority Priority => CodeActionPriority.Low;
        }
    }
}
