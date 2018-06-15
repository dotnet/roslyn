' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata
Imports Roslyn.Test.Utilities


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class TypeBindingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub ArrayTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as Integer()
    ' Public b() as String(,)(,,) is invalid 
    Public b as String()(,)(,,) 
End Class
    </file>
    <file name="b.vb">
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Assert.Equal(1, globalNSmembers.Length)
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim fieldA = DirectCast(membersOfC(1), FieldSymbol)
            Dim typeA = fieldA.Type
            Assert.Equal(TypeKind.Array, typeA.TypeKind)
            Dim arrayTypeA = DirectCast(typeA, ArrayTypeSymbol)
            Assert.Equal(1, arrayTypeA.Rank)
            Assert.True(arrayTypeA.IsSZArray)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int32), arrayTypeA.ElementType)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Array), arrayTypeA.BaseType)

            Dim fieldB = DirectCast(membersOfC(2), FieldSymbol)
            Dim typeB = fieldB.Type
            Assert.Equal(TypeKind.Array, typeB.TypeKind)
            Dim arrayTypeB = DirectCast(typeB, ArrayTypeSymbol)
            Assert.Equal(1, arrayTypeB.Rank)
            Dim arrayTypeB2 = DirectCast(arrayTypeB.ElementType, ArrayTypeSymbol)
            Assert.Equal(2, arrayTypeB2.Rank)
            Dim arrayTypeB3 = DirectCast(arrayTypeB2.ElementType, ArrayTypeSymbol)
            Assert.Equal(3, arrayTypeB3.Rank)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_String), arrayTypeB3.ElementType)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub NullableTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as Integer?
End Class
    </file>
    <file name="b.vb">
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Assert.Equal(1, globalNSmembers.Length)
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim fieldA = DirectCast(membersOfC(1), FieldSymbol)
            Dim typeA = fieldA.Type
            Assert.Equal(TypeKind.Structure, typeA.TypeKind)
            Dim namedTypeA = DirectCast(typeA, NamedTypeSymbol)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Nullable_T), namedTypeA.OriginalDefinition)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int32), namedTypeA.TypeArguments(0))

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub GlobalNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Namespace N        
    Public Class C
        Public Shared a as Q
        Public Shared b as Global.Q
        public Shared c as R
        public shared d as Global.R
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace N
    Public Class Q
    End Class
    Public Class R
    End Class
End Namespace   

Public Class Q
End Class 

Public Module M1
    Public Class R
    End Class
End Module
    
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(m) m.Name).ToArray()

            Dim moduleM = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim nsN = DirectCast(globalNSmembers(1), NamespaceSymbol)
            Dim classGlobalQ = DirectCast(globalNSmembers(2), NamedTypeSymbol)

            Dim membersOfM = moduleM.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classMDotR = DirectCast(membersOfM(0), NamedTypeSymbol)

            Dim membersOfN = nsN.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(membersOfN(0), NamedTypeSymbol)
            Dim classNDotQ = DirectCast(membersOfN(1), NamedTypeSymbol)
            Dim classNDotR = DirectCast(membersOfN(2), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim fieldA = DirectCast(membersOfC(1), FieldSymbol)
            Dim fieldB = DirectCast(membersOfC(2), FieldSymbol)
            Dim fieldC = DirectCast(membersOfC(3), FieldSymbol)
            Dim fieldD = DirectCast(membersOfC(4), FieldSymbol)

            Assert.Same(classNDotQ, fieldA.Type)
            Assert.Same(classGlobalQ, fieldB.Type)
            Assert.NotSame(classNDotQ, classGlobalQ)
            Assert.Same(classNDotR, fieldC.Type)
            Assert.Same(classMDotR, fieldD.Type)
            Assert.NotSame(classNDotR, classMDotR)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub BasicTypeName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System.Goo        
    Public Partial Class C
        Public Partial Class D
            Public Shared a as S
            Public Shared b%
            Public Shared c as D
            Public Shared d, d2 as C
            Public Shared e as R
            Public Shared f as Q
            Public Shared g as Environment
            public Shared h() As Q
            Public Shared i As Q()
        End Class
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace System
    Namespace Goo        
        Public Class Q
        End Class
        Public Partial Class C
            Public Partial Class D
                Public Class S
                End Class
            End Class
        End Class
    End Namespace

    Public Class R
    End Class
End Namespace
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim nsSystem = DirectCast(globalNSmembers(0), NamespaceSymbol)

            Dim systemMembers = nsSystem.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim nsGoo = DirectCast(systemMembers(0), NamespaceSymbol)
            Dim classR = DirectCast(systemMembers(1), NamedTypeSymbol)

            Dim gooMembers = nsGoo.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(gooMembers(0), NamedTypeSymbol)
            Dim classQ = DirectCast(gooMembers(1), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classD = DirectCast(cMembers(1), NamedTypeSymbol)

            Dim dMembers = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(dMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(dMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(dMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(dMembers(4), FieldSymbol)
            Dim fieldD2 = DirectCast(dMembers(5), FieldSymbol)
            Dim fieldE = DirectCast(dMembers(6), FieldSymbol)
            Dim fieldF = DirectCast(dMembers(7), FieldSymbol)
            Dim fieldG = DirectCast(dMembers(8), FieldSymbol)
            Dim fieldH = DirectCast(dMembers(9), FieldSymbol)
            Dim fieldI = DirectCast(dMembers(10), FieldSymbol)
            Dim classS = DirectCast(dMembers(11), NamedTypeSymbol)

            Assert.Same(classS, fieldA.Type)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int32), fieldB.Type)
            Assert.Same(classD, fieldC.Type)
            Assert.Same(classC, fieldD.Type)
            Assert.Same(classC, fieldD2.Type)
            Assert.Same(classR, fieldE.Type)
            Assert.Same(classQ, fieldF.Type)
            Assert.Equal("System.Environment", fieldG.Type.ToTestDisplayString())
            Dim typeH = fieldH.Type
            Assert.Equal(TypeKind.Array, typeH.TypeKind)
            Dim arrayTypeH = DirectCast(typeH, ArrayTypeSymbol)
            Assert.Equal(1, arrayTypeH.Rank)
            Assert.Same(classQ, arrayTypeH.ElementType)
            Assert.Equal(fieldH.Type, fieldI.Type)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub BasicTypeParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System.Goo        
    Public Partial Class C(Of T, U)
        Public Partial Class D(Of V)
            public shared j as V, k as U
        End Class
    End Class
End Namespace
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim nsSystem = DirectCast(globalNSmembers(0), NamespaceSymbol)

            Dim systemMembers = nsSystem.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim nsGoo = DirectCast(systemMembers(0), NamespaceSymbol)

            Dim gooMembers = nsGoo.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(gooMembers(0), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classD = DirectCast(cMembers(1), NamedTypeSymbol)

            Dim dMembers = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldJ = DirectCast(dMembers(1), FieldSymbol)
            Dim fieldK = DirectCast(dMembers(2), FieldSymbol)

            Assert.Same(classD.TypeParameters(0), fieldJ.Type)
            Assert.Same(classC.TypeParameters(1), fieldK.Type)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub GenericTypeName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System.Goo        
    Public Class C
        Public Shared a As Q(Of Environment, R)
        Public Shared b As IComparable(Of R)
        Friend Shared c As Gen(Of Gen(Of String, R), Integer)
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace System
    Namespace Goo        
        Public Class Q(Of T, U)
        End Class
    End Namespace

    Module W
       Public Class Gen(Of T, U)
       End Class
    End Module

    Public Class R
    End Class
End Namespace
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim nsSystem = DirectCast(globalNSmembers(0), NamespaceSymbol)

            Dim systemMembers = nsSystem.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim nsGoo = DirectCast(systemMembers(0), NamespaceSymbol)
            Dim classR = DirectCast(systemMembers(1), NamedTypeSymbol)

            Dim gooMembers = nsGoo.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(gooMembers(0), NamedTypeSymbol)
            Dim classQ = DirectCast(gooMembers(1), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)

            Dim typeA = DirectCast(fieldA.Type, NamedTypeSymbol)
            Assert.Equal("Q", typeA.Name)
            Assert.Same(classQ, typeA.OriginalDefinition)
            Assert.Same(classQ, typeA.ConstructedFrom)
            Assert.NotEqual(classQ, typeA)
            Assert.Equal("System.Environment", typeA.TypeArguments(0).ToTestDisplayString())
            Assert.Same(classR, typeA.TypeArguments(1))

            Dim typeB = DirectCast(fieldB.Type, NamedTypeSymbol)
            Assert.Equal("IComparable", typeB.Name)
            Assert.Equal("System.IComparable(Of System.R)", typeB.ToTestDisplayString())
            Assert.Equal("System.IComparable(Of In T)", typeB.OriginalDefinition.ToTestDisplayString())
            Assert.Same(classR, typeB.TypeArguments(0))

            Assert.Equal("System.W.Gen(Of System.W.Gen(Of System.String, System.R), System.Int32)", fieldC.Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub DottedTypeName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System.Goo        
    Public Partial Class C
        Public Partial Class D
            Public Shared a as D.S
            Public Shared b as Q(Of Integer).V
            Public Shared c as System.Environment
            Friend Shared d as R.SS
            Public Shared e as C.D.S
            Friend Shared f as R.CC
        End Class
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace System
    Namespace Goo        
        Public Class Q(Of T)
            Public Class V
            End Class
        End Class
        Public Partial Class C
            Public Partial Class D
                Public Class S
                End Class
            End Class
        End Class
    End Namespace

    Namespace R
        Class SS
        End Class

        Module W
           Class CC
           End Class
        End Module
    End Namespace
End Namespace
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim nsSystem = DirectCast(globalNSmembers(0), NamespaceSymbol)

            Dim systemMembers = nsSystem.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim nsGoo = DirectCast(systemMembers(0), NamespaceSymbol)
            Dim nsR = DirectCast(systemMembers(1), NamespaceSymbol)

            Dim rMembers = nsR.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classSS = DirectCast(rMembers(0), NamedTypeSymbol)
            Dim moduleW = DirectCast(rMembers(1), NamedTypeSymbol)

            Dim wMembers = moduleW.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classCC = DirectCast(wMembers(0), NamedTypeSymbol)

            Dim gooMembers = nsGoo.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(gooMembers(0), NamedTypeSymbol)
            Dim classQ = DirectCast(gooMembers(1), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classD = DirectCast(cMembers(1), NamedTypeSymbol)

            Dim qMembers = classQ.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classV = DirectCast(qMembers(1), NamedTypeSymbol)

            Dim dMembers = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(dMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(dMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(dMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(dMembers(4), FieldSymbol)
            Dim fieldE = DirectCast(dMembers(5), FieldSymbol)
            Dim fieldF = DirectCast(dMembers(6), FieldSymbol)
            Dim classS = DirectCast(dMembers(7), NamedTypeSymbol)

            Assert.Same(classS, fieldA.Type)

            Dim typeB = DirectCast(fieldB.Type, NamedTypeSymbol)
            Assert.Equal(classV, typeB.OriginalDefinition)
            Dim containerTypeB = DirectCast(typeB.ContainingSymbol, NamedTypeSymbol)
            Assert.Equal("Q", containerTypeB.Name)
            Assert.Same(classQ, containerTypeB.OriginalDefinition)
            Assert.Same(classQ, containerTypeB.ConstructedFrom)
            Assert.NotEqual(classQ, containerTypeB)
            Assert.Same(sourceMod.GetCorLibType(SpecialType.System_Int32), containerTypeB.TypeArguments(0))

            Assert.Equal("System.Environment", fieldC.Type.ToTestDisplayString())
            Assert.Same(classSS, fieldD.Type)
            Assert.Same(classS, fieldE.Type)

            Assert.Same(classCC, fieldF.Type)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub DottedGenericTypeName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System.Goo.Bar        
    Public Class C
        Public Shared a As A.Q(Of Environment, R)
        Public Shared b As System.IComparable(Of R)
        Friend Shared c As System.Gen(Of System.Gen(Of String, R), Integer)
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace System
    Namespace Goo   
        Public Class A     
            Public Class Q(Of T, U)
            End Class
        End Class
    End Namespace

    Public Class R
    End Class

    Module W
        Class Gen(Of T, U)
        End Class
    End Module
End Namespace
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim nsSystem = DirectCast(globalNSmembers(0), NamespaceSymbol)

            Dim systemMembers = nsSystem.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim nsGoo = DirectCast(systemMembers(0), NamespaceSymbol)
            Dim classR = DirectCast(systemMembers(1), NamedTypeSymbol)

            Dim gooMembers = nsGoo.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classA = DirectCast(gooMembers(0), NamedTypeSymbol)
            Dim nsBar = DirectCast(gooMembers(1), NamespaceSymbol)

            Dim aMembers = classA.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classQ = DirectCast(aMembers(1), NamedTypeSymbol)

            Dim barMembers = nsBar.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(barMembers(0), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)

            Dim typeA = DirectCast(fieldA.Type, NamedTypeSymbol)
            Assert.Equal("Q", typeA.Name)
            Assert.Same(classQ, typeA.OriginalDefinition)
            Assert.Same(classQ, typeA.ConstructedFrom)
            Assert.NotEqual(classQ, typeA)
            Assert.Equal("System.Environment", typeA.TypeArguments(0).ToTestDisplayString())
            Assert.Same(classR, typeA.TypeArguments(1))

            Dim typeB = DirectCast(fieldB.Type, NamedTypeSymbol)
            Assert.Equal("IComparable", typeB.Name)
            Assert.Equal("System.IComparable(Of System.R)", typeB.ToTestDisplayString())
            Assert.Equal("System.IComparable(Of In T)", typeB.OriginalDefinition.ToTestDisplayString())
            Assert.Same(classR, typeB.TypeArguments(0))

            Assert.Equal("System.W.Gen(Of System.W.Gen(Of System.String, System.R), System.Int32)", fieldC.Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ImportMembersAtFileLevel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System.Collections.Generic
Imports System.TimeZoneInfo  
Imports N1  
Imports N1.N2.Gen(Of String, Integer)    
Public Class C
    Public Shared a as List(Of Integer)
    Public Shared b as AdjustmentRule
    Friend Shared c as N2.Quux
    Friend Shared d As Sc
    Friend Shared e As Sh
    Friend Shared f as Di
End Class
    </file>
    <file name="b.vb">
Namespace N1.N2
    Class Quux
    End Class
    Class Gen(Of T, U)
       Class Sc
       End Class
    End Class
End Namespace

Namespace N1
    Module Zap
        Class Di
        End Class
    End Module

    Class Sc(Of T)
    End Class
    Class Sh
    End Class
End Namespace

Class Sh
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(m) m.Name).ToArray()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(cMembers(4), FieldSymbol)
            Dim fieldE = DirectCast(cMembers(5), FieldSymbol)
            Dim fieldF = DirectCast(cMembers(6), FieldSymbol)

            Assert.Equal("System.Collections.Generic.List(Of System.Int32)", fieldA.Type.ToTestDisplayString())
            Assert.Equal("System.TimeZoneInfo.AdjustmentRule", fieldB.Type.ToTestDisplayString())
            Assert.Equal("N1.N2.Quux", fieldC.Type.ToTestDisplayString())
            Assert.Equal("N1.N2.Gen(Of System.String, System.Int32).Sc", fieldD.Type.ToTestDisplayString())
            Assert.Equal("Sh", fieldE.Type.ToTestDisplayString())
            Assert.Equal("N1.Zap.Di", fieldF.Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ImportMembersAtProjectLevel()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse(
                    {"System.Collections.Generic",
                    "System.TimeZoneInfo",
                    "N1",
                    "N1.N2.Gen(Of String, Integer)"}))

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as List(Of Integer)
    Public Shared b as AdjustmentRule
    Friend Shared c as N2.Quux
    Friend Shared d As Scooby
    Friend Shared e As Shaggy
    Friend Shared f as Dingle
End Class
    </file>
    <file name="b.vb">
Namespace N1.N2
    Class Quux
    End Class
    Class Gen(Of T, U)
       Class Scooby
       End Class
    End Class
End Namespace

Namespace N1
    Module Zap
        Class Dingle
        End Class
    End Module

    Class Scooby(Of T)
    End Class
    Class Shaggy
    End Class
End Namespace

Class Shaggy
End Class
    </file>
</compilation>, options)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(m) m.Name).ToArray()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(cMembers(4), FieldSymbol)
            Dim fieldE = DirectCast(cMembers(5), FieldSymbol)
            Dim fieldF = DirectCast(cMembers(6), FieldSymbol)

            Assert.Equal("System.Collections.Generic.List(Of System.Int32)", fieldA.Type.ToTestDisplayString())
            Assert.Equal("System.TimeZoneInfo.AdjustmentRule", fieldB.Type.ToTestDisplayString())
            Assert.Equal("N1.N2.Quux", fieldC.Type.ToTestDisplayString())
            Assert.Equal("N1.N2.Gen(Of System.String, System.Int32).Scooby", fieldD.Type.ToTestDisplayString())
            Assert.Equal("Shaggy", fieldE.Type.ToTestDisplayString())
            Assert.Equal("N1.Zap.Dingle", fieldF.Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ImportAliasesAtFileLevel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Imports System.Collections
Imports IO=System.IO
Imports TZI=System.TimeZoneInfo  
Imports ArrayList=System.Collections.Generic.List(Of Integer) 
Imports DD=System.Console 
Public Class C
    Public Shared a as ArrayList
    Public Shared b as TZI.AdjustmentRule
    Public Shared c as IO.File
    Public Shared d As DD
End Class
Public Class DD
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim classDD = DirectCast(globalNSmembers(1), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(cMembers(4), FieldSymbol)

            Assert.Equal("System.Collections.Generic.List(Of System.Int32)", fieldA.Type.ToTestDisplayString())
            Assert.Equal("System.TimeZoneInfo.AdjustmentRule", fieldB.Type.ToTestDisplayString())
            Assert.Equal("System.IO.File", fieldC.Type.ToTestDisplayString())
            Assert.Same(classDD, fieldD.Type)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<expected>
  BC31403: Imports alias 'DD' conflicts with 'DD' declared in the root namespace.
Imports DD=System.Console 
        ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub


        <Fact>
        Public Sub ImportAliasesAtProjectLevel()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse(
                    {"System.Collections",
                     "IO=System.IO",
                     "TZI=System.TimeZoneInfo",
                     "ArrayList=System.Collections.Generic.List(Of Integer)",
                     "DD=System.Console"}))

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as ArrayList
    Public Shared b as TZI.AdjustmentRule
    Public Shared c as IO.File
    Public Shared d As DD
End Class
Public Class DD
End Class
    </file>
</compilation>, options:=options)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)

            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim classDD = DirectCast(globalNSmembers(1), NamedTypeSymbol)

            Dim cMembers = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim fieldA = DirectCast(cMembers(1), FieldSymbol)
            Dim fieldB = DirectCast(cMembers(2), FieldSymbol)
            Dim fieldC = DirectCast(cMembers(3), FieldSymbol)
            Dim fieldD = DirectCast(cMembers(4), FieldSymbol)

            Assert.Equal("System.Collections.Generic.List(Of System.Int32)", fieldA.Type.ToTestDisplayString())
            Assert.Equal("System.TimeZoneInfo.AdjustmentRule", fieldB.Type.ToTestDisplayString())
            Assert.Equal("System.IO.File", fieldC.Type.ToTestDisplayString())
            Assert.Same(classDD, fieldD.Type)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
<errors>
  BC31403: Error in project-level import 'DD=System.Console' at 'DD=System.Console' : Imports alias 'DD' conflicts with 'DD' declared in the root namespace.
</errors>)
        End Sub

        <Fact>
        Public Sub BasicTypeNameErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as Q$
    Public Shared b as P
    Public Shared c as R
    Public Shared d as Elvis
    Public Shared e As S
End Class
    </file>
    <file name="b.vb">
Public Class Q
End Class

Public Class P(Of T)
End Class

Namespace R
End Namespace

Module TMod
    Class S
    End Class
End Module

Module TMod2
    Class S
    End Class
End Module
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30468: Type declaration characters are not valid in this context.
    Public Shared a as Q$
                       ~~
BC32042: Too few type arguments to 'P(Of T)'.
    Public Shared b as P
                       ~
BC30182: Type expected.
    Public Shared c as R
                       ~
BC30002: Type 'Elvis' is not defined.
    Public Shared d as Elvis
                       ~~~~~
BC30562: 'S' is ambiguous between declarations in Modules 'TMod, TMod2'.
    Public Shared e As S
                       ~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub


        <Fact>
        Public Sub GenericTypeNameErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as P(Of Integer, R, String)
    Public Shared b as P(Of String)
    Public Shared c as P%(Of Q, Q)
    public Shared d As R(Of Q)
    public shared e As Elvis(Of Q)
End Class
    </file>
    <file name="b.vb">
Public Class Q
End Class

Public Class P(Of T, U)
End Class

Namespace R
End Namespace
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC32043: Too many type arguments to 'P(Of T, U)'.
    Public Shared a as P(Of Integer, R, String)
                       ~~~~~~~~~~~~~~~~~~~~~~~~
BC30182: Type expected.
    Public Shared a as P(Of Integer, R, String)
                                     ~
BC32042: Too few type arguments to 'P(Of T, U)'.
    Public Shared b as P(Of String)
                       ~~~~~~~~~~~~
BC30468: Type declaration characters are not valid in this context.
    Public Shared c as P%(Of Q, Q)
                       ~~
BC32045: 'R' has no type parameters and so cannot have type arguments.
    public Shared d As R(Of Q)
                       ~~~~~~~
BC30002: Type 'Elvis' is not defined.
    public shared e As Elvis(Of Q)
                       ~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub DottedTypeNameErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Public Class C
    Public Shared a as Banana.Orange
    Public Shared b as Banana.Orange.Apple
    Public Shared c as R.Apple
    Public Shared d as Q.I
    Public Shared e as Q.I.Apple
    Public Shared f as Q.I.J
    Friend Shared g as R.V%
    Friend Shared h as R$.V
End Class
    </file>
    <file name="b.vb">
Public Class Q
    Public Class I(Of T)
        Public Class J
        End Class
    End Class
End Class

Public Class P(Of T, U)
End Class

Namespace R
    Class V
    End Class
End Namespace
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30002: Type 'Banana.Orange' is not defined.
    Public Shared a as Banana.Orange
                       ~~~~~~~~~~~~~
BC30002: Type 'Banana.Orange.Apple' is not defined.
    Public Shared b as Banana.Orange.Apple
                       ~~~~~~~~~~~~~~~~~~~
BC30002: Type 'R.Apple' is not defined.
    Public Shared c as R.Apple
                       ~~~~~~~
BC32042: Too few type arguments to 'Q.I(Of T)'.
    Public Shared d as Q.I
                       ~~~
BC32042: Too few type arguments to 'Q.I(Of T)'.
    Public Shared e as Q.I.Apple
                       ~~~
BC32042: Too few type arguments to 'Q.I(Of T)'.
    Public Shared f as Q.I.J
                       ~~~
BC30468: Type declaration characters are not valid in this context.
    Friend Shared g as R.V%
                         ~~
BC30468: Type declaration characters are not valid in this context.
    Friend Shared h as R$.V
                       ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub GenericDottedTypeNameErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace N        
    Public Class C
        dim a as Q.I%(Of Integer)
        dim b as Q%.I(Of Integer)
        Dim c As Q.Banana(Of Integer)
        dim d As M.P(Of Q$)
        dim e as M.P(Of Integer, String).Z(Of String)
    End Class
End Namespace
    </file>
    <file name="b.vb">
Public Class Q
    Public Class I(Of T)
        Public Class J
        End Class
    End Class
End Class

Namespace N.M
    Public Class P(Of T, U)
    End Class
End Namespace

Namespace R
    Class V
    End Class
End Namespace
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30468: Type declaration characters are not valid in this context.
        dim a as Q.I%(Of Integer)
                   ~~
BC30468: Type declaration characters are not valid in this context.
        dim b as Q%.I(Of Integer)
                 ~~
BC30002: Type 'Q.Banana' is not defined.
        Dim c As Q.Banana(Of Integer)
                 ~~~~~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'P(Of T, U)'.
        dim d As M.P(Of Q$)
                 ~~~~~~~~~~
BC30468: Type declaration characters are not valid in this context.
        dim d As M.P(Of Q$)
                        ~~
BC30002: Type 'M.P.Z' is not defined.
        dim e as M.P(Of Integer, String).Z(Of String)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ImportResolutionErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System.Collections
Imports System.Collections.Generic
Imports N, N2
Class A
    public a as IEnumerable(Of String)
    public b as IEnumerable
    public c as IComparer ' ambiguous
    public d As Goo    
End Class        
    </file>
    <file name="b.vb">
Class HashTable
End Class
Namespace N
    Interface IComparer
    End Interface
    Module K
        Class Goo
        End Class
    End Module
End Namespace 
Namespace N2
    Class IComparer
    End Class
    Module L
        Class Goo
        End Class
    End Module
End Namespace       
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30561: 'IComparer' is ambiguous, imported from the namespaces or types 'System.Collections, N, N2'.
    public c as IComparer ' ambiguous
                ~~~~~~~~~
BC30561: 'Goo' is ambiguous, imported from the namespaces or types 'N.K, N2.L'.
    public d As Goo    
                ~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ProjectImportResolutionErrors()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse(
                    {"System.Collections",
                    "System.Collections.Generic",
                    "N",
                    "N2"}))

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    public a as IEnumerable(Of String)
    public b as IEnumerable
    public c as IComparer ' ambiguous
    public d as Goo 'ambiguous
End Class        
    </file>
    <file name="b.vb">
Class HashTable
End Class
Namespace N
    Interface IComparer
    End Interface
    Module K
        Class Goo
        End Class
    End Module
End Namespace 
Namespace N2
    Class IComparer
    End Class
    Module L
        Class Goo
        End Class
    End Module
End Namespace       
    </file>
</compilation>, options)

            Dim expectedErrors = <errors>
BC30561: 'IComparer' is ambiguous, imported from the namespaces or types 'System.Collections, N, N2'.
    public c as IComparer ' ambiguous
                ~~~~~~~~~
BC30561: 'Goo' is ambiguous, imported from the namespaces or types 'N.K, N2.L'.
    public d as Goo 'ambiguous
                ~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub AliasResolutionErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Imports System.Collections
Imports ArrayList=System.Collections.Generic.List(Of Object)
Imports HT=System.Collections.HashTable
Class A
    public a as ArrayList
    public b as HT
    public c as HT(Of String)      
    public d as ArrayList(Of String)      
End Class        
    </file>
    <file name="b.vb">
Class HT
End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31403: Imports alias 'HT' conflicts with 'HT' declared in the root namespace.
Imports HT=System.Collections.HashTable
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32045: 'HT' has no type parameters and so cannot have type arguments.
    public c as HT(Of String)      
                ~~~~~~~~~~~~~
BC32045: 'List(Of Object)' has no type parameters and so cannot have type arguments.
    public d as ArrayList(Of String)      
                ~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ProjectAliasResolutionErrors()
            Dim options = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse(
                    {"System.Collections",
                    "ArrayList=System.Collections.Generic.List(Of Object)",
                    "HT=System.Collections.HashTable"}))
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Class A
    public a as ArrayList
    public b as HT
    public c as HT(Of String)      
    public d as ArrayList(Of String)      
End Class        
    </file>
    <file name="b.vb">
Class HT
End Class
    </file>
</compilation>, options:=options)

            Dim expectedErrors = <errors>
BC31403: Error in project-level import 'HT=System.Collections.HashTable' at 'HT=System.Collections.HashTable' : Imports alias 'HT' conflicts with 'HT' declared in the root namespace.
BC32045: 'HT' has no type parameters and so cannot have type arguments.
    public c as HT(Of String)      
                ~~~~~~~~~~~~~
BC32045: 'List(Of Object)' has no type parameters and so cannot have type arguments.
    public d as ArrayList(Of String)      
                ~~~~~~~~~~~~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub GlobalNamespace1()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("RootNS")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Global.F.G
    Class H
    End Class
End Namespace

Namespace A
    Class B
    End Class
End Namespace

Namespace Global.C
    Namespace D
       Class E
       End Class
    End Namespace
    Class I
    End Class
End Namespace

Namespace Global
    Namespace C
        Namespace J
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>, options:=options)

            Dim globalNS = compilation.GlobalNamespace

            ' "RootNS" is inside global namespace
            Dim rootNS = DirectCast(globalNS.GetMembers("RootNS").Single(), NamespaceSymbol)
            Assert.Equal("RootNS", rootNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(rootNS.ContainingSymbol, NamespaceSymbol)))

            ' "Namespace F" is inside global namespace
            Dim fNS = DirectCast(globalNS.GetMembers("F").Single(), NamespaceSymbol)
            Assert.Equal("F", fNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(fNS.ContainingSymbol, NamespaceSymbol)))

            ' "Namespace C" is inside global namespace
            Dim cNS = DirectCast(globalNS.GetMembers("C").Single(), NamespaceSymbol)
            Assert.Equal("C", cNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(cNS.ContainingSymbol, NamespaceSymbol)))

            ' Namespace A is inside RootNS.
            Dim aNS = DirectCast(rootNS.GetMembers("A").Single(), NamespaceSymbol)
            Assert.Equal("A", aNS.Name)
            Assert.Equal(rootNS, aNS.ContainingSymbol)

            ' Class B is inside Namespace A.
            Dim bClass = DirectCast(aNS.GetMembers("B").Single(), NamedTypeSymbol)
            Assert.Equal("B", bClass.Name)
            Assert.Equal(aNS, bClass.ContainingSymbol)

            ' "Namespace D" is inside "Namespace C"
            Dim dNS = DirectCast(cNS.GetMembers("D").Single(), NamespaceSymbol)
            Assert.Equal("D", dNS.Name)
            Assert.Equal(cNS, dNS.ContainingSymbol)

            ' Class I is inside Namespace C.
            Dim iClass = DirectCast(cNS.GetMembers("I").Single(), NamedTypeSymbol)
            Assert.Equal("I", iClass.Name)
            Assert.Equal(cNS, iClass.ContainingSymbol)

            ' Namespace J is inside Namespace C.
            Dim jNS = DirectCast(cNS.GetMembers("J").Single(), NamespaceSymbol)
            Assert.Equal("J", jNS.Name)
            Assert.Equal(cNS, jNS.ContainingSymbol)

            ' Class E is inside Namespace D.
            Dim eClass = DirectCast(dNS.GetMembers("E").Single(), NamedTypeSymbol)
            Assert.Equal("E", eClass.Name)
            Assert.Equal(dNS, eClass.ContainingSymbol)

            ' "Namespace G" is inside "Namespace F"
            Dim gNS = DirectCast(fNS.GetMembers("G").Single(), NamespaceSymbol)
            Assert.Equal("G", gNS.Name)
            Assert.Equal(fNS, gNS.ContainingSymbol)

            ' Class H is inside Namespace G.
            Dim hClass = DirectCast(gNS.GetMembers("H").Single(), NamedTypeSymbol)
            Assert.Equal("H", hClass.Name)
            Assert.Equal(gNS, hClass.ContainingSymbol)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub GlobalNamespace2()
            Dim options = TestOptions.ReleaseDll ' no global namespace

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Global.F.G
    Class H
    End Class
End Namespace

Namespace A
    Class B
    End Class
End Namespace

Namespace Global.C
    Namespace D
       Class E
       End Class
    End Namespace
    Class I
    End Class
End Namespace
    </file>
</compilation>, options:=options)

            Dim globalNS = compilation.GlobalNamespace

            ' "Namespace F" is inside global namespace
            Dim fNS = DirectCast(globalNS.GetMembers("F").Single(), NamespaceSymbol)
            Assert.Equal("F", fNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(fNS.ContainingSymbol, NamespaceSymbol)))

            ' "Namespace C" is inside global namespace
            Dim cNS = DirectCast(globalNS.GetMembers("C").Single(), NamespaceSymbol)
            Assert.Equal("C", cNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(cNS.ContainingSymbol, NamespaceSymbol)))

            ' Namespace A is inside global namespace.
            Dim aNS = DirectCast(globalNS.GetMembers("A").Single(), NamespaceSymbol)
            Assert.Equal("A", aNS.Name)
            Assert.Equal(globalNS, compilation.GetCompilationNamespace(DirectCast(aNS.ContainingSymbol, NamespaceSymbol)))

            ' Class B is inside Namespace A.
            Dim bClass = DirectCast(aNS.GetMembers("B").Single(), NamedTypeSymbol)
            Assert.Equal("B", bClass.Name)
            Assert.Equal(aNS, bClass.ContainingSymbol)

            ' "Namespace D" is inside "Namespace C"
            Dim dNS = DirectCast(cNS.GetMembers("D").Single(), NamespaceSymbol)
            Assert.Equal("D", dNS.Name)
            Assert.Equal(cNS, dNS.ContainingSymbol)

            ' Class I is inside Namespace C.
            Dim iClass = DirectCast(cNS.GetMembers("I").Single(), NamedTypeSymbol)
            Assert.Equal("I", iClass.Name)
            Assert.Equal(cNS, iClass.ContainingSymbol)

            ' Class E is inside Namespace D.
            Dim eClass = DirectCast(dNS.GetMembers("E").Single(), NamedTypeSymbol)
            Assert.Equal("E", eClass.Name)
            Assert.Equal(dNS, eClass.ContainingSymbol)

            ' "Namespace G" is inside "Namespace F"
            Dim gNS = DirectCast(fNS.GetMembers("G").Single(), NamespaceSymbol)
            Assert.Equal("G", gNS.Name)
            Assert.Equal(fNS, gNS.ContainingSymbol)

            ' Class H is inside Namespace G.
            Dim hClass = DirectCast(gNS.GetMembers("H").Single(), NamedTypeSymbol)
            Assert.Equal("H", hClass.Name)
            Assert.Equal(gNS, hClass.ContainingSymbol)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub Error31544InNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Goo
    Namespace Global.NotOk1
    End Namespace
    Namespace Global
    End Namespace
End Namespace

Namespace Global.ThisIsOk
    Namespace Global.NotOk2
    End Namespace
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31544: Global namespace may not be nested in another namespace.
    Namespace Global.NotOk1
              ~~~~~~
BC31544: Global namespace may not be nested in another namespace.
    Namespace Global
              ~~~~~~
BC31544: Global namespace may not be nested in another namespace.
    Namespace Global.NotOk2
              ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error30468InNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Goo%
End Namespace

Namespace A.B.Cat$.Dog@.E
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30468: Type declaration characters are not valid in this context.
Namespace Goo%
          ~~~~
BC30468: Type declaration characters are not valid in this context.
Namespace A.B.Cat$.Dog@.E
              ~~~~
BC30468: Type declaration characters are not valid in this context.
Namespace A.B.Cat$.Dog@.E
                   ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error30231()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1
End Interface
Module M1
    Implements I1
End Module
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30231: 'Implements' not valid in Modules.
    Implements I1
    ~~~~~~~~~~~~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Error30232()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Class cls1
    Implements Object
End Class
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30232: Implemented type must be an interface.
    Implements Object
               ~~~~~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Error30354()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1
    Inherits System.Collections.IEnumerable, Object
End Interface
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30354: Interface can inherit only from another interface.
    Inherits System.Collections.IEnumerable, Object
                                             ~~~~~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Error31033()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1

End Interface

Class Cls1
    Implements I1, I1
End Class
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31033: Interface 'I1' can be implemented only once by this type.
    Implements I1, I1
                   ~~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Error32056()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1

End Interface

Class Cls1(of T)
    Implements I1, T
End Class
            </file>
</compilation>)

            Dim expectedErrors = <errors>
BC32056: Type parameter not allowed in 'Implements' clause.
    Implements I1, T
                   ~
                                             </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Error30584()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1   
End Interface
Interface A
    Inherits I1, I1
End Interface  
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30584: 'I1' cannot be inherited more than once.
    Inherits I1, I1
                 ~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

    End Class
End Namespace
