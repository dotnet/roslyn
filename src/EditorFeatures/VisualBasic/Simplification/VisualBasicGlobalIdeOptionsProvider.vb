' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    <ExportLanguageService(GetType(IGlobalIdeOptionsProvider), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicGlobalIdeOptionsProvider
        Implements IGlobalIdeOptionsProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetSimplifierOptions(globalOptions As IGlobalOptionService) As SimplifierOptions Implements IGlobalIdeOptionsProvider.GetSimplifierOptions
            Return globalOptions.GetVisualBasicSimplifierOptions()
        End Function
    End Class
End Namespace
