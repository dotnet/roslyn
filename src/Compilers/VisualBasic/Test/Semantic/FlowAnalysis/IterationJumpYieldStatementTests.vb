' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.FlowAnalysis

    Partial Public Class FlowAnalysisTests
        Inherits FlowTestBase

#Region "While, Do, Until"

        <Fact()>
        Public Sub TestDoLoop()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDoLoop">
          <file name="a.b">
Module Program
    Sub Main()
        [|
        Do
        Loop
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestDoLoop02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDoLoop02">
          <file name="a.b">
Module Program
    Sub Main()
        [|
        Do
        Loop While True
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestExitStatement()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestBreakStatement">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
        while true 
[|
            exit while
            while true 
                exit while
            end while
            dim y as integer 
|]
        end while
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestContinueStatement()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestContinueStatement">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
        while true 
[|
            continue while
            while true
                continue while
            end while
            dim y as integer
|]
        end while
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantBooleanFalse()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantBooleanFalse">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while false  
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantBooleanTrue()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantBooleanTrue">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while true 
        end while
|]
    end sub
end sub
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantBooleanXor()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantBooleanXor">
          <file name="a.b">
class C
    shared sub Goo()
        dim x
[|
        while true xor false 
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantBooleanNew()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantBooleanNew">
          <file name="a.b">
class C
    shared sub Goo()

        dim x as integer
[|
        while not new boolean()
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable) 'C# is false but VB does not consider New Boolean() as a constant expression
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithAssignmentInBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithAssignmentInBody">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as boolean
[|
        while not x
            x = true
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact(), WorkItem(538421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538421")>
        Public Sub TestLoopWithConstantEnumEquality()
            Dim analysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithConstantEnumEquality">
          <file name="a.b">
Imports System
Class C
    Shared Sub Goo()
[|
        While DayOfWeek.Sunday = 0
        End While
|]
    End Sub
End Class
            </file>
      </compilation>)
            Assert.True(analysisResults.StartPointIsReachable)
            Assert.False(analysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantNaNComparison()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantNaNComparison">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while not 0 > double.NaN
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantNaNComparison2()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantNaNComparison2">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while double.NaN &lt;&gt; double.NaN
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantStringEquality()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithConstantStringEquality">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while """" = """" + nothing
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithEmptyBlockAfterIt()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithEmptyBlockAfterIt">
          <file name="a.b">
class C
    shared sub Goo()
        dim x as integer
[|
        while true 
        end while
        if true then
        end if
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestLoopWithUnreachableExitStatement()
            Dim controlFlowAnalysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithUnreachableExitStatement">
          <file name="a.b">
class C
    shared sub Main()
[|
        while true
            if false then
                exit while
            end if
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithReachableExitStatement()
            Dim controlFlowAnalysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithReachableExitStatement">
          <file name="a.b">
class C
    shared sub Main()
[|
        while true
         if true then
            exit while
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithContinueStatement()
            Dim controlFlowAnalysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithContinueStatement">
          <file name="a.b">
class C
    shared sub Main()

[|
        while true
            continue while
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithContinueAndUnreachableExitStatement()
            Dim controlFlowAnalysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithContinueAndUnreachableExitStatement">
          <file name="a.b">
class C
    shared sub Main()
[|
        while true
            continue while
            exit while
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithConstantTernaryOperator()
            Dim controlFlowAnalysisResults = CompileAndAnalyzeControlFlow(
      <compilation name="TestLoopWithConstantTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()
[|
        while if(true, true, true) 
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestLoopWithShortCircuitingOr()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestLoopWithShortCircuitingOr">
          <file name="a.b">
class C
    shared sub Main()
        dim x as boolean
[|
        while true orelse x
        end while
|]
    end sub
end class
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(539303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539303")>
        <Fact()>
        Public Sub DoLoopWithContinue()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="DoLoopWithContinue">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        [|
        Do
            Console.Write(i)
            i = i + 1
            Continue Do
            'Blah
        Loop Until i > 5 |]        
        Return x
    End Function
End Class
            </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(539303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539303")>
        <Fact()>
        Public Sub DoLoopWithGoto()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="DoLoopWithGoto">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        goto l1
        [|
l2:
        Do
            Console.Write(i)
            i = i + 1
            Continue Do
            'Blah
        Loop Until i = 5|] 
l1:     goto l2  
        Return x
    End Function
End Class
            </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(1, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.False(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub DoWhile()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="DoLoopWithGoto">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        While [|x|] &lt; 10
            Console.Write(i)
            i = i + 1
            Continue While
            'Blah
            End  while 
        End Sub
    End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

#End Region

#Region "For, For Each"

        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact()>
        Public Sub TestVariablesDeclaredInForLoop()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclaredInForLoop">
          <file name="a.b">
class C 
    public sub F(x as integer)
        dim a as integer = 100
[|
        For i = 1 To a Step x
        Next
|]
        dim c as integer
    end sub
end class</file>\n</compilation>)

            Assert.Equal("i", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x, a", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact()>
        Public Sub TestVariablesDeclaredInForeachLoop()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclaredInForeachLoop">
          <file name="a.b">
class C
    public sub F(x as integer)

        Dim ary = New Byte() {1, 2, 3}
[|
        For Each a In ary

        Next
|]
        int b
    end sub
end class
    </file>
      </compilation>)

            Assert.Equal("a", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("ary", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
        End Sub

        <Fact()>
        Public Sub ForEachLoopBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On

Class C1
  public shared function goo() as Integer()
    return new Integer() {1,2,3}
  end function
End Class

Module M
    Public Sub Main()
        
        For each unassignedRef1 as C1 in New C1() {unassignedRef1}
            [|System.Console.WriteLine(unassignedRef1)

            if unassignedRef1 isnot Nothing then
                exit for
            end if

            continue for
            return
|]
            System.Console.WriteLine(unassignedRef1)
        Next 
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count)
            Assert.Equal(3, controlFlowAnalysisResults.ExitPoints.Count)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Empty(dataFlowAnalysisResults.Captured)
            Assert.Equal(1, dataFlowAnalysisResults.DataFlowsIn.Count)
            Assert.Equal("unassignedRef1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Equal(1, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("unassignedRef1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(1, dataFlowAnalysisResults.ReadOutside.Count)
            Assert.Equal("unassignedRef1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Empty(dataFlowAnalysisResults.VariablesDeclared)
            Assert.Empty(dataFlowAnalysisResults.WrittenInside)
            Assert.Equal(1, dataFlowAnalysisResults.WrittenOutside.Count)
            Assert.Equal("unassignedRef1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub ForEachLoopBlock()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On

Module M
    Public Sub Main()
        [|
        For each x in new Integer() {}
            For each y as Integer in new Integer() {}
            System.Console.WriteLine(x+y)
        Next y, x 
        |]
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Empty(dataFlowAnalysisResults.Captured)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsIn)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Equal(2, dataFlowAnalysisResults.ReadInside.Count)
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Empty(dataFlowAnalysisResults.ReadOutside)
            Assert.Equal(2, dataFlowAnalysisResults.WrittenInside.Count)
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Empty(dataFlowAnalysisResults.WrittenOutside)

            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub ForEachCollectionOnly()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On

Module M
    Public Sub Main()
        
        For each x in [| new Integer() {1, 2, 3} |]
            System.Console.WriteLine(x)
        Next x
        
    End Sub
End Module
    </file>
   </compilation>)

            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Empty(dataFlowAnalysisResults.VariablesDeclared)
            Assert.Empty(dataFlowAnalysisResults.Captured)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsIn)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Empty(dataFlowAnalysisResults.ReadInside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(0, dataFlowAnalysisResults.WrittenInside.Count)
            Assert.Empty(dataFlowAnalysisResults.WrittenInside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub ForEachControlVariableOnly()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On

Module M
    Public Sub Main()
        
        For each [| x |] in  new Integer() {1, 2, 3}
            System.Console.WriteLine(x)
        Next x
        
    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Empty(dataFlowAnalysisResults.VariablesDeclared) ' should be empty, or it could not be extracted.
            Assert.Empty(dataFlowAnalysisResults.Captured)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsIn)
            Assert.Empty(dataFlowAnalysisResults.DataFlowsOut)
            Assert.Empty(dataFlowAnalysisResults.ReadInside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Empty(dataFlowAnalysisResults.WrittenInside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub ForEachNextVariableOnly()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
                         <compilation>
                             <file name="a.vb">
                                Option Infer On
                                Module M
                                    Public Sub Main()
                                        For each x in  new Integer() {1, 2, 3}
                                            System.Console.WriteLine(x)
                                        Next [| x |]
                                    End Sub
                                End Module
                            </file>
                         </compilation>)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub CannotAnalyzeAnonymousTypeFieldName()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
                         <compilation>
                             <file name="a.vb">
                                Option Infer On
                                Module M
                                    Public Sub Main()
                                        Dim o = New With { . [|x|] = 1 }
                                    End Sub
                                End Module
                            </file>
                         </compilation>)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub CanAnalyzeAnonymousTypeFieldInitializer()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
              <compilation>
                  <file name="a.vb">
                    Option Infer On
                    Module M
                        Public Sub Main()
                            Dim y As Integer = 1
                            Dim o = New With { .x = [|y|] }
                        End Sub
                    End Module
                </file>
              </compilation>)

            Assert.False(dataFlowAnalysisResults.AlwaysAssigned.Any)
        End Sub

        <Fact()>
        Public Sub ForEachReuseControlVariable()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Module M
    Public Sub Main()
        
        Dim x as Integer
        
        [|For each x in new Integer() {1, 2, x}
            System.Console.WriteLine(x)
        Next x|]
        
        System.Console.WriteLine(x)
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Empty(dataFlowAnalysisResults.AlwaysAssigned)
            Assert.Empty(dataFlowAnalysisResults.VariablesDeclared)
            Assert.Empty(dataFlowAnalysisResults.Captured)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Empty(dataFlowAnalysisResults.WrittenOutside)

            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
              <compilation>
                  <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                Dim x as Integer = x + a + b
                if a = 1 andalso b = 3 then
                    del = Sub() call Console.WriteLine(x) 
                end if
            Next b, a|]

        del.Invoke()
    End Sub
End Module
    </file>
              </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal2()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
               <compilation>
                   <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                For Each c In New Integer() {3, 4}
                    Dim x as Integer = x + a + b
                    if a = 1 andalso b = 3 then
                        del = Sub() call Console.WriteLine(x) 
                    end if
                Next c, b|]

        del.Invoke()
    End Sub
End Module
    </file>
               </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.EntryPoints.Any)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal3()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
              <compilation>
                  <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                For Each c In New Integer() {3, 4}
                    Dim x as Integer = x + a + b
                    if a = 1 andalso b = 3 then
                        del = Sub() call Console.WriteLine(x) 
                    end if
                Next c, b, a|]

        del.Invoke()
    End Sub
End Module
    </file>
              </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal4()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
              <compilation>
                  <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                For Each c In New Integer() {3, 4}
                    Dim x as Integer = x + a + b
                    if a = 1 andalso b = 3 then
                        del = Sub() call Console.WriteLine(x) 
                    end if
                Next c, b, a|]

        del.Invoke()
    End Sub
End Module
    </file>
              </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal5()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
               <compilation>
                   <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                For Each c In New Integer() {3, 4}
                    Dim x as Integer = x + a + b
                    if a = 1 andalso b = 3 then
                        del = Sub() call Console.WriteLine(x) 
                    end if
                Next c, b|]
        Next 

        del.Invoke()
    End Sub
End Module
    </file>
               </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.EntryPoints.Any)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal6()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
               <compilation>
                   <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            [|For Each b In New Integer() {3, 4}
                For Each c In New Integer() {3, 4}
                    For Each d In New Integer() {3, 4}
                        Dim x as Integer = x + a + b
                    Next d, c, b|]
        Next 

        del.Invoke()
    End Sub
End Module
    </file>
               </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.EntryPoints.Any)
        End Sub

        <Fact()>
        Public Sub ForEachLiftedLocal_CorrectRegion()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation name="ForEachLiftedLocal_CorrectRegion">
       <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        [|For Each a In New Integer() {1, 2}
            For Each b In New Integer() {3, 4}
                Dim x as Integer = x + a + b
                if a = 1 andalso b = 3 then
                    del = Sub() call Console.WriteLine(x) 
                end if
            Next b, a|]

        del.Invoke()
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))

            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub ForEachReadInsideCollection()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On
Option Strict On
Class C
    Shared Property P1(ByVal x As Integer) As Integer        
      Get
              Return x + 5        
      End Get        
      Set(ByVal Value As Integer)
      End Set    
    End Property    
    
    Shared Function F(p as Integer) as Integer
        return p + 1    
    End Function

    Shared Field() as Integer = {3,4,5}

    Public Shared Sub Main()    
        Dim X As Integer = 1
        Dim Y As Integer = 1
        Dim Z As Integer = 1
        [|
        For Each Y As Integer In New Integer() {P1(X), F(Y), Field(Z)}
          Console.WriteLine(Y)
        Next
        |]
    End Sub
End Class
    </file>
   </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal("X, Z, Y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
        End Sub

        <WorkItem(542112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542112")>
        <Fact()>
        Public Sub ForEachExitPoints_Failure()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                    <compilation>
                        <file name="a.vb">
                        Option Strict On
                        Option Infer On
                        Public Class MyClass1
                            Public Shared Sub Main()
                                For each I in new Integer() {1, 2, 3}
                                    [|
                                    For each J in new Integer() {4, 5, 6}
                                        Exit For
                                    Next j, i
                                    |]
                            End Sub
                        End Class
                    </file>
                    </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.False(controlFlowAnalysisResults.Succeeded)
        End Sub

        <Fact()>
        Public Sub ForEachExitPoints_Success()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
                    <compilation>
                        <file name="a.vb">
                        Option Strict On
                        Option Infer On
                        Public Class MyClass1
                            Public Shared Sub Main()
                                For each I in new Integer() {1, 2, 3}
                                    [|
                                    For each J in new Integer() {4, 5, 6}
                                        Exit For
                                    Next
                                    |]
                                Next
                            End Sub
                        End Class
                    </file>
                    </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.True(controlFlowAnalysisResults.Succeeded)
            Assert.Empty(controlFlowAnalysisResults.ExitPoints)
        End Sub

        <Fact()>
        Public Sub Bug9014()
            Xunit.Assert.Throws(Of ArgumentException)(
                Sub()
                    Dim analysisResults =
                CompileAndAnalyzeControlAndDataFlow(
                   <compilation>
                       <file name="a.vb">
Option Infer On

Module M
    Public Sub Main()

        For each x in [| new Integer() {1, 2, 3} |]
            System.Console.WriteLine(x)
        Next x

    End Sub
End Module

    </file>
                   </compilation>)

                End Sub)
        End Sub

        <Fact()>
        Public Sub ForEachVariablesDeclared()
            Dim tree = VisualBasicSyntaxTree.ParseText(<file>
Public Module Program
    Public Sub Main()
        Dim args = new String(){ "hi" }
        For Each s in args
            Dim b as Integer() = new Integer(){args.Length}
        Next
    End Sub
End Module
                    </file>.Value)
            Dim comp = VisualBasicCompilation.Create("ForEach",
                                          syntaxTrees:={tree},
                                          references:={MsvbRef, MscorlibRef})
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim foreachBlock = tree.GetRoot.DescendantNodes.OfType(Of ForEachBlockSyntax).Single
            Dim flow = semanticModel.AnalyzeDataFlow(foreachBlock)
            Assert.Equal(2, flow.VariablesDeclared.Count)
            Assert.Equal(True, flow.VariablesDeclared.Any(Function(s) s.Name = "b"))
            Assert.Equal(True, flow.VariablesDeclared.Any(Function(s) s.Name = "s"))
        End Sub

        <WorkItem(543462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543462")>
        <Fact()>
        Public Sub NextStatementSyntaxInForEach()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        For Each i In New Integer() {4, 5}
        Next
    End Sub
End Module
      </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Next", StringComparison.Ordinal)).Parent, NextStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtNode, stmtNode)

            Assert.False(analysis.Succeeded)
        End Sub

#End Region

#Region "Goto, Label"

        <Fact, WorkItem(8784, "DevDiv_Projects/Roslyn")>
        Public Sub TestRegionGotoElseIf()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="RegionForIfElseIfWithoutElse">
          <file name="a.b">
Module Program
    Function Main(ByVal p As Long) As Long
      Dim v = 111
[|
        If 123 = p Then
            Return v - 1
        ElseIf -1 = p Then
L:          Return p * v      
        ElseIf p = 1 Then
           v = 222
        Else
           v = 212
        End If
|]
        goto L
    End Function
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(2, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(1, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Empty(dataFlowAnalysis.AlwaysAssigned)
            Assert.Equal("p, v", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Empty(dataFlowAnalysis.DataFlowsOut)
            Assert.Equal("p, v", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("v", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("p, v", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))

        End Sub

        <Fact, WorkItem(8784, "DevDiv_Projects/Roslyn")>
        Public Sub TestRegionGotoDoWhile()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="RegionForIfElseIfWithoutElse">
          <file name="a.b"><![CDATA[
Module Program
    Function M(ByVal p As Long) As Long
        '[|
        Do While 10 <= p
L:
            p = p - 3
            If p < 0 Then
                Return -p
            End If
        Loop
        '|]
        GoTo L
    End Sub
End Module
  ]]></file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(1, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.EndPointIsReachable)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Empty(dataFlowAnalysis.DataFlowsOut)
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))

        End Sub

        <Fact, WorkItem(543399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543399")>
        Public Sub GotoLabelName()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        GoTo [|45|]
    End Sub
End Module
      </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

#End Region

#Region "Return"

        <Fact()>
        Public Sub TestReturnStatements01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestReturnStatements01">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        return
|]
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub TestReturnStatements02()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestReturnStatements02">
          <file name="a.b">
class C 
    public sub F(x as integer)
        if x = 0 then return
[|
        if x = 1 then return
|]
        if x = 2 then return
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
        End Sub

        <WorkItem(539295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539295")>
        <Fact()>
        Public Sub TestReturnStatements03()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestReturnStatements03">
          <file name="a.b">
class C 
    public sub F(x as integer)
        if x = 0 then return
[|
        if x = 1 then exit sub
|]
        if x = 2 then return
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.True(analysis.StartPointIsReachable)
            Assert.True(analysis.EndPointIsReachable)
        End Sub

        <WorkItem(539295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539295")>
        <Fact()>
        Public Sub TestReturnStatements03a()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestReturnStatements03a">
          <file name="a.b">
class C 
    public sub F(x as integer)
        if True then return
[|
        if x = 1 then exit sub
|]
        if x = 2 then return
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.False(analysis.StartPointIsReachable)
            Assert.False(analysis.EndPointIsReachable)
        End Sub

        <WorkItem(539295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539295")>
        <Fact()>
        Public Sub TestReturnStatements04()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestReturnStatements03">
          <file name="a.b">
class C 
    public function F(x as integer) as integer
        if x = 0 then return
[|
        if x = 1 then exit function
|]
        if x = 2 then return
    end sub
end class
</file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.True(analysis.EndPointIsReachable)
        End Sub

        <Fact()>
        Public Sub TestReturnStatement()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestReturnStatement">
          <file name="a.b">
module C
    sub Main()
        dim x as integer = 1, y as integer = x
        [|
        return
        |]
        dim z as integer = (y) + 1
    end sub
end module
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestReturnStatementWithParenthesizedExpression()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestReturnStatementWithParenthesizedExpression">
          <file name="a.b">
module C
    function Goo() as integer
        dim x as integer = 1, y as integer = x
        [|
        return (y) + 1
        |]
    end function
end module
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub TestReturnStatementWithExpression()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestReturnStatementWithExpression">
          <file name="a.b">
module C
    function Goo() as integer
        dim x as integer = 0
[|
        x = 1
        return x 
|]
    end function
end module
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

#End Region

#Region "Yield Return"

#End Region

    End Class

End Namespace
