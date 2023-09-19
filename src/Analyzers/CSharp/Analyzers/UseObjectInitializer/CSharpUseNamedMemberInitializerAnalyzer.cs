// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;

internal sealed class CSharpUseNamedMemberInitializerAnalyzer :
    AbstractUseNamedMemberInitializerAnalyzer<
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseNamedMemberInitializerAnalyzer>
{
    protected override bool IsInitializerOfLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement, BaseObjectCreationExpressionSyntax rootExpression, out VariableDeclaratorSyntax variableDeclarator)
    {
        throw new System.NotImplementedException();
    }
}
