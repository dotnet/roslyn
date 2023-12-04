' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ComVisible(True)>
    Partial Public Class AutomationObject
        Inherits AbstractAutomationObject

        Friend Sub New(legacyGlobalOptions As ILegacyGlobalOptionService)
            MyBase.New(legacyGlobalOptions, LanguageNames.VisualBasic)
        End Sub

        Private Overloads Function GetBooleanOption(key As PerLanguageOption2(Of Boolean)) As Boolean
            Return GetOption(key)
        End Function

        Private Overloads Function GetBooleanOption(key As Option2(Of Boolean)) As Boolean
            Return GetOption(key)
        End Function

        Private Overloads Sub SetBooleanOption(key As PerLanguageOption2(Of Boolean), value As Boolean)
            SetOption(key, value)
        End Sub

        Private Overloads Sub SetBooleanOption(key As Option2(Of Boolean), value As Boolean)
            SetOption(key, value)
        End Sub
    End Class
End Namespace
