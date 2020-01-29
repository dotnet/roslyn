' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class CompilationEventTests
        Inherits BasicTestBase

        Friend Shared Sub VerifyEvents(queue As AsyncQueue(Of CompilationEvent), ParamArray expectedEvents As String())
            Dim expected = New HashSet(Of String)
            For Each s In expectedEvents
                If Not expected.Add(s) Then
                    Console.WriteLine("Expected duplicate " & s)
                End If
            Next

            Dim actual = ArrayBuilder(Of CompilationEvent).GetInstance()
            While queue.Count > 0
                Dim te = queue.DequeueAsync(CancellationToken.None)
                actual.Add(te.Result)
            End While
            Dim unexpected = False
            For Each a In actual
                Dim eventString = a.ToString()
                If Not expected.Remove(eventString) Then
                    If Not unexpected Then
                        Console.WriteLine("UNEXPECTED EVENTS:")
                        unexpected = True
                    End If
                    Console.WriteLine(eventString)
                End If
            Next
            If expected.Count <> 0 Then
                Console.WriteLine("MISSING EVENTS:")
            End If
            For Each e In expected
                Console.WriteLine(e)
            Next
            If unexpected OrElse expected.Count <> 0 OrElse expectedEvents.Length <> actual.Count Then
                Dim first = True
                Console.WriteLine("ACTUAL EVENTS:")
                For Each e In actual
                    If Not first Then
                        Console.WriteLine(",")
                    End If
                    first = False
                    Console.Write("""" & e.ToString() & """")
                Next
                Console.WriteLine()
                Assert.True(False)
            End If
        End Sub

        <Fact>
        Public Sub TestQueuedSymbols()
            Dim source =
    <compilation>
        <file name="TestQueuedSymbols.vb"><![CDATA[
Namespace N
    Partial Class C(Of T1)
        Partial Private Sub M(x1 As Integer)
        End Sub
        Friend Property P As Integer
        Dim F As Integer = 12
        Dim G As Integer
        Dim H, I As Integer
        Private Sub N(Of T2)(Optional y As Integer = 12)
            F = F + y
        End Sub
    End Class
    Partial Class C(Of T1)
        Private Sub M(x1 As Integer)
        End Sub
    End Class
    Module Mod1
        Private Sub M2(): End Sub
    End Module
    Enum Color
        Red
        Green
        Blue
    End Enum
End Namespace
]]>
        </file>
    </compilation>
            Dim q = New AsyncQueue(Of CompilationEvent)()
            CreateCompilationWithMscorlib40AndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q).VerifyDiagnostics().VerifyDiagnostics()
            VerifyEvents(q)
        End Sub

        Private Shared Sub VerifyEvents(q As AsyncQueue(Of CompilationEvent))
            VerifyEvents(q,
                            "CompilationStartedEvent",
                            "SymbolDeclaredCompilationEvent(<empty>  @ TestQueuedSymbols.vb: (0,0)-(25,0))",
                            "SymbolDeclaredCompilationEvent(N N @ TestQueuedSymbols.vb: (0,10)-(0,11))",
                            "SymbolDeclaredCompilationEvent(Red Color.Red @ TestQueuedSymbols.vb: (20,8)-(20,11))",
                            "SymbolDeclaredCompilationEvent(Green Color.Green @ TestQueuedSymbols.vb: (21,8)-(21,13))",
                            "SymbolDeclaredCompilationEvent(Blue Color.Blue @ TestQueuedSymbols.vb: (22,8)-(22,12))",
                            "SymbolDeclaredCompilationEvent(Color Color @ TestQueuedSymbols.vb: (19,4)-(23,12))",
                            "SymbolDeclaredCompilationEvent(Mod1 Mod1 @ TestQueuedSymbols.vb: (16,4)-(18,14))",
                            "SymbolDeclaredCompilationEvent(P Property C(Of T1).P As Integer @ TestQueuedSymbols.vb: (4,8)-(4,36))",
                            "SymbolDeclaredCompilationEvent(F C(Of T1).F As Integer @ TestQueuedSymbols.vb: (5,12)-(5,13))",
                            "SymbolDeclaredCompilationEvent(G C(Of T1).G As Integer @ TestQueuedSymbols.vb: (6,12)-(6,13))",
                            "SymbolDeclaredCompilationEvent(H C(Of T1).H As Integer @ TestQueuedSymbols.vb: (7,12)-(7,13))",
                            "SymbolDeclaredCompilationEvent(I C(Of T1).I As Integer @ TestQueuedSymbols.vb: (7,15)-(7,16))",
                            "SymbolDeclaredCompilationEvent(C C(Of T1) @ TestQueuedSymbols.vb: (1,4)-(11,13), TestQueuedSymbols.vb: (12,4)-(15,13))",
                            "SymbolDeclaredCompilationEvent(M Sub C(Of T1).M(x1 As Integer) @ TestQueuedSymbols.vb: (2,8)-(2,44))",
                            "SymbolDeclaredCompilationEvent(M Sub C(Of T1).M(x1 As Integer) @ TestQueuedSymbols.vb: (13,8)-(13,36))",
                            "SymbolDeclaredCompilationEvent(M2 Sub Mod1.M2() @ TestQueuedSymbols.vb: (17,8)-(17,24))",
                            "SymbolDeclaredCompilationEvent(get_P Property Get C(Of T1).P() As Integer)",
                            "SymbolDeclaredCompilationEvent(set_P Property Set C(Of T1).P(AutoPropertyValue As Integer))",
                            "SymbolDeclaredCompilationEvent(N Sub C(Of T1).N(Of T2)(y As Integer = 12) @ TestQueuedSymbols.vb: (8,8)-(8,56))",
                            "CompilationUnitCompletedEvent(TestQueuedSymbols.vb)",
                            "CompilationCompletedEvent")
            Assert.True(q.IsCompleted)
        End Sub

        <Fact>
        Public Sub TestQueuedSymbolsAndGetUsedAssemblyReferences()
            Dim source =
    <compilation>
        <file name="TestQueuedSymbols.vb"><![CDATA[
Namespace N
    Partial Class C(Of T1)
        Partial Private Sub M(x1 As Integer)
        End Sub
        Friend Property P As Integer
        Dim F As Integer = 12
        Dim G As Integer
        Dim H, I As Integer
        Private Sub N(Of T2)(Optional y As Integer = 12)
            F = F + y
        End Sub
    End Class
    Partial Class C(Of T1)
        Private Sub M(x1 As Integer)
        End Sub
    End Class
    Module Mod1
        Private Sub M2(): End Sub
    End Module
    Enum Color
        Red
        Green
        Blue
    End Enum
End Namespace
]]>
        </file>
    </compilation>

            Dim q = New AsyncQueue(Of CompilationEvent)()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q)
            comp.GetUsedAssemblyReferences()
            VerifyEvents(q)

            q = New AsyncQueue(Of CompilationEvent)()
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q)
            comp.VerifyDiagnostics()
            comp.GetUsedAssemblyReferences()
            VerifyEvents(q)

            q = New AsyncQueue(Of CompilationEvent)()
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q)
            comp.GetUsedAssemblyReferences()
            comp.VerifyDiagnostics()
            VerifyEvents(q)

            q = New AsyncQueue(Of CompilationEvent)()
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q)
            comp.GetUsedAssemblyReferences()
            comp.GetUsedAssemblyReferences()
            VerifyEvents(q)
        End Sub

        <Fact>
        <WorkItem(1958, "https://github.com/dotnet/roslyn/issues/1958")>
        Public Sub TestMyEvents()
            Dim source =
    <compilation>
        <file name="TestMyEvents.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(My.Computer.Clock.LocalTime)
    End Sub
End Module
]]>
        </file>
    </compilation>
            Dim q = New AsyncQueue(Of CompilationEvent)()
            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePairUtil.Create("_MyType", CObj("Console")))
            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compilationOptions = TestOptions.ReleaseExe.WithParseOptions(parseOptions)
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=compilationOptions).WithEventQueue(q)
            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            compilation.VerifyDiagnostics()
            VerifyEvents(q,
                "CompilationStartedEvent",
                "SymbolDeclaredCompilationEvent(<empty>  @ TestMyEvents.vb: (0,0)-(5,0))",
                "SymbolDeclaredCompilationEvent(Module1 Module1 @ TestMyEvents.vb: (0,0)-(4,10))",
                "SymbolDeclaredCompilationEvent(Main Sub Module1.Main() @ TestMyEvents.vb: (1,4)-(1,14))",
                "CompilationUnitCompletedEvent(TestMyEvents.vb)",
                "CompilationCompletedEvent")
            Assert.True(q.IsCompleted)
        End Sub
    End Class

End Namespace
