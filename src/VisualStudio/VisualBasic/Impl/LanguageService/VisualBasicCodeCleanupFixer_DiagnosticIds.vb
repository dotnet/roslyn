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
    Partial Friend Class VisualBasicCodeCleanUpFixer
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
        <FixId(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)>
        <Name(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.AddBracesDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(AnalyzersResources), NameOf(AnalyzersResources.Add_accessibility_modifiers))>
        Public Shared ReadOnly AddAccessibilityModifiersDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.OrderModifiersDiagnosticId)>
        <Name(IDEDiagnosticIds.OrderModifiersDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Sort_accessibility_modifiers))>
        Public Shared ReadOnly OrderModifiersDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)>
        <Name(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.AddQualificationDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(VBFeaturesResources), NameOf(VBFeaturesResources.Make_private_field_ReadOnly_when_possible))>
        Public Shared ReadOnly MakeFieldReadOnlyDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <Name(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Remove_unnecessary_casts))>
        Public Shared ReadOnly RemoveUnnecessaryCastDiagnosticId As FixIdDefinition

        <Export>
        <FixId(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Name(VisualBasicRemoveUnusedVariableCodeFixProvider.BC42024)>
        <Order(After:=IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Remove_unused_variables))>
        Public Shared ReadOnly BC42024 As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)>
        <Name(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Apply_object_collection_initialization_preferences))>
        Public Shared ReadOnly UseObjectInitializerDiagnosticId As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)>
        <Name(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)>
        <Order(After:=IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Apply_object_collection_initialization_preferences))>
        Public Shared ReadOnly UseCollectionInitializerDiagnosticId As FixIdDefinition

        <Export>
        <FixId(FormatDocumentFixId)>
        <Name(FormatDocumentFixId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <ExportMetadata("EnableByDefault", True)>
        <LocalizedName(GetType(ServicesVSResources), NameOf(ServicesVSResources.Format_document))>
        Public Shared ReadOnly FormatDocument As FixIdDefinition

        <Export>
        <FixId(RemoveUnusedImportsFixId)>
        <Name(RemoveUnusedImportsFixId)>
        <Order(After:=FormatDocumentFixId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <ExportMetadata("EnableByDefault", True)>
        <LocalizedName(GetType(BasicVSResources), NameOf(BasicVSResources.Remove_unnecessary_Imports))>
        Public Shared ReadOnly RemoveUnusedImports As FixIdDefinition

        <Export>
        <FixId(SortImportsFixId)>
        <Name(SortImportsFixId)>
        <Order(After:=RemoveUnusedImportsFixId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <ExportMetadata("EnableByDefault", True)>
        <LocalizedName(GetType(BasicVSResources), NameOf(BasicVSResources.Sort_imports))>
        Public Shared ReadOnly SortImports As FixIdDefinition

        <Export>
        <FixId(IDEDiagnosticIds.FileHeaderMismatch)>
        <Name(IDEDiagnosticIds.FileHeaderMismatch)>
        <Order(After:=SortImportsFixId)>
        <ConfigurationKey("unused")>
        <HelpLink("https://www.microsoft.com")>
        <ExportMetadata("EnableByDefault", True)>
        <LocalizedName(GetType(FeaturesResources), NameOf(FeaturesResources.Apply_file_header_preferences))>
        Public Shared ReadOnly FileHeaderMismatch As FixIdDefinition
    End Class
End Namespace
