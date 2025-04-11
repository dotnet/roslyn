' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EditAndContinue

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <UseExportProvider>
    Public Class TrackingSpanTests
        Inherits EditingTestBase

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846042")>
        Public Sub MovedOutsideOfMethod1()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Goo(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main(args As String())
    <AS:0>End Sub</AS:0>

    Private Shared Sub Goo()
        ' tracking span moves to another method as the user types around it
        <TS:0>Goo(1)</TS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.AddMethodToExistingType)
        End Sub

        <Fact>
        Public Sub MovedOutsideOfMethod2()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Goo(1)</AS:0>
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Main(args As String())
        <AS:0>Goo(1)</AS:0>
    End Sub

    Private Shared Sub Goo()
        <TS:0>Goo(2)</TS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(
                active,
                capabilities:=EditAndContinueCapabilities.AddMethodToExistingType)
        End Sub

        <Fact>
        Public Sub MovedOutsideOfLambda1()
            Dim src1 = "
Class C
    Shared Sub Main(args As String())
        Dim a = Sub() 
                    <AS:0>Goo(1)</AS:0>
                End Sub
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub Main(args As String())
        Dim a = Sub() 
                <AS:0>End Sub</AS:0>

        <TS:0>Goo(1)</TS:0>
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active,
                {Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Shared Sub Main(args As String())", GetResource("method"))})
        End Sub

        <Fact>
        Public Sub MovedOutsideOfLambda2()
            Dim src1 = "
Class C
    Sub Main()
        Dim a = Sub() 
            <AS:0>Goo(1)</AS:0> 
        End Sub

        Dim b = Sub() 
            Goo(2)
        End Sub
    End Sub
End Class
"
            Dim src2 = "
Class C
    Sub Main()
        Dim a = Sub() 
            <AS:0>Goo(1)</AS:0> 
        End Sub

        Dim b = Sub() 
            <TS:0>Goo(2)</TS:0>
        End Sub
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            edits.VerifySemanticDiagnostics(active)
        End Sub
    End Class
End Namespace
