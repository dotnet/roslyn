' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class MultiLineLambdaTests
        <WpfFact>
        Public Async Function TestApplyWithFunctionLambda() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Sub goo()
    Dim x = Function()
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    Dim x = Function()

            End Function
  End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyWithFunctionLambdaWithMissingEndFunction() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    Dim x = Function()
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Function goo()
    Dim x = Function()

            End Function
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyWithSubLambda() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    Dim x = Sub()
  End Function
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Function goo()
    Dim x = Sub()

            End Sub
  End Function
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Async Function TestApplyWithSubLambdaWithNoParameterParenthesis() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    Dim x = Sub
  End Function
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Function goo()
    Dim x = Sub()

            End Sub
  End Function
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Async Function TestApplyWithSubLambdaInsideMethodCall() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    M(Sub())
  End Function
End Class",
                beforeCaret:={2, 11},
                after:="Class c1
  Function goo()
    M(Sub()

      End Sub)
  End Function
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Async Function TestApplyWithSubLambdaAndStatementInsideMethodCall() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    M(Sub() Exit Sub)
  End Function
End Class",
                beforeCaret:={2, 11},
                after:="Class c1
  Function goo()
    M(Sub()
          Exit Sub
      End Sub)
  End Function
End Class",
                afterCaret:={3, 10})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Async Function TestApplyWithFunctionLambdaInsideMethodCall() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo()
    M(Function() 1)
  End Function
End Class",
                beforeCaret:={2, 17},
                after:="Class c1
  Function goo()
    M(Function()
          Return 1
      End Function)
  End Function
End Class",
                afterCaret:={3, 17})
        End Function

        <WpfFact>
        Public Async Function TestVerifyAnonymousType() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = New With {.x = Function(x)
    End Sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    Sub s()
        Dim x = New With {.x = Function(x)

                               End Function
    End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function VerifySingleLineLambdaFunc() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Dim x = Function(x) x
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifySingleLineLambdaSub() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Dim y = Sub(x As Integer) x.ToString()
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyAsDefaultParameterValue() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s(Optional ByVal f As Func(Of String, String) = Function(x As String)
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Sub s(Optional ByVal f As Func(Of String, String) = Function(x As String)

                                                        End Function
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyNestedLambda() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    sub s
        Dim x = Function (x)
                    Dim y = function(y)
                End Function
    End sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    sub s
        Dim x = Function (x)
                    Dim y = function(y)

                            End function
                End Function
    End sub
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyInField() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Dim x = Sub()
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Dim x = Sub()

            End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidLambdaSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Sub(x)
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyNotAppliedIfSubLambdaContainsEndSub() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Dim x = Sub() End Sub
    End Sub
End Class",
                caret:={2, 21})
        End Function

        <WpfFact>
        Public Async Function VerifyNotAppliedIfSyntaxIsFunctionLambdaContainsEndFunction() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Dim x = Function() End Function
    End Sub
End Class",
                caret:={2, 26})
        End Function

        <WpfFact>
        Public Async Function VerifyLambdaWithImplicitLC() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s()
        Dim x = Function(y As Integer) y +
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyLambdaWithMissingParenthesis() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = Function
    End Sub
End Class",
                   beforeCaret:={2, -1},
                   after:="Class C
    Sub s()
        Dim x = Function()

                End Function
    End Sub
End Class",
                   afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifySingleLineSubLambdaToMultiLine() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = Sub() f()
    End Sub
End Class",
                   beforeCaret:={2, 21},
                   after:="Class C
    Sub s()
        Dim x = Sub()
                    f()
                End Sub
    End Sub
End Class",
                   afterCaret:={3, 20})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Async Function TestVerifySingleLineSubLambdaToMultiLineWithTrailingTrivia() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = Sub() f() ' Invokes f()
    End Sub
End Class",
                   beforeCaret:={2, 21},
                   after:="Class C
    Sub s()
        Dim x = Sub()
                    f() ' Invokes f()
                End Sub
    End Sub
End Class",
                   afterCaret:={3, 20})
        End Function

        <WpfFact>
        Public Async Function TestVerifySingleLineFunctionLambdaToMultiLine() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = Function() f()
    End Sub
End Class",
                   beforeCaret:={2, 27},
                   after:="Class C
    Sub s()
        Dim x = Function()
                    Return f()
                End Function
    End Sub
End Class",
                   afterCaret:={3, 27})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Async Function TestVerifySingleLineFunctionLambdaToMultiLineWithTrailingTrivia() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = Function() 4 ' Returns Constant 4
    End Sub
End Class",
                   beforeCaret:={2, 27},
                   after:="Class C
    Sub s()
        Dim x = Function()
                    Return 4 ' Returns Constant 4
                End Function
    End Sub
End Class",
                   afterCaret:={3, 27})
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1922")>
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Async Function TestVerifySingleLineFunctionLambdaToMultiLineInsideXMLTag() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = <xml><%= Function()%></xml>
    End Sub
End Class",
                   beforeCaret:={2, 35},
                   after:="Class C
    Sub s()
        Dim x = <xml><%= Function()

                         End Function %></xml>
    End Sub
End Class",
                   afterCaret:={3, -1})
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1922")>
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Async Function TestVerifySingleLineSubLambdaToMultiLineInsideXMLTag() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub s()
        Dim x = <xml><%= Sub()%></xml>
    End Sub
End Class",
                   beforeCaret:={2, 30},
                   after:="Class C
    Sub s()
        Dim x = <xml><%= Sub()

                         End Sub %></xml>
    End Sub
End Class",
                   afterCaret:={3, -1})
        End Function
    End Class
End Namespace
