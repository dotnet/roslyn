// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternMatchingAsAndNullCheck), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpAsAndNullCheckCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.InlineAsTypeCheckId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_pattern_matching, nameof(CSharpAnalyzersResources.Use_pattern_matching));
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        using var _1 = PooledHashSet<Location>.GetInstance(out var declaratorLocations);
        using var _2 = PooledHashSet<SyntaxNode>.GetInstance(out var statementParentScopes);

        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var languageVersion = tree.Options.LanguageVersion();

        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (declaratorLocations.Add(diagnostic.AdditionalLocations[0]))
                AddEdits(editor, semanticModel, diagnostic, languageVersion, RemoveStatement, cancellationToken);
        }

        foreach (var parentScope in statementParentScopes)
        {
            editor.ReplaceNode(parentScope, (newParentScope, syntaxGenerator) =>
            {
                var firstStatement = newParentScope is BlockSyntax
                    ? ((BlockSyntax)newParentScope).Statements.First()
                    : ((SwitchSectionSyntax)newParentScope).Statements.First();
                return syntaxGenerator.ReplaceNode(newParentScope, firstStatement, firstStatement.WithoutLeadingBlankLinesInTrivia());
            });
        }

        return;

        void RemoveStatement(StatementSyntax statement)
        {
            editor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
            if (statement.Parent is BlockSyntax or SwitchSectionSyntax)
            {
                statementParentScopes.Add(statement.Parent);
            }
        }
    }

    private static void AddEdits(
        SyntaxEditor editor,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        LanguageVersion languageVersion,
        Action<StatementSyntax> removeStatement,
        CancellationToken cancellationToken)
    {
        var declaratorLocation = diagnostic.AdditionalLocations[0];
        var comparisonLocation = diagnostic.AdditionalLocations[1];
        var asExpressionLocation = diagnostic.AdditionalLocations[2];

        var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
        var comparison = (ExpressionSyntax)comparisonLocation.FindNode(cancellationToken);
        var asExpression = (BinaryExpressionSyntax)asExpressionLocation.FindNode(cancellationToken);

        var rightSideOfComparison = comparison is BinaryExpressionSyntax binaryExpression
            ? (SyntaxNode)binaryExpression.Right
            : ((IsPatternExpressionSyntax)comparison).Pattern;
        var newIdentifier = declarator.Identifier
            .WithoutTrivia().WithTrailingTrivia(rightSideOfComparison.GetTrailingTrivia());

        var declarationPattern = DeclarationPattern(
            GetPatternType().WithoutTrivia().WithTrailingTrivia(ElasticMarker),
            SingleVariableDesignation(newIdentifier));

        var condition = GetCondition(languageVersion, comparison, asExpression, declarationPattern);

        if (declarator.Parent is VariableDeclarationSyntax declaration &&
            declaration.Parent is LocalDeclarationStatementSyntax localDeclaration &&
            declaration.Variables.Count == 1)
        {
            // Trivia on the local declaration will move to the next statement.
            // use the callback form as the next statement may be the place where we're
            // inlining the declaration, and thus need to see the effects of that change.
            editor.ReplaceNode(
                localDeclaration.GetNextStatement()!,
                (s, g) => s.WithPrependedNonIndentationTriviaFrom(localDeclaration)
                           .WithAdditionalAnnotations(Formatter.Annotation));

            removeStatement(localDeclaration);
        }
        else
        {
            editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
        }

        editor.ReplaceNode(comparison, condition.WithTriviaFrom(comparison));

        return;

        TypeSyntax GetPatternType()
        {
            // Complex case: object?[]? arr = obj as object[];
            //
            // Because of array variance, the above is legal.  We want the `object?[]` from the LHS here.
            if (semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is ILocalSymbol local)
            {
                var asExpressionTypeInfo = semanticModel.GetTypeInfo(asExpression, cancellationToken);
                if (asExpressionTypeInfo.Type != null)
                {
                    // Strip off the outer ? if present.  But the inner ? will still be there.
                    var localType = local.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                    var asType = asExpressionTypeInfo.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

                    // If they're the same types, except for the inner ?, then use the local's type here.
                    if (SymbolEqualityComparer.Default.Equals(localType, asType) &&
                        !SymbolEqualityComparer.IncludeNullability.Equals(localType, asType))
                    {
                        return localType.GenerateTypeSyntax(allowVar: false);
                    }
                }
            }

            return (TypeSyntax)asExpression.Right;
        }
    }

    private static ExpressionSyntax GetCondition(
        LanguageVersion languageVersion,
        ExpressionSyntax comparison,
        BinaryExpressionSyntax asExpression,
        DeclarationPatternSyntax declarationPattern)
    {
        var isPatternExpression = IsPatternExpression(asExpression.Left, declarationPattern);

        // We should negate the is-expression if we have something like "x == null" or "x is null"
        if (comparison.Kind() is not (SyntaxKind.EqualsExpression or SyntaxKind.IsPatternExpression))
            return isPatternExpression;

        if (languageVersion >= LanguageVersion.CSharp9)
        {
            // In C# 9 and higher, convert to `x is not string s`.
            return isPatternExpression.WithPattern(
                UnaryPattern(NotKeyword, isPatternExpression.Pattern));
        }

        // In C# 8 and lower, convert to `!(x is string s)`
        return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, isPatternExpression.Parenthesize());
    }
}
