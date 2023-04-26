' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class MultiLineLambdaTests
        <WpfFact>
        Public Sub TestApplyWithFunctionLambda()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestApplyWithFunctionLambdaWithMissingEndFunction()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestApplyWithSubLambda()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Sub TestApplyWithSubLambdaWithNoParameterParenthesis()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Sub TestApplyWithSubLambdaInsideMethodCall()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Sub TestApplyWithSubLambdaAndStatementInsideMethodCall()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544362")>
        Public Sub TestApplyWithFunctionLambdaInsideMethodCall()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestVerifyAnonymousType()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub VerifySingleLineLambdaFunc()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Dim x = Function(x) x
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifySingleLineLambdaSub()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Dim y = Sub(x As Integer) x.ToString()
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyAsDefaultParameterValue()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub s(Optional ByVal f As Func(Of String, String) = Function(x As String)
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Sub s(Optional ByVal f As Func(Of String, String) = Function(x As String)

                                                        End Function
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyNestedLambda()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestVerifyInField()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Dim x = Sub()
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Dim x = Sub()

            End Sub
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidLambdaSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Sub(x)
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNotAppliedIfSubLambdaContainsEndSub()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Dim x = Sub() End Sub
    End Sub
End Class",
                caret:={2, 21})
        End Sub

        <WpfFact>
        Public Sub VerifyNotAppliedIfSyntaxIsFunctionLambdaContainsEndFunction()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Dim x = Function() End Function
    End Sub
End Class",
                caret:={2, 26})
        End Sub

        <WpfFact>
        Public Sub VerifyLambdaWithImplicitLC()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s()
        Dim x = Function(y As Integer) y +
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyLambdaWithMissingParenthesis()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestVerifySingleLineSubLambdaToMultiLine()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Sub TestVerifySingleLineSubLambdaToMultiLineWithTrailingTrivia()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub TestVerifySingleLineFunctionLambdaToMultiLine()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Sub TestVerifySingleLineFunctionLambdaToMultiLineWithTrailingTrivia()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/1922")>
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Sub TestVerifySingleLineFunctionLambdaToMultiLineInsideXMLTag()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/1922")>
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530683")>
        Public Sub TestVerifySingleLineSubLambdaToMultiLineInsideXMLTag()
            VerifyStatementEndConstructApplied(
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
        End Sub
    End Class
End Namespace
