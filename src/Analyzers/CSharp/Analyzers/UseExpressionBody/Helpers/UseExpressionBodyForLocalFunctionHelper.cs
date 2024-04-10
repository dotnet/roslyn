// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

internal class UseExpressionBodyForLocalFunctionHelper :
    UseExpressionBodyHelper<LocalFunctionStatementSyntax>
{
    public static readonly UseExpressionBodyForLocalFunctionHelper Instance = new();

    private UseExpressionBodyForLocalFunctionHelper()
        : base(IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId,
               EnforceOnBuildValues.UseExpressionBodyForLocalFunctions,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_local_function), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_local_function), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
               [SyntaxKind.LocalFunctionStatement])
    {
    }

    public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
        => options.PreferExpressionBodiedLocalFunctions;

    protected override BlockSyntax GetBody(LocalFunctionStatementSyntax statement)
        => statement.Body;

    protected override ArrowExpressionClauseSyntax GetExpressionBody(LocalFunctionStatementSyntax statement)
        => statement.ExpressionBody;

    protected override SyntaxToken GetSemicolonToken(LocalFunctionStatementSyntax statement)
        => statement.SemicolonToken;

    protected override LocalFunctionStatementSyntax WithSemicolonToken(LocalFunctionStatementSyntax statement, SyntaxToken token)
        => statement.WithSemicolonToken(token);

    protected override LocalFunctionStatementSyntax WithExpressionBody(LocalFunctionStatementSyntax statement, ArrowExpressionClauseSyntax expressionBody)
        => statement.WithExpressionBody(expressionBody);

    protected override LocalFunctionStatementSyntax WithBody(LocalFunctionStatementSyntax statement, BlockSyntax body)
        => statement.WithBody(body);

    protected override bool CreateReturnStatementForExpression(
        SemanticModel semanticModel, LocalFunctionStatementSyntax statement)
    {
        if (statement.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // if it's 'async TaskLike' (where TaskLike is non-generic) we do *not* want to
            // create a return statement.  This is just the 'async' version of a 'void' local function.
            var symbol = semanticModel.GetDeclaredSymbol(statement);
            return symbol is IMethodSymbol methodSymbol &&
                methodSymbol.ReturnType is INamedTypeSymbol namedType &&
                namedType.Arity != 0;
        }

        return !statement.ReturnType.IsVoid();
    }
}
