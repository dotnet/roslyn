'*****************************************************************************
'* SerializableEncodingConverter.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Globalization
Imports System.Text
Imports VB = Microsoft.VisualBasic

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A type converter for SerializableEncoding.  Associating this class with the Encoding property
    '''   on Resource allows the Encoding property to have a dropdown list that we control and fill with
    '''   suggested encoding values.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SerializableEncodingConverter
        Inherits TypeConverter

        'Our cached set of standard SerializableEncoding values
        Private m_StandardValuesCache As StandardValuesCollection


        ''' <summary>
        ''' Gets a value indicating whether this converter can convert an object in the given source 
        '''   type to a SerializableEncoding object using the specified context.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Function CanConvertFrom(ByVal Context As ITypeDescriptorContext, ByVal SourceType As Type) As Boolean
            If SourceType.Equals(GetType(String)) Then
                Return True
            End If

            Return MyBase.CanConvertFrom(Context, SourceType)
        End Function


        ''' <summary>
        ''' Converts the specified value object to a SerializableEncoding object.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Function ConvertFrom(ByVal Context As ITypeDescriptorContext, ByVal Culture As CultureInfo, ByVal Value As Object) As Object
            If TypeOf Value Is String Then
                Dim EncodingName As String = DirectCast(Value, String)

                'Try empty (indicates an Encoding of Nothing [default] - won't be written to the resx)
                If EncodingName = "" Then
                    Return New SerializableEncoding(Nothing)
                End If

                'Try as a codepage (in case they try typing in a codepage manually)
                If VB.IsNumeric(Value) Then
                    Return New SerializableEncoding(Encoding.GetEncoding(CInt(Value)))
                End If

                'Otherwise, try as a web name
                Return New SerializableEncoding(Encoding.GetEncoding(EncodingName))
            End If

            Return MyBase.ConvertFrom(Context, Culture, Value)
        End Function


        ''' <summary>
        ''' Converts the given value object to the specified destination type.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Function ConvertTo(ByVal Context As ITypeDescriptorContext, ByVal Culture As CultureInfo, ByVal Value As Object, ByVal DestinationType As Type) As Object
            If DestinationType Is Nothing Then
                Throw New ArgumentNullException("DestinationType")
            End If

            If DestinationType.Equals(GetType(String)) AndAlso TypeOf Value Is SerializableEncoding Then
                Dim SerializableEncoding As SerializableEncoding = DirectCast(Value, SerializableEncoding)

                'Here we return the localized encoding name.  That's what actually shows up
                '  in the properties window.
                Return SerializableEncoding.DisplayName()
            End If

            Return MyBase.ConvertTo(Context, Culture, Value, DestinationType)
        End Function


        ''' <summary>
        ''' Gets a value indicating whether this object supports a standard set of values that 
        '''   can be picked from a list using the specified context.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Function GetStandardValuesSupported(ByVal Context As ITypeDescriptorContext) As Boolean
            Return True
        End Function


        ''' <summary>
        ''' Indicates whether the standard values that we return are the only allowable values.
        ''' </summary>
        ''' <param name="Context"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' We return false so that the user is allows to type in a value manually (in particular,
        '''    a codepage value).
        ''' </remarks>
        Public Overrides Function GetStandardValuesExclusive(ByVal Context As ITypeDescriptorContext) As Boolean
            Return False
        End Function


        ''' <summary>
        ''' Gets a collection of standard values collection for a System.Globalization.CultureInfo
        '''   object using the specified context.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Function GetStandardValues(ByVal Context As ITypeDescriptorContext) As StandardValuesCollection
            If m_StandardValuesCache Is Nothing Then
                'We want to sort like the the Save As... dialog does.  In particular, we want this sorting:
                '
                '  Default
                '  Current code page
                '  Unicode encodings (alphabetized)
                '  All others (alphabetized)
                '
                'This corresponds to approximate likeliness of use

                Dim SortedUnicodeEncodings As New SortedList() 'Key=display name (localized), value = web name
                Dim SortedEncodings As New SortedList() 'Key=display name (localized), value = web name
                Dim CurrentCodePageEncoding As Encoding = Encoding.Default

                'Find all Unicode and other encodings, and alphabetize them
                For Each Info As EncodingInfo In Encoding.GetEncodings()
                    'Add the short name (web name) of the encoding to our list.  This
                    '  name is not localized, which is what we need, because ConvertFrom
                    '  will be used with this name to get the actual FriendlyEncoding
                    '  class.  The text displayed in the properties windows' dropdown
                    '  will come from calling ConvertToString

                    Dim Key As String = Info.DisplayName

                    Dim Encoding As Encoding = Info.GetEncoding()
                    If IsValidEncoding(Encoding) Then
                        If IsUnicodeEncoding(Encoding) Then
                            If Not SortedUnicodeEncodings.ContainsKey(Key) Then
                                SortedUnicodeEncodings.Add(Info.DisplayName, Info.Name)
                            End If
                        ElseIf Encoding.Equals(CurrentCodePageEncoding) Then
                            'We'll this separately, so skip it for now
                        Else
                            If Not SortedEncodings.ContainsKey(Key) Then
                                SortedEncodings.Add(Info.DisplayName, Info.Name)
                            End If
                        End If
                    Else
                        'If it's not valid (i.e., installed on this system), we don't want it in the list.
                    End If
                Next

                'Build up the full list
                Dim AllEncodings As New ArrayList
                AllEncodings.Add("") 'default
                AllEncodings.Add(CurrentCodePageEncoding.WebName)
                AllEncodings.AddRange(SortedUnicodeEncodings.Values)
                AllEncodings.AddRange(SortedEncodings.Values)

                m_StandardValuesCache = New StandardValuesCollection(AllEncodings)
            End If

            Return m_StandardValuesCache
        End Function


        ''' <summary>
        ''' Returns true if the encoding is a Unicode encoding variant
        ''' </summary>
        ''' <param name="Encoding"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsUnicodeEncoding(ByVal Encoding As Encoding) As Boolean
            Return Encoding.Equals(Text.Encoding.BigEndianUnicode) _
                OrElse Encoding.Equals(Text.Encoding.Unicode) _
                OrElse Encoding.Equals(Text.Encoding.UTF7) _
                OrElse Encoding.Equals(Text.Encoding.UTF8)
        End Function


        ''' <summary>
        ''' Returns True iff the given Encoding is valid (which means essentially that
        '''   it's currently installed in Windows).  The goal is to get the same list
        '''   of encodings that show up in the code page code editors or save as... list
        '''   in Visual Studio.
        ''' </summary>
        ''' <param name="Encoding">The encoding to check for validity.</param>
        ''' <returns>True if the encoding is valid.</returns>
        ''' <remarks></remarks>
        Private Function IsValidEncoding(ByVal Encoding As Encoding) As Boolean
            If Interop.NativeMethods.IsValidCodePage(CUInt(Encoding.CodePage)) Then
                Return True
            End If

            'A few exceptions that we consider valid
            If IsUnicodeEncoding(Encoding) OrElse Encoding.Equals(Text.Encoding.ASCII) Then
                Return True
            End If

            Return False
        End Function

    End Class

End Namespace
