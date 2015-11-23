' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryImports

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddMissingReference)>
    Friend Class RemoveUnnecessaryImportsCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(RemoveUnnecessaryImportsDiagnosticAnalyzerBase.DiagnosticFixableId)
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim cancellationToken = context.CancellationToken

            Dim service = document.GetLanguageService(Of IRemoveUnnecessaryImportsService)()
            Dim newDocument = Await service.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(False)
            If newDocument Is document OrElse newDocument Is Nothing Then
                Return
            End If

            context.RegisterCodeFix(
                New MyCodeAction(VBFeaturesResources.RemoveUnnecessaryImports, newDocument),
                context.Diagnostics)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, newDocument As Document)
                MyBase.New(title, Function(c) Task.FromResult(newDocument))
            End Sub
        End Class
    End Class
End Namespace
