' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:="ImplementNotifyPropertyChangedVB"), [Shared]>
Friend Class ImplementNotifyPropertyChangedCodeRefactoringProvider
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
        Dim document = context.Document
        Dim textSpan = context.Span
        Dim cancellationToken = context.CancellationToken

        Dim root = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
        Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

        ' if length Is 0 then no particular range Is selected, so pick the first enclosing declaration
        If textSpan.Length = 0 Then
            Dim decl = root.FindToken(textSpan.Start).Parent.AncestorsAndSelf().OfType(Of DeclarationStatementSyntax)().FirstOrDefault()
            If decl IsNot Nothing Then
                textSpan = decl.Span
            End If
        End If

        Dim properties = ExpansionChecker.GetExpandableProperties(textSpan, root, model)

        If properties.Any Then
#Disable Warning RS0005
            context.RegisterRefactoring(
                CodeAction.Create("Apply INotifyPropertyChanged pattern",
                                  Function(c) ImplementNotifyPropertyChangedAsync(document, root, model, properties, c)))
#Enable Warning RS0005
        End If
    End Function

    Private Async Function ImplementNotifyPropertyChangedAsync(document As Document, root As CompilationUnitSyntax, model As SemanticModel, properties As IEnumerable(Of ExpandablePropertyInfo), cancellationToken As CancellationToken) As Task(Of Document)
        document = document.WithSyntaxRoot(CodeGeneration.ImplementINotifyPropertyChanged(root, model, properties, document.Project.Solution.Workspace))
        document = Await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        document = Await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken:=cancellationToken).ConfigureAwait(False)
        Return document
    End Function
End Class
