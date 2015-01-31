' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateFromMembers.GenerateEqualsAndGetHashCode

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode
    ' <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCode)>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers)>
    Friend Class GenerateEqualsAndGetHashCodeCodeRefactoringProvider
        Inherits CodeRefactoringProvider

        Public Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
            Dim document = context.Document
            Dim textSpan = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim workspace = document.Project.Solution.Workspace
            If workspace.Kind = WorkspaceKind.MiscellaneousFiles Then
                Return
            End If

            Dim service = document.GetLanguageService(Of IGenerateEqualsAndGetHashCodeService)()
            Dim result = Await service.GenerateEqualsAndGetHashCodeAsync(document, textSpan, cancellationToken).ConfigureAwait(False)

            If Not result.ContainsChanges Then
                Return
            End If

            Dim actions = result.GetCodeRefactoring(cancellationToken).Actions
            context.RegisterRefactorings(actions)
        End Function
    End Class
End Namespace
