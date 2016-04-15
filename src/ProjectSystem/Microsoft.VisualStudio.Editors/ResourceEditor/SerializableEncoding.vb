' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Runtime.Serialization
Imports System.Text



Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a serializable wrapper around the System.Text.Encoding class.  We need this because we expose an "Encoding"
    '''   property on Resource for text files.  The Undo engine requires all the properties to be serializable, and 
    '''   System.Text.Encoding is not.
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable()> _
    Friend NotInheritable Class SerializableEncoding
        Implements ISerializable

        'The Encoding instance that we're wrapping
        Private _encoding As Encoding

        'Key for serialization
        Private Const s_KEY_NAME As String = "Name"


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="Encoding">The encoding to wrap.  Nothing is acceptable (indicates a default value - won't be written out to the resx if Nothing).</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal Encoding As Encoding)
            _encoding = Encoding
        End Sub


        ''' <summary>
        ''' Serialization constructor.
        ''' </summary>
        ''' <param name="info"></param>
        ''' <param name="context"></param>
        ''' <remarks></remarks>
        Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            Dim EncodingName As String = info.GetString(s_KEY_NAME)
            If EncodingName <> "" Then
                _encoding = Text.Encoding.GetEncoding(EncodingName)
            End If
        End Sub


        ''' <summary>
        ''' Returns/sets the encoding wrapped by this class.  Nothing is an okay value (indicates a default encoding).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property Encoding() As Encoding
            Get
                Return _encoding
            End Get
            Set(ByVal Value As Encoding)
                _encoding = Encoding
            End Set
        End Property


        ''' <summary>
        ''' Gets the display name (localized) of the encoding.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DisplayName() As String
            If _encoding IsNot Nothing Then
                Return SR.GetString(SR.RSE_EncodingDisplayName, _encoding.EncodingName, CStr(_encoding.CodePage))
            Else
                'Default
                Return SR.GetString(SR.RSE_DefaultEncoding)
            End If
        End Function


        ''' <summary>
        ''' Used during serialization.
        ''' </summary>
        ''' <param name="info"></param>
        ''' <param name="context"></param>
        ''' <remarks></remarks>
        Private Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements ISerializable.GetObjectData
            If _encoding IsNot Nothing Then
                info.AddValue(s_KEY_NAME, _encoding.WebName)
            Else
                info.AddValue(s_KEY_NAME, "")
            End If
        End Sub
    End Class

End Namespace
