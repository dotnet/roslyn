' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'////////////////////////////////////////////////////////////////////////////////////////////////////
'//GNAMBOO: Changing this code has implications for perf tests
'////////////////////////////////////////////////////////////////////////////////////////////////////
Option Infer On
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.VisualBasic

Module Program
    Sub Main()
        ns1.Test.Run()
        ns1.LowFrequencyTest.Run()
    End Sub
End Module

Namespace ns1
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()

            Dim e As c6(Of Integer, Long) = New c6(Of Integer, Long)(1, New List(Of Func(Of Long))()) : e.Test()

            Dim f As c7(Of Integer, Integer) = New c7(Of Integer, Integer)(1, New List(Of Func(Of Integer))()) : f.Test()

            Dim h As c9 = New c9() : h.test1()

            Dim i As s0(Of Integer) = New s0(Of Integer) : i.Test()

            s1.Test()

            Call CType(Nothing, nested.s2).Test(1, 1)
        End Sub
    End Class

    ' Abstract Class
    Public MustInherit Class c0
        ' Abstract Methods
        Public MustOverride Function abst(ByRef x As String, ParamArray y As Integer()) As Integer

        Public MustOverride Function abst(ByRef x As String, ParamArray y As Long()) As Integer

        Public MustOverride Function abst(ByRef x As String, y As Long, z As Long) As Integer
    End Class

    Public Class c1
        Inherits c0 ' Inheritance
        Private i As Integer = 2
        Friend ui As UInteger = 3
        Public a As c1 = Nothing

        ' Overloaded Constructors
        Public Sub New()
            i = 2 : Me.ui = 3
        End Sub

        Public Sub New(x As Integer)
            i = x : Me.ui = 3 : Me.a = New c1(Me.i, Me.ui, Me.a)
        End Sub

        Public Sub New(x As Integer, y As UInteger, c As c1)
            Me.i = x : ui = y : a = New c1()
        End Sub

        Public Sub test()
            Dim i As Integer = 2
            Dim b As Boolean = True

            ' Nested Scopes
            If b Then
                Dim o As Object = i
                b = False
                If True Then
                    Dim b1 As Byte = 1
                    Dim s As String = "c1.test()"
                    If True Then
                        Console.WriteLine(s)
                        Me.goo(o) : goo(i) : Me.goo(b) : Me.goo(b1) ' Overload Resolution, Implicit Conversions
                    End If
                End If
            End If
            ' Nested Scopes
            If Not b Then
                Dim o As Object = i
                b = False
                If True Then
                    Dim b1 As Byte = 1
                    Dim s As String = "c1.test()"
                    If Not False Then
                        Console.WriteLine(s)
                        bar2(o) : Me.bar2(i) : Me.bar2(b1) : bar1(s) ' Non-Overloaded Methods, Implicit Conversions
                    End If
                End If
            End If
            ' Nested Scopes
            If Not False Then
                Dim o As Object = i
                b = False
                If True Then
                    Dim b1 As Byte = 1
                    Dim s As String = "c1.test()"
                    If True Then
                        Console.WriteLine(s)
                        Me.bar4(o) : Me.bar4(i) : Me.bar4(b1) : Me.bar3(b) ' Non-Overloaded Methods, Implicit Conversions
                    End If
                End If
            End If

            If Not False Then
                Dim o As Object = i
                b = False
                If Not False Then
                    Dim s As String = "c1.test()"
                    If Not False Then
                        Console.WriteLine(s)

                        ' Method Calls - Ref, Paramarrays
                        ' Overloaded Abstract Methods
                        Dim c As c1 = New c1() : Dim l As Long = 1
                        Me.abst(s, 1, i)
                        c.abst(s, New Integer() {1, i, i})
                        c.abst(s, Me.abst(s, l, l), l, l, l)

                        ' Method Calls - Ref, Paramarrays
                        ' Overloaded Virtual Methods
                        c.virt(i, c, New c2(Of String)() {virt(i, c), New c4()})
                        virt(Me.virt(i, c), c.virt(i, c, virt(i, c)))
                        virt(c.abst(s, l, l), Me.abst(s, New Long() {1, i, l}))
                        c = New c4()
                        virt(i, c)
                        virt(i, New c4(), New c4(), New c2(Of String)())
                        virt(New Integer() {1, 2, 3})
                        virt(New Exception() {})
                        virt(New c1() {New c4(), New c2(Of String)()})
                    End If
                End If
            End If
        End Sub

        ' Overridden Abstract Methods
        Public Overrides Function abst(ByRef x As String, ParamArray y As Integer()) As Integer
            Console.WriteLine("    c1.abst(ref string, params int[])")
            x = x.ToString() : y = New Integer() {y(0)} ' Read, Write Ref + Paramarrays
            Return 0
        End Function

        Public Overrides Function abst(ByRef x As String, ParamArray y As Long()) As Integer
            Console.WriteLine("    c1.abst(ref string, params long[])")
            x = y(0).ToString() : y = Nothing ' Read, Write Ref + Paramarrays
            Return 1
        End Function

        Public Overrides Function abst(ByRef x As String, y As Long, z As Long) As Integer
            Console.WriteLine("    c1.abst(ref string, long, long)")
            x = z.ToString() ' Read, Write Ref
            Return 2
        End Function

        ' Virtual Methods
        Public Overridable Function virt(ByRef x As Integer, y As c1, ParamArray z As c2(Of String)()) As Integer
            Console.WriteLine("    c1.virt(ref int, c1, params c2<string>[])")
            x = x + x * 2 : z = Nothing ' Read, Write Ref + Paramarrays
            Return 0
        End Function

        Public Overridable Function virt(x As Integer, ByRef y As c1) As c2(Of String)
            Console.WriteLine("    c1.virt(int, ref c1)")
            y = New c1() ' Read, Write Ref
            Return New c4()
        End Function

        Public Overridable Function virt(ParamArray x As Object()) As Integer
            Console.WriteLine("    c1.virt(params object[])")
            x = New Object() {1, 2, Nothing} ' Read, Write Paramarrays
            Return New Integer()
        End Function

        Public Overridable Function virt(ParamArray x As Integer()) As Integer
            Console.WriteLine("    c1.virt(params int[])")
            x = New Integer() {0, 1} ' Read, Write Paramarrays
            Dim i As Integer = x(0)
            Return New Integer()
        End Function

        Friend Function goo(x As Integer) As Integer
            Console.WriteLine("    c1.goo(int)")

            ' Read, Write Fields
            Me.ui = 0UI + Me.ui
            i = i - 1 * 2 + 3 / 1
            Me.i = 1
            Me.a = Nothing : a = New c1(x)

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = Nothing
            s = String.Empty
            b = Me.i <> 1 + (2 - 3)
            s = ""
            Dim c As c1 = New c1(i, ui, New c1(Me.i, Me.ui, New c1(i)))
            c = Me.a
            b = b = True : s = s.ToString

            ' Read, Write Params
            x = (i - Me.i) * i + (x / i)
            x = x.GetHashCode : Me.i = x

            ' Read, Write Array Element
            Dim a1 As Integer() = New Integer() {1, 2, 3}
            a1(1) = i : a1(2) = x
            a1(1) = 1 : a1(2) = 2
            Dim i1 As Integer = a1(1) - a1(2)
            i1 = (a1(1) - (a1(2) + a1(1)))
            Dim o As Object = i1
            o = a1(2) + (a1(1) - a1(2))

            Return x
        End Function

        Public Function goo(x As Object) As Boolean
            Console.WriteLine("    c1.goo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.goo(i1) : bar4(b = (True <> b))

            ' Read, Write Params
            x = Nothing : x = New c1(Me.i, Me.ui, a)
            Me.bar4(x) : Me.bar4(x.GetHashCode() <> (x.GetHashCode()))

            ' Read, Write Array Element
            Dim a1 As Object() = New c1(2) {Nothing, Nothing, Nothing}
            Me.i = 1
            a1(1) = Nothing : a1(2) = New c1((i * i) / i, ui + (ui - ui), Nothing)
            Dim o As Object = Nothing
            o = a1(1) : Me.bar4(a1(2))

            If b Then
                Return b.GetHashCode() = Me.i
            Else
                Return b
            End If
        End Function

        Private Sub bar1(x As String)
            Console.WriteLine("    c1.bar1(string)")

            ' Read, Write Fields
            Me.ui = 0UI - 0UI
            i = Me.i * 1
            Me.a = New c1()
            Me.goo(i.GetHashCode()) : Me.a = Me

            ' Read, Write Locals
            Dim c As c1 = New c1(1, 0UI, (Nothing))
            c = Nothing : i = 1
            c = New c1(i / i)
            c = Me.a
            Me.ui = 1
            c.ui = Me.ui / ui
            c.i = Me.i + Me.i
            c.a = c
            c.a = Nothing : Me.a = c.a : c = Me.a
            c = New c1(i.GetHashCode())
            Me.goo(c.i) : bar3(c IsNot Nothing)

            If Me.i = 10321 Then
                Return
            Else
            End If

            ' Read, Write Params
            x = Nothing : Me.bar4(x)

            ' Read, Write Array Element
            Dim a1 As String() = New String() {"", Nothing, Nothing}
            a1(1) = Nothing : a1(2) = ""
            Dim s As String = Nothing
            s = a1(1) : goo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            goo(i)

            ' Read, Write Locals
            Dim c As c1
            Dim o As Object
            c = Nothing : c = New c1(i / 2, ui * (2UI), New c1(i / 2, ui * (2UI), c))
            Me.a = New c1(((1 + i) - 1))
            c = Me.a
            o = c
            c.ui = Me.ui
            c.i = Me.i * Me.i
            c.a = c : Me.a = c.a
            c.a = New c1(i, ui, New c1()) : Me.a = c.a : c = Me.a : c.a = c : c.a = c : o = c.a
            bar4(o.ToString()) : Me.bar4(c.a.a)

            ' Read, Write Params
            x = c : x = o
            o = x

            ' Read, Write Array Element
            Dim a1 As Object() = New c1() {Nothing, Me.a, c}
            a1(1) = Nothing : a1(2) = c
            o = a1(1) : bar3(a1(2) IsNot a1(1))

            If o Is Nothing Then
                Return Nothing
            Else
                Return String.Empty
            End If
        End Function

        Friend Function bar3(x As Boolean) As Object
            Console.WriteLine("    c1.bar3(bool)")

            ' Read, Write Fields
            ui = ui - Me.ui
            i = i + 1
            Me.a = New c1(i, ui, a)

            ' Read, Write Locals
            Dim b As Boolean = x
            b = Me.i > Me.i + 1

            ' Read, Write Params
            x = (Me.i = i + 1)
            goo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            goo(a1(1).GetHashCode())

            If b Then
                Return Me.i
            Else
                Return a1(1)
            End If
        End Function

        Public Function bar4(x As Object) As c1
            Console.WriteLine("    c1.bar4(object)")

            ' Read, Write Fields
            ui = 0
            Me.ui = Me.ui - (Me.ui + Me.ui) * Me.ui
            Me.i = (i + 1) - (1 * (i / 1))
            Me.a = (Nothing)
            goo(Me.i.GetHashCode())

            ' Read, Write Locals
            Dim o As Object = Nothing
            Dim b As Boolean = True
            b = Me.i <= Me.i + 1 - 1
            o = x
            Dim c As c1 = New c1(i, Me.ui, a)
            c.ui = (Me.ui) + (Me.ui) + c.ui
            x = c : o = x
            c.i = 1
            c.i = Me.i * (Me.i / c.i + c.i)
            c.a = New c1 : Me.a = c.a : c = Me.a : c.a = c : c.a = c
            goo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : goo(x.GetHashCode()) : goo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : goo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

            If b Then
                Return Me
            ElseIf Not b Then
                Return Me.a
            ElseIf Not b Then
                Return New c1(i, ui, New c1(i + 1, ui - 1UI, New c1(i + 2)))
            Else
                Return DirectCast(a1(2), c1)
            End If
        End Function
    End Class

    Public Class c2(Of T) ' Generics
        Inherits c1 ' Inheritance
        Protected c As c1 = New c1(0, 0, New c1(1, 1, New c1(2)))

        Public Sub TEST1()
            ' Nested Scopes
            Dim b As Byte = 0
            If True Then
                Dim sb As SByte = 0
                If Not False Then
                    Dim s As String = "c2<T>.test()"
                    If b = 0 Then
                        Console.WriteLine(s)
                        Me.goo(x:=b, y:=sb) ' Named Arguments
                    End If
                End If
                If sb <> 1 Then
                    Me.bar1(x:=b, y:=sb) ' Named Arguments
                End If
            End If
            If True Then
                Dim sb2 As SByte = 0
                If b <> 1 Then
                    Dim s2 As String = "c2<T>.test()"
                    If sb2 = 0 Then
                        Console.WriteLine(s2)
                        goo(x:=b, y:=sb2) ' Named Arguments
                    End If
                End If
                If b = sb2 Then
                    bar2(x:=b, y:=sb2) ' Named Arguments
                End If
            End If
            If Not False Then
                Dim c As c2(Of String) = New c4()
                If Not (Not True) Then
                    ' Method Calls - Ref, Paramarrays
                    ' Overloaded Abstract Methods
                    Dim i As Integer = 1 : Dim l As Long = i : Dim s As String = ""
                    Me.abst(s, 1, i)
                    c.abst(s, New Integer() {1, i, i})
                    c.abst(s, Me.abst(s, l, l), l, l, l)

                    ' Method Calls - Ref, Params
                    ' Overloaded Virtual Methods
                    Dim a As c1 = c
                    c.virt(i, c, New c2(Of String)() {virt(i, a), New c4()})
                    virt(Me.virt(i, a), c.virt(i, c, virt(i, a)))
                    virt(c.abst(s, l, l), Me.abst(s, New Long() {1, i, l}))
                    c = New c4()
                    virt(y:=a, x:=i)
                    virt(i, New c4(), New c4(), New c2(Of String)())
                    virt(New Integer() {1, 2, 3})
                    virt(New Exception() {})
                    virt(New c1() {New c4(), New c2(Of String)()})
                End If
            End If
        End Sub

        ' Overridden Abstract Methods
        Public Overrides Function abst(ByRef x As String, ParamArray y As Integer()) As Integer
            Console.WriteLine("    c2<T>.abst(ref string, params int[])")
            x = y(0).ToString() : y = Nothing ' Read, Write Ref + Paramarrays
            Return 0
        End Function

        Public Overrides Function abst(ByRef x As String, ParamArray y As Long()) As Integer
            Console.WriteLine("    c2<T>.abst(ref string, params long[])")
            x = y(0).ToString() : y = Nothing ' Read, Write Ref + Paramarrays
            Return 1
        End Function

        ' Overridden Virtual Methods
        Public Overrides Function virt(ByRef x As Integer, y As c1, ParamArray z As c2(Of String)()) As Integer
            Console.WriteLine("    c2<T>.virt(ref int, c1, params c2<string>[])")
            x = 0 : x = y.GetHashCode() : z = Nothing ' Read, Write Ref + Paramarrays
            Return 0
        End Function

        Public Overrides Function virt(x As Integer, ByRef y As c1) As c2(Of String)
            Console.WriteLine("    c2<T>.virt(int, ref c1)")
            x.ToString() : y = New c1(x, x, y) ' Read, Write Ref
            Return New c2(Of String)()
        End Function

        Public Overrides Function virt(ParamArray x As Object()) As Integer
            Console.WriteLine("    c2<T>.virt(params object[])")
            x.ToString() : x = Nothing ' Read, Write Paramarrays
            Return New Integer()
        End Function

        Private Sub bar(x As T)
            Console.WriteLine("    c2<T>.bar(T)")

            ' Read, Write Params
            Dim y As T = x
            x = y

            ' Read Consts
            Const const1 As String = ""
            If Not False Then
                Const const2 As Integer = 1
                Const const3 As Object = Nothing
                If True Then
                    Me.bar4(const1) : c.goo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function goo1(x As T) As T
            Console.WriteLine("    c2<T>.goo1(T)")

            Dim aa As Integer = 1

            ' Read, Write Params
            Dim y As T = x
            x = y : bar(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 < const2 - aa
                Continue Do
            Loop

            Do While const2 = const1 - aa + aa
                Me.bar4(const1) : c.goo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function goo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.goo(bool)")

            Dim aa As Integer = 1

            ' Read, Write Params
            x = x.ToString() = x.ToString() : a = c : a = Me

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 < const1 - aa
                Continue Do
            Loop

            Do While const2 = const2 - aa + aa
                Return x
            Loop
            Return x
        End Function

        Protected Overloads Function goo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.goo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.goo(x)

            ' Read Consts
            Const const1 As String = ""
            Dim b As Boolean = False
            If Not b Then
                Const const2 As Integer = 1
                Dim o As Object = y
                Do While y = o
                    Const const3 As Object = Nothing
                    Dim bb As Byte = 1
                    If bb = x Then
                        Me.bar4(const1) : Me.goo(const2 <> const2) : Me.a = const3
                        Exit Do
                    Else
                        Return const3
                    End If
                Loop
                Return c
            End If
            Return Nothing
        End Function

        Friend Sub bar1(x As Byte, y As Object)
            Console.WriteLine("    c2<T>.bar1(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.goo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.goo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.goo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.goo(const2 <> const2) : Me.a = const3
                End If
            End If
            Return const1
        End Function

        Friend Overloads Function bar3(x As Byte, y As Object) As Single
            Console.WriteLine("    c2<T>.bar3(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim d As Double = 1.1
            Dim c As c1 = New c1()
            Me.bar4(y) : c.goo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.goo(const2 <> const2) : c.a = const3
                    Return d
                End If
                Return 1.1F + 1.1
            End If
            Return d + 1.1
        End Function
    End Class

    Public Class c3(Of T, U) ' Generics
        Public Shared Sub test()
            Dim s As String = "c3<T>.test()"
            If True Then
                Console.WriteLine(s)
                goo() : goo(1) : goo("1") : goo(1.1) ' Overload Resolution, Implicit Conversions
            End If
            ' Nested Scopes
            If Not Not True Then
                Dim sb1 As SByte = 0
                If s <> "" Then
                    Do While sb1 = 0
                        Dim b As Byte = 0
                        Dim a As c2(Of String) = New c2(Of String)
                        a.bar1(b, sb1)
                        sb1 = 1
                        If sb1 = 1 Then Exit Do _
                        Else Continue Do
                    Loop
                End If
                Do While sb1 <> 0
                    Dim b As Byte = 1
                    Dim a As c2(Of String) = New c2(Of String)()
                    a.bar1(b, sb1)
                    sb1 = 0
                    If sb1 = 1 Then Exit Do _
                    Else Continue Do
                Loop
            End If
            ' Nested Scopes
            If Not False Then
                Dim sb2 As SByte = 0
                Do While sb2 < 2
                    sb2 = 3
                    Do While sb2 > 0
                        sb2 = 0
                        Dim b As Byte = 1
                        Dim a As c2(Of Integer) = New c2(Of Integer)
                        a.bar2(b, sb2)
                    Loop
                    sb2 = 3
                Loop
                If sb2 >= 3 Then
                    Dim b As Byte = 0
                    Dim a As c2(Of Integer) = New c2(Of Integer)
                    a.bar2(b, sb2)
                End If
            End If
            ' Nested Scopes
            If True Then
                Dim sb3 As SByte = 0
                Do While Not String.IsNullOrEmpty(s)
                    Dim b As Byte = 1
                    s = Nothing
                    If sb3 <> -20 Then
                        Dim a As c2(Of Boolean) = New c2(Of Boolean)
                        a.bar3(b, sb3)
                    End If
                    If s IsNot Nothing Then Exit Do
                Loop
                Do While s Is Nothing
                    Dim b As Byte = 0
                    If s IsNot Nothing Then
                        b = 1 : Continue Do
                    End If
                    Dim a As c2(Of Boolean) = New c2(Of Boolean)
                    a.bar3(b, sb3)
                    s = ""
                    Return
                Loop
            End If
        End Sub

        ' Static Methods
        Protected Shared Function goo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.goo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CLng(CInt(CLng(y.GetHashCode())))), Integer)
        End Function

        Friend Shared Function goo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.goo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function goo(x As String) As Single
            Console.WriteLine("    c3<T, U>.goo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return goo(x.GetHashCode())
        End Function

        Public Shared Function goo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.goo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function goo() As String
            Console.WriteLine("    c3<T, U>.goo()")
            Dim a As String() = New String() {"", Nothing} : a(0) = a(1) : a(1) = a(0)
            Return DirectCast(Nothing, String)
        End Function

        ' Instance Methods
        Protected Function bar(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.bar(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CInt((CLng(1) + CLng(CInt(CLng(2)))))
        End Function

        Public Function bar(x As Object) As c1
            Console.WriteLine("    c3<T, U>.bar(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            Return New c1(CInt(1.1F), CUInt(1), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(goo(x.GetHashCode()))
        End Function

        Public Function bar(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.bar(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Function bar() As String
            Console.WriteLine("    c3<T, U>.bar()")
            Dim a As String() = New String() {"", Nothing} : a(0) = a(1) : a(1) = a(0)
            Return DirectCast(Nothing, String)
        End Function
    End Class

    Public Class c4
        Inherits c2(Of String) ' Inheritance
        Public Shared b As Boolean = True
        Public Shared b1 As Byte = 0
        Public Shared sb As SByte = 1

        Private Shared s As Short = 4
        Private Shared us As UShort = 5
        Private Shared l As Long = 6
        Private Shared ul As ULong = 7

        Public Shared Function Test2() As Boolean
            Dim str As String = "c4.Test()"
            If True Then
                Dim i As Integer = 2
                Console.WriteLine(str)
                If Not False Then
                    Dim a As c1 = New c1(i) : a.goo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.goo(sb)
                    If True Then
                        a.goo(d)
                    End If
                End If

                ' Nested Scopes
                If (Not (Not True)) Then
                    Dim o As Object = i
                    Dim b As Boolean = False
                    If Not b Then
                        Dim b1 As Byte = 1
                        Dim s_ As String = "    This is a test"
                        Do While Not b
                            If True Then b = True
                            Console.WriteLine(s_)
                            Do While b
                                If True Then b = False
                                Dim oo As Object = i
                                Dim bb As Boolean = b
                                If Not bb Then
                                    If Not False Then bb = True
                                    Dim b11 As Byte = b1
                                    Dim ss As String = s_
                                    If bb Then
                                        Console.WriteLine(ss)
                                        If bb <> b Then
                                            Dim ooo As Object = i
                                            Dim bbb As Boolean = bb
                                            If bbb = True Then
                                                Dim b111 As Byte = b11
                                                Dim sss As String = ss
                                                Do While bbb
                                                    Console.WriteLine(sss)
                                                    bbb = False

                                                    ' Method Calls - Ref, Paramarrays
                                                    ' Overloaded Abstract Methods
                                                    Dim l As Long = i
                                                    Dim c As c4 = New c4()
                                                    c.abst(s_, 1, i)
                                                    c.abst(s_, New Integer() {1, i, i})
                                                    c.abst(s_, c.abst(s_, l, l), l, l, l)

                                                    ' Method Calls - Ref, Paramarrays
                                                    ' Overloaded Virtual Methods
                                                    Dim a As c1 = New c4()
                                                    c.virt(i, c, New c2(Of String)() {c.virt(i, a), New c4()})
                                                    c.virt(c.virt(i, a), c.virt(i, c, c.virt(i, a)))
                                                    c.
                                                      virt(
                                                           c.
                                                             abst(s_,
                                                                  l,
                                                                  l),
                                                           c.
                                                             abst(s_,
                                                                  New Long() {1,
                                                                              i,
                                                                              l}) _
                                                           )
                                                    c.virt(i, a)
                                                    c.virt(i, _
                                                           New c4(), _
                                                           New c4(), _
                                                           New c2(Of String)() _
                                                           )
                                                    c.virt(New Integer() {1, 2, 3})
                                                    c.virt(New Exception() {})
                                                    c.virt(New c1() {New c4(), New c2(Of String)()})
                                                    s = CShort(us)
                                                    If True Then Continue Do
                                                Loop
                                            ElseIf bbb <> True Then
                                                Console.WriteLine("Error - Should not have reached here")
                                                o = i : o = us
                                                Return DirectCast(o, Boolean)
                                            ElseIf bbb = False Then
                                                Console.WriteLine("Error - Should not have reached here")
                                                o = i : o = l
                                                Return DirectCast(o, Boolean)
                                            Else
                                                Console.WriteLine("Error - Should not have reached here")
                                                o = b : o = ul
                                                Return DirectCast(o, Boolean)
                                            End If
                                        End If
                                    ElseIf Not b Then
                                        Console.WriteLine("Error - Should not have reached here")
                                        Dim o1 As Object = b
                                        Return o1
                                    Else
                                        Console.WriteLine("Error - Should not have reached here")
                                        Dim o1 As Object = b
                                        Return o1
                                    End If
                                ElseIf Not bb Then
                                    Console.WriteLine("Error - Should not have reached here")
                                    o = b
                                    Return CBool(o)
                                Else
                                    Console.WriteLine("Error - Should not have reached here")
                                    Dim o1 As Object = b
                                    Return CBool(o1)
                                End If
                                Do While b <> False
                                    b = False : Exit Do
                                Loop
                                Exit Do
                            Loop
                            Do While b <> True
                                b = True : Continue Do
                            Loop
                        Loop
                    ElseIf b Then
                        Console.WriteLine("Error - Should not have reached here")
                        Return b
                    Else
                        Console.WriteLine("Error - Should not have reached here")
                        Return CBool(b) <> True
                    End If
                End If
            End If
            Return False
        End Function

        ' Non-Overloaded Method
        Public Overloads Shared Function goo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.goo(int, string, bool, byte, long, string)")
            Return New c4
        End Function

        ' Non-Overloaded Method
        Friend Shared Function bar(s As Short, us As UShort, sb As SByte, f As Single, d As Double, d1 As Double, f1 As Single) As c5
            Console.WriteLine("    c4.bar(short, ushort, sbyte, float, double, double, float)")
            Return New c5
        End Function

        Public Class c5 ' Nested Class
            Inherits c3(Of String, c1) ' Inheritance
            Friend Shared f As Single = 8.0F
            Friend Shared d As Double = 9.0
            Friend Shared s1 As String = "Test"
            Friend Shared o As Object = Nothing

            Shared Sub New
                o = s1 : s1 = CStr(o) : o = f : o = Nothing
            End Sub

            Public Function Test1() As Integer
                Dim i As Integer = 1, s As String = "1", b As Boolean = True
                Dim sh As Short = 1, us As UShort = 1, o As Object = i
                Dim cc As c5 = New c5()
                Console.WriteLine("c5.test")
                If True Then
                    Dim ui As UInteger = 1 : o = ui
                    i = sh : b = False : us = 1
                    ' Nested Scopes
                    If True Then
                        Dim b1 As Byte = 1, l As Long = i, s1 As String = s
                        Dim f As Single = 1.2F : o = f : l = ui
                        c4.goo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.goo(sh) : Me.bar(sh)
                        cc.bar(c5.goo(cc.bar()))
                        c5.goo(cc.bar(c5.goo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.goo(Me.bar(c5.goo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.goo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.goo(b)
                                Me.bar(b) : If c5.goo() IsNot Nothing Then c5.goo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.goo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.goo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.goo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.goo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.goo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.goo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.goo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
                                            If i <> i +
                                                1 -
                                                1 Then _
                                                Return i -
                                                b12
                                        End If
                                        If i <=
                                            1000 Then Exit Do
                                    End If
                                End If
                                If i <=
                                    1000 _
                                    Then i = 1000
                                Return sh
                            Loop
                        End If
                    End If
                End If
                Return CInt(sh)
            End Function
        End Class
    End Class

    Public Interface i0(Of T)
        Property prop1 As T
        ReadOnly Property prop2 As List(Of T)
        Sub method1()
        Sub method1(Of TT)(x As T, y As TT)
    End Interface

    Public Class c6(Of T, U)
        Implements IEnumerable(Of T), IEnumerator(Of T)
        ' Constructor
        Public Sub New()
            Console.WriteLine("    c6<T, U>.ctor")
        End Sub
        ' Constructor
        Public Sub New(i As Integer)
            'TODO: Uncomment once MyClass support is implemented
            'MyClass.New()
            Console.WriteLine("    c6<T, U>.ctor(int i)")
        End Sub
        ' Constructor
        Public Sub New(i As T, j As List(Of Func(Of U)))
            'TODO: Uncomment once MyClass support is implemented
            'MyClass.New(1)
            Console.WriteLine("    c6<T, U>.ctor(T i, List<Func<U>> j)")
        End Sub

        ' Const Fields, Field Initializers
        Protected Const L1 As Long = 10101
        Protected Const I1 As Integer = 10101

        ' Enums
        Protected Enum E1 As Long
            A = L1
            B = L2
            C = L3
        End Enum

        ' Const Fields, Field Initializers
        Public Const L2 As Long = 2 * CLng(E1.A)
        Public Const I2 As Integer = 2 * I1

        ' Enums
        Public Enum E2 As Long
            Member1 = L1 : Member2 : Member3 : Member4 : Member5
            : Member6 : Member7 : Member8 = L2 : Member9 = L1 + L1 : Member10
            : Member11 : Member12 = L3 * L2 : Member13 : Member14
            Member15 = L2 + L3 : Member16 : Member17 : Member18 = L3 : Member19 : Member20
        End Enum

        Public Enum E3 As Short
            Member1 = 1 : Member2 = 10 : Member3 = 100 : Member4 = 1000 : Member5 = 10000
            Member6 = 10 : Member7 = 20 : Member8 = 30 : Member9 = 40 : Member10 = 50
            Member11 = 11 : Member12 = 22 : Member13 = 33 : Member14 = 44 : Member15 = 55
            : Member16 : Member17 : Member18 : Member19 : Member20
        End Enum

        ' Const Fields, Field Initializers
        Protected Const L3 As Long = L2 + L1
        Protected Const I3 As Integer = I2 + I1

        ' Read-Write Auto-Property
        Public Property prop1 As U

        ' Read-Only Property
        Public ReadOnly Property prop2 As List(Of T)
            Get
                Console.WriteLine("    c6<T, U>.prop3.get()")
                Return New List(Of T)()
            End Get
        End Property

        ' Virtual Method
        Protected Overridable Sub virt(Of TT, UU, VV)(x As TT, y As UU, z As VV)
            Console.WriteLine("    c6<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)")
        End Sub

        ' Virtual Method
        Protected Overridable Sub virt(Of TT, UU, VV)(x As List(Of TT), y As List(Of UU), z As List(Of VV))
            Console.WriteLine("    c6<T, U>.virt<TT, UU, VV>(List<TT >x, List<UU> y, List<VV> z)")
        End Sub

#Region "IEnumerable Implementation"
        Protected Shared collection As IList(Of T) = New List(Of T)()
        ' Implement Interface
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Return collection.GetEnumerator()
        End Function

        ' Implement Interface
        Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
#End Region

#Region "IDisposable Implementation"
        ' Implement Interface
        Private proc As System.Threading.Tasks.Task = Nothing
        Sub Dispose() Implements IDisposable.Dispose
            proc.Dispose()
        End Sub
#End Region

#Region "IEnumerator Implementation"
        Private enumerator As IEnumerator(Of T) = collection.GetEnumerator()
        ' Implement Interface
        Public ReadOnly Property Current1 As T Implements IEnumerator(Of T).Current
            Get
                Return enumerator.Current
            End Get
        End Property

        ' Implement Interface
        ReadOnly Property Current As Object Implements System.Collections.IEnumerator.Current
            Get
                Return Current1
            End Get
        End Property

        ' Implement Interface
        Public Function MoveNext1() As Boolean Implements System.Collections.Generic.IEnumerator(Of T).MoveNext
            Return enumerator.MoveNext()
        End Function
        Public Function MoveNext() As Boolean
            Return enumerator.MoveNext()
        End Function

        ' Implement Interface
        Public Sub Reset() Implements IEnumerator(Of T).Reset
            enumerator.Reset()
        End Sub
#End Region

        Friend Sub Test()
            Dim b1 As Boolean = True, b2 As Boolean = True
            Dim x As Integer = 0
            If b1 AndAlso b2 Then
                Console.WriteLine("c6<T, U>.Test()")
                ' Generic Virtual Methods, Enums
                Dim enum1 As E1 = E1.A, enum2 As E2 = E2.Member17, enum3 As E3 = E3.Member19
                virt(enum1, E2.Member10, New List(Of E3)())
                enum1 = E1.A : enum3 = E3.Member3
                Dim c As c6(Of U, T) = New c7(Of T, U)(1)
                c.virt(Of E1(), E2(), E3)(New E1() {E1.A, E1.B}, New E2() {enum2, E2.Member16}, enum3)
                enum2 = E2.Member18

                x = CInt(enum1) : x = x + 1 : x = x - 1 : x = 1 + +x : x = x + -1
            ElseIf b1 OrElse b2 OrElse x + 1 >= x - 1 Then
                b1 = (x + 1).Equals(x - 1) OrElse (1 + x).CompareTo(-1 + x) > 0 AndAlso x = -1 + x
            End If
        End Sub
    End Class

    Public Interface i1(Of T, U) : Inherits i0(Of T)
        Shadows WriteOnly Property prop2 As List(Of U)
        Overloads Sub method1()
        Overloads Sub method1(Of TT, UU)(x As T, y As TT, xx As U, yy As UU, ByRef zz As TT)
        Sub method2()
    End Interface

    Public Class c7(Of T, U)
        Inherits c6(Of U, T)
        Implements IEnumerable(Of U), IDisposable, ICloneable, ICollection(Of U)

        ' Constructor
        Public Sub New()
            'TODO: Uncomment once MyBase support is implemented
            'MyBase.New()
            Console.WriteLine("    c7<T, U>.ctor()")
        End Sub
        ' Constructor
        Public Sub New(i As Integer)
            'TODO: Uncomment once MyBase support is implemented
            'MyBase.New(i)
            i = i + 1
            Console.WriteLine("    c7<T, U>.ctor(int i)")
            i = i - 1
        End Sub
        ' Constructor
        Public Sub New(i As T, j As List(Of Func(Of U)))
            'TODO: Uncomment once MyBase support is implemented
            'MyBase.New(Nothing, New List(Of Func(Of T))())
            Console.WriteLine("    c7<T, U>.ctor(T i, List<Func<U>> j)")
        End Sub

        ' Hide Enum
        Public Shadows Enum E1
            A = I1
            B = I1 + I2
            C = I2 / I3
        End Enum

        ' Const Fields
        Const enum1 As E1 = E1.A
        Const enum2 As E2 = E2.Member19

        ' Hide Enum
        Public Shadows Enum E2 As Long
            Member1 = L1 : Member2 = enum2 : Member3 : Member4 = I2 + (I1 - I3) : Member5 = ((I1 - I2))
            : Member6 : Member7 = I1 : Member8 = L2 : Member9 = (L1 + L1) - (I3) : Member10
            Member11 = enum1 : Member12 = L3 * (L2 + I1) / I3 : Member13 : Member14 = enum3 : Member15 = L2 + L3 + I2
            : Member16 : Member17 : Member18 = L3 : Member19 : Member20 = enum2
        End Enum

        ' Read-Write Property
        Public Property prop3 As List(Of T)
            Get
                Console.WriteLine("    c7<T, U>.prop3.get()")
                Return Nothing
            End Get
            ' Private Accessor
            Private Set(value As List(Of T))
                Console.WriteLine("    c7<T, U>.prop3.set()")
            End Set
        End Property
        ' Hide Read-Only Property
        Public Shadows ReadOnly Property prop2 As U
            Get
                Console.WriteLine("    c7<T, U>.prop2.get()")
                Return Nothing
            End Get
        End Property
        ' Hide Read-Write Property
        Public Overloads Property prop1 As IDictionary(Of T, U)
            Get
                Console.WriteLine("    c7<T, U>.prop1.get()")
                Return New Dictionary(Of T, U)()
            End Get
            Protected Friend Set(value As IDictionary(Of T, U))
                Console.WriteLine("    c7<T, U>.prop1.set()")
            End Set
        End Property

        ' Const Fields
        Const enum3 As E3 = E3.Member19

        ' Override Generic Virtual Method
        Protected Overrides Sub virt(Of TT, UU, VV)(x As TT, y As UU, z As VV)
            Console.WriteLine("    c7<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)")
            ' Enums
            Const enum1 As E1 = E1.A : Dim enum2 As E2 = E2.Member17 : Const enum3 As E3 = E3.Member19
            Dim b As Boolean = (E1.B = enum1) OrElse (enum2 < E2.Member19) AndAlso (enum3 >= E3.Member9)
            Dim e As Long = CLng(enum1) : b = e + 1 = -1 + e OrElse e - 1 = +1 + e
        End Sub

        ' Hide Generic Virtual Method
        Protected Overloads Function virt(Of TT, UU, VV)(x As List(Of TT), y As List(Of UU), z As List(Of VV)) As TT
            Console.WriteLine("    c7<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)")
            Return Nothing
        End Function

#Region "IEnumerable Re-Implementation"
        'TODO: Uncomment if bug 8877 is fixed
        ' Re-Implement Interface
        'Overloads Function GetEnumerator() As IEnumerator(Of U) Implements IEnumerable(Of U).GetEnumerator
        '    Return collection.GetEnumerator()
        'End Function

        'TODO: Uncomment if bug 8877 is fixed
        ' Re-Implement Interface
        'Shadows Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        '    Return GetEnumerator()
        'End Function
#End Region

#Region "ICollection Implementation"
        ' Implement Interface
        Private Shadows collection As List(Of U) = New List(Of U)()
        Public Sub Add(item As U) Implements ICollection(Of U).Add
            collection.Add(item)
        End Sub

        Public Sub Clear() Implements ICollection(Of U).Clear
            collection.Clear()
        End Sub

        Public Function Contains(item As U) As Boolean _
            Implements ICollection(Of U).Contains
            Return collection.Contains(item)
        End Function

        Public Sub CopyTo(array As U(), arrayIndex As Integer) _
            Implements ICollection(Of U).CopyTo
            collection.CopyTo(array, arrayIndex)
        End Sub

        Public ReadOnly Property Count As Integer _
            Implements ICollection(Of U).Count
            Get
                Return collection.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean _
            Implements ICollection(Of U).IsReadOnly
            Get
                Return False
            End Get
        End Property

        Public Function Remove(item As U) As Boolean _
            Implements ICollection(Of U).Remove
            Return collection.Remove(item)
        End Function
#End Region

#Region "ICloneable Implementation"
        ' Implement Interface
        Public Function Clone() As Object Implements ICloneable.Clone
            Return New c6(Of T, U)()
        End Function
#End Region

        Friend Shadows Sub Test()
            Console.WriteLine("c7<T, U>.Test()")
            Dim b As c6(Of T, U) = New c6(Of T, U)()
            ' Read, Write Properties
            Dim uu As U = Nothing, tt As T = Nothing
            b.prop1 = uu : uu = b.prop1
            b.prop2.Add(tt) : b.prop2.Count.ToString()

            Dim d As c7(Of T, U) = Me
            Dim dict As IDictionary(Of T, U) = New Dictionary(Of T, U)()
            ' Read, Write Properties
            d.prop1 = dict : dict = d.prop1 : d.prop1.Add(tt, uu)
            uu = d.prop2 : d.prop2.ToString()
            Dim l As List(Of T) = New List(Of T)()
            d.prop3 = l : l = d.prop3
        End Sub
    End Class

    Public Interface i2
        Inherits i1(Of Integer, Integer)
        Overloads ReadOnly Property prop1 As Integer
        Overloads Sub method1(x As Integer, y As Integer)
        Overloads Sub method1()
        Overloads Function method2() As Integer
    End Interface

    Public MustInherit Class c8
        Implements i0(Of Integer)
        Implements i1(Of Long, Long)

        Friend _prop1 As Integer = 0
        ' Implement Read-Write Property
        ' Virtual Property
        Public Overridable Property prop1 As Integer Implements i0(Of Integer).prop1
            Get
                Console.WriteLine("    c8.prop1.get()")
                _prop1 = _prop1 + 1
                Return _prop1
            End Get

            Set(val As Integer)
                _prop1 = val - 1
                Console.WriteLine("    c8.prop1.set()")
            End Set
        End Property

        Protected _prop2 As List(Of Integer) = New List(Of Integer)()
        Public Property prop2 As List(Of Integer)
            Get
                Console.WriteLine("    c8.prop2.get()")
                Return _prop2
            End Get
            ' Inaccessible Setter
            Private Set(value As List(Of Integer))
                _prop2 = value
                Console.WriteLine("    c8.prop2.set()")
            End Set
        End Property

        ' Implement ReadOnly Property
        Public ReadOnly Property prop21 As List(Of Integer) Implements i0(Of Integer).prop2
            Get
                Return prop2
            End Get
        End Property

        Dim __prop2 As List(Of Long) = New List(Of Long)()
        ' Implement Write-Only Property
        WriteOnly Property prop22 As List(Of Long) Implements i1(Of Long, Long).prop2
            Set(value As List(Of Long))
                __prop2 = value
                Console.WriteLine("    c8.prop22.set()")
            End Set
        End Property

        ' Implement ReadOnly Property
        ReadOnly Property prop23 As List(Of Long) Implements i0(Of Long).prop2
            Get
                Console.WriteLine("    c8.prop23.set()")
                Return __prop2
            End Get
        End Property

        ' Implement Read-Write Property
        Property prop11 As Long Implements i0(Of Long).prop1
            Get
                Console.WriteLine("    c8.prop11.get()")
                Return _prop1
            End Get

            Set(value As Long)
                _prop1 = Convert.ToInt32(value - 1) : _prop1 = -1 + _prop1
                Console.WriteLine("    c8.prop11.set()")
            End Set
        End Property

        ' Abstract Property
        Protected Friend MustOverride Property prop3 As IList(Of Integer)
        ' Virtual Auto-Property
        Public Overridable Property prop4 As IDictionary(Of String, IList(Of Integer))
        ' Virtual Property, Protected Friend Accessor
        Public Overridable Property prop5 As i2
            Get
                Console.WriteLine("    c8.prop5.get()")
                Return Nothing
            End Get
            Protected Friend Set(value As i2)
                Console.WriteLine("    c8.prop5.set()")
            End Set
        End Property

        ' Implement Methods
        ' Virtual Methods
        Public Overridable Sub method1() Implements i0(Of Integer).method1, i1(Of Long, Long).method1
            Console.WriteLine("    c8.method1()")
        End Sub
        Public Overridable Sub method1(Of TT)(x As Long, y As TT) Implements i1(Of Long, Long).method1
            Console.WriteLine("    c8.method1<TT>(long x, TT y)")
        End Sub
        Public Overridable Sub method2() Implements i1(Of Long, Long).method2
            Console.WriteLine("    c8.method2()")
        End Sub

        ' Implement Method
        ' Abstract Method
        Public MustOverride Sub method1(Of TT, UU)(x As Long, y As TT, xx As Long, yy As UU, ByRef zz As TT) Implements i1(Of Long, Long).method1

        'Implement Methods
        Sub method1(Of TT)(x As Integer, y As TT) Implements i0(Of Integer).method1
            Console.WriteLine("    c8.method1<TT>(int x, TT y)")
        End Sub
        Sub method11() Implements i0(Of Long).method1
            Console.WriteLine("    c8.method11()")
        End Sub

        ' Abstract Override Methods
        Public MustOverride Overrides Function ToString() As String
        Public MustOverride Overrides Function Equals(obj As Object) As Boolean
        Public MustOverride Overrides Function GetHashCode() As Integer

        Public Sub method1(x As Integer, y As Integer)
            Console.WriteLine("    c8.method1(x As Integer, y As Integer)")
        End Sub

        Public Sub Test()
            Console.WriteLine("c8.Test()")
            Dim a As i0(Of Integer) = Me
            ' Invoke Interface Methods
            a.method1()
            a.method1(1, True)
            a.method1(Of Exception)(1, New ArgumentException())
            ' Invoke Interface Properties
            a.prop1 = a.prop1 - 1
            Dim x As Integer = a.prop1 : a.prop1 = x
            Dim y As List(Of Integer) = a.prop2

            Dim b As i1(Of Long, Long) = Me
            ' Invoke Interface Methods
            b.method1()
            b.method1(1, 0)
            Dim e As AccessViolationException = New AccessViolationException()
            b.method1(1, e, 1, New ArgumentException(), e)
            b.method2()
            ' Invoke Interface Properties
            b.prop1 = x : b.prop1 = b.prop1 + 1
            x = Convert.ToInt32(b.prop1)
            b.prop2 = New List(Of Long)()
        End Sub
    End Class

    Public Class c9 : Inherits c8 : Implements i2
        ' Override Read-Write Property
        Public Overrides Property prop1 As Integer
            Get
                Console.WriteLine("    c9.prop1.get()")
                Return _prop1
            End Get
            Set(value As Integer)
                Console.WriteLine("    c9.prop1.set()")
                _prop1 = value
            End Set
        End Property

        'Implement Read-Only Property
        Public Overloads ReadOnly Property prop11 As Integer Implements i2.prop1
            Get
                Console.WriteLine("    c9.prop11.get()")
                Return prop1
            End Get
        End Property

        ' Hide Field
        Protected Shadows Function _prop2() As List(Of Integer)
            Console.WriteLine("    c9._prop2")
            Return New List(Of Integer)()
        End Function

        ' Implement Write-Only Property
        Public Overloads WriteOnly Property prop2 As List(Of Integer) Implements i2.prop2
            Set(v As List(Of Integer))
                Console.WriteLine("    c9.prop2")
                v = Nothing
            End Set
        End Property

        ' Sealed Property
        ' Override Read-Write Property
        Protected Friend NotOverridable Overrides Property prop3 As IList(Of Integer)
            Get
                Console.WriteLine("    c9.prop3.get()")
                Return _prop2()
            End Get
            ' Override Protected Accessor
            Set(value As IList(Of Integer))
                value.ToString()
                Console.WriteLine("    c9.prop3.set()")
            End Set
        End Property

        ' Sealed Property
        ' Override Read-Write Property
        Public NotOverridable Overrides Property prop4 As IDictionary(Of String, IList(Of Integer))
            Set(value As IDictionary(Of String, IList(Of Integer)))
                Console.WriteLine("    c9.prop5.get()")
                value = Nothing
            End Set
            Get
                Console.WriteLine("    c9.prop4.get()")
                Dim x As Dictionary(Of String, IList(Of Integer)) = New Dictionary(Of String, IList(Of Integer))()
                x.Add("", New List(Of Integer)())
                Return x
            End Get
        End Property

        ' Sealed Property
        ' Override Read-Write Property
        Public NotOverridable Overrides Property prop5 As i2
            Get
                Console.WriteLine("    c9.prop5.get()")
                Return Nothing
            End Get
            Protected Friend Set(value As i2)
                Dim x As i0(Of Integer) = value
                x.ToString()
                Console.WriteLine("    c9.prop5.set()")
            End Set
        End Property

        ' Hide Method
        ' Implement Method
        Public Overloads Sub method1() Implements i2.method1, i1(Of Integer, Integer).method1
            Console.WriteLine("    c9.method1()")
        End Sub

        ' Hide Methods
        ' Implement Methods
        Public Overloads Function method2() As Integer Implements i2.method2
            Console.WriteLine("    c9.method2()")
            Return 1
        End Function
        Public Shadows Sub method11(x As Integer, y As Integer) Implements i2.method1
            Console.WriteLine("    c8.method11(x As Integer, y As Integer)")
        End Sub
        Public Overloads Sub method21() Implements i2.method2
            Console.WriteLine("    c9.method21()")
        End Sub

        ' Implement Generic Method
        Public Overloads Sub method1(Of TT, UU)(x As Integer, y As TT, xx As Integer, yy As UU, ByRef z As TT) Implements i1(Of Integer, Integer).method1
            Console.WriteLine("    c9.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT z)")
        End Sub
        ' Override Abstract Method
        Public Overrides Sub method1(Of TT, UU)(x As Long, y As TT, xx As Long, yy As UU, ByRef z As TT)
            Console.WriteLine("    c9.method1<TT, UU>(long x, TT y, long xx, UU yy,  ref TT z)")
        End Sub

        ' Sealed Methods
        Public NotOverridable Overrides Function ToString() As String
            Return _prop1.ToString()
        End Function
        Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
            Return _prop1.Equals(obj)
        End Function
        Public NotOverridable Overrides Function GetHashCode() As Integer
            Return _prop1.GetHashCode()
        End Function

        Public Shadows Sub test1()
            test()
            Console.WriteLine("c9.Test()")
            Dim a As i0(Of Integer) = Me
            Dim b As i1(Of Integer, Integer) = Me
            a = b
            Dim c As i2 = Me
            b = c

            ' Invoke Interface Methods
            Dim i As Short = 1
            c.method1()
            i = i + 1 : c.method1(i, i - 1) : i = i - 1
            c.method1(i - 1, 1L) : i = i - 1
            c.method1(1, b, 1, 1L, a)
            c.method2()
            ' Invoke Interface Properties
            c.prop2 = New List(Of Integer)(c.prop1 + 100)

            Dim dd As c9 = Me, bb As c8 = dd
            ' Invoke Virtual / Abstract Methods
            bb.method1()
            bb.method1(1, 1)
            i = (i - 1) : bb.method1(i, 1L)
            bb.method1(i + 1 - (i - 1), b, (i - 1) * (i - 2), 1L, a)
            bb.method2()

            Dim x As Integer = 0
            ' Invoke Virtual / Abstract Properties
            bb.prop1 = bb.prop1 - 1 : bb.prop1 = x : x = bb.prop1 + bb.prop1 + 1 : bb.prop1 = bb.prop1 + 1
            Dim y As List(Of Integer) = bb.prop2
            bb.prop3.ToString() : dd.prop3 = bb.prop2
            bb.prop4 = New Dictionary(Of String, 
                                      IList(Of Integer))() : bb.prop4.ToString()
            bb.prop5 = Me : c = bb.prop5
        End Sub
    End Class

    <System.Runtime.InteropServices.ComVisible(True)>
    Structure s0(Of T) : Implements i1(Of Integer, Integer), i2

        Dim i, j As Integer
        Private _prop2 As List(Of Integer)

        ' Static Constructor
        Shared Sub New()
            ' Extension Methods
            Dim collection = New Integer() {1, 2, CByte(3), CShort(4), CInt(5L)}
            collection.AsParallel()
            collection.Aggregate(Function(a, b)
                                     Return a
                                 End Function)
            Dim bl = collection.AsQueryable().Any() OrElse
                collection.AsQueryable().Count() > collection.Sum()
            Console.WriteLine("    s0.cctor()")
        End Sub

        <System.Runtime.InteropServices.ComVisible(True)>
        Public WriteOnly Property prop2 As List(Of Integer)
            Set(v As List(Of Integer))
                Console.WriteLine("    s0.prop2.set()")
                _prop2 = v
                Throw New Exception()
            End Set
        End Property
        Dim k As Exception
        Public Sub method1()
            k = DirectCast(New ArgumentException(), Exception)
            Console.WriteLine("    s0.method1()")
            Throw If(k, New FormatException())
        End Sub

        Dim e As ArgumentException
        Public Sub method1(Of TT, UU)(x As Integer, y As TT, <nested.FirstAttribute.Second(Value:=0, Value2:=1)> xx As Integer, yy As UU, ByRef zz As TT)
            e = New ArgumentNullException()
            Console.WriteLine("    s0.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT zz)")
            Throw If(DirectCast(CType(e, ArgumentException), Exception), New Exception())
        End Sub

        Dim f As FieldAccessException
        Public Sub method2()
            f = New FieldAccessException()
            Console.WriteLine("    s0.method2()")
            Throw f
        End Sub

        Public Property prop1 As Integer
            Get
                Console.WriteLine("    s0.prop1.get()")
                Dim v = New AccessViolationException()
                Throw If(v, New Exception())
            End Get
            Set(val As Integer)
                Try
                    If val = j Then
                        i = val
                    Else
                        j = val
                    End If
                    Console.WriteLine("    s0.prop1.set()")
                    Dim e As Exception = New IndexOutOfRangeException()
                    Throw If(e, e)
                Catch
                    Throw
                Finally
                    Dim e As Exception = New DivideByZeroException()
                    Throw If(e, CType(New Object(), Exception))
                End Try
            End Set
        End Property

        ReadOnly Property prop22 As List(Of Integer) Implements i0(Of Integer).prop2
            Get
                Console.WriteLine("    s0.prop22.get()")
                Throw New InvalidOperationException()
            End Get
        End Property

        Public Sub method1(Of TT)(x As Integer, y As TT)
            Console.WriteLine("    s0.method1<TT>(int x, TT y)")
            Throw New MemberAccessException()
        End Sub

        ReadOnly Property prop11 As Integer Implements i2.prop1
            Get
                Console.WriteLine("    s0.prop11.get()")
                Throw New UnauthorizedAccessException()
            End Get
        End Property

        Sub method1(<nested.First(Value:=Value)> x As Integer, <nested.FirstAttribute.SecondAttribute.Third(1, 1, Value3:=0)> y As Integer) Implements i2.method1
            Dim ex As KeyNotFoundException = Nothing
            ex = If(ex, New KeyNotFoundException())
            Console.WriteLine("    s0.method1(int x, int y)")
            Throw ex
        End Sub

        <Obsolete()>
        Sub method11() Implements i2.method1
            Console.WriteLine("    s0.method11()")
            Throw New NotSupportedException()
        End Sub

        <ContextStatic()>
        Public Shared s As String = String.Empty
        Function method21() As Integer Implements i2.method2
            Console.WriteLine("    s0.method21()")
            Return s.Count()
        End Function

        WriteOnly Property prop21 As List(Of Integer) Implements i1(Of Integer, Integer).prop2
            Set(Value As List(Of Integer))
                Console.WriteLine("    s0.prop21.set()")
                Dim j = 0
                Value = New List(Of Integer)(New Integer() {1 / j})
            End Set
        End Property

        Sub method12() Implements i1(Of Integer, Integer).method1
            Console.WriteLine("    s0.method12()")
            Dim o As Object = Nothing
            o.ToString()
        End Sub

        Sub method11(Of TT, UU)(x As Integer, y As TT, xx As Integer, yy As UU, ByRef zz As TT) Implements i1(Of Integer, Integer).method1
            Console.WriteLine("    s0.method11<TT, UU>(int x, TT y, int xx, UU yy, ref TT zz)")
            Throw New NotImplementedException()
        End Sub

        <ThreadStatic()>
        Const l As Long = 0

        Sub method22() Implements i1(Of Integer, Integer).method2
            l.ToString()
            Console.WriteLine("    s0.method22()")
            Throw New OutOfMemoryException()
        End Sub

        <Flags()>
        Enum Flags
            A
            B
            C
        End Enum

        Property prop12 As Integer Implements i0(Of Integer).prop1
            Get
                Console.WriteLine("    s0.prop12.get()")
                Throw DirectCast(Nothing, Exception)
            End Get
            Set(value As Integer)
                Console.WriteLine("    s0.prop12.set()")
                Dim o As BadImageFormatException = Nothing
                Throw o
            End Set
        End Property

        <LoaderOptimization(LoaderOptimization.NotSpecified)>
        Sub method13() Implements i0(Of Integer).method1
            Console.WriteLine("    s0.method13()")
            Try
                Throw New NotImplementedException()
            Catch ex As NotImplementedException
                Throw ex
            Catch
                Throw
            End Try
        End Sub

        <Obsolete()>
        Sub method11(Of TT)(x As Integer, <nested.FirstAttribute.Second(Value, Value, Value:=Value, Value2:=Value)> y As TT) Implements i0(Of Integer).method1
            Console.WriteLine("    s0.method11<TT>(int x, TT y)")
        End Sub

        Const Value As Integer = 0
        <nested.First(Value, Value:=CShort(Value))>
        <LoaderOptimization(LoaderOptimization.NotSpecified)>
        Public Sub Test()
            Console.WriteLine("s0.Test()")
            Dim a As i0(Of Integer) = Me
            Dim b As i1(Of Integer, Integer) = Me
            a = b
            Dim c As i2 = Me
            b = c

            b = c : a = b : Dim aa = a
            b = c : Dim bb = b
            Dim cc = c

            If True Then
                ' Extension Methods
                Dim ii As Integer() = New Integer() {1, 2, 3}
                Dim q = ii.Where(Function(jj) jj > 0).Select(Function(jj) jj)
                Console.WriteLine("    Count = " & q.Count())
            End If

            ' Nested Exception Handling
            Try
                ' Invoke Interface Methods
                aa.method1()
            Catch ex As NotImplementedException
                Console.WriteLine("    " & ex.Message)
                Try
                    ' Invoke Interface Methods
                    aa.method1(1, True)
                    aa.method1(Of Exception)(1, New ArgumentException())
                    Throw
                Catch ex2 As NotImplementedException
                    Console.WriteLine("    " & ex2.Message)
                    Try
                        ' Invoke Interface Properties
                        aa.prop1 = aa.prop1 - 1 : Dim x = aa.prop1 : aa.prop1 = x
                        Dim y As List(Of Integer) = a.prop2
                    Catch ex3 As NotImplementedException
                        Console.WriteLine("    " & ex3.Message)
                        Throw ex3
                    Catch ex3 As Exception
                        Console.WriteLine("    " & ex3.Message)
                    Finally
                        ' Extension Methods
                        Dim q = "string".Where(Function(s) s.ToString() <> "string").
                            SelectMany(Function(s) New Char() {s})
                        For Each ii In q
                            Console.WriteLine("    Item: " & ii)
                        Next
                        Console.WriteLine("    First")
                    End Try
                Catch ex2 As Exception
                    Console.WriteLine("    " & ex2.Message)
                    Throw
                Finally
                    ' Extension Methods
                    Dim ii As Integer() = New Integer() {1, 2, 3}
                    For Each iii In ii.Where(Function(jj) jj >= ii(0)).Select(Function(jj) jj)
                        If ii.ToArray().Count() > 0 Then _
                            Console.WriteLine("    Item: " & iii)
                    Next
                    Console.WriteLine("    Second")
                End Try
            Catch ex As Exception
                Console.WriteLine("    " & ex.Message)
                Throw
            Finally
                Dim i As Integer = 0
                Dim ii As Integer() = New Integer() {1, 2, 3}
                ' Extension Methods
                Dim q = ii.Where(Function(jj) jj > 0).Select(Function(jj) jj)
                For i = 2 To 0
                    If q.Any() Then
                        Console.WriteLine("    Item: " & q.ElementAt(i))
                    ElseIf q.All(Function(jj) jj.GetType() Is New Object().GetType) Then
                        'TODO: Replace above line with below once TypeOf-Is starts working.
                        'ElseIf q.All(Function(jj) TypeOf jj.GetType() Is Object) Then
                        Console.WriteLine("    Item: " & q.ElementAt(i))
                    End If
                Next i
                Console.WriteLine("    Count = " & q.Count())
                Console.WriteLine("    Third")
            End Try

            Try
                ' Invoke Interface Methods
                bb.method1()
                bb.method1(1, 0)
                Dim e As AccessViolationException = New AccessViolationException()
                bb.method1(1, e, 1, New ArgumentException(), e)
                bb.method2()
                ' Invoke Interface Properties
                Dim x = 0
                bb.prop1 = x : bb.prop1 = bb.prop1 + 1 : x = Convert.ToInt32(bb.prop1)
                bb.prop2 = New List(Of Integer)()
            Catch ex As Exception
                Dim j As Integer = 2
                ' Extension Methods
                For Each ii In aa.ToString().Where(Function(e) e.ToString() <> j.ToString()).
                    OrderBy(Function(e) e).Distinct()
                    Console.WriteLine("    Item: " & ii)
                Next
                Console.WriteLine("    " & ex.Message)
                Console.WriteLine("    Fourth")
            End Try

            Try
                ' Invoke Interface Methods
                Dim i = 1L
                cc.method1()
                cc.method1(CType(i + 1, Integer), CShort(i - 1))
                cc.method1(CShort(-1 + i), CInt(1L))
                cc.method1(1, bb, 1, 1L, aa)
                cc.method2()
                ' Invoke Interface Properties
                cc.prop2 = New List(Of Integer)(cc.prop1 + 100)
                Dim o As Object = Nothing : o.ToString()
            Catch ex As Exception
                Dim j As Char = CChar(ChrW(0))
                ' Extension Methods
                For Each ii In ex.Message.
                    Where(Function(e) j.ToString() <> ex.Message + e.ToString()).
                    OrderBy(Function(e) e)
                    Console.WriteLine("    Item: " & ii)
                Next
                Console.WriteLine("    " & ex.Message)
            Finally
                Console.WriteLine("    Fifth")
            End Try
        End Sub
    End Structure

    <System.Runtime.InteropServices.StructLayout(CShort(0), Pack:=0, Size:=0)>
    <nested.First()>
    <Serializable()>
    Structure s1
        <NonSerialized()>
        Friend _i As Integer
        <NonSerialized()>
        <nested.First()>
        Friend _j As Integer

        ' Overloaded Constructors
        Private Sub New(i As Integer, l As Long)
            _i = i : _j = CInt(l)
            Console.WriteLine("    s1.ctor(int i, long l)")
        End Sub

        Private Sub New(i As Integer)
            _i = i : _j = CShort(i)
            Console.WriteLine("    s1.ctor(int i)")
        End Sub

        <nested.First()>
        Public Overrides Function Equals(<nested.FirstAttribute.SecondAttribute.Third(0, 1, Value2:=1)> obj As Object) As Boolean
            If Me.ToString() = (CType(obj, s1)).ToString() Then
                Dim s = CType(DirectCast(obj, s1), s1)
                Return True
            End If
            Console.WriteLine("    s1.Equals(object obj)")
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Console.WriteLine("    s1.GetHashCode()")
            Return 0
        End Function

        Public Overrides Function ToString() As String
            Console.WriteLine("    s1.ToString()")
            Return "    s1.ToString()"
        End Function

        ' Static Constructor
        <nested.FirstAttribute.SecondAttribute(Value:=0, Value2:=CByte(l))>
        Shared Sub New()
            ' Extension Methods
            Dim collection = New Double() {1, CDbl(2), CSng(3)}
            Dim bl = collection.AsEnumerable().Count() =
                collection.AsQueryable().DefaultIfEmpty().Distinct().
                ElementAt(CShort(collection.FirstOrDefault()))
            Dim s = New nested.FirstAttribute.SecondAttribute.ThirdAttribute(0, l, CShort(l))
            Console.WriteLine("    s2.cctor()")
        End Sub

        Const l As Long = 2
        <nested.First()>
        Public Shared Sub Test()
            Console.WriteLine("s1.Test()")
            Try
                Try
                    Dim s = New s1()
                    s.ToString()

                    Dim l = New List(Of Integer)(New Integer() {1, 2, 3})

                    ' For Loop
                    For Each i In l
                        If i > 0 Then
                            Console.WriteLine("    " & i)
                        End If
                        Continue For
                    Next
                    Throw New Exception()
                Catch
                    Dim s = New s1(1)

                    ' For Loop
                    For Each i In "string"
                        ' Boxing
                        Dim o As Object = s
                        ' Ternary
                        Dim str = If(o IsNot Nothing, New s1(o.GetHashCode()), If(o Is Nothing, Nothing,
                                        New s1(o.GetHashCode(), s.Equals(o).GetHashCode())))
                        Console.WriteLine(str)

                        ' Unboxing
                        s = o
                        Throw
                    Next
                Finally
                    Dim s = New s1(1, 1)
                    s.Equals(s)
                    ' Nested Loops
                    For i = 0 To 3
                        If i > 0 Then
                            ' Boxing
                            Dim o As Object = s

                            ' Ternary Operator
                            Dim str = If(o IsNot Nothing, o.ToString(), If(o Is Nothing, Nothing, o.ToString()))

                            ' Unboxing
                            s = DirectCast(o, s1)
                            Exit For
                        Else
                            For j As UInteger = CUInt(i) To 2 - i
                                ' Boxing
                                Dim o As Object = s

                                ' Ternary Operator
                                Dim str = If(o IsNot Nothing, o.ToString() = (i + j).ToString(),
                                             If(o Is Nothing, False, o.ToString() <> j.ToString()))

                                ' Unboxing
                                s = CType(o, s1)
                                If o.GetType() Is New s1().GetType AndAlso j > 0 Then
                                    'TODO: Replace above line with below once TypeOf-Is starts working.
                                    'If TypeOf o Is s1 AndAlso j > 0 Then
                                    Exit For
                                Else
                                    Continue For
                                End If
                            Next j
                        End If
                    Next

                    Dim iii = 1
                    Dim ooo As Object = ""
                    GoTo L1
L1:                 Console.WriteLine("    iii = " & iii)
                    ooo = ""
                    iii = iii + 1
                    If iii >= 5 AndAlso ooo.GetType() Is String.Empty.GetType Then
                        'TODO: Replace above line with below once TypeOf-Is starts working.
                        'If iii >= 5 AndAlso TypeOf ooo Is String Then
                        Dim sss = TryCast(ooo, String)
                        ooo = If(sss, String.Empty)
                        ooo = iii
                        GoTo L2
                    ElseIf ooo.GetType Is String.Empty.GetType Then
                        'TODO: Replace above line with below once TypeOf-Is starts working.
                        'ElseIf TypeOf ooo Is String Then
                        ooo = New ArgumentException()
                        Dim eee = TryCast(ooo, Exception)
                        ooo = If(eee, New Exception())
                        ooo = iii
                        ooo = New s1()
                        'TODO: Replace above line with below once TypeOf-Is starts working.
                        'If TypeOf ooo Is s1 Then _
                        '    GoTo L1
                        If ooo.GetType() Is New s1().GetType Then _
                            GoTo L1
                    End If
L2:                 Console.WriteLine("    iii = " & iii)
                    'TODO: Replace some occurrences of 'Is' with '=' once user defined operators work.
                    'This currently reports error that operator = is not defined between types
                    'System.Type and System.Type.
                    If ooo.GetType() Is String.Empty.GetType Then
                        Console.WriteLine("    ooo is string")
                    ElseIf ooo.GetType() Is (New Exception).GetType Then
                        Console.WriteLine("    ooo is Exception")
                    ElseIf ooo.GetType() Is (New s1).GetType Then
                        Console.WriteLine("    ooo is s1")
                    ElseIf ooo.GetType() Is 0.GetType Then
                        Console.WriteLine("    ooo is int")
                    End If
                End Try
            Catch
                Console.WriteLine("    First")
                Dim iii = 1
                GoTo L11
L11:            Console.WriteLine("    iii = " & iii)
                iii = iii + 1
                If iii >= 5 Then
                    GoTo L21
                Else
                    GoTo L11
                End If
L21:            Console.WriteLine("    iii = " & iii)
            End Try
        End Sub
    End Structure

    Interface i3(Of T)
        Sub method(Of U)(ByRef x As T, ByRef y As List(Of U), ByRef e As Exception, ByRef s As nested.s2)
        Sub method(Of U)(ByRef x As List(Of T), ByRef y As U, ByRef e As ArgumentException, ByRef s As nested.s2)
    End Interface

    Namespace nested
        <Serializable()>
        Structure s2
            Implements i3(Of String)
            ' Static Constructor
            <nested.FirstAttribute.SecondAttribute.Third(Value2:=0)>
            Shared Sub New()
                ' Extension Methods
                Dim collection = New Long() {1, 2, CByte(3), CShort(4), CInt(5L)}
                Dim bl = collection.Any(Function(a) a <> 0) OrElse collection.All(Function(a) a > 1)
                bl = collection.AsEnumerable().Average() >
                    collection.AsParallel().Average(Function(a As Long) a + bl.GetHashCode())
                Console.WriteLine("    s2.cctor()")
            End Sub

            ' Nested Struct
            <nested.FirstAttribute.Second()>
            Friend Structure s1
                <nested.FirstAttribute.SecondAttribute.Third()>
                Public Overrides Function ToString() As String
                    Try
                        Console.WriteLine("    s2.s1.ToString()")
                        Return String.Empty
                    Finally
                        Console.WriteLine("    First")
                    End Try
                End Function
            End Structure

            <nested.FirstAttribute.SecondAttribute.ThirdAttribute.Second()>
            Public Sub Test(Of T, U)(tt As T, uu As U)
                Console.WriteLine("s2.Test()")
                Try
                    s1.ReferenceEquals(tt, uu)

                    ' Extension Methods
                    Dim s = DirectCast(Nothing, s1).ToString()
                    Call CType(Nothing, s1).Stringize()

                    CType(Nothing, s1).ToString(s)
                    Dim zero = CInt(Nothing)
                    s1.Equals(tt.GetHashCode() / zero, zero)
                Catch ex As Exception
                    If True Then
                        Dim i As i3(Of String) = Me
                        Dim s As String = String.Empty : Dim l As List(Of Integer) = New List(Of Integer)()
                        i.method(s, l, ex, Me)
                    End If
                    If True Then
                        Dim i As i3(Of U) = New c1(Of U, T).c2(Of U)()
                        Dim r = DirectCast(i, c1(Of U, T).c2(Of U))
                        r.method()
                        Dim x = New List(Of String)()
                        i.method(uu, x, ex, Me)
                    End If
                End Try
            End Sub

            ' Interface Implementation
            Public Sub method(Of U)(ByRef x As String, ByRef y As List(Of U), ByRef e As Exception, ByRef s As s2) Implements i3(Of String).method
                Console.WriteLine("    s2.method<U>(out string x, ref List<U> y, out Exception e, out s2 s)")
                e = New ArgumentException()
                Dim ee = DirectCast(e, ArgumentException)
                Dim l = New List(Of String)()
                Call DirectCast(Me, i3(Of String)).method(l, x, ee, s)
            End Sub
            ' Interface Implementation
            Sub method(Of U)(ByRef x As List(Of String), ByRef y As U, ByRef e As ArgumentException, ByRef s As s2) Implements i3(Of String).method
                Console.WriteLine("    s2.method<U>(ref List<string> x, out U y, out ArgumentException e, out s2 s)")
                Dim b As Boolean = False
                y = Nothing : e = New ArgumentException()
                If b Then
                    Dim l = New List(Of U)() : l.Add(y)
                    Dim a = x.FirstOrDefault()
                    Dim ee = CType(e, Exception)
                    method(a, l, ee, s)
                End If
            End Sub
        End Structure

        <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Field Or AttributeTargets.Method Or
            AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Struct Or
            AttributeTargets.Parameter Or AttributeTargets.Assembly Or AttributeTargets.Module Or
            AttributeTargets.GenericParameter)>
        <First()>
        <FirstAttribute.SecondAttribute.Third(1, 2, 3, Value2:=1)>
        Public Class FirstAttribute : Inherits Attribute
            Public Value As Integer = CInt(CLng(Nothing))
            <First(Value:=Nothing)>
            <Second(CShort(Nothing), Value:=CShort(Nothing))>
            <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Field Or AttributeTargets.Method Or
                AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Struct Or
                AttributeTargets.Parameter Or AttributeTargets.Assembly Or AttributeTargets.Module Or
                AttributeTargets.Constructor)>
            Friend Class SecondAttribute
                Inherits FirstAttribute
                Public Shadows Value As Long = CInt(Nothing)
                Public Value2 As Short = CShort(CInt(Nothing))
                ' Static Constructor
                <Second()>
                Shared Sub New()
                    ' Extension Methods
                    Dim collection = New Char() {"a"c, "b"c, "c".Single()}
                    collection.ElementAtOrDefault(Val(collection.First()))
                    collection.Except(collection)
                    collection.Intersect(collection.AsEnumerable())
                    Console.WriteLine("    SecondAttribute.cctor()")
                End Sub
                <Third()>
                Public Sub New()
                    Console.WriteLine("    SecondAttribute.ctor()")
                End Sub
                <ThirdAttribute.Second()>
                Public Sub New(value As Integer)
                    Me.Value = value
                    Console.WriteLine("    SecondAttribute.ctor(int value)")
                End Sub
                <ThirdAttribute.Third()>
                Public Sub New(value As Integer, value2 As Long)
                    Me.Value = value
                    Me.Value2 = CShort(value2)
                    Console.WriteLine("    SecondAttribute.ctor(int value, long value2)")
                End Sub
            End Class

            <Second(0, 11)>
            <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Field Or AttributeTargets.Method Or
                AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Struct Or
                AttributeTargets.Parameter Or AttributeTargets.Assembly Or AttributeTargets.Module Or
                AttributeTargets.Constructor Or AttributeTargets.GenericParameter)>
            Friend Class ThirdAttribute
                Inherits SecondAttribute
                Public Shadows Value As Short = CByte(Nothing)
                Public Shadows Value2 As Long = CInt(CShort(Nothing))
                Public Value3 As Long = CInt(Nothing)

                ' Static Constructor
                <Third()>
                Shared Sub New()
                    ' Extension Methods
                    Dim collection = "string"
                    Dim i = collection.Skip(5).SingleOrDefault()
                    collection.Skip(2).SkipWhile(Function(a) Val(a) > 0)
                    collection.Take(2).TakeWhile(Function(a) Val(a) > 0).ToArray().ToList()
                    Console.WriteLine("    ThirdAttribute.cctor()")
                End Sub
                <ThirdAttribute.Second()>
                Public Sub New()
                    Console.WriteLine("    ThirdAttribute.ctor()")
                End Sub
                <ThirdAttribute.Third()>
                Public Sub New(value As Integer)
                    Me.Value = CShort(value)
                    Console.WriteLine("    ThirdAttribute.ctor(int value)")
                End Sub
                <Second()>
                Public Sub New(value As Integer, value2 As Long)
                    Me.Value = CByte(value)
                    Me.Value2 = value2
                    Console.WriteLine("    ThirdAttribute.ctor(int value, long value2)")
                End Sub
                Public Sub New(value As Integer, value2 As Long, value3 As Short)
                    Me.Value = CByte(value)
                    Me.Value2 = value2
                    Me.Value3 = value3
                    Console.WriteLine("    ThirdAttribute.ctor(int value, long value2, short value3)")
                End Sub
            End Class

            ' Static Constructor
            <Second()>
            Shared Sub New()
                ' Extension Methods
                Dim collection = New Single() {1, 2, 3}
                Dim bl = collection.AsParallel.AsOrdered.Cast(Of Single).
                    Concat(collection.AsParallel.AsOrdered.Cast(Of Single)).
                    Contains(CType(collection(0), Long))
                collection.CopyTo(collection, 0)
                Console.WriteLine("    FirstAttribute.cctor()")
            End Sub
            <SecondAttribute.Second()>
            Public Sub New()
                Console.WriteLine("    FirstAttribute.ctor()")
            End Sub
            <FirstAttribute.SecondAttribute.ThirdAttribute.Second(CType(Nothing, Byte))>
            Public Sub New(value As Integer)
                Me.Value = value
                Console.WriteLine("    FirstAttribute.ctor(int value)")
            End Sub
        End Class

        <First()>
        <FirstAttribute.Second(Nothing, Value:=0, Value2:=Nothing)>
        <FirstAttribute.SecondAttribute.ThirdAttribute()>
        Module ExtensionMethods
            Sub New()
                ' Extension Methods
                Dim collection = New Long() {1, 2, 3}
                Dim i = If(collection.Max() = collection.Min(),
                           collection.Max(Of Long)(Function(a) CSng(a)), collection.Min(Of Long)(Function(a) CDbl(a)))
                collection.OfType(Of Short)().OrderBy(Function(a) a).OrderByDescending(Function(a) a)
                collection.SequenceEqual(collection)

                Console.WriteLine("    ExtensionMethods.cctor()")
            End Sub
            <First()>
            <System.Runtime.CompilerServices.Extension()>
            Friend Function Stringize(<FirstAttribute.SecondAttribute.Third()> s As s2.s1) As String
                Console.WriteLine("    s2.ExtensionMethods.Stringize(this s2.s1 s)")
                Dim ss = If(s.ToString(), String.Empty)
                Return (s.ToString() = s.ToString(ss)).ToString()
            End Function
        End Module
        <FirstAttribute.Second()>
        Module ExtensionMethods2
            Sub New()
                ' Extension Methods
                Dim collection = "string"
                collection.ToLookup(Function(a) a).LongCount()
                collection.Intersect(collection).Reverse().Skip(4).Single()
                Console.WriteLine("    ExtensionMethods2.cctor()")
            End Sub
            <FirstAttribute.Second()>
            <System.Runtime.CompilerServices.Extension()>
            Public Function ToString(<FirstAttribute.SecondAttribute()> s As s2.s1) As String
                Console.WriteLine("    s2.ExtensionMethods.ToString(this s2.s1 s)")
                Dim ss = If(s.ToString(), String.Empty)
                Return s.ToString(ss)
            End Function
        End Module
        <FirstAttribute.SecondAttribute.Third()>
        Module ExtensionMethods3
            Sub New()
                Console.WriteLine("    ExtensionMethods3.cctor()")
            End Sub
            <FirstAttribute.SecondAttribute.Third()>
            <System.Runtime.CompilerServices.Extension()>
            Public Function ToString(<FirstAttribute.SecondAttribute()> s As s2.s1, <First(Nothing, Value:=CInt(CDbl(Nothing)))> ByRef ss As String) As String
                Console.WriteLine("    s2.ExtensionMethods.ToString(this s2.s1 s, string s2)")
                ss = If(s.ToString(), String.Empty)
                Return s.ToString()
            End Function
        End Module

        Class c1(Of T, U)
            Shared field As s1 = New s1()
            ' Static Constructor
            Shared Sub New()
                Console.WriteLine("    c1<T, U>.cctor()")
                field._i = 0 : field._i = field._i + 1 : field = CType(Nothing, s1)
                Dim i As i3(Of T) = New c2(Of Integer)()
                Dim t = CType(Nothing, T) : Dim l = New List(Of Integer)() : Dim s = Nothing
                Dim ex = DirectCast(New ArgumentException(), Exception)
                i.method(t, l, ex, s)
            End Sub

            Friend Class c2(Of V)
                Implements i3(Of T)
                ' Interface Implementation
                Sub method(Of UU)(ByRef x As T, ByRef y As List(Of UU), ByRef e As Exception, ByRef s As s2) Implements i3(Of T).method
                    Console.WriteLine("    c1<V>.method<UU>(out T x, ref List<UU> y, out Exception e, out s2 s)")
                    field = Nothing : s = Nothing : e = DirectCast(CType(Nothing, ArgumentException), Exception) : x = Nothing
                End Sub
                ' Interface Implementation
                Public Sub method(Of UU)(ByRef x As List(Of T), ByRef y As UU, ByRef e As ArgumentException, ByRef s As s2) Implements i3(Of T).method
                    Console.WriteLine("    c1<V>.method<UU>(ref List<T> x, out UU y, out ArgumentException e, out s2 s)")
                    field = Nothing : y = CType(Nothing, UU) : e = Nothing : s = CType(Nothing, s2)
                End Sub

                Public Sub method()
                    Console.WriteLine("    c1<V>.method()")
                    Dim i = (DirectCast(Me, i3(Of T))) : Dim t = CType(Nothing, T) : Dim s = CType(Nothing, s2)
                    Dim ex = DirectCast(New ArgumentException(), Exception)
                    Dim ee = CType(ex, ArgumentException) : Dim l = New List(Of T)() : l.Add(t)
                    i.method(l, l, ee, s)
                End Sub
            End Class
        End Class
    End Namespace
End Namespace
Namespace ns1
    Public Class LowFrequencyTest
        Public Shared Sub Run()
            Dim a As lowfrequency.c1(Of Integer, Long) = New lowfrequency.c1(Of Integer, Long)() : a.Test()

            Dim b As lowfrequency.c2(Of Integer, Integer) = New lowfrequency.c2(Of Integer, Integer)() : b.Test()
        End Sub
    End Class

    Namespace lowfrequency
        Public Class c1(Of T, U)
            ' Static Fields
            Public Shared tt As T = Nothing
            Public Shared uu As U = Nothing
            Public Shared l As List(Of T) = New List(Of T)()
            Public Shared d As Dictionary(Of List(Of T), U) = New Dictionary(Of List(Of T), U)()

            ' Delegates
            Public Delegate Function Del(Of TT, UU)(x As TT, y As List(Of TT), z As Dictionary(Of List(Of TT), UU)) As UU
            Public Delegate Function Del(Of TT)(x As TT, y As ArgumentException, z As Exception) As Integer
            Public Delegate Function Del(x As Integer, y As Long, z As Exception) As Integer

            ' Lambda
            Dim del1 As Del(Of T, U) = Function(x, y, z)
                                           If y IsNot l Then
                                               Return uu
                                           Else
                                               Dim d1 As Dictionary(Of List(Of T), U) = d
                                               If x.Equals(tt) Then
                                                   ' Nested Lambdas
                                                   Dim func As Func(Of Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U)), 
                                                                    Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U))) =
                                                                Function(a) Function(xx As T, yy As List(Of T), zz As U) a(xx, yy, zz)
                                                   'TODO: Uncomment below statement and delete above statement once we have
                                                   'support for anonymous delegates. See bug 9006. 
                                                   'Dim func As Func(Of Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U)), 
                                                   '                 Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U))) =
                                                   '             Function(a) (Function(xx As T, yy As List(Of T), zz As U) a(xx, yy, zz))
                                                   ' Invoke Lambdas
                                                   func(Function(xx As T, yy As List(Of T), zz As U) func(Function(aa, bb, cc) Nothing)(tt, l, uu))(tt, l, uu)
                                                   Console.WriteLine("    c1<T, U>.del1")
                                               End If
                                               Return Nothing
                                           End If
                                       End Function

            ' Lambda
            Public func As Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U)) = Function(x, y, z) d
            ' Lambda
            Public del2 As Del(Of U, T) = Function(x As U, y As List(Of U), z As Dictionary(Of List(Of U), T))
                                              If Not uu.Equals(x) Then 
                                                  Return tt
                                              Else
                                                  Dim d1 As Dictionary(Of List(Of T), U) = d
                                                  If Not l.Equals(y) Then
                                                      ' Nested Lambda
                                                      Dim func As Func(Of Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U)), 
                                                                       Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U))) =
                                                                   Function(a As Func(Of T, List(Of T), U, Dictionary(Of List(Of T), U)))
                                                                       ' Nested Lambda
                                                                       Return Function(xx As T, yy As List(Of T), zz As U)
                                                                                  Console.WriteLine("    c1<T, U>.del2")
                                                                                  Return d1
                                                                              End Function
                                                                   End Function
                                                      ' Invoke Lambdas
                                                      func(Function(xx, yy, zz) func(Function(aa As T, bb As List(Of T), cc As U) Nothing)(tt, l, uu))(tt, l, uu)
                                                  End If
                                                  Return Nothing
                                              End If
                                          End Function

            ' Generic Method
            Protected Sub goo(Of TT, UU, VV)(x As Func(Of TT, UU, VV), y As Func(Of UU, VV, TT), z As Func(Of VV, TT, UU))
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, UU, VV> x, Func<UU, VV, TT> y, Func<VV, TT, UU> z)")
                Dim t As TT = Nothing, u As UU = Nothing, v As VV = Nothing

                ' Invoke Lambdas
                z(v, y(u, x(t, u)))
            End Sub

            ' Generic Method
            Protected Sub goo(Of TT, UU, VV)(xx As TT, yy As UU, zz As VV)
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(TT xx, UU yy, VV zz)")
            End Sub

            ' Generic Method
            Protected Sub goo(Of TT, UU, VV)(x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)), y As Del(Of UU, VV), z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, List<TT>, Dictionary<List<TT>, UU>> x, Del<UU, VV> y, Action<VV, List<VV>, Dictionary<List<VV>, TT>> z)")
                Dim t As TT = Nothing, u As UU = Nothing, v As VV = Nothing

                ' Invoke Lambdas
                x(t, New List(Of TT)(), u)
                y(u, New List(Of UU)(), New Dictionary(Of List(Of UU), VV)())
                z(v, New List(Of VV)(), New Dictionary(Of List(Of VV), TT)())
            End Sub

            ' Generic Method
            Private Sub bar(Of TT, UU, VV)()
                Console.WriteLine("    c1<T, U>.bar<TT, UU, VV>()")
                Dim ttt As TT = Nothing, uuu As UU = Nothing, vvv As VV = Nothing
                Dim t As T = Nothing, u As U = Nothing, ltt As List(Of TT) = New List(Of TT)()

                ' 5 Levels Deep Nested Lambda, Closures
                Dim func As Func(Of TT, UU, Func(Of UU, VV, Func(Of VV, TT, Func(Of T, U, Func(Of U, T))))) =
                    Function(a, b)
                        Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level1()")
                        Dim v1 As Boolean = ttt.Equals(a)
                        Return Function(aa, bb)
                                   Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level2()")
                                   Dim v2 As Boolean = v1
                                   If ltt.Count >= 0 Then
                                       Dim dtu As Dictionary(Of T, List(Of U)) = New Dictionary(Of T, List(Of U))()
                                       v2 = aa.Equals(b) : aa.Equals(uuu)
                                       Return Function(aaa, bbb)
                                                  Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level3()")
                                                  Dim v3 As Boolean = v1
                                                  If dtu.Count = 0 Then
                                                      v3 = v2
                                                      Dim duuvv As Dictionary(Of List(Of UU), List(Of VV)) = New Dictionary(Of List(Of UU), List(Of VV))()
                                                      If ltt.Count >= 0 Then
                                                          v3 = aaa.Equals(bb)
                                                          v2 = aa.Equals(b)
                                                          aaa.Equals(vvv)
                                                          Return Function(aaaa, bbbb)
                                                                     Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level4()")
                                                                     Dim lu As List(Of U) = New List(Of U)()
                                                                     Dim v4 As Boolean = v3 : v4 = v2 : v4 = v1
                                                                     If duuvv.Count > 0 Then
                                                                         Console.WriteLine("Error - Should not have reached here")
                                                                         Return Nothing
                                                                     Else
                                                                         v4 = aaaa.Equals(t)
                                                                         v3 = aaa.Equals(bb)
                                                                         v2 = aa.Equals(b)
                                                                         Return Function(aaaaa)
                                                                                    Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level5()")
                                                                                    If lu.Count < 0 Then
                                                                                        Console.WriteLine("Error - Should not have reached here")
                                                                                        Return t
                                                                                    Else
                                                                                        v2 = v1 : v3 = v2 : v4 = v3
                                                                                        u.Equals(bbbb)
                                                                                        aa.Equals(b)
                                                                                        aaa.Equals(bb)
                                                                                        aaaa.Equals(t)
                                                                                        Return aaaa
                                                                                    End If
                                                                                End Function
                                                                     End If
                                                                 End Function
                                                      Else
                                                          Console.WriteLine("Error - Should not have reached here")
                                                          Return Nothing
                                                      End If
                                                  Else
                                                      Console.WriteLine("Error - Should not have reached here")
                                                      Return Nothing
                                                  End If
                                              End Function
                                   Else
                                       Console.WriteLine("Error - Should not have reached here")
                                       Return Nothing
                                   End If
                               End Function
                    End Function
                func(ttt, uuu)(uuu, vvv)(vvv, ttt)(t, u)(u)
            End Sub

            Public Sub goo(Of TT, UU, VV)(x As Func(Of TT, UU), y As Func(Of TT, VV), z As Func(Of UU, VV), a As Func(Of UU, TT), b As Func(Of VV, TT), c As Func(Of VV, UU))
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, UU> x, Func<TT, VV> y, Func<UU, VV> z, Func<UU, TT> a, Func<VV, TT> b, Func<VV, UU> c)")
            End Sub

            Public Sub Test()
                Console.WriteLine("c1<T, U>.Test()")
                func(tt, l, uu)
                del1(tt, l, d)
                del2(uu, Nothing, Nothing)

                Dim x As Integer = 0, y As Integer = x
                Dim del As Del = Function(a, b, c) a

                ' Generic Methods, Simple Closures
                goo(Of Integer, String, Del(Of T, U))(y, x.ToString(), Function(a, b, c) uu)
                goo(Of String, Del(Of T), Integer)(x.ToString(), Function(a, b, c) x, y)
                goo(Of Del, Integer, String)(Function(c, a) a.ToString(),
                                             Function(a As Integer, b As String) del,
                                             Function(b, c) y - c(x, y, Nothing))

                ' Generic Type Inference, Nested Lambdas
                goo(x, "", del1)
                'TODO: Below call currently binds to the wrong overload because we don't 
                'have anonymous delegates. Delete this comment once this support becomes available.
                goo(func, del2,
                    Sub(a As T, b As List(Of T), c As Dictionary(Of List(Of T), T))
                        Dim z As Integer = x
                        goo(New Action(Sub() x = 2),
                            New Func(Of ArgumentException, Integer)(Function(aa)
                                                                        Return y + x + +z + b.Count
                                                                    End Function),
                            New Func(Of Exception, Long)(Function(aa As Exception)
                                                             Return y * z * x - c.Count
                                                         End Function))
                        x = z
                        If True Then
                            goo(Function(aa, bb) a.ToString(),
                                Function(bb As Long, cc As String) y - CInt(bb) + b.Count - z,
                                Function(cc As String, aa As Integer) x + y + aa + c.Values.Count)
                        End If
                    End Sub)

                ' Generic Type Inference, Dominant Type
                'TODO: Below call currently binds to the wrong overload because we don't 
                'have anonymous delegates. Delete this comment once this support becomes available.
                goo(Function(a As Exception, b As Exception) New ArgumentException(),
                    Function(a As Exception, b As Exception) New ArgumentException(),
                    Function(a As Exception, b As Exception) New ArgumentException())
                'TODO: Below call currently binds to the wrong overload because we don't 
                'have anonymous delegates. Delete this comment once this support becomes available.
                goo(Function(a, b) New ArgumentException(),
                    Function(a, b) New ArgumentException(),
                    Function(a As ArgumentException, b As Exception) New Exception())
                Dim func2 As Func(Of Exception, ArgumentException) = Function(a As Exception) New ArgumentException()
                Dim func3 As Func(Of ArgumentException, Exception) = Function(a As ArgumentException) New ArgumentException()
                'TODO: Uncomment below call once we have support for variance. See bug 9029.
                'goo(func2, func2, func2, func2, func3, func3)
                goo(Function(a) New ArgumentException(),
                    Function(a As Exception) New InvalidCastException(),
                    Function(a) New InvalidCastException(),
                    Function(a As ArgumentException) New Exception(),
                    Function(a) New ArgumentException(),
                    Function(a As InvalidCastException) New ArgumentException())
                goo(Function(a As Exception) New Exception(),
                    Function(a As Exception) New ArgumentException(),
                    Function(a As Exception) New ArgumentException(),
                    Function(a As Exception) New Exception(),
                    Function(a As Exception) New ArgumentException(),
                    Function(a As Exception) New Exception())
                goo(Function(a) New Exception(),
                    Function(a As Exception) New ArgumentException(),
                    Function(a) New ArgumentException(),
                    Function(a) New Exception(),
                    Function(a) New ArgumentException(),
                    Function(a) New Exception())

                bar(Of Integer, Long, Double)()
            End Sub
        End Class

        Public Class c2(Of T, U) : Inherits c1(Of U, T)
            ' Delegates
            Public Delegate Function Del1(x As T, y As U, z As InvalidCastException, w As ArgumentException) As Exception
            Public Shadows Delegate Sub Del2(Of TT, UU, VV)(x As Func(Of TT, UU, VV), y As Func(Of UU, VV, TT), z As Func(Of VV, TT, UU))
            Protected Delegate Sub Del3(Of TT, UU, VV)(xx As TT, yy As UU, zz As VV)
            Protected Delegate Function Del3(Of TT, UU, VV, WW)(xx As TT, yy As UU, zz As VV) As WW
            Protected Delegate Sub Del4(Of TT, UU, VV)(x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)), y As Del(Of UU, VV), z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))

            Private Sub bar(Of TT, UU, VV)()
                Console.WriteLine("    c2<T, U>.bar<TT, UU, VV>()")
                Dim ttt As TT = Nothing, uuu As UU = Nothing, vvv As VV = Nothing
                Dim t As T = Nothing, u As U = Nothing

                ' Delegate Binding
                'TODO: Below lines are commented because of what seems like a bug in Dev10 that
                'results in bad (unverifiable) code generation. Investigate whether we can
                'work around this somehow.
                'Dim d2 As Del2(Of TT, UU, VV) = AddressOf goo(Of TT, UU, VV) : d2 = AddressOf goo(Of TT, UU, VV)
                'Dim d3 As Del3(Of TT, VV, UU) = AddressOf goo : d3 = AddressOf goo(Of TT, VV, UU)
                Dim d4 As Del4(Of UU, TT, VV) = AddressOf goo : d4 = AddressOf goo
                ' Invoke Delegates
                'd2(Function(a, b) vvv, Function(b, c) ttt, Function(c, a) uuu)
                'd3(ttt, vvv, uuu)
                d4(Function(a, b, c) Nothing, Function(a, b, c) vvv, Sub(a, b, c) uuu.Equals(vvv))

                ' Delegate Binding
                'TODO: Below lines are commented because of what seems like a bug in Dev10 that
                'results in bad (unverifiable) code generation. Investigate whether we can
                'work around this somehow.
                'Dim d22 As Del2(Of Integer, Del, VV) = AddressOf goo(Of Integer, Del, VV) : d22 = AddressOf goo(Of Integer, Del, VV)
                'Dim d32 As Del3(Of Long, Integer, Exception) = AddressOf goo : d32 = AddressOf goo(Of Long, Integer, Exception)
                Dim d42 As Del4(Of T, U, Dictionary(Of List(Of TT), Dictionary(Of List(Of UU), VV))) = AddressOf goo : d42 = AddressOf goo
                ' Invoke Delegates
                'd22(Function(a, b) vvv, Function(b, c) 1, Function(c, a) Nothing)
                'd32(1, 0, Nothing)
                d42(Function(a, b, c) Nothing, Function(a, b, c) Nothing, Sub(a, b, c) uuu.Equals(vvv))

                ' Delegate Relaxation
                Dim d1 As Del1 = AddressOf goo : d1 = AddressOf goo(Of T, U)
                Dim d33 As Del3(Of InvalidCastException, ArgumentNullException, NullReferenceException, Exception) =
                    AddressOf goo(Of Integer, Long) : d33 = AddressOf goo(Of Integer, Long) : d33 = AddressOf goo(Of Integer, Double)
                ' Invoke Delegates
                d1(t, u, Nothing, Nothing)
                d33(New InvalidCastException(), New ArgumentNullException(), New NullReferenceException())

                ' Delegate Relaxation, Generic Methods
                goo(Of ArgumentException, ArgumentException, Exception)(AddressOf goo(Of Integer), AddressOf goo(Of Long), AddressOf goo(Of Double))
                goo(Of ArgumentException, ArgumentException, Exception)(AddressOf goo(Of Exception, ArgumentException),
                                                                        AddressOf goo(Of Exception, ArgumentException),
                                                                        AddressOf goo(Of Exception, ArgumentException))
                goo(Of ArgumentException, ArgumentException, Exception)(AddressOf bar, AddressOf bar, AddressOf bar)
            End Sub

            Private Overloads Function goo(Of TT, UU)(x As Exception, y As Exception, z As Exception) As ArgumentException
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(Exception x, Exception y, Exception z)")
                Return Nothing
            End Function

            Private Overloads Function goo(Of TT, UU)(x As TT, y As UU, a As Exception, b As Exception) As ArgumentException
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(TT x, UU y, Exception a, Exception b)")
                Return Nothing
            End Function

            Private Overloads Function goo(Of TT)(a As Exception, b As Exception) As ArgumentException
                Console.WriteLine("    c2<T, U>.goo<TT>(Exception a, Exception b)")
                Return Nothing
            End Function

            Private Function bar(a As Exception, b As Exception) As ArgumentException
                Console.WriteLine("    c2<T, U>.bar(Exception a, Exception b)")
                Return Nothing
            End Function

            Private Overloads Function goo(Of TT, UU)(a As TT, b As TT) As UU
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(TT a, TT b)")
                Return Nothing
            End Function

            Public Overloads Sub Test()
                Console.WriteLine("c2<T, U>.Test()")
                bar(Of Integer, Long, Double)()
            End Sub
        End Class
    End Namespace
End Namespace

