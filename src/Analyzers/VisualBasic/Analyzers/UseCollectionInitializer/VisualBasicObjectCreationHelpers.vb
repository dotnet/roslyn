' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    Friend Module VisualBasicObjectCreationHelpers
        Public Function IsInitializerOfLocalDeclarationStatement(
                localDeclarationStatement As LocalDeclarationStatementSyntax,
                rootExpression As ObjectCreationExpressionSyntax,
                ByRef variableDeclarator As VariableDeclaratorSyntax) As Boolean

            For Each decl In localDeclarationStatement.Declarators
                Dim asClause = TryCast(decl.AsClause, AsNewClauseSyntax)
                If asClause?.NewExpression Is rootExpression OrElse decl.Initializer?.Value Is rootExpression Then
                    variableDeclarator = decl
                    Return True
                End If
            Next

            variableDeclarator = Nothing
            Return False
        End Function
    End Module
End Namespace
