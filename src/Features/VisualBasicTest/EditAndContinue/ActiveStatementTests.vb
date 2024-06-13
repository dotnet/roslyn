' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Contracts.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <UseExportProvider>
    Public Class ActiveStatementTests
        Inherits EditingTestBase

        <Fact>
        Public Sub Update_Inner()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main()
        While True
            <AS:1>Goo(2)</AS:1>
        End While
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo(2)"))
        End Sub

        <Fact>
        Public Sub Update_Inner_NewCommentAtEndOfActiveStatement()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>' comment
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Update_Inner_Reloadable()
            Dim src1 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C
    Shared Sub Main()
        While True
            <AS:1>Goo(2)</AS:1>
        End While
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo(2)"))
        End Sub

        <Fact>
        Public Sub Update_Leaf()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main()
        While True
            <AS:1>Goo(1)</AS:1>
        End While
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a + 1)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Update_Leaf_NewCommentAtEndOfActiveStatement()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>'comment
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        ' <summary>
        ' CreateNewOnMetadataUpdate has no effect in presence of active statements (in break mode).
        ' </summary>
        <Fact>
        Public Sub Update_Leaf_Reloadable()

            Dim src1 = ReloadableAttributeSrc + "
<CreateNewOnMetadataUpdate>
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>System.Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim src2 = ReloadableAttributeSrc + "
<CreateNewOnMetadataUpdate>
Class C
    Shared Sub Main()
        While True
            <AS:1>Goo(1)</AS:1>
        End While
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a + 1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemantics(active,
                semanticEdits:=
                {
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"), preserveLocalVariables:=True),
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Goo"), preserveLocalVariables:=True)
                })
        End Sub

        <Fact>
        Public Sub Headers()
            Dim src1 = "
Class C
    Property P(a As Integer) As Integer
        <AS:7>Get</AS:7>
            <AS:0>Me.value = a</AS:0>
        <AS:8>End Get</AS:8>
        <AS:9>Set(value As Integer)</AS:9>
            <AS:1>Me.value = a</AS:1>
        <AS:10>End Set</AS:10>
    End Property

    Custom Event E As Action
        <AS:11>AddHandler(value As Action)</AS:11>
            <AS:2>Me.value = a</AS:2>
        <AS:12>End AddHandler</AS:12>
        <AS:13>RemoveHandler(value As Action)</AS:13>
            <AS:3>Me.value = a</AS:3>
        <AS:14>End RemoveHandler</AS:14>
        <AS:15>RaiseEvent()</AS:15>
            <AS:4>Me.value = a</AS:4>
        <AS:16>End RaiseEvent</AS:16>
    End Event

    <AS:17>Shared Operator &(a As C, b As C) As C</AS:17>
        <AS:5>Me.value = a</AS:5>
    <AS:18>End Operator</AS:18>

    <AS:19>Sub New(a As C, b As C)</AS:19>
        <AS:6>Me.value = a</AS:6>
    <AS:20>End Sub</AS:20>

    <AS:21>Sub F(a As C, b As C)</AS:21>
        <AS:22>Me.value = a</AS:22>
    <AS:23>End Sub</AS:23>

    <AS:24>Function G(a As C, b As C) As Integer</AS:24>
        <AS:25>Me.value = a</AS:25>
        Return 0
    <AS:26>End Function</AS:26>
End Class
"

            Dim src2 = "
Class C
    Property P(a As Integer) As Integer
        <AS:7>Get</AS:7>
            <AS:0>Me.value = a*1</AS:0>
        <AS:8>End Get</AS:8>
        <AS:9>Set(value As Integer)</AS:9>
            <AS:1>Me.value = a*2</AS:1>
        <AS:10>End Set</AS:10>
    End Property

    Custom Event E As Action
        <AS:11>AddHandler(value As Action)</AS:11>
            <AS:2>Me.value = a*3</AS:2>
        <AS:12>End AddHandler</AS:12>
        <AS:13>RemoveHandler(value As Action)</AS:13>
            <AS:3>Me.value = a*4</AS:3>
        <AS:14>End RemoveHandler</AS:14>
        <AS:15>RaiseEvent()</AS:15>
            <AS:4>Me.value = a*5</AS:4>
        <AS:16>End RaiseEvent</AS:16>
    End Event

    <AS:17>Shared Operator &(a As C, b As C) As C</AS:17>
        <AS:5>Me.value = a*6</AS:5>
    <AS:18>End Operator</AS:18>

    <AS:19>Sub New(a As C, b As C)</AS:19>
        <AS:6>Me.value = a*7</AS:6>
    <AS:20>End Sub</AS:20>

    <AS:21>Sub F(a As C, b As C)</AS:21>
        <AS:22>Me.value = a*8</AS:22>
    <AS:23>End Sub</AS:23>

    <AS:24>Function G(a As C, b As C) As Integer</AS:24>
        <AS:25>Me.value = a*8</AS:25>
    <AS:26>End Function</AS:26>
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*6"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*7"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*8"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*8"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*2"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*3"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*4"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Me.value = a*5"))
        End Sub

#Region "Delete"
        <Fact>
        Public Sub Delete_Leaf_Method()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
<AS:0>Class C</AS:0>
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Class C", GetResource("method", "C.Goo(Integer)")))
        End Sub

        <Fact>
        Public Sub Delete_All_SourceText()
            Dim src1 = "
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim src2 = ""

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.class_, "C")))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Inner()
            Dim src1 = " 
Class C
    Shared Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main()
        While True
        End While
    <AS:1>End Sub</AS:1>

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Shared Sub Main()", FeaturesResources.code))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Inner_MultipleParents()
            Dim src1 = "
Class C 
    Shared Sub Main()
        Do
            <AS:1>Goo(1)</AS:1>
        Loop

        If True
            <AS:2>Goo(2)</AS:2>
        Else
            <AS:3>Goo(3)</AS:3>
        End If

        Dim x As Integer = 1
        Select Case x
            Case 1, 2
                <AS:4>Goo(4)</AS:4>

            Case Else
                <AS:5>Goo(5)</AS:5>
        End Select

        While True
            <AS:6>Goo(4)</AS:6>
        End While

        Do Until True
            <AS:7>Goo(7)</AS:7>
        Loop

        If True Then <AS:8>Goo(8)</AS:8> Else <AS:9>Goo(9)</AS:9>

        For i = 0 To 10 : <AS:10>Goo(10)</AS:10> : Next

        For Each i in {1, 2} : <AS:11>Goo(11)</AS:11> : Next

        Using z = new C() : <AS:12>Goo(12)</AS:12> : End Using

        With expr 
            <AS:13>.Bar = Goo(13)</AS:13>
        End With

        <AS:14>label:</AS:14>
        Console.WriteLine(1)

        SyncLock Nothing : <AS:15>Goo(15)</AS:15> : End SyncLock
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C 
    Shared Sub Main()
        Do
        <AS:1>Loop</AS:1>

        <AS:2>If True</AS:2>
        <AS:3>Else</AS:3>
        End If

        Dim x As Integer = 1
        Select Case x
            <AS:4>Case 1, 2</AS:4>
            <AS:5>Case Else</AS:5>
        End Select

        While True
        <AS:6>End While</AS:6>

        Do Until True
        <AS:7>Loop</AS:7>

        <AS:8>If True Then</AS:8> <AS:9>Else</AS:9>

        For i = 0 To 10 : <AS:10>Next</AS:10>

        For Each i In {1, 2} : <AS:11>Next</AS:11>

        Using z = New C() : <AS:12>End Using</AS:12>

        With expr 
        <AS:13>End With</AS:13>

        <AS:14>Console.WriteLine(1)</AS:14>

        SyncLock Nothing : <AS:15>End SyncLock</AS:15> 
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Do", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "If True", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Else", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Case 1, 2", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Case Else", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "While True", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Do Until True", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "If True Then", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Else", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "For i = 0 To 10", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "For Each i In {1, 2}", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Using z = New C()", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "With expr", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Shared Sub Main()", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "SyncLock Nothing", FeaturesResources.code))
        End Sub

        <Fact>
        Public Sub Delete_Inner_ElseIf1()
            Dim src1 = "
Class C 
    Shared Sub Main()
        If c1 Then
            Console.WriteLine(1)
        <AS:1>ElseIf c2 Then</AS:1>
            Console.WriteLine(2)
        ElseIf c3 Then
            Console.WriteLine(3)
        End If
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C 
    Shared Sub Main()
        If c1 Then
            Console.WriteLine(1)
        <AS:1>ElseIf c3 Then</AS:1>
            Console.WriteLine(3)
        End If
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "If c1 Then", FeaturesResources.code))
        End Sub

        <Fact>
        Public Sub Delete_Inner_ElseIf2()
            Dim src1 = <![CDATA[
Class C 
    Shared Sub Main()
        If c1 Then
            Console.WriteLine(1)
        <AS:1>ElseIf c2 Then</AS:1>
            Console.WriteLine(2)
        End If
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
]]>.Value

            Dim src2 = <![CDATA[
Class C 
    Shared Sub Main()
        If c1 Then
            Console.WriteLine(1)
        <AS:1>End If</AS:1>
    End Sub

    Shared Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "If c1 Then", FeaturesResources.code))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
    <AS:0>End Sub</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Leaf_InTry()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        Try
            <AS:0>Console.WriteLine(a)</AS:0>
        Catch
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        <AS:0>Try</AS:0>
        Catch
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Leaf_InTry2()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        Try
            Try
                <AS:0>Console.WriteLine(a)</AS:0>
            Catch
            End Try
        Catch
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        <AS:0>Try</AS:0>
        Catch
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Inner_CommentActiveStatement()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        'Goo(1)
    <AS:1>End Sub</AS:1>

    Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Sub Main()", FeaturesResources.code))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub Delete_Leaf_CommentActiveStatement()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        <AS:0>Console.WriteLine(a)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo(1)</AS:1>
    End Sub

    Sub Goo(a As Integer)
        'Console.WriteLine(a)
    <AS:0>End Sub</AS:0>
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Delete_EntireNamespace()
            Dim src1 = "
Module Module1
    Sub Main()
        <AS:0>Console.WriteLine(0)</AS:0>
    End Sub
End Module
"

            Dim src2 = "<AS:0/>"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "", GetResource("Module", "Module1")),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "", GetResource("method", "Module1.Main()")))
        End Sub
#End Region

#Region "Constructors"
        <Fact>
        Public Sub Updated_Inner_Constructor()
            Dim src1 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f As Goo = New Goo(5)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    Sub New(a As Integer)
        <AS:0>Me.value = a</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f As Goo = New Goo(5*2)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    Sub New(a As Integer)
        <AS:0>Me.value = a</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                                        Diagnostic(RudeEditKind.ActiveStatementUpdate, "f As Goo = New Goo(5*2)"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/741249")>
        Public Sub Updated_Leaf_Constructor()
            Dim src1 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f As Goo = New Goo(5)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    Sub New(a As Integer)
        <AS:0>Me.value = a</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f As Goo = New Goo(5)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    Sub New(a As Integer)
        <AS:0>Me.value = a*2</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Updated_Leaf_Constructor_Parameter()
            Dim src1 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f = new Goo(5)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    <AS:0>Public Sub New(Optional a As Integer = 1)</AS:0>
        Me.value = a
    End Sub
End Class
"
            Dim src2 = "
Class Program
    Shared Sub Main()
        Dim <AS:1>f = new Goo(5)</AS:1>
    End Sub
End Class

Class Goo
    Dim value As Integer

    <AS:0>Public Sub New(Optional a As Integer = 2)</AS:0>
        Me.value = a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InitializerUpdate, "Optional a As Integer = 2", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub Updated_Leaf_Constructor_Parameter_DefaultValue()
            Dim src1 = "
Class Goo
    Dim value As Integer
   
    Shared Sub Main()
        Dim <AS:1>f = new Goo(5)</AS:1>
    End Sub

    <AS:0>Sub New(Optional a As Integer = 5)</AS:0>
        Me.value = a
    End Sub
End Class
"
            Dim src2 = "
Class Goo
    Dim value As Integer
   
    Shared Sub Main()
        Dim <AS:1>f = new Goo(5)</AS:1>
    End Sub

    <AS:0>Sub New(Optional a As Integer = 42)</AS:0>
        Me.value = a
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InitializerUpdate, "Optional a As Integer = 42", FeaturesResources.parameter))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")>
        Public Sub Updated_Leaf_ConstructorChaining1()
            Dim src1 = "
Class B 
    Inherits A

    Sub Main
       Dim <AS:1>b = new B(2, 3)</AS:1>
    End Sub

    Sub New(x As Integer, y As Integer)
        <AS:0>MyBase.New(x + y, x - y)</AS:0> 
    End Sub
End Class
"

            Dim src2 = "
Class B 
    Inherits A

    Sub Main
       Dim <AS:1>b = new B(2, 3)</AS:1>
    End Sub

    Sub New(x As Integer, y As Integer)
        <AS:0>MyBase.New(x + y, x - y + 5)</AS:0> 
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")>
        Public Sub Updated_Leaf_ConstructorChaining2()
            Dim src1 = "
Class Test
    Sub Main()
       Dim <AS:2>b = new B(2, 3)</AS:2>
    End Sub
End Class

Class B
    Inherits A

    Sub New(x As Integer, y As Integer)
        <AS:1>MyBase.New(x + y, x - y)</AS:1>
    End Sub
End Class

Class A
    Sub New(x As Integer, y As Integer)
        <AS:0>Me.New(x, y, 0)</AS:0>
    End Sub

    Sub New(x As Integer, y As Integer, z As Integer) : End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
       Dim <AS:2>b = new B(2, 3)</AS:2>
    End Sub
End Class

Class B
    Inherits A

    Sub New(x As Integer, y As Integer)
        <AS:1>MyBase.New(x + y, x - y)</AS:1>
    End Sub
End Class

Class A
    Sub New(x As Integer, y As Integer)
        <AS:0>Me.New(5 + x, y, 0)</AS:0>
    End Sub

    Sub New(x As Integer, y As Integer, z As Integer) : End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstanceConstructorWithoutInitializer()
            Dim src1 = "
Class C
    Dim a As Integer = 5

    <AS:0>Public Sub New(a As Integer)</AS:0>
    End Sub

    Sub Main()
        Dim <AS:1>c = new C(3)</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Dim a As Integer = 42

    <AS:0>Public Sub New(a As Integer)</AS:0>
    End Sub

    Sub Main()
        Dim <AS:1>c = new C(3)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstanceConstructorWithInitializer_Internal_Update1()
            Dim src1 = "
Class C 
    Sub New(a As Integer)
        <AS:2>MyClass.New(True)</AS:2>
    End Sub

    Sub New(b As Boolean)
        <AS:1>MyBase.New(F())</AS:1>
    End Sub

    Shared Function F() As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim <AS:3>c As C = New C(3)</AS:3>
    End Sub
End Class
"
            Dim src2 = "
Class C 
    Sub New(a As Integer)
        <AS:2>MyClass.New(False)</AS:2>
    End Sub

    Sub New(b As Boolean)
        <AS:1>MyBase.New(F())</AS:1>
    End Sub

    Shared Function F() As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim <AS:3>c As C = New C(3)</AS:3>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "MyClass.New(False)"))
        End Sub

        <Fact>
        Public Sub InstanceConstructorWithInitializer_Leaf_Update1()
            Dim src1 = "
Class C
    Public Sub New() 
        <AS:0>MyBase.New(1)</AS:0>
    End Sub

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Public Sub New() 
        <AS:0>MyBase.New(2)</AS:0>
    End Sub

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")>
        Public Sub InstanceConstructorWithInitializer_Leaf_DeleteBaseCall()
            Dim src1 = "
Class C
    Sub New() 
        <AS:0>MyBase.New(2)</AS:0>
    End Sub

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub New()
    <AS:0>End Sub</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstanceConstructorWithInitializer_Leaf_InsertBaseCall()
            Dim src1 = "
Class C
    <AS:0>Public Sub New</AS:0>
    End Sub

    Sub Main
        Dim <AS:1>c = new C()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    <AS:0>Public Sub New</AS:0>
        MyBase.New(2)
    End Sub

    Sub Main
        Dim <AS:1>c = new C()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

#End Region

#Region "Initializers"

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_Init()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_AsNew()
            Dim src1 = "
Class C
    Dim <AS:0>a As New Integer</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a As New Integer</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_SharedAsNew()
            Dim src1 = "
Class C
    Dim <AS:0>a</AS:0>, b, c As New Integer

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a</AS:0>, b, c As New Integer

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_PropertyInitializer()
            Dim src1 = "
Class C
    Property <AS:0>P As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_PropertyInitializerUntyped()
            Dim src1 = "
Class C
    Property <AS:0>P = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P = 1</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_PropertyAsNewInitializer()
            Dim src1 = "
Class C
    Property <AS:0>P As New C()</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P As New C()</AS:0>

    Sub Main
        Dim <AS:1>b = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_ArrayInitializer_Untyped()
            Dim src1 = "
Class C
    Dim <AS:0>A(10)</AS:0>

    Sub Main
        Dim <AS:1>A(10)</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>A(10)</AS:0>

    Sub Main
        Dim <AS:1>A(10)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836523")>
        Public Sub Initializer_Unedited_ArrayInitializer_Typed()
            Dim src1 = "
Class C
    Dim <AS:0>A(10)</AS:0>, B(2) As Integer

    Sub Main
        Dim <AS:1>A(10)</AS:1>, B(2) As Integer
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>A(10)</AS:0>, B(2) As Integer

    Sub Main
        Dim <AS:1>A(10)</AS:1>, B(2) As Integer
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstancePropertyInitializer_Update()
            Dim src1 = "
Class C
    Property <AS:0>P As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P As Integer = 2</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstancePropertyAsNewInitializer_Update()
            Dim src1 = "
Class C
    Property <AS:0>P As New C(1)</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P As New C(2)</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub InstancePropertyInitializer_Delete()
            Dim src1 = "
Class C
    Property <AS:0>P As Integer = 1</AS:0>

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    <AS:0>Property P</AS:0> As Integer

    Sub Main
        Dim <AS:1>c = New C()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_Update1()
            Dim src1 = "
Class C
    Dim <AS:0>a = 1</AS:0>, b = 2

    Sub Main
        Dim <AS:1>c = 1</AS:1>, d = 2
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a = 2</AS:0>, b = 2

    Sub Main
        Dim <AS:1>c = 2</AS:1>, d = 2
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c = 2"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_Update2()
            Dim src1 = "
Class C
    Dim a = 1, <AS:0>b = 2</AS:0>

    Sub Main
        Dim c = 1, <AS:1>d = 2</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a = 1, <AS:0>b = 3</AS:0>

    Sub Main
        Dim c = 1, <AS:1>d = 3</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "d = 3"))
        End Sub

        <Fact>
        Public Sub Initializer_SemanticError()
            Dim src1 = "
Class C
    Dim <AS:0>a</AS:0>, <AS:1>b</AS:1> = 2

    Sub Main
        Dim <AS:2>a</AS:2>, <AS:3>b</AS:3> = 2
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a, b = 20

    Sub Main
        Dim <AS:2>a</AS:2>, <AS:3>b</AS:3> = 20
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            ' Since the code is semantically incorrect it's acceptable to misreport active statements.
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub FieldInitializer_AsNewToInit()
            Dim src1 = "
Class C
    Dim <AS:0>a As New Integer</AS:0>

    Sub Main
        Dim <AS:1>b As New Integer</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a As Integer = 0</AS:0>

    Sub Main
        Dim <AS:1>b As Integer = 0</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b As Integer = 0"))
        End Sub

        <Fact>
        Public Sub PropertyInitializer_AsNewToInit()
            Dim src1 = "
Class C
    Property <AS:0>P As New Integer()</AS:0>
End Class
"

            Dim src2 = "
Class C
    Property <AS:0>P As Integer = 0</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_InitToAsNew()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 0</AS:0>

    Sub Main
        Dim <AS:1>b As Integer = 0</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a As New Integer</AS:0>

    Sub Main
        Dim <AS:1>b As New Integer</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b As New Integer"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_AsNewMulti_Update1()
            Dim src1 = "
Class C
    Dim <AS:1>a</AS:1>, b As New D(1)
    Dim c, <AS:2>d</AS:2> As New D(1)
    Dim <AS:3>e</AS:3>, <AS:4>f</AS:4> As New D(1)

    Sub Main
        Dim <AS:5>x</AS:5>, y As New D(1)
    <AS:0>End Sub</AS:0>
End Class
"

            Dim src2 = "
Class C
    Dim <AS:1>a</AS:1>, b As New D(2)
    Dim c, <AS:2>d</AS:2> As New D(2)
    Dim <AS:3>e</AS:3>, <AS:4>f</AS:4> As New D(2)

    Sub Main
        Dim <AS:5>x</AS:5>, y As New D(2)
    <AS:0>End Sub</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "a"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "d"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "e"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "f"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "x"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_AsNewMulti_Update3()
            Dim src1 = "
Class C
    Private <AS:0>a As New D(1)</AS:0>
    Private <AS:1>e As New D(1)</AS:1>

    Sub Main
        Dim <AS:2>c As New D(1)</AS:2>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Private <AS:0>a</AS:0>, b As New D(2)
    Private <AS:1>e</AS:1>, f As New D(2)

    Sub Main
        Dim <AS:2>c</AS:2>, d As New D(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "e"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_AsNewMulti_Update4()
            Dim src1 = "
Class C
    Private <AS:0>a</AS:0>, b As New D(1)
    Private <AS:1>e</AS:1>, f As New D(1)

    Sub Main
        Dim <AS:2>c</AS:2>, d As New D(1)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Private <AS:0>a As New D(2)</AS:0>
    Private <AS:1>e As New D(2)</AS:1>

    Sub Main
        Dim <AS:2>c As New D(2)</AS:2>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c As New D(2)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "e As New D(2)"),
                Diagnostic(RudeEditKind.Delete, "a As New D(2)", GetResource("field", "b")),
                Diagnostic(RudeEditKind.Delete, "e As New D(2)", GetResource("field", "f")))
        End Sub

        <Fact>
        Public Sub Initializer_AsNewMulti_DeleteInsert()
            Dim srcA1 = "
Partial Class C
    Private <AS:0>a</AS:0>, b As New D(0)
End Class
"
            Dim srcB1 = "
Partial Class C
End Class
"

            Dim srcA2 = "
Partial Class C
    Private <AS:0>b As New D(1)</AS:0>
End Class
"

            Dim srcB2 = "
Partial Class C
    Private a As New D(0)
End Class
"

            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        GetActiveStatements(srcA1, srcA2),
                        diagnostics:=
                        {
                            Diagnostic(RudeEditKind.DeleteActiveStatement, "Partial Class C", GetResource("field", "a"))
                        }),
                    DocumentResults(
                        GetActiveStatements(srcB1, srcB2),
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        })
                })
        End Sub

        <Fact>
        Public Sub Initializer_AsNewMulti_WithLambda1()
            Dim src1 = "
Class C
    Private a As New D(Function() <AS:0>1</AS:0>)
End Class
"

            Dim src2 = "
Class C
    Private a As New D(Function() <AS:0>2</AS:0>)
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815933")>
        Public Sub Initializer_AsNewMulti_WithLambda2()
            Dim src1 = "
Class C
    Private a As New D(1, <AS:0>Sub()</AS:0>
                                End Sub)
End Class
"

            Dim src2 = "
Class C
    Private a As New D(2, <AS:0>Sub()</AS:0>
                                End Sub)
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Initializer_AsNewMulti_WithLambda3()
            Dim src1 = "
Class C
    Private a, b As New D(Function() <AS:0>1</AS:0>)
End Class
"

            Dim src2 = "
Class C
    Private a, b As New D(Function() <AS:0>2</AS:0>)
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Initializer_Array_SemanticError()
            Dim src1 = "
Class C
    Dim <AS:0>a(1)</AS:0>, <AS:1>b(2)</AS:1> = 3

    Sub Main
        Dim <AS:2>c(1)</AS:2>, <AS:3>d(2)</AS:3> = 3
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a(10)</AS:0>, <AS:1>b(20)</AS:1> = 30

    Sub Main
        Dim <AS:2>c(10)</AS:2>, <AS:3>d(20)</AS:3> = 30
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            ' since the code is semantically incorrect we only care about not crashing
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c(10)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "d(20)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b(20)"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        Public Sub Initializer_Array_Update1()
            Dim src1 = "
Class C
    Dim <AS:0>a(1)</AS:0>, b(1) As Integer

    Sub Main
        Dim <AS:1>c(1)</AS:1>, d(1) As Integer
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>a(2)</AS:0>, b(1) As Integer

    Sub Main
        Dim <AS:1>c(2)</AS:1>, d(1) As Integer
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c(2)"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        Public Sub Initializer_Array_Update2()
            Dim src1 = "
Class C
    Dim a(1), <AS:0>b(1)</AS:0> As Integer

    Sub Main
        Dim c(1), <AS:1>d(1)</AS:1> As Integer
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a(1), <AS:0>b(2)</AS:0> As Integer

    Sub Main
        Dim c(1), <AS:1>d(2)</AS:1> As Integer
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "d(2)"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        Public Sub Initializer_Array_Update3()
            Dim src1 = "
Class C
    Private <AS:0>a(1,1)</AS:0>
    Private <AS:1>e(1,0)</AS:1>
    Private f(1,0)

    Sub Main
        Dim <AS:2>c(1,0)</AS:2>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Private <AS:0>a(1,0)</AS:0>
    Private <AS:1>e(1,2)</AS:1>
    Private f(1,2)

    Sub Main
        Dim <AS:2>c(1,2)</AS:2>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "e(1,2)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c(1,2)"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        Public Sub Initializer_Array_WithLambda1()
            Dim src1 = "
Class C
    Private a(Function() <AS:0>1</AS:0>, Function() <AS:1>1</AS:1>)
End Class
"

            Dim src2 = "
Class C
    Private a(Function() <AS:0>2</AS:0>, Function() <AS:1>3</AS:1>)
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "3"))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        Public Sub Initializer_Array_WithLambda2()
            Dim src1 = "
Class C
    Private a(1, F(<AS:0>Sub()</AS:0>
                         End Sub))
End Class
"

            Dim src2 = "
Class C
    Private a(2, F(<AS:0>Sub()</AS:0>
                         End Sub))
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Initializer_Collection1()
            Dim src1 = "
Class C
    Dim <AS:0>A() = {1, 2, 3}</AS:0>
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>A() = {1, 2, 3, 4}</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Initializer_Collection2()
            Dim src1 = "
Class C
    Sub F
        Dim <AS:0>A() = {1, 2, 3}</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub F
        Dim <AS:0>A() = {1, 2, 3, 4}</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_InsertConst1()
            Dim src1 = "
Class C
    Private <AS:0>a As Integer = 1</AS:0>
End Class
"

            Dim src2 = "
Class C
    <AS:0>Private Const a As Integer = 1</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "Private Const a As Integer = 1", GetResource("field")))
        End Sub

        <Fact>
        Public Sub LocalInitializer_InsertConst1()
            Dim src1 = "
Class C
    Sub Goo
        Private <AS:0>a As Integer = 1</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Goo
        Private Const a As Integer = 1
    <AS:0>End Sub</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_InsertConst2()
            Dim src1 = "
Class C
    Private <AS:0>a As Integer = 1</AS:0>, b As Integer = 2
End Class
"

            Dim src2 = "
Class C
    <AS:0>Private Const a As Integer = 1, b As Integer = 2</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "Private Const a As Integer = 1, b As Integer = 2", GetResource("field")),
                Diagnostic(RudeEditKind.ModifiersUpdate, "Private Const a As Integer = 1, b As Integer = 2", GetResource("field")))
        End Sub

        <Fact>
        Public Sub LocalInitializer_InsertConst2()
            Dim src1 = "
Class C
    Sub Goo
        Dim <AS:0>a As Integer = 1</AS:0>, b As Integer = 2
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Goo
        Const a As Integer = 1, b As Integer = 2
    <AS:0>End Sub</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete1()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>
    Dim b As Integer = 2

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a As Integer
    Dim <AS:0>b As Integer = 2</AS:0>

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub LocalInitializer_Delete1()
            Dim src1 = "
Class C
    Sub F
        Dim <AS:0>a As Integer = 1</AS:0>
        Dim b As Integer = 2
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub F
        Dim a As Integer
        Dim <AS:0>b As Integer = 2</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete2()
            Dim src1 = "
Class C
    Dim b As Integer = 1
    Dim c As Integer
    Dim <AS:0>a = 1</AS:0>

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>b As Integer = 1</AS:0>
    Dim c As Integer
    Dim a

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete3()
            Dim src1 = "
Class C
    Dim b As Integer = 1
    Dim c As Integer
    Dim <AS:0>a = 1</AS:0>

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim <AS:0>b As Integer = 1</AS:0>
    Dim c As Integer

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Class C", GetResource("field", "C.a")),
                Diagnostic(RudeEditKind.Delete, "Class C", GetResource("field", "a")))
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_AsNew_Single()
            Dim src1 = "
Class C
    Dim <AS:0>a</AS:0> As New D()

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    <AS:0>Dim a As D</AS:0>

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_AsNew_Multi1()
            Dim src1 = "
Class C
    Dim a,<AS:0>b</AS:0>,c As New D()

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a,<AS:0>c</AS:0> As New D()

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Class C", GetResource("field", "C.b")),
                Diagnostic(RudeEditKind.Delete, "a,      c        As New D()", GetResource("field", "b")))
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_AsNew_Multi2()
            Dim src1 = "
Class C
    Dim a,b,<AS:0>c</AS:0> As New D()

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a,<AS:0>b</AS:0> As New D()

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Class C", GetResource("field", "C.c")),
                Diagnostic(RudeEditKind.Delete, "a,      b        As New D()", GetResource("field", "c")))
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_AsNew_WithLambda()
            Dim src1 = "
Class C
    Dim a As New D(<AS:0>Sub()</AS:0>
                   End Sub)

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    <AS:0>Dim a As D</AS:0>

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_Array1()
            Dim src1 = "
Class C
    Dim a,b,<AS:0>c(1)</AS:0> As Integer

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    <AS:0>Dim a,b As Integer</AS:0>

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Class C", GetResource("field", "C.c")),
                Diagnostic(RudeEditKind.Delete, "a,b As Integer", GetResource("field", "c")))
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_Array2()
            Dim src1 = "
Class C
    Dim a,b(1),<AS:0>c(1)</AS:0> As Integer

    Sub New
    End Sub
End Class
"

            Dim src2 = "
Class C
    Dim a,<AS:0>b(1)</AS:0>,c As Integer

    Sub New
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.TypeUpdate, "c", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_StaticInstanceMix1()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>
    Shared b As Integer = 1
    Dim c As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Dim a As Integer
    Shared b As Integer = 1
    Dim <AS:0>c As Integer = 1</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_StaticInstanceMix2()
            Dim src1 = "
Class C
    Shared c As Integer = 1
    Shared <AS:0>a As Integer = 1</AS:0>
    Dim b As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Shared <AS:0>c As Integer = 1</AS:0>
    Shared a As Integer
    Dim b As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_StaticInstanceMix3()
            Dim src1 = "
Class C
    Shared <AS:0>a As Integer = 1</AS:0>
    Dim b As Integer = 1
End Class
"

            Dim src2 = "
Class C
    <AS:0>Shared a As Integer</AS:0>
    Dim b As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub FieldInitializer_Delete_ArrayInit()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>
    Shared b As Integer = 1
    Dim c(1) As Integer
End Class
"

            Dim src2 = "
Class C
    Dim a As Integer
    Shared b As Integer = 1
    Dim <AS:0>c(1)</AS:0> As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub PropertyInitializer_Delete_StaticInstanceMix()
            Dim src1 = "
Class C
    Shared Property <AS:0>a As Integer = 1</AS:0>
    Property  b As Integer = 1
End Class
"

            Dim src2 = "
Class C
    <AS:0>Shared Property a</AS:0> As Integer
    Property b As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub PropertyFieldInitializer1()
            Dim src1 = "
Class C
    Property <AS:0>a As Integer = 1</AS:0>
    Dim b As Integer = 1
    Property c As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property a As Integer
    Dim <AS:0>b As Integer = 1</AS:0>
    Property c As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub PropertyFieldInitializer2()
            Dim src1 = "
Class C
    Dim <AS:0>a As Integer = 1</AS:0>
    Property b As Integer = 1
    Property c As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Dim a As Integer
    Property b As Integer
    Property <AS:0>c As Integer = 1</AS:0>
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub
#End Region

#Region "SyncLock"

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755749")>
        Public Sub SyncLock_Insert_Leaf()
            Dim src1 = "
Class Test
    Sub Main()
        <AS:0>System.Console.Write(5)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock lockThis
            <AS:0>System.Console.Write(5)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "SyncLock lockThis", VBFeaturesResources.SyncLock_block))
        End Sub

        <Fact>
        Public Sub SyncLock_Insert_Leaf4()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock a
            SyncLock b
                SyncLock c
                    <AS:0>System.Console.Write()</AS:0>
                End SyncLock
            End SyncLock
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock b
            SyncLock d
                SyncLock a
                    SyncLock e
                        <AS:0>System.Console.Write()</AS:0>
                    End SyncLock
                End SyncLock
            End SyncLock
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "SyncLock d", VBFeaturesResources.SyncLock_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "SyncLock e", VBFeaturesResources.SyncLock_block))
        End Sub

        <Fact>
        Public Sub SyncLock_Insert_Leaf5()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock a
            SyncLock c
                SyncLock b
                    SyncLock e
                        <AS:0>System.Console.Write()</AS:0>
                    End SyncLock
                End SyncLock
            End SyncLock
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock b
            SyncLock d
                SyncLock a
                    <AS:0>System.Console.Write()</AS:0>
                End SyncLock
            End SyncLock
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "SyncLock d", VBFeaturesResources.SyncLock_statement))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755752")>
        Public Sub SyncLock_Update_Leaf()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock lockThis
            <AS:0>System.Console.Write(5)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock ""test""
            <AS:0>System.Console.Write(5)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "SyncLock ""test""", VBFeaturesResources.SyncLock_statement))
        End Sub

        <Fact>
        Public Sub SyncLock_Update_Leaf2()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock lockThis
            System.Console.Write(5)
        End SyncLock
        <AS:0>System.Console.Write(5)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock  ""test""
            System.Console.Write(5)
        End SyncLock
        <AS:0>System.Console.Write(5)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub SyncLock_Delete_Leaf()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock lockThis
            <AS:0>System.Console.Write(5)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        <AS:0>System.Console.Write(5)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub SyncLock_Update_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock F(Function(a) a)
            <AS:0>System.Console.WriteLine(1)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock F(Function(a) a + 1)
            <AS:0>System.Console.WriteLine(2)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub SyncLock_Update_Lambda2()
            Dim src1 = "
Class Test
    Sub Main()
        SyncLock F(Function(a) a)
            <AS:0>System.Console.WriteLine(1)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        SyncLock G(Function(a) a)
            <AS:0>System.Console.WriteLine(2)</AS:0>
        End SyncLock
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "SyncLock G(Function(a) a)", VBFeaturesResources.SyncLock_statement))
        End Sub

#End Region

#Region "ForEach"

        <Fact>
        Public Sub ForEach_Reorder_Leaf1()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                For Each c In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"

            Dim src2 = "
Class Test
    Sub Main()
        For Each b In e1
            For Each c In e1
                For Each a In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Update_Leaf1()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                For Each c In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        For Each b In e1
            For Each c In e1
                For Each a In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Update_Leaf2()
            Dim src1 = "
Class Test
    Sub Main()
        <AS:0>System.Console.Write()</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class Test
    Sub Main()
        For Each b In e1
            For Each c In e1
                For Each a In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "For Each b In e1", VBFeaturesResources.For_Each_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "For Each c In e1", VBFeaturesResources.For_Each_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "For Each a In e1", VBFeaturesResources.For_Each_block))
        End Sub

        <Fact>
        Public Sub ForEach_Delete_Leaf1()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                For Each c In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"

            Dim src2 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                <AS:0>System.Console.Write()</AS:0>
            Next
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Delete_Leaf2()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                For Each c In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"

            Dim src2 = "
Class Test
    Sub Main()
        For Each b In e1
            For Each c In e1
                <AS:0>System.Console.Write()</AS:0>
            Next
        Next
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Delete_Leaf3()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each b In e1
                For Each c In e1
                    <AS:0>System.Console.Write()</AS:0>
                Next
            Next
        Next
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        For Each a In e1
            For Each c In e1
                <AS:0>System.Console.Write()</AS:0>
            Next
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        Dim a = Sub()
            <AS:0>System.Console.Write()</AS:0>
        End Sub

        <AS:1>a()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        For Each b In e1
            For Each c In e1
                Dim a = Sub()
                    For Each z In e1
                        <AS:0>System.Console.Write()</AS:0>
                    Next
                End Sub
            Next

            <AS:1>a()</AS:1>
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "For Each z In e1", VBFeaturesResources.For_Each_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "For Each b In e1", VBFeaturesResources.For_Each_block))
        End Sub

        <Fact>
        Public Sub ForEach_Update_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        Next
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        For Each a In F(Function(a) a + 1)
            <AS:0>System.Console.Write(2)</AS:0>
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub ForEach_Update_Lambda2()
            Dim src1 = "
Class Test
    Sub Main()
        For Each a In F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        Next
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        For Each a In G(Function(a) a)
            <AS:0>System.Console.Write(2)</AS:0>
        Next
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "For Each a In G(Function(a) a)", VBFeaturesResources.For_Each_statement))
        End Sub

#End Region

#Region "Using"

        <Fact>
        Public Sub Using_Update_Leaf1()
            Dim src1 = "
Class Test
    Sub Main()
        Using a
            Using b
                <AS:0>System.Console.Write()</AS:0>
            End Using
        End Using
    End sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        Using a
            Using c
                Using b
                    <AS:0>System.Console.Write()</AS:0>
                End Using
            End Using
        End Using
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active, Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Using c", VBFeaturesResources.Using_block))
        End Sub

        <Fact>
        Public Sub Using_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        Using a
            Dim z = Function()
                Using b
                    <AS:0>Return 1</AS:0>
                End Using
            End Function
        End Using

        <AS:1>a()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        Using d
            Dim z = Function()
                Using c
                    Using b
                        <AS:0>Return 1</AS:0>
                    End Using
                End Using
            End Function
        End Using

        <AS:1>a()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Using c", VBFeaturesResources.Using_block))
        End Sub

        <Fact>
        Public Sub Using_Update_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        Using F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        End Using
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        Using F(Function(a) a + 1)
            <AS:0>System.Console.Write(2)</AS:0>
        End Using
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Using_Update_Lambda2()
            Dim src1 = "
Class Test
    Sub Main()
        Using F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        End Using
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        Using G(Function(a) a)
            <AS:0>System.Console.Write(2)</AS:0>
        End Using
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Using G(Function(a) a)", VBFeaturesResources.Using_statement))
        End Sub

#End Region

#Region "With"

        <Fact>
        Public Sub With_Update_Leaf1()
            Dim src1 = "
Class Test
    Sub Main()
        With a
            With b
                <AS:0>System.Console.Write()</AS:0>
            End With
        End With
    End sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        With a
            With c
                With b
                    <AS:0>System.Console.Write()</AS:0>
                End With
            End With
        End With
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "With c", VBFeaturesResources.With_block))
        End Sub

        <Fact>
        Public Sub With_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        With a
            Dim z = Function()
                With b
                    <AS:0>Return 1</AS:0>
                End With
            End Function
        End With

        <AS:1>a()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        With d
            Dim z = Function()
                With c
                    With b
                        <AS:0>Return 1</AS:0>
                    End With
                End With
            End Function
        End With

        <AS:1>a()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "With c", VBFeaturesResources.With_block))
        End Sub

        <Fact>
        Public Sub With_Update_Lambda1()
            Dim src1 = "
Class Test
    Sub Main()
        With F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        End With
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        With F(Function(a) a + 1)
            <AS:0>System.Console.Write(1)</AS:0>
        End With
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub With_Update_Lambda2()
            Dim src1 = "
Class Test
    Sub Main()
        With F(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        End With
    End Sub
End Class
"
            Dim src2 = "
Class Test
    Sub Main()
        With G(Function(a) a)
            <AS:0>System.Console.Write(1)</AS:0>
        End With
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "With G(Function(a) a)", VBFeaturesResources.With_statement))
        End Sub

#End Region

#Region "Try, Catch, Finally"

        <Fact>
        Public Sub Try_Add_Inner()
            Dim src1 = "
Class C
    Shared Sub Bar()
        <AS:1>Goo()</AS:1>
    End Sub

    Shared Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()
        Try
            <AS:1>Goo()</AS:1>
        Catch
        End Try
    End Sub

    Shared Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Try", VBFeaturesResources.Try_block))
        End Sub

        <Fact>
        Public Sub Try_Add_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
            <AS:0>Console.WriteLine(1)</AS:0>
        Catch
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Try_Delete_Inner()
            Dim src1 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Catch 
        End Try</ER:1.0>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo()", VBFeaturesResources.Try_block))
        End Sub

        <Fact>
        Public Sub Try_Delete_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>        
    End Sub

    Sub Goo()
        Try
            <AS:0>Console.WriteLine(1)</AS:0>
        Catch
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>        
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Try_Update_Inner()
            Dim src1 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Catch
        End Try</ER:1.0>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        Catch e As IOException
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Try", VBFeaturesResources.Try_block))
        End Sub

        <Fact>
        Public Sub Try_Update_Inner2()
            Dim src1 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Catch
        End Try</ER:1.0>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Catch
        End Try</ER:1.0>
        Console.WriteLine(2)
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub TryFinally_Update_Inner()
            Dim src1 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Finally
        End Try</ER:1.0>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try
            <AS:1>Goo()</AS:1>
        <ER:1.0>Finally
        End Try</ER:1.0>
        Console.WriteLine(2)
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Try_Update_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
            <AS:0>Console.WriteLine(1)</AS:0>
        Catch
        End Try
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
            <AS:0>Console.WriteLine(1)</AS:0>
        Catch e As IOException
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub TryFinally_DeleteStatement_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <AS:0>Console.WriteLine(0)</AS:0>

        Try
            <AS:1>Console.WriteLine(1)</AS:1>
        <ER:1.0>Finally
            Console.WriteLine(2)
        End Try</ER:1.0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        <AS:0>Console.WriteLine(0)</AS:0>

        <AS:1>Try</AS:1>
        Finally
            Console.WriteLine(2)
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Try", FeaturesResources.code))
        End Sub

        <Fact>
        Public Sub TryFinally_DeleteStatement_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <ER:0.0>Try
            Console.WriteLine(0)
        Finally
            <AS:0>Console.WriteLine(1)</AS:0>
        End Try</ER:0.0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try
            Console.WriteLine(0)
        <AS:0>Finally</AS:0>
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Finally", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub Try_DeleteStatement_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <AS:0>Console.WriteLine(0)</AS:0>

        Try
            <AS:1>Console.WriteLine(1)</AS:1>
        <ER:1.0>Finally
            Console.WriteLine(2)
        End Try</ER:1.0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        <AS:0>Console.WriteLine(0)</AS:0>

        <AS:1>Try</AS:1>
        Finally
            Console.WriteLine(2)
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Try", FeaturesResources.code))
        End Sub

        <Fact>
        Public Sub Try_DeleteStatement_Leaf()
            Dim src1 = "
Class C
    Sub Main()

        Try
            <AS:0>Console.WriteLine(1)</AS:0>
        Finally
            Console.WriteLine(2)
        End Try
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()

        <AS:0>Try</AS:0>
        Finally
            Console.WriteLine(2)
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Catch_Add_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try
        Catch
            <AS:1>Goo()</AS:1>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Catch_Add_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        Catch 
            <AS:0>Console.WriteLine(1)</AS:0>
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Catch_Delete_Inner()
            Dim src1 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch 
            <AS:1>Goo()</AS:1></ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo()", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Catch_Delete_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>        
    End Sub

    Sub Goo()
        Try
        <ER:0.0>Catch
            <AS:0>Console.WriteLine(1)</AS:0></ER:0.0>
        End Try
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Console.WriteLine(1)", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Catch_Update_Inner()
            Dim src1 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch
            <AS:1>Goo()</AS:1></ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch e As IOException
            <AS:1>Goo()</AS:1></ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Catch_Update_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0>Catch
            <AS:0>Console.WriteLine(1)</AS:0></ER:0.0>
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0>Catch e As IOException
            <AS:0>Console.WriteLine(1)</AS:0></ER:0.0>
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub CatchFilter_Update_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <AS:0>Goo()</AS:0>
    End Sub

    Sub Goo()
        Try
        <ER:1.0><AS:1>Catch e As IOException When Goo(1)</AS:1>
            Console.WriteLine(1)</ER:1.0>
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:0>Goo()</AS:0>
    End Sub

    Sub Goo()
        Try
        <ER:1.0><AS:1>Catch e As IOException When Goo(2)</AS:1>
            Console.WriteLine(1)</ER:1.0>
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Catch e As IOException When Goo(2)"),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub CatchFilter_Update_Leaf1()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0><AS:0>Catch e As IOException When Goo(1)</AS:0>
            Console.WriteLine(1)</ER:0.0>
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0><AS:0>Catch e As IOException When Goo(2)</AS:0>
            Console.WriteLine(1)
        End Try</ER:0.0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub CatchFilter_Update_Leaf2()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0><AS:0>Catch e As IOException When Goo(1)</AS:0>
            Console.WriteLine(1)</ER:0.0>
        End Try
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        <ER:0.0><AS:0>Catch e As Exception When Goo(1)</AS:0>
            Console.WriteLine(1)
        End Try<ER:0.0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause))
        End Sub

        <Fact>
        Public Sub Finally_Add_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        Try
        Finally 
            <AS:1>Goo()</AS:1>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Finally", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub Finally_Add_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        Try
        Finally 
            <AS:0>Console.WriteLine(1)</AS:0>
        End Try
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active, Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Finally", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub Finally_Delete_Inner()
            Dim src1 = "
Class C
    Sub Main()
        <ER:1.0>Try
        Finally 
            <AS:1>Goo()</AS:1>
        End Try</ER:1.0>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo()", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub Finally_Delete_Leaf()
            Dim src1 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>        
    End Sub

    Sub Goo()
        <ER:0.0>Try
        Finally
            <AS:0>Console.WriteLine(1)</AS:0>
        End Try</ER:0.0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        <AS:1>Goo()</AS:1>
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Console.WriteLine(1)", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub TryCatchFinally()
            Dim src1 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch e As IOException
            Try
                Try
                    Try
                        <AS:1>Goo()</AS:1>
                    Catch 
                    End Try
                Catch Exception
                End Try
            Finally
            End Try</ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch e As Exception
            Try
                Try
                Finally
                    Try
                        <AS:1>Goo()</AS:1>
                    Catch 
                    End Try
                End Try
            Catch e As Exception
            End Try</ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Catch", VBFeaturesResources.Catch_clause),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Try", VBFeaturesResources.Try_block),
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo()", VBFeaturesResources.Try_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "Finally", VBFeaturesResources.Finally_clause))
        End Sub

        <Fact>
        Public Sub TryCatchFinally_Regions()
            Dim src1 = "
Class C
    Sub Main()
        Try
        <ER:1.0>Catch e As IOException
            Try
                Try
                    Try
                        <AS:1>Goo()</AS:1>
                    Catch 
                    End Try
                Catch e As Exception
                End Try
            Finally
            End Try</ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Try            
        <ER:1.0>Catch e As IOException
            Try : Try : Try : <AS:1>Goo()</AS:1> : Catch : End Try : Catch e As Exception : End Try : Finally : End Try</ER:1.0>
        End Try
    End Sub

    Sub Goo()
        <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Try_Lambda1()
            Dim src1 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f As Func(Of Integer, Integer) = Nothing
        Try
            f = Function(x) <AS:1>1 + Goo(x)</AS:1>
        Catch
        End Try

        <AS:2>Console.Write(f(2))</AS:2>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f As Func(Of Integer, Integer) = Nothing
        f = Function(x) <AS:1>1 + Goo(x)</AS:1>
        <AS:2>Console.Write(f(2))</AS:2>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Try_Lambda2()
            Dim src1 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f = Function(x) 
                    Try
                        <AS:1>Return 1 + Goo(x)</AS:1>
                    <ER:1.0>Catch
                    End Try</ER:1.0>
                End Function

        <AS:2>Console.Write(f(2))</AS:2>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f = Function(x) 
                     <AS:1>Return 1 + Goo(x)</AS:1>
                End Function

        <AS:2>Console.Write(f(2))</AS:2>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Return 1 + Goo(x)", VBFeaturesResources.Try_block))
        End Sub

        <Fact>
        Public Sub Try_Query_Join1()
            Dim src1 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Try
            q = From x In xs
                Join y In ys On <AS:1>F()</AS:1> Equals G()
                Select 1
        Catch
        End Try

        <AS:2>q.ToArray()</AS:2>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        q = From x In xs
            Join y In ys On <AS:1>F()</AS:1> Equals G()
            Select 1

        <AS:2>q.ToArray()</AS:2>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ComplexQueryExpression, "Join y In ys On       F()        Equals G()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Try_Query_Join2()
            Dim src1 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f = Sub()
                    Try
                        Dim q = From x In xs
                                Join y In ys On <AS:1>F()</AS:1> Equals G()
                                Select 1
                    Catch
                    End Try

                    <AS:2>q.ToArray()</AS:2>
                End Sub
    End Sub
End Class
"
            Dim src2 = "
Class C
    Function Goo(x As Integer) As Integer
        <AS:0>Return 1</AS:0>
    End Function

    Sub Main()
        Dim f = Sub()
                    Dim q = From x In xs
                            Join y In ys On <AS:1>F()</AS:1> Equals G()
                            Select 1

                    <AS:2>q.ToArray()</AS:2>
                End Sub
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ComplexQueryExpression, "Join y In ys On       F()        Equals G()", GetResource("Lambda")))
        End Sub
#End Region

#Region "Lambdas"
        <Fact>
        Public Sub Lambdas_SingleLineToMultiLine1()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a) <AS:0>1</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0>
                    Return 1
                End Function
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Lambdas_SingleLineToMultiLine2()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0> 1
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0>
                    Return 1
                End Function
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Lambdas_MultiLineToSingleLine1()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a)
                    <AS:0>Return 1</AS:0>
                End Function
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0> 1
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Lambdas_MultiLineToSingleLine2()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0>
                    Return 1
                End Function
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0> 1
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Lambdas_MultiLineToSingleLine3()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a)
                    Return 1
                <AS:0>End Function</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(a)</AS:0> 1
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub Lambdas_ActiveStatementRemoved1()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a)
            Return Function(b) <AS:0>b</AS:0>
        End Function

        Dim z = f(1)
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(b)</AS:0>  b

        Dim z = f
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ChangingLambdaReturnType, "Function(b)", GetResource("Lambda")),
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "Function(b)", GetResource("Lambda")))
        End Sub

        <Fact>
        Public Sub Lambdas_ActiveStatementRemoved2()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a) Function(b) <AS:0>b</AS:0>

        Dim z = f(1)
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim f = <AS:0>Function(b)</AS:0> b

        Dim z = f
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ChangingLambdaReturnType, "Function(b)", GetResource("Lambda")),
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "Function(b)", GetResource("Lambda")))
        End Sub

        <Fact>
        Public Sub Lambdas_ActiveStatementRemoved3()
            Dim src1 = "
Class C
    Sub Main()
        Dim f = Function(a)
            Dim z As Func(Of Integer, Integer)

            F(Function(b)
                <AS:0>Return b</AS:0>
            End Function, z)

            Return z
        End Function

        Dim z = f(1)
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        Dim f = Function(b)
            <AS:0>F(b)</AS:0>

            Return 1
        End Function

        Dim z = f
        <AS:1>z(2)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "F(b)", VBFeaturesResources.Lambda))
        End Sub

        <Fact>
        Public Sub Lambdas_ActiveStatementRemoved4()
            Dim src1 = "
Class C
    Shared Sub F()
        Dim f = Function(a)
            <AS:1>z(2)</AS:1>

            return Function (b)
                <AS:0>Return b</AS:0>
            End Function
        End Function
    End Sub
End Class"
            Dim src2 = "
Class C
    <AS:0,1>Shared Sub F()</AS:0,1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "Shared Sub F()", VBFeaturesResources.Lambda),
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "Shared Sub F()", VBFeaturesResources.Lambda))
        End Sub

        <Fact>
        Public Sub Queries_ActiveStatementRemoved_WhereClause()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a In b Where <AS:0>b.goo</AS:0> Select b.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select b.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Where_clause))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/841361")>
        Public Sub Queries_ActiveStatementRemoved_LetClause()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a In b Let x = <AS:0>a.goo</AS:0> Select x
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select x
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Let_clause))
        End Sub

        <Fact>
        Public Sub Queries_ActiveStatementRemoved_JoinClauseLeft()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a In b
                Join c In d On <AS:0>a.goo</AS:0> Equals c.bar
                Select a.bar

        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select a.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(
                active,
                Diagnostic(RudeEditKind.ComplexQueryExpression, "Sub Main()", GetResource("method")),
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Join_condition))
        End Sub

        <Fact>
        Public Sub Queries_ActiveStatementRemoved_OrderBy1()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a In b
                Order By <AS:0>a.x</AS:0>, a.y Descending, a.z Ascending
                Select a

        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select a.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Ordering_clause))
        End Sub

        <Fact>
        Public Sub Queries_ActiveStatementRemoved_OrderBy2()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a in b
                Order By a.x, <AS:0>a.y</AS:0> Descending, a.z Ascending
                Select a

        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select a.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Ordering_clause))
        End Sub

        <Fact>
        Public Sub Queries_ActiveStatementRemoved_OrderBy3()
            Dim src1 = "
Class C
    Sub Main()
        Dim s = From a in b
                Order By a.x, a.y Descending, <AS:0>a.z</AS:0> Ascending
                Select a

        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim s = <AS:0>From</AS:0> a In b Select a.bar
        <AS:1>s.ToArray()</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "From", VBFeaturesResources.Ordering_clause))
        End Sub

        <Fact>
        Public Sub MisplacedActiveStatement1()
            Dim src1 = "
<AS:1>Class C</AS:1>
    Function F(a As Integer) As Integer
        <AS:0>Return a</AS:0> 
        <AS:2>Return a</AS:2> 
    End Function
End Class
"
            Dim src2 = "
Class C
    Function F(a As Integer) As Integer
        <AS:0>return a</AS:0> 
        <AS:2>return a</AS:2> 
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MisplacedActiveStatement2()
            Dim src1 = "
Class C
    <AS:0><Attr></AS:0>
    Shared Sub Main()
    End Sub
End Class"
            Dim src2 = "
Class C
    <Attr>
    <AS:0>Shared Sub Main()</AS:0>
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MisplacedTrackingSpan1()
            Dim src1 = "
Class C
    <AS:0><Attr></AS:0>
    Shared Sub Main()
    End Sub
End Class"
            Dim src2 = "
Class C
    <TS:0><Attr></TS:0>
    <AS:0>Shared Sub Main()</AS:0>
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MisplacedTrackingSpan2()
            Dim src1 = "
Class C
    Dim f = <AS:0>1</AS:0>
End Class"
            Dim src2 = "
Class C
    <TS:0>Dim</TS:0> <AS:0>f = 1</AS:0>
End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MisplacedTrackingSpan3()
            Dim src1 = "
Class C
    Dim <AS:0>f</AS:0>, g As New C()
End Class"
            Dim src2 = "
Class C
    <TS:0>Dim</TS:0> <AS:0>f</AS:0>, g As New C()
End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MisplacedTrackingSpan4()
            Dim src1 = "
Class C
    <AS:0>Property</AS:0> f As New C()
End Class"
            Dim src2 = "
Class C
    <TS:0>Property</TS:0> <AS:0>f As New C()</AS:0>
End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1359")>
        Public Sub Lambdas_LeafEdits_GeneralStatement()
            Dim src1 = "
Class C
    Sub Main() 
        <AS:1>F(Function(a) <AS:0>1</AS:0>)</AS:1>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main() 
        <AS:1>F(Function(a) <AS:0>2</AS:0>)</AS:1>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1359")>
        Public Sub Lambdas_LeafEdits_NestedLambda()
            Dim src1 = "
Class C
    Sub Main() 
        <AS:2>F(Function(b) <AS:1>F(Function(a) <AS:0>1</AS:0>)</AS:1>)</AS:2>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main() 
        <AS:2>F(Function(b) <AS:1>G(Function(a) <AS:0>1</AS:0>)</AS:1>)</AS:2>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "G(Function(a)       1       )"))
        End Sub

#End Region

#Region "State Machines"
        <Fact>
        Public Sub MethodToIteratorMethod_WithActiveStatement()
            Dim src1 = "
Imports System
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Return {1, 1}
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Yield 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                 Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Iterator Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToIteratorMethod_WithActiveStatement_NoYield()
            Dim src1 = "
Imports System
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Iterator Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToIteratorMethod_WithActiveStatementInLambda()
            Dim src1 = "
Imports System
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        Dim a = Sub() <AS:0>Console.WriteLine(1)</AS:0>
        a()
        Return {1, 1}
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim a = Sub() <AS:0>Console.WriteLine(1)</AS:0>
        a()
        Yield 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub MethodToIteratorMethod_WithoutActiveStatement()
            Dim src1 = "
Imports System
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        Console.WriteLine(1)
        Return {1, 1}
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Console.WriteLine(1)
        Yield 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatement1()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Return Await Task.FromResult(1)
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatement2()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    <AS:0>Function F() As Task(Of Integer)</AS:0>
        Console.WriteLine(1)
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    <AS:0>Async Function F() As Task(Of Integer)</AS:0>
        Console.WriteLine(1)
        Return 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatement3()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        <AS:0>Console.WriteLine(1)</AS:0>
        Return 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatementInLambda1()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        Dim a = Sub() <AS:0>Console.WriteLine(1)</AS:0>
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim a = Sub() <AS:0>Console.WriteLine(1)</AS:0>
        Return Await Task.FromResult(1)
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatementInLambda2()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        Dim a = Sub() <AS:1>Console.WriteLine(1)</AS:1>
        <AS:0>a()</AS:0>
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim a = Sub() <AS:1>Console.WriteLine(1)</AS:1>
        <AS:0>a()</AS:0>
        Return 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function F()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatementInLambda3()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim a = Sub() <AS:0>Console.WriteLine(1)</AS:0>
        Return
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Sub F()
        Dim a = Async Sub() <AS:0>Console.WriteLine(1)</AS:0>
        Return
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Sub()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithActiveStatementInLambda4()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim a = Function() <AS:0>Task.FromResult(1)</AS:0>
        Return
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Sub F()
        Dim a = Async Function() <AS:0>1</AS:0>
        Return
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function()"))
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithoutActiveStatement1()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        Console.WriteLine(1)
        Return Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Console.WriteLine(1)
        Return Await Task.FromResult(1)
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod_WithoutActiveStatement2()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Console.WriteLine(1)
        Return
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Sub F()
        Console.WriteLine(1)
        Return
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub LambdaToAsyncLambda_WithActiveStatement_NoAwait()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Sub() <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Async Sub() <AS:0>Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Sub()"))
        End Sub

        <Fact>
        Public Sub LambdaToAsyncLambda_WithActiveStatement_NoAwait_Nested1()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Function(a) <AS:0>Task.FromResult(Function(b) 1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Async Function(a) <AS:0>Function(b) 1</AS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            ' Rude edit since the AS is within the outer function.
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Async Function(a)"))
        End Sub

        <Fact>
        Public Sub LambdaToAsyncLambda_WithActiveStatement_NoAwait_Nested2()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Function(a) Task.FromResult(<AS:0>Function(b)</AS:0> a + b)
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Sub F()
        Dim f = Async Function(a) <AS:0>Function(b)</AS:0> a + b
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            ' No rude edit since the AS is within the nested function.
            edits.VerifySemanticDiagnostics(active,
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition Or EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
        End Sub

        <Fact>
        Public Sub LambdaToIteratorLambda_WithActiveStatement_NoYield()
            Dim src1 = "
Class C
    Function G() As IEnumerable(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = Function() <AS:0>G()</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Function G() As IEnumerable(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = <AS:0>Iterator Function() As IEnumerable(Of Integer)</AS:0>
                  G()
                End Function
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "Iterator Function()"))
        End Sub

        <Fact>
        Public Sub AsyncLambdaToLambda_WithoutActiveStatement_NoAwait()
            Dim src1 = "
Class C
    Shared Function G() As Task(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = Async Function() As Task(Of Integer)
                End Function
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Function G() As Task(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = Function() As Task(Of Integer)
                  Return G()
                End Function
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "Function()", GetResource("Lambda")))
        End Sub

        <Fact>
        Public Sub IteratorLambdaToLambda_WithoutActiveStatement_NoYield()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Shared Function G() As IEnumerable(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = Iterator Function() As IEnumerable(Of Integer)
                End Function
    End Sub
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Shared Function G() As IEnumerable(Of Integer)
        Return Nothing
    End Function
 
    Sub F()
        Dim f = Function() As IEnumerable(Of Integer)
                  Return G()
                End Function
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "Function()", GetResource("Lambda")))
        End Sub

        <Fact>
        Public Sub AsyncMethodEdit_Semantics()
            Dim src1 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Console.WriteLine(1)
        Await Task.FromResult(1)
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Console.WriteLine(2)
        Await Task.FromResult(1)
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                capabilities:=EditAndContinueCapabilities.AddInstanceFieldToExistingType)
        End Sub

        <Fact>
        Public Sub IteratorMethodEdit_Semantics()
            Dim src1 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Console.WriteLine(1)
        Yield 1
    End Function
End Class
"
            Dim src2 = "
Imports System
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Console.WriteLine(2)
        Yield 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                capabilities:=EditAndContinueCapabilities.AddInstanceFieldToExistingType)
        End Sub
#End Region

#Region "On Error"
        <Fact>
        Public Sub MethodUpdate_OnError1()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "label : <AS:0>Console.Write(1)</AS:0> : On Error GoTo label : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "label : <AS:0>Console.Write(2)</AS:0> : On Error GoTo label : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "On Error GoTo label", VBFeaturesResources.On_Error_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError2()
            Dim src1 = "Class C" & vbLf & "<AS:0>Sub M()</AS:0>" & vbLf & "Console.Write(1) : On Error GoTo 0 : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "<AS:0>Sub M()</AS:0>" & vbLf & "Console.Write(2) : On Error GoTo 0 : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "On Error GoTo 0", VBFeaturesResources.On_Error_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError3()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : <AS:0>On Error GoTo -1</AS:0> : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : <AS:0>On Error GoTo -1</AS:0> : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "On Error GoTo -1", VBFeaturesResources.On_Error_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError4()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : On Error Resume Next : <AS:0>End Sub</AS:0> : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : On Error Resume Next : <AS:0>End Sub</AS:0> : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "On Error Resume Next", VBFeaturesResources.On_Error_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume1()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : <AS:0>Resume</AS:0> : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : <AS:0>Resume</AS:0> : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Resume", VBFeaturesResources.Resume_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume2()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : <AS:0>Resume Next</AS:0> : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : <AS:0>Resume Next</AS:0> : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Resume Next", VBFeaturesResources.Resume_statement))
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume3()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "<AS:0>label :</AS:0> Console.Write(1) : Resume label : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "<AS:0>label :</AS:0> Console.Write(2) : Resume label : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "Resume label", VBFeaturesResources.Resume_statement))
        End Sub
#End Region

        <Fact>
        Public Sub PartiallyExecutedActiveStatement()
            Dim src1 As String = "
Class C
    Sub F()
        <AS:0>Console.WriteLine(1)</AS:0> 
        <AS:1>Console.WriteLine(2)</AS:1> 
        <AS:2>Console.WriteLine(3)</AS:2> 
        <AS:3>Console.WriteLine(4)</AS:3> 
        <AS:4>Console.WriteLine(5)</AS:4> 
    End Sub
End Class
"
            Dim src2 As String = "
Class C
    Sub F()
        <AS:0>Console.WriteLine(10)</AS:0> 
        <AS:1>Console.WriteLine(20)</AS:1> 
        <AS:2>Console.WriteLine(30)</AS:2> 
        <AS:3>Console.WriteLine(40)</AS:3> 
        <AS:4>Console.WriteLine(50)</AS:4> 
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2,
            {
                ActiveStatementFlags.PartiallyExecuted Or ActiveStatementFlags.LeafFrame,
                ActiveStatementFlags.PartiallyExecuted Or ActiveStatementFlags.NonLeafFrame,
                ActiveStatementFlags.LeafFrame,
                ActiveStatementFlags.NonLeafFrame,
                ActiveStatementFlags.NonLeafFrame Or ActiveStatementFlags.LeafFrame
            })

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.PartiallyExecutedActiveStatementUpdate, "Console.WriteLine(10)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(20)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(40)"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(50)"))
        End Sub

        <Fact>
        Public Sub PartiallyExecutedActiveStatement_Delete1()
            Dim src1 As String = "
Class C
    Sub F()
        <AS:0>Console.WriteLine(1)</AS:0> 
    End Sub
End Class
"
            Dim src2 As String = "
Class C
    Sub F()
    <AS:0>End Sub</AS:0> 
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2, {ActiveStatementFlags.PartiallyExecuted Or ActiveStatementFlags.LeafFrame})

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.PartiallyExecutedActiveStatementDelete, "Sub F()", FeaturesResources.code))
        End Sub

        <Fact>
        Public Sub PartiallyExecutedActiveStatement_Delete2()
            Dim src1 As String = "
Class C
    Sub F()
        <AS:0>Console.WriteLine(1)</AS:0> 
    End Sub
End Class
"
            Dim src2 As String = "
Class C
    Sub F()
    <AS:0>End Sub</AS:0> 
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2, {ActiveStatementFlags.NonLeafFrame Or ActiveStatementFlags.LeafFrame})

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "Sub F()", FeaturesResources.code))
        End Sub
    End Class
End Namespace
