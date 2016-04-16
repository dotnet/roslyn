Imports System

Public Class LargeMethodTest

    ' Abstract Class
    Public MustInherit Class c0

        ' Abstract Methods
        Public MustOverride Function abst(ByRef x As String, ParamArray y As Integer()) As Integer

        Public MustOverride Function abst(ByRef x As String, ParamArray y As Long()) As Integer

        Public MustOverride Function abst(ByRef x As String, y As Long, z As Long) As Integer
    End Class

    Public Class c1
        Inherits c0

        Private i As Integer = 2

        Friend ui As UInteger = 3

        Public a As c1 = Nothing

        Public Sub New()
            i = 2
            Me.ui = 3
        End Sub

        Public Sub New(x As Integer)
            i = x
            Me.ui = 3
            Me.a = New c1(Me.i, Me.ui, Me.a)
        End Sub

        Public Sub New(x As Integer, y As UInteger, c As c1)
            Me.i = x
            ui = y
            a = New c1()
        End Sub

        Friend Function foo(x As Integer) As Integer
            Return 0
        End Function

        Public Function foo(x As Object) As Boolean
            Return False
        End Function

        ' Overridden Abstract Methods
        Public Overrides Function abst(ByRef x As String, ParamArray y As Integer()) As Integer
            Console.WriteLine("    c1.abst(ref string, params int[])")
            Return 0
        End Function

        Public Overrides Function abst(ByRef x As String, ParamArray y As Long()) As Integer
            Console.WriteLine("    c1.abst(ref string, params long[])")
            x = y(0).ToString()
            y = Nothing
            Return 1
        End Function

        Public Overrides Function abst(ByRef x As String, y As Long, z As Long) As Integer
            Console.WriteLine("    c1.abst(ref string, long, long)")
            x = z.ToString()
            Return 2
        End Function

        ' Virtual Methods
        Public Overridable Function virt(ByRef x As Integer, y As c1, ParamArray z As c2(Of String)()) As Integer
            Console.WriteLine("    c1.virt(ref int, c1, params c2<string>[])")
            x = x + x * 2
            z = Nothing
            Return 0
        End Function

        Public Overridable Function virt(x As Integer, ByRef y As c1) As c2(Of String)
            Console.WriteLine("    c1.virt(int, ref c1)")
            y = New c1()
            Return New c4()
        End Function

        Public Overridable Function virt(ParamArray x As Object()) As Integer
            Console.WriteLine("    c1.virt(params object[])")
            x = New Object() {1, 2, Nothing}
            Return New Integer()
        End Function

        Public Overridable Function virt(ParamArray x As Integer()) As Integer
            Console.WriteLine("    c1.virt(params int[])")
            x = New Integer() {0, 1}
            Dim i As Integer = x(0)
            Return New Integer()
        End Function
    End Class

    Public Class c2(Of T)
        Inherits c1


    End Class

    Public Class c4
        Inherits c2(Of String)

        Public Shared b As Boolean = True

        Public Shared b1 As Byte = 0

        Public Shared sb As SByte = 1

        Private Shared s As Short = 4

        Private Shared us As UShort = 5

        Private Shared l As Long = 6

        Private Shared ul As ULong = 7
    End Class

    Public Class c5

        Friend Shared f As Single = 8.0F

        Friend Shared d As Double = 9.0

        Friend Shared s1 As String = "Test"

        Friend Shared o As Object = Nothing

        Public Function bar(arg As Object) As Object
            Return Nothing
        End Function

        Public Function foo(arg As Object) As Object
            Return Nothing
        End Function
    End Class

    Public Function bar(arg As Object) As Object
        Return Nothing
    End Function

    Public Function foo(arg As Object) As Object
        Return Nothing
    End Function

    Public Function LargeMethod() As Boolean
        Dim str As String = "c4.Test()"
        If True Then
            Dim i As Integer = 2
            Console.WriteLine(str)
            If True Then
                Dim a As c1 = New c1(i)
                a.foo(i)
            End If
            Dim d As Double = 1.1
            If True Then
                Dim sb As SByte = 1
                Dim a As c1 = New c1(i + (i + i))
                a.foo(sb)
                If True Then
                    a.foo(d)
                End If
            End If

            ' Nested scopes
            If True Then
                Dim o As Object = i
                Dim b As Boolean = False
                If Not b Then
                    Dim b1 As Byte = 1
                    Dim s As String = "    This is a test"
                    While Not b
                        If True Then
                            b = True
                        End If

                        Console.WriteLine(s)
                        While b
                            If True Then
                                b = False
                            End If

                            Dim oo As Object = i
                            Dim bb As Boolean = b
                            If Not bb Then
                                If Not False Then
                                    bb = True
                                End If

                                Dim b11 As Byte = b1
                                Dim ss As String = s
                                If bb Then
                                    Console.WriteLine(ss)
                                    If bb <> b Then
                                        Dim ooo As Object = i
                                        Dim bbb As Boolean = bb
                                        If bbb = True Then
                                            Dim b111 As Byte = b11
                                            Dim sss As String = ss
                                            While bbb
                                                Console.WriteLine(sss)
                                                bbb = False
                                                ' Method Calls - Ref, Paramarrays
                                                ' Overloaded Abstract Methods
                                                Dim l As Long = i
                                                Dim c As c4 = New c4()
                                                c.abst(s, 1, i)
                                                c.abst(s, New Integer() {1, i, i})
                                                c.abst(s, c.abst(s, l, l), l, l, l)
                                                ' Method Calls - Ref, Paramarrays
                                                ' Overloaded Virtual Methods
                                                Dim a As c1 = New c4()
                                                c.virt(i, c, New c2(Of String)() {c.virt(i, a), New c4()})
                                                c.virt(c.virt(i, a), c.virt(i, c, c.virt(i, a)))
                                                c.virt(c.abst(s, l, l), c.abst(s, New Long() {1, i, l}))
                                                c.virt(i, a)
                                                c.virt(i, New c4(), New c4(), New c2(Of String)())
                                                c.virt(New Integer() {1, 2, 3})
                                                c.virt(New Exception() {})
                                                c.virt(New c1() {New c4(), New c2(Of String)()})
                                                If True Then
                                                    Continue While
                                                End If
                                            End While
                                        ElseIf bbb <> True Then
                                            Console.WriteLine("Error - Should not have reached here")
                                            o = i
                                            Return DirectCast(o, Boolean)
                                        ElseIf bbb = False Then
                                            Console.WriteLine("Error - Should not have reached here")
                                            o = i
                                            Return DirectCast(o, Boolean)
                                        Else
                                            Console.WriteLine("Error - Should not have reached here")
                                            o = b
                                            Return DirectCast(o, Boolean)
                                        End If
                                    End If
                                ElseIf Not b Then
                                    Console.WriteLine("Error - Should not have reached here")
                                    Dim o1 As Object = b
                                    Return DirectCast(o1, Boolean)
                                Else
                                    Console.WriteLine("Error - Should not have reached here")
                                    Dim o1 As Object = b
                                    Return DirectCast(o1, Boolean)
                                End If
                            ElseIf Not bb Then
                                Console.WriteLine("Error - Should not have reached here")
                                o = b
                                Return DirectCast(o, Boolean)
                            Else
                                Console.WriteLine("Error - Should not have reached here")
                                Dim o1 As Object = b
                                Return DirectCast(o1, Boolean)
                            End If

                            While b <> False
                                b = False
                                Exit While
                            End While

                            Exit While
                        End While

                        While b <> True
                            b = True
                            Continue While
                        End While
                    End While
                ElseIf b Then
                    Console.WriteLine("Error - Should not have reached here")
                    Return b
                Else
                    Console.WriteLine("Error - Should not have reached here")
                    Return b <> True
                End If
            End If
        End If
        Dim us As Integer = 0
        Dim sh As Short = 1
        Dim cc As c5 = New c5()
        Console.WriteLine("c5.test")
        If True Then
            Dim ui As UInteger = 1
            Dim o As Object = ui
            Dim i As Integer = sh
            Dim b As Boolean = False
            us = 1
            If True Then
                Dim b1 As Byte = 1
                Dim l As Long = i
                Dim s1 As String = ""
                Dim s As String = ""
                Dim f As Single = 1.2F
                o = f
                l = ui
                Dim c As c4 = New c4()
                c.foo(sh)
                Me.bar(sh)
                If b = False Then
                    Dim d As Double = f
                    s1 = s
                End If

                If b1 >= l Then
                    Dim ui1 As UInteger = 1
                    o = ui1
                    i = sh
                    b = False
                    us = 1
                    While i <> 1000
                        Dim b11 As Byte = 1
                        Dim l1 As Long = i
                        Dim s11 As String = s1
                        Dim f1 As Single = 1.2F
                        o = f1
                        l1 = ui1
                        c.foo(b)
                        b11 = l1
                        If Not False Then
                            Dim d1 As Double = f1
                            s1 = s
                            c.foo(b1)
                        End If

                        If i <> 1000 Then
                            Dim ui2 As UInteger = 1
                            o = ui2
                            i = sh
                            b = False
                            us = 1
                            If True Then
                                Dim l2 As Long = i
                                Dim s12 As String = s11
                                o = f1
                                l2 = ui1
                                If i <= 1000 Then
                                    Exit While
                                End If
                            End If
                        End If

                        If i <= 1000 Then
                            i = 1000
                        End If

                        Return False
                    End While
                End If
            End If
        End If
        Return us = 0


    End Function
End Class