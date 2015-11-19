' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateConversion), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateEvent)>
    Friend Class GenerateConversionCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30311 As String = "BC30311" ' error BC30311: Cannot convert type 'x' to type 'y'

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30311)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateConversionService)()
            Return service.GenerateConversionAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, diagnostic As Diagnostic) As Boolean
            Return TypeOf node Is QualifiedNameSyntax OrElse
                TypeOf node Is SimpleNameSyntax OrElse
                TypeOf node Is MemberAccessExpressionSyntax OrElse
                TypeOf node Is InvocationExpressionSyntax OrElse
                TypeOf node Is ExpressionSyntax OrElse
                TypeOf node Is IdentifierNameSyntax
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return memberAccess.Name
            End If

            Dim invocationExpression = TryCast(node, InvocationExpressionSyntax)
            If invocationExpression IsNot Nothing Then
                Return invocationExpression.Expression
            End If

            Return node
        End Function
    End Class
End Namespace
