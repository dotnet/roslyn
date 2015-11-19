' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class MethodBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterSimpleSubDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1",
                         "  Sub foo() 'Extra Comment",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "  Sub foo() 'Extra Comment",
                        "",
                        "  End Sub",
                        "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterConstructorDeclaration() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1",
                         "  Sub New()",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "  Sub New()",
                        "",
                        "  End Sub",
                        "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterConstructorDeclarationForDesignerGeneratedClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>",
                         "Class c1",
                         "    Sub New()",
                         "",
                         "    Sub InitializeComponent()",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>",
                        "Class c1",
                        "    Sub New()",
                        "",
                       $"        ' {ThisCallIsRequiredByTheDesigner}",
                        "        InitializeComponent()",
                        "",
                       $"        ' {AddAnyInitializationAfter}",
                        "",
                        "    End Sub",
                        "",
                        "    Sub InitializeComponent()",
                        "    End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterConstructorDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1",
                         "  Sub New() 'Extra Comment",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "  Sub New() 'Extra Comment",
                        "",
                        "  End Sub",
                        "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterSimpleFunctionDeclarationWithTrailingComment() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1",
                         "  Function foo() As Integer 'Extra Comment",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "  Function foo() As Integer 'Extra Comment",
                        "",
                        "  End Function",
                        "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DoNotApplyForInterfaceFunction() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Interface IFoo",
                       "Function Foo() as Integer",
                       "End Interface"},
                 caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifySubInAModule() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Module C",
                         "Public Sub s",
                         "End Module"},
                beforeCaret:={1, -1},
                 after:={"Module C",
                         "Public Sub s",
                         "",
                         "End Sub",
                         "End Module"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifySubWithParameters() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Module C",
                         "    Private Sub s1(byval x as Integer, Optional y as Integer = 5)",
                         "End Module"},
                beforeCaret:={1, -1},
                 after:={"Module C",
                         "    Private Sub s1(byval x as Integer, Optional y as Integer = 5)",
                         "",
                         "    End Sub",
                         "End Module"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyFuncWithParameters() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Module C",
                         "    Public function f(byval x as Integer,",
                         "                      byref y as string) as string",
                         "End Module"},
                beforeCaret:={2, -1},
                 after:={"Module C",
                         "    Public function f(byval x as Integer,",
                         "                      byref y as string) as string",
                         "",
                         "    End function",
                         "End Module"},
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyFuncNamedWithKeyWord() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class C",
                         "    private funCtion f1(Optional x as integer = 5) as [if]",
                         "End Class"},
                beforeCaret:={1, -1},
                 after:={"Class C",
                         "    private funCtion f1(Optional x as integer = 5) as [if]",
                         "",
                         "    End funCtion",
                         "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifySharedOperator() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class C",
                         "    Public Shared Operator +(ByVal a As bar, ByVal b As bar) As bar",
                         "End Class"},
                beforeCaret:={1, -1},
                 after:={"Class C",
                         "    Public Shared Operator +(ByVal a As bar, ByVal b As bar) As bar",
                         "",
                         "    End Operator",
                         "End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyRecommit() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Protected friend sub S",
                       "    End sub",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidLocation01() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub S",
                       "        Sub P",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WorkItem(528961)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyInvalidLocation02() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Sub S"},
                beforeCaret:={0, -1},
                after:={"Sub S",
                        "",
                        "End Sub"},
                afterCaret:={1, -1})
        End Function


    End Class
End Namespace
