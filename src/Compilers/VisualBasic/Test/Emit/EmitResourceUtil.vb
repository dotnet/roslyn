' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EmitResourceUtil

        ' CodeGen\ConversionsILGenTestBaseline.txt
        Private Shared _conversionsILGenTestBaseline As String
        Public Shared ReadOnly Property ConversionsILGenTestBaseline As String
            Get
                Return GetOrCreate("ConversionsILGenTestBaseline.txt", _conversionsILGenTestBaseline)
            End Get
        End Property

        ' CodeGen\ConversionsILGenTestBaseline1.txt
        Private Shared _conversionsILGenTestBaseline1 As String
        Public Shared ReadOnly Property ConversionsILGenTestBaseline1 As String
            Get
                Return GetOrCreate("ConversionsILGenTestBaseline1.txt", _conversionsILGenTestBaseline1)
            End Get
        End Property

        ' CodeGen\ConversionsILGenTestSource.vb
        Private Shared _conversionsILGenTestSource As String
        Public Shared ReadOnly Property ConversionsILGenTestSource As String
            Get
                Return GetOrCreate("ConversionsILGenTestSource.vb", _conversionsILGenTestSource)
            End Get
        End Property

        ' CodeGen\ConversionsILGenTestSource1.vb
        Private Shared _conversionsILGenTestSource1 As String
        Public Shared ReadOnly Property ConversionsILGenTestSource1 As String
            Get
                Return GetOrCreate("ConversionsILGenTestSource1.vb", _conversionsILGenTestSource1)
            End Get
        End Property

        ' CodeGen\ConversionsILGenTestSource2.vb
        Private Shared _conversionsILGenTestSource2 As String
        Public Shared ReadOnly Property ConversionsILGenTestSource2 As String
            Get
                Return GetOrCreate("ConversionsILGenTestSource2.vb", _conversionsILGenTestSource2)
            End Get
        End Property

        Private Shared Function GetOrCreate(ByVal name As String, ByRef value As String) As String
            If Not value Is Nothing Then
                Return value
            End If

            value = GetManifestResourceString(name)
            Return value
        End Function

        Private Shared Function GetManifestResourceString(name As String) As String
            Using reader As New StreamReader(GetType(EmitResourceUtil).GetTypeInfo().Assembly.GetManifestResourceStream(name))
                Return reader.ReadToEnd()
            End Using
        End Function
    End Class

End Namespace
