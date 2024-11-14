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
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateConversion), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateEvent)>
    Friend NotInheritable Class GenerateConversionCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30311 As String = "BC30311" ' error BC30311: Cannot convert type 'x' to type 'y'

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30311)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateConversionService)()
            Return service.GenerateConversionAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
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
