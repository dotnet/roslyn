﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;

using static CSharpSyntaxTokens;
using static SyntaxFactory;
using ObjectInitializerMatch = Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseObjectInitializer), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpUseObjectInitializerCodeFixProvider() :
    AbstractUseObjectInitializerCodeFixProvider<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseNamedMemberInitializerAnalyzer>
{
    protected override CSharpUseNamedMemberInitializerAnalyzer GetAnalyzer()
        => CSharpUseNamedMemberInitializerAnalyzer.Allocate();

    protected override ISyntaxFormatting SyntaxFormatting => CSharpSyntaxFormatting.Instance;

    protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

    protected override SyntaxTrivia Whitespace(string text)
        => SyntaxFactory.Whitespace(text);

    protected override StatementSyntax GetNewStatement(
        StatementSyntax statement,
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        return statement.ReplaceNode(
            objectCreation,
            GetNewObjectCreation(objectCreation, options, matches));
    }

    private BaseObjectCreationExpressionSyntax GetNewObjectCreation(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        return UseInitializerHelpers.GetNewObjectCreation(
            objectCreation,
            CreateExpressions(objectCreation, options, matches));
    }

    private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

        UseInitializerHelpers.AddExistingItems<ObjectInitializerMatch, ExpressionSyntax>(
            objectCreation, nodesAndTokens, addTrailingComma: true, static (_, e) => e);

        for (var i = 0; i < matches.Length; i++)
        {
            var match = matches[i];
            var expressionStatement = match.Statement;
            var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var trivia = match.MemberAccessExpression.GetLeadingTrivia();

            var newTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

            var newAssignment = assignment
                .WithLeft(match.MemberAccessExpression.Name.WithLeadingTrivia(newTrivia))
                .WithRight(Indent(assignment.Right, options));

            if (i < matches.Length - 1)
            {
                nodesAndTokens.Add(newAssignment);
                nodesAndTokens.Add(CommaToken.WithTriviaFrom(expressionStatement.SemicolonToken));
            }
            else
            {
                newAssignment = newAssignment.WithTrailingTrivia(
                    expressionStatement.GetTrailingTrivia());
                nodesAndTokens.Add(newAssignment);
            }
        }

        return SeparatedList<ExpressionSyntax>(nodesAndTokens);
    }
}
