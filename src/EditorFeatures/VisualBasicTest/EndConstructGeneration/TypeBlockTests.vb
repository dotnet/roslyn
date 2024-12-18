' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class TypeBlockTests
        <WpfFact>
        Public Async Function TestApplyAfterClassStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1",
                beforeCaret:={0, -1},
                after:="Class c1

End Class",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterModuleStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Module m1",
                beforeCaret:={0, -1},
                after:="Module m1

End Module",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForMatchedClass() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
End Class",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterInterfaceStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Interface IGoo",
                beforeCaret:={0, -1},
                after:="Interface IGoo

End Interface",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterStructureStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Structure Goo",
                beforeCaret:={0, -1},
                after:="Structure Goo

End Structure",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterEnumStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Enum Goo",
                beforeCaret:={0, -1},
                after:="Enum Goo

End Enum",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyGenericClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="NameSpace X
    Class C(of T)",
                beforeCaret:={1, -1},
                 after:="NameSpace X
    Class C(of T)

    End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyStructInAClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Structure s
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Structure s

    End Structure
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyClassInAModule() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Module M
    Class C
End Module",
                beforeCaret:={1, -1},
                 after:="Module M
    Class C

    End Class
End Module",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyClassDeclaration() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Partial Friend MustInherit Class C",
                beforeCaret:={0, -1},
                 after:="Partial Friend MustInherit Class C

End Class",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyEnumInAClass() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Public Enum e
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Public Enum e

    End Enum
End Class",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub S
        Class B
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSyntax01() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Enum e(Of T)",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSyntax02() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C Class",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyInheritsDecl() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C : Inherits B",
                beforeCaret:={0, -1},
                 after:="Class C : Inherits B

End Class",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInheritsDeclNotApplied() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C : Inherits B
End Class",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyImplementsDecl() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C : Implements IB",
                beforeCaret:={0, -1},
                 after:="Class C : Implements IB

End Class",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyImplementsDeclNotApplied() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C : Implements IB
End Class",
                caret:={0, -1})
        End Function
    End Class
End Namespace
