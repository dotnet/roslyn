' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class TypeBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterClassStatement()
            VerifyStatementEndConstructApplied(
                before:={"Class c1"},
                beforeCaret:={0, -1},
                after:={"Class c1",
                        "",
                        "End Class"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterModuleStatement()
            VerifyStatementEndConstructApplied(
                before:={"Module m1"},
                beforeCaret:={0, -1},
                after:={"Module m1",
                        "",
                        "End Module"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedClass() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "End Class"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterInterfaceStatement()
            VerifyStatementEndConstructApplied(
                before:={"Interface IFoo"},
                beforeCaret:={0, -1},
                after:={"Interface IFoo",
                        "",
                        "End Interface"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterStructureStatement()
            VerifyStatementEndConstructApplied(
                before:={"Structure Foo"},
                beforeCaret:={0, -1},
                after:={"Structure Foo",
                        "",
                        "End Structure"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterEnumStatement()
            VerifyStatementEndConstructApplied(
                before:={"Enum Foo"},
                beforeCaret:={0, -1},
                after:={"Enum Foo",
                        "",
                        "End Enum"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyGenericClass()
            VerifyStatementEndConstructApplied(
                before:={"NameSpace X",
                         "    Class C(of T)"},
                beforeCaret:={1, -1},
                 after:={"NameSpace X",
                         "    Class C(of T)",
                         "",
                         "    End Class"},
                afterCaret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyStructInAClass()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Structure s",
                         "End Class"},
                beforeCaret:={1, -1},
                 after:={"Class C",
                         "    Structure s",
                         "",
                         "    End Structure",
                         "End Class"},
                afterCaret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyClassInAModule()
            VerifyStatementEndConstructApplied(
                before:={"Module M",
                         "    Class C",
                         "End Module"},
                beforeCaret:={1, -1},
                 after:={"Module M",
                         "    Class C",
                         "",
                         "    End Class",
                         "End Module"},
                afterCaret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyClassDeclaration()
            VerifyStatementEndConstructApplied(
                before:={"Partial Friend MustInherit Class C"},
                beforeCaret:={0, -1},
                 after:={"Partial Friend MustInherit Class C",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyEnumInAClass()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Public Enum e",
                         "End Class"},
                beforeCaret:={1, -1},
                 after:={"Class C",
                         "    Public Enum e",
                         "",
                         "    End Enum",
                         "End Class"},
                afterCaret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSyntax() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Sub S",
                       "        Class B",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSyntax01() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Enum e(Of T)"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSyntax02() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C Class"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInheritsDecl()
            VerifyStatementEndConstructApplied(
                before:={"Class C : Inherits B"},
                beforeCaret:={0, -1},
                 after:={"Class C : Inherits B",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInheritsDeclNotApplied() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C : Inherits B",
                       "End Class"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyImplementsDecl()
            VerifyStatementEndConstructApplied(
                before:={"Class C : Implements IB"},
                beforeCaret:={0, -1},
                 after:={"Class C : Implements IB",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyImplementsDeclNotApplied() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C : Implements IB",
                       "End Class"},
                caret:={0, -1})
        End Function
    End Class
End Namespace
