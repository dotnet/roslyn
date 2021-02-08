' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineHints

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineHints
    ''' <summary>
    ''' The service to locate all positions where inline hints should be placed.
    ''' </summary>
    <ExportLanguageService(GetType(IInlineHintsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInlineHintsService
        Inherits AbstractInlineHintsService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
