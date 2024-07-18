' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateConstructor), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
    Friend Class GenerateConstructorCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return GenerateConstructorDiagnosticIds.AllDiagnosticIds.Concat(GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateConstructorService)()
            Return service.GenerateConstructorAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Dim invocation = TryCast(node, InvocationExpressionSyntax)
            If invocation IsNot Nothing AndAlso invocation.Expression IsNot Nothing Then
                Return GetRightmostName(invocation.Expression)
            End If

            Dim objectCreation = TryCast(node, ObjectCreationExpressionSyntax)
            If objectCreation IsNot Nothing AndAlso objectCreation.Type IsNot Nothing Then
                Return objectCreation.Type.GetRightmostName()
            End If

            Dim attribute = TryCast(node, AttributeSyntax)
            If attribute IsNot Nothing Then
                Return attribute.Name
            End If

            Return node
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            Return TypeOf node Is InvocationExpressionSyntax OrElse
                   TypeOf node Is ObjectCreationExpressionSyntax OrElse
                   TypeOf node Is AttributeSyntax
        End Function
    End Class
End Namespace
