' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRemoveUnnecessaryByValCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryByValDiagnosticId)

        Friend Overrides ReadOnly Property CodeFixCategory As CodeFixCategory
            Get
                Return CodeFixCategory.CodeStyle
            End Get
        End Property

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            context.RegisterCodeFix(New MyCodeAction(
                VisualBasicAnalyzersResources.Remove_ByVal,
                Function(ct) FixAsync(context.Document, context.Diagnostics.First(), ct)),
                context.Diagnostics)
            Return Task.CompletedTask
        End Function

        Protected Overrides Async Function FixAllAsync(document As Document, diagnostics As ImmutableArray(Of Diagnostic), editor As SyntaxEditor, cancellationToken As CancellationToken) As Task
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim node = root.FindNode(diagnostics.First().Location.SourceSpan)
            editor.RemoveNode(node)
        End Function

        Private Class MyCodeAction
            Inherits CustomCodeActions.DocumentChangeAction

            Friend Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
