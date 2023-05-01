' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class FlowAnalysisTestsWithStaticLocals
        Inherits FlowTestBase

        'All the scenarios have had local declarations changed to Static Local Declarations to Ensure that the
        'flow analysis for static locals is exactly the same as for normal local declarations.
        '
        'The scenarios here should be the same as those in RegionAnalysisTest.vb with the exception of 
        'a. scenarios that did not involve any locals or 
        'b. scenarios with locals declared in lambda's only
        'c. scenarios using IL
        '
        'The method names should be the same as those in RegionAnalysisTest.vb and the tests should result in the same
        'flow analysis despite the fact that the implementation for static locals is different because of the implementation
        'required to preserve state across multiple invocations.
        '
        'Multiple calls are NOT required to verify the flow analysis for static locals.

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug11067()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11067">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Static y(,) = New Integer(,) {{[|From|]}}
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug13053a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug13053a">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Static  i As Integer = 1
        Static o = New MyObject With { .A = [| i |] }
    End Sub
End Class
</file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub XmlNameInsideEndTag()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="XmlNameInsideEndTag">
          <file name="a.b">
Module Module1
    Sub S(par As Integer)
        Static a = &lt;tag&gt; &lt;/ [| tag |] &gt;
    End Sub
End Module
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ExpressionsInAttributeValues2()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="ExpressionsInAttributeValues2">
    <file name="a.b">
Imports System
Imports System.Reflection
Public Class MyAttribute
    Public Sub New(p As Object)
    End Sub
End Class

&lt;MyAttribute(p:=Sub()
                        [|Static a As Integer = 1
                        While a &lt; 110
                            a += 1
                        End While|]
                   End Sub)&gt;
Module Program
    Sub Main(args As String())
    End Sub
End Module
    </file>
</compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LowerBoundOfArrayDefinitionSize()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="LowerBoundOfArrayDefinitionSize">
          <file name="a.b">
Class Test
    Public Shared Sub S(x As Integer)
        Static newTypeArguments([|0|] To x - 1) As String
    End Sub
End Class
</file>
      </compilation>)
            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug11440b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug11440">
          <file name="a.b">
Imports System
Module Program    
    Sub Main(args As String())
        GoTo Label
        Static arg2 As Integer = 2
Label:
        Static y = [| arg2 |]
    End Sub
End Module
          </file>
      </compilation>)

            Assert.True(analysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.Captured))
            Assert.Equal("arg2", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("arg2", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("args, arg2, y", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug12423a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12423a">
          <file name="a.b">
Class A    
    Sub Goo()
        Static x = { [| New B (abc) |] }
    End Sub
End Class
Class B
    Public Sub New(i As Integer)
    End Sub
End Class
          </file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug12423b()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12423b">
          <file name="a.b">
Class A    
    Sub Goo(i As Integer)
        Static x = New B([| i |] ) { New B (abc) }
    End Sub
End Class
Class B
    Public Sub New(i As Integer)
    End Sub
End Class
          </file>
      </compilation>)
            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowForValueTypes()

            ' WARNING: test matches the same test in C# (TestDataFlowForValueTypes)
            '          Keep the two tests in sync

            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowForValueTypes">
          <file name="a.b">
Imports System

Class Tst
    Shared Sub Tst()
        Static a As S0
        Static b As S1
        Static c As S2
        Static d As S3
        Static e As E0
        Static f As E1

[|
        Console.WriteLine(a)
        Console.WriteLine(b)
        Console.WriteLine(c)
        Console.WriteLine(d)
        Console.WriteLine(e)
        Console.WriteLine(f)
|]
    End Sub
End Class


Structure S0
End Structure

Structure S1
    Public s0 As S0
End Structure

Structure S2
    Public s0 As S0
    Public s1 As Integer
End Structure

Structure S3
    Public s0 As S0
    Public s1 As Object
End Structure

Enum E0
End Enum

Enum E1
    V1
End Enum
</file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.Captured))
            Assert.Equal("c, d, e, f", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("a, b, c, d, e, f", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug10987()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug10987">
          <file name="a.b">
Class Test
    Public Shared Sub Main()
        Static y(1, 2) = [|New Integer|]
    End Sub
End Class
</file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestExpressionInIfStatement()
            Dim dataFlowAnalysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestExpressionInIfStatement">
          <file name="a.b">
Module Program
    Sub Main()
        Static x = 1
        If 1 = [|x|] Then 
        End If
    End Sub
End Module
  </file>
      </compilation>)

            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CallingMethodsOnUninitializedStructs()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="CallingMethodsOnUninitializedStructs2">
          <file name="a.b">
Public Structure XXX
    Public x As S(Of Object)
    Public y As S(Of String)
End Structure

Public Structure S(Of T)
    Public x As String
    Public Property y As T
End Structure

Public Class Test
    Public Shared Sub Main(args As String())
        Static s As XXX
        s.x = New S(Of Object)()
        [|s.x.y.ToString()|]
        Static t As Object = s
    End Sub
    Public Shared Sub S1(ByRef arg As XXX)
        arg.x.x = ""
        arg.x.y = arg.x.x
    End Sub
End Class
        </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.Captured))
            Assert.Equal("s", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("s", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("args, s, t", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug10172()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="Bug10172">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Static list = New Integer() {1, 2, 3, 4, 5, 6, 7, 8}
        Static b = From i In list Where i > Function(i) As String
                                             [|Return i|]
                                         End Function.Invoke
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Item1.Succeeded)
            Assert.True(analysis.Item2.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug11526()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="Bug10172">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Static x = True
        Static y = DateTime.Now
        [|
        Try
        Catch ex as Exception when x orelse y = #12:00:00 AM#
        End Try
        |]
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Item1.Succeeded)
            Assert.True(analysis.Item2.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug10683a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug10683a">
          <file name="a.b">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Static x = New Integer() {}
        x.First([|Function(i As Integer, r As Integer) As Boolean
                    Return True
                End Function|])
    End Sub
End Module
</file>
      </compilation>)

            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestArrayDeclaration01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration01">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|
        Static x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestArrayDeclaration02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration02">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|If True Then Static x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestArrayDeclaration02_()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestArrayDeclaration02">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Static  b As Boolean = True
        [|If b Then Static x(5), y As Integer |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestVariablesWithSameName()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestVariablesWithSameName">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        [|If True Then Static x = 1 Else Static x = 1 |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestVariablesWithSameName2()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestVariablesWithSameName2">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Dim b As Boolean = false
        [|If b Then Static x = 1 Else Static x = 1 |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestVariablesDeclared01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclared01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        Static a as integer
[|
        Static b as integer
        Static x as integer, y = 1
        if true then
          Static z = "a" 
        end if
|]
        Static c as integer
    end sub
end class</file>
      </compilation>)
            Assert.Equal("b, x, y, z", GetSymbolNamesJoined(analysis.VariablesDeclared))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y, z
        [|
        If True
            x = 1
        ElseIf True
            y = 1
        Else
            z = 1
        End If
        |]
        Console.WriteLine(x + y + z)
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y, z
        Static b As Boolean = True
        [|
        If b
            x = 1
        ElseIf b
            y = 1
        Else
            z = 1
        End If
        |]
        Console.WriteLine(x + y + z)
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranchReachability01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability01">
          <file name="a.b">
Imports System
Module Program
    Function Goo() As Integer
        Static x, y
        If True Then x = 1 Else If True Then Return 1 Else [|Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(1, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.False(analysis.StartPointIsReachable())
            Assert.False(analysis.EndPointIsReachable())
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranchReachability02()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability02">
          <file name="a.b">
Imports System
Module Program
    Function Goo() As Integer
        Static x, y
        If True Then x = 1 Else [|If True Then Return 1 Else Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(2, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.False(analysis.StartPointIsReachable())
            Assert.False(analysis.EndPointIsReachable())
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranchReachability03()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability03">
          <file name="a.b">
Imports System
Module Program
    Function Goo() As Integer
        Static x, y
        [|If True Then x = 1 Else If True Then Return 1 Else Return 1|]
        Return x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(2, analysis.ExitPoints.Count())
            Assert.Equal(0, analysis.EntryPoints.Count())
            Assert.True(analysis.StartPointIsReachable())
            Assert.True(analysis.EndPointIsReachable())
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch01">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y
        [|If True Then x = 1 Else y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch01_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch01">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static b As Boolean = True
        Static x, y
        [|If b Then x = 1 Else y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch02">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y
        If True Then [|x = 1|] Else y = 1
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y, z
        If True Then x = 1 Else [|y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut)) '' else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch03_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else [|y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("b, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch04()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y, z
        If True Then x = 1 Else If True Then y = 1 Else [|z = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))  ''  else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch04_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else If b Then y = 1 Else [|z = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("z", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("b, z", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch05()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static x, y, z
        If True Then x = 1 Else [|If True Then y = 1 Else y = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch05_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Goo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else [|If b Then y = 1 Else y = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("b, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("b", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestVariablesInitializedWithSelfReference()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesInitializedWithSelfReference">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        Static x as integer = x
        Static y as integer, z as integer = 1
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, y, z", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("Me, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, x, z", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("x, z", GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestVariablesDeclared02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestVariablesDeclared02">
          <file name="a.b">
class C 
    public sub F(x as integer)
[|
        Static a as integer
        Static b as integer
        Static x as integer, y as integer = 1
        if true then
            Static z as string = "a"
        end if
        Static c as integer
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("a, b, x, y, z, c", GetSymbolNamesJoined(analysis.VariablesDeclared))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AlwaysAssignedUnreachable()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="AlwaysAssignedUnreachable">
          <file name="a.b">
class C 
    Public Sub F(x As Integer)
[|
        Static y As Integer
        If x = 1 Then
            y = 2
            Return
        Else
            y = 3
            Throw New Exception
        End If
        Static c As Integer
|]
    End Sub
end class</file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowLateCall()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowLateCall">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static o as object = 1
[|
        goo(o)
|]
    End Sub

    Sub goo(x As String)

    End Sub

    Sub goo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, o", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, o", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowLateCall001()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowLateCall001">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    shared Sub Main(args As String())
        Static o as object = 1
        Static oo as object = new Program
[|
        oo.goo(o)
|]
    End Sub

    Sub goo(x As String)

    End Sub

    Sub goo(Byref x As Integer)

    End Sub
End Class
</file>
      </compilation>)
            Assert.Equal("o, oo", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, o, oo", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, o, oo", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("o, oo", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("args, o, oo", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowIndex()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut01">
          <file name="a.b">
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static o as object = 1
[|
        Static oo = o(o)
|]
    End Sub

    Sub goo(x As String)

    End Sub

    Sub goo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, o", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, o, oo", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("o", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("oo", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub UnassignedVariableFlowsOut01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="UnassignedVariableFlowsOut01">
          <file name="a.b">
class C 
    public sub F()
        Static i as Integer = 10
[|
        Static j as Integer = j + i
|]
        Console.Write(i)
        Console.Write(j)
    end sub
end class</file>
      </compilation>)
            Assert.Equal("j", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("i, j", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("j", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, i", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, j", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("i, j", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("i, j", GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("j", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("Me, i", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsIn01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        Static a as integer = 1, y as integer = 2
[|
        Static b as integer = a + x + 3
|]
        Static c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, a", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("Me, x, a, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, a, y, b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsIn02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn02">
          <file name="a.b">
class Program
    sub Test(of T as class, new)(byref t as T) 

[|
        Static  t1 as T
        Test(t1)
        t = t1
|]
        System.Console.WriteLine(t1.ToString())
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("Me, t1", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("Me, t", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, t", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsIn03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsIn03">
          <file name="a.b">
class Program
    shared sub Main(args() as string)
        Static x as integer = 1
        Static y as integer = 2
[|
        Static z as integer = x + y
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("args, x, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x, y, z", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut01">
          <file name="a.b">
class C 
    public sub F(x as integer)
        Static a as integer = 1, y as integer
[|
        if x = 1 then
            x = 2
            y = x 
        end if
|]
        Static c as integer = a + 4 + x + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, x, a", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, a", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut02">
          <file name="a.b">
class Program
    public sub Test(args() as string)
[|
        Static  s as integer = 10, i as integer = 1
        Static b as integer = s + i
|]
        System.Console.WriteLine(s)
        System.Console.WriteLine(i)
    end sub
end class</file>
      </compilation>)
            Assert.Equal("s, i", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args, s, i, b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut03">
          <file name="a.b">
imports System.Text
module Program
    sub Main() as string
        Static builder as StringBuilder = new StringBuilder()
[|
        builder.Append("Hello")
        builder.Append("From")
        builder.Append("Roslyn")
|]
        return builder.ToString()
    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("builder", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("builder", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut06()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
              <compilation>
                  <file name="a.b"><![CDATA[
Class C
    Sub F(b As Boolean)
        Static i As Integer = 1
        While b
            [|i = i + 1|]
        End While
    End Sub
End Class

            ]]></file>
              </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, b, i", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, b, i", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, b, i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut07()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut07">
          <file name="a.b">
class Program
   sub F(b as boolean)
        Static  i as integer
        [|
        i = 2
        goto [next]
        |]
    [next]:
        Static j as integer = i
    end sub
end class</file>
      </compilation>)
            Assert.Equal("i", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, b", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut08()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut08">
          <file name="a.b">
Class Program
   Sub F()
        Static i As Integer = 2
        Try
            [|
            i = 1
            |]
        Finally
            Static j As Integer = i
        End Try
    End Sub
End Class
</file>
      </compilation>)
            Assert.Equal("i", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, i", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOut09()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsOut09">
          <file name="a.b">
class Program
    sub Test(args() as string)
        Static i as integer
        Static s as string

        [|i = 10
        s = args(0) + i.ToString()|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args, i, s", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDataFlowsOutExpression01()
            Dim analysis = CompileAndAnalyzeDataFlow(
<compilation name="TestDataFlowsOutExpression01">
    <file name="a.b">
class C 
    public sub F(x as integer)
        Static a as integer = 1, y as integer
        Static tmp as integer = x 
[|
            x = 2
            y = x
|]
            temp += (a = 2)
        Static c as integer = a + 4 + x + y
    end sub
end class</file>
</compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, x, a, tmp", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, a, y, tmp", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAlwaysAssigned01()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned01">
          <file name="a.b">
class C
    public sub F(x as integer)

        Static  a as integer = 1, y as integer= 1
[|
        if x = 2 then
            a = 3
        else 
            a = 4
        end if
        x = 4
        if x = 3 then
            y = 12
        end if
|]
        Static c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)
            Assert.Equal("x, a", GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAlwaysAssigned03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned03">
          <file name="a.b">
module C
    sub Main(args() as string)

        Static i as integer = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestWrittenInside02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestWrittenInside02">
          <file name="a.b">
module C
    sub Main(args() as string)

        Static i as integer = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestWrittenInside03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestWrittenInside03">
          <file name="a.b">
module C
    sub Main(args() as string)

        Static i as integer 
        i = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAlwaysAssigned04()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssigned04">
          <file name="a.b">
module C
    sub Main(args() as string)

        Static i as integer 
        i = [|
        int.Parse(args(0).ToString())
        |]

    end sub
end module</file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAlwaysAssignedDuplicateVariables()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAlwaysAssignedDuplicateVariables">
          <file name="a.b">
class C
    public sub F(x as integer)

[|
        Static a, a, b, b as integer
        b = 1
|]
    end sub
end class</file>
      </compilation>)
            Assert.Equal("b", GetSymbolNamesJoined(analysis.AlwaysAssigned))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAccessedInsideOutside()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestAccessedInsideOutside">
          <file name="a.b">
class C
    public sub F(x as integer)

        Static a, b, c, d, e, f, g, h, i as integer
        a = 1
        b = a + x
        c = a + x
[|
        d = c
        f = d
        e = d
|]
        g = e
        i = g
        h = g
    end sub
end class</file>
      </compilation>)
            Assert.Equal("c, d", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("d, e, f", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("x, a, e, g", GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("Me, x, a, b, c, g, h, i", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestAlwaysAssignedViaPassingAsByRefParameter()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
           <compilation>
               <file name="a.b"><![CDATA[
Class C
    Public Sub F(x As Integer)
[|        Static  a As Integer
        G(a)|]
    End Sub

    Sub G(ByRef x As Integer)
        x = 1
    End Sub
End Class

            ]]></file>
           </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, a", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDeclarationWithSelfReferenceAndTernaryOperator()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDeclarationWithSelfReferenceAndTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()

[|
        Static  x as integer = if(true, 1, x)
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
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestTernaryExpressionWithAssignments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestTernaryExpressionWithAssignments">
          <file name="a.b">
class C
    shared sub Main()
        Static x as boolean = true
        Static y as integer
[|
        Static z as integer 
        y = if(x, 1, 2)
        z = y
|]
        y.ToString()
    end sub
end class
            </file>
      </compilation>)
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal("z", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestBranchOfTernaryOperator()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestBranchOfTernaryOperator">
          <file name="a.b">
class C
    shared sub Main()
       Static x as boolean = true
       Static y as boolean = if(x,[|x|],true)
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDeclarationWithSelfReference()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestDeclarationWithSelfReference">
          <file name="a.b">
class C
    shared sub Main()
[|
        Static x as integer = x
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
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfStatementWithAssignments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithAssignments">
          <file name="a.b">
class C
    shared sub Main()
        Static x as boolean = true
        Static y as integer
[|
        if x then
            y = 1
        else 
            y = 2
        end if
|]
        y.ToString()
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
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfStatementWithConstantCondition()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithConstantCondition">
          <file name="a.b">
class C
    shared sub Main()
        Static x as boolean = true
        Static y as integer
[|
        if true then
            y = x
        end if
|]
        y.ToString()
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
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfStatementWithNonConstantCondition()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestIfStatementWithNonConstantCondition">
          <file name="a.b">
class C
    shared sub Main()
       Static x as boolean = true
       Static y as integer
[|
        if true or x then
            y = x
        end if
|]
        y.ToString()
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
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSingleVariableSelection()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestSingleVariableSelection">
          <file name="a.b">
class C
    shared sub Main()
       Static x as boolean = true
       Static y as boolean = x or [|
x |]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestParenthesizedExpressionSelection()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestParenthesizedExpressionSelection">
          <file name="a.b">
class C
    shared sub Main()
       Static x as boolean = true
       Static y as boolean = x or [|(x = x) |] orelse x
    end sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned)) ' In C# '=' is an assignment while in VB it is a comparison.
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut)) 'C# flows out because this is an assignment expression.  In VB this is an equality test.
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside)) 'C# this is an assignment. In VB, this is a comparison so no assignment.
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestRefArgumentSelection()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestRefArgumentSelection">
          <file name="a.b">
class C
    shared sub Main()
        Static x as integer = 0
[|
        Goo(
x 
)
|]
      System.Console.WriteLine(x)
    end sub

    shared sub Goo(byref x as integer)
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
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(541891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541891")>
        <Fact()>
        Public Sub TestRefArgumentSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As UInteger
        System.Console.WriteLine([|Goo(x)|])
    End Sub

    Function Goo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(541891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541891")>
        <Fact()>
        Public Sub TestRefArgumentSelection02a()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As UInteger
        System.Console.WriteLine(Goo([|x|]))
    End Sub

    Function Goo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAssignmentTargetSelection01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAssignmentTargetSelection01">
          <file name="a.b">
class C
     Sub Main()
        Static x As String = ""
        [|x|]+=1
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAssignmentTargetSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAssignmentTargetSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As String = ""
        [|x+=1|]
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAssignmentTargetSelection03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAssignmentTargetSelection03">
          <file name="a.b">
Imports System
Module M1
    Sub M(ParamArray ary As Long())
        Static local01 As Integer = 1
        Static local02 As Short = 2
[|
        local01 ^= local02
        Try
           local02 &lt;&lt;= ary(0) 
           ary(1) *= local01
           Static flocal As Single = 0
           flocal /= ary(0)
           ary(1) \= ary(0)
        Catch ex As Exception
        Finally
            Dim slocal = Nothing
            slocal &amp;= Nothing
        End Try
|]
    End Sub
End Module
            </file>
      </compilation>)

            Assert.Equal("flocal, ex, slocal", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("local01, slocal", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ary, local01, local02, flocal", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("local01, local02, flocal", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ary, local01, local02", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("ary, local01, local02, slocal", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("ary, local01, local02, flocal, slocal", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("local01, local02, flocal, ex, slocal", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("ary, local01, local02", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestRefArgumentSelection03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection03">
          <file name="a.b">
class C
     Sub Main()
        Static x As ULong

        System.Console.WriteLine([|Goo(x)|])
    End Sub

    Function Goo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestInvocation()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestInvocation">
          <file name="a.b">
class C
    shared sub Main()
        Static x as integer = 1, y as integer = 1
[|
        Goo(x)
|]
    end sub

    shared sub Goo(int x) 
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
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me beng read
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestInvocationWithAssignmentInArguments()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestInvocationWithAssignmentInArguments">
          <file name="a.b">
class C
    shared sub Main()
        Static x as integer = 1, y as integer = 1
[|
        x = y
        y = 2
        Goo(y, 2) ' VB does not support expression assignment F(x = y, y = 2)
|]
        Static z as integer = x + y
    }

    shared sub Goo(int x, int y)
    end sub
}
            </file>
      </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me being read
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestArrayInitializer()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C
    Sub Main(args As String())
        Static y As Integer = 1
        Static x(,) As Integer x = { { 
[|y|]
 } }
    End Sub
End Class

            ]]></file>
        </compilation>)

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, args, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ByRefParameterNotInAppropriateCollections1()
            ' ByRef parameters are not considered assigned
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Imports System
Imports System.Collections.Generic
Class Program
    Sub Test(of T)(ByRef t As T)
[|
        Static t1 As T
        Test(t1)
        t = t1
|]
        System.Console.WriteLine(t1.ToString())
    End Sub
End Class
            </file>
</compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal("t1", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ByRefParameterNotInAppropriateCollections2()
            ' ByRef parameters are not considered assigned
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssertFromInvalidKeywordAsExpr">
    <file name="a.b">
Imports System
Imports System.Collections.Generic
Class Program
    Sub Test(Of T)(ByRef t As T)
[|
        Static t1 As T = GetValue(of T)(t)
|]
        System.Console.WriteLine(t1.ToString())
    End Sub
    Private Function GetValue(Of T)(ByRef t As T) As T
        Return t
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
            Assert.Equal("t1", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t1", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, t", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, t", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BinaryAndAlso01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryAndAlso01">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Static x As Boolean = True
        Static y As Boolean = False
        Static z As Boolean = IF(Nothing, [|F(x)|]) AndAlso IF(Nothing, F(y)) AndAlso False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BinaryAndAlso02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryAndAlso02">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Static  x As Boolean
        Static y As Boolean = False
        Static z As Boolean = x AndAlso [|y|] AndAlso False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BinaryOrElse01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryOrElse01">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Static  x As Boolean = True
        Static y As Boolean = False
        Static z As Boolean = IF(Nothing, [|F(x)|]) OrElse IF(Nothing, F(y)) OrElse False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BinaryOrElse02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation name="BinaryOrElse02">
    <file name="a.b">
Class A
    Function F(ByRef p As Boolean) As Boolean
        Return Nothing
    End Function
    Sub Test1()
        Static  x As Boolean
        Static y As Boolean = False
        Static z As Boolean = x OrElse [|y|] OrElse False
    End Sub
End Class
            </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestMultipleLocalsInitializedByAsNew1()
            Dim dataFlowAnalysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestMultipleLocalsInitializedByAsNew">
          <file name="a.b">
Module Program
    Class c
        Sub New(i As Integer)
        End Sub
    End Class

    Sub Main(args As String())
        Static a As Integer = 1
        Static x, y, z As New c([|a|]+1)
    End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("args, a, x, y, z", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestMultipleLocalsInitializedByAsNew2()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestMultipleLocalsInitializedByAsNew">
          <file name="a.b">
Module Program
    Class c
        Sub New(i As Integer)
        End Sub
    End Class

    Sub Main(args As String())
        Static a As Integer = 1
        [|Static  x, y, z As New c(a)|]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowAnalysis = analysis.Item1
            Dim dataFlowAnalysis = analysis.Item2
            Assert.Equal(0, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("args, a", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.True(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestElementAccess01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestElementAccess">
          <file name="elem.b">
Imports System

Public Class Test
    Sub F(p as Long())
        Static v() As Long =  new Long() { 1, 2, 3 }
        [|
        v(0) = p(0)
        p(0) = v(1)
        |]
        v(1) = v(0)
        ' p(2) = p(0)
    End Sub
End Class
  </file>
      </compilation>)

            Dim dataFlowAnalysis = analysis.Item2
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, v", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("Me, p, v", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, p, v", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnExit))
            Assert.Equal("p, v", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("v", GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, v", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub DataFlowForDeclarationOfEnumTypedVariable()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C
    Sub Main(args As String())
        [|Static s As color|]
        Try
        Catch ex As Exception When s = color.black
            Console.Write("Exception")
        End Try
        End Sub
End Class 

Enum color
    black
End Enum
]]></file>
        </compilation>)

            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, args", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, args, ex", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameInMemberAccessExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Public Class Goo
    Sub M()
        Static c As C = New C()
        Static n1 = c.[|M|]
  End Sub
End Class
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameInMemberAccessExpr2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Public Class C
    Sub M()
        Static c As C = New C()
        Static n1 = c.[|M|]
        End Sub
End Class
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameSyntax()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Static n1 = [|ChrW|](85)
    End Sub
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameSyntax2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Static  n1 = [|Goo|](85)
        End Sub
    Function Goo(i As Integer) As Integer
        Return i
    End Function
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameSyntax3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports Microsoft.VisualBasic
Public Class C
    Sub M()
        Static n1 = [|Goo|](85)
  End Sub
    ReadOnly Property Goo(i As Integer) As Integer
        Get
            Return i
        End Get
    End Property
End Class
  </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub PredefinedTypeIncompleteSub()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
    Friend Module AcceptVB7_12mod
        Sub AcceptVB7_12()
                Static lng As [|Integer|]
                Static int1 As Short
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub PredefinedType2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
    Friend Module AcceptVB7_12mod
        Sub AcceptVB7_12()
                Static lng As [|Integer|]
                Static int1 As Short
  End Sub
    And Module
  </file>
   </compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitSyntax()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Static i1 = New Integer() {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}", StringComparison.Ordinal)).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitSyntax2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.Collections.Generic
Module Program
    Sub Main(args As String())
        Static i1 = New List(Of Integer) From {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}", StringComparison.Ordinal)).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitSyntax3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Imports System.Collections.Generic
Module Program
    Sub Main(args As String())
        Static i1 = {4, 5}
  End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}", StringComparison.Ordinal)).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IfStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Static x = 10
        If False
            x = x + 1
        End If
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("If False", StringComparison.Ordinal)).Parent, IfStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ElseStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main(args As String())
        Static x = 10
        If False
            x = x + 1
        Else 
            x = x - 1
        End If
    End Sub
End Module
  </file>
   </compilation>)

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Else", StringComparison.Ordinal)).Parent, ElseStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        ReadOnly Property STForEach01 As Integer
            Get
                Return 1
            End Get
        End Property 
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        Static a As Integer = [|STForEach01|].STForEach01
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess4()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Namespace STForEach01
    Friend Module STForEach01mod
        ReadOnly Property STForEach01 As Integer
            Get
                Return 1
            End Get
        End Property 
    End Module
End Namespace

Friend Module MainModule
    Sub Main()
        Static  a As Integer = [|STForEach01.STForEach01mod|].STForEach01
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess5()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Static d1 = Sub(x As Integer)
                     [|System|].Console.WriteLine(x)
                 End Sub
    End Sub
End Module
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess9()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class Compilation
    Public Class B
        Public Shared Function M(a As Integer) As Boolean
            Return False
        End Function
    End Class
End Class

Friend Class Program
    Public Shared Sub Main()
        Static x = [| Compilation |].B.M(a:=123)
    End Sub
    Public ReadOnly Property Compilation As Compilation
        Get
            Return Nothing
        End Get
    End Property
End Class

    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NamespaceIdentifierNameInMemberAccess10()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Public Class Compilation
    Public Shared Function M(a As Integer) As Boolean
        Return False
    End Function
End Class

Friend Class Program
    Public Shared Sub Main()
        Static  x = [| Compilation |].M(a:=123)
    End Sub
    Public ReadOnly Property Compilation As Compilation
        Get
            Return Nothing
        End Get
    End Property
End Class

    </file>
</compilation>)
            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ConstLocalUsedInLambda01()
            Dim analysisResult = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        Static  local = 1
        Const constLocal = 2
        Static f = [| Function(p as sbyte) As Short
                    Return local + constlocal + p
                End Function |]
        Console.Write(f)
    End Sub
End Module
    </file>
</compilation>)

            Assert.Equal("p", GetSymbolNamesJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesJoined(analysisResult.Captured))
            Assert.Equal("local, constLocal", GetSymbolNamesJoined(analysisResult.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisResult.DataFlowsOut))
            Assert.Equal("local, constLocal, f", GetSymbolNamesJoined(analysisResult.DefinitelyAssignedOnEntry))
            Assert.Equal("local, constLocal, f", GetSymbolNamesJoined(analysisResult.DefinitelyAssignedOnExit))
            Assert.Equal("local, constLocal, p", GetSymbolNamesJoined(analysisResult.ReadInside))
            ' WHY
            Assert.Equal("p", GetSymbolNamesJoined(analysisResult.WrittenInside))
            Assert.Equal("f", GetSymbolNamesJoined(analysisResult.ReadOutside))
            Assert.Equal("local, constLocal, f", GetSymbolNamesJoined(analysisResult.WrittenOutside))

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ConstLocalUsedInLambda02()
            Dim analysisResult = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C
    Function F(mp As Short) As Integer
        Try
            Static  local = 1
            Const constLocal = 2
            Static lf = [| Sub()
                         local = constlocal + mp
                     End Sub |]
            lf()
            Return local
        Finally
        End Try
    End Function
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("mp, local", GetSymbolNamesJoined(analysisResult.Captured))
            Assert.Equal("mp, constLocal", GetSymbolNamesJoined(analysisResult.DataFlowsIn))
            Assert.Equal("local", GetSymbolNamesJoined(analysisResult.DataFlowsOut))
            Assert.Equal("Me, mp, local, constLocal, lf", GetSymbolNamesJoined(analysisResult.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, mp, local, constLocal, lf", GetSymbolNamesJoined(analysisResult.DefinitelyAssignedOnExit))
            Assert.Equal("mp, constLocal", GetSymbolNamesJoined(analysisResult.ReadInside))
            Assert.Equal("local", GetSymbolNamesJoined(analysisResult.WrittenInside))
            Assert.Equal("local, lf", GetSymbolNamesJoined(analysisResult.ReadOutside))
            Assert.Equal("Me, mp, local, constLocal, lf", GetSymbolNamesJoined(analysisResult.WrittenOutside))

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LiteralExprInVarDeclInsideSingleLineLambda()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Test
    Sub Sub1()
        Static x = Sub() Dim y = [|10|]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectCreationExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Static x As [|New C|]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

#Region "ObjectInitializer"
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersNoStaticLocalsAccessed()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Class C1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Class

Public Class C2
    Public Shared Sub Main()
        Static intlocal As Integer
        Static x = New C1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_OnlyImplicitReceiverRegion1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static x, y As New S1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_OnlyImplicitReceiverRegion2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static x, y As New S1() With {.FieldInt = [|.FieldStr.Length|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_DeclAndImplicitReceiverRegion()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        [| Static x, y As New S1() With {.FieldInt = .FieldStr.Length} |]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_ValidRegion1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Static x, y As New S1() With {.FieldInt = !A.Length }
        x.FieldInt = [| x!A.Length |]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_ValidRegion2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Static x, y As New S1() With {.FieldInt = [| x!A.Length |] }
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1_InvalidRegion3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Default Public Property PropInt(i As String) As String
        Get
            Return 0
        End Get
        Set(value As String)
        End Set
    End Property
End Structure

Public Class S2
    Public Shared Sub Main()
        Static x, y As New S1() With {.FieldStr = [| !A |] }
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1a_ObjectInitializer()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static o As New S1()
        With o
            [|Console.WriteLine(New S1() With {.FieldStr = .FieldInt.ToString()})|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1a_ObjectInitializer2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static o As New S1()
        With o
            Console.WriteLine(New S1() With {.FieldStr = [|.FieldInt.ToString()|] })
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1b()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static o As New S1()
        With o
            [|Console.WriteLine(New List(Of String) From {.FieldStr, "Brian", "Tim"})|]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersStaticLocalsAccessed1bb()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static o As New S1()
        [|Console.WriteLine(New List(Of String) From {o.FieldStr, "Brian", "Tim"})|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda1()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub TEST()
        Static a, b As New SS2() With {.X = Function() As SS1
                                          With .Y
                                              [| .A = "1" |]
                                              '.B = "2"
                                          End With
                                          Return .Y
                                      End Function.Invoke()}
    End Sub
End Structure
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub TEST()
        Static a, b As New SS2() With {.X = Function() As SS1
                                          With .Y
                                              [| 
                                                b.Y.B = a.Y.A
                                                a.Y.A = "1" 
                                              |]
                                          End With
                                          Return .Y
                                      End Function.Invoke()}
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda3()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
        Static l = Sub()
                    Dim a, b As New SS2() With {.X = Function() As SS1
                                                      With .Y
                                                          [| 
                                                            b.Y.B = a.Y.A
                                                            a.Y.A = "1" 
                                                          |]
                                                      End With
                                                      Return .Y
                                                  End Function.Invoke()}
                End Sub
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i, l, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("i, l, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("a", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, i, l, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda4()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Static  a, b As New SS2() With {.X = Function() As SS1
                                                [| a.Y = New SS1()
                                                   b.Y = New SS1() |]
                                                Return .Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, i, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda5()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Static a, b As New SS2() With {.X = Function() As SS1
                                                [| b.Y = New SS1() |]
                                                Return a.Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, a", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, i, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWithFieldAccessesInLambda6()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Sub New(i As Integer)
            Static a, b As New SS2() With {.X = Function() As SS1
                                                [| b.Y = New SS1() |]
                                                Return b.Y
                                             End Function.Invoke()}

            Console.WriteLine(a.ToString())
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, i, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_PassingFieldByRef()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Function Transform(ByRef p As SS1) As SS1
        Return p
    End Function

    Sub New(i As Integer)
        Static a, b As New SS2() With {.X = [| Transform(b.Y) |] }
    End Sub
End Structure
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("i, a", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, i, a, b", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersLocalsAccessed2()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public FieldInt As Long
    Public FieldStr As String

    Public Property PropInt As Integer
End Structure

Public Class S2
    Public Shared Sub Main()
        Static  x As New S1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersWithLocalsAccessed()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Public Class C1
    Public FieldStr As String
End Class

Public Class C2
    Public Shared Function GetStr(p as string)
        return p    
    end Function

    Public Shared Sub Main()
        Static  strlocal As String
        Static x = New C1() With {.FieldStr = [|GetStr(strLocal)|]}
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("strlocal", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("strlocal", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersWithLocalCaptured()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
    Public Field2 As Func(Of Integer)
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Static  localint as integer = 23
        Static x As New C1 With {.Field2 = [|Function() As Integer
                                            Return localint
                                        End Function|]}
        x.Field = 42
        Console.WriteLine(x.Field2.Invoke())
    End Sub
End Class 

    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("localint, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializersWholeStatement()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
    Public Field2 As Func(Of Integer)
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Static localint as integer
        [|Static x As New C1 With {.Field2 = Function() As Integer
                                            localInt = 23
                                            Return localint
                                        End Function}|]
        x.Field = 42
        Console.WriteLine(x.Field2.Invoke())
    End Sub
End Class 

    </file>
</compilation>)

            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("localint, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))

            Dim controlFlowAnalysisResults = analysisResults.Item1
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count)
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count)
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable)
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable)
        End Sub

#End Region

#Region "CollectionInitializer"

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitializersCompleteObjectCreationExpression()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        Static goo as string = "Hello World"
        Static x as [|New List(Of String) From {goo, "!"}|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("goo, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitializersOutermostInitializerAreNoVBExpressions()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        Static  goo as string = "Hello World"
        Static x as New List(Of String) From [|{goo, "!"}|]
    End Sub
End Class
    </file>
</compilation>)
            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitializersTopLevelInitializerAreNoVBExpressions()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        Static goo as string = "Hello World"
        Static x as New Dictionary(Of String, Integer) From {[|{goo, 1}|], {"bar", 42}}
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowAnalysisResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitializersLiftedLocals()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main()
        Static goo As String = "Hello World"
        Static x As [|New List(Of Action) From {
            Sub()
                Console.WriteLine(goo)
            End Sub,
            Sub()
                Console.WriteLine(x.Item(0))
                x = nothing
            End Sub
        }|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("goo, x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("goo", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("goo, x", GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("goo, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("goo, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitUndeclaredIdentifier()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static f1() As String = {[|X|]}

    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowAnalysisResults.Succeeded)
        End Sub

#End Region

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub UserDefinedOperatorBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            [| Return New B2(x) |]
        End Operator
    End Class

    Sub Main()
        Static x As Integer = 11
        Static b2 As B2 = x
    End Sub
End Module
    </file>
</compilation>)

            Dim ctrlFlowResults = analysisResults.Item1
            Assert.True(ctrlFlowResults.Succeeded)
            Assert.Equal(1, ctrlFlowResults.ExitPoints.Count())
            Assert.Equal(0, ctrlFlowResults.EntryPoints.Count())
            Assert.True(ctrlFlowResults.StartPointIsReachable)
            Assert.False(ctrlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysisResults.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty((dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub UserDefinedOperatorInExpression()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Public f As Integer
        Public Sub New(x As Integer)
            f = x
        End Sub
        Shared Operator -(x As Integer, y As B2) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main(args As String())
        Static x As Short = 123
        Static bb = New B2(x)
        Static ret = [| Function(y)
                      Return args.Length - (y - (x - bb))
                  End Function |]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("args, x, bb", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("args, x, bb", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("args, x, bb, ret", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x, bb, ret", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("args, x, bb, y", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, x, bb, ret", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub UserDefinedLiftedOperatorInExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Class A
    Structure S
        Shared Narrowing Operator CType(x As S?) As Integer
            System.Console.WriteLine("Operator Conv")
            Return 123 'Nothing
        End Operator

        Shared Operator *(x As S?, y As Integer?) As Integer?
            System.Console.WriteLine("Operator *")
            Return y
        End Operator
    End Structure
End Class

Module Program
     Sub M(Optional p As Integer? = Nothing)
        Static local As A.S? = New A.S() 
        Static f As Func(Of A.S, Integer?) = [| Function(x)
                                              Return x * local * p
                                          End Function |]
        Console.Write(f(local))
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("p, local", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("f", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("f", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("p, local, x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("local, f", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("p, local, f", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub DataFlowsInAndNullable()
            ' WARNING: if this test is edited, the test with the 
            '          test with the same name in C# must be modified too
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public F As Integer
    Public Sub New(_f As Integer)
        Me.F = _f
    End Sub
End Structure

Module Program
    Sub Main(args As String())
        Static i As Integer? = 1
        Static s As New S(1)
        [|
        Console.Write(i.Value)
        Console.Write(s.F)
        |]
    End Sub
End Module
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.Captured)
            Assert.Equal("i, s", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("i, s", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args, i, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestWithEventsInitializer()
            Dim comp = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    WithEvents e As C1 = [|Me|]
End Class
    </file>
</compilation>)
            Debug.Assert(comp.Succeeded)
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestDataFlowsInAndOut">
          <file name="a.b">
class Program
    shared sub Main(args() as string)
        Static x as integer
        Static y as integer = 2
[|
        If x = y Then
            x = 2
            y = 3
        End If
|]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut2()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
class Program
    shared sub Main(args() as string)
        Static x as integer = 1
        Static y as integer = 1

    [| 
        y = x
        x = 2 
    |]

        x = 3
        y = 3
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, x, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut3()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
class Program
    shared sub Main(args() as string)
        Static x as integer = 1

        if x = 1 then
    [|      x = 2       |]
            x = 3
        end if
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut4()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
class Program
    shared sub Main(args() as string)
    [|
        Static x as integer = 1
        Console.WriteLine(x)
    |]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut5()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
class Program
    shared sub Main(args() as string)
        Static x as integer = 1
        dim y = x
    [|
        x = 1
    |]
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, x, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub TestDataFlowsInAndOut6()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation>
          <file name="a.b">
Imports System
class Program
    shared sub Main(args() as string)
        Static x as integer = 1
    [|
        x = 1
    |]
        dim y = x
    end sub
end class
            </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("args, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
        End Sub

#Region "Anonymous Type, Lambda"
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCaptured()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestLifted">
          <file name="a.b">
class C
    Dim field = 123
    public sub F(x as integer)

        Static a as integer = 1, y as integer = 1
[|
        Static l1 = function() x+y+field
|]
        Static c as integer = a + 4 + y
    end sub
end class</file>
      </compilation>)

            Assert.Equal("l1", GetSymbolNamesJoined(analysis.VariablesDeclared))
            Assert.Equal("l1", GetSymbolNamesJoined(analysis.AlwaysAssigned))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(analysis.Captured))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, x, a, y", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, a, y, l1", GetSymbolNamesJoined(analysis.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x, y", GetSymbolNamesJoined(analysis.ReadInside))
            Assert.Equal("l1", GetSymbolNamesJoined(analysis.WrittenInside))
            Assert.Equal("a, y", GetSymbolNamesJoined(analysis.ReadOutside))
            Assert.Equal("Me, x, a, y, c", GetSymbolNamesJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Static  f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                                  [| Return lambdaParam + 1 |]
                                              End Function
    End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda2()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda2">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Static f1 As Object = Function(lambdaParam As Integer)
                               [| Return lambdaParam + 1 |]
                           End Function
        End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestRegionControlFlowAnalysisInsideLambda3()
            Dim controlFlowAnalysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestRegionControlFlowAnalysisInsideLambda3">
          <file name="a.b">
Imports System
Module Module1
    Sub Main()
        Static f1 As Object = Nothing 
        f1 = Function(lambdaParam As Integer)
                 [| Return lambdaParam + 1 |]
             End Function
        End Sub
End Module
  </file>
      </compilation>)
            Assert.Equal(1, controlFlowAnalysis.ExitPoints.Count())
            Assert.Equal(0, controlFlowAnalysis.EntryPoints.Count())
            Assert.True(controlFlowAnalysis.StartPointIsReachable)
            Assert.False(controlFlowAnalysis.EndPointIsReachable)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub DoLoopInLambdaBody()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
<compilation name="DoLoopWithContinue">
    <file name="a.b">
Class A
    Function Test1() As Integer
        Static  x As Integer = 5
        Console.Write(x)
        Static x as System.Action(of Integer) = Sub(i)
[|
            Do
                Console.Write(i)
                i = i + 1
                Continue Do
                'Blah
            Loop Until i > 5 |]   
        end sub     
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
            Assert.Equal("Me, x, x, i", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, x, i", GetSymbolNamesJoined(dataFlowAnalysisResults.DefinitelyAssignedOnExit))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x, x, i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAsLambdaLocal()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Option Infer On
Imports System

Public Class Test
  delegate R Func(OfT, R)(ref T t);
    Public Shared Sub Main()
        Dim local(3) As String
[|
        Static lambda As Func(Of Integer, Integer) =
                  Function(ByRef p As Integer) As Integer
                      p = p * 2
                      Dim at = New With {New C(Of Integer)().F, C(Of String).SF, .L = local.Length + p}
                      Console.Write("{0}, {1}, {2}", at.F, at.SF)
                      Return at.L
                  End Function
|]
    End Sub

    Class C(Of T)
        Public Function F() As T
            Return Nothing
        End Function
        Shared Public Function SF() As T
            Return Nothing
        End Function
    End Class

End Class
    </file>
   </compilation>)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2
            Assert.True(controlFlowResults.Succeeded)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("lambda", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("lambda, p, at", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("local", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("local", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("local, lambda", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("local, p, at", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("lambda, p, at", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("local", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAsNewInLocalContext()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IGoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Class CGoo
    Implements IGoo
End Class

Friend Module AM
    Sub Main(args As String())
        Static igoo As IGoo = New CGoo()
        Static at1 As New With {.if = igoo}
[|
        Static at2 As New With {.if = at1, igoo,
            .friend = New With {Key args, .lambda = DirectCast(Sub(ByRef p As Char)
                                                                   args(0) = p &amp; p
                                                                   p = "Q"c
                                                               End Sub, IGoo.DS)}}
|]
     Console.Write(args(0))
    End Sub
End Module
    </file>
   </compilation>)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2
            Assert.True(controlFlowResults.Succeeded)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("at2", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("at2, p", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("args, igoo, at1", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, igoo, at1", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("args, igoo, at1, at2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("args, igoo, at1, p", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("at2, p", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, igoo", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, igoo, at1", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAsExpression()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IGoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Friend Module AM
    Sub Main(args As String())

       Static at1 As New With {.friend = New With {args, Key.lambda = DirectCast(Sub(ByRef p As Char)
                                                                                  args(0) = p &amp; p
                                                                                  p = "Q"c
                                                                              End Sub, IGoo.DS) }
                          }
       Dim at2 As New With { Key .a= at1, .friend = New With { [| at1 |] }}
       Console.Write(args(0))

    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("at1", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, at1", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("args, at1", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("at1", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, at1, p", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, p, at2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAccessInstanceMember()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.vb">
Imports System

Class AM

    Dim field = 123
    Sub M(args As String())

       Static at1 As New With {.friend = [| New With {args, Key.lambda = Sub(ByRef ary As Char())
                                                                       Field = ary.Length
                                                                   End Sub } |]
                          }
    End Sub
End Class
    </file>
        </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("ary", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me, args", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("ary", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, args", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, args, ary", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("ary", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, args, at1", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeFieldInitializerWithLeftOmitted()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
        <compilation>
            <file name="a.vb">
Imports System

Class AM

    Dim field = 123
    Sub M(args As String())
       Static var1 As New AM
       Static at1 As New With { var1, .friend = [| .var1 |] }
    End Sub
End Class
    </file>
        </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Null(GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, args, var1", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, args, var1", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Null(GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("var1", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, args, var1, at1", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeUsingMe()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Class Base
    Protected Function F1() As Long
        Return 123
    End Function
    Friend Overridable Function F2(n As Integer) As Integer
        Return 456
    End Function
End Class

Class Derived
    Inherits Base
    Friend Overrides Function F2(n As Integer) As Integer
        Return 789
    End Function

    Sub M()
        Dim func = Function(x)
                       Dim at = [| New With {.dim = New With {Key .nested = Me.F2(x * x)}} |]
                       Return at.dim.nested
                   End Function
    End Sub
End Class
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, func, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, func, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, func, x, at", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAccessMyBase()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Class Base
    Protected Overridable Function F1() As Long
        Return 123
    End Function
End Class

Class Derived
    Inherits Base
    Protected Overrides Function F1() As Long
        Return 789
    End Function

    Sub M()

        Static  func = Function(x)
                       Dim at = [| New With {Key .dim = New With {MyBase.F1()}} |]
                       Return at.dim.F1
                   End Function
    End Sub
End Class
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, func, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, func, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, func, x, at", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAccessMyClass()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F_"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Overrides Function F() As String
            Return "B2::F_"
        End Function

        Public Sub TestMMM()
            Static  an = [| New With {.an = Function(s) As String
                                         Return s + Me.F() + MyBase.F() + MyClass.F()
                                     End Function
                              } |]
            Console.WriteLine(an.an("R="))
    End Sub

    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F_"
    End Function
End Class

    Public Sub Main()
        Call (New D()).TestMMM()
    End Sub

End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, s", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("an", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, an", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AddressOfExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Module Program
    Sub Main()
        Static x5 = Function() AddressOf [|Main|]
    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub XmlEmbeddedExpression()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
      <compilation>
          <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Module M
    Function F() As Object
        Static v0 = "v0"
        Static v1 = XName.Get("v1", "")
        Static v2 = XName.Get("v2", "")
        Static v3 = "v3"
        Static v4 = New XAttribute(XName.Get("v4", ""), "v4")
        Static v5 = "v5"
        Return <?xml version="1.0"?><<%= v1 %> <%= v2 %>="v2" v3=<%= v3 %> <%= v4 %>><%= v5 %></>
    End Function
End Module
    ]]></file>
      </compilation>, references:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return", StringComparison.Ordinal)).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("v0, v1, v2, v3, v4, v5", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnExit))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("v0, v1, v2, v3, v4, v5", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub XmlMemberAccess()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
      <compilation>
          <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Xml.Linq
Module M
    Function F() As Object
        Static x = <a><b><c d="e"/></b></a>
        Return x.<b>...<c>.@<d>
    End Function
End Module
    ]]></file>
      </compilation>, references:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return", StringComparison.Ordinal)).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub GenericStructureCycle()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Structure S(Of T)
    Public F As S(Of S(Of T))
End Structure
Module M
    Sub M()
        Static o As S(Of Object)
    End Sub
End Module
    ]]></file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Static", StringComparison.Ordinal)).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnEntry))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

#End Region

#Region "With Statement"
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_RValue_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With [| New SSS(Me.ToString(), i) |]
            Static s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With [| x |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.False(dataFlowResults.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With [| x |]
            .A = ""
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_2_()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With [| x |]
            .A = ""
            Dim a = .A
            Dim b = .B
            .B = 1
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, x, a, b", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_2a()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With x 
            [| .A = "" |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_2b()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With x 
            [| .B = "" |]
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, x", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_3()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSS
    Public A As String
    Public B As Integer

    Public Sub New(_a As String, _b As Integer)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        Static x As New SSS(Me.ToString(), i)
        With [| x |]
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, i, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, i, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, i", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, i, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Static x As New SSS()
        With [| x.S |]
            Dim s As Action = Sub()
                                .A = ""
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4a()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Static x As New SSS()
        With  [| x |] .S 
            Dim s As Action = Sub()
                                 .A = "" 
                              End Sub
        End With
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4b()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Static x As New SSS()
        With  x.S 
            Dim s As Action = Sub()
                                [| .A = "" |]
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4c()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

Structure SSSS
    Public A As String
    Public B As Integer
End Structure

Structure SSS
    Public S As SSSS
nd Structure

Class Clazz
    Sub TEST()
        Static x As New SSS()
        With  x.S 
            Dim s As Action = Sub()
                                [| .A |] = "" 
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40684: The test hook is blocked by this issue.
        <WorkItem(40684, "https://github.com/dotnet/roslyn/issues/40684")>
        Public Sub WithStatement_Expression_LValue_4d()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
        Static x As New SSS()
        With x.S 
            With .S2
                With .S3
                    Static s As Action = Sub()
                                        [| .A  = "" |]
                                      End Sub
                End With
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40684: The test hook is blocked by this issue.
        <WorkItem(40684, "https://github.com/dotnet/roslyn/issues/40684")>
        Public Sub WithStatement_Expression_LValue_4e()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
        Static x As New SSS()
        With x.S 
            With .S2
                With .S3
                    Static s As Action = Sub()
                                        Dim xyz = [| .A |]
                                      End Sub
                End With
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s, xyz", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4f()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
        Static x As New SSS()
        With [| x.S.S2 |].S3
            Static  s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_Expression_LValue_4g()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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

Class SSS
    Public S As SSSS
End Class

Class Clazz
    Sub TEST()
        Static x As New SSS()
        With [| x.S.S2 |].S3
            Static s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, x, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_MeReference_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
    Public x As New SSS()
    Sub TEST()
        With [| x.S.S2 |].S3
            Static s As Action = Sub()
                                .A = ""
                              End Sub
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me, s", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_ComplexExpression_1()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
    Public x As New SSS()
    Sub TEST()
        With DirectCast(Function()
                            Return [| Me.x |]
                        End Function, Func(Of SSS))()
            With .S.S2
                Static a = .S3.A
            End With
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, a", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WithStatement_ComplexExpression_2()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
<compilation>
    <file name="a.vb">
Imports System

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
    Public x As New SSS()
    Sub TEST()
        Static arr(,) As SSS

        With arr(1,
                 [| DirectCast(Function()
                            Return x 
                        End Function, Func(Of SSS)) |] ().S.S2.S3.B).S
            Static a = .S2.S3.A
        End With
        x.ToString()
    End Sub
End Class
    </file>
</compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, arr", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, a", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

#End Region

#Region "Select Statement"
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_Empty()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Empty">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj As Object = 0

        [|
        Select Case obj
        End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_SingleCaseBlock_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_SingleCaseBlock_01">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

        [|
            Select Case obj1
                Case obj2
                    Static obj4 = 1
                    obj3 = obj4
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_SingleCaseBlock_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_SingleCaseBlock_02">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

            Select Case obj1
                Case obj2
            [|
                    Static obj4 = 1
                    obj3 = obj4
            |]
            End Select
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_01">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object
        Static obj4 As Object

        [|
            Select Case obj1
                Case obj2
                    Static obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
                Case Else
                    Static obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            End Select
        |]

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_01_CaseElseRegion()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_01_CaseElseRegion">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object
        Static obj4 As Object

            Select Case obj1
                Case obj2
                    Static obj5 = 1
                    obj3 = obj5
                    obj4 = obj5

                Case Else
            [|
                    Static obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            |]
            End Select

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithCaseElse_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithCaseElse_02">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

        [|
            Select Case obj1
                Case obj2
                    Static obj4 = 1
                    obj3 = obj4
                Case Else
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlockWithCaseElse_03()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlockWithCaseElse_03">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

        [|
            Select Case obj1
                Case obj2
                  LabelCase:
                    Static obj4 = 1
                    obj3 = obj4
                Case Else
                    Goto LabelCase
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlocksWithoutCaseElse_01()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlocksWithoutCaseElse_01">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object
        Static obj4 As Object

        [|
            Select Case obj1
                Case obj2
                    Static obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
                Case obj3
                    Static obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            End Select
        |]

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj3, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseBlockWithoutCaseElse_02()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_CaseBlockWithoutCaseElse_02">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

        [|
            Select Case obj1
                Case obj2
                  LabelCase:
                    Static obj4 = 1
                    obj3 = obj4
                Case obj3
                    Goto LabelCase
            End Select
        |]
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj3", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_CaseStatementRegion()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestSelectCase_CaseStatementRegion">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object

        Select Case obj1
            Case [|obj2|]
                obj3 = 0
        End Select
    End Sub
End Module
  </file>
      </compilation>)
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj2", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestSelectCase_Error_CaseElseBeforeCaseBlock()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Error_CaseElseBeforeCaseBlock">
          <file name="a.b">
Module Program
    Sub Main()
        Static obj1 As Object = 0
        Static obj2 As Object = 0
        Static obj3 As Object
        Static obj4 As Object

            Select Case obj1
                Case Else
            [|
                    Static obj5 = 2
                    obj2 = obj5
                    obj4 = obj5
            |]
                Case obj2
                    Static obj5 = 1
                    obj3 = obj5
                    obj4 = obj5
            End Select

        obj1 = obj3 + obj4
    End Sub
End Module
  </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj4", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("obj1, obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("obj5", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(529089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529089")>
        <Fact>
        Public Sub CaseClauseNotReachable()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
      <compilation name="TestSelectCase_Error_CaseElseBeforeCaseBlock">
          <file name="a.b">
Module Program
    Sub Main(args As String())
        Static x = 10
        Select Case 5
            Case 10
                [|x = x + 1|]
        End Select
    End Sub
End Module
      </file>
      </compilation>)
            Dim controlFlowResults = analysis.Item1
            Assert.True(controlFlowResults.Succeeded)
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())
            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.True(controlFlowResults.StartPointIsReachable)
            Assert.True(controlFlowResults.EndPointIsReachable)

            Dim dataFlowResults = analysis.Item2
            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnEntry))
            Assert.Equal("args, x", GetSymbolNamesJoined(dataFlowResults.DefinitelyAssignedOnExit))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, x", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub MyBaseExpressionSyntax()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Public Class BaseClass
    Public Overridable Sub MyMeth()
    End Sub
End Class

Public Class MyClass : Inherits BaseClass
    Public Overrides Sub MyMeth()
        MyBase.MyMeth()
    End Sub
    Public Sub OtherMeth()
        Static f = Function() MyBase
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim invocation = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim flowAnalysis = model.AnalyzeDataFlow(invocation)
            Assert.Empty(flowAnalysis.Captured)
            Assert.Equal("Me As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("Me As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("Me As [MyClass]", flowAnalysis.WrittenOutside.Single().ToTestDisplayString())

            Dim lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()
            flowAnalysis = model.AnalyzeDataFlow(lambda)
            Assert.Equal("Me As [MyClass]", flowAnalysis.Captured.Single().ToTestDisplayString())
            Assert.Equal("Me As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("Me As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("Me, f", GetSymbolNamesJoined(flowAnalysis.WrittenOutside))
        End Sub
#End Region
    End Class
End Namespace
