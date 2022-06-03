// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ReplaceConditionalWithStatements;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceConditionalWithStatements;

internal class CSharpReplaceConditionalWithStatementsCodeRefactoringProvider :
    AbstractReplaceConditionalWithStatementsCodeRefactoringProvider<
        ExpressionSyntax,
        ConditionalExpressionSyntax,
        StatementSyntax,
        ReturnStatementSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        VariableDeclaratorSyntax>
{
    protected override bool HasSingleVariable(
        LocalDeclarationStatementSyntax localDeclarationStatement,
        [NotNullWhen(true)] out VariableDeclaratorSyntax? variable)
    {
        if (localDeclarationStatement.Declaration.Variables.Count == 1)
        {
            variable = localDeclarationStatement.Declaration.Variables[0];
            return true;
        }

        variable = null;
        return false;
    }

    protected override LocalDeclarationStatementSyntax GetUpdatedLocalDeclarationStatement(
        SyntaxGenerator generator,
        LocalDeclarationStatementSyntax localDeclarationStatement,
        ILocalSymbol symbol)
    {
        var type = 
        if (localDeclarationStatement.Declaration.Type.IsVar)
        {
            editor.ReplaceNode(
                localDeclarationStatement.Declaration.Type,
                symbol.Type.GenerateTypeSyntax(allowVar: false).WithTriviaFrom(localDeclarationStatement.Declaration.Type));
        }

        var variable = localDeclarationStatement.Declaration.Variables[0];
        editor.ReplaceNode(
            variable,
            variable.WithInitializer(null).WithIdentifier(variable.Identifier.WithoutTrailingTrivia()));

        return (localDeclarationStatement)editor.GetChangedRoot();
    }
}
