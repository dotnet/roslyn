' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text.Json.Serialization

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Public Class AlternateDefault(Of T)
        <JsonPropertyName("flagName")>
        Public Property FlagName As String

        <JsonPropertyName("default")>
        Public Property [Default] As T
    End Class
End Namespace
