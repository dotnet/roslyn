// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal sealed class CSharpUseCollectionInitializerAnalyzer : AbstractUseCollectionInitializerAnalyzer<
    ExpressionSyntax,
    StatementSyntax,
    BaseObjectCreationExpressionSyntax,
    MemberAccessExpressionSyntax,
    InvocationExpressionSyntax,
    ExpressionStatementSyntax,
    LocalDeclarationStatementSyntax,
    VariableDeclaratorSyntax,
    CSharpUseCollectionInitializerAnalyzer>
{
    protected override IUpdateExpressionSyntaxHelper<ExpressionSyntax, StatementSyntax> SyntaxHelper
        => CSharpUpdateExpressionSyntaxHelper.Instance;

    protected override bool IsInitializerOfLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement, BaseObjectCreationExpressionSyntax rootExpression, [NotNullWhen(true)] out VariableDeclaratorSyntax? variableDeclarator)
        => CSharpObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, out variableDeclarator);

    protected override bool IsComplexElementInitializer(SyntaxNode expression)
        => expression.IsKind(SyntaxKind.ComplexElementInitializerExpression);

    protected override bool HasExistingInvalidInitializerForCollection(BaseObjectCreationExpressionSyntax objectCreation)
    {
        // Can't convert to a collection expression if it already has an object-initializer.  Note, we do allow
        // conversion of empty `{ }` initializer.  So we only block if the expression count is more than zero.
        return objectCreation.Initializer is InitializerExpressionSyntax
        {
            RawKind: (int)SyntaxKind.ObjectInitializerExpression,
            Expressions.Count: > 0,
        };
    }
}
