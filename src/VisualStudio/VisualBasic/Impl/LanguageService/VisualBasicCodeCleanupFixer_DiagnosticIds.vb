' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable
Imports Microsoft.VisualStudio.Language.CodeCleanUp
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Common.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.Common.LanguageService

    Partial Class CommonCodeCleanUpFixer

        ' REVIEW I will delete the two below if they will never be done for VB

        '<Export>
        '<FixId(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)>
        '<Name(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)>
        '<Order(After:=IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)>
        '<ConfigurationKey("unused")>
        '<HelpLink("https://www.microsoft.com")>
        '<LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_implicit_explicit_type_preferences))>
        'Public Shared ReadOnly UseImplicitTypeDiagnosticId As FixIdDefinition

        '<Export>
        '<FixId(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)>
        '<Name(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)>
        '<Order(After:=IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)>
        '<ConfigurationKey("unused")>
        '<HelpLink("https://www.microsoft.com")>
        '<LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_implicit_explicit_type_preferences))>
        'Public Shared ReadOnly UseExplicitTypeDiagnosticId As FixIdDefinition

        ' REVIEW I don't know if this make sense for VB or has a fix available
        '<Export>
        '<FixId(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)>
        '<Name(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)>
        '<Order(After:=IDEDiagnosticIds.InlineDeclarationDiagnosticId)>
        '<ConfigurationKey("unused")>
        '<HelpLink("https://www.microsoft.com")>
        '<LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_language_framework_type_preferences))>
        'Public Shared ReadOnly PreferBuiltInOrFrameworkTypeDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.AddQualificationDiagnosticId)>
        <Name(IDEDiagnosticIds.AddQualificationDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.UseObjectInitializerDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_Me_qualification_preferences))>
        Public Shared ReadOnly AddQualificationDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.RemoveQualificationDiagnosticId)>
        <Name(IDEDiagnosticIds.RemoveQualificationDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.UseObjectInitializerDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Apply_Me_qualification_preferences))>
        Public Shared ReadOnly RemoveQualificationDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)>
        <Name(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.AddQualificationDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Make_private_field_ReadOnly_when_possible))>
        Public Shared ReadOnly MakeFieldReadOnlyDiagnosticId As FixIdDefinition

        <Export>
        <FixId(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Name(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Order(After:=IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(FeaturesResources.Remove_unused_variables))>
        Public Shared ReadOnly BC42024 As FixIdDefinition

    End Class

End Namespace
