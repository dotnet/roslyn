// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;

using static SyntaxFactory;
using static AnalyzedPattern;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternCombinators), Shared]
internal class CSharpUsePatternCombinatorsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private const string SafeEquivalenceKey = nameof(CSharpUsePatternCombinatorsCodeFixProvider) + "_safe";
    private const string UnsafeEquivalenceKey = nameof(CSharpUsePatternCombinatorsCodeFixProvider) + "_unsafe";

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
        => [IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(
        Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
    {
        var isSafe = CSharpUsePatternCombinatorsDiagnosticAnalyzer.IsSafe(diagnostic);
        return isSafe == (equivalenceKey == SafeEquivalenceKey);
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var isSafe = CSharpUsePatternCombinatorsDiagnosticAnalyzer.IsSafe(diagnostic);

        RegisterCodeFix(
            context,
            isSafe ? CSharpAnalyzersResources.Use_pattern_matching : CSharpAnalyzersResources.Use_pattern_matching_may_change_code_meaning,
            isSafe ? SafeEquivalenceKey : UnsafeEquivalenceKey,
            CodeActionPriority.Low);

        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
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
                    [.. p.Token.GetAllTrailingTrivia()]),
                AsPatternSyntax(p.Right).Parenthesize()),
            Constant p => ConstantPattern(AsExpressionSyntax(p.ExpressionSyntax, p)),
            Source p => p.PatternSyntax,
            Type p => TypePattern(p.TypeSyntax),
            Relational p => RelationalPattern(Token(MapToSyntaxKind(p.OperatorKind)), AsExpressionSyntax(p.Value, p)),
            Not p => UnaryPattern(AsPatternSyntax(p.Pattern).Parenthesize()),
            var p => throw ExceptionUtilities.UnexpectedValue(p)
        };
    }

    private static ExpressionSyntax AsExpressionSyntax(ExpressionSyntax expr, AnalyzedPattern p)
    {
        var semanticModel = p.Target.SemanticModel;
        RoslynDebug.Assert(semanticModel != null);
        var type = semanticModel.GetTypeInfo(expr).Type;
        if (type != null)
        {
            // default literals are not permitted in patterns
            if (expr.IsKind(SyntaxKind.DefaultLiteralExpression))
                return DefaultExpression(type.GenerateTypeSyntax());

            // 'null' is already the right form in a pattern, it does not need to be casted to anything else.
            if (expr.IsKind(SyntaxKind.NullLiteralExpression))
                return expr;

            // if we have a nullable value type, only cast to the underlying type.
            //
            // `x is (long?)0` is not legal, only `x is (long)0` is.
            var governingType = semanticModel.GetTypeInfo(p.Target.Syntax).Type.RemoveNullableIfPresent();
            if (governingType != null && !governingType.Equals(type))
                return CastExpression(governingType.GenerateTypeSyntax(), expr.Parenthesize()).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        return expr.Parenthesize();
    }
}
