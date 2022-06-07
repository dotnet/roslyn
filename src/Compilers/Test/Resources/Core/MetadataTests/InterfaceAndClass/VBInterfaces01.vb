' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Interface IProp

    ReadOnly Property ReadOnlyProp As String
    ReadOnly Property ReadOnlyPropWithParams(p1 As UShort) As UInteger

    WriteOnly Property WriteOnlyProp As String
    WriteOnly Property WriteOnlyPropWithParams(p1 As UInteger) As ULong

    Property NormalProp As String
    Property NormalPropWithParams(p1 As Long, p2 As Short) As Long

End Interface

Public Interface IProp02
    Inherits IProp

    Default Property DefaultPropWithParams(p1 As SByte) As SByte

End Interface

Public Interface IMeth01
    Sub Sub01(ParamArray ary() As Byte)
    Function Func01(ParamArray ary As String()) As String
End Interface

Public Interface IMeth02
    Inherits IMeth01
    Overloads Sub Sub01(p1 As SByte, ParamArray ary As Byte())
    Overloads Function Func01(p1 As Object, ParamArray ary As String()) As String

    Public Interface INested
        Sub NestedSub(p As Byte)
        Function NestedFunc(ByRef p As String) As String
    End Interface

End Interface

Public Interface IMeth03

    Public Interface INested
        Sub NestedSub(p As UShort)
        Function NestedFunc(ByRef p As Object) As String
    End Interface

    MustInherit Class Nested
        Dim _sbyte As SByte = -1
        Public Overridable ReadOnly Property ReadOnlySByte As SByte
            Get
                Return _sbyte
            End Get
        End Property

        Public Overridable WriteOnly Property WriteOnlySByte As SByte
            Set(value As SByte)
                _sbyte = value
            End Set
        End Property

        Public MustOverride Property PropSByte As SByte
    End Class

End Interface
