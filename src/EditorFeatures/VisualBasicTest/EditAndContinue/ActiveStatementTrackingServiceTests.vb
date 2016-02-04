' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.EditAndContinue

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    Public Class ActiveStatementTrackingServiceTests
        Inherits RudeEditTestBase

        <Fact, WorkItem(846042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846042")>
        Public Sub MovedOutsideOfMethod1()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Foo(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main(args As String())
    <AS:0>End Sub</AS:0>

    Private Shared Sub Foo()
        ' tracking span moves to another method as the user types around it
        <TS:0>Foo(1)</TS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifyRudeDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MovedOutsideOfMethod2()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Foo(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Foo(1)</AS:0>
    End Sub

    Private Shared Sub Foo()
        <TS:0>Foo(2)</TS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifyRudeDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MovedOutsideOfLambda1()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        Dim a = Sub() 
                    <AS:0>Foo(1)</AS:0>
                End Sub
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub Main(args As String())
        Dim a = Sub() 
                <AS:0>End Sub</AS:0>

        <TS:0>Foo(1)</TS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifyRudeDiagnostics(active)
        End Sub

        <Fact>
        Public Sub MovedOutsideOfLambda2()
            Dim src1 = "
Class C
    Sub Main()
        Dim a = Sub() 
            <AS:0>Foo(1)</AS:0> 
        End Sub

        Dim b = Sub() 
            Foo(2)
        End Sub
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim a = Sub() 
            <AS:0>Foo(1)</AS:0> 
        End Sub

        Dim b = Sub() 
            <TS:0>Foo(2)</TS:0>
        End Sub
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifyRudeDiagnostics(active)
        End Sub
    End Class
End Namespace
