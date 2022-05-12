' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.QualifyMemberAccess
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicQualifyMemberAccessDiagnosticAnalyzer
        Inherits AbstractQualifyMemberAccessDiagnosticAnalyzer(Of SyntaxKind, ExpressionSyntax, SimpleNameSyntax, VisualBasicSimplifierOptions)

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetSimplifierOptions(options As AnalyzerOptions, syntaxTree As SyntaxTree) As VisualBasicSimplifierOptions
            Return options.GetVisualBasicSimplifierOptions(syntaxTree)
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
