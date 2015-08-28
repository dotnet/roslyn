' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateFromMembers.AddConstructorParameters

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateFromMembers.AddConstructorParameters
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers)>
    Friend Class AddConstructorParametersCodeRefactoringProvider
        Inherits CodeRefactoringProvider

        Public Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
            Dim document = context.Document
            Dim textSpan = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim workspace = document.Project.Solution.Workspace
            If workspace.Kind = WorkspaceKind.MiscellaneousFiles Then
                Return
            End If

            Dim service = document.GetLanguageService(Of IAddConstructorParametersService)()
            Dim result = Await service.AddConstructorParametersAsync(document, textSpan, cancellationToken).ConfigureAwait(False)

            If Not result.ContainsChanges Then
                Return
            End If

            Dim actions = result.GetCodeRefactoring(cancellationToken).Actions
            context.RegisterRefactorings(actions)
        End Function
    End Class
End Namespace
