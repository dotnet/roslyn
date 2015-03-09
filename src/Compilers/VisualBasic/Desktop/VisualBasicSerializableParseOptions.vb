' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.Serialization

Namespace Microsoft.CodeAnalysis.VisualBasic
    <Serializable>
    Public Class VisualBasicSerializableParseOptions
        Inherits SerializableParseOptions

        Private ReadOnly _options As VisualBasicParseOptions

        Public Sub New(options As VisualBasicParseOptions)
            If options Is Nothing Then
                Throw New ArgumentNullException("options")
            End If

            _options = options
        End Sub

        Public Shadows ReadOnly Property Options As VisualBasicParseOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonOptions As ParseOptions
            Get
                Return _options
            End Get
        End Property

        Private Sub New(info As SerializationInfo, context As StreamingContext)
            ' We serialize keys and values as two separate arrays 
            ' to avoid the trouble dealing with situations where deserializer may want to deserialize
            ' an array before its elements (which gets complicated when elements are structs).

            Dim keys() As String = DirectCast(info.GetValue("Keys", GetType(String())), String())
            Dim values() As Object = DirectCast(info.GetValue("Values", GetType(Object())), Object())

            Dim count As Integer = keys.Length
            Debug.Assert(values.Length = count)

            Dim builder = ArrayBuilder(Of KeyValuePair(Of String, Object)).GetInstance

            For i As Integer = 0 To count - 1
                builder.Add(New KeyValuePair(Of String, Object)(keys(i), values(i)))
            Next

            _options = New VisualBasicParseOptions(
                languageVersion:=DirectCast(info.GetValue("LanguageVersion", GetType(LanguageVersion)), LanguageVersion),
                documentationMode:=DirectCast(info.GetValue("DocumentationMode", GetType(DocumentationMode)), DocumentationMode),
                kind:=DirectCast(info.GetValue("Kind", GetType(SourceCodeKind)), SourceCodeKind),
                preprocessorSymbols:=builder.ToImmutableAndFree())
        End Sub

        Public Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            CommonGetObjectData(_options, info, context)

            ' We serialize keys and values as two separate arrays 
            ' to avoid the trouble dealing with situations where deserializer may want to deserialize
            ' an array before its elements (which gets complicated when elements are structs).

            Dim ppSymbols = _options.PreprocessorSymbols
            Dim keys(ppSymbols.Length - 1) As String
            Dim values(ppSymbols.Length - 1) As Object

            For i As Integer = 0 To ppSymbols.Length - 1
                Dim sym = ppSymbols(i)
                keys(i) = sym.Key
                values(i) = sym.Value
            Next

            info.AddValue("Keys", keys, GetType(String()))
            info.AddValue("Values", values, GetType(Object()))
            info.AddValue("LanguageVersion", _options.LanguageVersion, GetType(LanguageVersion))
        End Sub
    End Class
End Namespace

