' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SemanticResourceUtil

        ' Semantics\Async_Overload_Change_3.vb.txt
        Private Shared _async_Overload_Change_3_vb As String
        Public Shared ReadOnly Property Async_Overload_Change_3_vb As String
            Get
                Return GetOrCreate("Async_Overload_Change_3.vb.txt", _async_Overload_Change_3_vb)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestBaseline1.txt
        Private Shared _binaryOperatorsTestBaseline1 As String
        Public Shared ReadOnly Property BinaryOperatorsTestBaseline1 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestBaseline1.txt", _binaryOperatorsTestBaseline1)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestBaseline2.txt
        Private Shared _binaryOperatorsTestBaseline2 As String
        Public Shared ReadOnly Property BinaryOperatorsTestBaseline2 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestBaseline2.txt", _binaryOperatorsTestBaseline2)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestBaseline3.txt
        Private Shared _binaryOperatorsTestBaseline3 As String
        Public Shared ReadOnly Property BinaryOperatorsTestBaseline3 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestBaseline3.txt", _binaryOperatorsTestBaseline3)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestBaseline4.txt
        Private Shared _binaryOperatorsTestBaseline4 As String
        Public Shared ReadOnly Property BinaryOperatorsTestBaseline4 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestBaseline4.txt", _binaryOperatorsTestBaseline4)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestBaseline5.txt
        Private Shared _binaryOperatorsTestBaseline5 As String
        Public Shared ReadOnly Property BinaryOperatorsTestBaseline5 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestBaseline5.txt", _binaryOperatorsTestBaseline5)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestSource1.vb
        Private Shared _binaryOperatorsTestSource1 As String
        Public Shared ReadOnly Property BinaryOperatorsTestSource1 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestSource1.vb", _binaryOperatorsTestSource1)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestSource2.vb
        Private Shared _binaryOperatorsTestSource2 As String
        Public Shared ReadOnly Property BinaryOperatorsTestSource2 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestSource2.vb", _binaryOperatorsTestSource2)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestSource3.vb
        Private Shared _binaryOperatorsTestSource3 As String
        Public Shared ReadOnly Property BinaryOperatorsTestSource3 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestSource3.vb", _binaryOperatorsTestSource3)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestSource4.vb
        Private Shared _binaryOperatorsTestSource4 As String
        Public Shared ReadOnly Property BinaryOperatorsTestSource4 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestSource4.vb", _binaryOperatorsTestSource4)
            End Get
        End Property

        ' Semantics\BinaryOperatorsTestSource5.vb
        Private Shared _binaryOperatorsTestSource5 As String
        Public Shared ReadOnly Property BinaryOperatorsTestSource5 As String
            Get
                Return GetOrCreate("BinaryOperatorsTestSource5.vb", _binaryOperatorsTestSource5)
            End Get
        End Property

        ' Semantics\LongTypeNameNative.vb.txt
        Private Shared _longTypeNameNative_vb As String
        Public Shared ReadOnly Property LongTypeNameNative_vb As String
            Get
                Return GetOrCreate("LongTypeNameNative.vb.txt", _longTypeNameNative_vb)
            End Get
        End Property

        ' Semantics\LongTypeName.vb.txt
        Private Shared _longTypeName_vb As String
        Public Shared ReadOnly Property LongTypeName_vb As String
            Get
                Return GetOrCreate("LongTypeName.vb.txt", _longTypeName_vb)
            End Get
        End Property

        ' Semantics\OverloadResolutionTestSource.vb
        Private Shared _overloadResolutionTestSource As String
        Public Shared ReadOnly Property OverloadResolutionTestSource As String
            Get
                Return GetOrCreate("OverloadResolutionTestSource.vb", _overloadResolutionTestSource)
            End Get
        End Property

        ' Semantics\PrintResultTestSource.vb
        Private Shared _printResultTestSource As String
        Public Shared ReadOnly Property PrintResultTestSource As String
            Get
                Return GetOrCreate("PrintResultTestSource.vb", _printResultTestSource)
            End Get
        End Property

        ' Binding\T_1247520.cs
        Private Shared _t_1247520 As String
        Public Shared ReadOnly Property T_1247520 As String
            Get
                Return GetOrCreate("T_1247520.cs", _t_1247520)
            End Get
        End Property

        ' Binding\T_68086.vb
        Private Shared _t_68086 As String
        Public Shared ReadOnly Property T_68086 As String
            Get
                Return GetOrCreate("T_68086.vb", _t_68086)
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
            Using reader As New StreamReader(GetType(SemanticResourceUtil).GetTypeInfo().Assembly.GetManifestResourceStream(name))
                Return reader.ReadToEnd()
            End Using
        End Function
    End Class

End Namespace
