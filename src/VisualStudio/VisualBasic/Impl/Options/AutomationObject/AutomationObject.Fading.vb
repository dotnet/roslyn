' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Fading_FadeOutUnreachableCode As Boolean
            Get
                Return GetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnreachableCode)
            End Get
            Set(value As Boolean)
                SetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnreachableCode, value)
            End Set
        End Property

        Public Property Fading_FadeOutUnusedImports As Boolean
            Get
                Return GetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnusedImports)
            End Get
            Set(value As Boolean)
                SetBooleanOption(IdeAnalyzerOptionsStorage.FadeOutUnusedImports, value)
            End Set
        End Property
    End Class
End Namespace
