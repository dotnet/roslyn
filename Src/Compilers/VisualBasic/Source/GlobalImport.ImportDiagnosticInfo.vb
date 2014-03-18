' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class GlobalImport

        ' A special Diagnostic info that wraps a particular diagnostic but customized the message with 
        ' the text of the import.
        <Serializable>
        Private Class ImportDiagnosticInfo
            Inherits DiagnosticInfo

            Private _importText As String
            Private _startIndex, _length As Integer
            Private _wrappedDiagnostic As DiagnosticInfo

            Private Sub New(info As SerializationInfo, context As StreamingContext)
                MyBase.New(info, context)

                Me._importText = DirectCast(info.GetValue("importText", GetType(String)), String)
                Me._startIndex = DirectCast(info.GetValue("startIndex", GetType(Integer)), Integer)
                Me._length = DirectCast(info.GetValue("length", GetType(Integer)), Integer)
                Me._wrappedDiagnostic = DirectCast(info.GetValue("wrappedDiagnostic", GetType(DiagnosticInfo)), DiagnosticInfo)
            End Sub

            Protected Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
                MyBase.GetObjectData(info, context)

                info.AddValue("importText", _importText, GetType(String))
                info.AddValue("startIndex", _startIndex, GetType(Integer))
                info.AddValue("length", _length, GetType(Integer))
                info.AddValue("wrappedDiagnostic", _wrappedDiagnostic, GetType(DiagnosticInfo))
            End Sub

            Private Sub New(reader As ObjectReader)
                MyBase.New(reader)
                Me._importText = reader.ReadString()
                Me._startIndex = reader.ReadInt32()
                Me._length = reader.ReadInt32()
                Me._wrappedDiagnostic = DirectCast(reader.ReadValue(), DiagnosticInfo)
            End Sub

            Protected Overrides Function GetReader() As Func(Of ObjectReader, Object)
                Return Function(r) New ImportDiagnosticInfo(r)
            End Function

            Protected Overrides Sub WriteTo(writer As ObjectWriter)
                MyBase.WriteTo(writer)

                writer.WriteString(_importText)
                writer.WriteInt32(_startIndex)
                writer.WriteInt32(_length)
                writer.WriteValue(_wrappedDiagnostic)
            End Sub

            Public Overrides Function GetMessage(Optional culture As CultureInfo = Nothing) As String
                If culture Is Nothing Then
                    culture = CultureInfo.InvariantCulture
                End If

                Dim msg = ErrorFactory.IdToString(ERRID.ERR_GeneralProjectImportsError3, culture)
                Return String.Format(msg, _importText, _importText.Substring(_startIndex, _length), _wrappedDiagnostic.GetMessage(culture))
            End Function

            Public Sub New(wrappedDiagnostic As DiagnosticInfo,
                           importText As String,
                           startIndex As Integer,
                           length As Integer)
                MyBase.New(VisualBasic.MessageProvider.Instance, wrappedDiagnostic.Code)
                _wrappedDiagnostic = wrappedDiagnostic
                _importText = importText
                _startIndex = startIndex
                _length = length
            End Sub
        End Class
    End Class
End Namespace