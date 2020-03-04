﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Utilities
    <ExportLanguageService(GetType(ICompilationOptionsChangingService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCompilationOptionsChangingService
        Implements ICompilationOptionsChangingService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CanApplyChange(oldOptions As CompilationOptions, newOptions As CompilationOptions) As Boolean Implements ICompilationOptionsChangingService.CanApplyChange
            Return False
        End Function

        Public Sub Apply(options As CompilationOptions, storage As ProjectPropertyStorage) Implements ICompilationOptionsChangingService.Apply
            Throw New InvalidOperationException(ServicesVSResources.This_workspace_does_not_support_updating_Visual_Basic_compilation_options)
        End Sub
    End Class
End Namespace
