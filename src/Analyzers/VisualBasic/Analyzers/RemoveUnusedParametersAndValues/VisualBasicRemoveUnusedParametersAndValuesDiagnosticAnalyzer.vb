' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedParametersAndValuesDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(unusedValueExpressionStatementOption:=VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                       unusedValueAssignmentOption:=VisualBasicCodeStyleOptions.UnusedValueAssignment)
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetUnusedValueExpressionStatementOption(provider As AnalyzerOptionsProvider) As CodeStyleOption2(Of UnusedValuePreference)
            Return CType(provider, VisualBasicAnalyzerOptionsProvider).UnusedValueExpressionStatement
        End Function

        Protected Overrides Function GetUnusedValueAssignmentOption(provider As AnalyzerOptionsProvider) As CodeStyleOption2(Of UnusedValuePreference)
            Return CType(provider, VisualBasicAnalyzerOptionsProvider).UnusedValueAssignment
        End Function

        Protected Overrides Function SupportsDiscard(tree As SyntaxTree) As Boolean
            Return False
        End Function

        Protected Overrides Function MethodHasHandlesClause(method As IMethodSymbol) As Boolean
            Return method.DeclaringSyntaxReferences().Any(Function(decl)
                                                              Return TryCast(decl.GetSyntax(), MethodStatementSyntax)?.HandlesClause IsNot Nothing
                                                          End Function)
        End Function

        Protected Overrides Function IsIfConditionalDirective(node As SyntaxNode) As Boolean
            Return TryCast(node, IfDirectiveTriviaSyntax) IsNot Nothing
        End Function

        Protected Overrides Function IsCallStatement(expressionStatement As IExpressionStatementOperation) As Boolean
            Return TryCast(expressionStatement.Syntax, CallStatementSyntax) IsNot Nothing
        End Function

        Protected Overrides Function ReturnsThrow(node As SyntaxNode) As Boolean
            Dim methodStatementSyntax = TryCast(node, MethodBaseSyntax)
            If methodStatementSyntax IsNot Nothing Then
                Dim methodSyntax = TryCast(node.Parent, MethodBlockBaseSyntax)
                If methodSyntax.BlockStatement Is Nothing Then
                    Return False
                End If

                If methodSyntax.Statements.Count = 1 Then
                    Return TryCast(methodSyntax.Statements.First(), ThrowStatementSyntax) IsNot Nothing
                End If
            End If

            Return False
        End Function

        Protected Overrides Function IsExpressionOfExpressionBody(expressionStatementOperation As IExpressionStatementOperation) As Boolean
            ' VB does not support expression body
            Return False
        End Function

        Protected Overrides Function GetDefinitionLocationToFade(unusedDefinition As IOperation) As Location
            Return unusedDefinition.Syntax.GetLocation()
        End Function
    End Class
End Namespace
