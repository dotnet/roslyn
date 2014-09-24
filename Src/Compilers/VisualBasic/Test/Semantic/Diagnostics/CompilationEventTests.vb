' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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
            While queue.Count > 0 OrElse Not queue.IsCompleted
                Dim te = queue.DequeueAsync(CancellationToken.None)
                Assert.True(te.IsCompleted)
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
                Console.WriteLine("MISING EVENTS:")
            End If
            For Each e In expected
                Console.WriteLine(e)
            Next
            If unexpected OrElse expected.Count <> 0 Then
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
End Namespace
]]>
        </file>
    </compilation>

            Dim q = New AsyncQueue(Of CompilationEvent)(CancellationToken.None)
            CreateCompilationWithMscorlibAndVBRuntime(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithEventQueue(q).VerifyDiagnostics().VerifyDiagnostics()
            VerifyEvents(q,
                "CompilationStartedEvent",
                "SymbolDeclaredCompilationEvent(Mod1 Mod1 @ TestQueuedSymbols.vb: (14,4)-(16,14))",
                "SymbolDeclaredCompilationEvent(P Property C(Of T1).P As Integer @ TestQueuedSymbols.vb: (4,8)-(4,36))",
                "SymbolDeclaredCompilationEvent(F C(Of T1).F As Integer @ TestQueuedSymbols.vb: (5,12)-(5,13))",
                "SymbolDeclaredCompilationEvent(M Sub C(Of T1).M(x1 As Integer) @ TestQueuedSymbols.vb: (2,8)-(2,44))",
                "SymbolDeclaredCompilationEvent(M2 Sub Mod1.M2() @ TestQueuedSymbols.vb: (15,8)-(15,24))",
                "SymbolDeclaredCompilationEvent(C C(Of T1) @ TestQueuedSymbols.vb: (1,4)-(9,13), TestQueuedSymbols.vb: (10,4)-(13,13))",
                "SymbolDeclaredCompilationEvent(N N @ TestQueuedSymbols.vb: (0,10)-(0,11))",
                "SymbolDeclaredCompilationEvent(<empty>  @ TestQueuedSymbols.vb: (0,0)-(18,0))",
                "SymbolDeclaredCompilationEvent(M Sub C(Of T1).M(x1 As Integer) @ TestQueuedSymbols.vb: (11,8)-(11,36))",
                "SymbolDeclaredCompilationEvent(get_P Property Get C(Of T1).P() As Integer)",
                "SymbolDeclaredCompilationEvent(set_P Property Set C(Of T1).P(AutoPropertyValue As Integer))",
                "SymbolDeclaredCompilationEvent(N Sub C(Of T1).N(Of T2)(y As Integer = 12) @ TestQueuedSymbols.vb: (6,8)-(6,56))",
                "CompilationUnitCompletedEvent(TestQueuedSymbols.vb)",
                "CompilationCompletedEvent")
        End Sub
    End Class

End Namespace
