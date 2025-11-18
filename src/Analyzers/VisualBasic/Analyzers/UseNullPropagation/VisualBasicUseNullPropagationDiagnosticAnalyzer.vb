' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseNullPropagation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUseNullPropagationDiagnosticAnalyzer
        Inherits AbstractUseNullPropagationDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            ExecutableStatementSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            MultiLineIfBlockSyntax,
            ExpressionStatementSyntax)

        Public Shared ReadOnly Instance As New VisualBasicUseNullPropagationDiagnosticAnalyzer()

        Protected Overrides ReadOnly Property IfStatementSyntaxKind As SyntaxKind = SyntaxKind.MultiLineIfBlock

        Protected Overrides ReadOnly Property SemanticFacts As ISemanticFacts = VisualBasicSemanticFacts.Instance

        Protected Overrides Function ShouldAnalyze(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function TryAnalyzePatternCondition(syntaxFacts As ISyntaxFacts, conditionNode As ExpressionSyntax, ByRef conditionPartToCheck As ExpressionSyntax, ByRef isEquals As Boolean) As Boolean
            ' VB does not support patterns.
            conditionPartToCheck = Nothing
            isEquals = False
            Return False
        End Function

        Protected Overrides Function TryGetPartsOfIfStatement(
                ifStatement As MultiLineIfBlockSyntax,
                ByRef condition As ExpressionSyntax,
                ByRef trueStatements As ImmutableArray(Of ExecutableStatementSyntax)) As Boolean

            condition = ifStatement.IfStatement.Condition

            If ifStatement.ElseBlock IsNot Nothing Then
                Return False
            End If

            If ifStatement.ElseIfBlocks.Count > 0 Then
                Return False
            End If

            If ifStatement.Statements.Count > 2 Then
                Return False
            End If

            If Not ifStatement.Statements.All(Function(s) TypeOf s Is ExecutableStatementSyntax) Then
                Return False
            End If

            trueStatements = ifStatement.Statements.Cast(Of ExecutableStatementSyntax)().ToImmutableArrayOrEmpty()
            Return True
        End Function
    End Class
End Namespace
