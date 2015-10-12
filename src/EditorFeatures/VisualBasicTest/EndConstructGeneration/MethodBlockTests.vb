' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class MethodBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSimpleSubDeclarationWithTrailingComment()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterConstructorDeclaration()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterConstructorDeclarationForDesignerGeneratedClass()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterConstructorDeclarationWithTrailingComment()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSimpleFunctionDeclarationWithTrailingComment()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyForInterfaceFunction()
            VerifyStatementEndConstructNotApplied(
                text:={"Interface IFoo",
                       "Function Foo() as Integer",
                       "End Interface"},
                 caret:={1, -1})
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifySubInAModule()
            VerifyStatementEndConstructApplied(
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
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifySubWithParameters()
            VerifyStatementEndConstructApplied(
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
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyFuncWithParameters()
            VerifyStatementEndConstructApplied(
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
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyFuncNamedWithKeyWord()
            VerifyStatementEndConstructApplied(
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
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifySharedOperator()
            VerifyStatementEndConstructApplied(
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
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyRecommit()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Protected friend sub S",
                       "    End sub",
                       "End Class"},
                caret:={1, -1})
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidLocation01()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Sub S",
                       "        Sub P",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub

        <WorkItem(528961)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidLocation02()
            VerifyStatementEndConstructApplied(
                before:={"Sub S"},
                beforeCaret:={0, -1},
                after:={"Sub S",
                        "",
                        "End Sub"},
                afterCaret:={1, -1})
        End Sub


    End Class
End Namespace
