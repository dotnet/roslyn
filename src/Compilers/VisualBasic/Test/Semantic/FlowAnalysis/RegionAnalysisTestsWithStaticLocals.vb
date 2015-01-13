' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        'Mulitple calls are NOT required to verify the flow analysis for static locals.

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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("arg2", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("arg2", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("arg2, args, y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug12423a()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="Bug12423a">
          <file name="a.b">
Class A    
    Sub Foo()
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
    Sub Foo(i As Integer)
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("c, d, e, f", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("a, b, c, d, e, f", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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

            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, s, t", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
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
            Assert.Equal("b, x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranchReachability01()
            Dim analysis = CompileAndAnalyzeControlFlow(
      <compilation name="TestIfElseBranchReachability01">
          <file name="a.b">
Imports System
Module Program
    Function Foo() As Integer
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
    Function Foo() As Integer
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
    Function Foo() As Integer
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
    Function Foo() As Integer
        Static x, y
        [|If True Then x = 1 Else y = 1|]
        Dim z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch01_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch01">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static b As Boolean = True
        Static x, y
        [|If b Then x = 1 Else y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch02()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch02">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static x, y
        If True Then [|x = 1|] Else y = 1
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch03()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static x, y, z
        If True Then x = 1 Else [|y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut)) '' else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch03_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch03">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else [|y = 1|]
        Static z = x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch04()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static x, y, z
        If True Then x = 1 Else If True Then y = 1 Else [|z = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))  ''  else clause is unreachable
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch04_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch04">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else If b Then y = 1 Else [|z = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch05()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static x, y, z
        If True Then x = 1 Else [|If True Then y = 1 Else y = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIfElseBranch05_()
            Dim analysis = CompileAndAnalyzeDataFlow(
      <compilation name="TestIfElseBranch05">
          <file name="a.b">
Imports System

Module Program
    Function Foo() As Integer
        Static b As Boolean = True
        Static x, y, z
        If b Then x = 1 Else [|If b Then y = 1 Else y = 1|]
        Static zz = z + x + y
    End Function
End Module
  </file>
      </compilation>)
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
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
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x, z", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
            Assert.Equal("a, b, c, x, y, z", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
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
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
        foo(o)
|]
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
        oo.foo(o)
|]
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Class
</file>
      </compilation>)
            Assert.Equal("o, oo", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o, oo", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o, oo", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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

    Sub foo(x As String)

    End Sub

    Sub foo(Byref x As Integer)

    End Sub
End Module
</file>
      </compilation>)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("oo", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("args, o", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("i, j", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("i, j", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("i, j", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("j", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
            Assert.Equal("a, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
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
            Assert.Equal("Me, t1", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
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
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
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
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
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
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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
            Assert.Equal("a, x", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
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
            Assert.Equal("c, d", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("d, e, f", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("a, e, g, x", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("a, b, c, g, h, i, Me, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned)) ' In C# '=' is an assignment while in VB it is a comparison.
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut)) 'C# flows out because this is an assignement expression.  In VB this is an equality test.
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside)) 'C# this is an assignment. In VB, this is a comparison so no assignment.
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
        Foo(
x 
)
|]
      System.Console.WriteLine(x)
    end sub

    shared sub Foo(byref x as integer)
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(541891, "DevDiv")>
        <Fact()>
        Public Sub TestRefArgumentSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As UInteger
        System.Console.WriteLine([|Foo(x)|])
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(541891, "DevDiv")>
        <Fact()>
        Public Sub TestRefArgumentSelection02a()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestRefArgumentSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As UInteger
        System.Console.WriteLine(Foo([|x|]))
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection01()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection01">
          <file name="a.b">
class C
     Sub Main()
        Static x As String = ""
        [|x|]+=1
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection02()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection02">
          <file name="a.b">
class C
     Sub Main()
        Static x As String = ""
        [|x+=1|]
    End Sub
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestCompoundAsseignmentTargetSelection03()
            Dim dataFlowAnalysisResults = CompileAndAnalyzeDataFlow(
      <compilation name="TestCompoundAsseignmentTargetSelection03">
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

            Assert.Equal("ex, flocal, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("local01, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("ary, flocal, local01, local02", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("flocal, local01, local02", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("ary, flocal, local01, local02, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("ex, flocal, local01, local02, slocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("ary, local01, local02", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

        System.Console.WriteLine([|Foo(x)|])
    End Sub

    Function Foo(ByRef x As ULong)
        x = 123
        Return x + 1
    End Function
end class
            </file>
      </compilation>)

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
        Foo(x)
|]
    end sub

    shared sub Foo(int x) 
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me beng read
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
        Foo(y, 2) ' VB does not support expression assignment F(x = y, y = 2)
|]
        Static z as integer = x + y
    }

    shared sub Foo(int x, int y)
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside)) ' Sees Me being read
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("args, Me, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
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
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, t", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, t, t1", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("a, args, x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
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
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("x, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal("a, args", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal("v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("Me, p, v", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))

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

            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("args, ex, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IdentifierNameInMemberAccessExpr()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Public Class Foo
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
        Static  n1 = [|Foo|](85)
        End Sub
    Function Foo(i As Integer) As Integer
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
        Static n1 = [|Foo|](85)
  End Sub
    ReadOnly Property Foo(i As Integer) As Integer
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitSyntax2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitSyntax3()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim exprSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("{4, 5}")).Parent, CollectionInitializerSyntax)
            Dim analysis = model.AnalyzeDataFlow(exprSyntaxNode)

            Assert.True(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IfStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("If False")).Parent, IfStatementSyntax)
            Dim analysis = model.AnalyzeControlFlow(stmtSyntaxNode, stmtSyntaxNode)

            Assert.False(analysis.Succeeded)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ElseStatementSyntax()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim stmtSyntaxNode = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetRoot.ToFullString().IndexOf("Else")).Parent, ElseStatementSyntax)
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

            Assert.Equal("p", GetSymbolNamesSortedAndJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.Captured))
            Assert.Equal("constLocal, local", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsOut))
            Assert.Equal("constLocal, local, p", GetSymbolNamesSortedAndJoined(analysisResult.ReadInside))
            ' WHY
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(analysisResult.WrittenInside))
            Assert.Equal("f", GetSymbolNamesSortedAndJoined(analysisResult.ReadOutside))
            Assert.Equal("constLocal, f, local", GetSymbolNamesSortedAndJoined(analysisResult.WrittenOutside))

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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysisResult.AlwaysAssigned))
            Assert.Equal("local, mp", GetSymbolNamesSortedAndJoined(analysisResult.Captured))
            Assert.Equal("constLocal, mp", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsIn))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.DataFlowsOut))
            Assert.Equal("constLocal, mp", GetSymbolNamesSortedAndJoined(analysisResult.ReadInside))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(analysisResult.WrittenInside))
            Assert.Equal("lf, local", GetSymbolNamesSortedAndJoined(analysisResult.ReadOutside))
            Assert.Equal("constLocal, lf, local, Me, mp", GetSymbolNamesSortedAndJoined(analysisResult.WrittenOutside))

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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda1()
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
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda2()
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda3()
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("a", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, l, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda4()
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda5()
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("a, b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ObjectInitializers_StructWitFiledsAccesesInLambda6()
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("a, b, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("b", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("a, b, i, Me", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("strlocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("strlocal", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("localint, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("localint, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal("localint", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))

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
        Static foo as string = "Hello World"
        Static x as [|New List(Of String) From {foo, "!"}|]
    End Sub
End Class
    </file>
</compilation>)

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("foo", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
        Static  foo as string = "Hello World"
        Static x as New List(Of String) From [|{foo, "!"}|]
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
        Static foo as string = "Hello World"
        Static x as New Dictionary(Of String, Integer) From {[|{foo, 1}|], {"bar", 42}}
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
        Static foo As String = "Hello World"
        Static x As [|New List(Of Action) From {
            Sub()
                Console.WriteLine(foo)
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
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("foo, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty((dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("args, bb, x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("args, bb, x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("args, bb, x, y", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, bb, ret, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("local, p, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("f, local", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("f, local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Equal("args, i, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Sub TestWithEventsInitializer()
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

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
        End Sub

        <WorkItem(546820, "DevDiv")>
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
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

            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared))
            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.Captured))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut))
            Assert.Equal("Me, x, y", GetSymbolNamesSortedAndJoined(analysis.ReadInside))
            Assert.Equal("l1", GetSymbolNamesSortedAndJoined(analysis.WrittenInside))
            Assert.Equal("a, y", GetSymbolNamesSortedAndJoined(analysis.ReadOutside))
            Assert.Equal("a, c, Me, x, y", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("i, Me, x, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside))
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
            Assert.Equal("lambda", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("at, lambda, p", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("at, local, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("at, lambda, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("local", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAsNewInLocalContext()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IFoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Class CFoo
    Implements IFoo
End Class

Friend Module AM
    Sub Main(args As String())
        Static ifoo As IFoo = New CFoo()
        Static at1 As New With {.if = ifoo}
[|
        Static at2 As New With {.if = at1, ifoo,
            .friend = New With {Key args, .lambda = DirectCast(Sub(ByRef p As Char)
                                                                   args(0) = p &amp; p
                                                                   p = "Q"c
                                                               End Sub, IFoo.DS)}}
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
            Assert.Equal("at2", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("args, at1, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, at1, ifoo, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, ifoo", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonymousTypeAsExpression()
            Dim dataFlowResults = CompileAndAnalyzeDataFlow(
   <compilation>
       <file name="a.vb">
Imports System

Interface IFoo
    Delegate Sub DS(ByRef p As Char)
End Interface

Friend Module AM
    Sub Main(args As String())

       Static at1 As New With {.friend = New With {args, Key.lambda = DirectCast(Sub(ByRef p As Char)
                                                                                  args(0) = p &amp; p
                                                                                  p = "Q"c
                                                                              End Sub, IFoo.DS) }
                          }
       Dim at2 As New With { Key .a= at1, .friend = New With { [| at1 |] }}
       Console.Write(args(0))

    End Sub
End Module
    </file>
   </compilation>)

            Assert.True(dataFlowResults.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("args", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("at1", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("at1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("args, at1, p", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, at2, p", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("args, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("args, ary, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("ary", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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

            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Null(GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("var1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, at1, Me, var1", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("at, func, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("at", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("at, func, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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

            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("an", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("an, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
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
      </compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("v0, v1, v2, v3, v4, v5", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub XmlMemberAccess()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
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
      </compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Return")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
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

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim root = tree.GetCompilationUnitRoot()
            Dim node = DirectCast(root.FindToken(root.ToFullString().IndexOf("Static")).Parent, StatementSyntax)
            Dim dataFlowAnalysis = model.AnalyzeDataFlow(node, node)
            Assert.True(dataFlowAnalysis.Succeeded)
            Assert.Equal("o", GetSymbolNamesSortedAndJoined(dataFlowAnalysis.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowAnalysis.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, b, i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("i, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("i, Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x, xyz", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, s", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("arr, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("a, Me", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj3, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj3, obj4, obj5, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj3", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj1, obj2, obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj3, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj2", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("obj2, obj4", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("obj2, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("obj1, obj2, obj3, obj4, obj5", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(529089, "DevDiv")>
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
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.Captured))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.ReadInside))
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesSortedAndJoined(dataFlowResults.ReadOutside))
            Assert.Equal("args, x", GetSymbolNamesSortedAndJoined(dataFlowResults.WrittenOutside))
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
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim invocation = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim flowAnalysis = model.AnalyzeDataFlow(invocation)
            Assert.Empty(flowAnalysis.Captured)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.WrittenOutside.Single().ToTestDisplayString())

            Dim lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()
            flowAnalysis = model.AnalyzeDataFlow(lambda)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.Captured.Single().ToTestDisplayString())
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.DataFlowsIn.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.DataFlowsOut)
            Assert.Equal("[Me] As [MyClass]", flowAnalysis.ReadInside.Single().ToTestDisplayString())
            Assert.Empty(flowAnalysis.WrittenInside)
            Assert.Equal("f, Me", GetSymbolNamesSortedAndJoined(flowAnalysis.WrittenOutside))
        End Sub
#End Region
    End Class
End Namespace
