' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Caching implmentation for value serializations. 
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class SettingsValueCache

        ' We cache the values in a hashtable of hashtables:
        ' Type -> Serialized value -> Deserialized value
        Private _cachedSettingValues As New System.Collections.Generic.Dictionary(Of System.Type, System.Collections.Generic.Dictionary(Of String, Object))

        Private _culture As System.Globalization.CultureInfo

        Public Sub New(ByVal culture As System.Globalization.CultureInfo)
            _culture = culture
        End Sub

        ''' <summary>
        ''' Given a type for the setting and a serialized representation, get the deserialized value.
        ''' If the value is not found in the cache, it will be added.
        ''' </summary>
        ''' <param name="settingType"></param>
        ''' <param name="serializedValue"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetValue(ByVal settingType As System.Type, ByVal serializedValue As String) As Object
            Dim valueDictionary As System.Collections.Generic.Dictionary(Of String, Object) = Nothing
            If Not _cachedSettingValues.TryGetValue(settingType, valueDictionary) Then
                ' Make sure we have a 1st level entry for this type
                valueDictionary = New System.Collections.Generic.Dictionary(Of String, Object)
                _cachedSettingValues(settingType) = valueDictionary
            End If

            Dim value As Object = Nothing
            If Not valueDictionary.TryGetValue(serializedValue, value) Then
                ' Make sure we have an entry for the serialied value for this type
                Dim serializer As New SettingsValueSerializer()
                value = serializer.Deserialize(settingType, serializedValue, _culture)
                valueDictionary(serializedValue) = value
            End If
            Return value
        End Function

    End Class

End Namespace