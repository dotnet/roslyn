﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.UseIsNullCheck
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseIsNotExpressionCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(IDEDiagnosticIds.UseIsNotExpressionDiagnosticId)

        Friend Overrides ReadOnly Property CodeFixCategory As CodeFixCategory = CodeFixCategory.CodeStyle

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            context.RegisterCodeFix(New MyCodeAction(
                Function(c) FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics)
            Return Task.CompletedTask
        End Function

        Protected Overrides Function FixAllAsync(
                document As Document,
                diagnostics As ImmutableArray(Of Diagnostic),
                editor As SyntaxEditor,
                cancellationToken As CancellationToken) As Task

            For Each diagnostic In diagnostics
                cancellationToken.ThrowIfCancellationRequested()
                ProcessDiagnostic(editor, diagnostic, cancellationToken)
            Next

            Return Task.CompletedTask
        End Function

        Private Shared Sub ProcessDiagnostic(
                editor As SyntaxEditor,
                diagnostic As Diagnostic,
                cancellationToken As CancellationToken)
            Dim notExpressionLocation = diagnostic.AdditionalLocations(0)

            Dim notExpression = DirectCast(notExpressionLocation.FindNode(getInnermostNodeForTie:=True, cancellationToken), UnaryExpressionSyntax)
            Dim operand = notExpression.Operand

            Dim replacement As ExpressionSyntax
            If operand.IsKind(SyntaxKind.IsExpression) Then
                Dim isExpression = DirectCast(operand, BinaryExpressionSyntax)
                replacement = SyntaxFactory.IsNotExpression(
                    isExpression.Left,
                    SyntaxFactory.Token(SyntaxKind.IsNotKeyword).WithTriviaFrom(isExpression.OperatorToken),
                    isExpression.Right)
            Else
                Contract.ThrowIfFalse(operand.IsKind(SyntaxKind.TypeOfIsExpression))
                Dim typeOfIsExpression = DirectCast(operand, TypeOfExpressionSyntax)
                replacement = SyntaxFactory.TypeOfIsNotExpression(
                    typeOfIsExpression.TypeOfKeyword,
                    typeOfIsExpression.Expression,
                    SyntaxFactory.Token(SyntaxKind.IsNotKeyword).WithTriviaFrom(typeOfIsExpression.OperatorToken),
                    typeOfIsExpression.Type)
            End If

            editor.ReplaceNode(
                notExpression,
                replacement.WithPrependedLeadingTrivia(notExpression.GetLeadingTrivia()))
        End Sub

        Private Class MyCodeAction
            Inherits CustomCodeActions.DocumentChangeAction

            Public Sub New(createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(VisualBasicAnalyzersResources.Use_IsNot_expression, createChangedDocument, VisualBasicAnalyzersResources.Use_IsNot_expression)
            End Sub
        End Class
    End Class
End Namespace
