' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestApplyAfterClassStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1"},
                beforeCaret:={0, -1},
                after:={"Class c1",
                        "",
                        "End Class"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterModuleStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Module m1"},
                beforeCaret:={0, -1},
                after:={"Module m1",
                        "",
                        "End Module"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedClass() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "End Class"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterInterfaceStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Interface IFoo"},
                beforeCaret:={0, -1},
                after:={"Interface IFoo",
                        "",
                        "End Interface"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterStructureStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Structure Foo"},
                beforeCaret:={0, -1},
                after:={"Structure Foo",
                        "",
                        "End Structure"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterEnumStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Enum Foo"},
                beforeCaret:={0, -1},
                after:={"Enum Foo",
                        "",
                        "End Enum"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyGenericClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"NameSpace X",
                         "    Class C(of T)"},
                beforeCaret:={1, -1},
                 after:={"NameSpace X",
                         "    Class C(of T)",
                         "",
                         "    End Class"},
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyStructInAClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyClassInAModule() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyClassDeclaration() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Partial Friend MustInherit Class C"},
                beforeCaret:={0, -1},
                 after:={"Partial Friend MustInherit Class C",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyEnumInAClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

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
        Public Async Function TestVerifyInheritsDecl() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class C : Inherits B"},
                beforeCaret:={0, -1},
                 after:={"Class C : Inherits B",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInheritsDeclNotApplied() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C : Inherits B",
                       "End Class"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyImplementsDecl() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class C : Implements IB"},
                beforeCaret:={0, -1},
                 after:={"Class C : Implements IB",
                         "",
                         "End Class"},
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyImplementsDeclNotApplied() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C : Implements IB",
                       "End Class"},
                caret:={0, -1})
        End Function
    End Class
End Namespace
