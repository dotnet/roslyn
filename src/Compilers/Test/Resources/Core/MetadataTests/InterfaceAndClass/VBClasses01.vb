' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Class VBIMeth02Impl
    Implements IMeth02

    Public Overridable Sub Sub01(ParamArray ary() As Byte) Implements IMeth01.Sub01
        Console.Write("VBS1_V ")
    End Sub

    Public Overloads Sub Sub011(p1 As SByte, ParamArray ary() As Byte) Implements IMeth02.Sub01
        Console.Write("VBS11_OL ")
    End Sub

    Public Overridable Function Func01(ParamArray ary() As String) As String Implements IMeth01.Func01
        Console.Write("VBF1_V ")
        Return ary(0)
    End Function

    Public Function Func011(p1 As Object, ParamArray ary() As String) As String Implements IMeth02.Func01
        Console.Write("VBF11 ")
        Return p1.ToString()
    End Function

End Class

Public Class VBIPropImpl
    Implements IProp

    'Default Property DefaultProp(p1 As SByte) As SByte Implements IProp.DefaultPropWithParams
    '    Get
    '        Return 11
    '    End Get
    '    Set(value As SByte)

    '    End Set
    'End Property

    Private _str As String = "VBDefault "
    Overridable ReadOnly Property ReadOnlyProp As String Implements IProp.ReadOnlyProp
        Get
            Return _str
        End Get
    End Property
    Overridable WriteOnly Property WriteOnlyProp As String Implements IProp.WriteOnlyProp
        Set(value As String)
            _str = value
        End Set
    End Property
    Overridable Property NormalProp As String Implements IProp.NormalProp
        Get
            Return _str
        End Get
        Set(value As String)
            _str = value
        End Set
    End Property

    Private _uint As UInteger = 7
    Overridable ReadOnly Property ReadOnlyPropWithParams(p1 As UShort) As UInteger Implements IProp.ReadOnlyPropWithParams
        Get
            Return _uint
        End Get
    End Property
    Overridable WriteOnly Property WriteOnlyPropWithParams(p1 As UInteger) As ULong Implements IProp.WriteOnlyPropWithParams
        Set(value As ULong)
            _uint = CUInt(value)
        End Set
    End Property
    Overridable Property NormalPropWithParams(p1 As Long, p2 As Short) As Long Implements IProp.NormalPropWithParams
        Get
            Return _uint
        End Get
        Set(value As Long)
            _uint = CUInt(Math.Abs(p2))
        End Set
    End Property

End Class

