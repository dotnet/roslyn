' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class ForLoopTests
        <WpfFact>
        Public Sub TestVerifyForWithIndex()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    For i = 1 To 10
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    For i = 1 To 10

    Next
  End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyForEach()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    For Each i In collection
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    For Each i In collection

    Next
  End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527481")>
        Public Sub VerifyIndexMatchedInner1()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
  Sub goo()
    For i = 1 To 10
      For j = 1 To 10
      Next j
  End Sub
End Class",
                 caret:={3, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527481")>
        Public Sub TestVerifyIndexMatchedInner2()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    For i = 1 To 10
      For j = 1 To 10
      Next j
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    For i = 1 To 10

    Next
      For j = 1 To 10
      Next j
  End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527481")>
        Public Sub VerifyIndexSharedNext()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
  Sub goo()
    For i = 1 To 10
      For j = 1 To 10
    Next j, i
  End Sub
End Class",
                 caret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyNestedFor()
            VerifyStatementEndConstructApplied(
                before:="' NestedFor
Class C
    Sub s
        For i = 1 to 10
            For i = 1 to 10
        Next
    End sub
End Class",
                beforeCaret:={4, -1},
                 after:="' NestedFor
Class C
    Sub s
        For i = 1 to 10
            For i = 1 to 10

            Next
        Next
    End sub
End Class",
                afterCaret:={5, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyNestedForEach()
            VerifyStatementEndConstructApplied(
                before:="Class C
    function f(byval x as Integer,
               byref y as string) as string
        for each k in {1,2,3}
            For each i in c
        Next
        return y
    End Function
End Class",
                beforeCaret:={4, -1},
                 after:="Class C
    function f(byval x as Integer,
               byref y as string) as string
        for each k in {1,2,3}
            For each i in c

            Next
        Next
        return y
    End Function
End Class",
                afterCaret:={5, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyReCommitForEach()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Public Property p(byval x as Integer) as Integer
        for each i in {1,2,3}
        Next
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyForAtIncorrectLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    For i = 1 to 10",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidForSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s
        for For
    End Sub
End Class",
                caret:={2, -1})
        End Sub

    End Class
End Namespace
