Imports System
Imports System.ComponentModel
Imports System.Configuration
Imports System.Diagnostics

Imports Microsoft.VisualStudio.Shell.Design.Serialization

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Serialize object in the same way that the runtime will serialize them, with the additional twist that you can pass
    ''' a culture info.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class SettingsValueSerializer

        Public Function Deserialize(ByVal ValueType As System.Type, ByVal serializedValue As String, ByVal culture As System.Globalization.CultureInfo) As Object
            If ValueType Is GetType(String) Then
                ' VsWhidbey 270764:
                ' Strings require special handling, since the ConfigHelper API assumes that all serialized representations
                ' of types that it will use anything but XML serialization to (de)serialize are correctly escaped.
                '
                Return serializedValue
            End If

            Dim Prop As New SettingsProperty("") ' We don't care about the name for the setting.... 
            Prop.ThrowOnErrorDeserializing = True
            Prop.PropertyType = ValueType
            Dim propVal As New SettingsPropertyValue(Prop)
            If ValueType IsNot Nothing Then
                Try
                    Dim configurationHelperService As New ConfigurationHelperService()
                    Prop.SerializeAs = configurationHelperService.GetSerializeAs(Prop.PropertyType)
                    Dim val As Object = Nothing
                    If Prop.SerializeAs = SettingsSerializeAs.String Then
                        ' We have to take care of this, cause we want to use the CultureInfo passed in to us when doing the deserialization
                        ' 
                        ' The runtime will always use InvariantCulture, but we sometimes have to show the default value in the UI, which 
                        ' means that we should use the current thread's culture...
                        Dim tc As TypeConverter = TypeDescriptor.GetConverter(ValueType)
#If DEBUG Then
                        Debug.Assert(tc.CanConvertFrom(GetType(String)), String.Format("Why were we told that serialization method was string for type {0} when type converter {1} can't convert from a string?", ValueType, tc))
                        Debug.Assert(tc.CanConvertTo(GetType(String)), String.Format("Why were we told that serialization method was string for type {0} when type converter {1} can't convert to a string?", ValueType, tc))
#End If
                        val = tc.ConvertFromString(Nothing, culture, serializedValue)
                    Else
                        propVal.SerializedValue = serializedValue
                        val = propVal.PropertyValue
                    End If

                    ' If the type converter was broken and returned an unknown type, we better stop it right here...
                    If val IsNot Nothing AndAlso Not ValueType.IsAssignableFrom(val.GetType()) Then
                        Return Nothing
                    End If

                    ' For some reason, enumerations' deserialization works even when the integer value is out of range...
                    ' Well, we know better - let's check for this funky case and return nothing :)
                    If val IsNot Nothing AndAlso GetType(System.Enum).IsAssignableFrom(ValueType) AndAlso Not System.Enum.IsDefined(ValueType, val) Then
                        ' If this is a flags attribute, we can't assume that the enum is defined
                        If ValueType.GetCustomAttributes(GetType(System.FlagsAttribute), False).Length = 0 Then
                            Return Nothing
                        End If
                    End If
                    Return val
                Catch Ex As System.Exception
                    ' Failed to convert - return NOTHING
                End Try
            End If

            ' Yet again we have some special handling for connection strings...
            If ValueType Is GetType(Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString) AndAlso serializedValue <> "" Then
                Dim scs As New Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString
                scs.ConnectionString = serializedValue
                scs.ProviderName = ""
                Return scs
            End If

            Return Nothing
        End Function

        Public Function Serialize(ByVal value As Object, ByVal culture As System.Globalization.CultureInfo) As String
            Dim serializedValue As String = Nothing
            Try
                serializedValue = SerializeImpl(value, culture)
            Catch ex As Exception
#If DEBUG Then
                System.Diagnostics.Debug.Fail(String.Format("Failed to serialize value: {0}", ex))
#End If
            End Try

            ' Make sure we always return a valid string...
            If serializedValue Is Nothing Then
                Return ""
            Else
                Return serializedValue
            End If
        End Function

        Private Function SerializeImpl(ByVal value As Object, ByVal culture As System.Globalization.CultureInfo) As String
            If value Is Nothing Then
                Return ""
            ElseIf value.GetType().Equals(GetType(String)) Then
                Return DirectCast(value, String)
            Else
                Dim prop As New SettingsProperty("") ' We don't care about the name of the setting!
                Dim configurationHelperService As New ConfigurationHelperService()
                prop.ThrowOnErrorSerializing = True
                prop.PropertyType = value.GetType()
                prop.SerializeAs = configurationHelperService.GetSerializeAs(prop.PropertyType)
                If prop.SerializeAs = SettingsSerializeAs.String Then
                    ' We have to take care of this, cause we want to use the CultureInfo passed in to us when doing the serialization
                    ' 
                    ' The runtime will always use InvariantCulture, but we sometimes have to show the default value in the UI, which 
                    ' means that we should use the current thread's culture...
                    Dim tc As TypeConverter = TypeDescriptor.GetConverter(prop.PropertyType) ' We intentionally pass in the type instead of the actual object, cause that is what the runtime does...
#If DEBUG Then
                    Debug.Assert(tc.CanConvertFrom(GetType(String)), String.Format("Why were we told that serialization method was string for type {0} when type converter {1} can't convert from a string?", value.GetType(), tc))
                    Debug.Assert(tc.CanConvertTo(GetType(String)), String.Format("Why were we told that serialization method was string for type {0} when type converter {1} can't convert to a string?", value.GetType(), tc))
#End If
                    Return tc.ConvertToString(Nothing, culture, value)
                Else
                    Dim propVal As New SettingsPropertyValue(prop)
                    Try
                        propVal.PropertyValue = value
                        Debug.Assert(TryCast(propVal.SerializedValue, String) IsNot Nothing, "Serialized value wasn't a string!?")
                        Return TryCast(propVal.SerializedValue, String)
                    Catch ex As ArgumentException
                        Debug.Fail("We failed to serialize a setting value - let's return it's ToString() value and pretend nothing happened...")
                        Return value.ToString()
                    End Try
                End If
            End If
        End Function

        ''' <summary>
        ''' Sometimes deserialize->serialize of a given string generates a new equivalent serialized representation
        ''' (i.e. DateTime "1991-01-01 00:00" and "1991-01-01 00:00:00" are different, but have the same deserialized
        '''  value)
        ''' This method "normalizes" the serialized value by simply deserialize and then re-serialize the
        ''' value...
        ''' </summary>
        ''' <param name="serializedValue"></param>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Normalize(ByVal serializedValue As String, ByVal type As System.Type) As String
            If SettingTypeValidator.IsTypeObsolete(type) Then
                Return serializedValue
            Else
                Return Serialize(Deserialize(type, serializedValue, Globalization.CultureInfo.InvariantCulture), Globalization.CultureInfo.InvariantCulture)
            End If
        End Function

    End Class


End Namespace