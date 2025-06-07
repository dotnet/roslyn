' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class FlowAnalysisTests
        Inherits FlowTestBase

#Region "Try-Catch-Finally"

        <Fact()>
        Public Sub TestTryCatchWithGoto01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithGoto01">
                    <file name="try.vb"><![CDATA[
Imports System

Public Class Test
    Sub Test()
        Dim x As SByte, y As SByte = 3
L1: 
        [|
        Try
            x = y * y
            y = x - 1
            If x < 222 Then
                GoTo L1
            End If
        Catch e As Exception When e.Message.Contains("ABC")
            x = x - 100
            GoTo L2
        Finally
            y = -y
        End Try
        |]
L2:
        End Sub
End Class 
    ]]></file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, y, e", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, y, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithGotoMix()
            Dim analysis = CompileAndAnalyzeControlFlow(
                <compilation name="TestTryCatchWithGotoMix">
                    <file name="try.vb">
Imports System

Public Class Test
    Shared Sub Test()
        Try
[|
            GoTo lInTry
lInTry:
            Dim a As Integer
|]
        Catch ex As InvalidOperationException

            GoTo lInTry
            GoTo lInCatch1
lInCatch1:
        Catch ex As Exception

            GoTo lInTry
            GoTo lInCatch2
lInCatch2:
        Finally

            GoTo lInFinally
lInFinally:
        End Try
    End Sub
End Class
  </file>
                </compilation>)

            Assert.Equal(1, analysis.EntryPoints.Count())
            Assert.Equal(0, analysis.ExitPoints.Count())
            Assert.True(analysis.StartPointIsReachable)
            Assert.True(analysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithGoto02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithGoto02">
                    <file name="try.vb">
Imports System

Public Class Test
    Shared Sub Test()
        Dim x As Byte, y as byte = 1
        Try [|
L1:         x = y + 123
            Exit Sub
            |]
        Catch e As Exception
            x = x - 100
            If x = -1 Then
                GoTo L1
            End If
        Finally
            x = x - 1
        End Try

    End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Equal(1, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithGoto03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithGoto03">
                    <file name="try.vb"><![CDATA[
Imports System

Public Class Test
    Shared Function Test(p As String) As String
        Dim x As UShort, s As String = "abc"
L1:
        Try
            x = s.Length
            s = p + p
        Catch e1 As ArgumentException
            [|
            Try
                s = p
                If x < 123 Then
                    GoTo L1
                End If
            Catch e2 As NullReferenceException
                Return "Y"
            End Try
            |]
        End Try
        Return s
    End Function
End Class
]]></file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("e2", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("p, s", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("s, e2", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("p, x, s, e1", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithLoop01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchInWhile01">
                    <file name="a.b">
Imports System

Public Class Test
    Function Test() As Integer
        Dim x As Short = 100

        While x > 0
            [|
            Try
                x = x - 3
                Continue While
            Catch e As Exception
                x = -111
                Exit While               
            End Try
            |]
        End While

        Return x
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithLoop02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchInLoop02">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TrySub()
    Dim x As SByte = 111
    Do Until x = 0
        x = x - 3
        [|
        Try
            If x Mod 7 = 0 Then
                Continue Do
           ElseIf x Mod 11 = 0 Then
                Exit Try
            End If
        Catch ex As ArgumentException
            Exit Do
        Finally
            Try
                While x >= 100
                    x = x - 5
                    Exit Try
                End While
            Catch ex As Exception

            End Try
        End Try
        |]
    Loop
  End Sub

End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("ex, ex", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, ex, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithLoop03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchInLoop03">
                    <file name="a.b">
Imports System

Public Class Test
  Shared Sub TrySub()
    Dim x As SByte = 111
    Do Until x = 0
        x = x - 3
   
        Try
            If x Mod 7 = 0 Then
                Continue Do
           ElseIf x Mod 11 = 0 Then
                Exit Try
            End If
        Catch ex As ArgumentException
           [|
            Exit Do
           |]
        Finally
            ' Start3
            Try
                While x >= 100
                    x = x - 5
                    Exit Try
                End While
            Catch ex As Exception

            End Try
            ' End3
        End Try        
    Loop
  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, ex, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithLoop04()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchInLoop04">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TrySub()
    Dim x As SByte = 111
    Do Until x = 0
        x = x - 3
   
        Try
            If x Mod 7 = 0 Then
                Continue Do
            ElseIf x Mod 11 = 0 Then
                Exit Try
            End If
        Catch ex As ArgumentException       
            Exit Do
        Finally
            [|
            Try
                While x >= 100
                    x = x - 5
                    Exit Try
                End While
            Catch ex As Exception

            End Try
            |]
        End Try        
    Loop
  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("ex", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, x, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithReturnExit01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithReturnExit01">
                    <file name="a.b">
Imports System

Public Class Test
    Function TryFunction() As UInteger
        Dim x As UInteger = 123
        [|     
        Try
            ' Start1
            If x Mod 7 = 0 Then
                Exit Function
            ElseIf x Mod 11 = 0 Then
                Exit Try
            End If
            ' End1
        Catch ex As ArgumentException
            ' Start2
            Try
                x = x - 5
                Return x
            Finally
                ' Start3
               Exit Function 'BC30101
                ' End3
            End Try
            ' End2
        End Try
        |]
        Return x
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(3, controlFlowAnalysis.ExitPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            'Assert.Equal("ex", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            'Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithReturnExit02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithReturnExit02">
                    <file name="a.b">
Imports System

Public Class Test
    Shared Function TryFunction() As UInteger
        Dim x As UInteger = 123
  
        Try
            ' Start1
            If x Mod 7 = 0 Then
                Exit Function
            ElseIf x Mod 11 = 0 Then
                Exit Try
            End If
            ' End1
        Catch ex As ArgumentException
            [|     
            Try
                x = x + ex.Message.Length
                Return x
            Finally
                ' Start3
               Exit Function 'BC30101
                ' End3
            End Try
            |]
        End Try
       
        Return x
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            'Assert.True(controlFlowAnalysis.StartPointIsReachable)
            'Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, ex", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            'Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, ex", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithThrow01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithThrow01">
                    <file name="a.b">
Imports System

Public Class Test
    Shared Sub Test(ByRef x As Short)
        Try
           [| Try
                x = x - 11
                Throw New ArgumentException()
            Catch e As ArgumentException When x = 77
                Console.WriteLine("E1")
            Catch e As ArgumentException When x = 88
                Console.WriteLine(e)
                Throw
            End Try
          |]
        Catch When x = 88
            Console.WriteLine("E3")
        Finally
            x = x + 11
            Console.WriteLine("F")
        End Try
    End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("e, e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            ' first 'e' because the end points of other two branch 'try' and 2nd 'catch' are unreachable
            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, e", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, e, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithAssignment01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithAssignment">
                    <file name="a.b">
Imports System

Public Class Test
    Sub F(x as Long, ByRef s as String)
        Dim local as Long
        [| Try
            Dim y as Integer = 11
            x = y*y - x
        Catch ex As Exception
            Dim y as String = s
            x = -1
        Finally
            local = - x
        End Try
        |]
    End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
            '
            Assert.Equal("y, ex, y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x, local", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, s", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, s, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, local, y, ex, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithAssignment02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithAssignment02">
                    <file name="a.b">
Imports System

Public Class Test
    Function F() As Short
        Dim sb, sb1 As SByte
        [| 
        Try
            sb = 0
        Catch ax As ArgumentException
            throw
        Catch ex As Exception
            sb = -128
        Finally
            if true then
              sb1 = -1
            End If
        End Try
        |]
        Return sb + sb1
    End Function
End Class
  </file>
                </compilation>)

            Dim dataFlowAnalysis = analysisResults.Item2
            '
            Assert.Equal("ax, ex", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("sb, sb1", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            '
            Assert.Equal("sb, sb1", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("sb, sb1, ax, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("sb, sb1", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithAssignment03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithAssignment03">
                    <file name="a.b"><![CDATA[
Imports System

Public Class Test
    Function F() As Short
        Dim sb As Sbyte, ss As SByte = 0
        [| 
        Try
            sb = 0
        Catch ax As ArgumentException
            if ss <> 0 Then
               sb = ss
            End If
        Catch ex As Exception
            sb = -128
        Finally
            Do While False
              ss = -1
            Loop
        End Try
        |]
        F = sb + ss
    End Function
End Class
                    ]]></file>
                </compilation>)

            Dim dataFlowAnalysis = analysisResults.Item2
            '
            Assert.Equal("ax, ex", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("ss", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("sb, ss", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("ss", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("sb, ss, ax, ex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("sb, ss", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("F, Me, ss", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithDataFlows01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithDataFlows01">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TryMethod(p As Long)
    Dim x As Long = 0, y As Long = 1
    Dim z As Long
    [|
    Try
        If (p > 0) Then
            z = x
        End If
    Catch e As Exception
        Throw
        z = y
    Finally
        If False Then
            x = y * y
        End If
    End Try
    |]
    x = z * y
  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
            '
            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            '
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("y, z", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, z, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithDataFlows02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithDataFlows02">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TryMethod(p As Long)
    Dim x As Long = 0, y As Long = 1
    Dim z As Long
    [|
    Try
        x = p
    Catch ax As ArgumentException

    Finally
        z = x
    End Try
    |]
    p = x * y

  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("ax", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            '
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x, z, ax", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithDataFlows03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithDataFlows03">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TryMethod(ByRef p As Long)
    Dim x As Long = 0, y, z As Long
    [|
    Try
        Try
            z = p
        Catch ex As Exception
            z = p + p
        End Try
    Catch e As Exception
        Throw
        y = x
    Finally
        z = z * p
    End Try
    |]
    x = z * y
  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
            '
            Assert.Equal("ex, e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, z", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, x, z", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("y, z, ex, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("p, y, z", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithDataFlows04()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithDataFlows04">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TryMethod(ByRef p As Long)
    Dim x As Long = 0, y, z As Long
    [|
    Try
L1:
        z = x + x
        GoTo L1
    Catch ax As ArgumentException
        p = y + p
    Finally
        p = p - y
    End Try
    |]
    p = z
  End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
            '
            Assert.Equal("ax", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("p, ax", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("p, z", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("p, z", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("p, z, ax", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, p, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestDataFlowsOutWithException()
            Dim analysisResults = CompileAndAnalyzeDataFlow(
                <compilation name="TestDataFlowsOutWithException">
                    <file name="a.b">
Imports System

Public Class Test
  Sub TryMethod(ByRef p As Long)
    Try
        [| p = 1 |]
        p = 2
    Finally
    End Try
  End Sub
End Class
  </file>
                </compilation>)

            Assert.Equal("p", GetSymbolNamesJoined(analysisResults.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithVarDecl01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithVarDecl01">
                    <file name="a.b">
Imports System

Public Class Test
    Sub TryMethod(ByRef p As Long)
        Dim x As Byte = 111, y As Byte = 222
[|
        Try
            Try
                If False Then
                    Dim s As String = "A"
                End If
L:          Catch ax As ArgumentException
                ' start1
                Console.Write(ax)
                GoTo L
                Dim s As UShort = x
                ' end1
            End Try
            ' start2
        Catch ex As Exception
            Console.Write(ex)
            Dim s As Byte = y
            Throw
            ' end2
        Finally
            Dim s As Char = "a"c
        End Try
|]
        'p = s 'BC30451
    End Sub

End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("s, ax, s, ex, s, s", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, y, ax, ex", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("s, ax, s, ex, s, s", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, x, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithVarDecl02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithVarDecl02">
                    <file name="a.b">
Imports System

Public Class Test
    Shared Sub TryMethod(ByRef p As Long)
        Dim x As Byte = 111, y As Byte = 222

        Try
            Try
                If False Then
                    Dim s As String = "A"
                End If
L:          Catch ax As ArgumentException
[|
                Console.Write(ax)
                GoTo L
                Dim s As UShort = x
|]
            End Try
            ' start2
        Catch ex As Exception
            Console.Write(ex)
            Dim s As Byte = y
            Throw
            ' end2
        Finally
            Dim s As Char = "a"c
        End Try

        'p = s 'BC30451
    End Sub

End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            '
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("ax", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, ax", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("p, y, ex", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("p, x, y, s, ax, ex, s, s", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchWithVarDecl03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchWithVarDecl03">
                    <file name="a.b"><![CDATA[
Imports System

Public Class Test
    Function F(x As ULong) As ULong
[|
        Try
            Dim y As UShort = 11
            x = y * y - x
        Catch e As Exception When x < 88
            Console.WriteLine(x)
        Catch e As Exception When x >= 88
            Console.WriteLine(e)
            x = 200
        Finally
            x = x - 1
        End Try
|]
        F = x
    End Function
End Class
  ]]></file>
                </compilation>)

            Dim dataFlowAnalysis = analysisResults.Item2
            '
            Assert.Equal("y, e, e", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x, y, e", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x, y, e, e", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("F, Me, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchUseLocalInCatch01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchUseLocalInCatch01">
                    <file name="a.b">
Imports System

Public Class Test
    Sub Test(ByRef x As UInteger)
        Dim local As Exception = Nothing
        [| 
        Try
            Dim y As UShort = 11
            x = y * y - x
        Catch local When Filter(x)
            'If TypeOf local Is ArgumentException Then
            If local.ToString().Contains("ArgumentException") Then
                x = 0
            End If
        Finally

        End Try
        |]
        If local IsNot Nothing Then

        End If
    End Sub

    Function Filter(z As ULong) As Boolean
        Return z = 0
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            '
            Assert.Equal("x, local", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("Me, x, local, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("x, local", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x, local, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("Me, x, local", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchParameterInCatch()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchParameterInCatch">
                    <file name="a.b">
Imports System

Public Class Test
    Shared Sub Test01(s As String, ByRef pex As ArgumentException)
        [| 
        Try
            If String.IsNullOrWhiteSpace(s) Then
                Throw New ArgumentException() ' NYI
            End If
        Catch pex When s Is Nothing
            Console.WriteLine("Nothing Ex={0}", pex)
        Catch pex When s.Trim() Is String.Empty
            Console.WriteLine("Empty")
        End Try
        |]
    End Sub
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("s, pex", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("s, pex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchUseLocalParamInCatch01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchUseLocalParamInCatch01">
                    <file name="a.b">
Imports System

Public Class Test
    Function Filter(s As String) As Boolean
        Return String.IsNullOrWhiteSpace(s)
    End Function

    Function TryFunc(s As String, ByRef pex As Exception) As Integer
        Dim lex As ArgumentException
            [|      
       Try
            TryFunc = s.Length
        Catch lex When s Is Nothing
            Console.Write("ArgEx={0}", lex)
        Catch pex
            Console.Write("OtherEx={0}", pex)
        Finally
    
            Try
                Dim y As UShort
                y = TryFunc + 1
            Catch lex
                Console.Write("X")
            Catch pex When Filter(s)
                TryFunc = -1
            End Try
        End Try         
            |]   
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("TryFunc, Me, s", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("TryFunc, Me, s, pex, lex", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("TryFunc, pex, lex, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, s, pex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryCatchUseLocalParamInCatch02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchUseLocalParamInCatch02">
                    <file name="a.b">
Imports System

Public Class Test
    Function Filter(s As String) As Boolean
        Return String.IsNullOrWhiteSpace(s)
    End Function

    Function TryFunc(s As String, ByRef pex As Exception) As Integer
        Dim lex As ArgumentException
        Try
            TryFunc = s.Length
        Catch lex When s Is Nothing
            Console.Write("ArgEx={0}", lex)
        Catch pex
            Console.Write("OtherEx={0}", pex)
        Finally
            [|  
            Try
                Dim y As UShort
                y = TryFunc + 1  
            Catch lex
                Console.Write("X")
            Catch pex When Filter(s)
                TryFunc = -1
            End Try
            |]   
        End Try    
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysis = analysisResults.Item1
            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Empty(controlFlowAnalysis.EntryPoints)
            Assert.Empty(controlFlowAnalysis.ExitPoints)
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("TryFunc, Me, s", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("pex", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("TryFunc, Me, s", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("TryFunc, pex, lex, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("s, pex, lex", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("TryFunc, Me, s, pex, lex", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact, WorkItem(8781, "DevDiv_Projects/Roslyn")>
        Public Sub TestTryCatchUseFuncAsLocalInCatch()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryCatchUseFuncLocalInCatch">
                    <file name="a.b"><![CDATA[
Imports System

Public Class Test
    Function TryFunc(ByRef x As UInteger) As Exception
        TryFunc = Nothing
        [|
        Try
            Dim y As UShort = 11
            x = y * y - x
        Catch TryFunc When x < 88
            x = 100
            Console.WriteLine(x)
        Catch TryFunc When x >= 88
            Console.WriteLine(TryFunc)
            x = 200
        Finally
           
        End Try
        |]
    End Function
End Class ]]>
                    </file>
                </compilation>)

            Dim dataFlowAnalysis = analysisResults.Item2

            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("TryFunc, x, y", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("TryFunc, x, y", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("TryFunc, Me, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryWithLambda01()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryWithLambda01">
                    <file name="a.b">
Imports System

Public Class TryCatchFinally

    Delegate Function D01(dp As Long) As Long

    Shared Function M(ByRef refp As Long) As Long
        M = 12345
[|
        Try
            refp = refp + 11
            M = refp + 1            
        Catch e As Exception

            Dim d As D01 = Function(ap)
                               e = New ArgumentException(ap.ToString())
                               Return e.Message.Length
                           End Function
            M = d(refp)

        End Try
|]
    End Function
End Class

  </file>
                </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)

            Assert.Equal("e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("M", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("refp", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("refp", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("refp, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("M, refp, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("refp", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("M, refp", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryWithLambda02()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryWithLambda02">
                    <file name="a.b">
Imports System

Public Class TryCatchFinally

    Delegate Function D01(dp As Long) As Long

    Function M(ByRef refp As Long) As Long
        M = 12345

        Try
            M = refp + 1
        Catch e As Exception
[|
            Dim d As D01 = Function(ap)
                               e = New ArgumentException(ap.ToString())
                               Return e.Message.Length
                           End Function
            M = d(refp)
|]
        End Try

    End Function
End Class

  </file>
                </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)

            Assert.Equal("d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("M, d", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("refp", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            ' Bug#8781 - By Design
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Equal("refp, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("M, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("refp", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("M, Me, refp, e", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestTryWithLambda03()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryWithLambda03">
                    <file name="a.b">
Imports System

Public Class TryCatchFinally

    Delegate Function D02(dp As Byte) As String

    Function M(p As UShort) As String
        Dim local As Byte = DirectCast((p Mod Byte.MaxValue), Byte)
        [| Try
            If local = p Then
                Return Nothing
            End If

            Return local.ToString()
        Catch e As Exception
            Dim d As D02 = Function(ap As Byte)
                               Return (ap + local + p).ToString() + e.Message
                           End Function
            Return d(local)
        End Try |]
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)

            Assert.Equal("e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Equal("p, local, e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Equal("p, local, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("local, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, p, local", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))

        End Sub

        <Fact()>
        Public Sub TestTryWithLambda04()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryWithLambda04">
                    <file name="a.b">
Imports System

Public Class TryCatchFinally

    Delegate Function D02(dp As Byte) As String
    Shared Function M(p As UShort) As String
        Dim local As Byte = DirectCast((p Mod Byte.MaxValue), Byte)
        Try
            If local = p Then
                Return Nothing
            End If

            Return local.ToString()
        Catch e As Exception
            [| Dim d As D02 = Function(ap As Byte)
                               Return (ap + local + p).ToString() + e.Message
                           End Function
            Return d(local)
            |]
        End Try
    End Function
End Class
  </file>
                </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)

            Assert.Equal("d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("d", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("p, local, e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("p, local, e", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Equal("p, local, e, d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("d, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("p, local, e", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact, WorkItem(541892, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541892")>
        Public Sub TestTryWithLambda05()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                <compilation name="TestTryWithLambda05">
                    <file name="a.b">
Imports System

Friend Module TryCatchFinally

    Delegate Function D02(dp As Byte) As String

    Sub M(p As Byte)
        Dim local As Byte = p + 1
        [| Try
            If local = p Then
                Exit Sub
            End If

            Return
        Catch e As Exception When (Function(ap As Byte) As String
                                       Return (ap + local + p).ToString() + e.Message
                                   End Function)(1).Length > 0

        End Try  |]
    End Sub
End Module
  </file>
                </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            '
            Assert.Equal("e, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Equal("p, local, e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            '
            Assert.Equal("p, local, e, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("e, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact, WorkItem(541892, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541892"), WorkItem(528622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528622")>
        Public Sub TestTryWithLambda06()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
                <compilation name="TestTryWithLambda06">
                    <file name="a.b">
Imports System

Class C
    Sub M(p() As String)
         Try
            If p Is Nothing Then
                Exit Sub
            End If
        Catch e As Exception When e.Message.Contains( [| (Function(ByRef ap As String) As String
                                     Return ap + e.Message
                                   End Function)(p(0)) |] ) 
        End Try  
    End Sub
End Class
  </file>
                </compilation>)

            Assert.Equal("ap", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            ' 8794 (Won't fix)
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Equal("e", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("p, e", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("ap", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("p, e, ap", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            ' Bug#8789 (fixed)
            Assert.Equal("ap", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("p, e", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, p, e", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(543597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543597")>
        <Fact()>
        Public Sub TryStatement()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation name="Test">
                    <file name="a.b">
Module Program
    Sub Main(args As String())
        Try
        Catch ex As Exception
        Finally
        End Try
    End Sub
End Module
  </file>
                </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim tryBlock = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.TryBlock).AsNode(), TryBlockSyntax)

            Dim statement As StatementSyntax = tryBlock.TryStatement
            Assert.False(model.AnalyzeControlFlow(statement, statement).Succeeded)
            Assert.False(model.AnalyzeDataFlow(statement, statement).Succeeded)

            statement = tryBlock.CatchBlocks(0).CatchStatement
            Assert.False(model.AnalyzeControlFlow(statement, statement).Succeeded)
            Assert.False(model.AnalyzeDataFlow(statement, statement).Succeeded)

            statement = tryBlock.FinallyBlock.FinallyStatement
            Assert.False(model.AnalyzeControlFlow(statement, statement).Succeeded)
            Assert.False(model.AnalyzeDataFlow(statement, statement).Succeeded)
        End Sub

        <WorkItem(543597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543597")>
        <Fact()>
        Public Sub CatchStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation name="Test">
                    <file name="a.b">
Module Program
    Sub Main(args As String())
        Try

        Catch ex As Exception
        Finally

        End Try
    End Sub
End Module
  </file>
                </compilation>)

            Dim index = compilation.SyntaxTrees.First().GetCompilationUnitRoot().ToFullString().IndexOf("Catch ex As Exception", StringComparison.Ordinal)
            Dim statement = DirectCast(compilation.SyntaxTrees.First().GetCompilationUnitRoot().FindToken(index).Parent, StatementSyntax)
            Dim binding = compilation.GetSemanticModel(compilation.SyntaxTrees.First())
            Dim controlFlowAnalysisResults = binding.AnalyzeControlFlow(statement, statement)
            Dim dataFlowAnalysisResults = binding.AnalyzeDataFlow(statement, statement)

            Assert.False(controlFlowAnalysisResults.Succeeded)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(543597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543597")>
        <Fact()>
        Public Sub FinallyStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation name="Test">
                    <file name="a.b">
Module Program
    Sub Main(args As String())
        Try

        Catch ex As Exception
        Finally

        End Try
    End Sub
End Module
  </file>
                </compilation>)

            Dim index = compilation.SyntaxTrees.First().GetCompilationUnitRoot().ToFullString().IndexOf("Finally", StringComparison.Ordinal)
            Dim statement = DirectCast(compilation.SyntaxTrees.First().GetCompilationUnitRoot().FindToken(index).Parent, StatementSyntax)
            Dim binding = compilation.GetSemanticModel(compilation.SyntaxTrees.First())
            Dim controlFlowAnalysisResults = binding.AnalyzeControlFlow(statement, statement)
            Dim dataFlowAnalysisResults = binding.AnalyzeDataFlow(statement, statement)

            Assert.False(controlFlowAnalysisResults.Succeeded)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

#End Region

#Region "SyncLock"
        <Fact()>
        Public Sub SimpleSyncLockBlock()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    
        dim lock = new C1()
        [|
        SyncLock lock
            Console.WriteLine(x)
        End SyncLock
        |]
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(2, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.DataFlowsIn(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(2, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.ReadInside(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.ReadOutside.Count)
            Assert.Equal(0, dataFlowAnalysisResults.VariablesDeclared.Count)
            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.WrittenOutside(1).ToDisplayString)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockInside()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    
        dim lock as Integer = 23
        SyncLock lock
            [|        
            Console.WriteLine(x)
            |]
        End SyncLock        
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(1, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)

            Assert.Equal(1, dataFlowAnalysisResults.ReadOutside.Count)
            Assert.Equal("lock", dataFlowAnalysisResults.ReadOutside(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.VariablesDeclared.Count)
            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.WrittenOutside(1).ToDisplayString)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockErrorValueType()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    
        dim lock = new C1()
        
        [|
        SyncLock lock
            Console.WriteLine(x)
        End SyncLock
        |]
        
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(2, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.DataFlowsIn(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(2, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.ReadInside(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.ReadOutside.Count)
            Assert.Equal(0, dataFlowAnalysisResults.VariablesDeclared.Count)
            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
            Assert.Equal("lock", dataFlowAnalysisResults.WrittenOutside(1).ToDisplayString)
        End Sub

#End Region

#Region "Using"

        <Fact()>
        Public Sub UsingAroundCatch()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="UsingAroundCatch">
        <file name="a.vb">
Class Test
    Public Shared Sub Main()
        Dim y As New MyManagedClass()
        Using y
           [|Try
                System.Console.WriteLine("Try")
            Finally
                y = Nothing
                System.Console.WriteLine("Catch")
            End Try|]
        End Using
    End Sub
End Class
Public Class MyManagedClass
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        System.Console.WriteLine("Res1.Dispose()")
    End Sub
End Class
</file>
    </compilation>)
            Assert.Empty(analysis.VariablesDeclared)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Empty(analysis.DataFlowsIn)
            Assert.Empty(analysis.DataFlowsOut)
            Assert.Empty(analysis.ReadInside)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub MultipleResourceWithDifferentType()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="MultipleResourceWithDifferentType">
        <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Using x = New MyManagedClass(), y = New MyManagedClass1()
        End Using|]
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose1")
    End Sub
End Structure
</file>
    </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Empty(analysis.ReadOutside)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Empty(analysis.WrittenOutside)
            Assert.Empty(analysis.DataFlowsIn)
            Assert.Empty(analysis.DataFlowsOut)
        End Sub

        <Fact()>
        Public Sub MultipleResourceWithDifferentType_1()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="MultipleResourceWithDifferentType">
        <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Using x = New MyManagedClass(), y = New MyManagedClass1()
        End Using|]
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Dim Name As String
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose1")
    End Sub
End Structure
</file>
    </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Empty(analysis.ReadOutside)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Empty(analysis.WrittenOutside)
            Assert.Empty(analysis.DataFlowsIn)
            Assert.Empty(analysis.DataFlowsOut)
        End Sub

        <Fact()>
        Public Sub MultipleResource()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="MultipleResource">
        <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Using x, y As New MyManagedClass()
        End Using|]
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
</file>
    </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Empty(analysis.ReadOutside)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Empty(analysis.WrittenOutside)
            Assert.Empty(analysis.DataFlowsIn)
            Assert.Empty(analysis.DataFlowsOut)
        End Sub

        <Fact()>
        Public Sub MultipleResource_1()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="MultipleResource">
        <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x = 1
        Dim y = 1
        [|Using goo, goo2 As New MyManagedClass(x), goo3, goo4 As New MyManagedClass(y)
        End Using|]
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub New(x As Integer)
    End Sub
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
</file>
    </compilation>)
            Assert.Equal("goo, goo2, goo3, goo4", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("goo, goo2, goo3, goo4", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y, goo, goo2, goo3, goo4", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Empty(analysis.ReadOutside)
            Assert.Equal("goo, goo2, goo3, goo4", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenOutside))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Empty(analysis.DataFlowsOut)
        End Sub

        <Fact()>
        Public Sub QueryInUsing()
            Dim analysis = CompileAndAnalyzeDataFlow(
    <compilation name="QueryInUsing">
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Imports System.Linq
Class Program
    Shared Sub Main()
        Dim objs = GetList()
        [|Using x As MyManagedClass = (From y In objs Select y).First
        End Using|]
    End Sub
    Shared Function GetList() As List(Of MyManagedClass)
        Return Nothing
    End Function
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Class
        </file>
    </compilation>)

            Assert.Equal("x, y, y", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("objs, x, y", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Empty(analysis.ReadOutside)
            Assert.Equal("x, y, y", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("objs", GetSymbolNamesJoined(analysis.WrittenOutside))
            Assert.Equal("objs", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Empty(analysis.DataFlowsOut)
        End Sub

        <Fact()>
        Public Sub JumpOutFromUsing()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="JumpOutFromUsing">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Using x = New MyManagedClass()
            GoTo label1
        End Using|]
label1:
    End Sub
End Class
Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>)

            Dim controlflowAnalysis = analysis.Item1
            Dim dataflowAnalysis = analysis.Item2
            Assert.Equal("x", GetSymbolNamesJoined(dataflowAnalysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataflowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataflowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataflowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.DataFlowsOut))

            Assert.Equal(0, controlflowAnalysis.EntryPoints.Count)
            Assert.Equal(1, controlflowAnalysis.ExitPoints.Count)

        End Sub

        <Fact()>
        Public Sub JumpOutFromUsing_1()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="JumpOutFromUsing">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        Dim Obj1 = New MyManagedClass()
        Try
            [|Using Obj1
                Dim i As Integer = i \ i
                Exit Try
            End Using|]
        Catch ex As Exception
        End Try
    End Sub
End Class
Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>)

            Dim controlflowAnalysis = analysis.Item1
            Dim dataflowAnalysis = analysis.Item2
            Assert.Equal("i", GetSymbolNamesJoined(dataflowAnalysis.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesJoined(dataflowAnalysis.AlwaysAssigned))
            Assert.Equal("Obj1, i", GetSymbolNamesJoined(dataflowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.ReadOutside))
            Assert.Equal("i", GetSymbolNamesJoined(dataflowAnalysis.WrittenInside))
            Assert.Equal("args, Obj1, ex", GetSymbolNamesJoined(dataflowAnalysis.WrittenOutside))
            Assert.Equal("Obj1", GetSymbolNamesJoined(dataflowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataflowAnalysis.DataFlowsOut))

            Assert.Equal(0, controlflowAnalysis.EntryPoints.Count)
            Assert.Equal(1, controlflowAnalysis.ExitPoints.Count)

        End Sub

        <Fact()>
        Public Sub UsingWithVariableDeclarations()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    

        [|
        Using goo1 As New MyDisposable(), goo2 As New MyDisposable()
            Console.WriteLine(x)
        End Using
        |]
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(2, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal("goo1", dataFlowAnalysisResults.AlwaysAssigned(0).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.AlwaysAssigned(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(3, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)
            Assert.Equal("goo1", dataFlowAnalysisResults.ReadInside(1).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.ReadInside(2).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.ReadOutside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.VariablesDeclared.Count)
            Assert.Equal("goo1", dataFlowAnalysisResults.VariablesDeclared(0).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.VariablesDeclared(1).ToDisplayString)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenInside.Count)
            Assert.Equal("goo1", dataFlowAnalysisResults.WrittenInside(0).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.WrittenInside(1).ToDisplayString)

            Assert.Equal(1, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
        End Sub

        <Fact()>
        Public Sub UsingWithExpression()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub New(name as String)
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    

        [|
        Using New MyDisposable(x)
            Console.WriteLine(x)
        End Using
        |]
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(1, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.ReadOutside.Count)

            Assert.Equal(0, dataFlowAnalysisResults.VariablesDeclared.Count)

            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)

            Assert.Equal(1, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
        End Sub

        <Fact()>
        Public Sub UsingInsideUsing()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub New(name as String)
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    

        Using goo1 As New MyDisposable(x)
        [|
            Console.WriteLine(x)
        |]
        End Using
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(1, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)

            Assert.Equal(2, dataFlowAnalysisResults.ReadOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
            Assert.Equal("goo1", dataFlowAnalysisResults.WrittenOutside(1).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.VariablesDeclared.Count)

            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
            Assert.Equal("goo1", dataFlowAnalysisResults.WrittenOutside(1).ToDisplayString)
        End Sub

        <Fact()>
        Public Sub UsingEmptyStructTypeErrorCase()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Strict On

Imports System

Structure MyDisposableStructure
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Dim x as String = "Inside Using."    

        [|
        Using goo1 As New MyDisposableStructure(), goo2 As New MyDisposableStructure()
            Console.WriteLine(x)
        End Using
        |]
    End Sub
End Class        
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)

            Assert.Equal(0, dataFlowAnalysisResults.AlwaysAssigned.Count)
            Assert.Equal(0, dataFlowAnalysisResults.Captured.Count)

            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("x", dataFlowAnalysisResults.DataFlowsIn(0).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.DataFlowsOut.Count)

            Assert.Equal(3, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.ReadInside(0).ToDisplayString)
            Assert.Equal("goo1", dataFlowAnalysisResults.ReadInside(1).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.ReadInside(2).ToDisplayString)

            Assert.Equal(0, dataFlowAnalysisResults.ReadOutside.Count)

            Assert.Equal(2, dataFlowAnalysisResults.VariablesDeclared.Count)
            Assert.Equal("goo1", dataFlowAnalysisResults.VariablesDeclared(0).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.VariablesDeclared(1).ToDisplayString)

            Assert.Equal(2, dataFlowAnalysisResults.WrittenInside.Count)
            Assert.Equal("goo1", dataFlowAnalysisResults.WrittenInside(0).ToDisplayString)
            Assert.Equal("goo2", dataFlowAnalysisResults.WrittenInside(1).ToDisplayString)

            Assert.Equal(1, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("x", dataFlowAnalysisResults.WrittenOutside(0).ToDisplayString)
        End Sub

#End Region

    End Class

End Namespace
