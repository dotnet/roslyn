' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class IfBlockTests
        <WpfFact>
        Public Sub TestApplyAfterSimpleIfThen()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    If True Then
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    If True Then

    End If
  End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterLineIfNextToThen()
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Sub goo()
        If True Then goo()
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub goo()
        If True Then
            goo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterLineIfWithMultipleStatements()
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Sub goo()
        If True Then goo() : goo()
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub goo()
        If True Then
            goo()
            goo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterLineIfNextToStatement()
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Sub goo()
        If True Then goo()
    End Sub
End Class",
                beforeCaret:={2, 21},
                after:="Class c1
    Sub goo()
        If True Then
            goo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub TestVerifySingleLineIfWithMultiLineLambda()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True Then Dim x = Function(x As Integer)
                                for each i in {1,2,3}
                                    i += 5
                                Next
                                Return x
                             End Function
    End Sub
End Class",
                beforeCaret:={2, 20},
                 after:="Class C
    Sub S
        If True Then
            Dim x = Function(x As Integer)
                                for each i in {1,2,3}
                                    i += 5
                                Next
                                Return x
                             End Function
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub TestVerifySingleLineIfThenElse()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True Then dim x = 1 Else y = 6
    End Sub
End Class",
                beforeCaret:={2, 20},
                 after:="Class C
    Sub S
        If True Then
            dim x = 1
        Else
            y = 6
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub TestVerifyNestedIf()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True Then
            If True Then
        Dim x = 5       
        End If
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        If True Then
            If True Then

            End If
        Dim x = 5       
        End If
    End Sub
End Class",
                afterCaret:={4, -1})

        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536441")>
        Public Sub TestVerifyNestedSingleLineIf()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True Then If True Then X = 1 Else X = 2
    End Sub
End Class",
                beforeCaret:={2, 20},
                 after:="Class C
    Sub S
        If True Then
            If True Then X = 1 Else X = 2
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub VerifyAddingElseIf()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        If true Then
        ElseIf True Then
        End If
    End Sub
End Class",
                caret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyIfWithImplicitLC()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True And
            true Then
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        If True And
            true Then

        End If
    End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyReCommitWithCode()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        If True Then
            Dim x = 5
            Dim y = ""abc""
        End If
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyReCommitWithoutCode()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        If True Then
        End If
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyWithMultiLineChar()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        If True Then : Elseif true then: End If
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539576")>
        Public Sub TestVerifyWithSkippedTokens()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        If True Then #Const goo = 2 ' x = 42
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class C
    Sub S
        If True Then
            #Const goo = 2 ' x = 42
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidMissingEndIf()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        If True Then
            Dim x = 5
    End Sub
End Class",
                caret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyIfInInvalidCode()
            VerifyStatementEndConstructNotApplied(
                text:="If True Then
    if True then
End If",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyInternationalCharacter()
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Sub goo()
        If True Then Dim xæ大% = 1
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub goo()
        If True Then
            Dim xæ大% = 1
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540204")>
        <WpfFact(Skip:="528838")>
        Public Sub TestBugFix6380()
            VerifyStatementEndConstructApplied(
                before:="Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer = 0
        If x = 0 Then #const goo = ""TEST"" : Console.WriteLine(""TEST"") : 'x = 10
    End Sub
End Module",
                beforeCaret:={7, 22},
                after:="Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer = 0
        If x = 0 Then
            #const goo = ""TEST""
            Console.WriteLine(""TEST"")            
        End If : 'x = 10
    End Sub
End Module",
                afterCaret:={8, 12})
        End Sub

        <WpfFact(Skip:="890307"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")>
        Public Sub TestVerifyRewriteOfIfWithColons()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub Goo()
        If True Then : Return : End If
    End Sub
End Class",
                beforeCaret:={2, 21, 2, 22},
                after:="Class C
    Sub Goo()
        If True Then
            Return
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530648")>
        Public Sub TestVerifyRewriteOfIfWithEmptyStatement()
            ' Verify the caret is at the beginning of line 3 here.  In VS, it will be moved to the
            ' correct virtual offset as part of the edit.  This is an edge case that we really just
            ' need to avoid crashing.
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub Goo()
        If True Then Else ' asdf 
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class C
    Sub Goo()
        If True Then

        Else ' asdf 

        End If
    End Sub
End Class",
                afterCaret:={3, 0})
        End Sub
    End Class
End Namespace
