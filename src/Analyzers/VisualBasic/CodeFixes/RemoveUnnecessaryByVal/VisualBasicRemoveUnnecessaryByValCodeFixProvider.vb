' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal
    <ExportCodeFixProvider(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryByValCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryByValDiagnosticId)

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim node = root.FindNode(context.Span)

            context.RegisterCodeFix(New MyCodeAction(
                VisualBasicAnalyzersResources.Remove_ByVal,
                Function(ct) RemoveByVal(context.Document, node, ct)),
                context.Diagnostics)
        End Function

        Private Async Function RemoveByVal(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim editor = Await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(False)
            editor.RemoveNode(node)
            Return editor.GetChangedDocument()
        End Function

        Private Class MyCodeAction
            Inherits CustomCodeActions.DocumentChangeAction

            Friend Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
