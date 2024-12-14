' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text.Json
Imports System.Text.Json.Serialization

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings
    Public Class UnifiedSettingsJsonResourceConverter
        Inherits JsonConverter(Of String)

        Public Overrides Sub Write(writer As Utf8JsonWriter, value As String, options As JsonSerializerOptions)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Read(ByRef reader As Utf8JsonReader, typeToConvert As Type, options As JsonSerializerOptions) As String
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
