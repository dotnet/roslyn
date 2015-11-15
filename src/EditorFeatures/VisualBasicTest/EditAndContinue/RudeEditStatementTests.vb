' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public Class RudeEditStatementTests
        Inherits RudeEditTestBase

#Region "Matching"

        <WpfFact>
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

        <WpfFact>
        Public Sub KnownMatches_Root()
            Dim src1 As String = "Console.WriteLine(1)"
            Dim src2 As String = "Console.WriteLine(2)"

            Dim m1 = MakeMethodBody(src1)
            Dim m2 = MakeMethodBody(src2)

            Dim knownMatches = {New KeyValuePair(Of SyntaxNode, SyntaxNode)(m1, m2)}
            Dim match = StatementSyntaxComparer.Default.ComputeMatch(m1, m2, knownMatches)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"Console.WriteLine(1)", "Console.WriteLine(2)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
        Public Sub MatchMethodBodiesWithQueries_From3()
            Dim src1 = "Dim result = From a In {Await F(0)}, b In {Q(Async Function() Await F(2))} Select a + b" & vbLf
            Dim src2 = "Dim result = From a In {Await F(1)}, b In {Q(Async Function() Await F(3))} Select a - b" & vbLf

            Dim match = GetMethodMatch(src1, src2, stateMachine:=StateMachineKind.Async)
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

        <WpfFact>
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

        <WpfFact>
        Public Sub MatchMethodBodiesWithQueries_Aggregate2()
            Dim src1 = "Dim result = From q in {0} Aggregate b In Q(Function() 1) Join c In Q(Function() 3) On c Equals b Skip Q(Function() 5) Select b Into Count()" & vbLf
            Dim src2 = "Dim result = From q in {0} Aggregate b In Q(Function() 2) Join c In Q(Function() 4) On c Equals b Skip Q(Function() 6) Select b Into Count()" & vbLf

            ' TODO
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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
                {"Function(b) Function(c) d", "Function(b) Function(c) d"},
                {"Function(b)", "Function(b)"},
                {"Function(c) d", "Function(c) d"},
                {"Function(c)", "Function(c)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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
                {"Function(b) Function(c) d", "Function(b) H(Function(c) d)"},
                {"Function(b)", "Function(b)"},
                {"Function(c) d", "Function(c) d"},
                {"Function(c)", "Function(c)"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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
                {"F(Function(c) d)", "F(Function(c) d + 1)"},
                {"Function(c) d", "Function(c) d + 1"},
                {"Function(c)", "Function(c)"},
                {"F(Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub)",
                 "F(Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub)"},
                {"Sub(u, v)         F(Function(w) Function(c)  d )         F(Function(p) p)       End Sub",
                 "Sub(u, v)         F(Function(w) Function(c) d  +  1)         F(Function(p) p*2)       End Sub"},
                {"Sub(u, v)", "Sub(u, v)"},
                {"F(Function(w) Function(c)  d )", "F(Function(w) Function(c) d  +  1)"},
                {"Function(w) Function(c)  d", "Function(w) Function(c) d  +  1"},
                {"Function(w)", "Function(w)"},
                {"Function(c)  d", "Function(c) d  +  1"},
                {"Function(c)", "Function(c)"},
                {"F(Function(p) p)", "F(Function(p) p*2)"},
                {"Function(p) p", "Function(p) p*2"},
                {"Function(p)", "Function(p)"},
                {"End Sub", "End Sub"},
                {"End Sub", "End Sub"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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
                {"Function(s) s", "Function(s) s + 3"},
                {"Function(s)", "Function(s)"},
                {"Join l In seq3 On F(Function(v) v) Equals G(Function(t) t)", "Join l In seq3 On F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)"},
                {"l In seq3", "l In seq3"},
                {"l", "l"},
                {"F(Function(v) v) Equals G(Function(t) t)", "F(Function(vv) vv + 2) Equals G(Function(tt) tt + 4)"},
                {"Function(v) v", "Function(vv) vv + 2"},
                {"Function(v)", "Function(vv)"},
                {"Function(t) t", "Function(tt) tt + 4"},
                {"Function(t)", "Function(tt)"},
                {"Select a", "Select a + 1"},
                {"a", "a + 1"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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
                {"Function(e1) Function(e2) (e1 - e2) Equals Function(f1) Function(f2) (f1 - f2)", "Function(e1) Function(e2) (e1 + e2) Equals Function(f1) Function(f2) (f1 + f2)"},
                {"Function(e1) Function(e2) (e1 - e2)", "Function(e1) Function(e2) (e1 + e2)"},
                {"Function(e1)", "Function(e1)"},
                {"Function(e2) (e1 - e2)", "Function(e2) (e1 + e2)"},
                {"Function(e2)", "Function(e2)"},
                {"Function(f1) Function(f2) (f1 - f2)", "Function(f1) Function(f2) (f1 + f2)"},
                {"Function(f1)", "Function(f1)"},
                {"Function(f2) (f1 - f2)", "Function(f2) (f1 + f2)"},
                {"Function(f2)", "Function(f2)"},
                {"g", "g"},
                {"h", "h"},
                {"Sum(Function(f) f + 1)", "Sum(Function(f) f + 2)"},
                {"Function(f) f + 1", "Function(f) f + 2"},
                {"Function(f)", "Function(f)"},
                {"Select g", "Select g"},
                {"g", "g"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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

        <WpfFact>
        Public Sub MatchYields()
            Dim src1 = <text>
Yield 1
Yield 2
For Each x In {1, 2, 3}
    Yield 3
Next
</text>.Value

            Dim src2 = <text>
Yield 1
Yield 3
For Each x In {1, 2, 3}
    Yield 2
Next
</text>.Value
            Dim match = GetMethodMatches(src1, src2, stateMachine:=StateMachineKind.Iterator)
            Dim actual = ToMatchingPairs(match)

            Dim expected = New MatchingPairs From
            {
                {"Iterator Function F() As IEnumerable(Of Integer)", "Iterator Function F() As IEnumerable(Of Integer)"},
                {"Yield 1", "Yield 1"},
                {"Yield 2", "Yield 3"},
                {"For Each x In {1, 2, 3}     Yield 3 Next", "For Each x In {1, 2, 3}     Yield 2 Next"},
                {"For Each x In {1, 2, 3}", "For Each x In {1, 2, 3}"},
                {"Yield 3", "Yield 2"},
                {"Next", "Next"},
                {"End Function", "End Function"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
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

            Dim match = GetMethodMatches(src1, src2, stateMachine:=StateMachineKind.None)
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

        <WpfFact>
        Public Sub KnownMatches()
            Dim src1 = "Console.WriteLine(1   ) : Console.WriteLine( 1  )"
            Dim src2 = "Console.WriteLine(  1 ) : Console.WriteLine(   1)"

            Dim m1 = DirectCast(MakeMethodBody(src1), MethodBlockSyntax)
            Dim m2 = DirectCast(MakeMethodBody(src2), MethodBlockSyntax)
            Dim knownMatches = {New KeyValuePair(Of SyntaxNode, SyntaxNode)(m1.Statements(1), m2.Statements(0))}

            ' pre-matched:
            Dim match = StatementSyntaxComparer.Default.ComputeMatch(m1, m2, knownMatches)
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
            match = StatementSyntaxComparer.Default.ComputeMatch(m1, m2)
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

        <WpfFact>
        Public Sub StringLiteral_update()
            Dim src1 = "Dim a = ""Hello1"""
            Dim src2 = "Dim a = ""Hello2"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = ""Hello1""]@12 -> [a = ""Hello2""]@12")
        End Sub

        <WpfFact>
        Public Sub InterpolatedStringText_update()
            Dim src1 = "Dim a = $""Hello1"""
            Dim src2 = "Dim a = $""Hello2"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello1""]@12 -> [a = $""Hello2""]@12")
        End Sub

        <WpfFact>
        Public Sub Interpolation_update()
            Dim src1 = "Dim a = $""Hello{123}"""
            Dim src2 = "Dim a = $""Hello{124}"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello{123}""]@12 -> [a = $""Hello{124}""]@12")
        End Sub

        <WpfFact>
        Public Sub InterpolationFormatClause_update()
            Dim src1 = "Dim a = $""Hello{123:N1}"""
            Dim src2 = "Dim a = $""Hello{123:N2}"""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits("Update [a = $""Hello{123:N1}""]@12 -> [a = $""Hello{123:N2}""]@12")
        End Sub

#End Region

#Region "Misc"
        <WpfFact>
        Public Sub VariableDeclaration_Insert()
            Dim src1 = "If x = 1 : x += 1 : End If"
            Dim src2 = "Dim x = 1 : If x = 1 : x += 1 : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Dim x = 1]@8",
                "Insert [x = 1]@12",
                "Insert [x]@12")
        End Sub

        <WpfFact>
        Public Sub VariableDeclaration_Update()
            Dim src1 = "Dim x = F(1), y = G(2)"
            Dim src2 = "Dim x = F(3), y = G(4)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [x = F(1)]@12 -> [x = F(3)]@12",
                "Update [y = G(2)]@22 -> [y = G(4)]@22")
        End Sub

        <WpfFact>
        Public Sub Redim1()
            Dim src1 = "ReDim Preserve a(F(Function() 1), 10, 20)"
            Dim src2 = "ReDim a(F(Function() 2), 1, 2)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ReDim Preserve a(F(Function() 1), 10, 20)]@8 -> [ReDim a(F(Function() 2), 1, 2)]@8",
                "Update [a(F(Function() 1), 10, 20)]@23 -> [a(F(Function() 2), 1, 2)]@14",
                "Update [Function() 1]@27 -> [Function() 2]@18")
        End Sub

        <WpfFact>
        Public Sub Assignments()
            Dim src1 = "a = F(Function() 1) : " &
                       "a += F(Function() 2) : " &
                       "a -= F(Function() 3) : " &
                       "a *= F(Function() 4) : " &
                       "a /= F(Function() 5) : " &
                       "a \= F(Function() 6) : " &
                       "a ^= F(Function() 7) : " &
                       "a <<= F(Function() 8) : " &
                       "a >>= F(Function() 9) : " &
                       "a &= F(Function() 10) : " &
                       "Mid(s, F(Function() 11), 1) = F(Function() ""a"")"

            Dim src2 = "a = F(Function() 100) : " &
                       "a += F(Function() 200) : " &
                       "a -= F(Function() 300) : " &
                       "a *= F(Function() 400) : " &
                       "a /= F(Function() 500) : " &
                       "a \= F(Function() 600) : " &
                       "a ^= F(Function() 700) : " &
                       "a <<= F(Function() 800) : " &
                       "a >>= F(Function() 900) : " &
                       "a &= F(Function() 1000) : " &
                       "Mid(s, F(Function() 1100), 1) = F(Function() ""b"")"

            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Function() 1]@14 -> [Function() 100]@14",
                "Update [Function() 2]@37 -> [Function() 200]@39",
                "Update [Function() 3]@60 -> [Function() 300]@64",
                "Update [Function() 4]@83 -> [Function() 400]@89",
                "Update [Function() 5]@106 -> [Function() 500]@114",
                "Update [Function() 6]@129 -> [Function() 600]@139",
                "Update [Function() 7]@152 -> [Function() 700]@164",
                "Update [Function() 8]@176 -> [Function() 800]@190",
                "Update [Function() 9]@200 -> [Function() 900]@216",
                "Update [Function() 10]@223 -> [Function() 1000]@241",
                "Update [Function() 11]@249 -> [Function() 1100]@269",
                "Update [Function() ""a""]@272 -> [Function() ""b""]@294")
        End Sub

        <WpfFact>
        Public Sub EventStatements()
            Dim src1 = "AddHandler e, Function(f) f : RemoveHandler e, Function(f) f : RaiseEvent e()"
            Dim src2 = "RemoveHandler e, Function(f) (f + 1) : AddHandler e, Function(f) f : RaiseEvent e()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [RemoveHandler e, Function(f) f]@38 -> @8",
                "Update [Function(f) f]@55 -> [Function(f) (f + 1)]@25")
        End Sub

        <WpfFact>
        Public Sub ExpressionStatements()
            Dim src1 = "Call F(Function(a) a)"
            Dim src2 = "F(Function(a) (a + 1))"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Call F(Function(a) a)]@8 -> [F(Function(a) (a + 1))]@8",
                "Update [Function(a) a]@15 -> [Function(a) (a + 1)]@10")
        End Sub

        <WpfFact>
        Public Sub ThrowReturn()
            Dim src1 = "Throw F(Function(a) a) : Return Function(b) b"
            Dim src2 = "Throw F(Function(b) b) : Return Function(a) a"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Function(b) b]@40 -> @16",
                "Move [Function(a) a]@16 -> @40")
        End Sub

        <WpfFact>
        Public Sub OnErrorGoToLabel()
            Dim src1 = "On Error GoTo ErrorHandler : Exit Sub : On Error GoTo label1 : " & vbLf & "label1:" & vbLf & "Resume Next"
            Dim src2 = "On Error GoTo -1 : On Error GoTo 0 : Exit Sub : " & vbLf & "label2:" & vbLf & "Resume"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [On Error GoTo label1]@48 -> @8",
                "Update [On Error GoTo label1]@48 -> [On Error GoTo -1]@8",
                "Update [On Error GoTo ErrorHandler]@8 -> [On Error GoTo 0]@27",
                "Update [label1:]@72 -> [label2:]@57",
                "Update [Resume Next]@80 -> [Resume]@65")
        End Sub

#End Region

#Region "Select"

        <WpfFact>
        Public Sub Select_Reorder1()
            Dim src1 = "Select Case a : Case 1 : f() : End Select : " &
                       "Select Case b : Case 2 : g() : End Select"

            Dim src2 = "Select Case b : Case 2 : f() : End Select : " &
                       "Select Case a : Case 1 : g() : End Select"

            Dim edits = GetMethodEdits(src1, src2)
            edits.VerifyEdits(
                "Reorder [Select Case b : Case 2 : g() : End Select]@52 -> @8",
                "Move [f()]@33 -> @33",
                "Move [g()]@77 -> @77")
        End Sub

        <WpfFact>
        Public Sub Select_Case_Reorder()
            Dim src1 = "Select Case expr : Case 1 :      f() : Case 2, 3, 4 : g() : End Select"
            Dim src2 = "Select Case expr : Case 2, 3, 4: g() : Case 1       : f() : End Select"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Case 2, 3, 4 : g()]@47 -> @27")
        End Sub

        <WpfFact>
        Public Sub Select_Case_Update()
            Dim src1 = "Select Case expr : Case 1 : f() : End Select"
            Dim src2 = "Select Case expr : Case 2 : f() : End Select"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [1]@32 -> [2]@32")
        End Sub

#End Region

#Region "Try, Catch, Finally"

        <WpfFact>
        Public Sub TryInsert1()
            Dim src1 = "x += 1"
            Dim src2 = "Try : x += 1 : Catch : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Try : x += 1 : Catch : End Try]@8",
                "Insert [Try]@8",
                "Move [x += 1]@8 -> @14",
                "Insert [Catch]@23",
                "Insert [End Try]@31",
                "Insert [Catch]@23")
        End Sub

        <WpfFact>
        Public Sub TryDelete1()
            Dim src1 = "Try : x += 1 : Catch : End Try"
            Dim src2 = "x += 1"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [x += 1]@14 -> @8",
                "Delete [Try : x += 1 : Catch : End Try]@8",
                "Delete [Try]@8",
                "Delete [Catch]@23",
                "Delete [Catch]@23",
                "Delete [End Try]@31")
        End Sub

        <WpfFact>
        Public Sub TryReorder()
            Dim src1 = "Try : x += 1 : Catch :  End Try : Try : y += 1 : Catch :::  End Try"
            Dim src2 = "Try : y += 1 : Catch :: End Try : Try : x += 1 : Catch :::: End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Try : y += 1 : Catch :::  End Try]@42 -> @8")
        End Sub

        <WpfFact>
        Public Sub Finally_DeleteHeader()
            Dim src1 = "Try : Catch e AS E1 : Finally : End Try"
            Dim src2 = "Try : Catch e AS E1 : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Finally]@30",
                "Delete [Finally]@30")
        End Sub

        <WpfFact>
        Public Sub Finally_InsertHeader()
            Dim src1 = "Try : Catch e AS E1 : End Try"
            Dim src2 = "Try : Catch e AS E1 : Finally : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Finally]@30",
                "Insert [Finally]@30")
        End Sub

        <WpfFact>
        Public Sub CatchUpdate()
            Dim src1 = "Try : Catch e As Exception : End Try"
            Dim src2 = "Try : Catch e As IOException : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Catch e As Exception]@14 -> [Catch e As IOException]@14")
        End Sub

        <WpfFact>
        Public Sub WhenUpdate()
            Dim src1 = "Try : Catch e As Exception When e.Message = ""a"" : End Try"
            Dim src2 = "Try : Catch e As Exception When e.Message = ""b"" : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [When e.Message = ""a""]@35 -> [When e.Message = ""b""]@35")
        End Sub

        <WpfFact>
        Public Sub WhenCatchUpdate()
            Dim src1 = "Try : Catch e As Exception When e.Message = ""a"" : End Try"
            Dim src2 = "Try : Catch e As IOException When e.Message = ""a"" : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Catch e As Exception When e.Message = ""a""]@14 -> [Catch e As IOException When e.Message = ""a""]@14")
        End Sub

        <WpfFact>
        Public Sub CatchInsert()
            Dim src1 = "Try : Catch e As Exception : End Try"
            Dim src2 = "Try : Catch e As IOException : Catch e As Exception : End Try"

            Dim edits = GetMethodEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Catch e As IOException]@14",
                "Insert [Catch e As IOException]@14")
        End Sub

        <WpfFact>
        Public Sub WhenInsert()
            Dim src1 = "Try : Catch e As Exception : End Try"
            Dim src2 = "Try : Catch e As Exception When e.Message = ""a"" : End Try"

            Dim edits = GetMethodEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [When e.Message = ""a""]@35")
        End Sub

        <WpfFact>
        Public Sub WhenDelete()
            Dim src1 = "Try : Catch e As Exception When e.Message = ""a"" : End Try"
            Dim src2 = "Try : Catch e As Exception : End Try"

            Dim edits = GetMethodEdits(src1, src2)
            edits.VerifyEdits(
                "Delete [When e.Message = ""a""]@35")
        End Sub

        <WpfFact>
        Public Sub CatchBodyUpdate()
            Dim src1 = "Try : Catch e As E : x += 1 : End Try"
            Dim src2 = "Try : Catch e As E : y += 1 : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [x += 1]@29 -> [y += 1]@29")
        End Sub

        <WpfFact>
        Public Sub CatchDelete()
            Dim src1 = "Try : Catch e As IOException : Catch e As Exception : End Try"
            Dim src2 = "Try : Catch e As IOException : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Catch e As Exception]@39",
                "Delete [Catch e As Exception]@39")
        End Sub

        <WpfFact>
        Public Sub CatchReorder()
            Dim src1 = "Try : Catch e As IOException : Catch e As Exception : End Try"
            Dim src2 = "Try : Catch e As Exception : Catch e As IOException : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Catch e As Exception]@39 -> @14")
        End Sub

        <WpfFact>
        Public Sub CatchInsertDelete()
            Dim src1 = "Try : x += 1 : Catch e As E : Catch e As Exception : End Try : " &
                       "Try : Console.WriteLine() : Finally : End Try"

            Dim src2 = "Try : x += 1 : Catch e As Exception : End Try : " &
                       "Try : Console.WriteLine() : Catch e As E : Finally : End Try"

            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Catch e As E]@84",
                "Insert [Catch e As E]@84",
                "Delete [Catch e As E]@23",
                "Delete [Catch e As E]@23")
        End Sub

        <WpfFact>
        Public Sub Catch_DeleteHeader1()
            Dim src1 = "Try : Catch e As E1 : Catch e As E2 : End Try"
            Dim src2 = "Try : Catch e As E1 : End Try"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Catch e As E2]@30",
                "Delete [Catch e As E2]@30")
        End Sub
#End Region

#Region "With"
        <WpfFact>
        Public Sub WithBlock_Insert()
            Dim src1 = ""
            Dim src2 = "With a : F(.x) : End With"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [With a : F(.x) : End With]@8",
                "Insert [With a]@8",
                "Insert [F(.x)]@17",
                "Insert [End With]@25")
        End Sub

        <WpfFact>
        Public Sub WithBlock_Delete()
            Dim src1 = "With a : F(.x) : End With"
            Dim src2 = ""
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [With a : F(.x) : End With]@8",
                "Delete [With a]@8",
                "Delete [F(.x)]@17",
                "Delete [End With]@25")
        End Sub

        <WpfFact>
        Public Sub WithBlock_Reorder()
            Dim src1 = "With a : F(.x) : End With  :  With a : F(.y) : End With"
            Dim src2 = "With a : F(.y) : End With  :  With a : F(.x) : End With"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [With a : F(.y) : End With]@38 -> @8")
        End Sub
#End Region

#Region "Using"
        <WpfFact>
        Public Sub Using1()
            Dim src1 As String = "Using a : Using b : Foo() : End Using : End Using"
            Dim src2 As String = "Using a : Using c : Using b : Foo() : End Using : End Using : End Using"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Using c : Using b : Foo() : End Using : End Using]@18",
                "Insert [Using c]@18",
                "Move [Using b : Foo() : End Using]@18 -> @28",
                "Insert [End Using]@58")
        End Sub

        <WpfFact>
        Public Sub Using_DeleteHeader()
            Dim src1 As String = "Using a : Foo() : End Using"
            Dim src2 As String = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@18 -> @8",
                "Delete [Using a : Foo() : End Using]@8",
                "Delete [Using a]@8",
                "Delete [End Using]@26")
        End Sub

        <WpfFact>
        Public Sub Using_InsertHeader()
            Dim src1 As String = "Foo()"
            Dim src2 As String = "Using a : Foo() : End Using"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Using a : Foo() : End Using]@8",
                "Insert [Using a]@8",
                "Move [Foo()]@8 -> @18",
                "Insert [End Using]@26")
        End Sub
#End Region

#Region "SyncLock"
        <WpfFact>
        Public Sub SyncLock1()
            Dim src1 As String = "SyncLock a : SyncLock b : Foo() : End SyncLock : End SyncLock"
            Dim src2 As String = "SyncLock a : SyncLock c : SyncLock b : Foo() : End SyncLock : End SyncLock : End SyncLock"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [SyncLock c : SyncLock b : Foo() : End SyncLock : End SyncLock]@21",
                "Insert [SyncLock c]@21",
                "Move [SyncLock b : Foo() : End SyncLock]@21 -> @34",
                "Insert [End SyncLock]@70")
        End Sub

        <WpfFact>
        Public Sub SyncLock_DeleteHeader()
            Dim src1 As String = "SyncLock a : Foo() : End SyncLock"
            Dim src2 As String = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@21 -> @8",
                "Delete [SyncLock a : Foo() : End SyncLock]@8",
                "Delete [SyncLock a]@8",
                "Delete [End SyncLock]@29")
        End Sub

        <WpfFact>
        Public Sub SyncLock_InsertHeader()
            Dim src1 As String = "Foo()"
            Dim src2 As String = "SyncLock a : Foo() : End SyncLock"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [SyncLock a : Foo() : End SyncLock]@8",
                "Insert [SyncLock a]@8",
                "Move [Foo()]@8 -> @21",
                "Insert [End SyncLock]@29")
        End Sub
#End Region

#Region "For Each"
        <WpfFact>
        Public Sub ForEach1()
            Dim src1 As String = "For Each a In e : For Each b In f : Foo() : Next : Next"
            Dim src2 As String = "For Each a In e : For Each c In g : For Each b In f : Foo() : Next : Next : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [For Each c In g : For Each b In f : Foo() : Next : Next]@26",
                "Insert [For Each c In g]@26",
                "Move [For Each b In f : Foo() : Next]@26 -> @44",
                "Insert [Next]@77")

            Dim actual = ToMatchingPairs(edits.Match)
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"For Each a In e : For Each b In f : Foo() : Next : Next",
                 "For Each a In e : For Each c In g : For Each b In f : Foo() : Next : Next : Next"},
                {"For Each a In e",
                 "For Each a In e"},
                {"For Each b In f : Foo() : Next",
                 "For Each b In f : Foo() : Next"},
                {"For Each b In f",
                 "For Each b In f"},
                {"Foo()", "Foo()"},
                {"Next", "Next"},
                {"Next", "Next"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
        Public Sub ForEach_Swap1()
            Dim src1 As String = "For Each a In e : For Each b In f : Foo() : Next : Next"
            Dim src2 As String = "For Each b In f : For Each a In e : Foo() : Next : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [For Each b In f : Foo() : Next]@26 -> @8",
                "Move [For Each a In e : For Each b In f : Foo() : Next : Next]@8 -> @26",
                "Move [Foo()]@44 -> @44")

            Dim actual = ToMatchingPairs(edits.Match)
            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"For Each a In e : For Each b In f : Foo() : Next : Next", "For Each a In e : Foo() : Next"},
                {"For Each a In e", "For Each a In e"},
                {"For Each b In f : Foo() : Next", "For Each b In f : For Each a In e : Foo() : Next : Next"},
                {"For Each b In f", "For Each b In f"},
                {"Foo()", "Foo()"},
                {"Next", "Next"},
                {"Next", "Next"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)
        End Sub

        <WpfFact>
        Public Sub Foreach_DeleteHeader()
            Dim src1 As String = "For Each a In b : Foo() : Next"
            Dim src2 As String = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@26 -> @8",
                "Delete [For Each a In b : Foo() : Next]@8",
                "Delete [For Each a In b]@8",
                "Delete [Next]@34")
        End Sub

        <WpfFact>
        Public Sub Foreach_InsertHeader()
            Dim src1 As String = "Foo()"
            Dim src2 As String = "For Each a In b : Foo() : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [For Each a In b : Foo() : Next]@8",
                "Insert [For Each a In b]@8",
                "Move [Foo()]@8 -> @26",
                "Insert [Next]@34")
        End Sub
#End Region

#Region "For"
        <WpfFact>
        Public Sub For1()
            Dim src1 = "For a = 0 To 10 : For a = 0 To 20 : Foo() : Next : Next"
            Dim src2 = "For a = 0 To 10 : For b = 0 To 10 : For a = 0 To 20 : Foo() : Next : Next : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [For b = 0 To 10 : For a = 0 To 20 : Foo() : Next : Next]@26",
                "Insert [For b = 0 To 10]@26",
                "Move [For a = 0 To 20 : Foo() : Next]@26 -> @44",
                "Insert [Next]@77")
        End Sub

        <WpfFact>
        Public Sub For2()
            Dim src1 = "For a = 0 To 10 Step 1 : For a = 0 To 20 : Foo() : Next : Next"
            Dim src2 = "For a = 0 To 10 Step 2 : For b = 0 To 10 Step 4 : For a = 0 To 20 Step 5 : Foo() : Next : Next : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [For b = 0 To 10 Step 4 : For a = 0 To 20 Step 5 : Foo() : Next : Next]@33",
                "Update [Step 1]@24 -> [Step 2]@24",
                "Insert [For b = 0 To 10 Step 4]@33",
                "Move [For a = 0 To 20 : Foo() : Next]@33 -> @58",
                "Insert [Next]@98",
                "Insert [Step 4]@49",
                "Insert [Step 5]@74")
        End Sub

        <WpfFact>
        Public Sub For_DeleteHeader()
            Dim src1 As String = "For a = 0 To 10 : Foo() : Next"
            Dim src2 As String = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@26 -> @8",
                "Delete [For a = 0 To 10 : Foo() : Next]@8",
                "Delete [For a = 0 To 10]@8",
                "Delete [Next]@34")
        End Sub

        <WpfFact>
        Public Sub For_DeleteStep()
            Dim src1 As String = "For a = 0 To 10 Step 1 : Foo() : Next"
            Dim src2 As String = "For a = 0 To 10 : Foo() : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Step 1]@24")
        End Sub

        <WpfFact>
        Public Sub For_InsertStep()
            Dim src1 As String = "For a = 0 To 10 : Foo() : Next"
            Dim src2 As String = "For a = 0 To 10 Step 1 : Foo() : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Step 1]@24")
        End Sub

        <WpfFact>
        Public Sub For_InsertHeader()
            Dim src1 As String = "Foo()"
            Dim src2 As String = "For a = 0 To 10 : Foo() : Next"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [For a = 0 To 10 : Foo() : Next]@8",
                "Insert [For a = 0 To 10]@8",
                "Move [Foo()]@8 -> @26",
                "Insert [Next]@34")
        End Sub
#End Region

#Region "Do, While, Loop"
        <WpfFact>
        Public Sub While1()
            Dim src1 As String = "While a : While b : Foo() : End While : End While"
            Dim src2 As String = "While a : While c : While b : Foo() : End While : End While : End While"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [While c : While b : Foo() : End While : End While]@18",
                "Insert [While c]@18",
                "Move [While b : Foo() : End While]@18 -> @28",
                "Insert [End While]@58")
        End Sub

        <WpfFact>
        Public Sub DoWhile1()
            Dim src1 As String = "While a : While b : Foo() : End While : End While"
            Dim src2 As String = "Do While a : While c : Do Until b : Foo() : Loop : End While : Loop"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [While a : While b : Foo() : End While : End While]@8 -> [Do While a : While c : Do Until b : Foo() : Loop : End While : Loop]@8",
                "Update [While a]@8 -> [Do While a]@8",
                "Insert [While c : Do Until b : Foo() : Loop : End While]@21",
                "Update [End While]@48 -> [Loop]@71",
                "Insert [While a]@11",
                "Insert [While c]@21",
                "Update [While b : Foo() : End While]@18 -> [Do Until b : Foo() : Loop]@31",
                "Move [While b : Foo() : End While]@18 -> @31",
                "Insert [End While]@59",
                "Update [While b]@18 -> [Do Until b]@31",
                "Update [End While]@36 -> [Loop]@52",
                "Insert [Until b]@34")
        End Sub

        <WpfFact>
        Public Sub While_DeleteHeader()
            Dim src1 As String = "While a : Foo() : End While"
            Dim src2 As String = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@18 -> @8",
                "Delete [While a : Foo() : End While]@8",
                "Delete [While a]@8",
                "Delete [End While]@26")
        End Sub

        <WpfFact>
        Public Sub While_InsertHeader()
            Dim src1 As String = "Foo()"
            Dim src2 As String = "While a : Foo() : End While"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [While a : Foo() : End While]@8",
                "Insert [While a]@8",
                "Move [Foo()]@8 -> @18",
                "Insert [End While]@26")
        End Sub

        <WpfFact>
        Public Sub Do1()
            Dim src1 = "Do : Do : Foo() : Loop While b : Loop Until a"
            Dim src2 = "Do : Do : Do : Foo() : Loop While b : Loop: Loop Until a"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Do : Do : Foo() : Loop While b : Loop]@13",
                "Insert [Do]@13",
                "Move [Do : Foo() : Loop While b]@13 -> @18",
                "Insert [Loop]@46")
        End Sub

        <WpfFact>
        Public Sub Do_DeleteHeader()
            Dim src1 = "Do : Foo() : Loop"
            Dim src2 = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@13 -> @8",
                "Delete [Do : Foo() : Loop]@8",
                "Delete [Do]@8",
                "Delete [Loop]@21")
        End Sub

        <WpfFact>
        Public Sub Do_InsertHeader()
            Dim src1 = "Foo()"
            Dim src2 = "Do : Foo() : Loop"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Do : Foo() : Loop]@8",
                "Insert [Do]@8",
                "Move [Foo()]@8 -> @13",
                "Insert [Loop]@21")
        End Sub
#End Region

#Region "If"
        <WpfFact>
        Public Sub IfStatement_TestExpression_Update1()
            Dim src1 = "Dim x = 1 : If x = 1 : x += 1 : End If"
            Dim src2 = "Dim x = 1 : If x = 2 : x += 1 : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [If x = 1]@20 -> [If x = 2]@20")
        End Sub

        <WpfFact>
        Public Sub IfStatement_TestExpression_Update2()
            Dim src1 = "Dim x = 1 : If x = 1 Then x += 1" & vbLf
            Dim src2 = "Dim x = 1 : If x = 2 Then x += 1" & vbLf
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [If x = 1 Then x += 1]@20 -> [If x = 2 Then x += 1]@20")
        End Sub

        <WpfFact>
        Public Sub IfStatement_TestExpression_Update3()
            Dim src1 = "Dim x = 1 : If x = 1 : x += 1 : End If" & vbLf
            Dim src2 = "Dim x = 1 : If x = 2 Then x += 1" & vbLf
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [If x = 1 : x += 1 : End If]@20 -> [If x = 2 Then x += 1]@20",
                "Delete [If x = 1]@20",
                "Delete [End If]@40")
        End Sub

        <WpfFact>
        Public Sub ElseClause_Insert()
            Dim src1 = "If x = 1 : x += 1 : End If"
            Dim src2 = "If x = 1 : x += 1 : Else : y += 1 : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Else : y += 1]@28",
                "Insert [Else]@28",
                "Insert [y += 1]@35")
        End Sub

        <WpfFact>
        Public Sub ElseClause_InsertMove()
            Dim src1 = "If x = 1 : x += 1 : Else : y += 1 : End If"
            Dim src2 = "If x = 1 : x += 1 : ElseIf x = 2 : y += 1 : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [ElseIf x = 2 : y += 1]@28",
                "Insert [ElseIf x = 2]@28",
                "Move [y += 1]@35 -> @43",
                "Delete [Else : y += 1]@28",
                "Delete [Else]@28")
        End Sub

        <WpfFact>
        Public Sub If1()
            Dim src1 As String = "If a : If b : Foo() : End If : End If"
            Dim src2 As String = "If a : If c : If b : Foo() : End If : End If : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [If c : If b : Foo() : End If : End If]@15",
                "Insert [If c]@15",
                "Move [If b : Foo() : End If]@15 -> @22",
                "Insert [End If]@46")
        End Sub

        <WpfFact>
        Public Sub If_DeleteHeader()
            Dim src1 = "If a : Foo() : End If"
            Dim src2 = "Foo()"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo()]@15 -> @8",
                "Delete [If a : Foo() : End If]@8",
                "Delete [If a]@8",
                "Delete [End If]@23")
        End Sub

        <WpfFact>
        Public Sub If_InsertHeader()
            Dim src1 = "Foo()"
            Dim src2 = "If a : Foo() : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [If a : Foo() : End If]@8",
                "Insert [If a]@8",
                "Move [Foo()]@8 -> @15",
                "Insert [End If]@23")
        End Sub

        <WpfFact>
        Public Sub Else_DeleteHeader()
            Dim src1 As String = "If a : Foo( ) : Else : Foo(  ) : End If"
            Dim src2 As String = "If a : Foo( ) : Foo(  ) : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo(  )]@31 -> @24",
                "Delete [Else : Foo(  )]@24",
                "Delete [Else]@24")
        End Sub

        <WpfFact>
        Public Sub Else_InsertHeader()
            Dim src1 = "If a : Foo( ) : End If : Foo(  )"
            Dim src2 = "If a : Foo( ) : Else : Foo(  ) : End If"

            Dim edits = GetMethodEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Else : Foo(  )]@24",
                "Insert [Else]@24",
                "Move [Foo(  )]@33 -> @31")
        End Sub

        <WpfFact>
        Public Sub ElseIf_DeleteHeader()
            Dim src1 = "If a : Foo( ) : ElseIf b : Foo(  ) : End If"
            Dim src2 = "If a : Foo( ) : End If : Foo(  )"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Foo(  )]@35 -> @33",
                "Delete [ElseIf b : Foo(  )]@24",
                "Delete [ElseIf b]@24")
        End Sub

        <WpfFact>
        Public Sub ElseIf_InsertHeader()
            Dim src1 = "If a : Foo( ) : Foo(  ) : End If"
            Dim src2 = "If a : Foo( ) : Else If b : Foo(  ) : End If"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Else If b : Foo(  )]@24",
                "Insert [Else If b]@24",
                "Move [Foo(  )]@24 -> @36")
        End Sub
#End Region

#Region "Lambdas"
        <WpfFact>
        Public Sub Lambdas_InVariableDeclarator()
            Dim src1 = "Dim x = Function(a) a, y = Function(b) b"
            Dim src2 = "Dim x = Sub(a) a, y = Function(b) (b + 1)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Function(a) a]@16 -> [Sub(a) a]@16",
                "Update [Function(b) b]@35 -> [Function(b) (b + 1)]@30")
        End Sub

        <WpfFact>
        Public Sub Lambdas_InExpressionStatement()
            Dim src1 = "F(Function(a) a, Function(b) b)"
            Dim src2 = "F(Function(b) b, Function(a)(a+1))"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Function(b) b]@25 -> @10",
                "Update [Function(a) a]@10 -> [Function(a)(a+1)]@25")
        End Sub

        <WpfFact>
        Public Sub Lambdas_InWhile()
            Dim src1 = "While F(Function(a) a) : End While"
            Dim src2 = "Do : Loop While F(Function(a) a)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [While F(Function(a) a) : End While]@8 -> [Do : Loop While F(Function(a) a)]@8",
                "Update [While F(Function(a) a)]@8 -> [Do]@8",
                "Update [End While]@33 -> [Loop While F(Function(a) a)]@13",
                "Insert [While F(Function(a) a)]@18",
                "Move [Function(a) a]@16 -> @26")
        End Sub

        <WpfFact>
        Public Sub Lambdas_InLambda()
            Dim src1 = "F(Sub()" & vbLf & "G(Function(x) y) : End Sub)"
            Dim src2 = "F(Function(q) G(Sub(x) f()))"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub()" & vbLf & "G(Function(x) y) : End Sub]@10 -> [Function(q) G(Sub(x) f())]@10")
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_Static_Top()
            Dim src1 = "
Imports System

Class C
    Sub F()
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub F()
        Dim f = new Func(Of Integer, Integer)(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_Static_Nested1()
            Dim src1 = "
Imports System

Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
        G(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
        G(Function(a) G(Function(b) b) + a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_ThisOnly_Top1()
            Dim src1 = "
Imports System

Class C
    Dim x As Integer = 0

    Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Dim x As Integer = 0

    Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
        G(Function(a) x)
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)

            ' TODO allow creating a new leaf closure
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "F", "Me"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_ThisOnly_Top2()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim y As Integer = 1
        Do
            Dim x As Integer = 2
            Dim f1 = New Func(Of Integer, Integer)(Function(a) y)
        Loop
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim y As Integer = 1
        Do
            Dim x As Integer = 2
            Dim f2 = From a In {1} Select a + y
            Dim f3 = From a In {1} Where x > 0 Select a
        Loop
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "x", "x"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_ThisOnly_Nested1()
            Dim src1 = "
Imports System

Class C
    Dim x As Integer = 0

    Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
        G(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Dim x As Integer = 0

    Function G(f As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Sub F()
        G(Function(a) G(Function(b) x))
    End Sub
End Class

"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "F", "Me"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Insert_ThisOnly_Nested2()
            Dim src1 = "
Imports System

Class C
    Dim x As Integer = 0

    Sub F()
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a)
                Dim f2 = New Func(Of Integer, Integer)(
                    Function(b)
                        Return b
                    End Function)
                Return a
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C

    Dim x As Integer = 0

    Sub F()
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a)
                Dim f2 = New Func(Of Integer, Integer)(
                    Function(b)
                        Return b
                    End Function)

                Dim f3 = New Func(Of Integer, Integer)(
                    Function(c)
                        Return c + x
                    End Function)
                Return a
            End Function)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "F", "Me"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_InsertAndDelete_Scopes1()
            Dim src1 = "
Imports System

Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0, y As Integer = 0                   ' Group #0

    Sub F()
        Dim x0 As Integer = 0, y0 As Integer = 0             ' Group #1
        Do
            Dim x1 As Integer = 0, y1 As Integer = 0         ' Group #2
            Do
                Dim x2 As Integer = 0, y2 As Integer = 0     ' Group #1
                Do
                    Dim x3 As Integer = 0, y3 As Integer = 0 ' Group #2

                    G(Function(a) x3 + x1)
                    G(Function(a) x0 + y0 + x2)
                    G(Function(a) x)
                Loop
            Loop
        Loop
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0, y As Integer = 0                   ' Group #0

    Sub F()
        Dim x0 As Integer = 0, y0 As Integer = 0             ' Group #1
        Do
            Dim x1 As Integer = 0, y1 As Integer = 0         ' Group #2
            Do
                Dim x2 As Integer = 0, y2 As Integer = 0     ' Group #1
                Do
                    Dim x3 As Integer = 0, y3 As Integer = 0 ' Group #2

                    G(Function(a) x3 + x1)
                    G(Function(a) x0 + y0 + x2)
                    G(Function(a) x)

                    G(Function(a) x)                         ' OK
                    G(Function(a) x0 + y0)                   ' OK
                    G(Function(a) x1 + y0)                   ' error - connecting Group #1 and Group #2
                    G(Function(a) x3 + x1)                   ' error - multi-scope (conservative)
                    G(Function(a) x + y0)                    ' error - connecting Group #0 and Group #1
                    G(Function(a) x + x3)                    ' error - connecting Group #0 and Group #2
                Loop
            Loop
        Loop
    End Sub
End Class"

            Dim insert = GetTopEdits(src1, src2)
            insert.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertLambdaWithMultiScopeCapture, "x1", VBFeaturesResources.LambdaExpression, "y0", "x1"),
                Diagnostic(RudeEditKind.InsertLambdaWithMultiScopeCapture, "x3", VBFeaturesResources.LambdaExpression, "x1", "x3"),
                Diagnostic(RudeEditKind.InsertLambdaWithMultiScopeCapture, "y0", VBFeaturesResources.LambdaExpression, "Me", "y0"),
                Diagnostic(RudeEditKind.InsertLambdaWithMultiScopeCapture, "x3", VBFeaturesResources.LambdaExpression, "Me", "x3"))

            Dim delete = GetTopEdits(src2, src1)
            delete.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.DeleteLambdaWithMultiScopeCapture, "x1", VBFeaturesResources.LambdaExpression, "y0", "x1"),
                Diagnostic(RudeEditKind.DeleteLambdaWithMultiScopeCapture, "x3", VBFeaturesResources.LambdaExpression, "x1", "x3"),
                Diagnostic(RudeEditKind.DeleteLambdaWithMultiScopeCapture, "y0", VBFeaturesResources.LambdaExpression, "Me", "y0"),
                Diagnostic(RudeEditKind.DeleteLambdaWithMultiScopeCapture, "x3", VBFeaturesResources.LambdaExpression, "Me", "x3"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_CeaseCapture_This()
            Dim src1 = "
Imports System

Class C
    Dim x As Integer = 1

    Sub F()
        Dim f = New Func(Of Integer, Integer)(Function(a) a + x)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Dim x As Integer

    Sub F()
        Dim f = New Func(Of Integer, Integer)(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "F", "Me"))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature1()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Long, Long))
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Long, Long))
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature2()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer, Integer))
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer, Integer))
    End Sub

    Sub F()
        G2(Function(a, b) a + b)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a, b)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature3()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Long))
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Long))
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaReturnType, "Function(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature_EmptyBody1()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Action(Of Integer))
    End Sub

    Sub G2(f As Action(Of Long))
    End Sub

    Sub F()
        G1(
Sub(a)
End Sub)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Action(Of Integer))
    End Sub

    Sub G2(f As Action(Of Long))
    End Sub

    Sub F()
        G2(
Sub(a)
End Sub)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Sub(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature_EmptyBody2()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Action)
    End Sub

    Sub G2(f As Func(Of Object))
    End Sub

    Sub F()
        G1(
Sub()
End Sub)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Action)
    End Sub

    Sub G2(f As Func(Of Object))
    End Sub

    Sub F()
        G2(
Function()
End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaReturnType, "Function()", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Signature_SyntaxOnly1()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature_ReturnType1()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Action(Of Integer))
    End Sub

    Sub F()
        G1(Function(a)
            Return 1
        End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Action(Of Integer))
    End Sub

    Sub F()
        G2(Sub(a)
           End Sub)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaReturnType, "Sub(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Signature_BodySyntaxOnly()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G1(Function(a)
               Return 1
           End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G2(Function(a) 2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Signature_ParameterName1()
            Dim src1 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G1(Function(a) 1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Sub G1(f As Func(Of Integer, Integer))
    End Sub

    Sub G2(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        G2(Function(b) 2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Signature_ParameterRefness1()
            Dim src1 = "
Imports System

Delegate Function D1(ByRef a As Integer) As Integer
Delegate Function D2(a As Integer) As Integer

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G1(Function(ByRef a As Integer) 1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Delegate Function D1(ByRef a As Integer) As Integer
Delegate Function D2(a As Integer) As Integer

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G2(Function(a As Integer) 2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a As Integer)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_DelegateType1()
            Dim src1 = "
Imports System

Delegate Function D1(a As Integer) As Integer
Delegate Function D2(a As Integer) As Integer

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Delegate Function D1(a As Integer) As Integer
Delegate Function D2(a As Integer) As Integer

Class C

    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_SourceType1()
            Dim src1 = "
Imports System

Delegate Function D1(a As C) As C
Delegate Function D2(a As C) As C

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Delegate Function D1(a As C) As C
Delegate Function D2(a As C) As C

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_SourceType2()
            Dim src1 = "
Imports System

Delegate Function D1(a As C) As C
Delegate Function D2(a As B) As B

Class B
End Class

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G1(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Delegate Function D1(a As C) As C
Delegate Function D2(a As B) As B

Class B
End Class

Class C
    Sub G1(f As D1)
    End Sub

    Sub G2(f As D2)
    End Sub

    Sub F()
        G2(Function(a) a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_SourceTypeAndMetadataType1()
            Dim src1 = "
Namespace [System]

    Delegate Function D1(a As String) As String
    Delegate Function D2(a As [String]) As [String]

    Class [String]
    End Class

    Class C

        Sub G1(f As D1)
        End Sub

        Sub G2(f As D2)
        End Sub

        Sub F()
            G1(Function(a) a)
        End Sub
    End Class
End Namespace
"
            Dim src2 = "
Namespace [System]

    Delegate Function D1(a As String) As String
    Delegate Function D2(a As [String]) As [String]

    Class [String]
    End Class

    Class C
        Sub G1(f As D1)
        End Sub

        Sub G2(f As D2)
        End Sub

        Sub F()
            G2(Function(a) a)
        End Sub
    End Class
End Namespace
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Generic1()
            Dim src1 = "
Delegate Function D1(Of S, T)(a As S, b As T) As T
Delegate Function D2(Of S, T)(a As T, b As S) As T

Class C

    Sub G1(f As D1(Of Integer, Integer))
    End Sub

    Sub G2(f As D2(Of Integer, Integer))
    End Sub

    Sub F()
        G1(Function(a, b) a + b)
    End Sub
End Class
"
            Dim src2 = "
Delegate Function D1(Of S, T)(a As S, b As T) As T
Delegate Function D2(Of S, T)(a As T, b As S) As T

Class C
    Sub G1(f As D1(Of Integer, Integer))
    End Sub

    Sub G2(f As D2(Of Integer, Integer))
    End Sub

    Sub F()
        G2(Function(a, b) a + b)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_Generic2()
            Dim src1 = "
Delegate Function D1(Of S, T)(a As S, b As T) As Integer
Delegate Function D2(Of S, T)(a As T, b As S) As Integer

Class C
    Sub G1(f As D1(Of Integer, Integer))
    End Sub

    Sub G2(f As D2(Of Integer, String))
    End Sub

    Sub F()
        G1(Function(a, b) 1)
    End Sub
End Class
"
            Dim src2 = "
Delegate Function D1(Of S, T)(a As S, b As T) As Integer
Delegate Function D2(Of S, T)(a As T, b As S) As Integer

Class C
    Sub G1(f As D1(Of Integer, Integer))
    End Sub

    Sub G2(f As D2(Of Integer, String))
    End Sub

    Sub F()
        G2(Function(a, b) 1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingLambdaParameters, "Function(a, b)", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_CapturedParameters1()
            Dim src1 = "
Imports System
Class C
    Sub F(x1 As Integer)
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) x1 + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F(x1 As Integer)
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) x1 + a2 + 1)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact, WorkItem(2223, "https://github.com/dotnet/roslyn/issues/2223")>
        Public Sub Lambdas_Update_CapturedParameters2()
            Dim src1 = "
Imports System
Class C
    Sub F(x1 As Integer)
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) x1 + a2)
                Return a1
            End Function)

        Dim f3 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f4 = New Func(Of Integer, Integer)(Function(a3) x1 + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F(x1 As Integer)
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) x1 + a2 + 1)
                Return a1
            End Function)

        Dim f3 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f4 = New Func(Of Integer, Integer)(Function(a3) x1 + a2 + 1)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_CeaseCapture_Closure1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) y + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2)
                Return a1 + y
            End Function)
    End Sub
End Class

"
            Dim edits = GetTopEdits(src1, src2)

            ' TODO: better location
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Function(a2)", "y", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_CeaseCapture_IndexerParameter2()
            Dim src1 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return New Func(Of Integer, Integer)(Function(a3) a1 + a2)
        End Get
    End Property
End Class
"
            Dim src2 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return New Func(Of Integer, Integer)(Function(a3) a2)
        End Get
    End Property
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a1", "a1"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_CeaseCapture_MethodParameter1()
            Dim src1 = "
Imports System
Class C
    Sub F(a1 As Integer, a2 As Integer)
        Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1 + a2)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F(a1 As Integer, a2 As Integer)
        Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a2", "a2"))
        End Sub

        <WpfFact, WorkItem(1290)>
        Public Sub Lambdas_Update_CeaseCapture_LambdaParameter1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1 + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) a2) 
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a1", "a1"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_DeleteCapture1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) y + a2)
                Return y
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim f1 = New Func(Of Integer, Integer)(Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.DeletingCapturedVariable, "Sub F()", "y"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Capturing_IndexerGetterParameter2()
            Dim src1 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return New Func(Of Integer, Integer)(Function(a3) a2)
        End Get
    End Property
End Class
"
            Dim src2 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return New Func(Of Integer, Integer)(Function(a3) a1 + a2)
        End Get
    End Property
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "a1", "a1"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Capturing_IndexerSetterParameter1()
            Dim src1 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return Nothing
        End Get
        Set
            Dim f = New Func(Of Integer, Integer)(Function(a3) a2)
        End Set
    End Property
End Class
"
            Dim src2 = "
Imports System
Class C
    Property Item(a1 As Integer, a2 As Integer) As Func(Of Integer, Integer)
        Get
            Return Nothing
        End Get
        Set
            Dim f = New Func(Of Integer, Integer)(Function(a3) a1 + a2)
        End Set
    End Property
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "a1", "a1"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Capturing_MethodParameter1()
            Dim src1 = "
Imports System
Class C
    Sub F(a1 As Integer, a2 As Integer)
        Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F(a1 As Integer, a2 As Integer)
        Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1 + a2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "a2", "a2"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Capturing_LambdaParameter1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim f1 = New Func(Of Integer, Integer, Integer)(
            Function(a1, a2)
                Dim f2 = New Func(Of Integer, Integer)(Function(a3) a1 + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "a1", "a1"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_StaticToThisOnly1()
            Dim src1 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim f = New Func(Of Integer, Integer)(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim f = New Func(Of Integer, Integer)(Function(a) a + x)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "F", "Me"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_StaticToThisOnly_Partial()
            Dim src1 = "
Imports System

Partial Class C
    Dim x As Integer = 1
    Partial Sub F() ' def
    End Sub
End Class

Partial Class C
    Partial Sub F() ' impl
        Dim f = New Func(Of Integer, Integer)(Function(a) a)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Partial Class C
    Dim x As Integer = 1
    Partial Sub F() ' def
    End Sub
End Class

Partial Class C
    Partial Sub F() ' impl
        Dim f = New Func(Of Integer, Integer)(Function(a) a + x)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "F", "Me").WithFirstLine("Partial Sub F() ' impl"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_StaticToThisOnly3()
            Dim src1 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim f1 = New Func(Of Integer, Integer)(Function(a1) a1)
        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim f1 = New Func(Of Integer, Integer)(Function(a1) a1 + x)
        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "Function(a1)", "Me", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_StaticToClosure1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim x As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(Function(a1) a1)
        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim x As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Return a1 +
                        x + ' 1
                        x   ' 2
            End Function)

        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "x", "x", VBFeaturesResources.LambdaExpression).WithFirstLine("x + ' 1"),
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "x", "x", VBFeaturesResources.LambdaExpression).WithFirstLine("x   ' 2"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_ThisOnlyToClosure1()
            Dim src1 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(Function(a1) a1 + x)
        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x + y)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Dim x As Integer = 1

    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(Function(a1) a1 + x + y)
        Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + x + y)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "y", "y", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Nested1()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + y)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C

    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2 + y)
                Return a1 + y
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Nested2()
            Dim src1 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub F()
        Dim y As Integer = 1
        Dim f1 = New Func(Of Integer, Integer)(
            Function(a1)
                Dim f2 = New Func(Of Integer, Integer)(Function(a2) a1 + a2)
                Return a1
            End Function)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "a1", "a1").WithFirstLine("Function(a1)"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Accessing_Closure1()
            Dim src1 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        Dim x0 As Integer = 0, y0 As Integer = 0
        G(Function(a) x0)
        G(Function(a) y0)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Sub F()
        Dim x0 As Integer = 0, y0 As Integer = 0
        G(Function(a) x0)
        G(Function(a) y0 + x0)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "x0", "x0", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Accessing_Closure2()
            Dim src1 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                    ' Group #0

    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0        ' Group #0
            Do
                Dim x1 As Integer = 0, y1 As Integer = 0    ' Group #1

                G(Function(a) x + x0)
                G(Function(a) x0)
                G(Function(a) y0)
                G(Function(a) x1)
            Loop
        Loop
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                    ' Group #0

    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0        ' Group #0
            Do
                Dim x1 As Integer = 0, y1 As Integer = 0    ' Group #1

                G(Function(a) x)  ' error: disconnecting previously connected closures
                G(Function(a) x0)
                G(Function(a) y0)
                G(Function(a) x1)
            Loop
        Loop
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Function(a)", "x0", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Accessing_Closure3()
            Dim src1 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                        ' Group #0

    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0            ' Group #0
            Do                                                  
                Dim x1 As Integer = 0, y1 As Integer = 0        ' Group #1
                
                G(Function(a) x)
                G(Function(a) x0)
                G(Function(a) y0)
                G(Function(a) x1)
                G(Function(a) y1)
            Loop
        Loop
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                        ' Group #0

    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0            ' Group #0
            Do                                                  
                Dim x1 As Integer = 0, y1 As Integer = 0        ' Group #1
                
                G(Function(a) x)
                G(Function(a) x0)
                G(Function(a) y0)
                G(Function(a) x1)
                G(Function(a) y1 + x0) ' error: connecting previously disconnected closures
            Loop
        Loop
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "x0", "x0", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Lambdas_Update_Accessing_Closure4()
            Dim src1 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                    ' Group #0
    
    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0        ' Group #0
            Do                                              
                Dim x1 As Integer = 0, y1 As Integer = 0    ' Group #1

                G(Function(a) x + x0)
                G(Function(a) x0)
                G(Function(a) y0)
                G(Function(a) x1)
                G(Function(a) y1)
            Loop
        Loop
    End Sub
End Class
"
            Dim src2 = "
Imports System
Class C
    Sub G(f As Func(Of Integer, Integer))
    End Sub

    Dim x As Integer = 0                                    ' Group #0

    Sub F()
        Do
            Dim x0 As Integer = 0, y0 As Integer = 0        ' Group #0  
            Do
                Dim x1 As Integer = 0, y1 As Integer = 0    ' Group #1

                G(Function(a) x)       ' error: disconnecting previously connected closures
                G(Function(a) x0)      
                G(Function(a) y0)      
                G(Function(a) x1)      
                G(Function(a) y1 + x0) ' error: connecting previously disconnected closures
            Loop
        Loop
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' TODO "Function(a) x + x0" is matched with "Function(a) y1 + x0", hence we report more errors.
            ' Including statement distance when matching would help.

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Function(a)", "Me", VBFeaturesResources.LambdaExpression).WithFirstLine("G(Function(a) y1 + x0) ' error: connecting previously disconnected closures"),
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "y1", "y1", VBFeaturesResources.LambdaExpression).WithFirstLine("G(Function(a) y1 + x0) ' error: connecting previously disconnected closures"),
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "Function(a)", "Me", VBFeaturesResources.LambdaExpression).WithFirstLine("G(Function(a) x)       ' error: disconnecting previously connected closures"),
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Function(a)", "y1", VBFeaturesResources.LambdaExpression).WithFirstLine("G(Function(a) x)       ' error: disconnecting previously connected closures"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_RenameCapturedLocal1()
            Dim src1 = "
Imports System
Imports System.Diagnostics

Class Program

    Shared Sub Main()
        Dim x As Integer = 1
        Dim f As Func(Of Integer) = Function() x
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Diagnostics

Class Program

    Shared Sub Main()
        Dim X As Integer = 1
        Dim f As Func(Of Integer) = Function() X
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' Note that lifted variable is a field, which can't be renamed
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RenamingCapturedVariable, "X", "x", "X"))
        End Sub

        <WpfFact>
        Public Sub Lambdas_RenameCapturedLocal2()
            Dim src1 = "
Imports System
Imports System.Diagnostics

Class Program

    Shared Sub Main()
        Dim x As Integer = 1
        Dim f As Func(Of Integer) = Function() x
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Diagnostics

Class Program

    Shared Sub Main()
        Dim y As Integer = 1
        Dim f As Func(Of Integer) = Function() y
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RenamingCapturedVariable, "y", "x", "y"))
        End Sub
#End Region

#Region "Queries"
        <WpfFact>
        Public Sub Queries_Update_Signature_Select1()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1} Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1.0} Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Select", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_Select2()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1} Select b = a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1.0} Select b = a.ToString()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Select", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_Select3()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1} Select b = a, c = a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1.0} Select b = a, c = a.ToString()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Select", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_From1()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1} From b In {2} Select b
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1.0} From b In {2} Select b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "From", VBFeaturesResources.FromClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_From2()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a As Long In {1} From b In {2} Select b
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a As System.Int64 In {1} From b In {2} Select b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_From3()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} From b In {2} Select b
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In New List(Of Integer)() From b In New List(Of Integer)() Select b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_FromInAggregate1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} From b in {2}, c in {3} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1.0} From b in {2}, c in {3} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no lambdas created
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "From", "From clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_FromInAggregate2()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} From b in {2}, c in {3} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} From b in {2.0}, c in {3} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no lambdas created
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "From", "From clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_FromInAggregate3()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} From b in {2}, c in {3} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} From b in {2}, c in {3.0} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no lambdas created
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "From", "From clause"))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_Let1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Let b = 1 Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Let b = 1.0 Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Let", VBFeaturesResources.LetClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_OrderBy1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Order By a + 1 Descending, a + 2 Ascending Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Order By a + 1.0 Descending, a + 2 Ascending Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "a + 1.0 Descending", VBFeaturesResources.OrderingClause))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_OrderBy2()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Order By a + 1 Descending, a + 2 Ascending Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Order By a + 1 Descending, a + 2.0 Ascending Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "a + 2.0 Ascending", VBFeaturesResources.OrderingClause))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2} On a Equals b Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2.0} On a Equals b Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join2()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2} On a Equals b Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2} On a + 1.0 Equals b Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join3()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2} On a Equals b Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b In {2} On a Equals b + 1.0 Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join4()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2.0} On b1 Equals b2 On b1 Equals a Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join5()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2} On b1 + 1.0 Equals b2 On b1 Equals a Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Join()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Join b1 In {2} Join b2 In {2} On b1 Equals b2 + 1.0 On b1 Equals a Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_GroupJoin1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2.0} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group Join", "Group Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_GroupJoin2()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2.0} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group Join", "Group Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_GroupJoin3()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 + 1.0 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group Join", "Group Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_GroupJoin4()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a) Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Group Join b1 In {2} Join b2 In {2} On b1 Equals b2 On b1 Equals a Into Count(a + 1.0) Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no change in type
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_GroupBy1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result1 = From a In {1} Group x = 1, y = 2 By z = a, u = a Into Group Select Group
        Dim result2 = From a In {1} Group x = 1, y = 2 By z = a, u = a Into Group Select Group
        Dim result3 = From a In {1} Group x = 1, y = 2 By z = a, u = a Into Group Select Group
        Dim result4 = From a In {1} Group x = 1, y = 2 By z = a, u = a Into Group Select Group
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result1 = From a In {1} Group x = 1.0, y = 2 By z = a, u = a Into Group Select Group
        Dim result2 = From a In {1} Group x = 1, y = 2.0 By z = a, u = a Into Group Select Group
        Dim result3 = From a In {1} Group x = 1, y = 2 By z = a + 1.0, u = a Into Group Select Group
        Dim result4 = From a In {1} Group x = 1, y = 2 By z = a, u = a + 1.0 Into Group Select Group
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group", "Group By clause"),
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group", "Group By clause"),
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group", "Group By clause"),
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Group", "Group By clause"))
        End Sub

        <WpfFact>
        Public Sub Queries_Update_Signature_Partition1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result1 = From a In {1} Take 1 Select a
        Dim result2 = From a In {1} Skip 1 Select a
        Dim result3 = From a In {1} Take While a Select a
        Dim result4 = From a In {1} Skip While a Select a
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result1 = From a In {1} Take 1.0 Select a
        Dim result2 = From a In {1} Skip 1.0 Select a
        Dim result3 = From a In {1} Take While a + 1.0 Select a
        Dim result4 = From a In {1} Skip While a + 1.0 Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no change in lambda types
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Aggregate1()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate a In {1.0} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' no lambdas created
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Aggregate2()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2.0} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            ' change is in the aggregate lambda body, but not in its signature
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Aggregate3()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2}, c In {3} Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2}, c In {3.0} Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Aggregate", "Aggregate clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Aggregate4()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2} Join c In {3} On c Equals b Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = From a In {1} Aggregate b In {2} Join c In {3.0} On c Equals b Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Join", "Join clause"))
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_Update_Signature_Aggregate5()
            Dim src1 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate b In {2} Select b Into Count()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq
Imports System.Collections.Generic

Class C
    Sub F()
        Dim result = Aggregate b In {2} Select b + 1.0 Into Count()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingQueryLambdaType, "Select", "Select clause"))
        End Sub

        <WpfFact>
        Public Sub Queries_FromSelect_Update()
            Dim src1 = "F(From a In b Select c)"
            Dim src2 = "F(From a In c Select c + 1)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a In b]@15 -> [a In c]@15",
                "Update [c]@29 -> [c + 1]@29")
        End Sub

        <WpfFact>
        Public Sub Queries_FromSelect_Delete()
            Dim src1 = "F(From a In b From c In d Select a + c)"
            Dim src2 = "F(From a In b Select c + 1)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a + c]@41 -> [c + 1]@29",
                "Delete [From c In d]@22",
                "Delete [c In d]@27",
                "Delete [c]@27")
        End Sub

        <WpfFact>
        Public Sub Queries_GroupBy_Update()
            Dim src1 = "F(From a In b Group a By a.x Into g Select g)"
            Dim src2 = "F(From a In b Group z By z.y Into h Select h)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@28 -> [z]@28",
                "Update [a.x]@33 -> [z.y]@33",
                "Update [g]@42 -> [h]@42",
                "Update [g]@51 -> [h]@51")
        End Sub

        <WpfFact>
        Public Sub Queries_OrderBy_Reorder()
            Dim src1 = "F(From a In b Order By a.x, a.b Descending, a.c Ascending Select a.d)"
            Dim src2 = "F(From a In b Order By a.x, a.c Ascending, a.b Descending Select a.d)"
            Dim edits = GetMethodEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [a.c Ascending]@52 -> @36")
        End Sub

        <WpfFact>
        Public Sub Queries_GroupJoin()
            Dim src1 = "F(From a1 In b1 Group Join c1 In d1 On e1 Equals f1 Into g1 = Group, h1 = Sum(f1) Select g1)"
            Dim src2 = "F(From a2 In b2 Group Join c2 In d2 On e2 Equals f2 Into g2 = Group, h2 = Sum(f2) Select g2)"
            Dim edits = GetMethodEdits(src1, src2)
            Dim actual = ToMatchingPairs(edits.Match)

            Dim expected = New MatchingPairs From
            {
                {"Sub F()", "Sub F()"},
                {"F(From a1 In b1 Group Join c1 In d1 On e1 Equals f1 Into g1 = Group, h1 = Sum(f1) Select g1)", "F(From a2 In b2 Group Join c2 In d2 On e2 Equals f2 Into g2 = Group, h2 = Sum(f2) Select g2)"},
                {"From a1 In b1 Group Join c1 In d1 On e1 Equals f1 Into g1 = Group, h1 = Sum(f1) Select g1", "From a2 In b2 Group Join c2 In d2 On e2 Equals f2 Into g2 = Group, h2 = Sum(f2) Select g2"},
                {"From a1 In b1", "From a2 In b2"},
                {"a1 In b1", "a2 In b2"},
                {"a1", "a2"},
                {"Group Join c1 In d1 On e1 Equals f1 Into g1 = Group, h1 = Sum(f1)", "Group Join c2 In d2 On e2 Equals f2 Into g2 = Group, h2 = Sum(f2)"},
                {"c1 In d1", "c2 In d2"},
                {"c1", "c2"},
                {"e1 Equals f1", "e2 Equals f2"},
                {"g1", "g2"},
                {"h1", "h2"},
                {"Sum(f1)", "Sum(f2)"},
                {"Select g1", "Select g2"},
                {"g1", "g2"},
                {"End Sub", "End Sub"}
            }

            expected.AssertEqual(actual)

            edits.VerifyEdits(
                "Update [a1 In b1]@15 -> [a2 In b2]@15",
                "Update [c1 In d1]@35 -> [c2 In d2]@35",
                "Update [e1 Equals f1]@47 -> [e2 Equals f2]@47",
                "Update [g1]@65 -> [g2]@65",
                "Update [h1]@77 -> [h2]@77",
                "Update [Sum(f1)]@82 -> [Sum(f2)]@82",
                "Update [g1]@97 -> [g2]@97",
                "Update [a1]@15 -> [a2]@15",
                "Update [c1]@35 -> [c2]@35")
        End Sub

        <WpfFact>
        Public Sub Queries_CapturedTransparentIdentifiers_FromClause1()
            Dim src1 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {2} 
                     Where Z(Function() a) > 0 
                     Where Z(Function() b) > 0 
                     Where Z(Function() a) > 0 
                     Where Z(Function() b) > 0 
                     Select a
    End Sub
End Class
"

            Dim src2 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1}
                     From b In {2} 
                     Where Z(Function() a) > 1  ' update
                     Where Z(Function() b) > 2  ' update
                     Where Z(Function() a) > 3  ' update
                     Where Z(Function() b) > 4  ' update
                     Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact>
        Public Sub Queries_CapturedTransparentIdentifiers_LetClause1()
            Dim src1 As String = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Let b = Z(Function() a) 
                     Select a + b
    End Sub
End Class
"
            Dim src2 As String = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Let b = Z(Function() a) 
                     Select a - b
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/1212"), WorkItem(1212)>
        Public Sub Queries_CapturedTransparentIdentifiers_JoinClause1()
            Dim src1 As String = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Group Join b In {3} On Z(Function() a + 1) Equals Z(Function() b - 1) Into g = Group
                     Select Z(Function() g.First())
    End Sub
End Class
"
            Dim src2 As String = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Group Join b In {3} On Z(Function() a + 1) Equals Z(Function() b - 1) Into g = Group
                     Select Z(Function() g.Last())
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)

            ' TODO (bug 1212) should report no error
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_COMPLEX_QUERY_EXPRESSION, "Group Join", "method"))
        End Sub

        <WpfFact, WorkItem(1312)>
        Public Sub Queries_CeaseCapturingTransparentIdentifiers1()
            Dim src1 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1}
                     From b In {2} 
                     Where Z(Function() a + b) > 0 
                     Select a
    End Sub
End Class
"
            Dim src2 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1}
                     From b In {2} 
                     Where Z(Function() a + 1) > 0 
                     Select a
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "b In {2}", "b"))
        End Sub

        <WpfFact, WorkItem(1312)>
        Public Sub Queries_CapturingTransparentIdentifiers1()
            Dim src1 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {2} 
                     Where Z(Function() a + 1) > 0 
                     Select a
    End Sub
End Class
"
            Dim src2 As String = "
Imports System
Imports System.Linq

Class C

    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {2} 
                     Where Z(Function() a + b) > 0 
                     Select a
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.CapturingVariable, "b", "b"))
        End Sub

        <WpfFact>
        Public Sub Queries_AccessingCapturedTransparentIdentifier1()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Where Z(Function() a) > 0
                     Select 1
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1}
                     Where Z(Function() a) > 0 
                     Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "a", "a", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_AccessingCapturedTransparentIdentifier2()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {1}
                     Where Z(Function() a) > 0
                     Select b
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {1} 
                     Where Z(Function() a) > 0 
                     Select a + b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "a", "a", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_AccessingCapturedTransparentIdentifier3()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Where Z(Function() a) > 0 
                     Select Z(Function() 1)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Where Z(Function() a) > 0 
                     Select Z(Function() a)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "a", "a", VBFeaturesResources.SelectClause),
                Diagnostic(RudeEditKind.AccessingCapturedVariableInLambda, "a", "a", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Queries_NotAccessingCapturedTransparentIdentifier1()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {1}
                     Where Z(Function() a) > 0
                     Select a + b
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     From b In {1} 
                     Where Z(Function() a) > 0 
                     Select b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Select", "a", VBFeaturesResources.SelectClause))
        End Sub

        <WpfFact>
        Public Sub Queries_NotAccessingCapturedTransparentIdentifier2()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Where Z(Function() a) > 0 
                     Select Z(Function() a)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Function Z(f As Func(Of Integer)) As Integer
        Return 1
    End Function

    Sub F()
        Dim result = From a In {1} 
                     Where Z(Function() a) > 0 
                     Select Z(Function() 1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Select", "a", VBFeaturesResources.SelectClause),
                Diagnostic(RudeEditKind.NotAccessingCapturedVariableInLambda, "Function()", "a", VBFeaturesResources.LambdaExpression))
        End Sub

        <WpfFact>
        Public Sub Queries_Insert1()
            Dim src1 = "
Imports System
Imports System.Linq

Class C
    Sub F()
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In {1} Select a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub
#End Region

#Region "Yield"
        <WpfFact>
        Public Sub Yield_Update1()
            Dim src1 = <text>
Yield 1
Yield 2
</text>.Value
            Dim src2 = <text>
Yield 3
Yield 4
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Iterator)

            edits.VerifyEdits(
                "Update [Yield 1]@50 -> [Yield 3]@50",
                "Update [Yield 2]@58 -> [Yield 4]@58")

        End Sub

        <WpfFact>
        Public Sub Yield_Update2()
            Dim src1 = <text>
Yield 1
Yield 2
</text>.Value
            Dim src2 = <text>
Yield 3
Yield 4
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Iterator)

            edits.VerifyEdits(
                "Update [Yield 1]@50 -> [Yield 3]@50",
                "Update [Yield 2]@58 -> [Yield 4]@58")
        End Sub

        <WpfFact>
        Public Sub Yield_Insert()
            Dim src1 = <text>
Yield 1
Yield 2
</text>.Value
            Dim src2 = <text>
Yield 1
Yield 2
Yield 3
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Iterator)

            edits.VerifyEdits(
                "Insert [Yield 3]@66")
        End Sub

        <WpfFact>
        Public Sub Yield_Delete()
            Dim src1 = <text>
Yield 1
Yield 2
Yield 3
</text>.Value
            Dim src2 = <text>
Yield 1
Yield 2
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Iterator)

            edits.VerifyEdits(
                "Delete [Yield 3]@66")
        End Sub
#End Region

#Region "Await"
        <WpfFact>
        Public Sub Await_Update1()
            Dim src1 = <text>
Await F(1)
Await F(2)
</text>.Value
            Dim src2 = <text>
Await F(3)
Await F(4)
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Async)

            edits.VerifyEdits(
                "Update [Await F(1)]@40 -> [Await F(3)]@40",
                "Update [Await F(2)]@51 -> [Await F(4)]@51")
        End Sub

        <WpfFact>
        Public Sub Await_Insert()
            Dim src1 = <text>
Await F(1)
Await F(3)
</text>.Value
            Dim src2 = <text>
Await F(1)
Await F(2)
Await F(3)
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Async)

            edits.VerifyEdits(
                "Insert [Await F(2)]@51")
        End Sub

        <WpfFact>
        Public Sub Await_Delete()
            Dim src1 = <text>
Await F(1)
Await F(2)
Await F(3)
</text>.Value
            Dim src2 = <text>
Await F(1)
Await F(3)
</text>.Value
            Dim edits = GetMethodEdits(src1, src2, stateMachine:=StateMachineKind.Async)

            edits.VerifyEdits(
                "Delete [Await F(2)]@51")
        End Sub
#End Region
    End Class
End Namespace
