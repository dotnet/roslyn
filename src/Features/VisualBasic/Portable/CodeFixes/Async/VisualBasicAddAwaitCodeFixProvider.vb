﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Async
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddAwait), [Shared]>
    Friend Class VisualBasicAddAwaitCodeFixProvider
        Inherits AbstractAddAwaitCodeFixProvider

        Friend Const BC30311 As String = "BC30311" ' error BC30311: Value of type 'X' cannot be converted to 'Y'.
        Friend Const BC37055 As String = "BC37055" ' error BC37055: Since this is an async method, the return expression must be of type 'blah' rather than 'baz'
        Friend Const BC42358 As String = "BC42358" ' error BC42358: Because this call is not awaited, execution of the current method continues before the call is completed.

        Friend ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC30311, BC37055, BC42358)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Async Function GetDescriptionAndNodeAsync(root As SyntaxNode, oldNode As SyntaxNode, semanticModel As SemanticModel, diagnostic As Diagnostic, document As Document, cancellationToken As CancellationToken) As Task(Of DescriptionAndNode)
            Dim newRoot = Await GetNewRootAsync(
                root, oldNode, semanticModel, diagnostic, document, cancellationToken).ConfigureAwait(False)
            If newRoot Is Nothing Then
                Return Nothing
            End If

            Return New DescriptionAndNode(VBFeaturesResources.Insert_Await, newRoot)
        End Function

        Private Function GetNewRootAsync(root As SyntaxNode, oldNode As SyntaxNode, semanticModel As SemanticModel, diagnostic As Diagnostic, document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim expression = TryCast(oldNode, ExpressionSyntax)
            If expression Is Nothing Then
                Return SpecializedTasks.Null(Of SyntaxNode)()
            End If

            Select Case diagnostic.Id
                Case BC30311
                    If Not DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(expression, semanticModel, document.Project, cancellationToken) Then
                        Return Task.FromResult(Of SyntaxNode)(Nothing)
                    End If
                    Return Task.FromResult(root.ReplaceNode(oldNode, ConverToAwaitExpression(expression, semanticModel, cancellationToken)))
                Case BC37055
                    If Not DoesExpressionReturnTask(expression, semanticModel) Then
                        Return Task.FromResult(Of SyntaxNode)(Nothing)
                    End If
                    Return Task.FromResult(root.ReplaceNode(oldNode, ConverToAwaitExpression(expression, semanticModel, cancellationToken)))
                Case BC42358
                    Return Task.FromResult(root.ReplaceNode(oldNode, ConverToAwaitExpression(expression, semanticModel, cancellationToken)))
                Case Else
                    Return SpecializedTasks.Null(Of SyntaxNode)()
            End Select
        End Function

        Private Function DoesExpressionReturnGenericTaskWhoseArgumentsMatchLeftSide(expression As ExpressionSyntax, semanticModel As SemanticModel, project As Project, cancellationToken As CancellationToken) As Boolean
            If Not IsInAsyncBlock(expression) Then
                Return False
            End If

            Dim taskType As INamedTypeSymbol = Nothing
            Dim rightSideType As INamedTypeSymbol = Nothing
            If Not TryGetTaskType(semanticModel, taskType) OrElse
               Not TryGetExpressionType(expression, semanticModel, rightSideType) Then
                Return False
            End If

            Dim compilation = semanticModel.Compilation
            If Not compilation.ClassifyConversion(taskType, rightSideType).Exists Then
                Return False
            End If

            If Not rightSideType.IsGenericType Then
                Return False
            End If

            Dim typeArguments = rightSideType.TypeArguments
            Dim typeInferer = project.LanguageServices.GetService(Of ITypeInferenceService)
            Dim inferredTypes = typeInferer.InferTypes(semanticModel, expression, cancellationToken)
            Return typeArguments.Any(Function(ta) inferredTypes.Any(Function(it) compilation.ClassifyConversion(it, ta).Exists))
        End Function

        Private Function IsInAsyncBlock(expression As ExpressionSyntax) As Boolean

            For Each ancestor In expression.Ancestors
                Select Case ancestor.Kind
                    Case SyntaxKind.MultiLineFunctionLambdaExpression,
                         SyntaxKind.MultiLineSubLambdaExpression,
                         SyntaxKind.SingleLineFunctionLambdaExpression,
                         SyntaxKind.SingleLineSubLambdaExpression
                        Dim result = TryCast(ancestor, LambdaExpressionSyntax)?.SubOrFunctionHeader?.Modifiers.Any(SyntaxKind.AsyncKeyword)
                        Return result.HasValue AndAlso result.Value
                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock
                        Dim result = TryCast(ancestor, MethodBlockBaseSyntax)?.BlockStatement?.Modifiers.Any(SyntaxKind.AsyncKeyword)
                        Return result.HasValue AndAlso result.Value
                    Case Else
                        Continue For
                End Select
            Next
            Return False
        End Function

        Private Function DoesExpressionReturnTask(expression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Dim taskType As INamedTypeSymbol = Nothing
            Dim returnType As INamedTypeSymbol = Nothing
            Return TryGetTaskType(semanticModel, taskType) AndAlso
                   TryGetExpressionType(expression, semanticModel, returnType) AndAlso
                semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists
        End Function

        Private Shared Function ConverToAwaitExpression(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ExpressionSyntax
            Return SyntaxFactory.AwaitExpression(expression.WithoutTrivia().Parenthesize()) _
                                .WithTriviaFrom(expression) _
                                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation)
        End Function

    End Class
End Namespace
