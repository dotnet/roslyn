' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text.Json.Serialization

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels

    Public Class UnifiedSettingsOption(Of T)
        <JsonPropertyName("title")>
        Public Property Title As String

        <JsonPropertyName("type")>
        Public Property Type As String

        <JsonPropertyName("default")>
        Public Property [Default] As T

        <JsonPropertyName("AlternateDefault")>
        Public Property AlternateDefault As AlternateDefault(Of T)

        <JsonPropertyName("order")>
        Public Property Order As Integer

        <JsonPropertyName("enableWhen")>
        Public Property EnableWhen As String

        <JsonPropertyName("migration")>
        Public Property Migration As Migration
    End Class
End Namespace
