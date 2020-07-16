' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    Friend Module EmbeddedLanguageUtilities
        Public Function EscapeText(text As String) As String
            ' VB has no need to escape any regex characters that would be passed in through this API.
            Return text
        End Function
    End Module
End Namespace
