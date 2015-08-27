' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Async
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Resources = Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddAwait), [Shared]>
    Friend Class VisualBasicAddAwaitCodeFixProvider
        Inherits AbstractAddAsyncAwaitCodeFixProvider

        Friend Const BC37055 As String = "BC37055" ' error BC37055: Since this is an async method, the return expression must be of type 'blah' rather than 'baz'
        Friend Const BC42358 As String = "BC42358" ' error BC42358: Because this call is not awaited, execution of the current method continues before the call is completed.

        Friend ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC37055, BC42358)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Function GetDescription(diagnostic As Diagnostic, node As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As String
            Return Resources.InsertAwait
        End Function

        Protected Overrides Function GetNewRoot(root As SyntaxNode, oldNode As SyntaxNode, semanticModel As SemanticModel, diagnostic As Diagnostic, document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim expression = TryCast(oldNode, ExpressionSyntax)

            Select Case diagnostic.Id
                Case BC37055
                    If expression Is Nothing Then
                        Return Task.FromResult(Of SyntaxNode)(Nothing)
                    End If
                    If Not IsCorrectReturnType(expression, semanticModel) Then
                        Return Task.FromResult(Of SyntaxNode)(Nothing)
                    End If
                    Return Task.FromResult(root.ReplaceNode(oldNode, ConverToAwaitExpression(expression)))
                Case BC42358
                    If expression Is Nothing Then
                        Return Task.FromResult(Of SyntaxNode)(Nothing)
                    End If
                    Return Task.FromResult(root.ReplaceNode(oldNode, ConverToAwaitExpression(expression)))
                Case Else
                    Return Task.FromResult(Of SyntaxNode)(Nothing)
            End Select
        End Function

        Private Function IsCorrectReturnType(expression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Dim taskType As INamedTypeSymbol = Nothing
            Dim returnType As INamedTypeSymbol = Nothing
            Return TryGetTypes(expression, semanticModel, taskType, returnType) AndAlso
                semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists
        End Function

        Private Function ConverToAwaitExpression(expression As ExpressionSyntax) As ExpressionSyntax
            Return SyntaxFactory.AwaitExpression(expression).WithAdditionalAnnotations(Formatter.Annotation)
        End Function

    End Class
End Namespace
