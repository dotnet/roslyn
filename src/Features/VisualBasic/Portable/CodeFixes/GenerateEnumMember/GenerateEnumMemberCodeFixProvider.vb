' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateEnumMember), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateConstructor)>
    Friend Class GenerateEnumMemberCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30456 As String = "BC30456" ' error BC30456: 'Red' is not a member of 'Color'.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30456)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateEnumMemberService)()
            Return service.GenerateEnumMemberAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            Return TypeOf node Is MemberAccessExpressionSyntax
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Return DirectCast(node, MemberAccessExpressionSyntax).Name
        End Function
    End Class
End Namespace
