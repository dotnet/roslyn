// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
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
        InitializerExpressionSyntax,
        CSharpUseNamedMemberInitializerAnalyzer>
{
    protected override bool IsInitializerOfLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement, BaseObjectCreationExpressionSyntax rootExpression, [NotNullWhen(true)] out VariableDeclaratorSyntax? variableDeclarator)
        => CSharpObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, out variableDeclarator);
}
