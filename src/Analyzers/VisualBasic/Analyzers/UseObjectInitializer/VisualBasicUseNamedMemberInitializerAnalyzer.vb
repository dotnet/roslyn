' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    Friend NotInheritable Class VisualBasicUseNamedMemberInitializerAnalyzer
        Inherits AbstractUseNamedMemberInitializerAnalyzer(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            ObjectMemberInitializerSyntax,
            VisualBasicUseNamedMemberInitializerAnalyzer)

        Protected Overrides Function IsInitializerOfLocalDeclarationStatement(localDeclarationStatement As LocalDeclarationStatementSyntax, rootExpression As ObjectCreationExpressionSyntax, ByRef variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return VisualBasicObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, variableDeclarator)
        End Function
    End Class
End Namespace
