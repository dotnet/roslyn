' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable
Imports Microsoft.VisualStudio.Language.CodeCleanUp
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.LanguageService
    Friend NotInheritable Class VisualBasicCodeCleanUpFixerDiagnosticIds
        <Export>
        <FixId(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Name(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Order(After:=IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Remove_unused_variables))>
        Public Shared ReadOnly BC42024 As FixIdDefinition
    End Class
End Namespace
