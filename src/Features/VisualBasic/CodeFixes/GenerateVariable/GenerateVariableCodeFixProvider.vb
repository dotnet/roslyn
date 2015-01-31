' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateVariable
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateVariable), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateMethod)>
    Friend Class GenerateVariableCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30456 As String = "BC30456" ' error BC30456: 'Foo' is not a member of 'P'.
        Friend Const BC30401 As String = "BC30401" ' error BC30401: 'Item' cannot implement 'Item' because there is no matching property on interface 'IFoo'.
        Friend Const BC30451 As String = "BC30451" ' error BC30451: 'xyz' is not declared. It may be inaccessible due to its protection level.
        Friend Const BC36610 As String = "BC36610" ' error BC36610: Name 'v' is either not declared or not in the current scope.

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30456, BC30401, BC30451, BC36610)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateVariableService)()
            Return service.GenerateVariableAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode) As Boolean
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
