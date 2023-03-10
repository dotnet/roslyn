' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module VisualBasicCodeGenerationOptionsStorage
        <ExportLanguageService(GetType(ICodeGenerationOptionsStorage), LanguageNames.VisualBasic), [Shared]>
        Private NotInheritable Class Service
            Implements ICodeGenerationOptionsStorage

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetOptions(globalOptions As IGlobalOptionService) As CodeGenerationOptions Implements ICodeGenerationOptionsStorage.GetOptions
                Return New VisualBasicCodeGenerationOptions() With
                {
                    .Common = globalOptions.GetCommonCodeGenerationOptions(LanguageNames.VisualBasic)
                }
            End Function
        End Class
    End Module
End Namespace

