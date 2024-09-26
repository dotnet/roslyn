' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    Friend NotInheritable Class VisualBasicCollectionInitializerAnalyzer
        Inherits AbstractUseCollectionInitializerAnalyzer(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides ReadOnly Property SyntaxHelper As IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax) =
            VisualBasicUpdateExpressionSyntaxHelper.Instance

        Protected Overrides Function IsInitializerOfLocalDeclarationStatement(localDeclarationStatement As LocalDeclarationStatementSyntax, rootExpression As ObjectCreationExpressionSyntax, ByRef variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return VisualBasicObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, variableDeclarator)
        End Function

        Protected Overrides Function IsComplexElementInitializer(expression As SyntaxNode) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function HasExistingInvalidInitializerForCollection() As Boolean
            ' In VB we cannot add a `From { }` initializer to an object if it already has a `With { }` initializer.
            Return TypeOf _objectCreationExpression.Initializer Is ObjectMemberInitializerSyntax
        End Function

        Protected Overrides Function AnalyzeMatchesAndCollectionConstructorForCollectionExpression(matches As ArrayBuilder(Of Match), cancellationToken As CancellationToken) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function
    End Class
End Namespace
