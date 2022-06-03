// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplaceConditionalWithStatements;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceConditionalWithStatements;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ReplaceConditionalWithStatements), Shared]
internal class CSharpReplaceConditionalWithStatementsCodeRefactoringProvider :
    AbstractReplaceConditionalWithStatementsCodeRefactoringProvider<
        ExpressionSyntax,
        ConditionalExpressionSyntax,
        StatementSyntax,
        ReturnStatementSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        VariableDeclaratorSyntax,
        EqualsValueClauseSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpReplaceConditionalWithStatementsCodeRefactoringProvider()
    {
    }

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
        // If we have `var x = a ? b : c;`
        // then we have to replace `var` with the actual type of the local when breaking this into multiple statements.
        var type = localDeclarationStatement.Declaration.Type;
        if (type.IsVar)
        {
            localDeclarationStatement = localDeclarationStatement.ReplaceNode(
                type, symbol.Type.GenerateTypeSyntax(allowVar: false).WithTriviaFrom(type));
        }

        var variable = localDeclarationStatement.Declaration.Variables[0];
        return localDeclarationStatement.ReplaceNode(
            variable,
            variable.WithInitializer(null).WithIdentifier(variable.Identifier.WithTrailingTrivia()));
    }
}
