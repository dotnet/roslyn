' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject

        Public Property FormatOnPaste As Boolean
            Get
                Return GetBooleanOption(FormattingOptionsStorage.FormatOnPaste)
            End Get
            Set(value As Boolean)
                SetBooleanOption(FormattingOptionsStorage.FormatOnPaste, value)
            End Set
        End Property

    End Class
End Namespace
