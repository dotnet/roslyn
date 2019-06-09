' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.QualifyMemberAccess
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicQualifyMemberAccessDiagnosticAnalyzer
        Inherits AbstractQualifyMemberAccessDiagnosticAnalyzer(Of SyntaxKind, ExpressionSyntax, SimpleNameSyntax)

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function IsAlreadyQualifiedMemberAccess(node As ExpressionSyntax) As Boolean
            Return node.IsKind(SyntaxKind.MeExpression)
        End Function

        Protected Overrides Function CanMemberAccessBeQualified(containingSymbol As ISymbol, node As SyntaxNode) As Boolean
            ' if we're in an attribute, we can't be qualified.
            If node.GetAncestorOrThis(Of AttributeSyntax) IsNot Nothing Then
                Return False
            End If

            ' If the member is already qualified with `MyBase.`, or `MyClass.`,
            ' or member is in object initialization context, it cannot be qualified.
            Return Not (
                node.IsKind(SyntaxKind.MyBaseExpression) OrElse
                node.IsKind(SyntaxKind.MyClassExpression) OrElse
                node.IsKind(SyntaxKind.ObjectCreationExpression))
        End Function

        Protected Overrides Function GetLocation(operation As IOperation) As Location
            Dim unaryExpressionSyntax As UnaryExpressionSyntax = TryCast(operation.Syntax, UnaryExpressionSyntax)
            If unaryExpressionSyntax IsNot Nothing AndAlso unaryExpressionSyntax.OperatorToken.Kind() = SyntaxKind.AddressOfKeyword Then
                Return unaryExpressionSyntax.Operand.GetLocation()
            End If

            Return operation.Syntax.GetLocation()
        End Function

    End Class
End Namespace
