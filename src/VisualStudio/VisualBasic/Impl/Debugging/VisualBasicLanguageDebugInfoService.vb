' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Debugging

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
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
