' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
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
        <HelpLink("https://learn.microsoft.com/dotnet/visual-basic/misc/bc42024")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Remove_unused_variables))>
        Public Shared ReadOnly BC42024 As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.SimplifyObjectCreationDiagnosticId)>
        <Name(IDEDiagnosticIds.SimplifyObjectCreationDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0140")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_object_creation_preferences))>
        Public Shared ReadOnly SimplifyObjectCreationDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.UseIsNotExpressionDiagnosticId)>
        <Name(IDEDiagnosticIds.UseIsNotExpressionDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0084")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_isnot_preferences))>
        Public Shared ReadOnly UseIsNotExpressionDiagnosticId As FixIdDefinition
    End Class
End Namespace
