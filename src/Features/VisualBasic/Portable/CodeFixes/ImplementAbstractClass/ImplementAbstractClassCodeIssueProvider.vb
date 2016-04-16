' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeActions.Providers
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeActions.ImplementAbstractClass
    <ExportCodeIssueProvider(PredefinedCodeActionProviderNames.ImplementAbstractClass, LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=PredefinedCodeActionProviderNames.GenerateType)>
    Partial Friend Class ImplementAbstractClassCodeIssueProvider
        Inherits AbstractVisualBasicCodeIssueProvider

        Public Overrides ReadOnly Property SyntaxNodeTypes As IEnumerable(Of Type)
            Get
                Return {GetType(TypeSyntax)}
            End Get
        End Property

        Protected Overrides Async Function GetIssueAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of CodeIssue)
            Dim workspace = document.Project.Solution.Workspace
            If workspace.Kind = WorkspaceKind.MiscellaneousFiles Then
                Return Nothing
            End If

            Dim service = document.GetLanguageService(Of IImplementAbstractClassService)()
            Dim result = Await service.ImplementAbstractClassAsync(document, node, cancellationToken).ConfigureAwait(False)
            Dim textChanges = Await result.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(False)
            If textChanges.Count = 0 Then
                Return Nothing
            End If

            Return New CodeIssue(CodeIssueKind.Error, node.Span, {New CodeAction(result)})
        End Function
    End Class
End Namespace
