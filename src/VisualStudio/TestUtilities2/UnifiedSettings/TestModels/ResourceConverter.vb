' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnifiedSettings.TestModels
    Friend Class ResourceConverter
        Inherits JsonConverter

        Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
            Dim token = JToken.Load(reader)
            If token.Type = JTokenType.String Then
                Return EvalResource(token.ToString)
            ElseIf token.Type = JTokenType.Array Then
                Dim array = token.Values
                Return array.Where(Function(arrayElement) arrayElement.Type = JTokenType.String).SelectAsArray(Function(arrayElement) EvalResource(arrayElement.ToString()))
            End If

            Throw ExceptionUtilities.UnexpectedValue(token.Type)
        End Function

        Public Overrides Function CanConvert(objectType As Type) As Boolean
            Return objectType = GetType(String) OrElse objectType = GetType(String())
        End Function
    End Class
End Namespace
