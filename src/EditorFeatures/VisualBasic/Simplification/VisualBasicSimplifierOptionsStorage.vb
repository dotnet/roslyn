' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend Module VisualBasicSimplifierOptionsStorage

        <ExportLanguageService(GetType(ISimplifierOptionsStorage), LanguageNames.VisualBasic), [Shared]>
        Friend NotInheritable Class Service
            Implements ISimplifierOptionsStorage

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetOptions(globalOptions As IGlobalOptionService) As SimplifierOptions Implements ISimplifierOptionsStorage.GetOptions
                Return GetVisualBasicSimplifierOptions(globalOptions)
            End Function
        End Class

        <Extension>
        Public Function GetVisualBasicSimplifierOptions(globalOptions As IGlobalOptionService) As VisualBasicSimplifierOptions
            Return New VisualBasicSimplifierOptions() With
            {
                .Common = globalOptions.GetCommonSimplifierOptions(LanguageNames.VisualBasic)
            }
        End Function
    End Module
End Namespace
