' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeRefactoringProvider("ImplementNotifyPropertyChangedVB", LanguageNames.VisualBasic)>
Friend Class ImplementNotifyPropertyChangedCodeRefactoringProvider
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Async Function GetRefactoringsAsync(context As CodeRefactoringContext) As Task(Of IEnumerable(Of CodeAction))
        Dim document = context.Document
        Dim textSpan = context.Span
        Dim cancellationToken = context.CancellationToken

        Dim root = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
        Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
        Dim properties = ExpansionChecker.GetExpandableProperties(textSpan, root, model)

        Return If(properties.Any(),
            {New ImplementNotifyPropertyChangedCodeAction("Apply INotifyPropertyChanged pattern", Function(c) ImplementNotifyPropertyChangedAsync(document, root, model, properties, c))},
            Nothing)
    End Function

    Private Async Function ImplementNotifyPropertyChangedAsync(document As Document, root As CompilationUnitSyntax, model As SemanticModel, properties As IEnumerable(Of ExpandablePropertyInfo), cancellationToken As CancellationToken) As Task(Of Document)
        document = document.WithSyntaxRoot(CodeGeneration.ImplementINotifyPropertyChanged(root, model, properties, document.Project.Solution.Workspace))
        document = Await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        document = Await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        Return document
    End Function

    Private Class ImplementNotifyPropertyChangedCodeAction
        Inherits CodeAction

        Private createDocument As Func(Of CancellationToken, Task(Of Document))
        Private _title As String

        Public Sub New(title As String, createDocument As Func(Of CancellationToken, Task(Of Document)))
            Me._title = title
            Me.createDocument = createDocument
        End Sub

        Public Overrides ReadOnly Property Title As String
            Get
                Return _title
            End Get
        End Property

        Protected Overrides Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Return Me.createDocument(cancellationToken)
        End Function
    End Class
End Class
