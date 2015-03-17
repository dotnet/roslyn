' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    Public Class RudeEditTopLevelTests
        Inherits RudeEditTestBase
#Region "Imports"

        <Fact>
        Public Sub ImportDelete1()
            Dim src1 As String = <text>
Imports System.Diagnostics
</text>.Value
            Dim src2 As String = ""
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Delete [Imports System.Diagnostics]@1")
            Assert.IsType(Of ImportsStatementSyntax)(edits.Edits.First().OldNode)
            Assert.Equal(edits.Edits.First().NewNode, Nothing)
        End Sub

        <Fact>
        Public Sub ImportDelete2()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System.Diagnostics
Imports System.Collections.Generic
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Delete [Imports System.Collections]@28")
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, "import"))
        End Sub

        <Fact>
        Public Sub ImportInsert()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Insert [Imports System.Collections]@28")
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Imports System.Collections", "import"))
        End Sub

        <Fact>
        Public Sub ImportUpdate1()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System.Diagnostics
Imports X = System.Collections
Imports System.Collections.Generic
</text>.Value
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits("Update [Imports System.Collections]@28 -> [Imports X = System.Collections]@28")
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Imports X = System.Collections", "import"))
        End Sub

        <Fact>
        Public Sub ImportUpdate2()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports X1 = System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System.Diagnostics
Imports X2 = System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Imports X1 = System.Collections]@28 -> [Imports X2 = System.Collections]@28")
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Imports X2 = System.Collections", "import"))
        End Sub

        <Fact>
        Public Sub ImportUpdate3()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Imports System.Diagnostics]@1 -> [Imports System]@1")
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Imports System", "import"))
        End Sub

        <Fact>
        Public Sub ImportReorder1()
            Dim src1 As String = <text>
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
</text>.Value

            Dim src2 As String = <text>
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Reorder [Imports System.Diagnostics]@1 -> @63")
        End Sub
#End Region

#Region "Attributes"
        <Fact>
        Public Sub UpdateAttributes1()
            Dim src1 = "<A1>Class C : End Class"
            Dim src2 = "<A2>Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [A1]@1 -> [A2]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A2", "attribute"))
        End Sub

        <Fact>
        Public Sub UpdateAttributes2()
            Dim src1 = "<A(1)>Class C : End Class"
            Dim src2 = "<A(2)>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A(1)]@1 -> [A(2)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", "attribute"))
        End Sub

        <Fact>
        Public Sub UpdateAttributes_TopLevel1()
            Dim src1 = "<Assembly: A(1)>"
            Dim src2 = "<Assembly: A(2)>"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Assembly: A(1)]@1 -> [Assembly: A(2)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Assembly: A(2)", "attribute"))
        End Sub

        <Fact>
        Public Sub UpdateAttributes_TopLevel2()
            Dim src1 = "<Assembly: A(1)>"
            Dim src2 = "<Module: A(1)>"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Assembly: A(1)]@1 -> [Module: A(1)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Module: A(1)", "attribute"))
        End Sub

        <Fact>
        Public Sub DeleteAttributes()
            Dim src1 = "<A, B>Class C : End Class"
            Dim src2 = "<A>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [<A, B>]@0 -> [<A>]@0",
                "Delete [B]@4")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "attribute"))
        End Sub

        <Fact>
        Public Sub DeleteAttributes_TopLevel()
            Dim src1 = "<Assembly: A1>"
            Dim src2 = ""

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Delete [<Assembly: A1>]@0",
                "Delete [<Assembly: A1>]@0",
                "Delete [Assembly: A1]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, "attributes"))
        End Sub

        <Fact>
        Public Sub InsertAttributes1()
            Dim src1 = "<A>Class C : End Class"
            Dim src2 = "<A, B>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [<A>]@0 -> [<A, B>]@0",
                "Insert [B]@4")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", "attribute"))
        End Sub

        <Fact>
        Public Sub InsertAttributes2()
            Dim src1 = "Class C : End Class"
            Dim src2 = "<A>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@0",
                "Insert [A]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub InsertAttributes_TopLevel()
            Dim src1 = ""
            Dim src2 = "<Assembly: A1>"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [<Assembly: A1>]@0",
                "Insert [<Assembly: A1>]@0",
                "Insert [Assembly: A1]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<Assembly: A1>", "attributes"))
        End Sub

        <Fact>
        Public Sub ReorderAttributes1()
            Dim src1 = "<A(1), B(2), C(3)>Class C : End Class"
            Dim src2 = "<C(3), A(1), B(2)>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits("Reorder [C(3)]@13 -> @1")
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ReorderAttributes2()
            Dim src1 = "<A, B, C>Class C : End Class"
            Dim src2 = "<B, C, A>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits("Reorder [A]@1 -> @7")
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ReorderAttributes_TopLevel()
            Dim src1 = "<Assembly: A1><Assembly: A2>"
            Dim src2 = "<Assembly: A2><Assembly: A1>"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Reorder [<Assembly: A2>]@14 -> @0")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ReorderAndUpdateAttributes()
            Dim src1 = "<A(1), B, C>Class C : End Class"
            Dim src2 = "<B, C, A(2)>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [A(1)]@1 -> @7",
                "Update [A(1)]@1 -> [A(2)]@7")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", "attribute"))
        End Sub

#End Region

#Region "Top-level Classes, Structs, Interfaces, Modules"
        <Fact>
        Public Sub ClassInsert()
            Dim src1 = ""
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub StructInsert()
            Dim src1 = ""
            Dim src2 = "Structure C : End Structure"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PartialInterfaceInsert()
            Dim src1 = ""
            Dim src2 = "Partial Interface C : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PartialInterfaceDelete()
            Dim src1 = "Partial Interface C : End Interface"
            Dim src2 = ""
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, "interface"))
        End Sub

        <Fact>
        Public Sub ModuleInsert()
            Dim src1 = ""
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Module C", "module"))
        End Sub

        <Fact>
        Public Sub PartialModuleInsert()
            Dim src1 = ""
            Dim src2 = "Partial Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Partial Module C", "module"))
        End Sub

        <Fact>
        Public Sub PartialModuleDelete()
            Dim src1 = "Partial Module C : End Module"
            Dim src2 = ""
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, "module"))
        End Sub

        <Fact>
        Public Sub TypeKindChange1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Structure C : End Structure"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : End Class]@0 -> [Structure C : End Structure]@0",
                "Update [Class C]@0 -> [Structure C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeKindUpdate, "Structure C", "structure"))
        End Sub

        <Fact>
        Public Sub TypeKindChange2()
            Dim src1 = "Public Module C : End Module"
            Dim src2 = "Public Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Module C : End Module]@0 -> [Public Class C : End Class]@0",
                "Update [Public Module C]@0 -> [Public Class C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeKindUpdate, "Public Class C", "class"))
        End Sub

        <Fact>
        Public Sub ModuleModifiersUpdate()
            Dim src1 = "Module C : End Module"
            Dim src2 = "Partial Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Module C]@0 -> [Partial Module C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Partial Module C", "module"))
        End Sub

        <Fact>
        Public Sub ModuleModifiersUpdate2()
            Dim src1 = "Partial Module C : End Module"
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Partial Module C]@0 -> [Module C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Module C", "module"))
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate()
            Dim src1 = "Public Interface C : End Interface"
            Dim src2 = "Interface C : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Interface C]@0 -> [Interface C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Interface C", "interface"))
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate2()
            Dim src1 = "Public Interface C : Sub Foo() : End Interface"
            Dim src2 = "Interface C : Sub Foo() : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Interface C]@0 -> [Interface C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Interface C", "interface"))
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate3()
            Dim src1 = "Interface C : Sub Foo() : End Interface"
            Dim src2 = "Partial Interface C : Sub Foo() : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Interface C]@0 -> [Partial Interface C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Partial Interface C", "interface"))
        End Sub

        <Fact>
        Public Sub StructModifiersUpdate()
            Dim src1 = "Structure C : End Structure"
            Dim src2 = "Public Structure C : End Structure"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Structure C]@0 -> [Public Structure C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Public Structure C", "structure"))
        End Sub

        <Fact>
        Public Sub ClassRename1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "class c : end class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits("Update [Class C]@0 -> [class c]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class c", "class"))
        End Sub

        <Fact>
        Public Sub ClassRename2()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class D : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C]@0 -> [Class D]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Class D", "class"))
        End Sub

        <Fact>
        Public Sub InterfaceNameReplace()
            Dim src1 = "Interface C : End Interface"
            Dim src2 = "Interface D : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Interface C]@0 -> [Interface D]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Interface D", "interface"))
        End Sub

        <Fact>
        Public Sub StructNameReplace()
            Dim src1 = "Structure C : End Structure"
            Dim src2 = "Structure D : End Structure"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Structure C]@0 -> [Structure D]@0")
            edits.VerifyRudeDiagnostics(Diagnostic(RudeEditKind.Renamed, "Structure D", "structure"))
        End Sub

        <Fact>
        Public Sub ClassNameUpdate()
            Dim src1 = "Class LongerName : End Class"
            Dim src2 = "Class LongerMame : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class LongerName]@0 -> [Class LongerMame]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Class LongerMame", "class"))
        End Sub

        <Fact>
        Public Sub BaseTypeUpdate1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Inherits D : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : End Class]@0 -> [Class C : Inherits D : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", "class"))
        End Sub

        <Fact>
        Public Sub BaseTypeUpdate2()
            Dim src1 = "Class C : Inherits D1 : End Class"
            Dim src2 = "Class C : Inherits D2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Inherits D1 : End Class]@0 -> [Class C : Inherits D2 : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", "class"))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Implements IDisposable : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : End Class]@0 -> [Class C : Implements IDisposable : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", "class"))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate2()
            Dim src1 = "Class C : Implements IFoo, IBar : End Class"
            Dim src2 = "Class C : Implements IFoo : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Implements IFoo, IBar : End Class]@0 -> [Class C : Implements IFoo : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", "class"))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate3()
            Dim src1 = "Class C : Implements IFoo : Implements IBar : End Class"
            Dim src2 = "Class C : Implements IBar : Implements IFoo : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Implements IFoo : Implements IBar : End Class]@0 -> [Class C : Implements IBar : Implements IFoo : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", "class"))
        End Sub

        <Fact>
        Public Sub ClassInsert_AbstractVirtualOverride()
            Dim src1 = ""
            Dim src2 = "
Public MustInherit Class C(Of T)
    Public MustOverride Sub F()
    Public Overridable Sub G() : End Sub
    Public Overrides Sub H() : End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub InterfaceInsert()
            Dim src1 = ""
            Dim src2 = "
Public Interface I 
    Sub F()
End Interface
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics()
        End Sub
#End Region

#Region "Enums"
        <Fact>
        Public Sub Enum_NoModifiers_Insert()
            Dim src1 = ""
            Dim src2 = "Enum C : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Enum_NoModifiers_IntoNamespace_Insert()
            Dim src1 = "Namespace N : End Namespace"
            Dim src2 = "Namespace N : Enum C : End Enum : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Enum_Name_Update()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Colors : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Enum Color]@0 -> [Enum Colors]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Enum Colors", "enum"))
        End Sub

        <Fact>
        Public Sub Enum_Modifiers_Update()
            Dim src1 = "Public Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Enum Color]@0 -> [Enum Color]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Enum Color", "enum"))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Insert()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As UShort]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As UShort", "as clause"))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Update()
            Dim src1 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color As Long : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As UShort]@11 -> [As Long]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Enum Color", "enum"))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Delete()
            Dim src1 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                 "Delete [As UShort]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Enum Color", "as clause"))
        End Sub

        <Fact>
        Public Sub Enum_Attribute_Insert()
            Dim src1 = "Enum E : End Enum"
            Dim src2 = "<A>Enum E : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@0", "Insert [A]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Delete()
            Dim src1 = "Enum E : <A>X : End Enum"
            Dim src2 = "Enum E : X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<A>]@9", "Delete [A]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "X", "attributes"))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Insert()
            Dim src1 = "Enum E : X : End Enum"
            Dim src2 = "Enum E : <A>X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@9", "Insert [A]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Update()
            Dim src1 = "Enum E : <A1>X : End Enum"
            Dim src2 = "Enum E : <A2>X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A1]@10 -> [A2]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A2", "attribute"))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Update1()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 3 : End Enum"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Blue = 2]@23 -> [Blue = 3]@23")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 3", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Update2()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 << 0 : Blue = 2 << 1 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red = 1]@13 -> [Red = 1 << 0]@13",
                "Update [Blue = 2]@23 -> [Blue = 2 << 1]@28")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1 << 0", "enum value"),
                Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 2 << 1", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Update3()
            Dim src1 = "Enum Color : Red = int.MinValue : End Enum"
            Dim src2 = "Enum Color : Red = int.MaxValue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red = int.MinValue]@13 -> [Red = int.MaxValue]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = int.MaxValue", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Insert()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red = 1 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red]@13 -> [Red = 1]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Delete()
            Dim src1 = "Enum Color : Red = 1 : End Enum"
            Dim src2 = "Enum Color : Red : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red = 1]@13 -> [Red]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_Member_Insert1()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red : Blue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Blue", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_Member_Insert2()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red : Blue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Blue", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_Member_Update()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Orange : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red]@13 -> [Orange]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Orange", "enum value"))
        End Sub

        <Fact>
        Public Sub Enum_Member_Delete()
            Dim src1 = "Enum Color : Red : Blue : End Enum"
            Dim src2 = "Enum Color : Red : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Enum Color", "enum value"))
        End Sub
#End Region

#Region "Delegates"
        <Fact>
        Public Sub Delegates_NoModifiers_Insert()
            Dim src1 = ""
            Dim src2 = "Delegate Sub C()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Delegates_NoModifiers_IntoNamespace_Insert()
            Dim src1 = "Namespace N : End Namespace"
            Dim src2 = "Namespace N : Delegate Sub C() : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Delegates_NoModifiers_IntoType_Insert()
            Dim src1 = "Class N : End Class"
            Dim src2 = "Class N : Delegate Sub C() : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Delegates_Rename()
            Dim src1 = "Public Delegate Sub D()"
            Dim src2 = "Public Delegate Sub Z()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Sub D()]@0 -> [Public Delegate Sub Z()]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Public Delegate Sub Z()", "delegate"))
        End Sub

        <Fact>
        Public Sub Delegates_Update_Modifiers()
            Dim src1 = "Public Delegate Sub D()"
            Dim src2 = "Private Delegate Sub D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Sub D()]@0 -> [Private Delegate Sub D()]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Private Delegate Sub D()", "delegate"))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType1()
            Dim src1 = "Public Delegate Function D()"
            Dim src2 = "Public Delegate Sub D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Function D()]@0 -> [Public Delegate Sub D()]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Public Delegate Sub D()", "delegate"))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType2()
            Dim src1 = "Public Delegate Function D() As Integer"
            Dim src2 = "Public Delegate Sub D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Function D() As Integer]@0 -> [Public Delegate Sub D()]@0",
                "Delete [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Public Delegate Sub D()", "delegate"),
                Diagnostic(RudeEditKind.Delete, "Public Delegate Sub D()", "as clause"))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType3()
            Dim src1 = "Public Delegate Function D()"
            Dim src2 = "Public Delegate Function D() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Integer", "as clause"))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType4()
            Dim src1 = "Public Delegate Function D() As Integer"
            Dim src2 = "Public Delegate Function D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", "as clause"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Insert()
            Dim src1 = "Public Delegate Function D() As Integer"
            Dim src2 = "Public Delegate Function D(a As Integer) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [a As Integer]@27",
                "Insert [a]@27",
                "Insert [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "a As Integer", "parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Delete()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [a As Integer]@27",
                "Delete [a]@27",
                "Delete [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", "parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Rename()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D(b As Integer) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@27 -> [b]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Update()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D(a As Byte) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@29 -> [As Byte]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Byte", "parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_UpdateModifier()
            Dim src1 = "Public Delegate Function D(a As Integer()) As Integer"
            Dim src2 = "Public Delegate Function D(ParamArray a As Integer()) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer()]@27 -> [ParamArray a As Integer()]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "ParamArray a As Integer()", "parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_AddAttribute()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D(<A> a As Integer) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@27",
                "Insert [A]@28")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Insert()
            Dim src1 = "Public Delegate Function D() As Integer"
            Dim src2 = "Public Delegate Function D(Of T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [(Of T)]@26",
                "Insert [T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "(Of T)", "type parameters"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Delete()
            Dim src1 = "Public Delegate Function D(Of T)() As Integer"
            Dim src2 = "Public Delegate Function D() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [(Of T)]@26",
                "Delete [T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", "type parameters"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Rename()
            Dim src1 = "Public Delegate Function D(Of T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of S)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [T]@30 -> [S]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "S", "type parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance1()
            Dim src1 = "Public Delegate Function D(Of T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of In T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [T]@30 -> [In T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", "type parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance2()
            Dim src1 = "Public Delegate Function D(Of Out T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Out T]@30 -> [T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", "type parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance3()
            Dim src1 = "Public Delegate Function D(Of Out T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of In T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Out T]@30 -> [In T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", "type parameter"))
        End Sub

        <Fact>
        Public Sub Delegates_AddAttribute()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "<A>Public Delegate Function D(a As Integer) As Integer"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [<A>]@0",
                "Insert [A]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

#End Region

#Region "Nested Types"

        <Fact>
        Public Sub NestedClass_ClassMove1()
            Dim src1 = "Class C : Class D : End Class : End Class"
            Dim src2 = "Class C : End Class : Class D : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Class D : End Class]@10 -> @22")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Class D", "class"))
        End Sub

        <Fact>
        Public Sub NestedClass_ClassMove2()
            Dim src1 = "Class C : Class D : End Class : Class E : End Class : Class F : End Class : End Class"
            Dim src2 = "Class C : Class D : End Class : Class F : End Class : End CLass : Class E : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Class E : End Class]@32 -> @66")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Class E", "class"))
        End Sub

        <Fact>
        Public Sub NestedClass_ClassInsertMove1()
            Dim src1 = "Class C : Class D : End Class : End Class"
            Dim src2 = "Class C : Class E : Class D : End Class : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Class E : Class D : End Class : End Class]@10",
                "Insert [Class E]@10",
                "Move [Class D : End Class]@10 -> @20")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Class D", "class"))
        End Sub

        <Fact>
        Public Sub NestedClass_Insert1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Class D : Class E : End Class : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Class D : Class E : End Class : End Class]@10",
                "Insert [Class D]@10",
                "Insert [Class E : End Class]@20",
                "Insert [Class E]@20")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_Insert2()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Protected Class D : Public Class E : End Class : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Protected Class D : Public Class E : End Class : End Class]@10",
                "Insert [Protected Class D]@10",
                "Insert [Public Class E : End Class]@30",
                "Insert [Public Class E]@30")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_Insert3()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private Class D : Public Class E : End Class : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Class D : Public Class E : End Class : End Class]@10",
                "Insert [Private Class D]@10",
                "Insert [Public Class E : End Class]@28",
                "Insert [Public Class E]@28")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_Insert4()
            Dim src1 = "Class C : End Class"
            Dim src2 = <text>
Class C
  Private Class D
    Public Sub New(a As Integer, b As Integer) : End Sub
    Public Property P As Integer
  End Class
End Class
</text>.Value

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(<text>Insert [Private Class D
    Public Sub New(a As Integer, b As Integer) : End Sub
    Public Property P As Integer
  End Class]@11</text>.Value,
                "Insert [Private Class D]@11",
                "Insert [Public Sub New(a As Integer, b As Integer) : End Sub]@31",
                "Insert [Public Property P As Integer]@88",
                "Insert [Public Sub New(a As Integer, b As Integer)]@31",
                "Insert [As Integer]@106",
                "Insert [(a As Integer, b As Integer)]@45",
                "Insert [a As Integer]@46",
                "Insert [b As Integer]@60",
                "Insert [a]@46",
                "Insert [As Integer]@48",
                "Insert [b]@60",
                "Insert [As Integer]@62")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_InsertMemberWithInitializer1()
            Dim src1 = "Public Class C : End Class"
            Dim src2 = "Public Class C : Private Class D : Public Property P As New List(Of String) : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.D"), preserveLocalVariables:=False)})
        End Sub

        <Fact>
        Public Sub NestedClass_InsertMemberWithInitializer2()
            Dim src1 = "Public Module C : End Module"
            Dim src2 = "Public Module C : Private Class D : Property P As New List(Of String) : End Class : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.D"), preserveLocalVariables:=False)})
        End Sub

        <WorkItem(835827)>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Syntactic()
            Dim src1 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
]]>.Value

            Dim src2 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private MustInherit Class D 
        Declare Ansi Function A Lib "A" () As Integer
        Declare Ansi Sub B Lib "B" ()
    End Class
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Declare Ansi Function A Lib ""A"" ()", "method"),
                Diagnostic(RudeEditKind.Insert, "Declare Ansi Sub B Lib ""B"" ()", "method"))
        End Sub

        <WorkItem(835827)>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Semantic1()
            Dim src1 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
]]>.Value

            Dim src2 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private MustInherit Class D 
        <DllImport("msvcrt.dll")>
        Public Shared Function puts(c As String) As Integer
        End Function

        <DllImport("msvcrt.dll")>
        Public Shared Operator +(d As D, g As D) As Integer
        End Operator

        <DllImport("msvcrt.dll")>
        Public Shared Narrowing Operator CType(d As D) As Integer
        End Operator
    End Class
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "puts"),
                Diagnostic(RudeEditKind.InsertDllImport, "+"),
                Diagnostic(RudeEditKind.InsertDllImport, "CType"))
        End Sub

        <WorkItem(835827)>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Semantic2()
            Dim src1 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
]]>.Value

            Dim src2 As String = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    <DllImport("msvcrt.dll")>
    Private Shared Function puts(c As String) As Integer
    End Function
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "puts"))
        End Sub

        <WorkItem(835827)>
        <Fact>
        Public Sub NestedClass_Insert_VirtualAbstract()
            Dim src1 As String = <text>
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
</text>.Value

            Dim src2 As String = <text>
Imports System
Imports System.Runtime.InteropServices

Class C
    Private MustInherit Class D
        Public MustOverride ReadOnly Property P As Integer
        Public MustOverride Property Indexer(i As Integer) As Integer
        Public MustOverride Function F(c As String) As Integer

        Public Overridable ReadOnly Property Q As Integer
            Get
                Return 1
            End Get
        End Property

        Public Overridable Function M(c As String) As Integer
            Return 1
        End Function
    End Class
End Class
</text>.Value
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_TypeReorder1()
            Dim src1 = "Class C : Structure E : End Structure : Class F : End Class : Delegate Sub D() : Interface I : End Interface : End Class"
            Dim src2 = "Class C : Class F : End Class : Interface I : End Interface : Delegate Sub D() : Structure E : End Structure : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Reorder [Structure E : End Structure]@10 -> @81",
                "Reorder [Interface I : End Interface]@81 -> @32")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedClass_MethodDeleteInsert()
            Dim src1 = "Public Class C" & vbLf & "Public Sub foo() : End Sub : End Class"
            Dim src2 = "Public Class C : Private Class D" & vbLf & "Public Sub foo() : End Sub : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Class D" & vbLf & "Public Sub foo() : End Sub : End Class]@17",
                "Insert [Private Class D]@17",
                "Insert [Public Sub foo() : End Sub]@33",
                "Insert [Public Sub foo()]@33",
                "Insert [()]@47",
                "Delete [Public Sub foo() : End Sub]@15",
                "Delete [Public Sub foo()]@15",
                "Delete [()]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Class C", "method"))
        End Sub

        <Fact>
        Public Sub NestedClass_ClassDeleteInsert()
            Dim src1 = "Public Class C : Public Class X : End Class : End Class"
            Dim src2 = "Public Class C : Public Class D : Public Class X : End Class : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Public Class D : Public Class X : End Class : End Class]@17",
                "Insert [Public Class D]@17",
                "Move [Public Class X : End Class]@17 -> @34")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Public Class X", "class"))
        End Sub


#End Region

#Region "Namespaces"
        <Fact>
        Public Sub NamespaceInsert()
            Dim src1 = ""
            Dim src2 = "Namespace C : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Namespace C", "namespace"))
        End Sub

        <Fact>
        Public Sub NamespaceMove1()
            Dim src1 = "Namespace C : Namespace D : End Namespace : End Namespace"
            Dim src2 = "Namespace C : End Namespace : Namespace D : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Namespace D : End Namespace]@14 -> @30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Namespace D", "namespace"))
        End Sub

        <Fact>
        Public Sub NamespaceReorder1()
            Dim src1 = "Namespace C : Namespace D : End Namespace : Class T : End Class : Namespace E : End Namespace : End Namespace"
            Dim src2 = "Namespace C : Namespace E : End Namespace : Class T : End Class : Namespace D : End Namespace : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Class T : End Class]@44 -> @44",
                "Reorder [Namespace E : End Namespace]@66 -> @14")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NamespaceReorder2()
            Dim src1 = "Namespace C : " &
                          "Namespace D1 : End Namespace : " &
                          "Namespace D2 : End Namespace : " &
                          "Namespace D3 : End Namespace : " &
                          "Class T : End Class : " &
                          "Namespace E : End Namespace : " &
                        "End Namespace"

            Dim src2 = "Namespace C : " &
                          "Namespace E : End Namespace : " &
                          "Class T : End Class : " &
                          "Namespace D1 : End Namespace : " &
                          "Namespace D2 : End Namespace : " &
                          "Namespace D3 : End Namespace : " &
                       "End Namespace"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Class T : End Class]@107 -> @44",
                "Reorder [Namespace E : End Namespace]@129 -> @14")

            edits.VerifyRudeDiagnostics()
        End Sub

#End Region

#Region "Methods"

        <Fact>
        Public Sub MethodUpdate1()
            Dim src1 As String =
                "Class C" & vbLf &
                  "Shared Sub Main()" & vbLf &
                     "Dim a As Integer = 1 : " &
                     "Dim b As Integer = 2 : " &
                     "Console.ReadLine(a + b) : " &
                  "End Sub : " &
                "End Class"

            Dim src2 As String =
                "Class C" & vbLf &
                  "Shared Sub Main()" & vbLf &
                     "Dim a As Integer = 2 : " &
                     "Dim b As Integer = 1 : " &
                     "Console.ReadLine(a + b) : " &
                  "End Sub : " &
                "End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Shared Sub Main()" & vbLf & "Dim a As Integer = 1 : Dim b As Integer = 2 : Console.ReadLine(a + b) : End Sub]@8 -> " &
                       "[Shared Sub Main()" & vbLf & "Dim a As Integer = 2 : Dim b As Integer = 1 : Console.ReadLine(a + b) : End Sub]@8")

            edits.VerifyRudeDiagnostics()

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"), preserveLocalVariables:=False)})
        End Sub

        <Fact>
        Public Sub MethodUpdate2()
            Dim src1 As String = "Class C" & vbLf & "Sub Foo() : End Sub : End Class"
            Dim src2 As String = "Class C" & vbLf & "Function Foo() : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo() : End Sub]@8 -> [Function Foo() : End Function]@8",
                "Update [Sub Foo()]@8 -> [Function Foo()]@8")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodKindUpdate, "Function Foo()", "method"))
        End Sub

        <Fact>
        Public Sub InterfaceMethodUpdate1()
            Dim src1 As String = "Interface I" & vbLf & "Sub Foo() : End Interface"
            Dim src2 As String = "Interface I" & vbLf & "Function Foo() : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo()]@12 -> [Function Foo()]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodKindUpdate, "Function Foo()", "method"))
        End Sub

        <Fact>
        Public Sub InterfaceMethodUpdate2()
            Dim src1 As String = "Interface I" & vbLf & "Sub Foo() : End Interface"
            Dim src2 As String = "Interface I" & vbLf & "Sub Foo(a As Boolean) : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "a As Boolean", "parameter"))
        End Sub

        <Fact>
        Public Sub MethodDelete()
            Dim src1 As String = "Class C" & vbLf & "Sub foo() : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Sub foo() : End Sub]@8",
                "Delete [Sub foo()]@8",
                "Delete [()]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "method"))
        End Sub

        <Fact>
        Public Sub InterfaceMethodDelete()
            Dim src1 As String = "Interface C" & vbLf & "Sub Foo() : End Interface"
            Dim src2 As String = "Interface C : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Interface C", "method"))
        End Sub

        <Fact>
        Public Sub MethodDelete_WithParameters()
            Dim src1 As String = "Class C" & vbLf & "Sub foo(a As Integer) : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Sub foo(a As Integer) : End Sub]@8",
                "Delete [Sub foo(a As Integer)]@8",
                "Delete [(a As Integer)]@15",
                "Delete [a As Integer]@16",
                "Delete [a]@16",
                "Delete [As Integer]@18")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "method"))
        End Sub

        <Fact>
        Public Sub MethodDelete_WithAttribute()
            Dim src1 As String = "Class C : " & vbLf & "<Obsolete> Sub foo(a As Integer) : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<Obsolete> Sub foo(a As Integer) : End Sub]@11",
                "Delete [<Obsolete> Sub foo(a As Integer)]@11",
                "Delete [<Obsolete>]@11",
                "Delete [Obsolete]@12",
                "Delete [(a As Integer)]@29",
                "Delete [a As Integer]@30",
                "Delete [a]@30",
                "Delete [As Integer]@32")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "method"))
        End Sub

        <Fact>
        Public Sub MethodInsert_Private()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Private Function F : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Private Function F : End Function]@11",
                "Insert [Private Function F]@11")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodInsert_PrivateWithParameters()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Private Function F(a As Integer) : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Function F(a As Integer) : End Function]@11",
                "Insert [Private Function F(a As Integer)]@11",
                "Insert [(a As Integer)]@29",
                "Insert [a As Integer]@30",
                "Insert [a]@30",
                "Insert [As Integer]@32")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodInsert_PrivateWithOptionalParameters()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Private Function F(Optional a As Integer = 1) : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Function F(Optional a As Integer = 1) : End Function]@11",
                "Insert [Private Function F(Optional a As Integer = 1)]@11",
                "Insert [(Optional a As Integer = 1)]@29",
                "Insert [Optional a As Integer = 1]@30",
                "Insert [a]@39",
                "Insert [As Integer]@41")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F"))})
        End Sub

        <Fact>
        Public Sub MethodInsert_PrivateWithAttribute()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "<A>Private Sub F : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>Private Sub F : End Sub]@11",
                "Insert [<A>Private Sub F]@11",
                "Insert [<A>]@11",
                "Insert [A]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F"))})
        End Sub

        <Fact>
        Public Sub MethodInsert_Overridable()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Overridable Sub F : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "Overridable Sub F", "method"))
        End Sub

        <Fact>
        Public Sub MethodInsert_MustOverride()
            Dim src1 = "MustInherit Class C : End Class"
            Dim src2 = "MustInherit Class C : " & vbLf & "MustOverride Sub F : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "MustOverride Sub F", "method"))
        End Sub

        <Fact>
        Public Sub MethodInsert_Overrides()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Overrides Sub F : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "Overrides Sub F", "method"))
        End Sub

        <Fact>
        Public Sub Method_Reorder1()
            Dim src1 = "Class C : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : " & vbLf & "Sub g() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Sub g() : End Sub : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Sub g() : End Sub]@64 -> @11")

            edits.VerifySemantics(ActiveStatementsDescription.Empty, {})
        End Sub

        <Fact>
        Public Sub InterfaceMethod_Reorder1()
            Dim src1 = "Interface I : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "Sub g() : End Interface"
            Dim src2 = "Interface I : " & vbLf & "Sub g() : " & vbLf & "Sub f(a As Integer, b As Integer) : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Sub g()]@49 -> @15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Sub g()", "method"))
        End Sub

        <Fact>
        Public Sub Method_InsertDelete1()
            Dim src1 = "Class C : Class D : End Class : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : End Class"
            Dim src2 = "Class C : Class D : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub]@21",
                "Insert [Sub f(a As Integer, b As Integer)]@21",
                "Insert [(a As Integer, b As Integer)]@26",
                "Insert [a As Integer]@27",
                "Insert [b As Integer]@41",
                "Insert [a]@27",
                "Insert [As Integer]@29",
                "Insert [b]@41",
                "Insert [As Integer]@43",
                "Delete [Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub]@33",
                "Delete [Sub f(a As Integer, b As Integer)]@33",
                "Delete [(a As Integer, b As Integer)]@38",
                "Delete [a As Integer]@39",
                "Delete [a]@39",
                "Delete [As Integer]@41",
                "Delete [b As Integer]@53",
                "Delete [b]@53",
                "Delete [As Integer]@55")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "method"))
        End Sub

        <Fact>
        Public Sub Method_Rename()
            Dim src1 = "Class C : " & vbLf & "Sub Foo : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Sub Bar : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo]@11 -> [Sub Bar]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Sub Bar", "method"))
        End Sub

        <Fact>
        Public Sub InterfaceMethod_Rename()
            Dim src1 = "Interface C : " & vbLf & "Sub Foo : End Interface"
            Dim src2 = "Interface C : " & vbLf & "Sub Bar : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo]@15 -> [Sub Bar]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Sub Bar", "method"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncModifier()
            Dim src1 = "Class C : " & vbLf & "Async Function F() As Task(Of String) : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Function F() As Task(Of String) : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Async Function F() As Task(Of String)]@11 -> [Function F() As Task(Of String)]@11")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncMethod1()
            Dim src1 = "Class C : " & vbLf & "Async Function F() As Task(Of String)" & vbLf & "Return 0" & vbLf & "End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Async Function F() As Task(Of String)" & vbLf & "Return 1" & vbLf & "End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Async Function F() As Task(Of String)" & vbLf & "Return 0" & vbLf & "End Function]@11 -> " &
                       "[Async Function F() As Task(Of String)" & vbLf & "Return 1" & vbLf & "End Function]@11")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncMethod2()
            Dim src1 = <text>
Class C
    Async Function F(x As Integer) As Task(Of Integer)
        Await F(2)
        If Await F(2) Then : End If
        While Await F(2) : End While
        Do : Loop Until Await F(2)
        Do : Loop While Await F(2)
        Do Until Await F(2) : Loop
        Do While Await F(2) : Loop
        For i As Integer = 1 To Await F(2) : Next
        Using a = Await F(2) : End Using
        Dim a = Await F(2)
        Dim b = Await F(2), c = Await F(2)
        b = Await F(2)
        Select Case Await F(2) : Case 1 : Return Await F(2) : End Select
        Return Await F(2)
    End Function
End Class
</text>.Value
            Dim src2 = <text>
Class C
    Async Function F(x As Integer) As Task(Of Integer)
        Await F(1)
        If Await F(1) Then : End If
        While Await F(1) : End While
        Do : Loop Until Await F(1)
        Do : Loop While Await F(1)
        Do Until Await F(1) : Loop
        Do While Await F(1) : Loop
        For i As Integer = 1 To Await F(1) : Next
        Using a = Await F(1) : End Using
        Dim a = Await F(1)
        Dim b = Await F(1), c = Await F(1)
        b = Await F(1)
        Select Case Await F(1) : Case 1 : Return Await F(1) : End Select
        Return Await F(1)
    End Function
End Class
</text>.Value

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncMethod3()
            Dim src1 = <text>
Class C
    Async Function F(x As Integer) As Task(Of Integer)
        If Await F(Await F(2)) Then : End If
        For Each i In {Await F(2)} : Next
        Dim a = 1
        a += Await F(2)
    End Function
End Class
</text>.Value
            Dim src2 = <text>
Class C
    Async Function F(x As Integer) As Task(Of Integer)
        If Await F(Await F(1)) Then : End If
        For Each i In {Await F(1)} : Next
        Dim a = 1
        a += Await F(1)
    End Function
End Class
</text>.Value

            ' consider: these edits can be allowed if we get more sophisticated
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.AwaitStatementUpdate, "Await F(Await F(1))"),
                Diagnostic(RudeEditKind.AwaitStatementUpdate, "{Await F(1)}"),
                Diagnostic(RudeEditKind.AwaitStatementUpdate, "a += Await F(1)"))
        End Sub

        <Fact(Skip:="TODO: Enable lambda edits")>
        Public Sub MethodWithLambda_Update()
            Dim src1 = "
Class C
    Shared Sub F()
        Dim a = Function() 1
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub F()
        Dim a = Function() 1
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.F"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub MethodUpdate_AddAttribute()
            Dim src1 = "Class C : " & vbLf & "Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@11",
                "Insert [A]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AddAttribute2()
            Dim src1 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A, B>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [<A>]@11 -> [<A, B>]@11",
                "Insert [B]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", "attribute"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AddAttribute3()
            Dim src1 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A><B>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<B>]@14",
                "Insert [B]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<B>", "attributes"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AddAttribute4()
            Dim src1 = "Class C : " & vbLf & "Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A, B>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A, B>]@11",
                "Insert [A]@12",
                "Insert [B]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A, B>", "attributes"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_UpdateAttribute()
            Dim src1 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A(1)>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@12 -> [A(1)]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(1)", "attribute"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_DeleteAttribute()
            Dim src1 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<A>]@11",
                "Delete [A]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Sub F()", "attributes"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_DeleteAttribute2()
            Dim src1 = "Class C : " & vbLf & "<A, B>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [<A, B>]@11 -> [<A>]@11",
                "Delete [B]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Sub F()", "attribute"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_DeleteAttribute3()
            Dim src1 = "Class C : " & vbLf & "<A><B>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<B>]@14",
                "Delete [B]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Sub F()", "attributes"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsDelete()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub Foo Implements I.Foo : End Sub : " & vbLf & "Sub JFoo Implements J.Foo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub Foo : End Sub : " & vbLf & "Sub JFoo Implements J.Foo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo Implements I.Foo]@29 -> [Sub Foo]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub Foo", "method"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsInsert()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub Foo : End Sub : " & vbLf & "Sub JFoo Implements J.Foo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub Foo Implements I.Foo : End Sub : " & vbLf & "Sub JFoo Implements J.Foo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Foo]@29 -> [Sub Foo Implements I.Foo]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub Foo", "method"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsUpdate()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub IFoo Implements I.Foo : End Sub : " & vbLf & "Sub JFoo Implements J.Foo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub IFoo Implements J.Foo : End Sub : " & vbLf & "Sub JFoo Implements I.Foo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub IFoo Implements I.Foo]@29 -> [Sub IFoo Implements J.Foo]@29",
                "Update [Sub JFoo Implements J.Foo]@68 -> [Sub JFoo Implements I.Foo]@68")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub IFoo", "method"),
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub JFoo", "method"))
        End Sub

        <Fact>
        Public Sub MethodUpdate_LocalWithCollectionInitializer()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim numbers() = {1, 2, 3} : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim numbers() = {1, 2, 3, 4} : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Dim numbers() = {1, 2, 3} : End Sub]@12 -> [Sub Main()" & vbLf & "Dim numbers() = {1, 2, 3, 4} : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_CatchVariableType()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Try : Catch a As Exception : End Try : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Try : Catch a As IOException : End Try : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Try : Catch a As Exception : End Try : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Try : Catch a As IOException : End Try : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_CatchVariableName()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Try : Catch a As Exception : End Try : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Try : Catch b As Exception : End Try : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Try : Catch a As Exception : End Try : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Try : Catch b As Exception : End Try : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_LocalVariableType1()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a As Exception : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a As IOException : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Dim a As Exception : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Dim a As IOException : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_LocalVariableType2()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a As New Exception : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a As New IOException : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Dim a As New Exception : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Dim a As New IOException : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_LocalVariableName1()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a As Exception : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim b As Exception : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Dim a As Exception : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Dim b As Exception : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_LocalVariableName2()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a,b As Exception : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a,c As Exception : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Main()" & vbLf & "Dim a,b As Exception : End Sub]@12 -> " &
                       "[Sub Main()" & vbLf & "Dim a,c As Exception : End Sub]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Main"))})
        End Sub

        <Fact>
        Public Sub MethodUpdate_UpdateAnonymousMethod()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "F(1, Function(a As Integer) a) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "F(2, Function(a As Integer) a) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_Query()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "F(1, From foo In bar Select baz) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "F(2, From foo In bar Select baz) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError1()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "label : Console.Write(1) : On Error GoTo label : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "label : Console.Write(2) : On Error GoTo label : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError2()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : On Error GoTo 0 : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : On Error GoTo 0 : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError3()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : On Error GoTo -1 : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : On Error GoTo -1 : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_OnError4()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : On Error Resume Next : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : On Error Resume Next : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume1()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : Resume : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : Resume : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume2()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(1) : Resume Next : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "Console.Write(2) : Resume Next : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_Resume3()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "label : Console.Write(1) : Resume label : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "label : Console.Write(2) : Resume label : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_AnonymousType()
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "F(1, New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "F(2, New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_Iterator_Yield()
            Dim src1 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : End Function : End Class"
            Dim src2 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 2 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodInsert_Iterator_Yield()
            Dim src1 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : End Function : End Class"
            Dim src2 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : Yield 2: End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Yield 2", "Yield statement"))
        End Sub

        <Fact>
        Public Sub MethodDelete_Iterator_Yield()
            Dim src1 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : Yield 2: End Function : End Class"
            Dim src2 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Iterator Function M()", "Yield statement"))
        End Sub

        <Fact>
        Public Sub MethodInsert_Handles_Clause()
            Dim src1 = "Class C : Event E1 As Action" & vbLf & "End Class"
            Dim src2 = "Class C : Event E1 As Action" & vbLf & "Private Sub Foo() Handles Me.E1 : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Private Sub Foo() Handles Me.E1 : End Sub]@29",
                "Insert [Private Sub Foo() Handles Me.E1]@29",
                "Insert [()]@44")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertHandlesClause, "Private Sub Foo()", "method"))
        End Sub
#End Region

#Region "Constructors"
        <Fact>
        Public Sub ConstructorInitializer_Update1()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer)" & vbLf & "MyBase.New(a + 1) : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub]@8 -> " &
                                     "[Public Sub New(a As Integer)" & vbLf & "MyBase.New(a + 1) : End Sub]@8")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ConstructorInitializer_Update2()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub]@8 -> " &
                                     "[Public Sub New(a As Integer) : End Sub]@8")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ConstructorInitializer_Update3()
            Dim src1 = "Class C(Of T)" & vbLf & "Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub : End Class"
            Dim src2 = "Class C(Of T)" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Public Sub New(a As Integer)" & vbLf & "MyBase.New(a) : End Sub]@14 -> " &
                                     "[Public Sub New(a As Integer) : End Sub]@14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "Public Sub New(a As Integer)", "constructor"))
        End Sub

        <Fact>
        Public Sub ConstructorUpdate_AddParameter()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer, b As Integer) : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [(a As Integer)]@22 -> [(a As Integer, b As Integer)]@22",
                "Insert [b As Integer]@37",
                "Insert [b]@37",
                "Insert [As Integer]@39")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "b As Integer", "parameter"))
        End Sub

        <Fact, WorkItem(789577)>
        Public Sub ConstructorUpdate_AnonymousTypeInFieldInitializer()
            Dim src1 = "Class C : Dim a As Integer = F(New With { .A = 1, .B = 2 })" & vbLf & "Sub New()" & vbLf & " x = 1 : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(New With { .A = 1, .B = 2 })" & vbLf & "Sub New()" & vbLf & " x = 2 : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub StaticCtorDelete()
            Dim src1 = "Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub ModuleCtorDelete()
            Dim src1 = "Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Module C", "constructor"))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Public1()
            Dim src1 = "Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Public2()
            Dim src1 = "Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Private1()
            Dim src1 = "Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Protected()
            Dim src1 = "Class C" & vbLf & "Protected Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Internal()
            Dim src1 = "Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_ProtectedInternal()
            Dim src1 = "Class C" & vbLf & "Protected Friend Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub StaticCtorInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub ModuleCtorInsert()
            Dim src1 = "Module C : End Module"
            Dim src2 = "Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Public_Implicit()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Public_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub New(a As Integer) : End Sub : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Private_Implicit1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Private Sub New()"))
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Protected_PublicImplicit()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & "Protected Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Protected Sub New()"))
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Internal_PublicImplicit()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Friend Sub New()"))
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Internal_ProtectedImplicit()
            Dim src1 = "MustInherit Class C : End Class"
            Dim src2 = "MustInherit Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Friend Sub New()"))
        End Sub

        <Fact>
        Public Sub InstanceCtorUpdate_ProtectedImplicit()
            Dim src1 = "Class C" & vbLf & "End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c)
                                                           Return c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
                                                       End Function)})
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Private_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").
                                      InstanceConstructors.Single(Function(ctor) ctor.DeclaredAccessibility = Accessibility.Private))})
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Internal_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : " & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Protected_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : " & vbLf & "Protected Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_FriendProtected_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : " & vbLf & "Friend Protected Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub StaticCtor_Partial_Delete()
            Dim srcA1 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty, {srcB1}, {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub ModuleCtor_Partial_Delete()
            Dim srcA1 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim srcB1 = "Partial Module C : End Module"

            Dim srcA2 = "Partial Module C : End Module"
            Dim srcB2 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty, {srcB1}, {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePrivate()
            Dim srcA1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePublic()
            Dim srcA1 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePrivateToPublic()
            Dim srcA1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  Nothing,
                                  Diagnostic(RudeEditKind.Delete, "Partial Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub StaticCtor_Partial_Insert()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub ModuleCtor_Partial_Insert()
            Dim srcA1 = "Partial Module C : End Module"
            Dim srcB1 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"

            Dim srcA2 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim srcB2 = "Partial Module C : End Module"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPublic()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPrivate()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertInternal()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPrivateToPublic()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  Nothing,
                                  Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Sub New()"))
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPrivateToInternal()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            Dim edits = GetTopEdits(srcA1, srcA2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {srcB1},
                                  {srcB2},
                                  Nothing,
                                  Diagnostic(RudeEditKind.ChangingConstructorVisibility, "Friend Sub New()"))
        End Sub

#End Region

#Region "Declare"
        <Fact>
        Public Sub Declare_Update1()
            Dim src1 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Foo Lib ""Baz"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10 -> [Declare Ansi Function Foo Lib ""Baz"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareLibraryUpdate, "Declare Ansi Function Foo Lib ""Baz"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Update2()
            Dim src1 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Unicode Function Foo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10 -> [Declare Unicode Function Foo Lib ""Bar"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Declare Unicode Function Foo Lib ""Bar"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Update3()
            Dim src1 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" Alias ""Al"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10 -> [Declare Ansi Function Foo Lib ""Bar"" Alias ""Al"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareAliasUpdate, "Declare Ansi Function Foo Lib ""Bar"" Alias ""Al"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Update4()
            Dim src1 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" Alias ""A1"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" Alias ""A2"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Foo Lib ""Bar"" Alias ""A1"" () As Integer]@10 -> [Declare Ansi Function Foo Lib ""Bar"" Alias ""A2"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareAliasUpdate, "Declare Ansi Function Foo Lib ""Bar"" Alias ""A2"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Delete()
            Dim src1 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Delete [Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10",
                "Delete [()]@46",
                "Delete [As Integer]@49")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Insert1()
            Dim src1 As String = "Class C : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10",
                "Insert [()]@46",
                "Insert [As Integer]@49")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Declare Ansi Function Foo Lib ""Bar"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Insert2()
            Dim src1 As String = "Class C : End Class"
            Dim src2 As String = "Class C : Private Declare Ansi Function Foo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Private Declare Ansi Function Foo Lib ""Bar"" () As Integer]@10",
                "Insert [()]@54",
                "Insert [As Integer]@57")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Private Declare Ansi Function Foo Lib ""Bar"" ()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Insert3()
            Dim src1 As String = "Module M : End Module"
            Dim src2 As String = "Module M : Declare Ansi Sub ExternSub Lib ""ExternDLL""() : End Module"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Declare Ansi Sub ExternSub Lib ""ExternDLL""()", "method"))
        End Sub

        <Fact>
        Public Sub Declare_Insert4()
            Dim src1 As String = "Module M : End Module"
            Dim src2 As String = "Module M : Declare Ansi Sub ExternSub Lib ""ExternDLL"" : End Module"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Declare Ansi Sub ExternSub Lib ""ExternDLL""", "method"))
        End Sub
#End Region

#Region "Fields"
        <Fact>
        Public Sub FieldUpdate_Rename1()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim b As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@14 -> [b]@14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "field"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_Rename2()
            Dim src1 = "Class C : Dim a1(), b1? As Integer, c1(1,2) As New D() : End Class"
            Dim src2 = "Class C : Dim a2(), b2? As Integer, c2(1,2) As New D() : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a1()]@14 -> [a2()]@14",
                "Update [b1?]@20 -> [b2?]@20",
                "Update [c1(1,2)]@36 -> [c2(1,2)]@36")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "a2()", "field"),
                Diagnostic(RudeEditKind.Renamed, "b2?", "field"),
                Diagnostic(RudeEditKind.Renamed, "c2(1,2)", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate1()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Boolean : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@16 -> [As Boolean]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Boolean", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove1()
            Dim src1 = "Class C : Dim b As Object, c : End Class"
            Dim src2 = "Class C : Dim b, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim b As Object, c]@10 -> [Dim b, c As Object]@10",
                "Update [b As Object]@14 -> [b, c As Object]@14",
                "Move [c]@27 -> @17",
                "Delete [c]@27")

            ' TODO: We could check that the types and order of b and c haven't changed. 
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "c", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove2()
            Dim src1 = "Class C : Dim b As Object, c As Object : End Class"
            Dim src2 = "Class C : Dim b, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim b As Object, c As Object]@10 -> [Dim b, c As Object]@10",
                "Update [b As Object]@14 -> [b, c As Object]@14",
                "Move [c]@27 -> @17",
                "Delete [c As Object]@27",
                "Delete [As Object]@29")

            ' TODO: We could check that the types and order of b and c haven't changed. 
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "c", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove3()
            Dim src1 = "Class C : Dim b, c As Object : End Class"
            Dim src2 = "Class C : Dim b As Object, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim b, c As Object]@10 -> [Dim b As Object, c As Object]@10",
                "Update [b, c As Object]@14 -> [b As Object]@14",
                "Insert [c As Object]@27",
                "Move [c]@17 -> @27",
                "Insert [As Object]@29")

            ' TODO: We could check that the types and order of b and c haven't changed. 
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "c", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove4()
            Dim src1 = "Class C : Dim a, b As Object, c As Object : End Class"
            Dim src2 = "Class C : Dim a As Object, b, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a, b As Object]@14 -> [a As Object]@14",
                "Update [c As Object]@30 -> [b, c As Object]@27",
                "Move [b]@17 -> @27")

            ' TODO: We could check that the types and order of b and c haven't changed. 
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove5()
            Dim src1 = "Class C : Dim a As Object, b, c As Object : End Class"
            Dim src2 = "Class C : Dim a, b As Object, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Object]@14 -> [a, b As Object]@14",
                "Update [b, c As Object]@27 -> [c As Object]@30",
                "Move [b]@27 -> @17")

            ' TODO: We could check that the types and order of b and c haven't changed. 
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove6()
            Dim src1 = "Class C : Dim a As Object, b As Object : End Class"
            Dim src2 = "Class C : Dim b As Object, a As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b As Object]@27 -> @14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b As Object", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableMove7()
            Dim src1 = "Class C : Dim a As Object, b, c As Object : End Class"
            Dim src2 = "Class C : Dim b, c As Object, a As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b, c As Object]@27 -> @14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b, c As Object", "field"))
        End Sub

        <Fact>
        Public Sub Field_VariableDelete1()
            Dim src1 = "Class C : Dim b As Object, c As Object : End Class"
            Dim src2 = "Class C : Dim b As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim b As Object, c As Object]@10 -> [Dim b As Object]@10",
                "Delete [c As Object]@27",
                "Delete [c]@27",
                "Delete [As Object]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Dim b As Object", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate2()
            Dim src1 = "Class C : Dim a,  b   As Integer, c?, d() As New D() : End Class"
            Dim src2 = "Class C : Dim a?, b() As Integer, c,  d   As New D() : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@14 -> [a?]@14",
                "Update [b]@18 -> [b()]@18",
                "Update [c?]@34 -> [c]@34",
                "Update [d()]@38 -> [d]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a?", "field"),
                Diagnostic(RudeEditKind.TypeUpdate, "b()", "field"),
                Diagnostic(RudeEditKind.TypeUpdate, "c", "field"),
                Diagnostic(RudeEditKind.TypeUpdate, "d", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate3()
            Dim src1 = "Class C : Dim a(3) As Integer, c(2,2) : End Class"
            Dim src2 = "Class C : Dim a(2) As Integer, c(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a(3)]@14 -> [a(2)]@14",
                "Update [c(2,2)]@31 -> [c(2)]@31")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "c(2)", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate4a()
            Dim src1 = "Class C : Dim a As Integer() : End Class"
            Dim src2 = "Class C : Dim a() As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@14 -> [a()]@14",
                "Update [As Integer()]@16 -> [As Integer]@18")

            ' TODO: the type didn't really change, we can allow this
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a()", "field"),
                Diagnostic(RudeEditKind.TypeUpdate, "a() As Integer", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate4b()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a(1) As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a(1)", "field"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate5()
            Dim src1 = "Class C : Dim a, b : End Class"
            Dim src2 = "Class C : Dim a, b As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As Integer]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Integer", "as clause"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate6()
            Dim src1 = "Class C : Dim a, b As Integer : End Class"
            Dim src2 = "Class C : Dim a, b : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a, b", "as clause"))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate7()
            Dim src1 = "Class C : Dim a(1) As Integer : End Class"
            Dim src2 = "Class C : Dim a(1,2) As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a(1,2)", "field"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_FieldToEvent()
            Dim src1 = "Class C : Dim a As Action : End Class"
            Dim src2 = "Class C : Event a As Action : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Event a As Action]@10",
                "Insert [As Action]@18",
                "Delete [Dim a As Action]@10",
                "Delete [a As Action]@14",
                "Delete [a]@14",
                "Delete [As Action]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "field"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_EventToField()
            Dim src1 = "Class C : Event a As Action : End Class"
            Dim src2 = "Class C : Dim a As Action : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Dim a As Action]@10",
                "Insert [a As Action]@14",
                "Insert [a]@14",
                "Insert [As Action]@16",
                "Delete [Event a As Action]@10",
                "Delete [As Action]@18")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "event"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_FieldToWithEvents()
            Dim src1 = "Class C : Dim a As WE : End Class"
            Dim src2 = "Class C : WithEvents a As WE : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim a As WE]@10 -> [WithEvents a As WE]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "WithEvents a As WE", "WithEvents field"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_WithEventsToField()
            Dim src1 = "Class C : WithEvents a As WE : End Class"
            Dim src2 = "Class C : Dim a As WE : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [WithEvents a As WE]@10 -> [Dim a As WE]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Dim a As WE", "field"))
        End Sub

        <Fact>
        Public Sub FieldReorder()
            Dim src1 = "Class C : Dim a = 0 : Dim b = 1 : Dim c = 2 : End Class"
            Dim src2 = "Class C : Dim c = 2 : Dim a = 0 : Dim b = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Dim c = 2", "field"))
        End Sub

        <Fact>
        Public Sub EventFieldReorder()
            Dim src1 = "Class C : Dim a As Integer = 0 : Dim b As Integer = 1 : Event c As Action : End Class"
            Dim src2 = "Class C : Event c As Action : Dim a As Integer = 0 : Dim b As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Event c As Action]@56 -> @10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Event c", "event"))
        End Sub

        <Fact>
        Public Sub FieldInsert1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Dim _private1 = 1 : Private _private2 : Public _public = 2 : Protected _protected : Friend _f : Protected Friend _pf : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldInsert_WithEvents1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : WithEvents F As C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "WithEvents F As C", "WithEvents field"))
        End Sub

        <Fact>
        Public Sub FieldInsert_WithEvents2()
            Dim src1 = "Class C : WithEvents F As C : End Class"
            Dim src2 = "Class C : WithEvents F, G As C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "G", "WithEvents field"))
        End Sub

        <Fact>
        Public Sub FieldInsert_WithEvents3()
            Dim src1 = "Class C : WithEvents F As C : End Class"
            Dim src2 = "Class C : WithEvents F As C, G As C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "G", "WithEvents field"))
        End Sub

        <Fact>
        Public Sub FieldInsert_IntoStruct()
            Dim src1 = "Structure S : Private a As Integer : End Structure"
            Dim src2 = <text>
Structure S 
    Private a As Integer
    Private b As Integer
    Private Shared c As Integer
    Private Event d As System.Action
End Structure
</text>.Value
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Event d As System.Action", "event", "structure"),
                Diagnostic(RudeEditKind.InsertIntoStruct, "b", "field", "structure"),
                Diagnostic(RudeEditKind.InsertIntoStruct, "c", "field", "structure"))
        End Sub

        <Fact>
        Public Sub FieldInsert_IntoLayoutClass_Auto()
            Dim src1 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Auto)>
Class C
    Private a As Integer
End Class
"

            Dim src2 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Auto)>
Class C
    Private a As Integer
    Private b As Integer
    Private c As Integer
    Private Shared d As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.b")),
                                   SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.c")),
                                   SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.d"))})
        End Sub

        <Fact>
        Public Sub FieldInsert_IntoLayoutClass_Explicit()
            Dim src1 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Explicit)>
Class C
    <FieldOffset(0)>
    Private a As Integer
End Class
"

            Dim src2 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Explicit)>
Class C
    <FieldOffset(0)>
    Private a As Integer
    
    <FieldOffset(0)>
    Private b As Integer

    Private c As Integer
    Private Shared d As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", "field", "class"),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", "field", "class"),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", "field", "class"))
        End Sub

        <Fact>
        Public Sub FieldInsert_IntoLayoutClass_Sequential()
            Dim src1 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private a As Integer
End Class
"

            Dim src2 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private a As Integer
    Private b As Integer
    Private c As Integer
    Private Shared d As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", "field", "class"),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", "field", "class"),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", "field", "class"))
        End Sub

#End Region

#Region "Properties"
        <Fact>
        Public Sub PropertyReorder1()
            Dim src1 = "Class C : ReadOnly Property P" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : " &
                                 "ReadOnly Property Q" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : End Class"
            Dim src2 = "Class C : ReadOnly Property Q" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : " &
                                 "ReadOnly Property P" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [ReadOnly Property Q" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property]@70 -> @10")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyReorder2()
            Dim src1 = "Class C : Property P As Integer : Property Q As Integer : End Class"
            Dim src2 = "Class C : Property Q As Integer : Property P As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Property Q As Integer]@34 -> @10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Property Q", "auto-property"))
        End Sub

        <Fact>
        Public Sub PropertyAccessorReorder()
            Dim src1 = "Class C : Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get" & vbLf & "Set : End Set : End Property : End Class"
            Dim src2 = "Class C : Property P As Integer" & vbLf & "Set : End Set" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Set : End Set]@55 -> @32")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyTypeUpdate()
            Dim src1 = "Class C : Property P As Integer : End Class"
            Dim src2 = "Class C : Property P As Char : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@21 -> [As Char]@21")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Property P", "auto-property"))
        End Sub

        <Fact>
        Public Sub PrivatePropertyAccessorAddSetter()
            Dim src1 = "Class C : Private _p As Integer : Private ReadOnly Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : End Class"
            Dim src2 = "Class C : Private _p As Integer : Private Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get" & vbLf & "Set(value As Integer)" & vbLf & "_p = value : End Set : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Private ReadOnly Property P As Integer]@34 -> [Private Property P As Integer]@34",
                "Insert [Set(value As Integer)" & vbLf & "_p = value : End Set]@87",
                "Insert [Set(value As Integer)]@87",
                "Insert [(value As Integer)]@90",
                "Insert [value As Integer]@91",
                "Insert [value]@91",
                "Insert [As Integer]@97")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PrivatePropertyAccessorAddGetter()
            Dim src1 = "Class C : Private _p As Integer : Private WriteOnly Property P As Integer" & vbLf & "Set(value As Integer)" & vbLf & "_p = value : End Set : End Property : End Class"
            Dim src2 = "Class C : Private _p As Integer : Private Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get" & vbLf & "Set(value As Integer)" & vbLf & "_p = value : End Set : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Private WriteOnly Property P As Integer]@34 -> [Private Property P As Integer]@34",
                "Insert [Get" & vbLf & "Return 1 : End Get]@64",
                "Insert [Get]@64")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PrivatePropertyAccessorDelete()
            Dim src1 = "Class C : Private _p As Integer : Private Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get" & vbLf & "Set(value As Integer)" & vbLf & "_p = value : End Set : End Property : End Class"
            Dim src2 = "Class C : Private _p As Integer : Private ReadOnly Property P As Integer" & vbLf & "Get" & vbLf & "Return 1 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Private Property P As Integer]@34 -> [Private ReadOnly Property P As Integer]@34",
                "Delete [Set(value As Integer)" & vbLf & "_p = value : End Set]@87",
                "Delete [Set(value As Integer)]@87",
                "Delete [(value As Integer)]@90",
                "Delete [value As Integer]@91",
                "Delete [value]@91",
                "Delete [As Integer]@97")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Private ReadOnly Property P", "property accessor"))
        End Sub

        <Fact>
        Public Sub PropertyRename()
            Dim src1 = "Class C : ReadOnly Property P As Integer" & vbLf & "Get : End Get : End Property : End Class"
            Dim src2 = "Class C : ReadOnly Property Q As Integer" & vbLf & "Get : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ReadOnly Property P As Integer]@10 -> [ReadOnly Property Q As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "ReadOnly Property Q", "auto-property"))
        End Sub

        <Fact>
        Public Sub PropertyInsert_IntoStruct()
            Dim src1 = "Structure S : Private a As Integer : End Structure"
            Dim src2 = <text>
Structure S
    Private a As Integer
    Private Property b As Integer
    Private Shared Property c As Integer

    Private Property d As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property

    Private Shared Property e As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Structure
</text>.Value
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Property b As Integer", "auto-property", "structure"),
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Shared Property c As Integer", "auto-property", "structure"))
        End Sub

        <Fact>
        Public Sub PropertyInsert_IntoLayoutClass_Sequential()
            Dim src1 = <![CDATA[
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private a As Integer
End Class
]]>.Value

            Dim src2 = <![CDATA[
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private a As Integer
    Private Property b As Integer
    Private Shared Property c As Integer

    Private Property d As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property

    Private Shared Property e As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "Private Property b As Integer", "auto-property", "class"),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "Private Shared Property c As Integer", "auto-property", "class"))
        End Sub

#End Region

#Region "Field And Property Initializers"
        <Fact>
        Public Sub Field_InitializerUpdate1()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer = 0]@14 -> [a As Integer = 1]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate2()
            Dim src1 = "Class C : Dim a, b As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a, b As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a, b As Integer = 0]@14 -> [a, b As Integer = 1]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_Instance_InitializerUpdate()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer = 0]@10 -> [Property a As Integer = 1]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_Instance_AsNewInitializerUpdate()
            Dim src1 = "Class C : Property a As New C(0) : End Class"
            Dim src2 = "Class C : Property a As New C(1) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As New C(0)]@10 -> [Property a As New C(1)]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Array1()
            Dim src1 = "Class C : Dim a(1), b(1) : End Class"
            Dim src2 = "Class C : Dim a(2), b(1) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a(1)]@14 -> [a(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_AsNew1()
            Dim src1 = "Class C : Dim a As New D(1) : End Class"
            Dim src2 = "Class C : Dim a As New D(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As New D(1)]@14 -> [a As New D(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_AsNew2()
            Dim src1 = "Class C : Dim a, b As New C(1) : End Class"
            Dim src2 = "Class C : Dim a, b As New C(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a, b As New C(1)]@14 -> [a, b As New C(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_AsNew()
            Dim src1 = "Class C : Property a As New D(1) : End Class"
            Dim src2 = "Class C : Property a As New D(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As New D(1)]@10 -> [Property a As New D(2)]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Untyped()
            Dim src1 = "Class C : Dim a = 1 : End Class"
            Dim src2 = "Class C : Dim a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a = 1]@14 -> [a = 2]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Untyped()
            Dim src1 = "Class C : Property a = 1 : End Class"
            Dim src2 = "Class C : Property a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a = 1]@10 -> [Property a = 2]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Delete()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer = 0]@14 -> [a As Integer]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Delete()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer = 0]@10 -> [Property a As Integer]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_StructInitializerUpdate_Delete()
            Dim src1 = "Structure C : Property a As Integer = 0 : End Structure"
            Dim src2 = "Structure C : Property a As Integer : End Structure"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer = 0]@14 -> [Property a As Integer]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Insert()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@14 -> [a As Integer = 0]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Insert()
            Dim src1 = "Class C : Property a As Integer : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer]@10 -> [Property a As Integer = 0]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_AsNewToInitializer()
            Dim src1 = "Class C : Property a As New Integer() : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As New Integer()]@10 -> [Property a As Integer = 0]@10",
                "Insert [As Integer]@21")

            ' TODO: we could detect that the type haven't changed and allow this
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Integer", "as clause"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticCtorUpdate1()
            Dim src1 = "Class C : Shared a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@17 -> [a As Integer = 0]@17",
                "Delete [Shared Sub New() : End Sub]@33",
                "Delete [Shared Sub New()]@33",
                "Delete [()]@47")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticStructCtorUpdate1()
            Dim src1 = "Structure C : Shared a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Structure"
            Dim src2 = "Structure C : Shared a As Integer = 0 : End Structure"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@21 -> [a As Integer = 0]@21",
                "Delete [Shared Sub New() : End Sub]@37",
                "Delete [Shared Sub New()]@37",
                "Delete [()]@51")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorUpdate1()
            Dim src1 = "Module C : Dim a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@15 -> [a As Integer = 0]@15",
                "Delete [Shared Sub New() : End Sub]@31",
                "Delete [Shared Sub New()]@31",
                "Delete [()]@45")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_StaticCtorUpdate1()
            Dim src1 = "Class C : Shared Property a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Shared Property a As Integer]@10 -> [Shared Property a As Integer = 0]@10",
                "Delete [Shared Sub New() : End Sub]@42",
                "Delete [Shared Sub New()]@42",
                "Delete [()]@56")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_ModuleCtorUpdate1()
            Dim src1 = "Module C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Property a As Integer = 0 : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer]@11 -> [Property a As Integer = 0]@11",
                "Delete [Sub New() : End Sub]@36",
                "Delete [Sub New()]@36",
                "Delete [()]@43")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_Private()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Private Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_Private()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Private Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "constructor"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_Public()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_Public()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticCtorUpdate2()
            Dim src1 = "Class C : Shared a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorUpdate2()
            Dim src1 = "Module C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_StaticCtorUpdate2()
            Dim src1 = "Class C : Shared Property a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared Property a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_ModuleCtorUpdate2()
            Dim src1 = "Module C : Property a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Property a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate2()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate2()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate3()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate3()
            Dim src1 = "Class C : Property a As Integer : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate4()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate4()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate5()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)")),
                                  SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate5()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)")),
                                  SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_MeNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_MeNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_MyClassNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_MyClassNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_EscapedNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_EscapedNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticCtorInsertImplicit()
            Dim src1 = "Class C : Shared a As Integer : End Class"
            Dim src2 = "Class C : Shared a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorInsertImplicit()
            Dim src1 = "Module C : Dim a As Integer : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_StaticCtorInsertImplicit()
            Dim src1 = "Class C : Shared Property a As Integer : End Class"
            Dim src2 = "Class C : Shared Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_ModuleCtorInsertImplicit()
            Dim src1 = "Module C : Property a As Integer : End Module"
            Dim src2 = "Module C : Property a As Integer = 0 : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticCtorInsertExplicit()
            Dim src1 = "Class C : Shared a As Integer : End Class"
            Dim src2 = "Class C : Shared a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorInsertExplicit()
            Dim src1 = "Module C : Dim a As Integer : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_StaticCtorInsertExplicit()
            Dim src1 = "Class C : Shared Property a As Integer : End Class"
            Dim src2 = "Class C : Shared Property a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_ModuleCtorInsertExplicit()
            Dim src1 = "Module C : Property a As Integer : End Module"
            Dim src2 = "Module C : Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorInsertExplicit()
            Dim src1 = "Class C : Private a As Integer : End Class"
            Dim src2 = "Class C : Private a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PeopertyUpdate_InstanceCtorInsertExplicit()
            Dim src1 = "Class C : Private Property a As Integer : End Class"
            Dim src2 = "Class C : Private Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub FieldUpdate_GenericType()
            Dim src1 = "Class C(Of T) : Dim a As Integer = 1 : End Class"
            Dim src2 = "Class C(Of T) : Dim a As Integer = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "a As Integer = 2", "field"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_GenericType()
            Dim src1 = "Class C(Of T) : Property a As Integer = 1 : End Class"
            Dim src2 = "Class C(Of T) : Property a As Integer = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "Property a", "auto-property"))
        End Sub

        <Fact(Skip:="726990")>
        Public Sub FieldUpdate_LambdaInConstructor()
            Dim src1 = "Class C : Dim a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Sub()", "constructor"))
        End Sub

        <Fact(Skip:="726990")>
        Public Sub PropertyUpdate_LambdaInConstructor()
            Dim src1 = "Class C : Property a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Sub()", "constructor"))
        End Sub

        <Fact(Skip:="726990")>
        Public Sub FieldUpdate_QueryInConstructor()
            Dim src1 = "Class C : Dim a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "From", "constructor"))
        End Sub

        <Fact(Skip:="726990")>
        Public Sub PropertyUpdate_QueryInConstructor()
            Dim src1 = "Class C : Property a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "From", "constructor"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_AnonymousTypeInConstructor()
            Dim src1 = "Class C : Dim a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_AnonymousTypeInConstructor()
            Dim src1 = "Class C : Property a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(New With { .A = 1, .B = 2 }) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldUpdate_PartialTypeWithSingleDeclaration()
            Dim src1 = "Partial Class C : Dim a = 1 : End Class"
            Dim src2 = "Partial Class C : Dim a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "a = 2"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithSingleDeclaration()
            Dim src1 = "Partial Class C : Property a = 1 : End Class"
            Dim src2 = "Partial Class C : Property a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "Property a = 2"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_PartialTypeWithMultipleDeclarations1()
            Dim src1 = "Partial Class C : Dim a = 1 : End Class : Class C : End Class"
            Dim src2 = "Partial Class C : Dim a = 2 : End Class : Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "a = 2"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithMultipleDeclarations1()
            Dim src1 = "Partial Class C : Property a = 1 : End Class : Class C : End Class"
            Dim src2 = "Partial Class C : Property a = 2 : End Class : Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "Property a = 2"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_PartialTypeWithMultipleDeclarations2()
            Dim src1 = "Class C : Dim a = 1 : End Class : Partial Class C : End Class"
            Dim src2 = "Class C : Dim a = 2 : End Class : Partial Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "a = 2"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithMultipleDeclarations2()
            Dim src1 = "Class C : Property a = 1 : End Class : Partial Class C : End Class"
            Dim src2 = "Class C : Property a = 2 : End Class : Partial Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "Property a = 2"))
        End Sub

        <Fact>
        Public Sub PrivateFieldInsert1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private a As Integer = 1]@10",
                "Insert [a As Integer = 1]@18",
                "Insert [a]@18",
                "Insert [As Integer]@20")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PrivateFieldInsert2()
            Dim src1 = "Class C : Private a As Integer = 1 : End Class"
            Dim src2 = "Class C : Private a, b As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer = 1]@18 -> [a, b As Integer = 1]@18",
                "Insert [b]@21")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.b")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PrivatePropertyInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Property a As Integer = 1]@10",
                "Insert [As Integer]@29")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PrivateFieldInsert_Untyped()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private a = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private a = 1]@10",
                "Insert [a = 1]@18",
                "Insert [a]@18")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PrivatePropertyInsert_Untyped()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private Property a = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Property a = 1]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PrivateReadOnlyFieldInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private ReadOnly a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private ReadOnly a As Integer = 1]@10",
                "Insert [a As Integer = 1]@27",
                "Insert [a]@27",
                "Insert [As Integer]@29")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

        <Fact>
        Public Sub PublicFieldInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Public a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PublicPropertyInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ProtectedFieldInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Protected a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ProtectedPropertyInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Protected Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldDelete()
            Dim src1 = "Class C : Private Dim a As Integer = 1 : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Private Dim a As Integer = 1]@10",
                "Delete [a As Integer = 1]@22",
                "Delete [a]@22",
                "Delete [As Integer]@24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "field"))
        End Sub

        <Fact>
        Public Sub PropertyDelete()
            Dim src1 = "Class C : Private Property a As Integer = 1 : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Private Property a As Integer = 1]@10",
                "Delete [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "auto-property"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_SingleLineFunction()
            Dim src1 = "Class C : Dim a As Integer = F(1, Function(x, y) x + y) : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(2, Function(x, y) x + y) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Function(x, y)", "field"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_SingleLineFunction()
            Dim src1 = "Class C : Property a As Integer = F(1, Function(x, y) x + y) : End Class"
            Dim src2 = "Class C : Property a As Integer = F(2, Function(x, y) x + y) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Function(x, y)", "auto-property"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_MultiLineFunction()
            Dim src1 = "Class C : Dim a As Integer = F(1, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(2, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Function(x)", "field"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_MultiLineFunction()
            Dim src1 = "Class C : Property a As Integer = F(1, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim src2 = "Class C : Property a As Integer = F(2, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "Function(x)", "auto-property"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_Query()
            Dim src1 = "Class C : Dim a = F(1, From foo In bar Select baz) : End Class"
            Dim src2 = "Class C : Dim a = F(2, From foo In bar Select baz) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "From", "field"))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_Query()
            Dim src1 = "Class C : Property a = F(1, From foo In bar Select baz) : End Class"
            Dim src2 = "Class C : Property a = F(2, From foo In bar Select baz) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "From", "auto-property"))
        End Sub

        <Fact>
        Public Sub FieldUpdate_AnonymousType()
            Dim src1 = "Class C : Dim a As Integer = F(1, New With { .A = 1, .B = 2 }) : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(2, New With { .A = 1, .B = 2 }) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_AnonymousType()
            Dim src1 = "Class C : Property a As Integer = F(1, New With { .A = 1, .B = 2 }) : End Class"
            Dim src2 = "Class C : Property a As Integer = F(2, New With { .A = 1, .B = 2 }) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_AsNewAnonymousType()
            Dim src1 = "Class C : Property a As New C(1, New With { .A = 1, .B = 2 }) : End Class"
            Dim src2 = "Class C : Property a As New C(2, New With { .A = 1, .B = 2 }) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub
#End Region

#Region "Events"

        <Fact>
        Public Sub EventAccessorReorder1()
            Dim src1 = "Class C : " &
                          "Custom Event E As Action" & vbLf &
                             "AddHandler(value As Action) : End AddHandler" & vbLf &
                             "RemoveHandler(value As Action) : End RemoveHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim src2 = "Class C : " &
                          "Custom Event E As Action" & vbLf &
                             "RemoveHandler(value As Action) : End RemoveHandler" & vbLf &
                             "AddHandler(value As Action) : End AddHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [RemoveHandler(value As Action) : End RemoveHandler]@80 -> @35")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub Event_Rename()
            Dim src1 = "Class C : " &
                          "Custom Event E As Action" & vbLf &
                             "AddHandler(value As Action) : End AddHandler" & vbLf &
                             "RemoveHandler(value As Action) : End RemoveHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim src2 = "Class C : " &
                          "Custom Event F As Action" & vbLf &
                             "AddHandler(value As Action) : End AddHandler" & vbLf &
                             "RemoveHandler(value As Action) : End RemoveHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Custom Event E As Action]@10 -> [Custom Event F As Action]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Event F", "event"))
        End Sub

        <Fact>
        Public Sub Event_UpdateType()
            Dim src1 = "Class C : " &
                          "Custom Event E As Action" & vbLf &
                             "AddHandler(value As Action) : End AddHandler" & vbLf &
                             "RemoveHandler(value As Action) : End RemoveHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim src2 = "Class C : " &
                          "Custom Event E As Action(Of T)" & vbLf &
                             "AddHandler(value As Action(Of T)) : End AddHandler" & vbLf &
                             "RemoveHandler(value As Action(Of T)) : End RemoveHandler" & vbLf &
                             "RaiseEvent() : End RaiseEvent : " &
                          "End Event : " &
                       "End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Action]@25 -> [As Action(Of T)]@25",
                "Update [As Action]@52 -> [As Action(Of T)]@58",
                "Update [As Action]@100 -> [As Action(Of T)]@112")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Event E", "event"),
                Diagnostic(RudeEditKind.TypeUpdate, "value As Action(Of T)", "parameter"),
                Diagnostic(RudeEditKind.TypeUpdate, "value As Action(Of T)", "parameter"))
        End Sub

        <Fact>
        Public Sub EventInsert_IntoLayoutClass_Sequential()
            Dim src1 = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
End Class
]]>.Value
            Dim src2 = <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private Custom Event c As Action
        AddHandler
        End AddHandler

        RemoveHandler
        End RemoveHandler
    End Event
End Class
]]>.Value
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics()
        End Sub

#End Region

#Region "Parameters And Return Values"

        <Fact>
        Public Sub ParameterRename_Method1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@24 -> [b]@24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterRename_Ctor1()
            Dim src1 = "Class C : " & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub New(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@26 -> [b]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterRename_Operator1()
            Dim src1 = "Class C : " & vbLf & "Public Shared Operator CType(a As C) As Integer : End Operator : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Shared Operator CType(b As C) As Integer : End Operator : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@40 -> [b]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterRename_Operator2()
            Dim src1 = "Class C : " & vbLf & "Public Shared Operator +(a As C, b As C) As Integer" & vbLf & "Return 0 : End Operator : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Shared Operator +(a As C, x As C) As Integer" & vbLf & "Return 0 : End Operator : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [b]@44 -> [x]@44")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "x", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterRename_Property1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer, b As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer, x As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [b]@52 -> [x]@52")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "x", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@24 -> [b]@24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_Modifier()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(ByRef a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ByRef a As Integer]@38 -> [a As Integer]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "a As Integer", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_AsClause1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Object) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@40 -> [As Object]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Object", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_AsClause2()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a", "as clause"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_DefaultValue1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 0) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@38 -> [Optional a As Integer = 0]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Optional a As Integer = 0", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_DefaultValue2()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 0) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 1) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Optional a As Integer = 0]@38 -> [Optional a As Integer = 1]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Optional a As Integer = 1", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterInsert1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [a As Integer]@24",
                "Insert [a]@24",
                "Insert [As Integer]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "a As Integer", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterInsert2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(a As Integer, ByRef b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(a As Integer)]@23 -> [(a As Integer, ByRef b As Integer)]@23",
                "Insert [ByRef b As Integer]@38",
                "Insert [b]@44",
                "Insert [As Integer]@46")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "ByRef b As Integer", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterListInsert1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [()]@23")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ParameterListInsert2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [(a As Integer)]@23",
                "Insert [a As Integer]@24",
                "Insert [a]@24",
                "Insert [As Integer]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "(a As Integer)", "parameters"))
        End Sub

        <Fact>
        Public Sub ParameterDelete1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [a As Integer]@24",
                "Delete [a]@24",
                "Delete [As Integer]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M()", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterDelete2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer, b As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(a As Integer, b As Integer)]@23 -> [(b As Integer)]@23",
                "Delete [a As Integer]@24",
                "Delete [a]@24",
                "Delete [As Integer]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M(b As Integer)", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterListDelete1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [()]@23")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ParameterListDelete2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [(a As Integer)]@23",
                "Delete [a As Integer]@24",
                "Delete [a]@24",
                "Delete [As Integer]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M", "parameters"))
        End Sub

        <Fact>
        Public Sub ParameterReorder()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer, b As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer, a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b As Integer]@38 -> @24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b As Integer", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterReorderAndUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer, b As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer, c As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b As Integer]@38 -> @24",
                "Update [a]@24 -> [c]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b As Integer", "parameter"),
                Diagnostic(RudeEditKind.Renamed, "c", "parameter"))
        End Sub

        <Fact>
        Public Sub ParameterAttributeInsert1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(<A>a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@24",
                "Insert [A]@25")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub ParameterAttributeInsert2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(<A>a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(<A, B>a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [<A>]@24 -> [<A, B>]@24",
                "Insert [B]@28")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", "attribute"))
        End Sub

        <Fact>
        Public Sub ParameterAttributeDelete()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(<A>a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<A>]@24",
                "Delete [A]@25")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a As Integer", "attributes"))
        End Sub

        <Fact>
        Public Sub ParameterAttributeUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(<A(1), C>a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(<A(2), B>a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A(1)]@25 -> [A(2)]@25",
                "Update [C]@31 -> [B]@31")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", "attribute"),
                Diagnostic(RudeEditKind.Update, "B", "attribute"))
        End Sub

        <Fact>
        Public Sub ReturnValueAttributeUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As <B>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@35 -> [B]@35")

            edits.VerifyRudeDiagnostics(
               Diagnostic(RudeEditKind.Update, "B", "attribute"))
        End Sub

        <Fact>
        Public Sub ParameterAttributeReorder()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(<A(1), A(2)>a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(<A(2), A(1)>a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [A(2)]@31 -> @25")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub FunctionAsClauseAttributeInsert()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@34",
                "Insert [A]@35")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", "attributes"))
        End Sub

        <Fact>
        Public Sub FunctionAsClauseAttributeDelete()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<A>]@34",
                "Delete [A]@35")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Function M()", "attributes"))
        End Sub

        <Fact>
        Public Sub FunctionAsClauseDelete()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M()" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As <A>Integer]@31",
                "Delete [<A>]@34",
                "Delete [A]@35")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Function M()", "as clause"))
        End Sub

        <Fact>
        Public Sub FunctionAsClauseInsert()
            Dim src1 = "Class C : " & vbLf & "Public Function M()" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As <A>Integer]@31",
                "Insert [<A>]@34",
                "Insert [A]@35")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As <A>Integer", "as clause"))
        End Sub

        <Fact>
        Public Sub FunctionAsClauseUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As Object" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@31 -> [As Object]@31")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Public Function M()", "method"))
        End Sub

#End Region

#Region "Method Type Parameter"

        <Fact>
        Public Sub MethodTypeParameterInsert1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of A)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [(Of A)]@23",
                "Insert [A]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "(Of A)", "type parameters"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterInsert2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of A, B)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A)]@23 -> [(Of A, B)]@23",
                "Insert [B]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterDelete1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [(Of A)]@23",
                "Delete [A]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M()", "type parameters"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterDelete2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A, B)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A, B)]@23 -> [(Of B)]@23",
                "Delete [A]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M(Of B)()", "type parameter"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@27 -> [B]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterReorder()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A, B)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B, A)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@30 -> @27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterReorderAndUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A, B)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B, C)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@30 -> @27",
                "Update [A]@27 -> [C]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", "type parameter"),
                Diagnostic(RudeEditKind.Renamed, "C", "type parameter"))
        End Sub
#End Region

#Region "Type Type Parameter"

        <Fact>
        Public Sub TypeTypeParameterInsert1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C(Of A) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [(Of A)]@7",
                "Insert [A]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "(Of A)", "type parameters"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterInsert2()
            Dim src1 = "Class C(Of A) : End Class"
            Dim src2 = "Class C(Of A, B) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A)]@7 -> [(Of A, B)]@7",
                "Insert [B]@14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterDelete1()
            Dim src1 = "Class C(Of A) : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [(Of A)]@7",
                "Delete [A]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", "type parameters"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterDelete2()
            Dim src1 = "Class C(Of A, B) : End Class"
            Dim src2 = "Class C(Of B) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A, B)]@7 -> [(Of B)]@7",
                "Delete [A]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C(Of B)", "type parameter"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterUpdate()
            Dim src1 = "Class C(Of A) : End Class"
            Dim src2 = "Class C(Of B) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@11 -> [B]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterReorder()
            Dim src1 = "Class C(Of A, B) : End Class"
            Dim src2 = "Class C(Of B, A) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@14 -> @11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", "type parameter"))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterReorderAndUpdate()
            Dim src1 = "Class C(Of A, B) : End Class"
            Dim src2 = "Class C(Of B, C) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@14 -> @11",
                "Update [A]@11 -> [C]@14")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", "type parameter"),
                Diagnostic(RudeEditKind.Renamed, "C", "type parameter"))
        End Sub
#End Region

#Region "Type Parameter Constraints"

        <Fact>
        Public Sub TypeConstraintInsert()
            Dim src1 = "Class C(Of T) : End Class"
            Dim src2 = "Class C(Of T As Class) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As Class]@13",
                "Insert [Class]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Class", "type constraint"))
        End Sub

        <Fact>
        Public Sub TypeConstraintInsert2()
            Dim src1 = "Class C(Of S, T As Class) : End Class"
            Dim src2 = "Class C(Of S As New, T As Class) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As New]@13",
                "Insert [New]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As New", "type constraint"))
        End Sub

        <Fact>
        Public Sub TypeConstraintDelete1()
            Dim src1 = "Class C(Of S, T As Class) : End Class"
            Dim src2 = "Class C(Of S, T) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Class]@16",
                "Delete [Class]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "T", "type constraint"))
        End Sub

        <Fact>
        Public Sub TypeConstraintDelete2()
            Dim src1 = "Class C(Of S As New, T As Class) : End Class"
            Dim src2 = "Class C(Of S, T As Class) : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As New]@13",
                "Delete [New]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "S", "type constraint"))
        End Sub

        <Fact>
        Public Sub TypeConstraintUpdate1()
            Dim src1 = "Class C(Of S As Structure, T As Class) : End Class"
            Dim src2 = "Class C(Of S As Class, T As Structure) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Structure]@16 -> [Class]@16",
                "Update [Class]@32 -> [Structure]@28")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ConstraintKindUpdate, "Class", "Structure", "Class"),
                Diagnostic(RudeEditKind.ConstraintKindUpdate, "Structure", "Class", "Structure"))
        End Sub

        <Fact>
        Public Sub TypeConstraintUpdate2()
            Dim src1 = "Class C(Of S As New, T As Class) : End Class"
            Dim src2 = "Class C(Of S As {New, J}, T As {Class, I}) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As New]@13 -> [As {New, J}]@13",
                "Update [As Class]@23 -> [As {Class, I}]@28",
                "Insert [J]@22",
                "Insert [I]@39")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "J", "type constraint"),
                Diagnostic(RudeEditKind.Insert, "I", "type constraint"))
        End Sub

        <Fact>
        Public Sub TypeConstraintUpdate3()
            Dim src1 = "Class C(Of S As New, T As Class, U As I) : End Class"
            Dim src2 = "Class C(Of S As {New}, T As {Class}, U As {I}) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As New]@13 -> [As {New}]@13",
                "Update [As Class]@23 -> [As {Class}]@25",
                "Update [As I]@35 -> [As {I}]@39")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub TypeConstraintUpdate4()
            Dim src1 = "Class C(Of S As {I, J}) : End Class"
            Dim src2 = "Class C(Of S As {J, I}) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [J]@20 -> @17")

            edits.VerifyRudeDiagnostics()
        End Sub

#End Region

    End Class
End Namespace
