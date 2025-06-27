' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class FlowAnalysisTests

        <Fact>
        Public Sub Query1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As QueryAble
        Dim q1 As Object = From s In q Where 2 > s

        Dim x1, x2, x3, x4 As Object
        Dim qq As New QueryAble()
        Dim q2 As Object = From s In qq Where x1 > s Where x2 > s Where x3 > s Select CInt(x4) + s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'q' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q1 As Object = From s In q Where 2 > s
                                     ~
BC42104: Variable 'x1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q2 As Object = From s In qq Where x1 > s Where x2 > s Where x3 > s Select CInt(x4) + s
                                              ~~
BC42104: Variable 'x2' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q2 As Object = From s In qq Where x1 > s Where x2 > s Where x3 > s Select CInt(x4) + s
                                                           ~~
BC42104: Variable 'x3' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q2 As Object = From s In qq Where x1 > s Where x2 > s Where x3 > s Select CInt(x4) + s
                                                                        ~~
BC42104: Variable 'x4' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q2 As Object = From s In qq Where x1 > s Where x2 > s Where x3 > s Select CInt(x4) + s
                                                                                           ~~
</expected>)

        End Sub

        <Fact>
        Public Sub Query2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = From s1 In q
                           Where [|s1 > 0|]
                           Where 10 > s1 + y
                           Where DirectCast(Function()
                                                System.Console.WriteLine(s1)
                                                Return True
                                            End Function, Func(Of Boolean)).Invoke()

    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub Query3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = From s1 In [|q|]
                           Where s1 > 0
                           Where 10 > s1 + y
                           Where DirectCast(Function()
                                                System.Console.WriteLine(s1)
                                                Return True
                                            End Function, Func(Of Boolean)).Invoke()

    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub Query7()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = [|From s1 In q
                           Where s1 > 0
                           Where 10 > s1 + y
                           Where DirectCast(Function()
                                                System.Console.WriteLine(s1)
                                                Return True
                                            End Function, Func(Of Boolean)).Invoke()|]

    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q, y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Query4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = From s1 In q
                           Where s1 > 0
                           Where 10 > s1 + y
                           Where [|DirectCast(Function()
                                                Dim z As Integer = 0
                                                y=s1
                                                System.Console.WriteLine(s1+z)
                                                Return True
                                            End Function, Func(Of Boolean))|].Invoke()

        System.Console.WriteLine(y)
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1, z", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Query5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = From s1 In q
                           Where s1 > 0
                           Where 10 > [|s1 + y|]
                           Where DirectCast(Function()
                                                Dim z As Integer = 0
                                                y=s1
                                                System.Console.WriteLine(s1+z)
                                                Return True
                                            End Function, Func(Of Boolean)).Invoke()

        System.Console.WriteLine(y)
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, y, s1, z", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1, s1, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select [|s1|]
                           Where 10 > s1
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))

            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Dim flowsIn = dataFlowAnalysisResults.DataFlowsIn(0)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))

            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Same(flowsIn, dataFlowAnalysisResults.ReadInside(0))

            Assert.Equal("q, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))

            Assert.Equal("Me, args, q, q1, s1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Dim ss = dataFlowAnalysisResults.WrittenOutside.Where(Function(s) s.Name.Equals("s1", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(ss(0).Name, ss(1).Name)
            Assert.NotEqual(ss(0), ss(1))

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = [|From s1 In q
                           Select s1
                           Where 10 > s1|]
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal("s1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.NotSame(dataFlowAnalysisResults.VariablesDeclared(0), dataFlowAnalysisResults.VariablesDeclared(1))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q, s1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select s1 = [|s1|]
                           Where 10 > s1
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.NotSame(dataFlowAnalysisResults.ReadInside(0), GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside)(1))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select s2 = [|s1|]
                           Where 10 > s2
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select [|s1 + 1|]
                           Where 10 > s1
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In [|q|]
                           Select s1 + 1
                           Where 10 > s1
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select7()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In [|q|]
                           Select 1
                           Where 10 > s1
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select8()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select s2 = [|s1|]
                           Where 10 > s2
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select9()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Select s2 = [|s1|]
                           Where 10 > s2
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, q1, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub ImplicitSelect1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Long In [|q|] Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub ImplicitSelect2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Long In [|q|] Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub ImplicitSelect3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Long In [|q|] Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub ImplicitSelect4()

            Assert.Throws(Of System.ArgumentException)(
                Sub()
                    Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
                <compilation>
                    <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s [|As Long|] In q Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
                </compilation>)
                End Sub)

#If False Then
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
#End If

        End Sub

        <Fact>
        Public Sub ImplicitSelect5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = [|From s As Long In q Where s > 1|]
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q, s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub ImplicitSelect6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As [|Long|] In q Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

#If True Then
            Assert.False(dataFlowAnalysisResults.Succeeded)
#Else
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
#End If
        End Sub

        <Fact()>
        Public Sub ImplicitSelect7()
            Assert.Throws(Of System.ArgumentException)(
                Sub()
                    Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
                <compilation>
                    <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s [|As|] Long In q Where s > 1 
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
                </compilation>)
                End Sub)

#If False Then
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
#End If
        End Sub

        <Fact>
        Public Sub ImplicitSelect8()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = [|From s In q|]
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
            ]]></file>
        </compilation>)

            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By [|x|], 
                                    x, 
                                    x Descending 
                           Order By x Descending 
                           Select y = x
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By x, 
                                    [|x|], 
                                    x Descending 
                           Order By x Descending 
                           Select y = x
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By x, 
                                    x, 
                                    [|x|] Descending 
                           Order By x Descending 
                           Select y = x
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By x, 
                                    x, 
                                    x Descending 
                           Order By [|x|] Descending 
                           Select y = x
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From x In [|q|]
                           Order By x, 
                                    x, 
                                    x Descending 
                           Order By x Descending 
                           Select y = x 
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = [|From x In q
                           Order By x, 
                                    x, 
                                    x Descending 
                           Order By x Descending 
                           Select y = x|] 
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub OrderBy7()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From z In q
                           Select x = [|z|]
                           Order By x, 
                                    x, 
                                    x Descending 
                           Order By x Descending 
                           Select y = x 
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, z, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Query6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Class C
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim y As Integer = 0

        Dim q1 As Object = From s1 In q
                           Where [|s1 > y|]

        System.Console.WriteLine(y)
    End Sub
End Class

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, q, y, q1, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Select10()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System

Class QueryAble1
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble2
        Return Me
    End Function
End Class

Class QueryAble2
    Public Function [Select](Of T, U)(x As Func(Of T, U)) As QueryAble2
        Return Me
    End Function
End Class

Module C
    Sub Main()
        Dim q As New QueryAble1()

        Dim q1 As Object = From s1 In q
                           Where 10 > s2
                           Select s1.MaxValue,
                                  s2 = [|s1|],
                                  s3 = s1 + 1
                           Select s2 + s3
    End Sub
End Module

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, MaxValue, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Let1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return Me
    End Function
End Class

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](Of T, S)(this As QueryAble, x As Func(Of T, S)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return this
    End Function

    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Let s2 = [|s1|],
                               s3 = s1 + 1
                           Let s4 = s2 + s3
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Let2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return Me
    End Function
End Class

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](Of T, S)(this As QueryAble, x As Func(Of T, S)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return this
    End Function

    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Let s2 = s1,
                               s3 = [|s1 + 1|]
                           Let s4 = s2 + s3
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Let3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return Me
    End Function
End Class

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](Of T, S)(this As QueryAble, x As Func(Of T, S)) As QueryAble
        System.Console.WriteLine("[Select] {0}", x)
        Return this
    End Function

    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From s1 In q
                           Let s2 = s1,
                               s3 = s1 + 1
                           Let s4 = [|s2 + s3|]
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s1 In qi
                           From s2 In [|s1|],
                                s3 In s1 
                           From s4 In s2
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s1 In q
                           from s2 In s1,
                                s3 In [|s2|]
                           From s4 In s3
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s1 In q
                           from s2 In s1,
                                s3 In s2
                           From s4 In [|s3|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s1 In q
                           from s2 In s1,
                                s3 In s2
                           Let s4 = [|s3|], s5 = s4
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4, s5", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s1 In q
                           from s2 In s1,
                                s3 In s2
                           Select s4 = [|s3|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub From6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble(Of Integer)(0)

        Dim q1 As Object = From s1 In q Select s1+1
                           From s2 In [|q|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("q, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("q, q1, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("q", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In [|qb|]
                                Join s3 In qs
                                On s2 Equals s3
                           On s1 Equals s2 
                           Join s4 In qu
                           On s4 Equals s1
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qs, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In qb
                                Join s3 In [|qs|]
                                On s2 Equals s3
                           On s1 Equals s2 
                           Join s4 In qu
                           On s4 Equals s1
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qs", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qs", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In qb
                                Join s3 In qs
                                On s2 Equals s3
                           On s1 Equals s2 
                           Join s4 In [|qu|]
                           On s4 Equals s1
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qu", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qu", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In qb
                                Join s3 In qs
                                On s2 Equals s3
                           On s1 Equals s2 
                           Let s4 = [|s3|], s5 = s4
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4, s5", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In qb
                                Join s3 In qs
                                On s2 Equals s3
                           On s1 Equals s2 
                           Select s4 = [|s3|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, s1, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Join6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Join s2 In qb
                                Join s3 In qs
                                On [|s2|] Equals s3
                           On s1 Equals s2 
                           Select s4 = s3
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, s1, s2, s3", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupBy1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = From s1 In [|qi|]
                           Group i1=s1 
                           By k1=s1 
                           Into Group, Count(), c1=Count(i1)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s1, i1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, i1, k1, Group, Count, c1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupBy2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = [|From s1 In qi
                           Group i1=s1 
                           By k1=s1 
                           Into Group, Count(), c1=Count(i1)|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("s1, i1, k1, Group, Count, c1", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, s1, i1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1, i1, k1, Group, Count, c1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupBy3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = From s1 In qi
                           Group i1=s1 
                           By k1=s1 
                           Into Group, Count(), c1=Count([|i1|])
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, i1, k1, Group, Count, c1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupBy4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = From s1 In qi
                           Group By k1=[|s1|] 
                           Into Group, Count(), c1=Count(s1)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, k1, Group, Count, c1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Group Join s2 In [|qb|]
                                Group Join s3 In qs
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals s2 
                           Into c2 = Count(s2)
                           Group Join s4 In qu
                           On s4 Equals s1
                           Into Group
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qs, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Group Join s2 In qb
                                Group Join s3 In [|qs|]
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals s2 
                           Into c2 = Count(s2)
                           Group Join s4 In qu
                           On s4 Equals s1
                           Into Group
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qs", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qs", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Group Join s2 In qb
                                Group Join s3 In qs
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals s2 
                           Into c2 = Count(s2)
                           Group Join s4 In [|qu|]
                           On s4 Equals s1
                           Into Group
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qu", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qu", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Group Join s2 In qb
                                Group Join s3 In qs
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals [|s2|] 
                           Into c2 = Count(s2)
                           Group Join s4 In qu
                           On s4 Equals s1
                           Into Group
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s1 In qi
                           Group Join s2 In qb
                                Group Join s3 In qs
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals s2 
                           Into c2 = Count([|s2|])
                           Group Join s4 In qu
                           On s4 Equals s1
                           Into Group
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, qs, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1, s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub GroupJoin6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = [|From s1 In qi
                           Group Join s2 In qb
                                Group Join s3 In qs
                                On s2 Equals s3
                                Into c1 = Count()
                           On s1 Equals s2 
                           Into c2 = Count(s2)
                           Group Join s4 In qu
                           On s4 Equals s1
                           Into Group|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi, qb, qs, qu", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, qb, qs, qu, s1, s2, s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1, s2, s3, c1, c2, s4, Group", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, qs, qu, ql, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In [|qi|]
                           Let s2 = s1 + 1
                           Into Count
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In [|qi|]
                           Let s2 = s1 + 1
                           Into Count, c = Count(s2)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In qi
                           Let s2 = [|s1 + 1|]
                           Into Count
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In qi
                           Let s2 = [|s1 + 1|]
                           Into Count, c = Count(s2)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count([|s2|])
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count, c = Count([|s2|])
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate7()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = [|Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count(s2)|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate8()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = [|Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count, c = Count(s2)|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate9()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in [|qb|]
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate10()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in [|qb|]
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count, c = Count(s2)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qb", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate11()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In [|qi|]
                           Let s2 = s1 + 1
                           Into Count
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qb, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate12()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In [|qi|]
                           Let s2 = s1 + 1
                           Into Count, c = Count(s2)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qb, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate13()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = [|s1 + 1|]
                           Into Count
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate14()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = [|s1 + 1|]
                           Into Count, c = Count(s2)
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate15()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count([|s2|])
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate16()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count, c = Count([|s2|])
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("qi, qb, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1, t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate17()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = [|From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count(s2)|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi, qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, qb, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t1, s1, s2, Count", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact>
        Public Sub Aggregate18()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q1 As Object = [|From t1 in qb
                           Aggregate s1 In qi
                           Let s2 = s1 + 1
                           Into Count, c = Count(s2)|]
    End Sub
End Module
            ]]></file>
        </compilation>)

            Assert.Equal("t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("qi, qb", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("qi, qb, s1, s2", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t1, s1, s2, Count, c", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("qi, qb, q1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("qi", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(543164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543164")>
        <Fact()>
        Public Sub LambdaFunctionInsideSkipClause()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System
Imports System.Linq

Module Module1
    Sub Main(arr As String())
        Dim q2 = From s1 In arr Skip [|Function() s1|]
    End Sub
End Module
            ]]></file>
        </compilation>,
                errors:=<errors>
BC36594: Definition of method 'Skip' is not accessible in this context.
        Dim q2 = From s1 In arr Skip Function() s1
                                ~~~~
BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        Dim q2 = From s1 In arr Skip Function() s1
                                     ~~~~~~~~~~~~~
                        </errors>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("arr", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("arr, q2, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(543164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543164")>
        <Fact()>
        Public Sub LambdaFunctionInsideSkipClause2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Imports System
Imports System.Linq

Module Module1
    Sub Main(arr As String())
        Dim q2 = From s1 In arr Skip [|s1|]
    End Sub
End Module
            ]]></file>
        </compilation>,
                errors:=<errors>
BC30451: 's1' is not declared. It may be inaccessible due to its protection level.
        Dim q2 = From s1 In arr Skip s1
                                     ~~
                        </errors>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("arr", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("arr, q2, s1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

    End Class
End Namespace
