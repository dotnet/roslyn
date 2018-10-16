' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddMissingImports
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.PasteTracking
Imports Microsoft.CodeAnalysis.VisualBasic

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddMissingImports), [Shared]>
Friend Class VisualBasicAddMissingImportsRefactoringProvider
    Inherits AbstractAddMissingImportsRefactoringProvider

    Protected Overrides ReadOnly Property CodeActionTitle As String = VBFeaturesResources.Add_missing_Imports

    <ImportingConstructor>
    Public Sub New(pasteTrackingService As IPasteTrackingService)
        MyBase.New(pasteTrackingService)
    End Sub
End Class
