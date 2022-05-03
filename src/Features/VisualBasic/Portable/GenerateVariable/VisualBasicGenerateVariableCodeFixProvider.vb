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
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateVariable
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateVariable), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateMethod)>
    Friend Class VisualBasicGenerateVariableCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30456 As String = "BC30456" ' error BC30456: 'Goo' is not a member of 'P'.
        Friend Const BC30401 As String = "BC30401" ' error BC30401: 'Item' cannot implement 'Item' because there is no matching property on interface 'IGoo'.
        Friend Const BC30451 As String = "BC30451" ' error BC30451: 'xyz' is not declared. It may be inaccessible due to its protection level.
        Friend Const BC36610 As String = "BC36610" ' error BC36610: Name 'v' is either not declared or not in the current scope.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30456, BC30401, BC30451, BC36610)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, fallbackOptions As CodeAndImportGenerationOptionsProvider, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateVariableService)()
            Return service.GenerateVariableAsync(document, node, fallbackOptions, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            If TypeOf node Is QualifiedNameSyntax OrElse TypeOf node Is MemberAccessExpressionSyntax Then
                Return True
            End If

            Dim simple = TryCast(node, SimpleNameSyntax)
            If simple IsNot Nothing Then
                If simple.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    Return True
                End If

                Return Not simple.IsParentKind(SyntaxKind.QualifiedName)
            End If

            Return False
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            If TypeOf node Is MemberAccessExpressionSyntax Then
                Return DirectCast(node, ExpressionSyntax).GetRightmostName()
            End If

            Return node
        End Function
    End Class
End Namespace
