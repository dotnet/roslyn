' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedParametersAndValuesDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(unusedValueExpressionStatementOption:=VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                       unusedValueAssignmentOption:=VisualBasicCodeStyleOptions.UnusedValueAssignment,
                       LanguageNames.VisualBasic)
        End Sub

        Protected Overrides Function IsRecordDeclaration(node As SyntaxNode) As Boolean
            Return False
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

        Protected Overrides Function IsExpressionOfExpressionBody(expressionStatementOperation As IExpressionStatementOperation) As Boolean
            ' VB does not support expression body
            Return False
        End Function

        Protected Overrides Function GetDefinitionLocationToFade(unusedDefinition As IOperation) As Location
            Return unusedDefinition.Syntax.GetLocation()
        End Function
    End Class
End Namespace
