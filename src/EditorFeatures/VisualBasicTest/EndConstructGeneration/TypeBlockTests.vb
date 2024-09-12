' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class TypeBlockTests
        <WpfFact>
        Public Sub TestApplyAfterClassStatement()
            VerifyStatementEndConstructApplied(
                before:="Class c1",
                beforeCaret:={0, -1},
                after:="Class c1

End Class",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterModuleStatement()
            VerifyStatementEndConstructApplied(
                before:="Module m1",
                beforeCaret:={0, -1},
                after:="Module m1

End Module",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForMatchedClass()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
End Class",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterInterfaceStatement()
            VerifyStatementEndConstructApplied(
                before:="Interface IGoo",
                beforeCaret:={0, -1},
                after:="Interface IGoo

End Interface",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterStructureStatement()
            VerifyStatementEndConstructApplied(
                before:="Structure Goo",
                beforeCaret:={0, -1},
                after:="Structure Goo

End Structure",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyAfterEnumStatement()
            VerifyStatementEndConstructApplied(
                before:="Enum Goo",
                beforeCaret:={0, -1},
                after:="Enum Goo

End Enum",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyGenericClass()
            VerifyStatementEndConstructApplied(
                before:="NameSpace X
    Class C(of T)",
                beforeCaret:={1, -1},
                 after:="NameSpace X
    Class C(of T)

    End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyStructInAClass()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Structure s
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Structure s

    End Structure
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyClassInAModule()
            VerifyStatementEndConstructApplied(
                before:="Module M
    Class C
End Module",
                beforeCaret:={1, -1},
                 after:="Module M
    Class C

    End Class
End Module",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyClassDeclaration()
            VerifyStatementEndConstructApplied(
                before:="Partial Friend MustInherit Class C",
                beforeCaret:={0, -1},
                 after:="Partial Friend MustInherit Class C

End Class",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyEnumInAClass()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Public Enum e
End Class",
                beforeCaret:={1, -1},
                 after:="Class C
    Public Enum e

    End Enum
End Class",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        Class B
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidSyntax01()
            VerifyStatementEndConstructNotApplied(
                text:="Enum e(Of T)",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidSyntax02()
            VerifyStatementEndConstructNotApplied(
                text:="Class C Class",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyInheritsDecl()
            VerifyStatementEndConstructApplied(
                before:="Class C : Inherits B",
                beforeCaret:={0, -1},
                 after:="Class C : Inherits B

End Class",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInheritsDeclNotApplied()
            VerifyStatementEndConstructNotApplied(
                text:="Class C : Inherits B
End Class",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyImplementsDecl()
            VerifyStatementEndConstructApplied(
                before:="Class C : Implements IB",
                beforeCaret:={0, -1},
                 after:="Class C : Implements IB

End Class",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyImplementsDeclNotApplied()
            VerifyStatementEndConstructNotApplied(
                text:="Class C : Implements IB
End Class",
                caret:={0, -1})
        End Sub
    End Class
End Namespace
