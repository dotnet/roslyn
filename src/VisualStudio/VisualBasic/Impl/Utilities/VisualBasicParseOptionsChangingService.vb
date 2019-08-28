' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Utilities
    <ExportLanguageService(GetType(IParseOptionsChangingService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicParseOptionsChangingService
        Implements IParseOptionsChangingService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CanApplyChange(oldOptions As ParseOptions, newOptions As ParseOptions, maxLangVersion As String) As Boolean Implements IParseOptionsChangingService.CanApplyChange
            Return False
        End Function

        Public Sub Apply(options As ParseOptions, storage As ProjectPropertyStorage) Implements IParseOptionsChangingService.Apply
            Throw New InvalidOperationException(ServicesVSResources.This_workspace_does_not_support_updating_Visual_Basic_parse_options)
        End Sub
    End Class
End Namespace
