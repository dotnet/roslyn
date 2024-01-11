' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddMissingImports
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddMissingImports), [Shared]>
Friend Class VisualBasicAddMissingImportsRefactoringProvider
    Inherits AbstractAddMissingImportsRefactoringProvider

    Protected Overrides ReadOnly Property CodeActionTitle As String = VBFeaturesResources.Add_missing_Imports

    <ImportingConstructor>
    <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
    Public Sub New()
    End Sub
End Class
