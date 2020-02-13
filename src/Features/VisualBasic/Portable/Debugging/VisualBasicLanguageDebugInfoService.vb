' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging
    <ExportLanguageService(GetType(ILanguageDebugInfoService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicLanguageDebugInfoService
        Implements ILanguageDebugInfoService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function GetLocationInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugLocationInfo) Implements ILanguageDebugInfoService.GetLocationInfoAsync
            Return LocationInfoGetter.GetInfoAsync(document, position, cancellationToken)
        End Function

        Public Function GetDataTipInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugDataTipInfo) Implements ILanguageDebugInfoService.GetDataTipInfoAsync
            Return DataTipInfoGetter.GetInfoAsync(document, position, cancellationToken)
        End Function
    End Class
End Namespace
