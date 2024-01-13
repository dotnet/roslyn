' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SpellCheck

Namespace Microsoft.CodeAnalysis.VisualBasic.SpellCheck
    <ExportLanguageService(GetType(ISpellCheckSpanService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSpellCheckSpanService
        Inherits AbstractSpellCheckSpanService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(escapeCharacter:=Nothing)
        End Sub
    End Class
End Namespace
