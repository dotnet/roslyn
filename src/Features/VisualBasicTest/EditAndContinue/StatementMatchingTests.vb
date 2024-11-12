' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <UseExportProvider>
    Public Class StatementMatchingTests
        Inherits EditingTestBase

        <Fact>
        Public Sub Match1()
            Dim src1 = <text>
Dim x As Integer = 1 
Console.WriteLine( 1)
x  +=  1
Console.WriteLine(2)

While True : x += 1 : End While

Console.WriteLine(1)
</text>.Value

            Dim src2 = <text>
Dim x As Integer = 1 
x  +=  1
For i = 0 To 10
Next
y += 1
If x = 1
    While True : x += 1 : End While
    Console.WriteLine(1 )
End If
</text>.Value

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' TODO: consider adding location distance between nodes that match into consideration when
            ' there are multiple equivalent nodes to match, in this case "Console.WriteLine(1)" 
            ' should match "Console.WriteLine(1 )"
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim x As Integer = 1", "Dim x As Integer = 1"},
                {"x As Integer = 1", "x As Integer = 1"},
                {"x", "x"},
                {"Console.WriteLine( 1)", "Console.WriteLine(1 )"},
                {"x  +=  1", "x  +=  1"},
                {"While True : x += 1 : End While", "While True : x += 1 : End While"},
                {"While True", "While True"},
                {"x += 1", "x += 1"},
                {"End While", "End While"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub KnownMatches_Root()
            Dim src1 As String = "Console.WriteLine(1)"
            Dim src2 As String = "Console.WriteLine(2)"

            Dim m1 = MakeMethodBody(src1)
            Dim m2 = MakeMethodBody(src2)

            Dim knownMatches = {New KeyValuePair(Of SyntaxNode, SyntaxNode)(m1.RootNodes.First(), m2.RootNodes().First())}
            Dim match = m1.ComputeSingleRootMatch(m2, knownMatches)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Console.WriteLine(1)", "Console.WriteLine(2)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub Locals_Rename1()
            Dim src1 = "Dim x = 1"
            Dim src2 = "Dim y = 1"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim x = 1", "Dim y = 1"},
                {"x = 1", "y = 1"},
                {"x", "y"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub Locals_Rename2()
            Dim src1 = "Dim x As Integer = 1"
            Dim src2 = "Dim y As Integer = 1"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim x As Integer = 1", "Dim y As Integer = 1"},
                {"x As Integer = 1", "y As Integer = 1"},
                {"x", "y"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub Locals_TypeChange()
            Dim src1 = "Dim x As Integer = 1"
            Dim src2 = "Dim x As Byte = 1"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim x As Integer = 1", "Dim x As Byte = 1"},
                {"x As Integer = 1", "x As Byte = 1"},
                {"x", "x"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub BlocksWithLocals1()
            Dim src1 = <text>
While True : Dim a As Integer = 1 : End While
While True : Dim b As Integer = 2 : End While
</text>.Value
            Dim src2 = <text>
While True : Dim a As Integer = 3 : Dim b As Integer = 4 : End While
While True : Dim b As Integer = 5 : End While
</text>.Value

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"While True : Dim a As Integer = 1 : End While", "While True : Dim a As Integer = 3 : Dim b As Integer = 4 : End While"},
                {"While True", "While True"},
                {"Dim a As Integer = 1", "Dim a As Integer = 3"},
                {"a As Integer = 1", "a As Integer = 3"},
                {"a", "a"},
                {"End While", "End While"},
                {"While True : Dim b As Integer = 2 : End While", "While True : Dim b As Integer = 5 : End While"},
                {"While True", "While True"},
                {"Dim b As Integer = 2", "Dim b As Integer = 5"},
                {"b As Integer = 2", "b As Integer = 5"},
                {"b", "b"},
                {"End While", "End While"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub LoopBlocksWithLocals1()
            Dim src1 = <text>
Do While True : Dim a As Integer : Loop
While True : Dim b As Integer : End While
Do : Dim c As Integer : Loop While True
Do : Dim d As Integer : Loop
</text>.Value
            Dim src2 = <text>
Do While True : Dim d As Integer : Loop
While True : Dim c As Integer : End While
Do : Dim b As Integer : Loop While True
Do : Dim a As Integer : Loop
</text>.Value

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Do While True : Dim a As Integer : Loop", "Do : Dim a As Integer : Loop"},
                {"Do While True", "Do"},
                {"Dim a As Integer", "Dim a As Integer"},
                {"a As Integer", "a As Integer"},
                {"a", "a"},
                {"Loop", "Loop"},
                {"While True : Dim b As Integer : End While", "Do : Dim b As Integer : Loop While True"},
                {"While True", "Do"},
                {"Dim b As Integer", "Dim b As Integer"},
                {"b As Integer", "b As Integer"},
                {"b", "b"},
                {"End While", "Loop While True"},
                {"Do : Dim c As Integer : Loop While True", "While True : Dim c As Integer : End While"},
                {"Do", "While True"},
                {"Dim c As Integer", "Dim c As Integer"},
                {"c As Integer", "c As Integer"},
                {"c", "c"},
                {"Loop While True", "End While"},
                {"Do : Dim d As Integer : Loop", "Do While True : Dim d As Integer : Loop"},
                {"Do", "Do While True"},
                {"Dim d As Integer", "Dim d As Integer"},
                {"d As Integer", "d As Integer"},
                {"d", "d"},
                {"Loop", "Loop"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub IfBlocksWithLocals1()
            Dim src1 = <text>
If X : Dim a As Integer = 1 : End If
If Y : Dim b As Integer = 2 : End If
</text>.Value
            Dim src2 = <text>
If Y : Dim a As Integer = 3 : Dim b As Integer = 4 : End If
If X : Dim b As Integer = 5 : End If
</text>.Value
            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"If X : Dim a As Integer = 1 : End If", "If Y : Dim a As Integer = 3 : Dim b As Integer = 4 : End If"},
                {"If X", "If Y"},
                {"Dim a As Integer = 1", "Dim a As Integer = 3"},
                {"a As Integer = 1", "a As Integer = 3"},
                {"a", "a"},
                {"End If", "End If"},
                {"If Y : Dim b As Integer = 2 : End If", "If X : Dim b As Integer = 5 : End If"},
                {"If Y", "If X"},
                {"Dim b As Integer = 2", "Dim b As Integer = 5"},
                {"b As Integer = 2", "b As Integer = 5"},
                {"b", "b"},
                {"End If", "End If"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub IfBlocksWithLocals2()
            Dim src1 = <text>
If X Then Dim a As Integer = 1
If Y : Dim b As Integer = 2 : End If
</text>.Value
            Dim src2 = <text>
If X Then Dim b As Integer = 3
If Y : Dim a As Integer = 5 : End If
</text>.Value
            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"If X Then Dim a As Integer = 1", "If Y : Dim a As Integer = 5 : End If"},
                {"Dim a As Integer = 1", "Dim a As Integer = 5"},
                {"a As Integer = 1", "a As Integer = 5"},
                {"a", "a"},
                {"If Y : Dim b As Integer = 2 : End If", "If X Then Dim b As Integer = 3"},
                {"Dim b As Integer = 2", "Dim b As Integer = 3"},
                {"b As Integer = 2", "b As Integer = 3"},
                {"b", "b"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub IfBlockToStatement()
            Dim src1 = <text>
If X Then Console.WriteLine(1) Else Console.WriteLine(2)
</text>.Value
            Dim src2 = <text>
If X Then : Console.WriteLine(1) : Else : Console.WriteLine(2) : End If
</text>.Value
            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"If X Then Console.WriteLine(1) Else Console.WriteLine(2)", "If X Then : Console.WriteLine(1) : Else : Console.WriteLine(2) : End If"},
                {"Console.WriteLine(1)", "Console.WriteLine(1)"},
                {"Else Console.WriteLine(2)", "Else : Console.WriteLine(2)"},
                {"Console.WriteLine(2)", "Console.WriteLine(2)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub BlocksWithLocals2()
            Dim src1 = <text>
While True : Dim a As Integer = 1 : End While
While True : While True : Dim b As Integer = 2 : End While : End While
</text>.Value
            Dim src2 = <text>
While True : Dim b As Integer = 1 : End While 
While True : While True : Dim a As Integer = 2 : End While : End While
</text>.Value
            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"While True : Dim a As Integer = 1 : End While", "While True : Dim a As Integer = 2 : End While"},
                {"While True", "While True"},
                {"Dim a As Integer = 1", "Dim a As Integer = 2"},
                {"a As Integer = 1", "a As Integer = 2"},
                {"a", "a"},
                {"End While", "End While"},
                {"While True : While True : Dim b As Integer = 2 : End While : End While", "While True : While True : Dim a As Integer = 2 : End While : End While"},
                {"While True", "While True"},
                {"While True : Dim b As Integer = 2 : End While", "While True : Dim b As Integer = 1 : End While"},
                {"While True", "While True"},
                {"Dim b As Integer = 2", "Dim b As Integer = 1"},
                {"b As Integer = 2", "b As Integer = 1"},
                {"b", "b"},
                {"End While", "End While"},
                {"End While", "End While"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub BlocksWithLocals3()
            Dim src1 = <text>
Do : Dim a = 1, b = 2, c = 3 : Console.WriteLine(a + b + c) : Loop
Do : Dim c = 4, b = 5, a = 6 : Console.WriteLine(a + b + c) : Loop
Do : Dim a = 7, b = 8 : Console.WriteLine(a + b) : Loop
</text>.Value

            Dim src2 = <text>
Do : Dim a = 9, b = 10 : Console.WriteLine(a + b) : Loop
Do : Dim c = 11, b = 12, a = 13 : Console.WriteLine(a + b + c) : Loop
Do : Dim a = 14, b = 15, c = 16 : Console.WriteLine(a + b + c) : Loop
</text>.Value

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Do : Dim a = 1, b = 2, c = 3 : Console.WriteLine(a + b + c) : Loop", "Do : Dim a = 14, b = 15, c = 16 : Console.WriteLine(a + b + c) : Loop"},
                {"Do", "Do"},
                {"Dim a = 1, b = 2, c = 3", "Dim a = 14, b = 15, c = 16"},
                {"a = 1", "a = 14"},
                {"a", "a"},
                {"b = 2", "b = 15"},
                {"b", "b"},
                {"c = 3", "c = 16"},
                {"c", "c"},
                {"Console.WriteLine(a + b + c)", "Console.WriteLine(a + b + c)"},
                {"Loop", "Loop"},
                {"Do : Dim c = 4, b = 5, a = 6 : Console.WriteLine(a + b + c) : Loop", "Do : Dim c = 11, b = 12, a = 13 : Console.WriteLine(a + b + c) : Loop"},
                {"Do", "Do"},
                {"Dim c = 4, b = 5, a = 6", "Dim c = 11, b = 12, a = 13"},
                {"c = 4", "c = 11"},
                {"c", "c"},
                {"b = 5", "b = 12"},
                {"b", "b"},
                {"a = 6", "a = 13"},
                {"a", "a"},
                {"Console.WriteLine(a + b + c)", "Console.WriteLine(a + b + c)"},
                {"Loop", "Loop"},
                {"Do : Dim a = 7, b = 8 : Console.WriteLine(a + b) : Loop", "Do : Dim a = 9, b = 10 : Console.WriteLine(a + b) : Loop"},
                {"Do", "Do"},
                {"Dim a = 7, b = 8", "Dim a = 9, b = 10"},
                {"a = 7", "a = 9"},
                {"a", "a"},
                {"b = 8", "b = 10"},
                {"b", "b"},
                {"Console.WriteLine(a + b)", "Console.WriteLine(a + b)"},
                {"Loop", "Loop"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithLambdas1()
            Dim src1 = "Dim a = Sub() Console.WriteLine(1)" & vbLf
            Dim src2 = "Dim a = Sub() Console.WriteLine(2)" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' note that the lambda bodies are not included
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim a = Sub() Console.WriteLine(1)", "Dim a = Sub() Console.WriteLine(2)"},
                {"a = Sub() Console.WriteLine(1)", "a = Sub() Console.WriteLine(2)"},
                {"a", "a"},
                {"Sub() Console.WriteLine(1)", "Sub() Console.WriteLine(2)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_From1()
            Dim src1 = "Dim result = From a In F(Function() 0), b In Q(Function() 2) Select a + b" & vbLf
            Dim src2 = "Dim result = From a In F(Function() 1), b In Q(Function() 3) Select a - b" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' note missing {"Function() 2", "Function() 3"} -- it's in a lambda body (CRV.Expression)
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In F(Function() 0), b In Q(Function() 2) Select a + b", "Dim result = From a In F(Function() 1), b In Q(Function() 3) Select a - b"},
                {"result = From a In F(Function() 0), b In Q(Function() 2) Select a + b", "result = From a In F(Function() 1), b In Q(Function() 3) Select a - b"},
                {"result", "result"},
                {"From a In F(Function() 0), b In Q(Function() 2) Select a + b", "From a In F(Function() 1), b In Q(Function() 3) Select a - b"},
                {"From a In F(Function() 0), b In Q(Function() 2)", "From a In F(Function() 1), b In Q(Function() 3)"},
                {"a In F(Function() 0)", "a In F(Function() 1)"},
                {"a", "a"},
                {"Function() 0", "Function() 1"},
                {"b In Q(Function() 2)", "b In Q(Function() 3)"},
                {"b", "b"},
                {"Select a + b", "Select a - b"},
                {"a + b", "a - b"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_From2()
            Dim src1 = "Dim result = From a In F(Function() 0) From b In Q(Function() 2) Select a + b" & vbLf
            Dim src2 = "Dim result = From a In F(Function() 1) From b In Q(Function() 3) Select a - b" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' note missing {"Function() 2", "Function() 3"} -- it's in a lambda body (CRV.Expression)
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In F(Function() 0) From b In Q(Function() 2) Select a + b",
                 "Dim result = From a In F(Function() 1) From b In Q(Function() 3) Select a - b"},
                {"result = From a In F(Function() 0) From b In Q(Function() 2) Select a + b",
                 "result = From a In F(Function() 1) From b In Q(Function() 3) Select a - b"},
                {"result", "result"},
                {"From a In F(Function() 0) From b In Q(Function() 2) Select a + b",
                 "From a In F(Function() 1) From b In Q(Function() 3) Select a - b"},
                {"From a In F(Function() 0)", "From a In F(Function() 1)"},
                {"a In F(Function() 0)", "a In F(Function() 1)"},
                {"a", "a"},
                {"Function() 0", "Function() 1"},
                {"From b In Q(Function() 2)", "From b In Q(Function() 3)"},
                {"b In Q(Function() 2)", "b In Q(Function() 3)"},
                {"b", "b"},
                {"Select a + b", "Select a - b"},
                {"a + b", "a - b"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_From3()
            Dim src1 = "Dim result = From a In {Await F(0)}, b In {Q(Async Function() Await F(2))} Select a + b" & vbLf
            Dim src2 = "Dim result = From a In {Await F(1)}, b In {Q(Async Function() Await F(3))} Select a - b" & vbLf

            Dim match = GetMethodMatch(src1, src2, methodKind:=MethodKind.Async)
            Dim actual = ToMatchingPairs(match)

            ' Note that 
            ' - both {"a", "a"} And {"b", "b"} are included
            ' - {"Await F(0)", "Await F(1)"} is included but not the other await, since the other is in a lambda
            Dim expected = New MatchingPairs From
            {
                {"Async Function F() As Task(Of Integer)", "Async Function F() As Task(Of Integer)"},
                {"Dim result = From a In {Await F(0)}, b In {Q(Async Function() Await F(2))} Select a + b", "Dim result = From a In {Await F(1)}, b In {Q(Async Function() Await F(3))} Select a - b"},
                {"result = From a In {Await F(0)}, b In {Q(Async Function() Await F(2))} Select a + b", "result = From a In {Await F(1)}, b In {Q(Async Function() Await F(3))} Select a - b"},
                {"result", "result"},
                {"From a In {Await F(0)}, b In {Q(Async Function() Await F(2))} Select a + b", "From a In {Await F(1)}, b In {Q(Async Function() Await F(3))} Select a - b"},
                {"From a In {Await F(0)}, b In {Q(Async Function() Await F(2))}", "From a In {Await F(1)}, b In {Q(Async Function() Await F(3))}"},
                {"a In {Await F(0)}", "a In {Await F(1)}"},
                {"a", "a"},
                {"Await F(0)", "Await F(1)"},
                {"b In {Q(Async Function() Await F(2))}", "b In {Q(Async Function() Await F(3))}"},
                {"b", "b"},
                {"Select a + b", "Select a - b"},
                {"a + b", "a - b"},
                {"End Function", "End Function"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_Aggregate1()
            Dim src1 = "Dim result = From a In {1} Aggregate b In Q(Function() 2) Into c = Sum(Q(Function() 4)) Select 5" & vbLf
            Dim src2 = "Dim result = From a In {10} Aggregate b In Q(Function() 3) Into c = Sum(Q(Function() 5)) Select 50" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' note missing {"Function() 2", "Function() 3"} -- it's in the aggregate lambda body
            ' note missing {"Function() 4", "Function() 5"} -- it's in the aggregation lambda body
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {1} Aggregate b In Q(Function() 2) Into c = Sum(Q(Function() 4)) Select 5", "Dim result = From a In {10} Aggregate b In Q(Function() 3) Into c = Sum(Q(Function() 5)) Select 50"},
                {"result = From a In {1} Aggregate b In Q(Function() 2) Into c = Sum(Q(Function() 4)) Select 5", "result = From a In {10} Aggregate b In Q(Function() 3) Into c = Sum(Q(Function() 5)) Select 50"},
                {"result", "result"},
                {"From a In {1} Aggregate b In Q(Function() 2) Into c = Sum(Q(Function() 4)) Select 5", "From a In {10} Aggregate b In Q(Function() 3) Into c = Sum(Q(Function() 5)) Select 50"},
                {"From a In {1}", "From a In {10}"},
                {"a In {1}", "a In {10}"},
                {"a", "a"},
                {"Aggregate b In Q(Function() 2) Into c = Sum(Q(Function() 4))", "Aggregate b In Q(Function() 3) Into c = Sum(Q(Function() 5))"},
                {"b In Q(Function() 2)", "b In Q(Function() 3)"},
                {"b", "b"},
                {"c", "c"},
                {"Sum(Q(Function() 4))", "Sum(Q(Function() 5))"},
                {"Select 5", "Select 50"},
                {"5", "50"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_Aggregate2()
            Dim src1 = "Dim result = From q in {0} Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()" & vbLf
            Dim src2 = "Dim result = From q in {0} Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()" & vbLf

            Dim match = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From q in {0} Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()", "Dim result = From q in {0} Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()"},
                {"result = From q in {0} Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()", "result = From q in {0} Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()"},
                {"result", "result"},
                {"From q in {0} Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()", "From q in {0} Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()"},
                {"From q in {0}", "From q in {0}"},
                {"q in {0}", "q in {0}"},
                {"q", "q"},
                {"Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()", "Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()"},
                {"b In Q(Function() 1)", "b In Q(Function() 2)"},
                {"b", "b"},
                {"Function() 1", "Function() 2"},
                {"Function()", "Function()"},
                {"()", "()"},
                {"Join c In Q(Function() 3) On c Equals b", "Join c In Q(Function() 4) On c Equals b"},
                {"c In Q(Function() 3)", "c In Q(Function() 4)"},
                {"c", "c"},
                {"c Equals b", "c Equals b"},
                {"Skip Q(Function() 5)", "Skip Q(Function() 6)"},
                {"Select b", "Select b"},
                {"b", "b"},
                {"Count()", "Count()"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_Select1()
            Dim src1 = "Dim result = From a As Integer In {0} Select a, b = Q(Function() 1), c = Q(Function() 3)" & vbLf
            Dim src2 = "Dim result = From a As Integer In {0} Select a, b = Q(Function() 2), c = Q(Function() 4)" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a As Integer In {0} Select a, b = Q(Function() 1), c = Q(Function() 3)", "Dim result = From a As Integer In {0} Select a, b = Q(Function() 2), c = Q(Function() 4)"},
                {"result = From a As Integer In {0} Select a, b = Q(Function() 1), c = Q(Function() 3)", "result = From a As Integer In {0} Select a, b = Q(Function() 2), c = Q(Function() 4)"},
                {"result", "result"},
                {"From a As Integer In {0} Select a, b = Q(Function() 1), c = Q(Function() 3)", "From a As Integer In {0} Select a, b = Q(Function() 2), c = Q(Function() 4)"},
                {"From a As Integer In {0}", "From a As Integer In {0}"},
                {"a As Integer In {0}", "a As Integer In {0}"},
                {"a", "a"},
                {"Select a, b = Q(Function() 1), c = Q(Function() 3)", "Select a, b = Q(Function() 2), c = Q(Function() 4)"},
                {"a", "a"},
                {"b = Q(Function() 1)", "b = Q(Function() 2)"},
                {"b", "b"},
                {"c = Q(Function() 3)", "c = Q(Function() 4)"},
                {"c", "c"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_Let1()
            Dim src1 = "Dim result = From a In {0} Let b = Q(Function() 1), c = Q(Function() 3) Select a" & vbLf
            Dim src2 = "Dim result = From a In {0} Let b = Q(Function() 2), c = Q(Function() 4) Select a" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {0} Let b = Q(Function() 1), c = Q(Function() 3) Select a", "Dim result = From a In {0} Let b = Q(Function() 2), c = Q(Function() 4) Select a"},
                {"result = From a In {0} Let b = Q(Function() 1), c = Q(Function() 3) Select a", "result = From a In {0} Let b = Q(Function() 2), c = Q(Function() 4) Select a"},
                {"result", "result"},
                {"From a In {0} Let b = Q(Function() 1), c = Q(Function() 3) Select a", "From a In {0} Let b = Q(Function() 2), c = Q(Function() 4) Select a"},
                {"From a In {0}", "From a In {0}"},
                {"a In {0}", "a In {0}"},
                {"a", "a"},
                {"Let b = Q(Function() 1), c = Q(Function() 3)", "Let b = Q(Function() 2), c = Q(Function() 4)"},
                {"b = Q(Function() 1)", "b = Q(Function() 2)"},
                {"b", "b"},
                {"c = Q(Function() 3)", "c = Q(Function() 4)"},
                {"c", "c"},
                {"Select a", "Select a"},
                {"a", "a"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_GroupBy1()
            Dim src1 = "Dim result = From a In {0} Group a = Q(Function() 1) By b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a" & vbLf
            Dim src2 = "Dim result = From a In {0} Group a = Q(Function() 2) By b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {0} Group a = Q(Function() 1) By b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "Dim result = From a In {0} Group a = Q(Function() 2) By b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"result = From a In {0} Group a = Q(Function() 1) By b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "result = From a In {0} Group a = Q(Function() 2) By b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"result", "result"},
                {"From a In {0} Group a = Q(Function() 1) By b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "From a In {0} Group a = Q(Function() 2) By b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"From a In {0}", "From a In {0}"},
                {"a In {0}", "a In {0}"},
                {"a", "a"},
                {"Group a = Q(Function() 1) By b = Q(Function() 3) Into Sum(Q(Function() 5))", "Group a = Q(Function() 2) By b = Q(Function() 4) Into Sum(Q(Function() 6))"},
                {"a = Q(Function() 1)", "a = Q(Function() 2)"},
                {"a", "a"},
                {"b = Q(Function() 3)", "b = Q(Function() 4)"},
                {"b", "b"},
                {"Sum(Q(Function() 5))", "Sum(Q(Function() 6))"},
                {"Select a", "Select a"},
                {"a", "a"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_GroupBy2()
            Dim src1 = "Dim result = From a In {0} Group z = Q(Function() 0) By a = Q(Function() 1), b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a" & vbLf
            Dim src2 = "Dim result = From a In {0} Group By a = Q(Function() 2), b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            ' Note "z = Q(Function() 0)" doesn't match to "a = Q(Function() 4)" -- the are in different lambda bodies
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {0} Group z = Q(Function() 0) By a = Q(Function() 1), b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "Dim result = From a In {0} Group By a = Q(Function() 2), b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"result = From a In {0} Group z = Q(Function() 0) By a = Q(Function() 1), b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "result = From a In {0} Group By a = Q(Function() 2), b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"result", "result"},
                {"From a In {0} Group z = Q(Function() 0) By a = Q(Function() 1), b = Q(Function() 3) Into Sum(Q(Function() 5)) Select a", "From a In {0} Group By a = Q(Function() 2), b = Q(Function() 4) Into Sum(Q(Function() 6)) Select a"},
                {"From a In {0}", "From a In {0}"},
                {"a In {0}", "a In {0}"},
                {"a", "a"},
                {"Group z = Q(Function() 0) By a = Q(Function() 1), b = Q(Function() 3) Into Sum(Q(Function() 5))", "Group By a = Q(Function() 2), b = Q(Function() 4) Into Sum(Q(Function() 6))"},
                {"a = Q(Function() 1)", "a = Q(Function() 2)"},
                {"a", "a"},
                {"b = Q(Function() 3)", "b = Q(Function() 4)"},
                {"b", "b"},
                {"Sum(Q(Function() 5))", "Sum(Q(Function() 6))"},
                {"Select a", "Select a"},
                {"a", "a"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchMethodBodiesWithQueries_Join1()
            Dim src1 = "Dim result = From a In {0} Join b In {1} On Q(Function() 1) Equals Q(Function() 3) And Q(Function() 5) Equals Q(Function() 7) Select a" & vbLf
            Dim src2 = "Dim result = From a In {0} Join b In {1} On Q(Function() 2) Equals Q(Function() 4) And Q(Function() 6) Equals Q(Function() 8) Select a" & vbLf

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {0} Join b In {1} On Q(Function() 1) Equals Q(Function() 3) And Q(Function() 5) Equals Q(Function() 7) Select a", "Dim result = From a In {0} Join b In {1} On Q(Function() 2) Equals Q(Function() 4) And Q(Function() 6) Equals Q(Function() 8) Select a"},
                {"result = From a In {0} Join b In {1} On Q(Function() 1) Equals Q(Function() 3) And Q(Function() 5) Equals Q(Function() 7) Select a", "result = From a In {0} Join b In {1} On Q(Function() 2) Equals Q(Function() 4) And Q(Function() 6) Equals Q(Function() 8) Select a"},
                {"result", "result"},
                {"From a In {0} Join b In {1} On Q(Function() 1) Equals Q(Function() 3) And Q(Function() 5) Equals Q(Function() 7) Select a", "From a In {0} Join b In {1} On Q(Function() 2) Equals Q(Function() 4) And Q(Function() 6) Equals Q(Function() 8) Select a"},
                {"From a In {0}", "From a In {0}"},
                {"a In {0}", "a In {0}"},
                {"a", "a"},
                {"Join b In {1} On Q(Function() 1) Equals Q(Function() 3) And Q(Function() 5) Equals Q(Function() 7)", "Join b In {1} On Q(Function() 2) Equals Q(Function() 4) And Q(Function() 6) Equals Q(Function() 8)"},
                {"b In {1}", "b In {1}"},
                {"b", "b"},
                {"Q(Function() 1) Equals Q(Function() 3)", "Q(Function() 2) Equals Q(Function() 4)"},
                {"Q(Function() 5) Equals Q(Function() 7)", "Q(Function() 6) Equals Q(Function() 8)"},
                {"Select a", "Select a"},
                {"a", "a"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        ' TODO: test GroupBy with known matches across CRVs (coming from active statement tracking)

        <Fact>
        Public Sub MatchLambdas1()
            Dim src1 = "Dim x As Action(Of Object) = Sub(a) Console.WriteLine(a)" & vbLf
            Dim src2 = "Dim x As Action(Of Object) = Sub(b)" & vbLf & "Console.WriteLine(b) : End Sub"
            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim x As Action(Of Object) = Sub(a) Console.WriteLine(a)", "Dim x As Action(Of Object) = Sub(b) Console.WriteLine(b) : End Sub"},
                {"x As Action(Of Object) = Sub(a) Console.WriteLine(a)", "x As Action(Of Object) = Sub(b) Console.WriteLine(b) : End Sub"},
                {"x", "x"},
                {"Sub(a) Console.WriteLine(a)", "Sub(b) Console.WriteLine(b) : End Sub"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas2()
            Dim src1 = "F(Function(x) (x + 1), 1, Function(y) As Integer" & vbLf & " Return y + 1 : End Function, Function(x As Integer) x, Async Function(u) u)"
            Dim src2 = "F(Function(y) (y + 1), G(), Function(x) (x + 1), Function(x) x, Function(u) u, Async Function(u, v) (u + v))"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(Function(x) (x + 1), 1, Function(y) As Integer  Return y + 1 : End Function, Function(x As Integer) x, Async Function(u) u)",
                 "F(Function(y) (y + 1), G(), Function(x) (x + 1), Function(x) x, Function(u) u, Async Function(u, v) (u + v))"},
                {"Function(x) (x + 1)", "Function(x) (x + 1)"},
                {"Function(y) As Integer  Return y + 1 : End Function", "Function(y) (y + 1)"},
                {"Function(x As Integer) x", "Function(x) x"},
                {"Async Function(u) u", "Async Function(u, v) (u + v)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas3()
            Dim src1 = "AddHandler a, Async Function(u) u"
            Dim src2 = "AddHandler a, Function(u) u"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"AddHandler a, Async Function(u) u", "AddHandler a, Function(u) u"},
                {"Async Function(u) u", "Function(u) u"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas4()
            Dim src1 = "For Each a In z : Dim e = From q In a.Where(Function(l) l > 10) Select q + 1 : Next"
            Dim src2 = "For Each a In z : Dim e = From q In a.Where(Function(l) l < 0) Select q + 1 : Next"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"For Each a In z : Dim e = From q In a.Where(Function(l) l > 10) Select q + 1 : Next", "For Each a In z : Dim e = From q In a.Where(Function(l) l < 0) Select q + 1 : Next"},
                {"For Each a In z", "For Each a In z"},
                {"Dim e = From q In a.Where(Function(l) l > 10) Select q + 1", "Dim e = From q In a.Where(Function(l) l < 0) Select q + 1"},
                {"e = From q In a.Where(Function(l) l > 10) Select q + 1", "e = From q In a.Where(Function(l) l < 0) Select q + 1"},
                {"e", "e"},
                {"From q In a.Where(Function(l) l > 10) Select q + 1", "From q In a.Where(Function(l) l < 0) Select q + 1"},
                {"From q In a.Where(Function(l) l > 10)", "From q In a.Where(Function(l) l < 0)"},
                {"q In a.Where(Function(l) l > 10)", "q In a.Where(Function(l) l < 0)"},
                {"q", "q"},
                {"Function(l) l > 10", "Function(l) l < 0"},
                {"Select q + 1", "Select q + 1"},
                {"q + 1", "q + 1"},
                {"Next", "Next"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas5()
            Dim src1 = "F(Function(a) Function(b) Function(c) d)"
            Dim src2 = "F(Function(a) Function(b) Function(c) d)"

            Dim matches = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(matches)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(Function(a) Function(b) Function(c) d)", "F(Function(a) Function(b) Function(c) d)"},
                {"Function(a) Function(b) Function(c) d", "Function(a) Function(b) Function(c) d"},
                {"Function(a)", "Function(a)"},
                {"(a)", "(a)"},
                {"a", "a"},
                {"a", "a"},
                {"Function(b) Function(c) d", "Function(b) Function(c) d"},
                {"Function(b)", "Function(b)"},
                {"(b)", "(b)"},
                {"b", "b"},
                {"b", "b"},
                {"Function(c) d", "Function(c) d"},
                {"Function(c)", "Function(c)"},
                {"(c)", "(c)"},
                {"c", "c"},
                {"c", "c"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas6()
            Dim src1 = "F(Function(a) Function(b) Function(c) d)"
            Dim src2 = "F(Function(a) G(Function(b) H(Function(c) d)))"

            Dim matches = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(matches)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(Function(a) Function(b) Function(c) d)", "F(Function(a) G(Function(b) H(Function(c) d)))"},
                {"Function(a) Function(b) Function(c) d", "Function(a) G(Function(b) H(Function(c) d))"},
                {"Function(a)", "Function(a)"},
                {"(a)", "(a)"},
                {"a", "a"},
                {"a", "a"},
                {"Function(b) Function(c) d", "Function(b) H(Function(c) d)"},
                {"Function(b)", "Function(b)"},
                {"(b)", "(b)"},
                {"b", "b"},
                {"b", "b"},
                {"Function(c) d", "Function(c) d"},
                {"Function(c)", "Function(c)"},
                {"(c)", "(c)"},
                {"c", "c"},
                {"c", "c"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchLambdas7()
            Dim src1 = <text>
F(Sub(a)
    F(Function(c) d)
    F(Sub(u, v)
        F(Function(w) Function(c)  d )
        F(Function(p) p)
      End Sub)
  End Sub)
</text>.Value

            Dim src2 = <text>
F(Sub(a)
    F(Function(c) d + 1)
    F(Sub(u, v)
        F(Function(w) Function(c) d  +  1)
        F(Function(p) p*2)
      End Sub)
  End Sub)
</text>.Value

            Dim matches = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(matches)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(Sub(a)     F(Function(c) d)     F(Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub)   End Sub)",
                 "F(Sub(a)     F(Function(c) d + 1)     F(Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub)   End Sub)"},
                {"Sub(a)     F(Function(c) d)     F(Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub)   End Sub",
                 "Sub(a)     F(Function(c) d + 1)     F(Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub)   End Sub"},
                {"Sub(a)", "Sub(a)"},
                {"(a)", "(a)"},
                {"a", "a"},
                {"a", "a"},
                {"F(Function(c) d)", "F(Function(c) d + 1)"},
                {"Function(c) d", "Function(c) d + 1"},
                {"Function(c)", "Function(c)"},
                {"(c)", "(c)"},
                {"c", "c"},
                {"c", "c"},
                {"F(Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub)",
                 "F(Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub)"},
                {"Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub",
                 "Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub"},
                {"Sub(u, v)", "Sub(u, v)"},
                {"(u, v)", "(u, v)"},
                {"u", "u"},
                {"u", "u"},
                {"v", "v"},
                {"v", "v"},
                {"F(Function(w) Function(c)  d )", "F(Function(w) Function(c) d  +  1)"},
                {"Function(w) Function(c)  d", "Function(w) Function(c) d  +  1"},
                {"Function(w)", "Function(w)"},
                {"(w)", "(w)"},
                {"w", "w"},
                {"w", "w"},
                {"Function(c)  d", "Function(c) d  +  1"},
                {"Function(c)", "Function(c)"},
                {"(c)", "(c)"},
                {"c", "c"},
                {"c", "c"},
                {"F(Function(p) p)", "F(Function(p) p*2)"},
                {"Function(p) p", "Function(p) p*2"},
                {"Function(p)", "Function(p)"},
                {"(p)", "(p)"},
                {"p", "p"},
                {"p", "p"},
                {"End Sub", "End Sub"},
                {"End Sub", "End Sub"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchQueries1()
            Dim src1 = <text>
Dim q = From c In cars
        From ud In users_details
        From bd In bids
        Select 1
</text>.Value

            Dim src2 = <text>
Dim q = From c In cars
        From ud In users_details
        From bd In bids
        Select 2
</text>.Value

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim q = From c In cars         From ud In users_details         From bd In bids         Select 1", "Dim q = From c In cars         From ud In users_details         From bd In bids         Select 2"},
                {"q = From c In cars         From ud In users_details         From bd In bids         Select 1", "q = From c In cars         From ud In users_details         From bd In bids         Select 2"},
                {"q", "q"},
                {"From c In cars         From ud In users_details         From bd In bids         Select 1", "From c In cars         From ud In users_details         From bd In bids         Select 2"},
                {"From c In cars", "From c In cars"},
                {"c In cars", "c In cars"},
                {"c", "c"},
                {"From ud In users_details", "From ud In users_details"},
                {"ud In users_details", "ud In users_details"},
                {"ud", "ud"},
                {"From bd In bids", "From bd In bids"},
                {"bd In bids", "bd In bids"},
                {"bd", "bd"},
                {"Select 1", "Select 2"},
                {"1", "2"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchQueries2()
            Dim src1 = <text>
Dim q = From c In cars
        From ud In users_details
        From bd In bids
        Order By c.listingOption
        Where a.userID = ud.userid
        Let images = From ai In auction_images
                        Where ai.belongs_to = c.id
                        Select ai
        Let bid = (From b In bids
                    Order By b.id
                    Where b.carID = c.id
                    Select b.bidamount).FirstOrDefault()
        Select bid
</text>.Value

            Dim src2 = <text>
Dim q = From c In cars
        From ud In users_details
        From bd In bids
        Order By c.listingOption Descending
        Where a.userID = ud.userid
        Let images = From ai In auction_images
                        Where ai.belongs_to = c.id2
                        Select ai + 1
        Let bid = (From b In bids
                    Order By b.id Ascending
                    Where b.carID = c.id2
                    Select b.bidamount).FirstOrDefault()
        Select bid
</text>.Value

            Dim match = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim q = From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "Dim q = From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption Descending         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"q = From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "q = From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption Descending         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"q", "q"},
                {"From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "From c In cars         From ud In users_details         From bd In bids         Order By c.listingOption Descending         Where a.userID = ud.userid         Let images = From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"From c In cars", "From c In cars"},
                {"c In cars", "c In cars"},
                {"c", "c"},
                {"From ud In users_details", "From ud In users_details"},
                {"ud In users_details", "ud In users_details"},
                {"ud", "ud"},
                {"From bd In bids", "From bd In bids"},
                {"bd In bids", "bd In bids"},
                {"bd", "bd"},
                {"Order By c.listingOption", "Order By c.listingOption Descending"},
                {"c.listingOption", "c.listingOption Descending"},
                {"Where a.userID = ud.userid", "Where a.userID = ud.userid"},
                {"Let images = From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "Let images = From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"images = From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "images = From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"images", "images"},
                {"From ai In auction_images                         Where ai.belongs_to = c.id                         Select ai         Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()         Select bid",
                 "From ai In auction_images                         Where ai.belongs_to = c.id2                         Select ai + 1         Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()         Select bid"},
                {"From ai In auction_images", "From ai In auction_images"},
                {"ai In auction_images", "ai In auction_images"},
                {"ai", "ai"},
                {"Where ai.belongs_to = c.id", "Where ai.belongs_to = c.id2"},
                {"Select ai", "Select ai + 1"},
                {"ai", "ai + 1"},
                {"Let bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()",
                 "Let bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()"},
                {"bid = (From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount).FirstOrDefault()",
                 "bid = (From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount).FirstOrDefault()"},
                {"bid", "bid"},
                {"From b In bids                     Order By b.id                     Where b.carID = c.id                     Select b.bidamount",
                 "From b In bids                     Order By b.id Ascending                     Where b.carID = c.id2                     Select b.bidamount"},
                {"From b In bids", "From b In bids"},
                {"b In bids", "b In bids"},
                {"b", "b"},
                {"Order By b.id", "Order By b.id Ascending"},
                {"b.id", "b.id Ascending"},
                {"Where b.carID = c.id", "Where b.carID = c.id2"},
                {"Select b.bidamount", "Select b.bidamount"},
                {"b.bidamount", "b.bidamount"},
                {"Select bid", "Select bid"},
                {"bid", "bid"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchQueries3()
            Dim src1 = "
Dim q = From a In seq1
        Join c In seq2 On F(Function(u) u) Equals G(Function(s) s)
        Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)
        Select a
"

            Dim src2 = "
Dim q = From a In seq1
        Join c In seq2 On F(Function(u) u + 1) Equals G(Function(s) s + 3)
        Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)
        Select a + 1
"

            Dim match = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim q = From a In seq1         Join c In seq2 On F(Function(u) u) Equals G(Function(s) s)         Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)         Select a",
                 "Dim q = From a In seq1         Join c In seq2 On F(Function(u) u + 1) Equals G(Function(s) s + 3)         Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)         Select a + 1"},
                {"q = From a In seq1         Join c In seq2 On F(Function(u) u) Equals G(Function(s) s)         Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)         Select a",
                 "q = From a In seq1         Join c In seq2 On F(Function(u) u + 1) Equals G(Function(s) s + 3)         Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)         Select a + 1"},
                {"q", "q"},
                {"From a In seq1         Join c In seq2 On F(Function(u) u) Equals G(Function(s) s)         Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)         Select a",
                 "From a In seq1         Join c In seq2 On F(Function(u) u + 1) Equals G(Function(s) s + 3)         Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)         Select a + 1"},
                {"From a In seq1", "From a In seq1"},
                {"a In seq1", "a In seq1"},
                {"a", "a"},
                {"Join c In seq2 On F(Function(u) u) Equals G(Function(s) s)", "Join c In seq2 On F(Function(u) u + 1) Equals G(Function(s) s + 3)"},
                {"c In seq2", "c In seq2"},
                {"c", "c"},
                {"F(Function(u) u) Equals G(Function(s) s)", "F(Function(u) u + 1) Equals G(Function(s) s + 3)"},
                {"Function(u) u", "Function(u) u + 1"},
                {"Function(u)", "Function(u)"},
                {"(u)", "(u)"},
                {"u", "u"},
                {"u", "u"},
                {"Function(s) s", "Function(s) s + 3"},
                {"Function(s)", "Function(s)"},
                {"(s)", "(s)"},
                {"s", "s"},
                {"s", "s"},
                {"Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)", "Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)"},
                {"l In seq3", "l In seq3"},
                {"l", "l"},
                {"F(Function(v) v) Equals G(Function(t) t)", "F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)"},
                {"Function(v) v", "Function(vv) vv + 2"},
                {"Function(v)", "Function(vv)"},
                {"(v)", "(vv)"},
                {"v", "vv"},
                {"v", "vv"},
                {"Function(t) t", "Function(tt) tt + 4"},
                {"Function(t)", "Function(tt)"},
                {"(t)", "(tt)"},
                {"t", "tt"},
                {"t", "tt"},
                {"Select a", "Select a + 1"},
                {"a", "a + 1"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchQueries4()
            Dim src1 = "F(From a In b Group Join c In (Function() d)() On Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2) Into g = Group, h = Sum(Function(f) f + 1) Select g)"
            Dim src2 = "F(From a In b Group Join c In (Function() d + 1)() On Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2) Into g = Group, h = Sum(Function(f) f + 2) Select g)"
            Dim match = GetMethodMatches(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(From a In b Group Join c In (Function() d)() On Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2) Into g = Group, h = Sum(Function(f) f + 1) Select g)", "F(From a In b Group Join c In (Function() d + 1)() On Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2) Into g = Group, h = Sum(Function(f) f + 2) Select g)"},
                {"From a In b Group Join c In (Function() d)() On Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2) Into g = Group, h = Sum(Function(f) f + 1) Select g", "From a In b Group Join c In (Function() d + 1)() On Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2) Into g = Group, h = Sum(Function(f) f + 2) Select g"},
                {"From a In b", "From a In b"},
                {"a In b", "a In b"},
                {"a", "a"},
                {"Group Join c In (Function() d)() On Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2) Into g = Group, h = Sum(Function(f) f + 1)", "Group Join c In (Function() d + 1)() On Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2) Into g = Group, h = Sum(Function(f) f + 2)"},
                {"c In (Function() d)()", "c In (Function() d + 1)()"},
                {"c", "c"},
                {"Function() d", "Function() d + 1"},
                {"Function()", "Function()"},
                {"()", "()"},
                {"Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2)", "Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2)"},
                {"Function(e1) Function(e2) (e1 - e2)", "Function(e1) Function(e2) (e1 + e2)"},
                {"Function(e1)", "Function(e1)"},
                {"(e1)", "(e1)"},
                {"e1", "e1"},
                {"e1", "e1"},
                {"Function(e2) (e1 - e2)", "Function(e2) (e1 + e2)"},
                {"Function(e2)", "Function(e2)"},
                {"(e2)", "(e2)"},
                {"e2", "e2"},
                {"e2", "e2"},
                {"Function(f1) Function(f2) (f1 - f2)", "Function(f1) Function(f2) (f1 + f2)"},
                {"Function(f1)", "Function(f1)"},
                {"(f1)", "(f1)"},
                {"f1", "f1"},
                {"f1", "f1"},
                {"Function(f2) (f1 - f2)", "Function(f2) (f1 + f2)"},
                {"Function(f2)", "Function(f2)"},
                {"(f2)", "(f2)"},
                {"f2", "f2"},
                {"f2", "f2"},
                {"g", "g"},
                {"h", "h"},
                {"Sum(Function(f) f + 1)", "Sum(Function(f) f + 2)"},
                {"Function(f) f + 1", "Function(f) f + 2"},
                {"Function(f)", "Function(f)"},
                {"(f)", "(f)"},
                {"f", "f"},
                {"f", "f"},
                {"Select g", "Select g"},
                {"g", "g"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchQueries_Aggregate1()
            Dim src1 = "
Dim result = From a In {1}
             Aggregate b In {2} Join c In {3} Join d In {4} On d Equals c On c Equals b Skip 1 Where b > 0 Select b + 1 Into Count(Q(1)), Sum(Q(2))
"

            Dim src2 = "
Dim result = From a In {10}
             Aggregate b In {20} Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10 Where b*10 > 0 Skip 10 Select b*10 + 1 Into Count(Q(10)), Sum(Q(20))
"

            Dim match = GetMethodMatch(src1, src2)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Dim result = From a In {1}              Aggregate b In {2} Join c In {3} Join d In {4} On d Equals c On c Equals b Skip 1 Where b > 0 Select b + 1 Into Count(Q(1)), Sum(Q(2))", "Dim result = From a In {10}              Aggregate b In {20} Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10 Where b*10 > 0 Skip 10 Select b*10 + 1 Into Count(Q(10)), Sum(Q(20))"},
                {"result = From a In {1}              Aggregate b In {2} Join c In {3} Join d In {4} On d Equals c On c Equals b Skip 1 Where b > 0 Select b + 1 Into Count(Q(1)), Sum(Q(2))", "result = From a In {10}              Aggregate b In {20} Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10 Where b*10 > 0 Skip 10 Select b*10 + 1 Into Count(Q(10)), Sum(Q(20))"},
                {"result", "result"},
                {"From a In {1}              Aggregate b In {2} Join c In {3} Join d In {4} On d Equals c On c Equals b Skip 1 Where b > 0 Select b + 1 Into Count(Q(1)), Sum(Q(2))", "From a In {10}              Aggregate b In {20} Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10 Where b*10 > 0 Skip 10 Select b*10 + 1 Into Count(Q(10)), Sum(Q(20))"},
                {"From a In {1}", "From a In {10}"},
                {"a In {1}", "a In {10}"},
                {"a", "a"},
                {"Aggregate b In {2} Join c In {3} Join d In {4} On d Equals c On c Equals b Skip 1 Where b > 0 Select b + 1 Into Count(Q(1)), Sum(Q(2))", "Aggregate b In {20} Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10 Where b*10 > 0 Skip 10 Select b*10 + 1 Into Count(Q(10)), Sum(Q(20))"},
                {"b In {2}", "b In {20}"},
                {"b", "b"},
                {"Join c In {3} Join d In {4} On d Equals c On c Equals b", "Join c In {30} Join d In {40} On d*10 Equals c*10 On c*10 Equals b*10"},
                {"c In {3}", "c In {30}"},
                {"c", "c"},
                {"Join d In {4} On d Equals c", "Join d In {40} On d*10 Equals c*10"},
                {"d In {4}", "d In {40}"},
                {"d", "d"},
                {"d Equals c", "d*10 Equals c*10"},
                {"c Equals b", "c*10 Equals b*10"},
                {"Skip 1", "Skip 10"},
                {"Where b > 0", "Where b*10 > 0"},
                {"Select b + 1", "Select b*10 + 1"},
                {"b + 1", "b*10 + 1"},
                {"Count(Q(1))", "Count(Q(10))"},
                {"Sum(Q(2))", "Sum(Q(20))"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchYields()
            Dim src1 = "
Yield 1
Yield 2
For Each x In {1, 2, 3}
    Yield 3
Next"

            Dim src2 = "
Yield 1
Yield 3
For Each x In {1, 2, 3}
    Yield 2
Next"
            Dim match = GetMethodMatches(src1, src2, kind:=MethodKind.Iterator)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Iterator Function F() As IEnumerable(Of Integer)", "Iterator Function F() As IEnumerable(Of Integer)"},
                {"Yield 1", "Yield 1"},
                {"Yield 2", "Yield 2"},
                {"For Each x In {1, 2, 3}     Yield 3 Next", "For Each x In {1, 2, 3}     Yield 2 Next"},
                {"For Each x In {1, 2, 3}", "For Each x In {1, 2, 3}"},
                {"Yield 3", "Yield 3"},
                {"Next", "Next"},
                {"End Function", "End Function"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub MatchExceptionHandlers()
            Dim src1 = "
Try
    Throw New InvalidOperationException()
Catch e As IOException When filter(e)
    Console.WriteLine(1)
Catch e As Exception When filter(e)
    Console.WriteLine(2)
End Try
"
            Dim src2 = "
Try
    Throw New InvalidOperationException()
Catch e As IOException When filter(e)
    Console.WriteLine(1)
Catch e As Exception When filter(e)
    Console.WriteLine(2)
End Try
"

            Dim match = GetMethodMatches(src1, src2, kind:=MethodKind.Regular)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Try     Throw New InvalidOperationException() Catch e As IOException When filter(e)     Console.WriteLine(1) Catch e As Exception When filter(e)     Console.WriteLine(2) End Try", "Try     Throw New InvalidOperationException() Catch e As IOException When filter(e)     Console.WriteLine(1) Catch e As Exception When filter(e)     Console.WriteLine(2) End Try"},
                {"Try", "Try"},
                {"Throw New InvalidOperationException()", "Throw New InvalidOperationException()"},
                {"Catch e As IOException When filter(e)     Console.WriteLine(1)", "Catch e As IOException When filter(e)     Console.WriteLine(1)"},
                {"Catch e As IOException When filter(e)", "Catch e As IOException When filter(e)"},
                {"When filter(e)", "When filter(e)"},
                {"Console.WriteLine(1)", "Console.WriteLine(1)"},
                {"Catch e As Exception When filter(e)     Console.WriteLine(2)", "Catch e As Exception When filter(e)     Console.WriteLine(2)"},
                {"Catch e As Exception When filter(e)", "Catch e As Exception When filter(e)"},
                {"When filter(e)", "When filter(e)"},
                {"Console.WriteLine(2)", "Console.WriteLine(2)"},
                {"End Try", "End Try"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub KnownMatches()
            Dim src1 = "Console.WriteLine(1   ) : Console.WriteLine( 1  )"
            Dim src2 = "Console.WriteLine(  1 ) : Console.WriteLine(   1)"

            Dim m1 = MakeMethodBody(src1)
            Dim m2 = MakeMethodBody(src2)
            Dim b1 = DirectCast(m1.RootNodes.First(), MethodBlockSyntax)
            Dim b2 = DirectCast(m2.RootNodes.First(), MethodBlockSyntax)
            Dim knownMatches = {New KeyValuePair(Of SyntaxNode, SyntaxNode)(b1.Statements(1), b2.Statements(0))}

            ' pre-matched:
            Dim match = m1.ComputeSingleRootMatch(m2, knownMatches)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Console.WriteLine(1   )", "Console.WriteLine(   1)"},
                {"Console.WriteLine( 1  )", "Console.WriteLine(  1 )"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)

            ' not pre-matched:
            match = m1.ComputeSingleRootMatch(m2, knownMatches:=Nothing)
            actual = ToMatchingPairs(match)

            expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Console.WriteLine(1   )", "Console.WriteLine(  1 )"},
                {"Console.WriteLine( 1  )", "Console.WriteLine(   1)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub StringLiteral_update()
            Dim src1 = "Dim a = ""Hello1"""
            Dim src2 = "Dim a = ""Hello2"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = ""Hello1""]@12 -> [a = ""Hello2""]@12")
        End Sub

        <Fact>
        Public Sub InterpolatedStringText_update()
            Dim src1 = "Dim a = $""Hello1"""
            Dim src2 = "Dim a = $""Hello2"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello1""]@12 -> [a = $""Hello2""]@12")
        End Sub

        <Fact>
        Public Sub Interpolation_update()
            Dim src1 = "Dim a = $""Hello{123}"""
            Dim src2 = "Dim a = $""Hello{124}"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello{123}""]@12 -> [a = $""Hello{124}""]@12")
        End Sub

        <Fact>
        Public Sub InterpolationFormatClause_update()
            Dim src1 = "Dim a = $""Hello{123:N1}"""
            Dim src2 = "Dim a = $""Hello{123:N2}"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello{123:N1}""]@12 -> [a = $""Hello{123:N2}""]@12")
        End Sub

        <Fact>
        Public Sub AwaitExpressions()
            Dim src1 = "
F(Await x, Await y)
"
            Dim src2 = "
F(Await y, Await x)
"
            Dim match = GetMethodMatches(src1, src2, kind:=MethodKind.Async)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Async Function F() As Task(Of Integer)", "Async Function F() As Task(Of Integer)"},
                {"F(Await x, Await y)", "F(Await y, Await x)"},
                {"Await x", "Await x"},
                {"Await y", "Await y"},
                {"End Function", "End Function"}
            }

            expected.AssertEqual(actual)
        End Sub

        <Fact>
        Public Sub For_ForEach_Using()
            Dim src1 = "
For Each a As Integer In {1}
Next

Using b As New C(1)
End Using

For c As Integer = 0 To 1
Next
"
            Dim src2 = "
For Each a As Integer In {2}
Next

Using b As New C(2)
End Using

For c As Integer = 0 To 2
Next
"
            Dim match = GetMethodMatches(src1, src2, kind:=MethodKind.Regular)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"For Each a As Integer In {1} Next", "For Each a As Integer In {2} Next"},
                {"For Each a As Integer In {1}", "For Each a As Integer In {2}"},
                {"a As Integer", "a As Integer"},
                {"a", "a"},
                {"Next", "Next"},
                {"Using b As New C(1) End Using", "Using b As New C(2) End Using"},
                {"Using b As New C(1)", "Using b As New C(2)"},
                {"b As New C(1)", "b As New C(2)"},
                {"b", "b"},
                {"End Using", "End Using"},
                {"For c As Integer = 0 To 1 Next", "For c As Integer = 0 To 2 Next"},
                {"For c As Integer = 0 To 1", "For c As Integer = 0 To 2"},
                {"c As Integer", "c As Integer"},
                {"c", "c"},
                {"Next", "Next"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub
    End Class
End Namespace
