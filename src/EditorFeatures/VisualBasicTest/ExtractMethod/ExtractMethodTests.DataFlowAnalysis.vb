' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        ''' <summary>
        ''' This contains tests for Extract Method scenarios that depend on Data Flow Analysis API
        ''' Implements scenarios outlined in /Services/CSharp/Impl/Refactoring/ExtractMethod/ExtractMethodMatrix.xlsx
        ''' </summary>
        ''' <remarks></remarks>
        Public Class DataFlowPass

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod1()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer
        i = 10|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod2()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer = 10
        Dim i2 As Integer = 10|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Dim i2 As Integer = 10
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod3()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        [|Dim i2 As Integer = i|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim i2 As Integer = i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod4()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        Dim i2 As Integer = i

        [|i2 += i|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        Dim i2 As Integer = i
        i2 = NewMethod(i)
    End Sub

    Private Shared Function NewMethod(i As Integer, i2 As Integer) As Integer
        i2 += i
        Return i2
    End Function
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod5()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        Dim i2 As Integer = i

        [|i2 = i|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        Dim i2 As Integer = i

        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim i2 As Integer = i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod6()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Dim field As Integer

    Sub Test(args As String())
        Dim i As Integer = 10
        [|field = i|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Dim field As Integer

    Sub Test(args As String())
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Sub NewMethod(i As Integer)
        field = i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod7()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim a As String() = Nothing
        [|Test(a)|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim a As String() = Nothing
        NewMethod(a)
    End Sub

    Private Sub NewMethod(a() As String)
        Test(a)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod8()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Test(args As String())
        Dim a As String() = Nothing
        [|Test(a)|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Test(args As String())
        Dim a As String() = Nothing
        NewMethod(a)
    End Sub

    Private Shared Sub NewMethod(a() As String)
        Test(a)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod9()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer
        Dim s As String
        [|i = 10
        s = args(0) + i.ToString()|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        NewMethod(args)
    End Sub

    Private Shared Sub NewMethod(args() As String)
        Dim i As Integer
        Dim s As String
        i = 10
        s = args(0) + i.ToString()
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod10()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer
        i = 10
        Dim s As String
        s = args(0) + i.ToString()|]
        Console.WriteLine(s)
    End Sub

End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim s As String = NewMethod(args)
        Console.WriteLine(s)
    End Sub

    Private Shared Function NewMethod(args() As String) As String
        Dim i As Integer = 10
        Dim s As String
        s = args(0) + i.ToString()
        Return s
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod11()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer
        Dim i2 As Integer = 10|]
        i = 10
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer
        NewMethod()
        i = 10
    End Sub

    Private Shared Sub NewMethod()
        Dim i2 As Integer = 10
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod11_1()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer
        Dim i2 As Integer = 10|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        Dim i2 As Integer = 10
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod12()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        [|i = i + 1|]
        Console.WriteLine(i)
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = 10
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        i = i + 1
        Return i
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod13()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        For Each s In args
            [|Console.WriteLine(s)|]
        Next
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        For Each s In args
            NewMethod(s)
        Next
    End Sub

    Private Shared Sub NewMethod(s As String)
        Console.WriteLine(s)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod14()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        i = 1
        While i &lt; 10
            [|Console.WriteLine(i)|]
            i = i + 1
        End While
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        i = 1
        While i &lt; 10
            NewMethod(i)
            i = i + 1
        End While
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
    End Sub
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod15()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim s As Integer = 10, i As Integer = 1
        Dim b As Integer = s + i|]

        System.Console.WriteLine(s)
        System.Console.WriteLine(i)
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim s As Integer = Nothing
        Dim i As Integer = Nothing
        NewMethod(s, i)

        System.Console.WriteLine(s)
        System.Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod(ByRef s As Integer, ByRef i As Integer)
        s = 10
        i = 1
        Dim b As Integer = s + i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod16()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        [|Dim i As Integer = 1|]
        System.Console.WriteLine(i)
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(args As String())
        Dim i As Integer = NewMethod()
        System.Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539197)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod17()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(Of T)(ByRef t2 As T)
        [|Dim t1 As T
        Test(t1)
        t2 = t1|]
        System.Console.WriteLine(t1.ToString())
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(Of T)(ByRef t2 As T)
        Dim t1 As T = Nothing
        NewMethod(t2, t1)
        System.Console.WriteLine(t1.ToString())
    End Sub

    Private Sub NewMethod(Of T)(ByRef t2 As T, ByRef t1 As T)
        Test(t1)
        t2 = t1
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(527775)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod18()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(Of T)(ByRef t3 As T)
        [|Dim t1 As T = GetValue(t3)|]
        System.Console.WriteLine(t1.ToString())
    End Sub

    Private Function GetValue(Of T)(ByRef t2 As T) As T
        Return t2
    End Function
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(Of T)(ByRef t3 As T)
        Dim t1 As T = Nothing
        NewMethod(t3, t1)
        System.Console.WriteLine(t1.ToString())
    End Sub

    Private Sub NewMethod(Of T)(ByRef t3 As T, ByRef t1 As T)
        t1 = GetValue(t3)
    End Sub

    Private Function GetValue(Of T)(ByRef t2 As T) As T
        Return t2
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod19()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        [|Dim i As Integer = 1|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 1
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod20()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Test()
        [|Dim i As Integer = 1|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Test()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim i As Integer = 1
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod21()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        [|Dim i As Integer = 1|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 1
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod22()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        Dim i As Integer
        [|Dim b As Integer = 10
        If b &lt; 10
            i = 5
        End If|]
        i = 6
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        Dim i As Integer
        NewMethod(i)
        i = 6
        Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim b As Integer = 10
        If b &lt; 10
            i = 5
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod23()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If True
            [|Console.WriteLine(args(0).ToString())|]
        End If
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If True
            NewMethod(args)
        End If
    End Sub

    Private Shared Sub NewMethod(args() As String)
        Console.WriteLine(args(0).ToString())
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod24()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim y As Integer = [|Integer.Parse(args(0).ToString())|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim y As Integer = GetY(args)
    End Sub

    Private Shared Function GetY(args() As String) As Integer
        Return Integer.Parse(args(0).ToString())
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod25()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If([|New Integer(){ 1, 2, 3 }|]).Any() Then
            Return
        End If
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If (NewMethod()).Any() Then
            Return
        End If
    End Sub

    Private Shared Function NewMethod() As Integer()
        Return New Integer() {1, 2, 3}
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod26()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If [|(New Integer(){ 1, 2, 3 })|].Any()
            Return
        End If
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        If NewMethod().Any()
            Return
        End If
    End Sub

    Private Shared Function NewMethod() As Integer()
        Return (New Integer() {1, 2, 3})
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod27()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        Dim i As Integer = 1
        [|Dim b As Integer = 10
        If b &lt; 10
            i = 5
        End If|]
        i = 6
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test()
        Dim i As Integer = 1
        NewMethod(i)
        i = 6
        Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim b As Integer = 10
        If b &lt; 10
            i = 5
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540046)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod28()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Function Test() As Integer
        [|Return 1|]
    End Function
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Function Test() As Integer
        Return NewMethod()
    End Function

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540046)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod29()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Function Test() As Integer
        Dim i As Integer = 0
        [|If i &lt; 0
            Return 1
        Else
            Return 0
        End If|]
    End Function
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Function Test() As Integer
        Dim i As Integer = 0
        Return NewMethod(i)
    End Function

    Private Shared Function NewMethod(i As Integer) As Integer
        If i &lt; 0
            Return 1
        Else
            Return 0
        End If
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod30()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(ByRef i As Integer)
        [|i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Test(ByRef i As Integer)
        i = NewMethod()
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod31()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Text

Class Program
    Sub Test()
        Dim builder As StringBuilder = New StringBuilder()
        [|builder.Append("Hello")
        builder.Append("From")
        builder.Append("Roslyn")|]
        Return builder.ToString()
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Text

Class Program
    Sub Test()
        Dim builder As StringBuilder = New StringBuilder()
        NewMethod(builder)
        Return builder.ToString()
    End Sub

    Private Shared Sub NewMethod(builder As StringBuilder)
        builder.Append("Hello")
        builder.Append("From")
        builder.Append("Roslyn")
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod32()
                Dim code = <text>Imports System

Class Program
    Sub Test()
        Dim v As Integer = 0
        Console.Write([|v|])
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test()
        Dim v As Integer = 0
        Console.Write(GetV(v))
    End Sub

    Private Shared Function GetV(v As Integer) As Integer
        Return v
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod34()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim x As Integer = 1
        Dim y As Integer = 2
        Dim z As Integer = [|x + y|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim x As Integer = 1
        Dim y As Integer = 2
        Dim z As Integer = GetZ(x, y)
    End Sub

    Private Shared Function GetZ(x As Integer, y As Integer) As Integer
        Return x + y
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538239)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod35()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim r As Integer() = [|New Integer(){ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim r As Integer() = GetR()
    End Sub

    Private Shared Function GetR() As Integer()
        Return New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20}
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod36()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(ByRef i As Integer)
        [|i = 1|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(ByRef i As Integer)
        i = NewMethod()
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod37()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(ByRef i As Integer)
        [|i = 1|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(ByRef i As Integer)
        i = NewMethod()
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538231)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod38()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        &apos; int v = 0;
        &apos; while (true)
        Dim unassigned As Integer
        &apos; {
        &apos; NewMethod(v++);
        [|unassigned = unassigned + 10|]

        &apos; NewMethod(ReturnVal(v++));

        &apos; }
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        &apos; int v = 0;
        &apos; while (true)
        Dim unassigned As Integer
        &apos; {
        &apos; NewMethod(v++);
        NewMethod(unassigned)

        &apos; NewMethod(ReturnVal(v++));

        &apos; }
    End Sub

    Private Shared Sub NewMethod(unassigned As Integer)
        unassigned = unassigned + 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538231)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod39()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        &apos; int v = 0;
        &apos; while (true)

        Dim unassigned As Integer
        &apos; {
        [|&apos; NewMethod(v++);
        unassigned = unassigned + 10

        &apos; NewMethod(ReturnVal(v++));|]

        &apos; }

    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        &apos; int v = 0;
        &apos; while (true)

        Dim unassigned As Integer
        &apos; {
        NewMethod(unassigned)

        &apos; }

    End Sub

    Private Shared Sub NewMethod(unassigned As Integer)
        &apos; NewMethod(v++);
        unassigned = unassigned + 10

        &apos; NewMethod(ReturnVal(v++));
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538303)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod40()
                Dim code = <text>Class Program
    Shared Sub Main(args As String())
        [|Dim x As Integer|]
    End Sub
End Class</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(538314)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod41()
                Dim code = <text>Class Program
    Shared Sub Main(args As String())
        Dim x As Integer = 10
        [|Dim y As Integer
        If x = 10
            y = 5
        End If|]
        Console.WriteLine(y)
    End Sub
End Class</text>
                Dim expected = <text>Class Program
    Shared Sub Main(args As String())
        Dim x As Integer = 10
        Dim y As Integer = NewMethod(x)
        Console.WriteLine(y)
    End Sub

    Private Shared Function NewMethod(x As Integer) As Integer
        Dim y As Integer
        If x = 10
            y = 5
        End If

        Return y
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(527499)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix3992()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        [|While False
            Console.WriteLine(x)
        End While|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        NewMethod(x)
    End Sub

    Private Shared Sub NewMethod(x As Integer)
        While False
            Console.WriteLine(x)
        End While
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538327)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod42()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer, b As Integer
        [|a = 5
        b = 7|]
        Console.Write(a + b)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer, b As Integer
        NewMethod(a, b)
        Console.Write(a + b)
    End Sub

    Private Shared Sub NewMethod(ByRef a As Integer, ByRef b As Integer)
        a = 5
        b = 7
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538327)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod43()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer, b As Integer
        [|a = 5
        b = 7
        Dim c As Integer
        Dim d As Integer
        Dim e As Integer, f As Integer
        c = 1
        d = 1
        e = 1
        f = 1|]
        Console.Write(a + b)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer, b As Integer
        NewMethod(a, b)
        Console.Write(a + b)
    End Sub

    Private Shared Sub NewMethod(ByRef a As Integer, ByRef b As Integer)
        a = 5
        b = 7
        Dim c As Integer
        Dim d As Integer
        Dim e As Integer, f As Integer
        c = 1
        d = 1
        e = 1
        f = 1
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538328)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod44()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer
        &apos; comment
        [|a = 1|]
        &apos; comment
        Console.Write(a)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main(args As String())
        Dim a As Integer
        &apos; comment
        a = NewMethod()
        &apos; comment
        Console.Write(a)
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538393)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod46()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        [|Foo(x)|]
        Console.WriteLine(x)
    End Sub

    Shared Sub Foo(ByRef x As Integer)
        x = x + 1
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        x = NewMethod(x)
        Console.WriteLine(x)
    End Sub

    Private Shared Function NewMethod(x As Integer) As Integer
        Foo(x)
        Return x
    End Function

    Shared Sub Foo(ByRef x As Integer)
        x = x + 1
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538399)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod47()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        [|While True
            Console.WriteLine(x)
        End While|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer = 1
        NewMethod(x)
    End Sub

    Private Shared Sub NewMethod(x As Integer)
        While True
            Console.WriteLine(x)
        End While
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538401)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod48()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer() = [|{ 1, 2, 3 }|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Integer() = GetX()
    End Sub

    Private Shared Function GetX() As Integer()
        Return {1, 2, 3}
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538405)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethod49()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Foo(GetX As Integer)
        Dim x As Integer = [|1|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Foo(GetX As Integer)
        Dim x As Integer = GetX1()
    End Sub

    Private Shared Function GetX1() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodNormalProperty()
                Dim code = <text>Class [Class]
    Private Shared name As String

    Public Shared Property Names As String
        Get
            Return 1
        End Get
        Set
            name = value
        End Set
    End Property

    Shared Sub Foo(i As Integer)
        Dim str As String = [|[Class].Names|]
    End Sub
End Class</text>
                Dim expected = <text>Class [Class]
    Private Shared name As String

    Public Shared Property Names As String
        Get
            Return 1
        End Get
        Set
            name = value
        End Set
    End Property

    Shared Sub Foo(i As Integer)
        Dim str As String = GetStr()
    End Sub

    Private Shared Function GetStr() As String
        Return [Class].Names
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538932)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodAutoProperty()
                Dim code = <text>Class [Class]
    Public Property Name As String

    Shared Sub Main()
        Dim str As String = New [Class]().[|Name|]
    End Sub
End Class</text>

                Dim expected = <text>Class [Class]
    Public Property Name As String

    Shared Sub Main()
        Dim str As String = GetStr()
    End Sub

    Private Shared Function GetStr() As String
        Return New [Class]().Name
    End Function
End Class</text>

                ' given span is not an expression, use suggestion
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538402)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix3994()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Byte = [|1|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Byte = GetX()
    End Sub

    Private Shared Function GetX() As Byte
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538404)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix3996()
                Dim code = <text>Class A(Of T)
    Class D
        Inherits A(Of T)
    End Class

    Class B
    End Class

    Shared Function Foo() As D.B
        Return Nothing
    End Function

    Class C(Of T2)
        Shared Sub Bar()
            Dim x As D.B = [|Foo()|]
        End Sub
    End Class
End Class</text>
                Dim expected = <text>Class A(Of T)
    Class D
        Inherits A(Of T)
    End Class

    Class B
    End Class

    Shared Function Foo() As D.B
        Return Nothing
    End Function

    Class C(Of T2)
        Shared Sub Bar()
            Dim x As D.B = GetX()
        End Sub

        Private Shared Function GetX() As B
            Return Foo()
        End Function
    End Class
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub InsertionPoint()
                Dim code = <text>Class Test
    Sub Method(i As String)
        Dim y2 As Integer = [|1|]
    End Sub

    Sub Method(i As Integer)
    End Sub
End Class</text>
                Dim expected = <text>Class Test
    Sub Method(i As String)
        Dim y2 As Integer = GetY2()
    End Sub

    Private Shared Function GetY2() As Integer
        Return 1
    End Function

    Sub Method(i As Integer)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538980)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4757()
                Dim code = <text>Class GenericMethod
    Sub Method(Of T)(t1 As T)
        Dim a As T
        [|a = t1|]
    End Sub
End Class</text>
                Dim expected = <text>Class GenericMethod
    Sub Method(Of T)(t1 As T)
        NewMethod(t1)
    End Sub

    Private Shared Sub NewMethod(Of T)(t1 As T)
        Dim a As T = t1
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538980)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4757_2()
                Dim code = <text>Class GenericMethod(Of T1)
    Sub Method(Of T)(t1 As T)
        Dim a As T
        Dim b As T1
        [|a = t1
        b = Nothing|]
    End Sub
End Class</text>
                Dim expected = <text>Class GenericMethod(Of T1)
    Sub Method(Of T)(t1 As T)
        NewMethod(t1)
    End Sub

    Private Shared Sub NewMethod(Of T)(t1 As T)
        Dim a As T
        Dim b As T1
        a = t1
        b = Nothing
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538980)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4757_3()
                Dim code = <text>Class GenericMethod
    Sub Method(Of T, T1)(t1 As T)
        Dim a1 As T1
        Dim a As T
        [|a = t1
        a1 = Nothing|]
    End Sub
End Class</text>
                Dim expected = <text>Class GenericMethod
    Sub Method(Of T, T1)(t1 As T)
        NewMethod(Of T, T1)(t1)
    End Sub

    Private Shared Sub NewMethod(Of T, T1)(t1 As T)
        Dim a1 As T1
        Dim a As T
        a = t1
        a1 = Nothing
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538422)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4758()
                Dim code = <text>Imports System

Class TestOutParameter
    Sub Method(ByRef x As Integer)
        x = 5
        Console.Write([|x|])
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class TestOutParameter
    Sub Method(ByRef x As Integer)
        x = 5
        Console.Write(GetX(x))
    End Sub

    Private Shared Function GetX(x As Integer) As Integer
        Return x
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538422)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4758_2()
                Dim code = <text>Class TestOutParameter
    Sub Method(ByRef x As Integer)
        x = 5
        Console.Write([|x|])
    End Sub
End Class</text>
                Dim expected = <text>Class TestOutParameter
    Sub Method(ByRef x As Integer)
        x = 5
        Console.Write(GetX(x))
    End Sub

    Private Shared Function GetX(x As Integer) As Integer
        Return x
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538984)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4761()
                Dim code = <text>Class A
    Sub Method()
        Dim a As System.Func(Of Integer, Integer) = Function(x) [|x * x|]
    End Sub
End Class</text>
                Dim expected = <text>Class A
    Sub Method()
        Dim a As System.Func(Of Integer, Integer) = Function(x) NewMethod(x)
    End Sub

    Private Shared Function NewMethod(x As Integer) As Integer
        Return x * x
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538997)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4779()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Func(Of String) = [|s|].ToString
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Func(Of String) = GetS(s).ToString
    End Sub

    Private Shared Function GetS(s As String) As String
        Return s
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538997)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4779_2()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f = [|s|].ToString()
    End Sub

End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f = GetS(s).ToString()
    End Sub

    Private Shared Function GetS(s As String) As String
        Return s
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(4780, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4780()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = DirectCast([|s.ToString|], Func(Of String))
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = DirectCast(GetToString(s), Func(Of String))
    End Sub

    Private Shared Function GetToString(s As String) As Func(Of String)
        Return s.ToString
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539201)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4780_2()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = DirectCast([|s.ToString()|], String)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = DirectCast(NewMethod(s), String)
    End Sub

    Private Shared Function NewMethod(s As String) As String
        Return s.ToString()
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539201)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4780_3()
                Dim code = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = TryCast([|s.ToString()|], String)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Shared Sub Main()
        Dim s As String = ""
        Dim f As Object = TryCast(NewMethod(s), String)
    End Sub

    Private Shared Function NewMethod(s As String) As String
        Return s.ToString()
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(4782, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4782_2()
                Dim code = <text>Class A(Of T)
    Class D
        Inherits A(Of T())
    End Class

    Class B
    End Class

    Class C(Of T)
        Shared Sub Foo()
            Dim x As D.B = [|New D.B()|]
        End Sub
    End Class

End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(4791, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4791()
                Dim code = <text>Class Program
    Delegate Function Func(a As Integer) As Integer

    Shared Sub Main(args As String())
        Dim v As Func = Function(a As Integer) [|a|]
    End Sub
End Class</text>
                Dim expected = <text>Class Program
    Delegate Function Func(a As Integer) As Integer

    Shared Sub Main(args As String())
        Dim v As Func = Function(a As Integer) GetA(a)
    End Sub

    Private Shared Function GetA(a As Integer) As Integer
        Return a
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539019)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4809()
                Dim code = <text>Class Program
    Public Sub New()
        [|Dim x As Integer = 2|]
    End Sub
End Class</text>
                Dim expected = <text>Class Program
    Public Sub New()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim x As Integer = 2
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(551797)>
            <WorkItem(539029)>
            <WpfFact(Skip:="551797"), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4813()
                Dim code = <text>Imports System

Class Program
    Public Sub New()
        Dim o As [Object] = [|New Program()|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Public Sub New()
        Dim o As [Object] = GetO()
    End Sub

    Private Shared Function GetO() As Program
        Return New Program()
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538425)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4031()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Boolean = True, y As Boolean = True, z As Boolean = True
        If x
            While y
            End While
        Else
            [|While z
            End While|]
        End If
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim x As Boolean = True, y As Boolean = True, z As Boolean = True
        If x
            While y
            End While
        Else
            NewMethod(z)
        End If
    End Sub

    Private Shared Sub NewMethod(z As Boolean)
        While z
        End While
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539029)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4823()
                Dim code = <text>Class Program
    Private area As Double = 1.0

    Public ReadOnly Property Area As Double
        Get
            Return area
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return String.Format("{0:F2}", [|Area|])
    End Function
End Class</text>
                Dim expected = <text>Class Program
    Private area As Double = 1.0

    Public ReadOnly Property Area As Double
        Get
            Return area
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return String.Format("{0:F2}", GetArea())
    End Function

    Private Function GetArea() As Double
        Return Area
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538985)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4762()
                Dim code = <text>Class Program
    Shared Sub Main(args As String())
        &apos;comments
        [|Dim x As Integer = 2|]
    End Sub
End Class</text>
                Dim expected = <text>Class Program
    Shared Sub Main(args As String())
        &apos;comments
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim x As Integer = 2
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(538966)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix4744()
                Dim code = <text>Class Program
    Shared Sub Main(args As String())
        [|Dim x As Integer = 2
        &apos;comments|]
    End Sub
End Class</text>
                Dim expected = <text>Class Program
    Shared Sub Main(args As String())
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim x As Integer = 2
        &apos;comments
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539049)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodInProperty1()
                Dim code = <text>Class C2
    Shared Public ReadOnly Property Area As Integer
        Get
            Return 1
        End Get
    End Property
End Class

Class C3
    Public Shared ReadOnly Property Area As Integer
        Get
            Return [|C2.Area|]
        End Get
    End Property
End Class</text>
                Dim expected = <text>Class C2
    Shared Public ReadOnly Property Area As Integer
        Get
            Return 1
        End Get
    End Property
End Class

Class C3
    Public Shared ReadOnly Property Area As Integer
        Get
            Return GetArea()
        End Get
    End Property

    Private Shared Function GetArea() As Integer
        Return C2.Area
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539049)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodInProperty2()
                Dim code = <text>Class C3
    Public Shared ReadOnly Property Area As Integer
        Get
            [|Dim i As Integer = 10
            Return i|]
        End Get
    End Property
End Class</text>
                Dim expected = <text>Class C3
    Public Shared ReadOnly Property Area As Integer
        Get
            Return NewMethod()
        End Get
    End Property

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539049)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodInProperty3()
                Dim code = <text>Class C3
    Public Shared WriteOnly Property Area As Integer
        Set(value As Integer)
            [|Dim i As Integer = value|]
        End Set
    End Property

End Class</text>
                Dim expected = <text>Class C3
    Public Shared WriteOnly Property Area As Integer
        Set(value As Integer)
            NewMethod(value)
        End Set
    End Property

    Private Shared Sub NewMethod(value As Integer)
        Dim i As Integer = value
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoNoYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test1()
        Dim i As Integer
        [|If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test1()
        Dim i As Integer
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoNoYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test2()
        Dim i As Integer = 0
        [|If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test2()
        Dim i As Integer = 0
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test3()
        Dim i As Integer
        While i &gt; 10
        End While
        [|If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test3()
        Dim i As Integer
        While i &gt; 10
        End While

        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test4()
        Dim i As Integer = 10
        While i &gt; 10
        End While
        [|If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test4()
        Dim i As Integer = 10
        While i &gt; 10
        End While

        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test4_1()
        Dim i As Integer
        [|If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test4_1()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer

        If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test4_2()
        Dim i As Integer = 10
        [|If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test4_2()
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test4_3()
        Dim i As Integer
        Console.WriteLine(i)
        [|If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test4_3()
        Dim i As Integer
        Console.WriteLine(i)
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer

        If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test4_4()
        Dim i As Integer = 10
        Console.WriteLine(i)
        [|If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test4_4()
        Dim i As Integer = 10
        Console.WriteLine(i)
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        If Integer.Parse(1) &gt; 0
            i = 10
            Console.WriteLine(i)
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesNoNoNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test5()
        [|Dim i As Integer|]
    End Sub
End Class</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesNoNoNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test6()
        [|Dim i As Integer|]
        i = 1
    End Sub
End Class</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesNoYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test7()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test7()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesNoYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test8()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
        i = 2
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test8()
        Dim i As Integer
        NewMethod()
        i = 2
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesYesNoNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test9()
        [|Dim i As Integer
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test9()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesYesNoNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test10()
        [|Dim i As Integer
        Console.WriteLine(i)|]
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test10()
        Dim i As Integer
        NewMethod()
        i = 10
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test11()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test11()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoNoYesYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test12()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)|]
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test12()
        Dim i As Integer
        NewMethod()
        i = 10
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoNoYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test13()
        Dim i As Integer
        [|i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test13()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoNoYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test14()
        Dim i As Integer
        [|i = 10|]
        i = 1
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test14()
        Dim i As Integer
        NewMethod()
        i = 1
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test15()
        Dim i As Integer
        Console.WriteLine(i)
        [|i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test15()
        Dim i As Integer
        Console.WriteLine(i)
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test16()
        Dim i As Integer
        [|i = 10|]
        i = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test16()
        Dim i As Integer
        NewMethod()
        i = 10
        Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test16_1()
        Dim i As Integer
        [|i = 10
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test16_1()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test16_2()
        Dim i As Integer = 10
        [|i = 10
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test16_2()
        Dim i As Integer = 10
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test16_3()
        Dim i As Integer
        Console.WriteLine(i)
        [|i = 10
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test16_3()
        Dim i As Integer
        Console.WriteLine(i)
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test16_4()
        Dim i As Integer = 10
        Console.WriteLine(i)
        [|i = 10
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test16_4()
        Dim i As Integer = 10
        Console.WriteLine(i)
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesYesNoYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test17()
        [|Dim i As Integer = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test17()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesYesNoYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test18()
        [|Dim i As Integer = 10|]
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test18()
        Dim i As Integer
        NewMethod()
        i = 10
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesYesYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test19()
        [|Dim i As Integer = 10
        Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test19()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoNoYesYesYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test20()
        [|Dim i As Integer = 10
        Console.WriteLine(i)|]
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test20()
        Dim i As Integer
        NewMethod()
        i = 10
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 10
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoNoNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test21()
        Dim i As Integer
        [|If Integer.Parse(1) &gt; 10
            i = 1
        End If|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test21()
        Dim i As Integer
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        If Integer.Parse(1) &gt; 10
            i = 1
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoNoNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test22()
        Dim i As Integer = 10
        [|If Integer.Parse(1) &gt; 10
            i = 1
        End If|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test22()
        Dim i As Integer = 10
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        If Integer.Parse(1) &gt; 10
            i = 1
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test22_1()
        Dim i As Integer
        [|If Integer.Parse(1) &gt; 10
            i = 1
            Console.WriteLine(i)
        End If|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test22_1()
        Dim i As Integer
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        If Integer.Parse(1) &gt; 10
            i = 1
            Console.WriteLine(i)
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test22_2()
        Dim i As Integer = 10
        [|If Integer.Parse(1) &gt; 10
            i = 1
            Console.WriteLine(i)
        End If|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test22_2()
        Dim i As Integer = 10
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        If Integer.Parse(1) &gt; 10
            i = 1
            Console.WriteLine(i)
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesNoNoYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test23()
        [|Dim i As Integer|]
        Console.WriteLine(i)
    End Sub

End Class</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesNoNoYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test24()
        [|Dim i As Integer|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test25()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 9
            i = 10
        End If|]
        Console.WriteLine(i)
    End Sub

End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test25()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        If Integer.Parse(1) &gt; 9
            i = 10
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test26()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 9
            i = 10
        End If|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test26()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        If Integer.Parse(1) &gt; 9
            i = 10
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesYesNoYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test27()
        [|Dim i As Integer
        Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test27()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesYesNoYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test28()
        [|Dim i As Integer
        Console.WriteLine(i)|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test28()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test29()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test29()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesNoYesYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test30()
        [|Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test30()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesNoNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test31()
        Dim i As Integer
        [|i = 10|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test31()
        Dim i As Integer
        i = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesNoNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test32()
        Dim i As Integer
        [|i = 10|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test32()
        Dim i As Integer
        i = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test32_1()
        Dim i As Integer
        [|i = 10
        Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test32_1()
        Dim i As Integer
        i = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer = 10
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test32_2()
        Dim i As Integer = 10
        [|i = 10
        Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test32_2()
        Dim i As Integer = 10
        i = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer = 10
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesYesNoYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test33()
        [|Dim i As Integer = 10|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test33()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesYesNoYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test34()
        [|Dim i As Integer = 10|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test34()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesYesYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test35()
        [|Dim i As Integer = 10
        Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test35()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer = 10
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_NoYesYesYesYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test36()
        [|Dim i As Integer = 10
        Console.WriteLine(i)|]
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test36()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
        i = 10
    End Sub

    Private Shared Function NewMethod() As Integer
        Dim i As Integer = 10
        Console.WriteLine(i)
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesNoNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test37()
        Dim i As Integer
        [|Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test37()
        Dim i As Integer
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesNoNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test38()
        Dim i As Integer = 10
        [|Console.WriteLine(i)|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test38()
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesNoYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test39()
        Dim i As Integer
        [|Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test39()
        Dim i As Integer
        NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesNoYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test40()
        Dim i As Integer = 10
        [|Console.WriteLine(i)|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test40()
        Dim i As Integer = 10
        NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test41()
        Dim i As Integer
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test41()
        Dim i As Integer
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test42()
        Dim i As Integer = 10
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test42()
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test43()
        Dim i As Integer
        Console.WriteLine(i)
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test43()
        Dim i As Integer
        Console.WriteLine(i)
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoNoNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test44()
        Dim i As Integer = 10
        Console.WriteLine(i)
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test44()
        Dim i As Integer = 10
        Console.WriteLine(i)
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoYesNoYesYesNoNo()
                Dim code = <text>Imports System

Class Program
    Sub Test45()
        Dim i As Integer
        [|Console.WriteLine(i)
        i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test45()
        Dim i As Integer
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoYesNoYesYesNoYes()
                Dim code = <text>Imports System

Class Program
    Sub Test46()
        Dim i As Integer = 10
        [|Console.WriteLine(i)
        i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test46()
        Dim i As Integer = 10
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoYesNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test47()
        Dim i As Integer
        Console.WriteLine(i)
        [|Console.WriteLine(i)
        i = 10|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test47()
        Dim i As Integer
        Console.WriteLine(i)
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesNoYesNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test48()
        Dim i As Integer = 10
        Console.WriteLine(i)
        [|Console.WriteLine(i)
        i = 10|]
    End Sub

End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test48()
        Dim i As Integer = 10
        Console.WriteLine(i)
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Console.WriteLine(i)
        i = 10
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesYesNoNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test49()
        Dim i As Integer
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
        Console.WriteLine(i)
    End Sub

End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test49()
        Dim i As Integer
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesYesNoNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test50()
        Dim i As Integer = 10
        [|Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test50()
        Dim i As Integer = 10
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        Console.WriteLine(i)
        If Integer.Parse(1) &gt; 0
            i = 10
        End If

        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesYesYesNoYesYesYesNo()
                Dim code = <text>Imports System

Class Program
    Sub Test51()
        Dim i As Integer
        [|Console.WriteLine(i)
        i = 10|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test51()
        Dim i As Integer
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        Console.WriteLine(i)
        i = 10
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MatrixCase_YesYesYesNoYesYesYesYes()
                Dim code = <text>Imports System

Class Program
    Sub Test52()
        Dim i As Integer = 10
        [|Console.WriteLine(i)
        i = 10|]
        Console.WriteLine(i)
    End Sub
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Sub Test52()
        Dim i As Integer = 10
        i = NewMethod(i)
        Console.WriteLine(i)
    End Sub

    Private Shared Function NewMethod(i As Integer) As Integer
        Console.WriteLine(i)
        i = 10
        Return i
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540046)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitFunctionLocal1()
                Dim code = <text>Imports System

Class Program
    Function Test() as Integer
        [|Return 1|]
    End Function
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Function Test() as Integer
        Return NewMethod()
    End Function

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitFunctionLocal2()
                Dim code = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        [|If Test > 10 Then
            Test = 2
        End If|]

        Console.Write(Test)
        Return 1
    End Function
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        Test = NewMethod(Test)

        Console.Write(Test)
        Return 1
    End Function

    Private Shared Function NewMethod(Test As Integer) As Integer
        If Test > 10 Then
            Test = 2
        End If

        Return Test
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitFunctionLocal3()
                Dim code = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        [|If Test > 10 Then
            Test = 2
        End If|]
        Return 1
    End Function
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        NewMethod(Test)
        Return 1
    End Function

    Private Shared Sub NewMethod(Test As Integer)
        If Test > 10 Then
            Test = 2
        End If
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitFunctionLocal4()
                Dim code = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        [|If Test > 10 Then
            Test = 2
        End If|]
        Console.WriteLine(Test)
    End Function
End Class</text>
                Dim expected = <text>Imports System

Class Program
    Function Test() as Integer
        Test = 1
        Test = NewMethod(Test)
        Console.WriteLine(Test)
    End Function

    Private Shared Function NewMethod(Test As Integer) As Integer
        If Test > 10 Then
            Test = 2
        End If

        Return Test
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539295)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ImplicitFunctionLocal5()
                Dim code = <text>Module Module1
    Sub Main()
        Console.WriteLine(Foo(2))
    End Sub

    Function Foo%(ByVal j As Integer)
        [|Foo = 3.87 * j|]
        Exit Function
    End Function
End Module</text>
                Dim expected = <text>Module Module1
    Sub Main()
        Console.WriteLine(Foo(2))
    End Sub

    Function Foo%(ByVal j As Integer)
        Foo = NewMethod(j)
        Exit Function
    End Function

    Private Function NewMethod(j As Integer) As Integer
        Return 3.87 * j
    End Function
End Module</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(527776)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug5079()
                Dim code = <text>Class C
    Function f() As Integer
        [|Dim x As Integer = 5|]
        Return x
    End Function
End Class</text>
                Dim expected = <text>Class C
    Function f() As Integer
        Dim x As Integer = NewMethod()
        Return x
    End Function

    Private Shared Function NewMethod() As Integer
        Return 5
    End Function
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539225), Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug5098()
                Dim code = <code>Class Program
    Shared Sub Main()
        [|Return|]
        Console.Write(4)
    End Sub
End Class</code>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(5092, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix5092()
                Dim code = <text>Imports System
Module Module1
    Sub Main()
        Dim d As Integer?
        d = [|New Integer?()|]
    End Sub
End Module
</text>

                Dim expected = <text>Imports System
Module Module1
    Sub Main()
        Dim d As Integer?
        d = NewMethod()
    End Sub

    Private Function NewMethod() As Integer?
        Return New Integer?()
    End Function
End Module
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539224)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix5096()
                Dim code = <text>Module Program
    Sub Main(args As String())
        [|Console.Write(4)|]    'comments
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main(args As String())
        NewMethod()    'comments
    End Sub

    Private Sub NewMethod()
        Console.Write(4)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539251)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix5135()
                Dim code = <text>Module Module1
    Sub Main()
    End Sub
    Public Function Foo(ByVal params&amp;)
        Foo = [|params&amp;|]
    End Function
End Module</text>

                Dim expected = <text>Module Module1
    Sub Main()
    End Sub
    Public Function Foo(ByVal params&amp;)
        Foo = GetParams(params)
    End Function

    Private Function GetParams(params As Long) As Long
        Return params&amp;
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            ''' <summary>
            ''' Console.Write is not bound, as there is no Imports System
            ''' The flow analysis API in this case should still provide information about variable x (error tolerance)
            ''' </summary>
            ''' <remarks></remarks>
            <WorkItem(5220, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestErrorTolerance()
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        [|Console.Write(x)|]
        Return x
    End Function

    Private Shared Sub NewMethod(x As Integer)
    End Sub
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        NewMethod1(x)
        Return x
    End Function

    Private Shared Sub NewMethod1(x As Integer)
        Console.Write(x)
    End Sub

    Private Shared Sub NewMethod(x As Integer)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539298)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug5195()
                Dim code = <text>Imports System

Class A
    Function Test1() As Integer
        [|Dim i as Integer = 10
        Dim j as Integer = 0
        Console.Write("hello vb!")|]
        j = i + 42
        Console.Write(j) 
    End Function
End Class</text>

                Dim expected = <text>Imports System

Class A
    Function Test1() As Integer
        Dim j As Integer
        Dim i As Integer = NewMethod()
        j = i + 42
        Console.Write(j)
    End Function

    Private Shared Function NewMethod() As Integer
        Dim i as Integer = 10
        Dim j as Integer = 0
        Console.Write("hello vb!")
        Return i
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540003)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6138()
                Dim code = <text>Class Test
    Private _foo As Integer
    Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(ByVal value As Integer)
            [|_foo = value|]
        End Set
    End Property
End Class </text>

                Dim expected = <text>Class Test
    Private _foo As Integer
    Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(ByVal value As Integer)
            NewMethod(value)
        End Set
    End Property

    Private Sub NewMethod(value As Integer)
        _foo = value
    End Sub
End Class </text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540068)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6215()
                Dim code = <text>Module Program
    Sub Main()
        [|Dim i As Integer = 1|]
        i = 2
        Console.WriteLine(i)
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        Dim i As Integer = NewMethod()
        i = 2
        Console.WriteLine(i)
    End Sub

    Private Function NewMethod() As Integer
        Return 1
    End Function
End Module</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WorkItem(540072)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6220()
                Dim code = <text>Module Program
    Sub Main()
[|        Dim i As Integer = 1
|]        Console.WriteLine(i)
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Function NewMethod() As Integer
        Return 1
    End Function
End Module</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WorkItem(540072)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6220_1()
                Dim code = <text>Module Program
    Sub Main()
[|        Dim i As Integer = 1 ' test
|]        Console.WriteLine(i)
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        Dim i As Integer = NewMethod()
        Console.WriteLine(i)
    End Sub

    Private Function NewMethod() As Integer
        Return 1 ' test
    End Function
End Module</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WorkItem(540080)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6230()
                Dim code = <text>Module Program
    Sub Main()
        Dim y As Integer =[| 1 + 1|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        Dim y As Integer = GetY()
    End Sub

    Private Function GetY() As Integer
        Return 1 + 1
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540080)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6230_1()
                Dim code = <text>Module Program
    Sub Main()
        Dim i As Integer [|= 1 + 1|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim i As Integer = 1 + 1
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540063)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6208()
                Dim code = <text>Module Program
    Sub Main()
        [|'selection
        Console.Write(4)
        'end selection|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        'selection
        Console.Write(4)
        'end selection
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540063)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6208_1()
                Dim code = <text>Module Program
    Sub Main()
        [|'selection
        Console.Write(4)
        'end selection
|]    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        'selection
        Console.Write(4)
        'end selection
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539915)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6022()
                Dim code = <text>Imports System

Module Module1
    Delegate Function Del(ByVal v As String) As String
    Sub Main(args As String())
        Dim r As Del = [|AddressOf Foo|]

        Console.WriteLine(r.Invoke("test"))
    End Sub

    Function Foo(ByVal value As String) As String
        Return value
    End Function

End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Delegate Function Del(ByVal v As String) As String
    Sub Main(args As String())
        Dim r As Del = GetR()

        Console.WriteLine(r.Invoke("test"))
    End Sub

    Private Function GetR() As Del
        Return AddressOf Foo
    End Function

    Function Foo(ByVal value As String) As String
        Return value
    End Function

End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539915)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6022_1()
                Dim code = <text>Imports System

Module Module1
    Delegate Function Del(ByVal v As String) As String
    Sub Main(args As String())
        Dim r As Del = AddressOf [|Foo|]

        Console.WriteLine(r.Invoke("test"))
    End Sub

    Function Foo(ByVal value As String) As String
        Return value
    End Function

End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Delegate Function Del(ByVal v As String) As String
    Sub Main(args As String())
        Dim r As Del = GetR()

        Console.WriteLine(r.Invoke("test"))
    End Sub

    Private Function GetR() As Del
        Return AddressOf Foo
    End Function

    Function Foo(ByVal value As String) As String
        Return value
    End Function

End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(8285, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6310()
                Dim code = <text>Imports System

Module Module1
    Sub Main(args As String())
        [|If True Then Return|]
        Console.WriteLine(1)
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Sub Main(args As String())
        NewMethod()
        Return
        Console.WriteLine(1)
    End Sub

    Private Sub NewMethod()
        If True Then Return
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(8285, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6310_1()
                Dim code = <text>Imports System

Module Module1
    Sub Main(args As String())
        If True Then [|If True Then Return|]
        Console.WriteLine(1)
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Sub Main(args As String())
        If True Then NewMethod() : Return
        Console.WriteLine(1)
    End Sub

    Private Sub NewMethod()
        If True Then Return
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540151)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6310_2()
                Dim code = <text>Imports System

Module Module1
    Sub Main(args As String())
        [|If True Then Return|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Sub Main(args As String())
        NewMethod()
    End Sub

    Private Sub NewMethod()
        If True Then Return
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(8285, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6310_3()
                Dim code = <text>Imports System

Module Module1
    Sub Main(args As String())
        If True Then [|If True Then Return|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Module1
    Sub Main(args As String())
        If True Then NewMethod() : Return
    End Sub

    Private Sub NewMethod()
        If True Then Return
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540338)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub Bug6566()
                Dim code = <text>Imports System
Module Module1
    Sub Main(args As String())
        Dim s as String = [|Nothing|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Module1
    Sub Main(args As String())
        Dim s as String = GetS()
    End Sub

    Private Function GetS() As String
        Return Nothing
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540361)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6598()
                Dim code = <text>Imports System

Class C
    Sub Test()
        [|Program|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class C
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Program
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541671)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub UnreachableCodeWithReturnStatement()
                Dim code = <text>Class Test
    Sub Test()
        Return

        [|Dim i As Integer = 1
        Dim j As Integer = i|]

        Return
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Sub Test()
        Return

        NewMethod()

        Return
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 1
        Dim j As Integer = i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541671)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub UnreachableCodeWithExitSub()
                Dim code = <text>Class Test
    Sub Test()
        Return

        [|Dim i As Integer = 1
        Dim j As Integer = i|]

        Exit Sub
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Sub Test()
        Return

        NewMethod()

        Exit Sub
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 1
        Dim j As Integer = i
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541671)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub EmbededStatementWithoutStatementEndToken()
                Dim code = <text>Module Program
    Sub Main(args As String())
        If True Then Dim i As Integer = 10 : [|i|] = i + 10 Else Dim j As Integer = 45 : j = j + 10
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main(args As String())
        If True Then Dim i As Integer = 10 : NewMethod(i) Else Dim j As Integer = 45 : j = j + 10
    End Sub

    Private Sub NewMethod(i As Integer)
        i = i + 10
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(8075, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub FieldInitializer()
                Dim code = <text>Module Program
    Dim x As Object = [|Nothing|]

    Sub Main(args As String())
    End Sub
End Module</text>
                Dim expected = <text>Module Program
    Dim x As Object = GetX()

    Private Function GetX() As Object
        Return Nothing
    End Function

    Sub Main(args As String())
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541409), WorkItem(542687)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DontCrashOnInvalidAddressOf()
                Dim code = <text>Module Program
    Sub Main(args As String())
    End Sub
    Sub UseThread()
        Dim t As New System.Threading.Thread(AddressOf [|foo|])
    End Sub
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(541515)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DontCrashWhenCanFindOutContainingScopeType()
                Dim code = <text>Class Program
    Sub Main()
        Dim x As New List(Of Program) From {[|New Program|]}
    End Sub

    Public Property Name As String
End Class</text>
                Dim expected = <text>Class Program
    Sub Main()
        Dim x As New List(Of Program) From {NewMethod()}
    End Sub

    Private Shared Function NewMethod() As Program
        Return New Program
    End Function

    Public Property Name As String
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542512)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub QueryVariable1()
                Dim code = <text>Option Infer On
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim x = From [|y|] In ""
    End Sub
End Module</text>
                Dim expected = <text>Option Infer On
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim x = GetX()
    End Sub

    Private Function GetX() As Collections.Generic.IEnumerable(Of Char)
        Return From y In ""
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542615)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub FixedNullExceptionCrash()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer = 5
        If x &gt; 0 Then
        Else
            [|Console.Write|]
        End If
    End Sub
End Module</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer = 5
        If x &gt; 0 Then
        Else
            NewMethod()
        End If
    End Sub

    Private Sub NewMethod()
        Console.Write
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542629)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LambdaSymbol()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, Function(x As Integer)
                     [|Return x * 2|]
                 End Function)
    End Sub
End Module</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, Function(x As Integer)
                     Return NewMethod(x)
                 End Function)
    End Sub

    Private Function NewMethod(x As Integer) As Integer
        Return x * 2
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542511)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodOnVariableUsedInWhen()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        [|Dim s As color|]
        Try
        Catch ex As Exception When s = color.blue
            Console.Write("Exception")
        End Try
    End Sub
End Module
Enum color
    blue
End Enum</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(542512)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodOnVariableDeclaredInFrom()
                Dim code = <text>Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim x = From [|y|] In ""
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim x = GetX()
    End Sub

    Private Function GetX() As Collections.Generic.IEnumerable(Of Char)
        Return From y In ""
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542825)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DanglingField()
                Dim code = <text>Dim query1 = From i As Integer In New Integer() {4, 5} Where [|i > 5|] Select i</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MultipleNamesLocalDecl()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        [|Dim i, i2, i3 As Object = New Object()
        Dim i4|]

lab1:
        Console.Write(i)
        GoTo lab1

        Console.Write(i2)
        Console.Write(i3)
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i, i2 As Object
        Dim i3 As Object = NewMethod()

lab1:
        Console.Write(i)
        GoTo lab1

        Console.Write(i2)
        Console.Write(i3)
    End Sub

    Private Function NewMethod() As Object
        Dim i3 As Object = New Object()
        Dim i4
        Return i3
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub UnreachableUninitialized()
                Dim code = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Dim x1 As Object
        SyncLock x1
            Return
        End SyncLock|]
        System.Threading.Monitor.Exit(x1)
    End Sub
End Class</text>

                Dim expected = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x1 As Object = Nothing
        NewMethod(x1)
        Return
        System.Threading.Monitor.Exit(x1)
    End Sub

    Private Shared Sub NewMethod(ByRef x1 As Object)
        SyncLock x1
            Return
        End SyncLock
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543053)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AddBlankLineBetweenMethodAndType()
                Dim code = <text>Class Program
    Private Shared Sub Main(args As String())
        Dim i As Integer = 2
        Dim c As New C(Of Integer)([|i|])
    End Sub
 
    Private Class C(Of T)
        Private v As Integer
        Public Sub New(ByRef v As Integer)
            Me.v = v
        End Sub
    End Class
End Class</text>

                Dim expected = <text>Class Program
    Private Shared Sub Main(args As String())
        Dim i As Integer = 2
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim c As New C(Of Integer)(i)
    End Sub

    Private Class C(Of T)
        Private v As Integer
        Public Sub New(ByRef v As Integer)
            Me.v = v
        End Sub
    End Class
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543047)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ResourceDeclaredOutside()
                Dim code = <text>Option Infer On
Option Strict Off
Class C1
    Shared Sub main()
        [|Dim mnObj As MyManagedClass
        Using mnObj
        End Using|]
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Structure</text>
                Dim expected = <text>Option Infer On
Option Strict Off
Class C1
    Shared Sub main()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim mnObj As MyManagedClass
        Using mnObj
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Structure</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(528962)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub FunctionCallWithFunctionName()
                Dim code = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        factorial(4)
    End Sub
    Shared Function factorial(ByVal x As Integer) As Integer
       [| factorial = x * factorial(x - 1) * x|]
    End Function
End Class
</text>
                Dim expected = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        factorial(4)
    End Sub
    Shared Function factorial(ByVal x As Integer) As Integer
        factorial = NewMethod(x)
    End Function

    Private Shared Function NewMethod(x As Integer) As Integer
        Return x * factorial(x - 1) * x
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForBadCode()
                Dim code = <text>Module M1
    WriteOnly Property Age() As Integer
        Set(ByVal Value As Integer)
            Dim a, b, c As [|Object =|] New Object()
lab1:
            SyncLock a
                GoTo lab1
            End SyncLock
            Console.WriteLine(b)
            Console.WriteLine(c)
        End Set
    End Property
End Module
</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(10878, "DevDiv_Projects/Roslyn")>
            <WorkItem(544408)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForBranchOutFromSyncLock()
                Dim code = <text>Imports System
Class Program
    Shared Sub Main(args As String())
        SyncLock Sub()
                     [|Exit While|]
                 End Sub
        End SyncLock
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543304)>
            <WorkItem(544408)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForLambdaInSyncLock()
                Dim code = <text>Imports System
Class Program
    Public Shared Sub Main(args As String())
        SyncLock Function(ByRef int As [|Integer|])
                     SyncLock x
                     End SyncLock
                 End Function
        End SyncLock
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543320)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForBadRegion()
                Dim code = <text>Imports System
Class Test
    Public Shared Sub Main()
        Dim x(1, 2) As Integer = New Integer[|(,)|] {{1}, {2}}
    End Sub
End Class
</text>
                Dim expected = <text>Imports System
Class Test
    Public Shared Sub Main()
        Dim x(1, 2) As Integer = GetX()
    End Sub

    Private Shared Function GetX() As Integer(,)
        Return New Integer(,) {{1}, {2}}
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543320)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForBadRegion_1()
                Dim code = <text>Imports System
Class Test
    Public Shared Sub Main()
        Dim y(1, 2) = [|New Integer|]
    End Sub
End Class
</text>
                Dim expected = <text>Imports System
Class Test
    Public Shared Sub Main()
        Dim y(1, 2) = GetY()
    End Sub

    Private Shared Function GetY() As Integer
        Return New Integer
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543362)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForBadRegion_2()
                Dim code = <text>Imports System
Class Test
                Public Shared Sub Main()
                    Dim y(,) = New Integer(,) {{[|From|]}}
                End Sub
            End Class
</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForSynclockBlockContainsReturn()
                Dim code = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Dim x1 As Object
        SyncLock x1
            Return
        End SyncLock|]
        System.Threading.Monitor.Exit(x1)
    End Sub
End Class
</text>
                Dim expected = <text>Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x1 As Object = Nothing
        NewMethod(x1)
        Return
        System.Threading.Monitor.Exit(x1)
    End Sub

    Private Shared Sub NewMethod(ByRef x1 As Object)
        SyncLock x1
            Return
        End SyncLock
    End Sub
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForUsingBlockContainsReturn()
                Dim code = <text>Imports System
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Dim x1 As C1
        Using x1
            Return
        End Using|]
        System.Threading.Monitor.Exit(x1)
    End Sub
End Class
Class C1
    Implements IDisposable
    Public Sub Dispose Implements IDisposable.Dispose
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x1 As C1 = Nothing
        NewMethod(x1)
        Return
        System.Threading.Monitor.Exit(x1)
    End Sub

    Private Shared Sub NewMethod(ByRef x1 As C1)
        Using x1
            Return
        End Using
    End Sub
End Class
Class C1
    Implements IDisposable
    Public Sub Dispose Implements IDisposable.Dispose
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodExitInLambda()
                Dim code = <text>Imports System
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        [|SyncLock Sub()
                     myLock = New Object()
                     Exit Sub
                 End Sub
        End SyncLock|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        NewMethod(myLock)
    End Sub

    Private Shared Sub NewMethod(myLock As Object)
        SyncLock Sub()
                     myLock = New Object()
                     Exit Sub
                 End Sub
        End SyncLock
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543332)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodExitInLambda_2()
                Dim code = <text>Imports System
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        [|SyncLock Function()
                     myLock = New Object()
                     Exit Function
                     Return Nothing
                 End Function
        End SyncLock|]
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        NewMethod(myLock)
    End Sub

    Private Shared Sub NewMethod(myLock As Object)
        SyncLock Function()
                     myLock = New Object()
                     Exit Function
                     Return Nothing
                 End Function
        End SyncLock
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543334)>
            <WorkItem(11186, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodExitInLambda_3()
                Dim code = <text>Imports System
Public Class Program
    Shared Sub foo()
        Dim syncroot As Object = New Object
        SyncLock syncroot
            SyncLock Sub x
                         [|Exit Sub|]
                     End Sub
            End SyncLock
    End SyncLock
    End Sub
End Class
</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543096)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectCaseExpr()
                Dim code = <text>
Module Program
    Sub Main(args As String())
        Select Case [|5|]
            Case 5
                Console.Write(5)
        End Select
    End Sub
End Module</text>

                Dim expected = <text>
Module Program
    Sub Main(args As String())
        Select Case NewMethod()
            Case 5
                Console.Write(5)
        End Select
    End Sub

    Private Function NewMethod() As Integer
        Return 5
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542800)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub OutmostXmlElement()
                Dim code = <text>Imports System
Imports System.Xml.Linq

Module Program
    Sub Main(args As String())
         Dim x = [|&lt;x&gt;&lt;/x&gt;|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Xml.Linq

Module Program
    Sub Main(args As String())
        Dim x = GetX()
    End Sub

    Private Function GetX() As XElement
        Return &lt;x&gt;&lt;/x&gt;
    End Function
End Module</text>

                TestExtractMethod(code, expected, metadataReference:=GetType(System.Xml.Linq.XElement).Assembly.Location)
            End Sub

            <WorkItem(13658, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForElementInitializers()
                Dim code = <text>
Module Module1
    Property Prop As New List(Of String) From {[|"One"|], "two"}
End Module</text>

                Dim expected = <text>
Module Module1
    Property Prop As New List(Of String) From {NewMethod(), "two"}

    Private Function NewMethod() As String
        Return "One"
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(529967)>
            <WpfFact(), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExtractObjectArray()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim o3 As Object = "hi"
        Dim col1 = [|{o3, o3}|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim o3 As Object = "hi"
        Dim col1 = GetCol1(o3)
    End Sub

    Private Function GetCol1(o3 As Object) As Object()
        Return {o3, o3}
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(669341)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStructure1()
                Dim code = <text>Structure XType
    Public Y As YType
End Structure
 
Structure YType
    Public Z As ZType
End Structure
 
Structure ZType
    Public Value As Integer
End Structure
 
Module Program
    Sub Main(args As String())
        Dim x As XType
 
        Dim value = [|x.Y|].Z.Value
        With x
            .Y.Z.Value += 1
        End With
    End Sub
End Module</text>
                Dim expected = <text>Structure XType
    Public Y As YType
End Structure
 
Structure YType
    Public Z As ZType
End Structure
 
Structure ZType
    Public Value As Integer
End Structure
 
Module Program
    Sub Main(args As String())
        Dim x As XType

        Dim value = GetY(x).Z.Value
        With x
            .Y.Z.Value += 1
        End With
    End Sub

    Private Function GetY(ByRef x As XType) As YType
        Return x.Y
    End Function
End Module</text>
                TestExtractMethod(code, expected, dontPutOutOrRefOnStruct:=False)
            End Sub

            <WorkItem(529266)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStructure2()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Structure SSSS3
    Public A As String
    Public B As Integer
End Structure
 
Structure SSSS2
    Public S3 As SSSS3
End Structure
 
Structure SSSS
    Public S2 As SSSS2
End Structure
 
Structure SSS
    Public S As SSSS
End Structure
 
Class Clazz
    Sub TEST()
        Dim x As New SSS()
        [|x.S|].S2.S3.A = "1"
    End Sub
End Class</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Structure SSSS3
    Public A As String
    Public B As Integer
End Structure
 
Structure SSSS2
    Public S3 As SSSS3
End Structure
 
Structure SSSS
    Public S2 As SSSS2
End Structure
 
Structure SSS
    Public S As SSSS
End Structure
 
Class Clazz
    Sub TEST()
        Dim x As New SSS()
        GetS(x).S2.S3.A = "1"
    End Sub

    Private Shared Function GetS(ByRef x As SSS) As SSSS
        Return x.S
    End Function
End Class</text>
                TestExtractMethod(code, expected, dontPutOutOrRefOnStruct:=False)
            End Sub
        End Class
    End Class
End Namespace
