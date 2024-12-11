' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text.Json.Serialization

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Public Class Map
        <JsonPropertyName("result")>
        Public Property Result As String
        <JsonPropertyName("match")>
        Public Property Match As Integer
    End Class
End Namespace
