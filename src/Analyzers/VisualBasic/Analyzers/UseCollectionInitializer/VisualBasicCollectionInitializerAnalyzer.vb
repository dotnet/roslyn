' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            ForEachStatementSyntax,
            IfStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides Sub GetPartsOfForeachStatement(statement As ForEachStatementSyntax, ByRef identifier As SyntaxToken, ByRef expression As ExpressionSyntax, ByRef statements As IEnumerable(Of StatementSyntax))
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Sub

        Protected Overrides Sub GetPartsOfIfStatement(statement As IfStatementSyntax, ByRef condition As ExpressionSyntax, ByRef whenTrueStatements As IEnumerable(Of StatementSyntax), ByRef whenFalseStatements As IEnumerable(Of StatementSyntax))
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Sub

        Protected Overrides Function IsComplexElementInitializer(expression As SyntaxNode) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function HasExistingInvalidInitializerForCollection(objectCreation As ObjectCreationExpressionSyntax) As Boolean
            ' In VB we cannot add a `From { }` initializer to an object if it already has a `With { }` initializer.
            Return TypeOf objectCreation.Initializer Is ObjectMemberInitializerSyntax
        End Function
    End Class
End Namespace
