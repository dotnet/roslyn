' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class MethodBlockTests
        <WpfFact>
        Public Async Function TestApplyAfterSimpleSubDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Sub goo() 'Extra Comment
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
  Sub goo() 'Extra Comment

  End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterConstructorDeclaration() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Sub New()
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
  Sub New()

  End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterConstructorDeclarationForDesignerGeneratedClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>
Class c1
    Sub New()

    Sub InitializeComponent()
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:=$"<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>
Class c1
    Sub New()

        ' {VBEditorResources.This_call_is_required_by_the_designer}
        InitializeComponent()

        ' {VBEditorResources.Add_any_initialization_after_the_InitializeComponent_call}

    End Sub

    Sub InitializeComponent()
    End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterConstructorDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Sub New() 'Extra Comment
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
  Sub New() 'Extra Comment

  End Sub
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterSimpleFunctionDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
  Function goo() As Integer 'Extra Comment
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
  Function goo() As Integer 'Extra Comment

  End Function
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForInterfaceFunction() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Interface IGoo
Function Goo() as Integer
End Interface",
                 caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifySubInAModule() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Module C
Public Sub s
End Module",
                beforeCaret:={1, -1},
                 after:="Module C
Public Sub s

End Sub
End Module",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifySubWithParameters() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Module C
    Private Sub s1(byval x as Integer, Optional y as Integer = 5)
End Module",
                beforeCaret:={1, -1},
                 after:="Module C
    Private Sub s1(byval x as Integer, Optional y as Integer = 5)

    End Sub
End Module",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyFuncWithParameters() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Module C
    Public function f(byval x as Integer,
                      byref y as string) as string
End Module",
                beforeCaret:={2, -1},
                 after:="Module C
    Public function f(byval x as Integer,
                      byref y as string) as string

    End function
End Module",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyFuncNamedWithKeyWord() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    private funCtion f1(Optional x as integer = 5) as [if]
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    private funCtion f1(Optional x as integer = 5) as [if]

    End funCtion
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifySharedOperator() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Public Shared Operator +(ByVal a As bar, ByVal b As bar) As bar
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Public Shared Operator +(ByVal a As bar, ByVal b As bar) As bar

    End Operator
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyRecommit() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Protected friend sub S
    End sub
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidLocation01() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        Sub P
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528961")>
        Public Async Function TestVerifyInvalidLocation02() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Sub S",
                beforeCaret:={0, -1},
                after:="Sub S

End Sub",
                afterCaret:={1, -1})
        End Function
    End Class
End Namespace
