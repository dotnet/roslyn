' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class IfBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterSimpleIfThen() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Sub foo()
    If True Then
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub foo()
    If True Then

    End If
  End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterLineIfNextToThen() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
    Sub foo()
        If True Then foo()
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub foo()
        If True Then
            foo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterLineIfWithMultipleStatements() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
    Sub foo()
        If True Then foo() : foo()
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub foo()
        If True Then
            foo()
            foo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterLineIfNextToStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
    Sub foo()
        If True Then foo()
    End Sub
End Class",
                beforeCaret:={2, 21},
                after:="Class c1
    Sub foo()
        If True Then
            foo()
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifySingleLineIfWithMultiLineLambda() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifySingleLineIfThenElse() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyNestedIf() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536441")>
        Public Async Function TestVerifyNestedSingleLineIf() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyAddingElseIf() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        If true Then
        ElseIf True Then
        End If
    End Sub
End Class",
                caret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyIfWithImplicitLC() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyReCommitWithCode() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        If True Then
            Dim x = 5
            Dim y = ""abc""
        End If
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyReCommitWithoutCode() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        If True Then
        End If
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyWithMultiLineChar() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        If True Then : Elseif true then: End If
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, WorkItem(539576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539576"), Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyWithSkippedTokens() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub S
        If True Then #Const foo = 2 ' x = 42
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class C
    Sub S
        If True Then
            #Const foo = 2 ' x = 42
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidMissingEndIf() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        If True Then
            Dim x = 5
    End Sub
End Class",
                caret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyIfInInvalidCode() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="If True Then
    if True then
End If",
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyInternationalCharacter() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
    Sub foo()
        If True Then Dim xæ大% = 1
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class c1
    Sub foo()
        If True Then
            Dim xæ大% = 1
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WorkItem(540204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540204")>
        <WpfFact(Skip:="528838"), Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestBugFix6380() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer = 0
        If x = 0 Then #const foo = ""TEST"" : Console.WriteLine(""TEST"") : 'x = 10
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
            #const foo = ""TEST""
            Console.WriteLine(""TEST"")            
        End If : 'x = 10
    End Sub
End Module",
                afterCaret:={8, 12})
        End Function

        <WpfFact(Skip:="890307"), Trait(Traits.Feature, Traits.Features.EndConstructGeneration), WorkItem(544523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544523")>
        Public Async Function TestVerifyRewriteOfIfWithColons() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub Foo()
        If True Then : Return : End If
    End Sub
End Class",
                beforeCaret:={2, 21, 2, 22},
                after:="Class C
    Sub Foo()
        If True Then
            Return
        End If
    End Sub
End Class",
                afterCaret:={3, 12})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration), WorkItem(530648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530648")>
        Public Async Function TestVerifyRewriteOfIfWithEmptyStatement() As Task
            ' Verify the caret is at the beginning of line 3 here.  In VS, it will be moved to the
            ' correct virtual offset as part of the edit.  This is an edge case that we really just
            ' need to avoid crashing.
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub Foo()
        If True Then Else ' asdf 
    End Sub
End Class",
                beforeCaret:={2, 20},
                after:="Class C
    Sub Foo()
        If True Then

        Else ' asdf 

        End If
    End Sub
End Class",
                afterCaret:={3, 0})
        End Function
    End Class
End Namespace
