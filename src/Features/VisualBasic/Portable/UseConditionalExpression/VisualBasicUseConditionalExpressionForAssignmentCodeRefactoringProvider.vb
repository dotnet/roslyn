﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression

    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseConditionalExpressionForAssignmentCodeRefactoringProvider
        Inherits AbstractUseConditionalExpressionForAssignmentCodeFixProvider(Of
            StatementSyntax, MultiLineIfBlockSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ExpressionSyntax, TernaryConditionalExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMultiLineFormattingRule() As AbstractFormattingRule
            Return MultiLineConditionalExpressionFormattingRule.Instance
        End Function

        Protected Overrides Function WithInitializer(variable As VariableDeclaratorSyntax, value As ExpressionSyntax) As VariableDeclaratorSyntax
            Return variable.WithoutTrivia().WithInitializer(SyntaxFactory.EqualsValue(value)).
                                            WithTriviaFrom(variable)
        End Function

        Protected Overrides Function GetDeclaratorSyntax(declarator As IVariableDeclaratorOperation) As VariableDeclaratorSyntax
            Return DirectCast(declarator.Syntax.Parent, VariableDeclaratorSyntax)
        End Function

        Protected Overrides Function AddSimplificationToType(statement As LocalDeclarationStatementSyntax) As LocalDeclarationStatementSyntax
            Dim declarator = statement.Declarators(0)
            Return statement.ReplaceNode(declarator, declarator.WithAdditionalAnnotations(Simplifier.Annotation))
        End Function

        Protected Overrides Function WrapWithBlockIfAppropriate(ifStatement As MultiLineIfBlockSyntax, statement As StatementSyntax) As StatementSyntax
            Return statement
        End Function
    End Class
End Namespace
