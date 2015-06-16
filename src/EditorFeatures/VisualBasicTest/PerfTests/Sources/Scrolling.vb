' Additional namespace added to better match LOC of C# scrolling file
Imports System

Namespace ns1
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns2
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns3
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns4
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns5
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns6
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns7
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns8
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace


Namespace ns9
    Public Class Test
        Public Shared Sub Run()
            Dim a As c1 = New c1() : a.test()

            Dim b As c2(Of String) = New c2(Of String)() : b.TEST1()

            c3(Of String, String).test()

            c4.Test2()

            Dim d As c4.c5 = New c4.c5() : d.Test1()
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
                        Me.foo(o) : foo(i) : Me.foo(b) : Me.foo(b1) ' Overload Resolution, Implicit Conversions
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

        Friend Function foo(x As Integer) As Integer
            Console.WriteLine("    c1.foo(int)")

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

        Public Function foo(x As Object) As Boolean
            Console.WriteLine("    c1.foo(object)")

            ' Read, Write Fields
            ui = 0UI
            Me.i = Me.i - 1
            a = Nothing
            Dim ui1 As UInteger = ui : Dim i1 As Integer = i

            ' Read, Write Locals
            Dim b As Boolean = True : Dim s As String = String.Empty
            s = Nothing : b = Me.i <> 1
            ui = ui1 : i = i1
            bar4(b) : Me.foo(i1) : bar4(b = (True <> b))

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
            Me.foo(i.GetHashCode()) : Me.a = Me

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
            Me.foo(c.i) : bar3(c IsNot Nothing)

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
            s = a1(1) : foo(a1(2))
        End Sub

        Protected Function bar2(x As Object) As String
            Console.WriteLine("    c1.bar2(object)")

            ' Read, Write Fields
            Me.ui = ui - Me.ui
            i = i / 1
            a = Nothing
            foo(i)

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
            foo(x.GetHashCode)

            ' Read, Write Array Element
            Dim a1 As Boolean() = New Boolean() {True, False, x}
            a1(1) = x = (Me.i <> i - 1 + 1) : a1(2) = x = (i >= Me.i + 1 - 1)
            b = (a1(1)) : b = a1(2)
            Dim o As Object = b <> a1(2)
            o = (a1(1).ToString()) = (a1(2).ToString())
            foo(a1(1).GetHashCode())

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
            foo(Me.i.GetHashCode())

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
            foo(c.GetHashCode()) : bar3(c.a.GetHashCode() <> i)

            ' Read, Write Params
            x = (o.ToString())
            x = x.ToString() : foo(x.GetHashCode()) : foo(x.ToString().GetHashCode())

            ' Read, Write Array Element
            Dim a1 As Object() = New Object() {(Nothing), (Me.a), c}
            a1(1) = ((Me.a)) : a1(2) = (c) : a1(1) = (i)
            Array.Reverse(a1)
            o = a1(1) : foo(a1.GetHashCode()) : bar3(a1(2) Is Nothing)

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
                        Me.foo(x:=b, y:=sb) ' Named Arguments
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
                        foo(x:=b, y:=sb2) ' Named Arguments
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
                    Me.bar4(const1) : c.foo(const2 <> const2) : Me.a = const3
                End If
            End If
        End Sub

        Private Function foo1(x As T) As T
            Console.WriteLine("    c2<T>.foo1(T)")

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
                Me.bar4(const1) : c.foo(const2 <> const2)
                Return x
            Loop
            Return x
        End Function

        Private Overloads Function foo(x As Boolean) As Boolean
            Console.WriteLine("    c2<T>.foo(bool)")

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

        Protected Overloads Function foo(x As Byte, y As Object) As c1
            Console.WriteLine("    c2<T>.foo(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : c.foo(x)

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
                        Me.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            c.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            Const const2 As UInteger = 1
            Do While const1 <> x
                Continue Do
            Loop

            Do While const2 = const2
                Me.bar4(const1) : Me.foo(const2 <> const2)
                Exit Do
            Loop
        End Sub

        Public Overloads Function bar2(x As Byte, y As Object) As Integer
            Console.WriteLine("    c2<T>.bar2(byte, object)")

            ' Read, Write Params
            y = x : x = 1
            Dim c As c1 = New c1()
            c.bar4(y) : Me.foo(x)

            ' Read Consts
            Const const1 As Long = 1
            If Not False Then
                Const const2 As SByte = 1
                Const const3 As Object = Nothing
                If c IsNot const3 Then
                    c.bar4(const1) : Me.foo(const2 <> const2) : Me.a = const3
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
            Me.bar4(y) : c.foo(x)

            ' Read Consts
            Const const1 As String = "hi"
            Dim b As Boolean = Not False
            If b Then
                Const const2 As Byte = 1
                Const const3 As Object = Nothing
                If const3 IsNot c Then
                    Me.bar4(const1) : c.foo(const2 <> const2) : c.a = const3
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
                foo() : foo(1) : foo("1") : foo(1.1) ' Overload Resolution, Implicit Conversions
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
        Protected Shared Function foo(x As T, y As U) As Integer
            Console.WriteLine("    c3<T, U>.foo(T, U)")
            Dim a As Integer() = New Integer(2) {1, 2, 3} : a(1) = a(2)
            Return CType((CType(x.GetHashCode(), Long) + CType(CInt(CLng(y.GetHashCode())), Long)), Integer)
        End Function

        Friend Shared Function foo(x As Object) As c1
            Console.WriteLine("    c3<T, U>.foo(object)")
            Dim a As c1() = New c1(2) {Nothing, New c1(), New c1(1)} : a(1) = a(2)
            x = "hi"
            Return New c1(1.1F, CUInt(1), New c1(x.GetHashCode()))
        End Function

        Private Shared Function foo(x As String) As Single
            Console.WriteLine("    c3<T, U>.foo(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            Return foo(x.GetHashCode())
        End Function

        Public Shared Function foo(x As Integer) As Integer
            Console.WriteLine("    c3<T, U>.foo(int)")
            Dim a As Integer() = New Integer() {x, x, 1, 0} : a(1) = a(2) : a(2) = a(1)
            Return CInt(x.GetHashCode()) + x
        End Function

        Public Shared Function foo() As String
            Console.WriteLine("    c3<T, U>.foo()")
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
            Return New c1(CType(1.1F, Integer), CType(1, UInteger), New c1(x.GetHashCode()))
        End Function

        Public Function bar(x As String) As Single
            Console.WriteLine("    c3<T, U>.bar(string)")
            Dim a As String() = New String() {x, x, "", Nothing} : a(1) = a(2) : a(2) = a(1)
            x = a(2)
            Return CSng(foo(x.GetHashCode()))
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
                    Dim a As c1 = New c1(i) : a.foo(i)
                End If
                Dim d As Double = 1.1
                If Not (Not (Not False)) Then
                    Dim sb As SByte = 1
                    Dim a As c1 = New c1(i + (i + i))
                    a.foo(sb)
                    If True Then
                        a.foo(d)
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
                                                    s = CType(us, Short)
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
                                    Return CType(o1, Boolean)
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
        Public Overloads Shared Function foo(i As Integer, s As String, b As Boolean, b1 As Byte, l As Long, s1 As String) As c4
            Console.WriteLine("    c4.foo(int, string, bool, byte, long, string)")
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
                        c4.foo(sh, s, b, b1, i, s1) ' Implicit Conversions
                        Dim c As c4 = New c4()
                        c.foo(sh) : Me.bar(sh)
                        cc.bar(c5.foo(cc.bar()))
                        c5.foo(cc.bar(c5.foo()))
                        If b = False Then
                            Dim d As Double = f, ul As ULong = 1, sb As SByte = 1 : s1 = s
                            c4.bar(sh, us, sb, f, d, ui, ul) ' Implicit Conversions
                            c.bar4(us)
                            Me.bar(cc.bar(), c)
                            c5.foo(Me.bar(c5.foo(), c))
                        End If
                        If b1 >= l Then
                            Dim ui1 As UInteger = 1 : o = ui1
                            i = sh : b = False : us = 1
                            Do While i <> 1000
                                Dim b11 As Byte = 1, l1 As Long = i, s11 As String = s1
                                Dim f1 As Single = 1.2F : o = f1 : l1 = ui1
                                c4.foo(sh, s1, b, b11, i, s11) ' Implicit Conversions
                                c.foo(b)
                                Me.bar(b) : If c5.foo() IsNot Nothing Then c5.foo().ToString().GetHashCode()
                                cc.bar(Me.bar(c5.foo()))

                                If Not False Then
                                    Dim d1 As Double = f1, ul1 As ULong = 1, sb1 As SByte = 1 : s1 = s
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1) ' Implicit Conversions
                                    c.foo(b1, sb1)
                                    Me.bar(o).bar4(c)
                                    cc.bar(c5.foo(o)).bar4(c).ToString()
                                    d1 = d
                                    If d <> d1 Then Return i
                                End If
                                If i <> 1000 Then
                                    Dim ui2 As UInteger = 1 : o = ui2
                                    i = sh : b = False : us = 1
                                    If True Then
                                        Dim b12 As Byte = 1, l2 As Long = i, s12 As String = s11
                                        Dim f2 As Single = 1.2F : o = f1 : l2 = ui1
                                        c4.foo(sh, s1, b, b12, i, s12) ' Implicit Conversions
                                        c.bar4(b.ToString() = b.ToString())
                                        Me.bar(c5.foo(cc.bar(i)))
                                        If Not False Then
                                            Dim d2 As Double = f2, ul2 As ULong = 1, sb2 As SByte = 1 : s1 = s
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2) ' Implicit Conversions
                                            c.foo(False = True <> False = b)
                                            c.bar4(sh > us = sh <= us)
                                            Me.bar(TryCast(c5.foo(TryCast(cc.bar(TryCast(i, Object)), Object)), Object))
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
End Namespace