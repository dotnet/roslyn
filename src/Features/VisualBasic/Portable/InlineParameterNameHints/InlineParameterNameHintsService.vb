' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineParameterNameHints

    <ExportLanguageServiceFactory(GetType(IInlineParameterNameHintsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class InlineParameterNameHintsService
        Implements IInlineParameterNameHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetInlineParameterNameHintsAsync(document As Document, textSpan As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of InlineParameterHint)) Implements IInlineParameterNameHintsService.GetInlineParameterNameHintsAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
