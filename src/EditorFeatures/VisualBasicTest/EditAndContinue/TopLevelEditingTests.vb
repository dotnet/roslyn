' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <UseExportProvider>
    Public Class TopLevelEditingTests
        Inherits EditingTestBase
#Region "Imports"

        <Fact>
        Public Sub ImportDelete1()
            Dim src1 = "
Imports System.Diagnostics
"
            Dim src2 As String = ""

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Delete [Imports System.Diagnostics]@2")
            Assert.IsType(Of ImportsStatementSyntax)(edits.Edits.First().OldNode)
            Assert.Equal(edits.Edits.First().NewNode, Nothing)
        End Sub

        <Fact>
        Public Sub ImportDelete2()
            Dim src1 = "
Imports D = System.Diagnostics
Imports <xmlns=""http://roslyn/default1"">
Imports System.Collections
Imports System.Collections.Generic
"

            Dim src2 = "
Imports System.Collections.Generic
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Delete [Imports D = System.Diagnostics]@2",
                "Delete [Imports <xmlns=""http://roslyn/default1"">]@34",
                "Delete [Imports System.Collections]@76")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ImportInsert()
            Dim src1 = "
Imports System.Collections.Generic
"

            Dim src2 = "
Imports D = System.Diagnostics
Imports <xmlns=""http://roslyn/default1"">
Imports System.Collections
Imports System.Collections.Generic
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Imports D = System.Diagnostics]@2",
                "Insert [Imports <xmlns=""http://roslyn/default1"">]@34",
                "Insert [Imports System.Collections]@76")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ImportUpdate1()
            Dim src1 = "
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
"

            Dim src2 = "
Imports System.Diagnostics
Imports X = System.Collections
Imports System.Collections.Generic
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Imports System.Collections]@30 -> [Imports X = System.Collections]@30")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact, WorkItem(51374, "https://github.com/dotnet/roslyn/issues/51374")>
        Public Sub ImportUpdate2()
            Dim src1 = "
Imports System.Diagnostics
Imports X1 = System.Collections
Imports <xmlns=""http://roslyn/default1"">
Imports System.Collections.Generic
"

            Dim src2 = "
Imports System.Diagnostics
Imports X2 = System.Collections
Imports <xmlns=""http://roslyn/default2"">
Imports System.Collections.Generic
"

            Dim edits = GetTopEdits(src1, src2)

            ' TODO: https://github.com/dotnet/roslyn/issues/51374
            ' Should be following:
            'edits.VerifyEdits(
            '    "Update [Imports X1 = System.Collections]@30 -> [Imports X2 = System.Collections]@30",
            '    "Update [Imports <xmlns=""http://roslyn/default1"">]@28 -> [Imports <xmlns=""http://roslyn/default2"">]@28")
            '
            'edits.VerifyRudeDiagnostics(
            '    Diagnostic(RudeEditKind.Update, "Imports X2 = System.Collections", VBFeaturesResources.import),
            '    Diagnostic(RudeEditKind.Update, "Imports <xmlns=""http://roslyn/default2"">", VBFeaturesResources.import))

            edits.VerifyEdits(
                "Update [Imports X1 = System.Collections]@30 -> [Imports X2 = System.Collections]@30")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ImportUpdate3()
            Dim src1 = "
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
"

            Dim src2 = "
Imports System
Imports System.Collections
Imports System.Collections.Generic
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Imports System.Diagnostics]@2 -> [Imports System]@2")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ImportReorder1()
            Dim src1 = "
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
"

            Dim src2 = "
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Imports System.Diagnostics]@2 -> @66")
        End Sub

        <Fact>
        Public Sub ImportInsert_WithNewCode()
            Dim src1 = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim src2 = "
Imports System

Class C
    Sub M()
        Console.WriteLine(1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.M"))
            })
        End Sub

        <Fact>
        Public Sub ImportDelete_WithOldCode()
            Dim src1 = "
Imports System

Class C
    Sub M()
        Console.WriteLine(1)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.M"))
            })
        End Sub
#End Region

#Region "Option"
        <Fact>
        Public Sub OptionDelete()
            Dim src1 = "
Option Strict On
"

            Dim src2 = "
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Option Strict On]@2")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, VBFeaturesResources.option_))
        End Sub

        <Fact>
        Public Sub OptionInsert()
            Dim src1 = "
"

            Dim src2 = "
Option Strict On
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Option Strict On]@2")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Option Strict On", VBFeaturesResources.option_))
        End Sub

        <Fact>
        Public Sub OptionUpdate()
            Dim src1 = "
Option Strict On
"

            Dim src2 = "
Option Strict Off
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Option Strict On]@2 -> [Option Strict Off]@2")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Option Strict Off", VBFeaturesResources.option_))
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
                Diagnostic(RudeEditKind.Update, "A2", FeaturesResources.attribute))
        End Sub

        <Fact>
        Public Sub UpdateAttributes2()
            Dim src1 = "<A(1)>Class C : End Class"
            Dim src2 = "<A(2)>Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A(1)]@1 -> [A(2)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute))
        End Sub

        <Fact>
        Public Sub UpdateAttributes_TopLevel1()
            Dim src1 = "<Assembly: A(1)>"
            Dim src2 = "<Assembly: A(2)>"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Assembly: A(1)]@1 -> [Assembly: A(2)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Assembly: A(2)", FeaturesResources.attribute))
        End Sub

        <Fact>
        Public Sub UpdateAttributes_TopLevel2()
            Dim src1 = "<Assembly: A(1)>"
            Dim src2 = "<Module: A(1)>"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Assembly: A(1)]@1 -> [Module: A(1)]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "Module: A(1)", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Delete, "Class C", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Delete, Nothing, VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "<Assembly: A1>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute))
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

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C"))
            })
        End Sub

        <Fact>
        Public Sub PartialInterfaceDelete()
            Dim src1 = "Partial Interface C : End Interface"
            Dim src2 = ""
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.interface_, "C")))
        End Sub

        <Fact>
        Public Sub ModuleInsert()
            Dim src1 = ""
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Module C", VBFeaturesResources.module_))
        End Sub

        <Fact>
        Public Sub PartialModuleInsert()
            Dim src1 = ""
            Dim src2 = "Partial Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Partial Module C", VBFeaturesResources.module_))
        End Sub

        <Fact>
        Public Sub PartialModuleDelete()
            Dim src1 = "Partial Module C : End Module"
            Dim src2 = ""
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(VBFeaturesResources.module_, "C")))
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
                Diagnostic(RudeEditKind.TypeKindUpdate, "Structure C", VBFeaturesResources.structure_))
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
                Diagnostic(RudeEditKind.TypeKindUpdate, "Public Class C", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub ModuleModifiersUpdate()
            Dim src1 = "Module C : End Module"
            Dim src2 = "Partial Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Module C]@0 -> [Partial Module C]@0")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub ModuleModifiersUpdate2()
            Dim src1 = "Partial Module C : End Module"
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Partial Module C]@0 -> [Module C]@0")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate()
            Dim src1 = "Public Interface C : End Interface"
            Dim src2 = "Interface C : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Interface C]@0 -> [Interface C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Interface C", FeaturesResources.interface_))
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate2()
            Dim src1 = "Public Interface C : Sub Goo() : End Interface"
            Dim src2 = "Interface C : Sub Goo() : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Interface C]@0 -> [Interface C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Interface C", FeaturesResources.interface_))
        End Sub

        <Fact>
        Public Sub InterfaceModifiersUpdate3()
            Dim src1 = "Interface C : Sub Goo() : End Interface"
            Dim src2 = "Partial Interface C : Sub Goo() : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Interface C]@0 -> [Partial Interface C]@0")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub StructModifiersUpdate()
            Dim src1 = "Structure C : End Structure"
            Dim src2 = "Public Structure C : End Structure"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Structure C]@0 -> [Public Structure C]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Public Structure C", VBFeaturesResources.structure_))
        End Sub

        <Fact>
        Public Sub ClassRename1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "class c : end class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits("Update [Class C]@0 -> [class c]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class c", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub ClassRename2()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class D : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C]@0 -> [Class D]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Class D", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub InterfaceNameReplace()
            Dim src1 = "Interface C : End Interface"
            Dim src2 = "Interface D : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Interface C]@0 -> [Interface D]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Interface D", FeaturesResources.interface_))
        End Sub

        <Fact>
        Public Sub StructNameReplace()
            Dim src1 = "Structure C : End Structure"
            Dim src2 = "Structure D : End Structure"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits("Update [Structure C]@0 -> [Structure D]@0")
            edits.VerifyRudeDiagnostics(Diagnostic(RudeEditKind.Renamed, "Structure D", VBFeaturesResources.structure_))
        End Sub

        <Fact>
        Public Sub ClassNameUpdate()
            Dim src1 = "Class LongerName : End Class"
            Dim src2 = "Class LongerMame : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class LongerName]@0 -> [Class LongerMame]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Class LongerMame", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub BaseTypeUpdate1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Inherits D : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : End Class]@0 -> [Class C : Inherits D : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub BaseTypeUpdate2()
            Dim src1 = "Class C : Inherits D1 : End Class"
            Dim src2 = "Class C : Inherits D2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Inherits D1 : End Class]@0 -> [Class C : Inherits D2 : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate1()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Implements IDisposable : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : End Class]@0 -> [Class C : Implements IDisposable : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate2()
            Dim src1 = "Class C : Implements IGoo, IBar : End Class"
            Dim src2 = "Class C : Implements IGoo : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Implements IGoo, IBar : End Class]@0 -> [Class C : Implements IGoo : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub BaseInterfaceUpdate3()
            Dim src1 = "Class C : Implements IGoo : Implements IBar : End Class"
            Dim src2 = "Class C : Implements IBar : Implements IGoo : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Class C : Implements IGoo : Implements IBar : End Class]@0 -> [Class C : Implements IBar : Implements IGoo : End Class]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class C", FeaturesResources.class_))
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

        <Fact>
        Public Sub Interface_InsertMembers()
            Dim src1 = "
Imports System

Interface I 
End Interface
"
            Dim src2 = "
Imports System

Interface I
    Sub VirtualMethod()
    Property VirtualProperty() As String
    Property VirtualIndexer(a As Integer) As String
    Event VirtualEvent As Action

    MustInherit Class C
    End Class

    Interface J
    End Interface

    Enum E
        A
    End Enum

    Delegate Sub D()
End Interface
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "Sub VirtualMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "Property VirtualProperty()", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "Property VirtualIndexer(a As Integer)", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "Event VirtualEvent", FeaturesResources.event_))
        End Sub

        <Fact>
        Public Sub Interface_InsertDelete()
            Dim srcA1 = "
Interface I
    Sub VirtualMethod()
    Function VirtualFunction() As Integer
    Property VirtualProperty() As String
    ReadOnly Property VirtualReadonlyProperty() As String
    Property VirtualIndexer(a As Integer) As String
    Event VirtualEvent As Action

    MustInherit Class C
    End Class

    Interface J
    End Interface

    Enum E
        A
    End Enum

    Delegate Sub D()
End Interface
"
            Dim srcB1 = "
"

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub GenericType_InsertMembers()
            Dim src1 = "
Imports System
Class C(Of T)
End Class
"
            Dim src2 = "
Imports System
Class C(Of T)
    Dim F1, F2 As New Object, F3 As Integer, F4 As New Object, F5(1, 2), F6? As Integer

    Sub M()
    End Sub

    Property P1(i As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property P2 As Integer
    Property P3 As New Object

    Event E1(sender As Object, e As EventArgs)

    Event E2 As Action

    Custom Event E3 As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event

    Dim WithEvents WE As Object

    Enum N
        A
    End Enum

    Interface I
    End Interface

    Class D
    End Class

    Delegate Sub G()
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Sub M()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Property P1(i As Integer)", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Property P2", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Property P3", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Event E1(sender As Object, e As EventArgs)", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Event E2", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "Event E3", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F1", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F2", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F3 As Integer", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F4 As New Object", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F5(1, 2)", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F6?", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "WE As Object", VBFeaturesResources.WithEvents_field))
        End Sub

        <Fact>
        Public Sub Type_Delete()
            Dim src1 = "
Class C
  Sub F()
  End Sub
End Class

Module M
  Sub F()
  End Sub
End Module

Structure S
  Sub F()
  End Sub
End Structure

Interface I
  Sub F()
End Interface

Delegate Sub D()
"
            Dim src2 = ""

            GetTopEdits(src1, src2).VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.class_, "C")),
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(VBFeaturesResources.module_, "M")),
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(VBFeaturesResources.structure_, "S")),
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.interface_, "I")),
                Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.delegate_, "D")))
        End Sub

        <Fact>
        Public Sub PartialType_Delete()
            Dim srcA1 = "
Partial Class C
  Sub F()
  End Sub
  Sub M()
  End Sub
End Class"
            Dim srcB1 = "
Partial Class C
  Sub G()
  End Sub
End Class
"
            Dim srcA2 = ""
            Dim srcB2 = "
Partial Class C
  Sub G()
  End Sub
  Sub M()
  End Sub
End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Delete, Nothing, DeletedSymbolDisplay(FeaturesResources.method, "C.F()"))}),
                    DocumentResults(
                        semanticEdits:={
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("M"))
                        })
                })
        End Sub

        <Fact>
        Public Sub PartialType_InsertFirstDeclaration()
            Dim src1 = ""
            Dim src2 = "
Partial Class C
  Sub F()
  End Sub
End Class"

            GetTopEdits(src1, src2).VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C"), preserveLocalVariables:=False)})
        End Sub

        <Fact>
        Public Sub PartialType_InsertSecondDeclaration()
            Dim srcA1 = "
Partial Class C
  Sub F()
  End Sub
End Class"
            Dim srcB1 = ""

            Dim srcA2 = "
Partial Class C
  Sub F()
  End Sub
End Class"
            Dim srcB2 = "
Partial Class C
  Sub G()
  End Sub
End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("G"), preserveLocalVariables:=False)
                        })
                })
        End Sub

        <Fact>
        Public Sub Type_DeleteInsert()
            Dim srcA1 = "
Class C
  Sub F()
  End Sub
End Class

Module M
  Sub F()
  End Sub
End Module

Structure S
  Sub F()
  End Sub
End Structure

Interface I
  Sub F()
End Interface

Delegate Sub D()
"
            Dim srcB1 = ""

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("M").GetMember("F")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("S").GetMember("F"))
                        })
                })
        End Sub

        <Fact>
        Public Sub GenericType_DeleteInsert()
            Dim srcA1 = "
Class C(Of T)
  Sub F()
  End Sub
End Class

Structure S(Of T)
  Sub F()
  End Sub
End Structure

Interface I(Of T)
  Sub F()
End Interface
"
            Dim srcB1 = ""

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:=
                        {
                            Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "Sub F()", FeaturesResources.method),
                            Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "Sub F()", FeaturesResources.method),
                            Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "Sub F()", FeaturesResources.method)
                        })
                })
        End Sub

        <Fact>
        Public Sub Type_DeleteInsert_NonInsertableMembers()
            Dim srcA1 = "
MustInherit Class C
    Implements I
    Public MustOverride Sub AbstractMethod()

    Public Overridable Sub VirtualMethod()
    End Sub

    Public Overrides Function ToString() As String
        Return Nothing
    End Function

    Public Sub IG() Implements I.G
    End Sub
End Class

Interface I
    Sub G()
End Interface
"
            Dim srcB1 = ""

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("VirtualMethod")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("ToString")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("IG"))
                        })
                })
        End Sub

        <Fact>
        Public Sub Type_DeleteInsert_DataMembers()
            Dim srcA1 = "
Class C
    Dim F1 = 1, F2 As New Object, F3 As Integer, F4 As New Object, F5(1, 2), F6? As Integer
End Class
"
            Dim srcB1 = ""

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        })
                })
        End Sub

        <Fact>
        Public Sub Type_DeleteInsert_DataMembers_PartialSplit()
            Dim srcA1 = "
Class C
    Public x = 1
    Public y = 2
    Public z = 2
End Class
"
            Dim srcB1 = ""

            Dim srcA2 = "
Class C
    Public x = 1
    Public y = 2
End Class
"
            Dim srcB2 = "
Partial Class C
    Public z = 3
End Class
"
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        })
                })
        End sub

        <Fact>
        Public Sub Type_DeleteInsert_DataMembers_PartialMerge()
            Dim srcA1 = "
Partial Class C
    Public x = 1
    Public y = 2
End Class
"
            Dim srcB1 = "
Class C
    Public z = 1
End Class
"

            Dim srcA2 = "
Class C
    Public x = 1
    Public y = 2
    Public z = 2
End Class
"

            Dim srcB2 = "
"
            ' note that accessors are not updated since they do not have bodies
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        }),
                    DocumentResults()
                })
        End sub
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
                Diagnostic(RudeEditKind.Renamed, "Enum Colors", FeaturesResources.enum_))
        End Sub

        <Fact>
        Public Sub Enum_Modifiers_Update()
            Dim src1 = "Public Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Enum Color]@0 -> [Enum Color]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Enum Color", FeaturesResources.enum_))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Insert()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As UShort]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As UShort", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Update()
            Dim src1 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color As Long : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As UShort]@11 -> [As Long]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Enum Color", FeaturesResources.enum_))
        End Sub

        <Fact>
        Public Sub Enum_BaseType_Delete()
            Dim src1 = "Enum Color As UShort : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                 "Delete [As UShort]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Enum Color", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Enum_Attribute_Insert()
            Dim src1 = "Enum E : End Enum"
            Dim src2 = "<A>Enum E : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@0", "Insert [A]@1")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Delete()
            Dim src1 = "Enum E : <A>X : End Enum"
            Dim src2 = "Enum E : X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<A>]@9", "Delete [A]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "X", VBFeaturesResources.attributes))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Insert()
            Dim src1 = "Enum E : X : End Enum"
            Dim src2 = "Enum E : <A>X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<A>]@9", "Insert [A]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
        End Sub

        <Fact>
        Public Sub Enum_MemberAttribute_Update()
            Dim src1 = "Enum E : <A1>X : End Enum"
            Dim src2 = "Enum E : <A2>X : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A1]@10 -> [A2]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A2", FeaturesResources.attribute))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Update1()
            Dim src1 = "Enum Color : Red = 1 : Blue = 2 : End Enum"
            Dim src2 = "Enum Color : Red = 1 : Blue = 3 : End Enum"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Blue = 2]@23 -> [Blue = 3]@23")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 3", FeaturesResources.enum_value))
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
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1 << 0", FeaturesResources.enum_value),
                Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 2 << 1", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Update3()
            Dim src1 = "Enum Color : Red = int.MinValue : End Enum"
            Dim src2 = "Enum Color : Red = int.MaxValue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red = int.MinValue]@13 -> [Red = int.MaxValue]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = int.MaxValue", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Insert()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red = 1 : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red]@13 -> [Red = 1]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_MemberInitializer_Delete()
            Dim src1 = "Enum Color : Red = 1 : End Enum"
            Dim src2 = "Enum Color : Red : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red = 1]@13 -> [Red]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Red", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_Member_Insert1()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red : Blue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_Member_Insert2()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Red : Blue : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_Member_Update()
            Dim src1 = "Enum Color : Red : End Enum"
            Dim src2 = "Enum Color : Orange : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Red]@13 -> [Orange]@13")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Orange", FeaturesResources.enum_value))
        End Sub

        <Fact>
        Public Sub Enum_Member_Delete()
            Dim src1 = "Enum Color : Red : Blue : End Enum"
            Dim src2 = "Enum Color : Red : End Enum"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Blue]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Enum Color", DeletedSymbolDisplay(FeaturesResources.enum_value, "Blue")))
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
                Diagnostic(RudeEditKind.Renamed, "Public Delegate Sub Z()", FeaturesResources.delegate_))
        End Sub

        <Fact>
        Public Sub Delegates_Update_Modifiers()
            Dim src1 = "Public Delegate Sub D()"
            Dim src2 = "Private Delegate Sub D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Sub D()]@0 -> [Private Delegate Sub D()]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Private Delegate Sub D()", FeaturesResources.delegate_))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType1()
            Dim src1 = "Public Delegate Function D()"
            Dim src2 = "Public Delegate Sub D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Public Delegate Function D()]@0 -> [Public Delegate Sub D()]@0")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Public Delegate Sub D()", FeaturesResources.delegate_))
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
                Diagnostic(RudeEditKind.TypeUpdate, "Public Delegate Sub D()", FeaturesResources.delegate_),
                Diagnostic(RudeEditKind.Delete, "Public Delegate Sub D()", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType3()
            Dim src1 = "Public Delegate Function D()"
            Dim src2 = "Public Delegate Function D() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Integer", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Delegates_Update_ReturnType4()
            Dim src1 = "Public Delegate Function D() As Integer"
            Dim src2 = "Public Delegate Function D()"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", VBFeaturesResources.as_clause))
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
                Diagnostic(RudeEditKind.Insert, "a As Integer", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Rename()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D(b As Integer) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@27 -> [b]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_Update()
            Dim src1 = "Public Delegate Function D(a As Integer) As Integer"
            Dim src2 = "Public Delegate Function D(a As Byte) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@29 -> [As Byte]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Byte", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub Delegates_Parameter_UpdateModifier()
            Dim src1 = "Public Delegate Function D(a As Integer()) As Integer"
            Dim src2 = "Public Delegate Function D(ParamArray a As Integer()) As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer()]@27 -> [ParamArray a As Integer()]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "ParamArray a As Integer()", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "(Of T)", VBFeaturesResources.type_parameters))
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
                Diagnostic(RudeEditKind.Delete, "Public Delegate Function D()", VBFeaturesResources.type_parameters))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Rename()
            Dim src1 = "Public Delegate Function D(Of T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of S)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [T]@30 -> [S]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "S", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance1()
            Dim src1 = "Public Delegate Function D(Of T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of In T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [T]@30 -> [In T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance2()
            Dim src1 = "Public Delegate Function D(Of Out T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Out T]@30 -> [T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub Delegates_TypeParameter_Variance3()
            Dim src1 = "Public Delegate Function D(Of Out T)() As Integer"
            Dim src2 = "Public Delegate Function D(Of In T)() As Integer"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Out T]@30 -> [In T]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Move, "Class D", FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub NestedClass_ClassMove2()
            Dim src1 = "Class C : Class D : End Class : Class E : End Class : Class F : End Class : End Class"
            Dim src2 = "Class C : Class D : End Class : Class F : End Class : End CLass : Class E : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Class E : End Class]@32 -> @66")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Class E", FeaturesResources.class_))
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
                Diagnostic(RudeEditKind.Move, "Class D", FeaturesResources.class_))
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
            Dim src2 = "Public Class C : Private Class D : Public Property P As New Object : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.D"), preserveLocalVariables:=False)})
        End Sub

        <Fact>
        Public Sub NestedClass_InsertMemberWithInitializer2()
            Dim src1 = "Public Module C : End Module"
            Dim src2 = "Public Module C : Private Class D : Property P As New Object : End Class : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.D"), preserveLocalVariables:=False)})
        End Sub

        <WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Syntactic()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
"

            Dim src2 = "
Imports System
Imports System.Runtime.InteropServices

Class C
    Private MustInherit Class D 
        Declare Ansi Function A Lib ""A"" () As Integer
        Declare Ansi Sub B Lib ""B"" ()
    End Class
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "Declare Ansi Function A Lib ""A"" ()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertDllImport, "Declare Ansi Sub B Lib ""B"" ()", FeaturesResources.method))
        End Sub

        <WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Semantic1()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
End Class"

            Dim src2 = "
Imports System
Imports System.Runtime.InteropServices

Class C
    Private MustInherit Class D 
        <DllImport(""msvcrt.dll"")>
        Public Shared Function puts(c As String) As Integer
        End Function

        <DllImport(""msvcrt.dll"")>
        Public Shared Operator +(d As D, g As D) As Integer
        End Operator

        <DllImport(""msvcrt.dll"")>
        Public Shared Narrowing Operator CType(d As D) As Integer
        End Operator
    End Class
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                diagnostics:=
                {
                    Diagnostic(RudeEditKind.InsertDllImport, "Public Shared Function puts(c As String)", FeaturesResources.method),
                    Diagnostic(RudeEditKind.InsertDllImport, "Public Shared Operator +(d As D, g As D)", FeaturesResources.operator_),
                    Diagnostic(RudeEditKind.InsertDllImport, "Public Shared Narrowing Operator CType(d As D)", FeaturesResources.operator_)
                },
                targetFrameworks:={TargetFramework.NetStandard20})
        End Sub

        <WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")>
        <Fact>
        Public Sub NestedClass_Insert_PInvoke_Semantic2()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
End Class"

            Dim src2 = "
Imports System
Imports System.Runtime.InteropServices

Class C
    <DllImport(""msvcrt.dll"")>
    Private Shared Function puts(c As String) As Integer
    End Function
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                diagnostics:=
                {
                    Diagnostic(RudeEditKind.InsertDllImport, "Private Shared Function puts(c As String)", FeaturesResources.method)
                },
                targetFrameworks:={TargetFramework.NetStandard20})
        End Sub

        <WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")>
        <Fact>
        Public Sub NestedClass_Insert_VirtualAbstract()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
"

            Dim src2 = "
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
"
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
            Dim src1 = "Public Class C" & vbLf & "Public Sub goo() : End Sub : End Class"
            Dim src2 = "Public Class C : Private Class D" & vbLf & "Public Sub goo() : End Sub : End Class : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Class D" & vbLf & "Public Sub goo() : End Sub : End Class]@17",
                "Insert [Private Class D]@17",
                "Insert [Public Sub goo() : End Sub]@33",
                "Insert [Public Sub goo()]@33",
                "Insert [()]@47",
                "Delete [Public Sub goo() : End Sub]@15",
                "Delete [Public Sub goo()]@15",
                "Delete [()]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Class C", DeletedSymbolDisplay(FeaturesResources.method, "goo()")))
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
                Diagnostic(RudeEditKind.Move, "Public Class X", FeaturesResources.class_))
        End Sub

        ''' <summary>
        ''' A new generic type can be added whether it's nested and inherits generic parameters from the containing type, or top-level.
        ''' </summary>
        <Fact>
        public Sub NestedClassGeneric_Insert()
            Dim src1 = "
Imports System
Class C(Of T)
End Class
"
            Dim src2 = "
Imports System
Class C(Of T)
    Class C
    End Class

    Structure S
    End Structure

    Enum N
        A
    End Enum

    Interface I
    End Interface

    Class D
    End Class

    Delegate Sub G()
End Class

Class D(Of T)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub NestedEnum_InsertMember()
            Dim src1 = "
Structure S
  Enum N
    A = 1
  End Enum
End Structure
"
            Dim src2 = "
Structure S
  Enum N
    A = 1
    B = 2
  End Enum
End Structure
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [B = 2]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value))
        End Sub

        <Fact, WorkItem(50876, "https://github.com/dotnet/roslyn/issues/50876")>
        Public Sub NestedEnumInPartialType_InsertDelete()
            Dim srcA1 = "Partial Structure S : End Structure"
            Dim srcB1 = "Partial Structure S : Enum N : A = 1 : End Enum" + vbCrLf + "End Structure"
            Dim srcA2 = "Partial Structure S : Enum N : A = 1 : End Enum" + vbCrLf + "End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults()
                })
        End Sub

        <Fact, WorkItem(50876, "https://github.com/dotnet/roslyn/issues/50876")>
        Public Sub NestedEnumInPartialType_InsertDeleteAndUpdateMember()
            Dim srcA1 = "Partial Structure S : End Structure"
            Dim srcB1 = "Partial Structure S : Enum N : A = 1 : End Enum" + vbCrLf + "End Structure"
            Dim srcA2 = "Partial Structure S : Enum N : A = 2 : End Enum" + vbCrLf + "End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:=
                        {
                            Diagnostic(RudeEditKind.InitializerUpdate, "A = 2", FeaturesResources.enum_value)
                        }),
                    DocumentResults()
                })
        End Sub

        <Fact, WorkItem(50876, "https://github.com/dotnet/roslyn/issues/50876")>
        Public Sub NestedEnumInPartialType_InsertDeleteAndInsertMember()
            Dim srcA1 = "Partial Structure S : End Structure"
            Dim srcB1 = "Partial Structure S : Enum N : A = 1 : End Enum" + vbCrLf + "End Structure"
            Dim srcA2 = "Partial Structure S : Enum N : A = 1 : B = 2 : End Enum" + vbCrLf + "End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value)}),
                    DocumentResults()
                })
        End Sub

        <Fact>
        public sub NestedDelegateInPartialType_InsertDelete()
            Dim srcA1 = "Partial Structure S : End Structure"
            Dim srcB1 = "Partial Structure S : Delegate Sub D()" + vbCrLf + "End Structure"
            Dim srcA2 = "Partial Structure S : Delegate Sub D()" + vbCrLf + "End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"

            ' delegate does not have any user-defined method body and this does not need a PDB update
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        ),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub NestedDelegateInPartialType_InsertDeleteAndChangeSignature()
            Dim srcA1 = "Partial Structure S : End Structure"
            Dim srcB1 = "Partial Structure S : Delegate Sub D()" + vbCrLf + "End Structure"
            Dim srcA2 = "Partial Structure S : Delegate Sub D(a As Integer)" + vbCrLf + "End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:=
                        {
                            Diagnostic(RudeEditKind.Insert, "a As Integer", FeaturesResources.parameter)
                        }),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteAndChange()
            Dim srcA1 = "Partial Structure S : Partial Class C" + vbCrLf + "Sub F1() : End Sub : End Class : End Structure"
            Dim srcB1 = "Partial Structure S : Partial Class C" + vbCrLf + "Sub F2(x As Byte) : End Sub : End Class : End Structure"
            Dim srcC1 = "Partial Structure S : End Structure"

            Dim srcA2 = "Partial Structure S : Partial Class C" + vbCrLf + "Sub F1() : End Sub : End Class : End Structure"
            Dim srcB2 = "Partial Structure S : End Structure"
            Dim srcC2 = "Partial Structure S : Partial Class C" + vbCrLf + "Sub F2(x As Integer) : End Sub : End Class : End Structure"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Delete, "Partial Structure S", DeletedSymbolDisplay(FeaturesResources.method, "F2(Byte)"))}),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("S").GetMember(Of NamedTypeSymbol)("C").GetMember("F2"))})
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteAndChange_BaseType()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = ""
            Dim srcC1 = "Partial Class C : End Class"

            Dim srcA2 = ""
            Dim srcB2 = "Partial Class C : Inherits D" + vbCrLf + "End Class"
            Dim srcC2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Partial Class C", FeaturesResources.class_)}),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteAndChange_Attribute()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = ""
            Dim srcC1 = "Partial Class C : End Class"

            Dim srcA2 = ""
            Dim srcB2 = "<A>Partial Class C : End Class"
            Dim srcC2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Update, "Partial Class C", FeaturesResources.class_)}),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteAndChange_Constraint()
            Dim srcA1 = "Partial Class C(Of T) : End Class"
            Dim srcB1 = ""
            Dim srcC1 = "Partial Class C(Of T) : End Class"

            Dim srcA2 = ""
            Dim srcB2 = "Partial Class C(Of T As New) : End Class"
            Dim srcC2 = "Partial Class C(Of T) : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Update, "Partial Class C(Of T As New)", FeaturesResources.class_)}),
                    DocumentResults()
                })
        End Sub

        ''' <summary>
        ''' Moves partial classes to different files while moving around their attributes and base interfaces.
        ''' </summary>
        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteRefactor()
            Dim srcA1 = "Partial Class C : Implements I" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcB1 = "<A><B>Partial Class C : Implements J" + vbCrLf + "Sub G() : End Sub : End Class"
            Dim srcC1 = ""
            Dim srcD1 = ""

            Dim srcA2 = ""
            Dim srcB2 = ""
            Dim srcC2 = "<A>Partial Class C : Implements I, J" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcD2 = "<B>Partial Class C" + vbCrLf + "Sub G() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F"))}),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("G"))})
                })
        End Sub

        ''' <summary>
        ''' Moves partial classes to different files while moving around their attributes and base interfaces.
        ''' Currently we do not support splitting attribute lists.
        ''' </summary>
        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteRefactor_AttributeListSplitting()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcB1 = "<A,B>Partial Class C" + vbCrLf + "Sub G() : End Sub : End Class"
            Dim srcC1 = ""
            Dim srcD1 = ""

            Dim srcA2 = ""
            Dim srcB2 = ""
            Dim srcC2 = "<A>Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcD2 = "<B>Partial Class C" + vbCrLf + "Sub G() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(diagnostics:={Diagnostic(RudeEditKind.Update, "Partial Class C", FeaturesResources.class_)}),
                    DocumentResults(diagnostics:={Diagnostic(RudeEditKind.Update, "Partial Class C", FeaturesResources.class_)})
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteChangeMember()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Sub F(Optional y As Integer = 1) : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Sub G(Optional x As Integer = 1) : End Sub : End Class"
            Dim srcC1 = ""

            Dim srcA2 = ""
            Dim srcB2 = "Partial Class C" + vbCrLf + "Sub G(Optional x As Integer = 2) : End Sub : End Class"
            Dim srcC2 = "Partial Class C" + vbCrLf + "Sub F(Optional y As Integer = 2) : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:={Diagnostic(RudeEditKind.InitializerUpdate, "Optional x As Integer = 2", FeaturesResources.parameter)}),
                    DocumentResults(diagnostics:={Diagnostic(RudeEditKind.InitializerUpdate, "Optional y As Integer = 2", FeaturesResources.parameter)})
                })
        End Sub

        <Fact>
        Public Sub NestedPartialTypeInPartialType_InsertDeleteAndInsertVirtual()
            Dim srcA1 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub F1()" + vbCrLf + "End Sub : End Class : End Interface"
            Dim srcB1 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub F2()" + vbCrLf + "End Sub : End Class : End Interface"
            Dim srcC1 = "Partial Interface I : Partial Class C" + vbCrLf + "End Class : End Interface"
            Dim srcD1 = "Partial Interface I : Partial Class C" + vbCrLf + "End Class : End Interface"
            Dim srcE1 = "Partial Interface I : End Interface"
            Dim srcF1 = "Partial Interface I : End Interface"

            Dim srcA2 = "Partial Interface I : Partial Class C" + vbCrLf + "End Class : End Interface"
            Dim srcB2 = ""
            Dim srcC2 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub F1()" + vbCrLf + "End Sub : End Class : End Interface" ' move existing virtual into existing partial decl
            Dim srcD2 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub N1()" + vbCrLf + "End Sub : End Class : End Interface" ' insert new virtual into existing partial decl
            Dim srcE2 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub F2()" + vbCrLf + "End Sub : End Class : End Interface" ' move existing virtual into a new partial decl
            Dim srcF2 = "Partial Interface I : Partial Class C" + vbCrLf + "Overridable Sub N2()" + vbCrLf + "End Sub : End Class : End Interface" ' insert new virtual into new partial decl

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2), GetTopEdits(srcE1, srcE2), GetTopEdits(srcF1, srcF2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("I").GetMember(Of NamedTypeSymbol)("C").GetMember("F1"))}),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.InsertVirtual, "Overridable Sub N1()", FeaturesResources.method)}),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("I").GetMember(Of NamedTypeSymbol)("C").GetMember("F2"))}),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.InsertVirtual, "Overridable Sub N2()", FeaturesResources.method)})
                })
        End Sub

#End Region

#Region "Namespaces"
        <Fact>
        Public Sub NamespaceInsert()
            Dim src1 = ""
            Dim src2 = "Namespace C : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Namespace C", FeaturesResources.namespace_))
        End Sub

        <Fact>
        Public Sub NamespaceDelete()
            Dim src1 = "Namespace C : End Namespace"
            Dim src2 = ""
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, Nothing, FeaturesResources.namespace_))
        End Sub

        <Fact>
        Public Sub NamespaceMove1()
            Dim src1 = "Namespace C : Namespace D : End Namespace : End Namespace"
            Dim src2 = "Namespace C : End Namespace : Namespace D : End Namespace"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Move [Namespace D : End Namespace]@14 -> @30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Namespace D", FeaturesResources.namespace_))
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

#Region "Members"

        <Fact>
        Public Sub PartialMember_DeleteInsert()
            Dim srcA1 = "
Imports System

Partial Class C
    Dim F1 = 1, F2 As New Object, F3 As Integer, F4 As New Object, F5(1, 2), F6? As Integer

    Sub M()
    End Sub

    Property P1(i As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property P2 As Integer
    Property P3 As New Object

    Event E1(sender As Object, e As EventArgs)

    Event E2 As Action

    Custom Event E3 As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event

    Dim WithEvents WE As Object
End Class
"
            Dim srcB1 = "
Imports System

Partial Class C
End Class
"
            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P1").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P1").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E3").AddMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E3").RemoveMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E3").RaiseMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_InsertDelete_MultipleDocuments()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcA2 = "Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F"), preserveLocalVariables:=False)
                        }),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_MultipleDocuments()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"
            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Sub F() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F"), preserveLocalVariables:=False)
                        })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_GenericMethod()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Sub F(Of T)() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"
            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Sub F(Of T)() : End Sub : End Class"

            ' TODO better message
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:=
                    {
                        Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "Sub F(Of T)()", FeaturesResources.method)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_GenericType()
            Dim srcA1 = "Partial Class C(Of T)" + vbCrLf + "Sub F(Of T)() : End Sub : End Class"
            Dim srcB1 = "Partial Class C(Of T) : End Class"
            Dim srcA2 = "Partial Class C(Of T) : End Class"
            Dim srcB2 = "Partial Class C(Of T)" + vbCrLf + "Sub F(Of T)() : End Sub : End Class"

            ' TODO better message
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:=
                    {
                        Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "Sub F(Of T)()", FeaturesResources.method)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialNestedType_InsertDeleteAndChange()
            Dim srcA1 = "
Partial Class C 
End Class
"
            Dim srcB1 = "
Partial Class C
    Class D
        Sub M()
        End Sub
    End Class

    Interface I 
    End Interface 
End Class"

            Dim srcA2 = "
Partial Class C
    Class D
        Implements I

        Sub M()
        End Sub
    End Class

    Interface I 
    End Interface 
End Class"

            Dim srcB2 = "
Partial Class C 
End Class
"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:=
                        {
                            Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "Class D", FeaturesResources.class_)
                        }),
                    DocumentResults()
                })
        End Sub

        <Fact, WorkItem(51011, "https://github.com/dotnet/roslyn/issues/51011")>
        Public Sub PartialMember_RenameInsertDelete()
            ' The syntactic analysis for A And B produce rename edits since it  doesn't see that the member was in fact moved.
            ' TODO: Currently, we don't even pass rename edits to semantic analysis where we could handle them as updates.

            Dim srcA1 = "Partial Class C" + vbCrLf + "Sub F1() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Sub F2() : End Sub : End Class"
            Dim srcA2 = "Partial Class C" + vbCrLf + "Sub F2() : End Sub : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Sub F1() : End Sub : End Class"

            ' current outcome
            GetTopEdits(srcA1, srcA2).VerifyRudeDiagnostics(Diagnostic(RudeEditKind.Renamed, "Sub F2()", FeaturesResources.method))
            GetTopEdits(srcB1, srcB2).VerifyRudeDiagnostics(Diagnostic(RudeEditKind.Renamed, "Sub F1()", FeaturesResources.method))

            ' correct outcome
            'EditAndContinueValidation.VerifySemantics(
            '     { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
            '    
            '    {
            '        DocumentResults(semanticEdits:=
            '            {
            '                SemanticEdit(SemanticEditKind.Update, c => c.GetMember(Of NamedTypeSymbol)("C").GetMember("F2")),
            '            }),
            '
            '        DocumentResults(
            '            semanticEdits:=
            '            {
            '                SemanticEdit(SemanticEditKind.Update, c => c.GetMember(Of NamedTypeSymbol)("C").GetMember("F1")),
            '            })
            '    });
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_UpdateMethodBodyError()
            Dim srcA1 = "
Imports System.Collections.Generic

Partial Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
"
            Dim srcB1 = "
Imports System.Collections.Generic

Partial Class C
End Class
"

            Dim srcA2 = "
Imports System.Collections.Generic

Partial Class C
End Class
"
            Dim srcB2 = "
Imports System.Collections.Generic

Partial Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function
End Class
"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:=
                    {
                        Diagnostic(RudeEditKind.Insert, "Yield 2", VBFeaturesResources.Yield_statement)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_UpdatePropertyAccessors()
            Dim srcA1 = "
Partial Class C
    Property P As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
            Console.WriteLine(1)
        End Set
    End Property
End Class
"
            Dim srcB1 = "Partial Class C: End Class"

            Dim srcA2 = "Partial Class C: End Class"
            Dim srcB2 = "
Partial Class C
    Property P As Integer
        Get
            Return 2
        End Get
        Set(value As Integer)
            Console.WriteLine(2)
        End Set
    End Property
End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P").SetMethod)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_UpdateAutoProperty()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Property P As Integer = 1 : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Property P As Integer = 2 : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_AddFieldInitializer()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Dim P As Integer : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Dim P As Integer = 1 : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_RemoveFieldInitializer()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Dim P As Integer = 1 : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Dim P As Integer : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
        End Sub

        <Fact>
        Public Sub PartialMember_DeleteInsert_ConstructorWithInitializers()
            Dim srcA1 = "
Partial Class C
    Dim F = 1

    Sub New(x As Integer)
        F = x
    End Sub
End Class"
            Dim srcB1 = "
Partial Class C
End Class"

            Dim srcA2 = "
Partial Class C
    Dim F = 1
End Class"
            Dim srcB2 = "
Partial Class C
    Sub New(x As Integer)
        F = x + 1
    End Sub
End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
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
            Dim src1 As String = "Class C" & vbLf & "Sub Goo() : End Sub : End Class"
            Dim src2 As String = "Class C" & vbLf & "Function Goo() : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo() : End Sub]@8 -> [Function Goo() : End Function]@8",
                "Update [Sub Goo()]@8 -> [Function Goo()]@8")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodKindUpdate, "Function Goo()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub InterfaceMethodUpdate1()
            Dim src1 As String = "Interface I" & vbLf & "Sub Goo() : End Interface"
            Dim src2 As String = "Interface I" & vbLf & "Function Goo() : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo()]@12 -> [Function Goo()]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodKindUpdate, "Function Goo()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub InterfaceMethodUpdate2()
            Dim src1 As String = "Interface I" & vbLf & "Sub Goo() : End Interface"
            Dim src2 As String = "Interface I" & vbLf & "Sub Goo(a As Boolean) : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "a As Boolean", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub MethodDelete()
            Dim src1 As String = "Class C" & vbLf & "Sub goo() : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Sub goo() : End Sub]@8",
                "Delete [Sub goo()]@8",
                "Delete [()]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.method, "goo()")))
        End Sub

        <Fact>
        Public Sub InterfaceMethodDelete()
            Dim src1 As String = "Interface C" & vbLf & "Sub Goo() : End Interface"
            Dim src2 As String = "Interface C : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Interface C", DeletedSymbolDisplay(FeaturesResources.method, "Goo()")))
        End Sub

        <Fact>
        Public Sub MethodDelete_WithParameters()
            Dim src1 As String = "Class C" & vbLf & "Sub goo(a As Integer) : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [Sub goo(a As Integer) : End Sub]@8",
                "Delete [Sub goo(a As Integer)]@8",
                "Delete [(a As Integer)]@15",
                "Delete [a As Integer]@16",
                "Delete [a]@16",
                "Delete [As Integer]@18")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.method, "goo(Integer)")))
        End Sub

        <Fact>
        Public Sub MethodDelete_WithAttribute()
            Dim src1 As String = "Class C : " & vbLf & "<Obsolete> Sub goo(a As Integer) : End Sub : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [<Obsolete> Sub goo(a As Integer) : End Sub]@11",
                "Delete [<Obsolete> Sub goo(a As Integer)]@11",
                "Delete [<Obsolete>]@11",
                "Delete [Obsolete]@12",
                "Delete [(a As Integer)]@29",
                "Delete [a As Integer]@30",
                "Delete [a]@30",
                "Delete [As Integer]@32")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.method, "goo(Integer)")))
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

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.F"))})
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
            Dim src2 = "Class C : " & vbLf & "<System.Obsolete>Private Sub F : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [<System.Obsolete>Private Sub F : End Sub]@11",
                "Insert [<System.Obsolete>Private Sub F]@11",
                "Insert [<System.Obsolete>]@11",
                "Insert [System.Obsolete]@12")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F"))})
        End Sub

        <Fact>
        Public Sub MethodInsert_Overridable()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Overridable Sub F : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "Overridable Sub F", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodInsert_MustOverride()
            Dim src1 = "MustInherit Class C : End Class"
            Dim src2 = "MustInherit Class C : " & vbLf & "MustOverride Sub F : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "MustOverride Sub F", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodInsert_Overrides()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & vbLf & "Overrides Sub F : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "Overrides Sub F", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub ExternMethodDeleteInsert()
            Dim srcA1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
    <DllImport(""msvcrt.dll"")>
    Public Shared Function puts(c As String) As Integer
    End Function
End Class"
            Dim srcA2 = "
Imports System
Imports System.Runtime.InteropServices
"

            Dim srcB1 = srcA2
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("puts"))
                    })
                })
        End Sub

        <Fact>
        Public Sub Method_Reorder1()
            Dim src1 = "Class C : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : " & vbLf & "Sub g() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Sub g() : End Sub : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "a = b : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Sub g() : End Sub]@64 -> @11")

            edits.VerifySemantics(ActiveStatementsDescription.Empty, Array.Empty(Of SemanticEditDescription)())
        End Sub

        <Fact>
        Public Sub InterfaceMethod_Reorder1()
            Dim src1 = "Interface I : " & vbLf & "Sub f(a As Integer, b As Integer)" & vbLf & "Sub g() : End Interface"
            Dim src2 = "Interface I : " & vbLf & "Sub g() : " & vbLf & "Sub f(a As Integer, b As Integer) : End Interface"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Sub g()]@49 -> @15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Sub g()", FeaturesResources.method))
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.method, "f(Integer, Integer)")))
        End Sub

        <Fact>
        Public Sub Method_Rename()
            Dim src1 = "Class C : " & vbLf & "Sub Goo : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Sub Bar : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo]@11 -> [Sub Bar]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Sub Bar", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub InterfaceMethod_Rename()
            Dim src1 = "Interface C : " & vbLf & "Sub Goo : End Interface"
            Dim src2 = "Interface C : " & vbLf & "Sub Bar : End Interface"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo]@15 -> [Sub Bar]@15")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "Sub Bar", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncModifier1()
            Dim src1 = "Class C : " & vbLf & "Function F() As Task(Of String) : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Async Function F() As Task(Of String) : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Function F() As Task(Of String) : End Function]@11 -> [Async Function F() As Task(Of String) : End Function]@11",
                "Update [Function F() As Task(Of String)]@11 -> [Async Function F() As Task(Of String)]@11")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_AsyncModifier2()
            Dim src1 = "Class C : " & vbLf & "Async Function F() As Task(Of String) : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Function F() As Task(Of String) : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Async Function F() As Task(Of String) : End Function]@11 -> [Function F() As Task(Of String) : End Function]@11",
                "Update [Async Function F() As Task(Of String)]@11 -> [Function F() As Task(Of String)]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "Function F()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_IteratorModifier1()
            Dim src1 = "Class C : " & vbLf & "Function F() As Task(Of String) : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Iterator Function F() As Task(Of String) : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Function F() As Task(Of String) : End Function]@11 -> [Iterator Function F() As Task(Of String) : End Function]@11",
                "Update [Function F() As Task(Of String)]@11 -> [Iterator Function F() As Task(Of String)]@11")

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodUpdate_IteratorModifier2()
            Dim src1 = "Class C : " & vbLf & "Iterator Function F() As Task(Of String) : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Function F() As Task(Of String) : End Function : End Class"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Iterator Function F() As Task(Of String) : End Function]@11 -> [Function F() As Task(Of String) : End Function]@11",
                "Update [Iterator Function F() As Task(Of String)]@11 -> [Function F() As Task(Of String)]@11")

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.ModifiersUpdate, "Function F()", FeaturesResources.method))
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

        <Fact>
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Insert, "<B>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "<A, B>", VBFeaturesResources.attributes))
        End Sub

        <Fact>
        Public Sub MethodUpdate_UpdateAttribute()
            Dim src1 = "Class C : " & vbLf & "<A>Sub F() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "<A(1)>Sub F() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@12 -> [A(1)]@12")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(1)", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Delete, "Sub F()", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Delete, "Sub F()", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Delete, "Sub F()", VBFeaturesResources.attributes))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsDelete()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub Goo Implements I.Goo : End Sub : " & vbLf & "Sub JGoo Implements J.Goo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub Goo : End Sub : " & vbLf & "Sub JGoo Implements J.Goo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo Implements I.Goo]@29 -> [Sub Goo]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub Goo", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsInsert()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub Goo : End Sub : " & vbLf & "Sub JGoo Implements J.Goo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub Goo Implements I.Goo : End Sub : " & vbLf & "Sub JGoo Implements J.Goo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub Goo]@29 -> [Sub Goo Implements I.Goo]@29")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub Goo", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_ImplementsUpdate()
            Dim src1 = "Class C : Implements I, J : " & vbLf & "Sub IGoo Implements I.Goo : End Sub : " & vbLf & "Sub JGoo Implements J.Goo : End Sub : End Class"
            Dim src2 = "Class C : Implements I, J : " & vbLf & "Sub IGoo Implements J.Goo : End Sub : " & vbLf & "Sub JGoo Implements I.Goo : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Sub IGoo Implements I.Goo]@29 -> [Sub IGoo Implements J.Goo]@29",
                "Update [Sub JGoo Implements J.Goo]@68 -> [Sub JGoo Implements I.Goo]@68")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub IGoo", FeaturesResources.method),
                Diagnostic(RudeEditKind.ImplementsClauseUpdate, "Sub JGoo", FeaturesResources.method))
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
            Dim src1 = "Class C" & vbLf & "Sub M()" & vbLf & "F(1, From goo In bar Select baz) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub M()" & vbLf & "F(2, From goo In bar Select baz) : End Sub : End Class"
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
                Diagnostic(RudeEditKind.Insert, "Yield 2", VBFeaturesResources.Yield_statement))
        End Sub

        <Fact>
        Public Sub MethodDelete_Iterator_Yield()
            Dim src1 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : Yield 2: End Function : End Class"
            Dim src2 = "Class C " & vbLf & "Iterator Function M() As IEnumerable(Of Integer)" & vbLf & "Yield 1 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Iterator Function M()", VBFeaturesResources.Yield_statement))
        End Sub

        <Fact>
        Public Sub MethodInsert_Handles_Clause()
            Dim src1 = "Class C : Event E1 As System.Action" & vbLf & "End Class"
            Dim src2 = "Class C : Event E1 As System.Action" & vbLf & "Private Sub Goo() Handles Me.E1 : End Sub : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Private Sub Goo() Handles Me.E1 : End Sub]@36",
                "Insert [Private Sub Goo() Handles Me.E1]@36",
                "Insert [()]@51")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertHandlesClause, "Private Sub Goo()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_WithStaticLocal()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Static a = 0 : a = 1 : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Static a = 0 : a = 2 : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.UpdateStaticLocal, "Sub Main()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_AddingStaticLocal()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a = 0 : a = 1 : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Static a = 0 : a = 2 : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.UpdateStaticLocal, "Sub Main()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub MethodUpdate_DeletingStaticLocal()
            Dim src1 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Static a = 0 : a = 1 : End Sub : End Module"
            Dim src2 = "Module C : " & vbLf & "Sub Main()" & vbLf & "Dim a = 0 : a = 2 : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PartialMethod_DeleteInsert_DefinitionPart()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcC1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "partial class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcC2 = "partial class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F").PartialImplementationPart)})
                })
        End Sub

        <Fact>
        Public Sub PartialMethod_DeleteInsert_ImplementationPart()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"
            Dim srcC1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"
            Dim srcC2 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F").PartialImplementationPart)})
                })
        End Sub

        <Fact, WorkItem(51011, "https://github.com/dotnet/roslyn/issues/51011")>
        Public Sub PartialMethod_Swap_ImplementationAndDefinitionParts()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"

            ' TODO: current
            GetTopEdits(srcA1, srcA2).VerifyRudeDiagnostics(Diagnostic(RudeEditKind.ModifiersUpdate, "Private Sub F()", FeaturesResources.method))
            GetTopEdits(srcB1, srcB2).VerifyRudeDiagnostics(Diagnostic(RudeEditKind.ModifiersUpdate, "Partial Private Sub F()", FeaturesResources.method))

            ' TODO: correct 
            ' EditAndContinueValidation.VerifySemantics(
            '      { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
            '     
            '     {
            '         DocumentResults(),
            '         DocumentResults(
            '             semanticEdits:= { SemanticEdit(SemanticEditKind.Update, c => c.GetMember(Of NamedTypeSymbol)("C").GetMember("F")) }),
            '     });
        End Sub

        <Fact>
        Public Sub PartialMethod_DeleteImplementation()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Delete, "Partial Class C", DeletedSymbolDisplay(FeaturesResources.method, "F()"))})
                })
        End Sub

        <Fact>
        Public Sub PartialMethod_DeleteBoth()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.Delete, "Partial Class C", DeletedSymbolDisplay(FeaturesResources.method, "F()"))})
                })
        End Sub

        <Fact>
        Public Sub PartialMethod_DeleteInsertBoth()
            Dim srcA1 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB1 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"
            Dim srcC1 = "Partial Class C : End Class"
            Dim srcD1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C : End Class"
            Dim srcC2 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcD2 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)},
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F").PartialImplementationPart)})
                })
        End Sub

        <Fact>
        Public Sub PartialMethod_Insert()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C" + vbCrLf + "Partial Private Sub F() : End Sub : End Class"
            Dim srcB2 = "Partial Class C" + vbCrLf + "Private Sub F() : End Sub : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F").PartialImplementationPart)})
                })
        End Sub
#End Region

#Region "Operators"

        <Fact>
        Public Sub OperatorInsert()
            Dim src1 = "
Class C
End Class
"
            Dim src2 = "
Class C
    Public Shared Operator +(d As C, g As C) As Integer
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(d As C) As Integer
        Return Nothing
    End Operator
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertOperator, "Public Shared Operator +(d As C, g As C)", FeaturesResources.operator_),
                Diagnostic(RudeEditKind.InsertOperator, "Public Shared Narrowing Operator CType(d As C)", FeaturesResources.operator_))
        End Sub

        <Fact>
        Public Sub OperatorDelete()
            Dim src1 = "
Class C
    Public Shared Operator +(d As C, g As C) As Integer
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(d As C) As Integer
        Return Nothing
    End Operator
End Class
"
            Dim src2 = "
Class C
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.operator_, "+(C, C)")),
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.operator_, "CType(C)")))
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/51011"), WorkItem(51011, "https://github.com/dotnet/roslyn/issues/51011")>
        Public Sub OperatorInsertDelete()
            Dim srcA1 = "
Partial Class C
    Public Shared Narrowing Operator CType(d As C) As Integer
        Return Nothing
    End Operator
End Class
"
            Dim srcB1 = "
Partial Class C
    Public Shared Operator +(d As C, g As C) As Integer
        Return Nothing
    End Operator
End Class
"

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("op_Addition"))
                        }),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("op_Implicit"))
                        })
                })
        End Sub

        <Fact>
        Public Sub OperatorUpdate()
            Dim src1 = "
Class C
    Public Shared Narrowing Operator CType(d As C) As Integer
        Return 0
    End Operator

    Public Shared Operator +(d As C, g As C) As Integer
        Return 0
    End Operator
End Class
"
            Dim src2 = "
Class C
    Public Shared Narrowing Operator CType(d As C) As Integer
        Return 1
    End Operator

    Public Shared Operator +(d As C, g As C) As Integer
        Return 1
    End Operator
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("op_Explicit")),
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("op_Addition"))
            })
        End Sub

        <Fact>
        Public Sub OperatorReorder()
            Dim src1 = "
Class C
    Public Shared Narrowing Operator CType(d As C) As Integer
        Return 0
    End Operator

    Public Shared Operator +(d As C, g As C) As Integer
        Return 0
    End Operator
End Class
"
            Dim src2 = "
Class C
    Public Shared Operator +(d As C, g As C) As Integer
        Return 0
    End Operator

    Public Shared Narrowing Operator CType(d As C) As Integer
        Return 0
    End Operator
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Public Shared Operator +(d As C, g As C) As Integer
        Return 0
    End Operator]@116 -> @15")

            edits.VerifyRudeDiagnostics()
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
                Diagnostic(RudeEditKind.GenericTypeUpdate, "Public Sub New(a As Integer)", FeaturesResources.constructor))
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
                Diagnostic(RudeEditKind.Insert, "b As Integer", FeaturesResources.parameter))
        End Sub

        <Fact, WorkItem(789577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789577")>
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(VBFeaturesResources.Shared_constructor, "New()")))
        End Sub

        <Fact>
        Public Sub ModuleCtorDelete()
            Dim src1 = "Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Module C", DeletedSymbolDisplay(FeaturesResources.constructor, "New()")))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Public1()
            Dim src1 = "Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Public2()
            Dim src1 = "Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Theory>
        <InlineData("Private")>
        <InlineData("Protected")>
        <InlineData("Friend")>
        <InlineData("Protected Friend")>
        Public Sub InstanceCtorDelete_NonPublic(visibility As String)
            Dim src1 = "Class C" & vbLf & visibility & " Sub New() : End Sub : End Class"
            Dim src2 = "Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingVisibility, "Class C", DeletedSymbolDisplay(FeaturesResources.constructor, "New()")))
        End Sub

        <Fact>
        Public Sub InstanceCtorDelete_Public_PartialWithInitializerUpdate()
            Dim srcA1 = "Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim srcB1 = "Class C" & vbLf & "Dim x = 1 : End Class"

            Dim srcA2 = "Class C : End Class"
            Dim srcB2 = "Class C" & vbLf & "Dim x = 2 : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Partial_Public_Implicit()
            Dim srcA1 = "Partial Class C" & vbLf & "End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"

            ' no change in document A
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Public_NoImplicit()
            Dim src1 = "Class C" & vbLf & "Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Sub New(a As Integer) : End Sub : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                semanticEdits:=
                {
                    SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.Parameters.IsEmpty))
                })
        End Sub

        <Fact>
        Public Sub InstanceCtorInsert_Partial_Public_NoImplicit()
            Dim srcA1 = "Partial Class C" & vbLf & "End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Sub New(a As Integer) : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Sub New(a As Integer) : End Sub : End Class"

            ' no change in document B
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.Parameters.IsEmpty))}),
                    DocumentResults()
                })
        End Sub

        <Theory>
        <InlineData("Private")>
        <InlineData("Protected")>
        <InlineData("Friend")>
        <InlineData("Friend Protected")>
        Public Sub InstanceCtorInsert_Private_Implicit1(visibility As String)
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C" & vbLf & visibility & " Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingVisibility, visibility & " Sub New()", FeaturesResources.constructor))
        End Sub

        <Fact>
        Public Sub InstanceCtorUpdate_ProtectedImplicit()
            Dim src1 = "Class C" & vbLf & "End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
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

        <Theory>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Friend Protected")>
        Public Sub InstanceCtorInsert_Internal_NoImplicit(visibility As String)
            Dim src1 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C" & vbLf & "Public Sub New(a As Integer) : End Sub : " & vbLf & visibility & " Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub StaticCtor_Partial_DeleteInsert()
            Dim srcA1 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

        <Fact>
        Public Sub ModuleCtor_Partial_DeleteInsert()
            Dim srcA1 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim srcB1 = "Partial Module C : End Module"

            Dim srcA2 = "Partial Module C : End Module"
            Dim srcB2 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        ),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePrivateInsertPrivate()
            Dim srcA1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        ),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePublicInsertPublic()
            Dim srcA1 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeletePrivateInsertPublic()
            Dim srcA1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C" & vbLf & "Public Sub New() : End Sub : End Class"

            ' delete of the constructor in partial part will be reported as rude edit in the other document where it was inserted back with changed visibility
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.ModifiersUpdate, "Public Sub New()", FeaturesResources.constructor)})
                })
        End Sub

        <Fact>
        Public Sub StaticCtor_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be reported as rude edit in the other document where it was inserted back with changed visibility
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        )
                })
        End Sub

        <Fact>
        Public Sub ModuleCtor_Partial_InsertDelete()
            Dim srcA1 = "Partial Module C : End Module"
            Dim srcB1 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"

            Dim srcA2 = "Partial Module C" & vbLf & "Sub New() : End Sub : End Module"
            Dim srcB2 = "Partial Module C : End Module"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        )
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPublicDeletePublic()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        )
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertPrivateDeletePrivate()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        )
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_DeleteInternalInsertInternal()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        )
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_InsertInternalDeleteInternal_WithBody()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Friend Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & "Friend Sub New()" & vbLf & "Console.WriteLine(1) : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults()
                })
        End Sub

        <Theory>
        <InlineData("")>
        <InlineData("Friend")>
        <InlineData("Public")>
        Public Sub InstanceCtor_Partial_VisibilityUpdate(visibility As String)
            If visibility.Length > 0 Then
                visibility &= " "
            End If

            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbLf & "Private Sub New() : End Sub : End Class"

            Dim srcA2 = "Partial Class C" & vbLf & visibility & "Sub New() : End Sub : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            ' delete of the constructor in partial part will be reported as rude edit in the other document where it was inserted back with changed visibility
            VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        diagnostics:={Diagnostic(RudeEditKind.ModifiersUpdate, visibility & "Sub New()", FeaturesResources.constructor)}),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_Update_LambdaInInitializer1()
            Dim src1 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A1(F(<N:0.0>Function(a1) a1 + 1</N:0.0>))
    Dim A2 As Integer = F(<N:0.1>Function(a2) a2 + 1</N:0.1>)
    Dim A3, A4 As New Func(Of Integer, Integer)(<N:0.2>Function(a34) a34 + 1</N:0.2>)
    Dim A5(F(<N:0.3>Function(a51) a51 + 1</N:0.3>), F(<N:0.4>Function(a52) a52 + 1</N:0.4>)) As Integer
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.5>Function(b) b + 1</N:0.5>)

    Public Sub New()
        F(<N:0.6>Function(c) c + 1</N:0.6>)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A1(F(<N:0.0>Function(a1) a1 + 1</N:0.0>))
    Dim A2 As Integer = F(<N:0.1>Function(a2) a2 + 1</N:0.1>)
    Dim A3, A4 As New Func(Of Integer, Integer)(<N:0.2>Function(a34) a34 + 1</N:0.2>)
    Dim A5(F(<N:0.3>Function(a51) a51 + 1</N:0.3>), F(<N:0.4>Function(a52) a52 + 1</N:0.4>)) As Integer
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.5>Function(b) b + 1</N:0.5>)

    Public Sub New()
        F(<N:0.6>Function(c) c + 2</N:0.6>)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_Update_LambdaInInitializer_Trivia1()
            Dim src1 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Public Sub New()
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

       Public Sub New()
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_Update_LambdaInInitializer_ExplicitInterfaceImpl1()
            Dim src1 As String = "
Imports System

Public Interface I
    ReadOnly Property B As Integer
End Interface

Public Interface J
    ReadOnly Property B As Integer
End Interface

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    Implements I, J

    Private ReadOnly Property I_B As Integer = F(<N:0.1>Function(ib) ib + 1</N:0.1>) Implements I.B
    Private ReadOnly Property J_B As Integer = F(<N:0.2>Function(jb) jb + 1</N:0.2>) Implements J.B

    Public Sub New()
        F(<N:0.3>Function(c) c + 1</N:0.3>)
    End Sub
End Class
"
            Dim src2 As String = "
Imports System

Public Interface I
    ReadOnly Property B As Integer
End Interface

Public Interface J
    ReadOnly Property B As Integer
End Interface

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    Implements I, J

    Private ReadOnly Property I_B As Integer = F(<N:0.1>Function(ib) ib + 1</N:0.1>) Implements I.B
    Private ReadOnly Property J_B As Integer = F(<N:0.2>Function(jb) jb + 1</N:0.2>) Implements J.B

    Public Sub New()
        F(<N:0.3>Function(c) c + 2</N:0.3>)   ' update
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub InstanceCtor_Partial_Insert_Parameterless_LambdaInInitializer1()
            Dim src1 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(a) a + 1</N:0.1>)
End Class
"
            Dim src2 = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(a) a + 1</N:0.1>)

    Sub New()      ' new ctor
        F(Function(c) c + 1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemanticDiagnostics(
                {Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "Sub New()")})
        End Sub

        <Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")>
        Public Sub InstanceCtor_Partial_Insert_WithParameters_LambdaInInitializer1()
            Dim src1 As String = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)
End Class
"
            Dim src2 As String = "
Imports System

Partial Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    ReadOnly Property B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(x As Integer)              ' new ctor
        F(Function(c) c + 1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "Sub New(x As Integer)"))

            ' TODO bug https://github.com/dotnet/roslyn/issues/2504
            ' edits.VerifySemantics(
            '     ActiveStatementsDescription.Empty,
            '     {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap(0))})

        End Sub

        <Fact>
        public sub PartialTypes_ConstructorWithInitializerUpdates()
            Dim srcA1 = "
Imports System

Partial Class C
    Sub New(arg As Integer)
        Console.WriteLine(0)
    End Sub

    Sub New(arg As Boolean)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim srcB1 = "
Imports System

Partial Class C
    Dim a <N:0.0>= 1</N:0.0>

    Sub New(arg As UInteger)
        Console.WriteLine(2)
    End Sub
End Class
"

            Dim srcA2 = "
Imports System

Partial Class C
    Sub New(arg As Integer)
        Console.WriteLine(0)
    End Sub

    Sub New(arg As Boolean)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim srcB2 = "
Imports System

Partial Class C
    Dim a <N:0.0>= 2</N:0.0>    ' updated field initializer

    Sub New(arg As UInteger)
        Console.WriteLine(2)
    End Sub

    Sub New(arg As Byte)
        Console.WriteLine(3)    ' new ctor
    End Sub
End Class
"
            Dim syntaxMapB = GetSyntaxMap(srcB1, srcB2)(0)

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits:=
                        {
                           SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(m) m.Parameters.Single().Type.Name = "Int32"), partialType:="C", syntaxMap:=syntaxMapB),
                           SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(m) m.Parameters.Single().Type.Name = "Boolean"), partialType:="C", syntaxMap:=syntaxMapB),
                           SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(m) m.Parameters.Single().Type.Name = "UInt32"), partialType:="C", syntaxMap:=syntaxMapB),
                           SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(m) m.Parameters.Single().Type.Name = "Byte"), syntaxMap:=Nothing)
                        })
                })
        End Sub

        <Fact>
        Public Sub PartialTypes_ConstructorWithInitializerUpdates_SemanticErrors()
            Dim srcA1 = "
Imports System

Partial Class C
    Sub New(arg As Integer)
        Console.WriteLine(0)
    End Sub

    Sub New(arg As Integer)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim srcB1 = "
Imports System

Partial Class C
    Dim a = 1
End Class
"

            Dim srcA2 = "
Imports System

Partial Class C
    Sub New(arg As Integer)
        Console.WriteLine(0)
    End Sub

    Sub New(arg As Integer)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim srcB2 = "
Imports System

Partial Class C
    Dim a = 1

    Sub New(arg As Integer)
        Console.WriteLine(2)
    End Sub
End Class
"

            ' The actual edits do not matter since there are semantic errors in the compilation.
            ' We just should not crash.
            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:=Array.Empty(Of RudeEditDiagnosticDescription)())
                })
        End Sub

        <Fact>
        Public Sub Constructor_SemanticError_Partial()
            Dim src1 = "
Partial Class C
    Partial Sub New(x As Integer)
    End Sub
End Class

Class C
    Partial Sub New(x As Integer)
        System.Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 = "
Partial Class C
    Partial Sub New(x As Integer)
    End Sub
End Class

Class C
    Partial Sub New(x As Integer)
        System.Console.WriteLine(2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(ActiveStatementsDescription.Empty, semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.First(), preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        public sub PartialDeclaration_Delete()
            Dim srcA1 = "
Partial Class C
    Sub New()
    End Sub

    Sub F()
    End Sub
End Class
"
            Dim srcB1 = "
Partial Class C
    Dim x = 1
End Class
"

            Dim srcA2 = ""
            Dim srcB2 = "
Partial Class C
    Dim x = 2

    Sub F()
    End Sub
End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        })
                })
        End Sub

        <Fact>
        Public Sub PartialDeclaration_Insert()
            Dim srcA1 = ""
            Dim srcB1 = "
Partial Class C
    Dim x = 1

    Sub F()
    End Sub
End Class
"
            Dim srcA2 = "
Partial Class C
    Public Sub New()
    End Sub

    Sub F()
    End Sub
End Class"
            Dim srcB2 = "
Partial Class C
    Dim x = 2
End Class
"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F")),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                        }),
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)})
                })
        End Sub

#End Region

#Region "Declare"
        <Fact>
        Public Sub Declare_Update1()
            Dim src1 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Goo Lib ""Baz"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10 -> [Declare Ansi Function Goo Lib ""Baz"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareLibraryUpdate, "Declare Ansi Function Goo Lib ""Baz"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Update2()
            Dim src1 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Unicode Function Goo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10 -> [Declare Unicode Function Goo Lib ""Bar"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Declare Unicode Function Goo Lib ""Bar"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Update3()
            Dim src1 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" Alias ""Al"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10 -> [Declare Ansi Function Goo Lib ""Bar"" Alias ""Al"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareAliasUpdate, "Declare Ansi Function Goo Lib ""Bar"" Alias ""Al"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Update4()
            Dim src1 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" Alias ""A1"" () As Integer : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" Alias ""A2"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Update [Declare Ansi Function Goo Lib ""Bar"" Alias ""A1"" () As Integer]@10 -> [Declare Ansi Function Goo Lib ""Bar"" Alias ""A2"" () As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.DeclareAliasUpdate, "Declare Ansi Function Goo Lib ""Bar"" Alias ""A2"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Delete()
            Dim src1 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"
            Dim src2 As String = "Class C : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Delete [Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10",
                "Delete [()]@46",
                "Delete [As Integer]@49")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.method, "Goo()")))
        End Sub

        <Fact>
        Public Sub Declare_Insert1()
            Dim src1 As String = "Class C : End Class"
            Dim src2 As String = "Class C : Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10",
                "Insert [()]@46",
                "Insert [As Integer]@49")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "Declare Ansi Function Goo Lib ""Bar"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Insert2()
            Dim src1 As String = "Class C : End Class"
            Dim src2 As String = "Class C : Private Declare Ansi Function Goo Lib ""Bar"" () As Integer : End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyEdits(
                "Insert [Private Declare Ansi Function Goo Lib ""Bar"" () As Integer]@10",
                "Insert [()]@54",
                "Insert [As Integer]@57")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "Private Declare Ansi Function Goo Lib ""Bar"" ()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Insert3()
            Dim src1 As String = "Module M : End Module"
            Dim src2 As String = "Module M : Declare Ansi Sub ExternSub Lib ""ExternDLL""() : End Module"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "Declare Ansi Sub ExternSub Lib ""ExternDLL""()", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_Insert4()
            Dim src1 As String = "Module M : End Module"
            Dim src2 As String = "Module M : Declare Ansi Sub ExternSub Lib ""ExternDLL"" : End Module"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertDllImport, "Declare Ansi Sub ExternSub Lib ""ExternDLL""", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Declare_DeleteInsert()
            Dim srcA1 = "Module M : Declare Ansi Sub ExternSub Lib ""ExternDLL"" : End Module"
            Dim srcB1 = "Module M : End Module"

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults()
                })
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
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.field))
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/51373"), WorkItem(51373, "https://github.com/dotnet/roslyn/issues/51373")>
        Public Sub FieldUpdate_Rename2()
            Dim src1 = "Class C : Dim a1(), b1? As Integer, c1(1,2) As New D() : End Class"
            Dim src2 = "Class C : Dim a2(), b2? As Integer, c2(1,2) As New D() : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a1()]@14 -> [a2()]@14",
                "Update [b1?]@20 -> [b2?]@20",
                "Update [c1(1,2)]@36 -> [c2(1,2)]@36")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "a2()", FeaturesResources.field),
                Diagnostic(RudeEditKind.Renamed, "b2?", FeaturesResources.field),
                Diagnostic(RudeEditKind.Renamed, "c2(1,2)", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate1()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Boolean : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@16 -> [As Boolean]@16")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Boolean", FeaturesResources.field))
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

            edits.VerifySemantics()
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

            edits.VerifySemantics()
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

            edits.VerifySemantics()
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

            edits.VerifySemantics()
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

            edits.VerifySemantics()
        End Sub

        <Fact>
        Public Sub Field_VariableMove6()
            Dim src1 = "Class C : Dim a As Object, b As Object : End Class"
            Dim src2 = "Class C : Dim b As Object, a As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b As Object]@27 -> @14")

            edits.VerifySemantics()
        End Sub

        <Fact>
        Public Sub Field_VariableMove7()
            Dim src1 = "Class C : Dim a As Object, b, c As Object : End Class"
            Dim src2 = "Class C : Dim b, c As Object, a As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b, c As Object]@27 -> @14")

            edits.VerifySemantics()
        End Sub

        <Fact>
        Public Sub Field_VariableMove_TypeChange()
            Dim src1 = "Class C : Dim a As Object, b, c As Object : End Class"
            Dim src2 = "Class C : Dim a, b As Integer, c As Object : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a, b As Integer", FeaturesResources.field))
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
                Diagnostic(RudeEditKind.Delete, "Dim b As Object", DeletedSymbolDisplay(FeaturesResources.field, "c")))
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
                Diagnostic(RudeEditKind.TypeUpdate, "a?", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "b()", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "c", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "d", FeaturesResources.field))
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
                Diagnostic(RudeEditKind.TypeUpdate, "c(2)", FeaturesResources.field))
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
                Diagnostic(RudeEditKind.TypeUpdate, "a()", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "a() As Integer", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate4b()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a(1) As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a(1)", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate5()
            Dim src1 = "Class C : Dim a, b : End Class"
            Dim src2 = "Class C : Dim a, b As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [As Integer]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "As Integer", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate6()
            Dim src1 = "Class C : Dim a, b As Integer : End Class"
            Dim src2 = "Class C : Dim a, b : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@19")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a, b", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Field_TypeUpdate7()
            Dim src1 = "Class C : Dim a(1) As Integer : End Class"
            Dim src2 = "Class C : Dim a(1,2) As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a(1,2)", FeaturesResources.field))
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.field, "a")))
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.event_, "a")))
        End Sub

        <Fact>
        Public Sub FieldUpdate_FieldToWithEvents()
            Dim src1 = "Class C : Dim a As WE : End Class"
            Dim src2 = "Class C : WithEvents a As WE : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Dim a As WE]@10 -> [WithEvents a As WE]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "WithEvents a As WE", VBFeaturesResources.WithEvents_field))
        End Sub

        <Fact>
        Public Sub FieldUpdate_WithEventsToField()
            Dim src1 = "Class C : WithEvents a As WE : End Class"
            Dim src2 = "Class C : Dim a As WE : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [WithEvents a As WE]@10 -> [Dim a As WE]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Dim a As WE", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub FieldReorder()
            Dim src1 = "Class C : Dim a = 0 : Dim b = 1 : Dim c = 2 : End Class"
            Dim src2 = "Class C : Dim c = 2 : Dim a = 0 : Dim b = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Dim c = 2", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub EventFieldReorder()
            Dim src1 = "Class C : Dim a As Integer = 0 : Dim b As Integer = 1 : Event c As Action : End Class"
            Dim src2 = "Class C : Event c As Action : Dim a As Integer = 0 : Dim b As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [Event c As Action]@56 -> @10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "Event c", FeaturesResources.event_))
        End Sub

        <Fact>
        Public Sub EventField_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "Partial Class C" & vbCrLf & "Event E As System.Action : End Class"

            Dim srcA2 = "Partial Class C" & vbCrLf & "Event E As System.Action : End Class"
            Dim srcB2 = "Partial Class C : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults()
                })
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
                Diagnostic(RudeEditKind.InsertVirtual, "F As C", VBFeaturesResources.WithEvents_field))
        End Sub

        <Fact>
        Public Sub FieldInsert_WithEvents2()
            Dim src1 = "Class C : WithEvents F As C : End Class"
            Dim src2 = "Class C : WithEvents F, G As C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "G", VBFeaturesResources.WithEvents_field))
        End Sub

        <Fact>
        Public Sub FieldInsert_WithEvents3()
            Dim src1 = "Class C : WithEvents F As C : End Class"
            Dim src2 = "Class C : WithEvents F As C, G As C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "G As C", VBFeaturesResources.WithEvents_field))
        End Sub

        <Fact>
        Public Sub FieldInsert_IntoStruct()
            Dim src1 = "Structure S : Private a As Integer : End Structure"
            Dim src2 = "
Structure S 
    Private a As Integer
    Private b As Integer
    Private Shared c As Integer
    Private Event d As System.Action
End Structure
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Event d As System.Action", FeaturesResources.event_, VBFeaturesResources.structure_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "b As Integer", FeaturesResources.field, VBFeaturesResources.structure_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "c As Integer", FeaturesResources.field, VBFeaturesResources.structure_))
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
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b As Integer", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c As Integer", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d As Integer", FeaturesResources.field, FeaturesResources.class_))
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
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b As Integer", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c As Integer", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d As Integer", FeaturesResources.field, FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_LayoutClass_Sequential_OrderPreserved()
            Dim src1 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Partial Class C
    Private a As Integer
    Private b As Integer
End Class
"

            Dim src2 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Partial Class C
    Private a As Integer
End Class

Partial Class C
    Private b As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            ' TODO: We don't compare the ordering currently. We could allow this edit if the ordering is preserved.
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b As Integer", FeaturesResources.field, FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_LayoutClass_Sequential_OrderChanged()
            Dim src1 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Partial Class C
    Private a As Integer
    Private b As Integer
End Class
"

            Dim src2 = "
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Partial Class C
    Private b As Integer
End Class

Partial Class C
    Private a As Integer
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "a As Integer", FeaturesResources.field, FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub FieldInsert_WithInitializersAndLambdas1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)

    Public Sub New()
        F(<N:0.1>Function(c) c + 1</N:0.1>)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(Function(b) b + 1)   ' new field

    Public Sub New()
        F(<N:0.1>Function(c) c + 1</N:0.1>)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.B")),
                 SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInsert_ConstructorReplacingImplicitConstructor_WithInitializersAndLambdas()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)

    Dim B As Integer = F(Function(b) b + 1)   ' new field

    Sub New()                                 ' new ctor replacing existing implicit constructor
        F(Function(c) c + 1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.B")),
                 SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")>
        Public Sub FieldInsert_ParameterlessConstructorInsert_WithInitializersAndLambdas()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 1
    End Function

    Dim A = F(<N:0.0>Function(a) a + 1</N:0.0>)

    Public Sub New(x As Integer)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 1
    End Function

    Dim A = F(<N:0.0>Function(a) a + 1</N:0.0>)

    Public Sub New(x As Integer)
    End Sub

    Public Sub New                           ' new ctor
        F(Function(c) c + 1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "Public Sub New"))

            '  TODO (bug https//github.com/dotnet/roslyn/issues/2504):
            ' edits.VerifySemantics(
            '     ActiveStatementsDescription.Empty,
            '     {
            '         SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap(0))
            '     })
        End Sub

        <Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")>
        Public Sub FieldInsert_ConstructorInsert_WithInitializersAndLambdas1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)

    Dim B As Integer = F(Function(b) b + 1)   ' new field

    Sub New(x As Integer)                     ' new ctor
        F(Function(c) c + 1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "Sub New(x As Integer)"))

            ' TODO (bug https://github.com/dotnet/roslyn/issues/2504):
            'edits.VerifySemantics(
            '    ActiveStatementsDescription.Empty,
            '    {
            '        SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.B")),
            '        SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))
            '    })

        End Sub

        <Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")>
        Public Sub FieldInsert_ConstructorInsert_WithInitializersButNoExistingLambdas1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(Nothing)
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
        Return 0
    End Function

    Dim A As Integer = F(Nothing)
    Dim B As Integer = F(Function(b) b + 1)   ' new field

    Sub New(x As Integer)                     ' new ctor
        F(Function(c) c + 1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {
                    SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single())
                })
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
                Diagnostic(RudeEditKind.Move, "Property Q", FeaturesResources.auto_property))
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
                Diagnostic(RudeEditKind.TypeUpdate, "Property P", FeaturesResources.auto_property))
        End Sub

        <Fact>
        Public Sub PropertyInsert()
            Dim src1 = "Class C : End Class"
            Dim src2 = "
Class C 
    Property P
        Get
            Return 1
        End Get
        Set(value)
        End Set
    End Property
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                semanticEdits:={SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("P"))})
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
                Diagnostic(RudeEditKind.Delete, "Private ReadOnly Property P", DeletedSymbolDisplay(VBFeaturesResources.property_accessor, "P(Integer)")))
        End Sub

        <Fact>
        Public Sub PropertyRename1()
            Dim src1 = "Class C : ReadOnly Property P As Integer" & vbLf & "Get : End Get : End Property : End Class"
            Dim src2 = "Class C : ReadOnly Property Q As Integer" & vbLf & "Get : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ReadOnly Property P As Integer]@10 -> [ReadOnly Property Q As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "ReadOnly Property Q", FeaturesResources.property_))
        End Sub

        <Fact>
        Public Sub PropertyRename2()
            Dim src1 = "Class C : ReadOnly Property P As Integer : End Class"
            Dim src2 = "Class C : ReadOnly Property Q As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ReadOnly Property P As Integer]@10 -> [ReadOnly Property Q As Integer]@10")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "ReadOnly Property Q", FeaturesResources.auto_property))
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
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Property b As Integer", FeaturesResources.auto_property, VBFeaturesResources.structure_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "Private Shared Property c As Integer", FeaturesResources.auto_property, VBFeaturesResources.structure_))
        End Sub

        <Fact>
        Public Sub PropertyInsert_IntoLayoutClass_Sequential()
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
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "Private Property b As Integer", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "Private Shared Property c As Integer", FeaturesResources.auto_property, FeaturesResources.class_))
        End Sub

        <Fact>
        Public Sub Property_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "
Partial Class C
    Private Property P As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class
"

            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P").SetMethod)
                        }),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub AutoProperty_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "
Partial Class C
    Private Property P As Integer
End Class
"
            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub AutoPropertyWithInitializer_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "
Partial Class C
    Private Property P As Integer = 1
End Class
"
            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)}),
                    DocumentResults()
                })
        End Sub

        <Fact>
        Public Sub Property_Update_LiftedParameter()
            Dim src1 = "
Imports System

Partial Class C
    Private Property P(a As Integer) As Integer
        Get
            Return New Func(Of Integer)(Function() a + 1).Invoke()
        End Get
        Set
        End Set
    End Property
End Class
"
            Dim src2 = "
Imports System

Partial Class C
    Private Property P(a As Integer) As Integer
        Get
            Return New Func(Of Integer)(Function() 2).Invoke()
        End Get
        Set
        End Set
    End Property
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"))
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_Instance_InitializerUpdate()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer = 0]@10 -> [Property a As Integer = 1]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_Instance_AsNewInitializerUpdate()
            Dim src1 = "Class C : Property a As New C(0) : End Class"
            Dim src2 = "Class C : Property a As New C(1) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As New C(0)]@10 -> [Property a As New C(1)]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Array1()
            Dim src1 = "Class C : Dim a(1), b(1) : End Class"
            Dim src2 = "Class C : Dim a(2), b(1) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a(1)]@14 -> [a(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_AsNew1()
            Dim src1 = "Class C : Dim a As New Decimal(1) : End Class"
            Dim src2 = "Class C : Dim a As New Decimal(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As New Decimal(1)]@14 -> [a As New Decimal(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        ''' <summary>
        ''' It's a semantic error to specify array bunds and initializer at the same time.
        ''' EnC analysis needs to handle this case without failing.
        ''' </summary>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/51373"), WorkItem(51373, "https://github.com/dotnet/roslyn/issues/51373")>
        Public Sub Field_InitializerUpdate_InitializerAndArrayBounds()
            Dim src1 = "
Class C
    Dim x(1) As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Dim x(2) As Integer = 2
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
        End Sub

        ''' <summary>
        ''' It's a semantic error to specify array bunds and initializer at the same time.
        ''' EnC analysis needs to handle this case without failing.
        ''' </summary>
        <Fact>
        Public Sub Field_InitializerUpdate_AsNewAndArrayBounds()
            Dim src1 = "
Class C
    Dim x(1) As New C
End Class
"

            Dim src2 = "
Class C
    Dim x(2) As New C
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
        End Sub

        <Fact(), WorkItem(2543, "https://github.com/dotnet/roslyn/issues/2543")>
        Public Sub Field_InitializerUpdate_AsNew2()
            Dim src1 = "Class C : Dim a, b As New Decimal(1) : End Class"
            Dim src2 = "Class C : Dim a, b As New Decimal(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a, b As New Decimal(1)]@14 -> [a, b As New Decimal(2)]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_AsNew()
            Dim src1 = "Class C : Property a As New Decimal(1) : End Class"
            Dim src2 = "Class C : Property a As New Decimal(2) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As New Decimal(1)]@10 -> [Property a As New Decimal(2)]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Untyped()
            Dim src1 = "Class C : Dim a = 1 : End Class"
            Dim src2 = "Class C : Dim a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a = 1]@14 -> [a = 2]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Untyped()
            Dim src1 = "Class C : Property a = 1 : End Class"
            Dim src2 = "Class C : Property a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a = 1]@10 -> [Property a = 2]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Delete()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer = 0]@14 -> [a As Integer]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Delete()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer = 0]@10 -> [Property a As Integer]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_InitializerUpdate_Insert()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@14 -> [a As Integer = 0]@14")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_InitializerUpdate_Insert()
            Dim src1 = "Class C : Property a As Integer : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Property a As Integer]@10 -> [Property a As Integer = 0]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
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
                Diagnostic(RudeEditKind.Insert, "As Integer", VBFeaturesResources.as_clause))
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorUpdate1()
            Dim src1 = "Module C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@15 -> [a As Integer = 0]@15",
                "Delete [Sub New() : End Sub]@31",
                "Delete [Sub New()]@31",
                "Delete [()]@38")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_Private()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Private Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingVisibility, "Class C", DeletedSymbolDisplay(FeaturesResources.constructor, "New()")))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_Private()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Private Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingVisibility, "Class C", DeletedSymbolDisplay(FeaturesResources.constructor, "New()")))
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_Public()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_Public()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_StaticCtorUpdate2()
            Dim src1 = "Class C : Shared a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@17 -> [a As Integer = 0]@17")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_ModuleCtorUpdate2()
            Dim src1 = "Module C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Dim a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@15 -> [a As Integer = 0]@15")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_StaticCtorUpdate2()
            Dim src1 = "Class C : Shared Property a As Integer : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Shared Property a As Integer = 0 : " & vbLf & "Shared Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_ModuleCtorUpdate2()
            Dim src1 = "Module C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Module"
            Dim src2 = "Module C : Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Module"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate2()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate2()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Sub New() : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate3()
            Dim src1 = "Class C : Dim a As Integer : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate3()
            Dim src1 = "Class C : Property a As Integer : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate4()
            Dim src1 = "Class C : Dim a As Integer = 0 : End Class"
            Dim src2 = "Class C : Dim a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate4()
            Dim src1 = "Class C : Property a As Integer = 0 : End Class"
            Dim src2 = "Class C : Property a As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate5()
            Dim src1 = "Class C : Dim a As Integer : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)"), preserveLocalVariables:=True),
                                  SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate5()
            Dim src1 = "Class C : Property a As Integer : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)"), preserveLocalVariables:=True),
                                  SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_MeNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_MeNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_MyClassNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_MyClassNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "MyClass.New(True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_InstanceCtorUpdate_EscapedNew()
            Dim src1 = "Class C : Dim a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)"), preserveLocalVariables:=True),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorUpdate_EscapedNew()
            Dim src1 = "Class C : Property a As Integer :     " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 0 : " & vbLf & "Private Sub New(a As Integer)" & vbLf & "Me.[New](True) : End Sub : " & vbLf & "Private Sub New(a As Boolean) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Integer)"), preserveLocalVariables:=True),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(Function(m) m.ToString() = "Private Sub New(a As Boolean)"), preserveLocalVariables:=True)})
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
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyUpdate_InstanceCtorInsertExplicit()
            Dim src1 = "Class C : Private Property a As Integer : End Class"
            Dim src2 = "Class C : Private Property a As Integer = 0 : " & vbLf & "Sub New() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub FieldUpdate_GenericType()
            Dim src1 = "Class C(Of T) : Dim a As Integer = 1 : End Class"
            Dim src2 = "Class C(Of T) : Dim a As Integer = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "a As Integer = 2", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub PropertyUpdate_GenericType()
            Dim src1 = "Class C(Of T) : Property a As Integer = 1 : End Class"
            Dim src2 = "Class C(Of T) : Property a As Integer = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "Property a", FeaturesResources.auto_property))
        End Sub

        <Fact>
        Public Sub FieldUpdate_LambdaInConstructor()
            Dim src1 = "Class C : Dim a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_LambdaInConstructor()
            Dim src1 = "Class C : Property a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(Sub() System.Console.WriteLine()) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldUpdate_QueryInConstructor()
            Dim src1 = "Class C : Dim a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim src2 = "Class C : Dim a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_QueryInConstructor()
            Dim src1 = "Class C : Property a As Integer = 1 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim src2 = "Class C : Property a As Integer = 2 : " & vbLf & "Sub New()" & vbLf & "F(From a In b Select c) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemanticDiagnostics()
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

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithSingleDeclaration()
            Dim src1 = "Partial Class C : Property a = 1 : End Class"
            Dim src2 = "Partial Class C : Property a = 2 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub FieldUpdate_PartialTypeWithMultipleDeclarations1()
            Dim src1 = "Partial Class C : Dim a = 1 : End Class : Class C : End Class"
            Dim src2 = "Partial Class C : Dim a = 2 : End Class : Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithMultipleDeclarations1()
            Dim src1 = "Partial Class C : Property a = 1 : End Class : Class C : End Class"
            Dim src2 = "Partial Class C : Property a = 2 : End Class : Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub FieldUpdate_PartialTypeWithMultipleDeclarations2()
            Dim src1 = "Class C : Dim a = 1 : End Class : Partial Class C : End Class"
            Dim src2 = "Class C : Dim a = 2 : End Class : Partial Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub PropertyUpdate_PartialTypeWithMultipleDeclarations2()
            Dim src1 = "Class C : Property a = 1 : End Class : Partial Class C : End Class"
            Dim src2 = "Class C : Property a = 2 : End Class : Partial Class C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single, preserveLocalVariables:=True)
            })
        End Sub

        <Theory>
        <InlineData("Dim")>
        <InlineData("Private")>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        <InlineData("Private ReadOnly")>
        Public Sub Field_Insert(modifiers As String)
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & modifiers & " a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("a")),
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
        End Sub

        <Theory>
        <InlineData("")>
        <InlineData("Private")>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Sub Property_Insert(accessibility As String)
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : " & accessibility & " Property a As Integer = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("a")),
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
        End Sub

        <Fact, WorkItem(2543, "https://github.com/dotnet/roslyn/issues/2543")>
        Public Sub Field_Insert_MultiDeclaration()
            Dim src1 = "Class C : Private a As Integer = 1 : End Class"
            Dim src2 = "Class C : Private a, b As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer = 1]@18 -> [a, b As Integer]@18",
                "Insert [b]@21")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.b")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Insert_MultiDeclaration_AsNew()
            Dim src1 = "Class C : Private a As C = New C : End Class"
            Dim src2 = "Class C : Private a, b As New C : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As C = New C]@18 -> [a, b As New C]@18",
                "Insert [b]@21",
                "Delete [As C]@20")

            ' TODO: allow this edit
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a, b As New C", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub Field_Insert_MultiDeclaration_Split()
            Dim src1 = "Class C : Private a, b As Integer = 1 : End Class"
            Dim src2 = "Class C : Private a As Integer : Private b As Integer : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private b As Integer]@33",
                "Update [a, b As Integer = 1]@18 -> [a As Integer]@18",
                "Insert [b As Integer]@41",
                "Move [b]@21 -> @41",
                "Insert [As Integer]@43")

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_MultiDeclaration_TypeChange()
            Dim src1 = "Class C : Private a, b As Integer = 1 : End Class"
            Dim src2 = "Class C : Private a As Integer : Private b As Byte : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "b As Byte", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_Partial_MultiDeclaration_TypeChange()
            Dim srcA1 = "Partial Class C : Private a As Integer = 1 : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C : Private a, b As Byte : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(diagnostics:=
                    {
                        Diagnostic(RudeEditKind.TypeUpdate, "a, b As Byte", FeaturesResources.field)
                    })
                })
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_MultiDeclaration_Split()
            Dim srcA1 = "Partial Class C : Private a, b As Integer : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C : Private a As Integer = 1 : Private b As Integer = 2 : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
        End Sub

        <Fact>
        Public Sub Field_DeleteInsert_Partial_MultiDeclaration_UpdateArrayBounds()
            Dim srcA1 = "Partial Class C : Dim F1(1, 2), F2? As Integer : End Class"
            Dim srcB1 = "Partial Class C : End Class"

            Dim srcA2 = "Partial Class C : End Class"
            Dim srcB2 = "Partial Class C : Dim F1(1, 3), F2? As Integer : End Class"

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits:=
                    {
                        SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), partialType:="C", preserveLocalVariables:=True)
                    })
                })
        End Sub

        <Fact>
        Public Sub FieldInsert_PrivateUntyped()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private a = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private a = 1]@10",
                "Insert [a = 1]@18",
                "Insert [a]@18")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyInsert_PrivateUntyped()
            Dim src1 = "Class C : End Class"
            Dim src2 = "Class C : Private Property a = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [Private Property a = 1]@10")

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember("C.a")),
                                   SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.field, "a")))
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
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.auto_property, "a")))
        End Sub

        <Fact>
        Public Sub FieldUpdate_SingleLineFunction()
            Dim src1 = "Class C : Dim a As Integer = F(1, Function(x, y) x + y) : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(2, Function(x, y) x + y) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_SingleLineFunction()
            Dim src1 = "Class C : Property a As Integer = F(1, Function(x, y) x + y) : End Class"
            Dim src2 = "Class C : Property a As Integer = F(2, Function(x, y) x + y) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldUpdate_MultiLineFunction()
            Dim src1 = "Class C : Dim a As Integer = F(1, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim src2 = "Class C : Dim a As Integer = F(2, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_MultiLineFunction()
            Dim src1 = "Class C : Property a As Integer = F(1, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim src2 = "Class C : Property a As Integer = F(2, Function(x)" & vbLf & "Return x" & vbLf & "End Function) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub FieldUpdate_Query()
            Dim src1 = "Class C : Dim a = F(1, From goo In bar Select baz) : End Class"
            Dim src2 = "Class C : Dim a = F(2, From goo In bar Select baz) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
        End Sub

        <Fact>
        Public Sub PropertyUpdate_Query()
            Dim src1 = "Class C : Property a = F(1, From goo In bar Select baz) : End Class"
            Dim src2 = "Class C : Property a = F(2, From goo In bar Select baz) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics()
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

        <Fact>
        Public Sub ConstField_Update()
            Dim src1 = "Class C : Const x = 0 : End Class"
            Dim src2 = "Class C : Const x = 1 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "x = 1", FeaturesResources.const_field))
        End Sub

        <Fact>
        Public Sub ConstField_Delete()
            Dim src1 = "Class C : Const x = 0 : End Class"
            Dim src2 = "Class C : Dim x = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Dim x = 0", FeaturesResources.field))
        End Sub

        <Fact>
        Public Sub ConstField_Add()
            Dim src1 = "Class C : Dim x = 0 : End Class"
            Dim src2 = "Class C : Const x = 0 : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Const x = 0", FeaturesResources.const_field))
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 2</N:0.1>)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)
            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithoutLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = 1
    Dim B As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = 2
    Dim B As Integer = F(<N:0.0>Function(b) b + 2</N:0.0>)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)
            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 2</N:0.1>)

    Sub New
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)
            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithoutLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = 1
    Dim B As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)

    Sub New
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = 2
    Dim B As Integer = F(<N:0.0>Function(b) b + 2</N:0.0>)

    Sub New
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)
            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializers_EditInitializerWithLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
    End Sub

    Sub New(b As Boolean)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 2</N:0.1>)

    Sub New(a As Integer)
    End Sub

    Sub New(b As Boolean)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(0), syntaxMap(0)),
                 SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(1), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(<N:0.3>Function(d) d + 1</N:0.3>)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(b) b + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 2</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(<N:0.3>Function(d) d + 1</N:0.3>)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(0), syntaxMap(0)),
                 SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(1), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda_Trivia1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(<N:0.3>Function(d) d + 1</N:0.3>)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer =      F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(<N:0.3>Function(d) d + 1</N:0.3>)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(0), syntaxMap(0)),
                 SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(1), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(Function(d) d + 1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer)
        F(<N:0.2>Function(c) c + 2</N:0.2>)
    End Sub

    Sub New(b As Boolean)
        F(Function(d) d + 1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(a As System.Int32)"), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda_Trivia1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer) 
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub
    
    Sub New(b As Boolean)
        F(Function(d) d + 1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

        Sub New(a As Integer) 
        F(<N:0.2>Function(c) c + 1</N:0.2>)
    End Sub
    
    Sub New(b As Boolean) 
        F(Function(d) d + 1) 
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(a As System.Int32)"), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithoutLambda1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        Console.WriteLine(2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(b As System.Boolean)"), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_EditConstructorNotIncludingInitializers()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(Function(a) a + 1)
    Dim B As Integer = F(Function(b) b + 1)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        MyClass.New(1)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(Function(a) a + 1)
    Dim B As Integer = F(Function(b) b + 1)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        MyClass.New(1)
        Console.WriteLine(2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(b As System.Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_RemoveCtorInitializer1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer) 
        ' method with a static local is currently non-editable
        Static s As Integer = 1
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        MyClass.New(1)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(<N:0.0>Function(a) a + 1</N:0.0>)
    Dim B As Integer = F(<N:0.1>Function(b) b + 1</N:0.1>)

    Sub New(a As Integer) 
        ' method with a static local is currently non-editable
        Static s As Integer = 1
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(b As System.Boolean)"), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_AddCtorInitializer1()
            Dim src1 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(Function(a) a + 1)
    Dim B As Integer = F(Function(b) b + 1)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 = "
Imports System

Class C
    Shared Function F(x As Func(Of Integer, Integer)) As Integer
    End Function

    Dim A As Integer = F(Function(a) a + 1)
    Dim B As Integer = F(Function(b) b + 1)

    Sub New(a As Integer) 
        F(Function(c) c + 1)
    End Sub
    
    Sub New(b As Boolean)
        MyClass.New(1)
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors.Single(Function(ctor) ctor.ToTestDisplayString() = "Sub C..ctor(b As System.Boolean)"))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_ImplicitCtor_ArrayBounds1()
            Dim src1 = "Class C : Dim a((<N:0.0>Function(n) n + 1</N:0.0>)(1)), b(1) : End Class"
            Dim src2 = "Class C : Dim a((<N:0.0>Function(n) n + 1</N:0.0>)(2)), b(1) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a((       Function(n) n + 1        )(1))]@14 -> [a((       Function(n) n + 1        )(2))]@14")

            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), syntaxMap(0))})
        End Sub

        <Fact(), WorkItem(2543, "https://github.com/dotnet/roslyn/issues/2543")>
        Public Sub FieldInitializerUpdate_Lambdas_ImplicitCtor_AsNew1()
            Dim src1 = "Class C : Dim a, b As New C((<N:0.0>Function(n) n + 1</N:0.0>)(1))" & vbCrLf & "Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : Dim a, b As New C((<N:0.0>Function(n) n + 1</N:0.0>)(2))" & vbCrLf & "Sub New(a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_Lambdas_PartialDeclarationDelete_SingleDocument()
            Dim src1 = "
Partial Class C
    Dim x = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    Dim y = F(<N:0.1>Function(a) a + 10</N:0.1>)
End Class

Partial Class C
    Public Sub New()
    End Sub

    Shared Function F(x As Func(Of Integer, Integer))
        Return 1
    End Function
End Class
"

            Dim src2 = "
Partial Class C
    Dim x = F(<N:0.0>Function(a) a + 1</N:0.0>)
End Class

Partial Class C
    Dim y = F(<N:0.1>Function(a) a + 10</N:0.1>)

    Shared Function F(x As Func(Of Integer, Integer))
        Return 1
    End Function
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                {
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("F")),
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), syntaxMap(0))
                })
        End Sub

        <Fact>
        Public Sub FieldInitializerUpdate_ActiveStatements1()
            Dim src1 As String = "
Imports System

Class C
    Dim A As Integer = <N:0.0>1</N:0.0>
    Dim B As Integer = 1

    Public Sub New(a As Integer) 
        Console.WriteLine(1)
    End Sub

    Public Sub New(b As Boolean) 
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim src2 As String = "
Imports System

Class C
    Dim A As Integer = <N:0.0>1</N:0.0>
    Dim B As Integer = 2

    Public Sub New(a As Integer) 
        Console.WriteLine(1)
    End Sub

    Public Sub New(b As Boolean) 
        Console.WriteLine(1)
    End Sub
End Class"

            Dim edits = GetTopEdits(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(0), syntaxMap(0)),
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").Constructors(1), syntaxMap(0))
            })
        End Sub

        <Fact>
        Public Sub PropertyWithInitializer_SemanticError_Partial()
            Dim src1 = "
Partial Class C
    Partial Public ReadOnly Property P() As String
        Get
            Return 1
        End Get
    End Property
End Class

Partial Class C
    Partial Public ReadOnly Property P() As String
        Get
            Return 1
        End Get
    End Property
End Class
"
            Dim src2 = "
Partial Class C
    Partial Public ReadOnly Property P() As String
        Get
            Return 1
        End Get
    End Property
End Class

Partial Class C
    Partial Public ReadOnly Property P() As String
        Get
            Return 2
        End Get
    End Property

    Sub New()
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(ActiveStatementsDescription.Empty, semanticEdits:=
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) CType(c.GetMember(Of NamedTypeSymbol)("C").GetMembers("P").First(), IPropertySymbol).GetMethod),
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
            })
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
                Diagnostic(RudeEditKind.Renamed, "Event F", FeaturesResources.event_))
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
                Diagnostic(RudeEditKind.TypeUpdate, "Event E", FeaturesResources.event_),
                Diagnostic(RudeEditKind.TypeUpdate, "value As Action(Of T)", FeaturesResources.parameter),
                Diagnostic(RudeEditKind.TypeUpdate, "value As Action(Of T)", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub EventInsert_IntoLayoutClass_Sequential()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
End Class
"
            Dim src2 = "
Imports System
Imports System.Runtime.InteropServices

<StructLayoutAttribute(LayoutKind.Sequential)>
Class C
    Private Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemantics(
                semanticEdits:={SemanticEdit(SemanticEditKind.Insert, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember("E"))})
        End Sub

        <Fact>
        Public Sub Event_Delete()
            Dim src1 = "
Imports System
Imports System.Runtime.InteropServices

Class C
    Private Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class
"
            Dim src2 = "
Imports System
Imports System.Runtime.InteropServices

Class C
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Class C", DeletedSymbolDisplay(FeaturesResources.event_, "E")))
        End Sub

        <Fact>
        Public Sub Event_Partial_InsertDelete()
            Dim srcA1 = "Partial Class C : End Class"
            Dim srcB1 = "
Partial Class C
    Custom Event E As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class
"
            Dim srcA2 = srcB1
            Dim srcB2 = srcA1

            EditAndContinueValidation.VerifySemantics(
                {GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)},
                {
                    DocumentResults(
                        semanticEdits:=
                        {
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E").AddMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E").RemoveMethod),
                            SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("E").RaiseMethod)
                        }),
                    DocumentResults()
                })
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
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterRename_Ctor1()
            Dim src1 = "Class C : " & vbLf & "Public Sub New(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub New(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@26 -> [b]@26")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterRename_Operator1()
            Dim src1 = "Class C : " & vbLf & "Public Shared Operator CType(a As C) As Integer : End Operator : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Shared Operator CType(b As C) As Integer : End Operator : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@40 -> [b]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterRename_Operator2()
            Dim src1 = "Class C : " & vbLf & "Public Shared Operator +(a As C, b As C) As Integer" & vbLf & "Return 0 : End Operator : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Shared Operator +(a As C, x As C) As Integer" & vbLf & "Return 0 : End Operator : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [b]@44 -> [x]@44")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "x", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterRename_Property1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer, b As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer, x As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [b]@52 -> [x]@52")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "x", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a]@24 -> [b]@24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_Modifier()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(ByRef a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [ByRef a As Integer]@38 -> [a As Integer]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "a As Integer", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_AsClause1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Object) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@40 -> [As Object]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "a As Object", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_AsClause2()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(a) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Delete [As Integer]@40")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "a", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_DefaultValue1()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(a As Integer) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 0) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [a As Integer]@38 -> [Optional a As Integer = 0]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Optional a As Integer = 0", FeaturesResources.parameter))
        End Sub

        <Fact>
        Public Sub ParameterUpdate_DefaultValue2()
            Dim src1 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 0) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim src2 = "Class C : " & vbLf & "Public ReadOnly Property P(Optional a As Integer = 1) As Integer" & vbLf & "Get" & vbLf & "Return 0 : End Get : End Property : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [Optional a As Integer = 0]@38 -> [Optional a As Integer = 1]@38")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "Optional a As Integer = 1", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Insert, "a As Integer", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Insert, "ByRef b As Integer", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Insert, "(a As Integer)", VBFeaturesResources.parameters))
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
                Diagnostic(RudeEditKind.Delete, "Public Sub M()", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Delete, "Public Sub M(b As Integer)", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Delete, "Public Sub M", VBFeaturesResources.parameters))
        End Sub

        <Fact>
        Public Sub ParameterReorder()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(a As Integer, b As Integer) : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(b As Integer, a As Integer) : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [b As Integer]@38 -> @24")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "b As Integer", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Move, "b As Integer", FeaturesResources.parameter),
                Diagnostic(RudeEditKind.Renamed, "c", FeaturesResources.parameter))
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Delete, "a As Integer", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute),
                Diagnostic(RudeEditKind.Update, "B", FeaturesResources.attribute))
        End Sub

        <Fact>
        Public Sub ReturnValueAttributeUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As <A>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As <B>Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@35 -> [B]@35")

            edits.VerifyRudeDiagnostics(
               Diagnostic(RudeEditKind.Update, "B", FeaturesResources.attribute))
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
                Diagnostic(RudeEditKind.Insert, "<A>", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Delete, "Public Function M()", VBFeaturesResources.attributes))
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
                Diagnostic(RudeEditKind.Delete, "Public Function M()", VBFeaturesResources.as_clause))
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
                Diagnostic(RudeEditKind.Insert, "As <A>Integer", VBFeaturesResources.as_clause))
        End Sub

        <Fact>
        Public Sub FunctionAsClauseUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Function M() As Integer" & vbLf & "Return 0 : End Function : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Function M() As Object" & vbLf & "Return 0 : End Function : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [As Integer]@31 -> [As Object]@31")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Public Function M()", FeaturesResources.method))
        End Sub

#End Region

#Region "Method Type Parameter"

        <Fact>
        Public Sub MethodTypeParameterInsert1()
            Dim src1 = "Class C : " & vbLf & "Public Sub M()" & vbLf & "End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of A)()" & vbLf & "End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Insert [(Of A)]@23",
                "Insert [A]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "(Of A)", VBFeaturesResources.type_parameters))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterInsert2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A)()" & vbLf & "End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of A, B)()" & vbLf & "End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A)]@23 -> [(Of A, B)]@23",
                "Insert [B]@30")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Delete, "Public Sub M()", VBFeaturesResources.type_parameters))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterDelete2()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A, B)()" & vbLf & "End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B)()" & vbLf & "End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [(Of A, B)]@23 -> [(Of B)]@23",
                "Delete [A]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "Public Sub M(Of B)()", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterUpdate()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@27 -> [B]@27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub MethodTypeParameterReorder()
            Dim src1 = "Class C : " & vbLf & "Public Sub M(Of A, B)() : End Sub : End Class"
            Dim src2 = "Class C : " & vbLf & "Public Sub M(Of B, A)() : End Sub : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@30 -> @27")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.Renamed, "C", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Insert, "(Of A)", VBFeaturesResources.type_parameters))
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
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Delete, "Class C", VBFeaturesResources.type_parameters))
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
                Diagnostic(RudeEditKind.Delete, "Class C(Of B)", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterUpdate()
            Dim src1 = "Class C(Of A) : End Class"
            Dim src2 = "Class C(Of B) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Update [A]@11 -> [B]@11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", FeaturesResources.type_parameter))
        End Sub

        <Fact>
        Public Sub TypeTypeParameterReorder()
            Dim src1 = "Class C(Of A, B) : End Class"
            Dim src2 = "Class C(Of B, A) : End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyEdits(
                "Reorder [B]@14 -> @11")

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.Renamed, "C", FeaturesResources.type_parameter))
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
                Diagnostic(RudeEditKind.Insert, "As Class", FeaturesResources.type_constraint))
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
                Diagnostic(RudeEditKind.Insert, "As New", FeaturesResources.type_constraint))
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
                Diagnostic(RudeEditKind.Delete, "T", FeaturesResources.type_constraint))
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
                Diagnostic(RudeEditKind.Delete, "S", FeaturesResources.type_constraint))
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
                Diagnostic(RudeEditKind.Insert, "J", FeaturesResources.type_constraint),
                Diagnostic(RudeEditKind.Insert, "I", FeaturesResources.type_constraint))
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
