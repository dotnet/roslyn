' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImport
    Friend Module VisualBasicAddImportPlacementOptionsStorage

        <ExportLanguageService(GetType(IAddImportPlacementOptionsStorage), LanguageNames.VisualBasic), [Shared]>
        Private NotInheritable Class Service
            Implements IAddImportPlacementOptionsStorage

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()

            End Sub

            Public Function GetOptions(globalOptions As IGlobalOptionService) As AddImportPlacementOptions Implements IAddImportPlacementOptionsStorage.GetOptions
                Return GetVisualBasicAddImportPlacementOptions(globalOptions)
            End Function
        End Class

        <Extension>
        Public Function GetVisualBasicAddImportPlacementOptions(globalOptions As IGlobalOptionService) As AddImportPlacementOptions
            Return New AddImportPlacementOptions(
                PlaceSystemNamespaceFirst:=globalOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic),
                PlaceImportsInsideNamespaces:=False, ' VB does not support imports in namespace declarations
                AllowInHiddenRegions:=False)         ' no global option available
        End Function
    End Module
End Namespace
