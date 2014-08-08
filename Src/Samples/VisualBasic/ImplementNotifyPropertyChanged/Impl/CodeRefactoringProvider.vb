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
Friend Class CodeRefactoringProvider
    Implements ICodeRefactoringProvider

    Public Async Function GetRefactoringsAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction)) Implements ICodeRefactoringProvider.GetRefactoringsAsync
        Dim root = DirectCast(Await document.GetSyntaxRootAsync().ConfigureAwait(False), CompilationUnitSyntax)
        Dim model = Await document.GetSemanticModelAsync().ConfigureAwait(False)
        Dim properties = ExpansionChecker.GetExpandableProperties(span, root, model)

        Return If(properties.Any(),
            {CodeAction.Create("Apply INotifyPropertyChanged pattern", Function(c) ImplementNotifyPropertyChangedAsync(document, root, model, properties, c))},
            Nothing)
    End Function

    Private Async Function ImplementNotifyPropertyChangedAsync(document As Document, root As CompilationUnitSyntax, model As SemanticModel, properties As IEnumerable(Of ExpandablePropertyInfo), cancellationToken As CancellationToken) As Task(Of Document)
        document = Document.WithSyntaxRoot(CodeGeneration.ImplementINotifyPropertyChanged(root, model, properties, document.Project.Solution.Workspace))
        document = Await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        document = Await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        Return document
    End Function
End Class
