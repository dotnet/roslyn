' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ContainerTests
        Inherits BasicTestBase

        ' Check that "basRootNS" is actually a bad root namespace
        Private Sub BadDefaultNS(badRootNS As String)
            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace(badRootNS).Errors,
<expected>
BC2014: the value '<%= badRootNS %>' is invalid for option 'RootNamespace'
</expected>)
        End Sub

        <Fact>
        Public Sub SimpleAssembly()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Banana">
    <file name="b.vb">
        Namespace NS
            Public Class Goo
            End Class
        End Namespace
    </file>
</compilation>)

            Dim sym = compilation.Assembly
            Assert.Equal("Banana", sym.Name)
            Assert.Equal("Banana, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", sym.ToTestDisplayString())
            Assert.Equal(String.Empty, sym.GlobalNamespace.Name)
            Assert.Equal(SymbolKind.Assembly, sym.Kind)
            Assert.Equal(Accessibility.NotApplicable, sym.DeclaredAccessibility)
            Assert.False(sym.IsShared)
            Assert.False(sym.IsOverridable)
            Assert.False(sym.IsOverrides)
            Assert.False(sym.IsMustOverride)
            Assert.False(sym.IsNotOverridable)
            Assert.Null(sym.ContainingAssembly)
            Assert.Null(sym.ContainingSymbol)
        End Sub

        <WorkItem(537302, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537302")>
        <Fact>
        Public Sub SourceModule()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Banana">
    <file name="m.vb">
        Namespace NS
            Public Class Goo
            End Class
        End Namespace
    </file>
</compilation>)

            Dim sym = compilation.SourceModule
            Assert.Equal("Banana.dll", sym.Name)
            Assert.Equal(String.Empty, sym.GlobalNamespace.Name)
            Assert.Equal(SymbolKind.NetModule, sym.Kind)
            Assert.Equal(Accessibility.NotApplicable, sym.DeclaredAccessibility)
            Assert.False(sym.IsShared)
            Assert.False(sym.IsOverridable)
            Assert.False(sym.IsOverrides)
            Assert.False(sym.IsMustOverride)
            Assert.False(sym.IsNotOverridable)
            Assert.Equal("Banana", sym.ContainingAssembly.Name)
            Assert.Equal("Banana", sym.ContainingSymbol.Name)
        End Sub

        <WorkItem(537421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537421")>
        <Fact>
        Public Sub StandardModule()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Banana">
    <file name="m.vb">
    Namespace NS
        Module MGoo
          Dim A As Integer
          Sub MySub()
          End Sub
          Function Func(x As Long) As Long
            Return x
          End Function
        End Module
    End Namespace
    </file>
</compilation>)

            Dim ns As NamespaceSymbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers("NS").Single(), NamespaceSymbol)
            Dim sym1 = ns.GetMembers("MGoo").Single()
            Assert.Equal("MGoo", sym1.Name)
            Assert.Equal("NS.MGoo", sym1.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sym1.Kind)
            ' default - Friend
            Assert.Equal(Accessibility.Friend, sym1.DeclaredAccessibility)
            Assert.False(sym1.IsShared)
            Assert.False(sym1.IsOverridable)
            Assert.False(sym1.IsOverrides)
            Assert.False(sym1.IsMustOverride)
            Assert.False(sym1.IsNotOverridable)
            Assert.Equal("Banana", sym1.ContainingAssembly.Name)
            Assert.Equal("NS", sym1.ContainingSymbol.Name)

            ' module member
            Dim smod = DirectCast(sym1, NamedTypeSymbol)
            Dim sym2 = DirectCast(smod.GetMembers("A").Single(), FieldSymbol)
            ' default - Private
            Assert.Equal(Accessibility.Private, sym2.DeclaredAccessibility)
            Assert.True(sym2.IsShared)

            Dim sym3 = DirectCast(smod.GetMembers("MySub").Single(), MethodSymbol)
            Dim sym4 = DirectCast(smod.GetMembers("Func").Single(), MethodSymbol)
            ' default - Public
            Assert.Equal(Accessibility.Public, sym3.DeclaredAccessibility)
            Assert.Equal(Accessibility.Public, sym4.DeclaredAccessibility)
            Assert.True(sym3.IsShared)
            Assert.True(sym4.IsShared)

            ' shared cctor
            'sym4 = DirectCast(smod.GetMembers(WellKnownMemberNames.StaticConstructorName).Single(), MethodSymbol)

        End Sub

        ' Check that we disallow certain kids of bad root namespaces
        <Fact>
        Public Sub BadDefaultNSTest()
            BadDefaultNS("Goo.7")
            BadDefaultNS("Goo..Bar")
            BadDefaultNS(".X")
            BadDefaultNS("$")
        End Sub

        ' Check that parse errors are reported 
        <Fact>
        Public Sub NamespaceParseErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Banana">
    <file name="a.vb">
        Imports System.7
    </file>
    <file name="b.vb">
        Namespace Goo
        End Class
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30205: End of statement expected.
Imports System.7
              ~~
BC30626: 'Namespace' statement must end with a matching 'End Namespace'.
Namespace Goo
~~~~~~~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
        End Class
        ~~~~~~~~~
                                 </errors>

            CompilationUtils.AssertTheseParseDiagnostics(compilation, expectedErrors)
        End Sub

        ' Check namespace symbols
        <Fact>
        Public Sub NSSym()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Namespace A
End Namespace

Namespace C
End Namespace
    </file>
    <file name="b.vb">
Namespace A.B
End Namespace

Namespace a.B
End Namespace

Namespace e
End Namespace
    </file>
    <file name="c.vb">
Namespace A.b.D
End Namespace
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Assert.Equal("", globalNS.Name)
            Assert.Equal(SymbolKind.Namespace, globalNS.Kind)
            Assert.Equal(3, globalNS.GetMembers().Length())

            Dim members = globalNS.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name, IdentifierComparison.Comparer).ToArray()
            Dim membersA = globalNS.GetMembers("a")
            Dim membersC = globalNS.GetMembers("c")
            Dim membersE = globalNS.GetMembers("E")
            Assert.Equal(3, members.Length)
            Assert.Equal(1, membersA.Length)
            Assert.Equal(1, membersC.Length)
            Assert.Equal(1, membersE.Length)
            Assert.True(members.SequenceEqual(membersA.Concat(membersC).Concat(membersE).AsEnumerable()))
            Assert.Equal("a", membersA.First().Name, IdentifierComparison.Comparer)
            Assert.Equal("C", membersC.First().Name, IdentifierComparison.Comparer)
            Assert.Equal("E", membersE.First().Name, IdentifierComparison.Comparer)

            Dim nsA As NamespaceSymbol = DirectCast(membersA.First(), NamespaceSymbol)
            Assert.Equal("A", nsA.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsA.Kind)
            Assert.Equal(1, nsA.GetMembers().Length())

            Dim nsB As NamespaceSymbol = DirectCast(nsA.GetMembers().First(), NamespaceSymbol)
            Assert.Equal("B", nsB.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsB.Kind)
            Assert.Equal(1, nsB.GetMembers().Length())

            Dim nsD As NamespaceSymbol = DirectCast(nsB.GetMembers().First(), NamespaceSymbol)
            Assert.Equal("D", nsD.Name)
            Assert.Equal(SymbolKind.Namespace, nsD.Kind)
            Assert.Equal(0, nsD.GetMembers().Length())

            AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC40055: Casing of namespace name 'a' does not match casing of namespace name 'A' in 'a.vb'.
Namespace a.B
          ~
BC40055: Casing of namespace name 'b' does not match casing of namespace name 'B' in 'b.vb'.
Namespace A.b.D
            ~
</expected>)
        End Sub

        ' Check namespace symbols in the presence of a root namespace
        <Fact>
        Public Sub NSSymWithRootNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Namespace A
End Namespace
Namespace E
End Namespace
    </file>
    <file name="b.vb">
Namespace A.B
End Namespace
Namespace C
End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithRootNamespace("Goo.Bar"))

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Assert.Equal("", globalNS.Name)
            Assert.Equal(SymbolKind.Namespace, globalNS.Kind)
            Assert.Equal(1, globalNS.GetMembers().Length())

            Dim members = globalNS.GetMembers()
            Dim nsGoo As NamespaceSymbol = DirectCast(members.First(), NamespaceSymbol)
            Assert.Equal("Goo", nsGoo.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsGoo.Kind)
            Assert.Equal(1, nsGoo.GetMembers().Length())

            Dim membersGoo = nsGoo.GetMembers()
            Dim nsBar As NamespaceSymbol = DirectCast(membersGoo.First(), NamespaceSymbol)
            Assert.Equal("Bar", nsBar.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsBar.Kind)
            Assert.Equal(3, nsBar.GetMembers().Length())

            Dim membersBar = nsBar.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name, IdentifierComparison.Comparer).ToArray()
            Dim membersA = nsBar.GetMembers("a")
            Dim membersC = nsBar.GetMembers("c")
            Dim membersE = nsBar.GetMembers("E")
            Assert.Equal(3, membersBar.Length)
            Assert.Equal(1, membersA.Length)
            Assert.Equal(1, membersC.Length)
            Assert.Equal(1, membersE.Length)
            Assert.True(membersBar.SequenceEqual(membersA.Concat(membersC).Concat(membersE).AsEnumerable()))
            Assert.Equal("a", membersA.First().Name, IdentifierComparison.Comparer)
            Assert.Equal("C", membersC.First().Name, IdentifierComparison.Comparer)
            Assert.Equal("E", membersE.First().Name, IdentifierComparison.Comparer)

            Dim nsA As NamespaceSymbol = DirectCast(membersA.First(), NamespaceSymbol)
            Assert.Equal("A", nsA.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsA.Kind)
            Assert.Equal(1, nsA.GetMembers().Length())

            Dim nsB As NamespaceSymbol = DirectCast(nsA.GetMembers().First(), NamespaceSymbol)
            Assert.Equal("B", nsB.Name, IdentifierComparison.Comparer)
            Assert.Equal(SymbolKind.Namespace, nsB.Kind)
            Assert.Equal(0, nsB.GetMembers().Length())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        ' Check namespace symbol containers in the presence of a root namespace
        <Fact>
        Public Sub NSContainersWithRootNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Class Type1
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithRootNamespace("Goo.Bar"))

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim members = globalNS.GetMembers()
            Dim nsGoo As NamespaceSymbol = DirectCast(members.First(), NamespaceSymbol)
            Dim membersGoo = nsGoo.GetMembers()
            Dim nsBar As NamespaceSymbol = DirectCast(membersGoo.First(), NamespaceSymbol)
            Dim type1Sym = nsBar.GetMembers("Type1").Single()

            Assert.Same(nsBar, type1Sym.ContainingSymbol)
            Assert.Same(nsGoo, nsBar.ContainingSymbol)
            Assert.Same(globalNS, nsGoo.ContainingSymbol)
            Assert.Same(compilation.SourceModule, globalNS.ContainingSymbol)
        End Sub

        ' Check namespace symbol containers in the presence of a root namespace
        <Fact>
        Public Sub NSContainersWithoutRootNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Class Type1
End Class
    </file>
</compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim type1Sym = globalNS.GetMembers("Type1").Single()

            Assert.Same(globalNS, type1Sym.ContainingSymbol)
            Assert.Same(compilation.SourceModule, globalNS.ContainingSymbol)
        End Sub

        <Fact>
        Public Sub ImportsAlias01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Test">
    <file name="a.vb">
Imports ALS = N1.N2

Namespace N1
    Namespace N2
        Public Class A
           Sub S()
           End Sub
        End Class
    End Namespace
End Namespace

Namespace N3
    Public Class B
        Inherits ALS.A
    End Class
End Namespace
    </file>
    <file name="b.vb">
Imports ANO = N3
Namespace N1.N2
    Class C
        Inherits ANO.B
    End Class
End Namespace
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace

            Dim n3 = DirectCast(globalNS.GetMembers("N3").Single(), NamespaceSymbol)
            Dim mem1 = DirectCast(n3.GetTypeMembers("B").Single(), NamedTypeSymbol)
            Assert.Equal("A", mem1.BaseType.Name)
            Assert.Equal("N1.N2.A", mem1.BaseType.ToTestDisplayString())

            Dim n1 = DirectCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim n2 = DirectCast(n1.GetMembers("N2").Single(), NamespaceSymbol)
            Dim mem2 = DirectCast(n2.GetTypeMembers("C").Single(), NamedTypeSymbol)
            Assert.Equal("B", mem2.BaseType.Name)
            Assert.Equal("N3.B", mem2.BaseType.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact, WorkItem(544009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544009")>
        Public Sub MultiModulesNamespace()

            Dim text3 = <![CDATA[
    Namespace N1
      Structure SGoo
      End Structure
    End Namespace
    ]]>.Value

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Test1">
    <file name="a.vb">
    Namespace N1
      Class CGoo
      End Class
    End Namespace
    </file>
</compilation>)

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Test2">
    <file name="b.vb">
    Namespace N1
      Interface IGoo
      End Interface
    End Namespace
    </file>
</compilation>)

            Dim compRef1 = New VisualBasicCompilationReference(comp1)
            Dim compRef2 = New VisualBasicCompilationReference(comp2)

            Dim comp = VisualBasicCompilation.Create("Test3", {VisualBasicSyntaxTree.ParseText(text3)}, {MscorlibRef, compRef1, compRef2})

            Dim globalNS = comp.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Assert.Equal(3, ns.GetTypeMembers().Length())
            Dim ext = ns.Extent
            Assert.Equal(NamespaceKind.Compilation, ext.Kind)
            Assert.Equal("Compilation: " & GetType(VisualBasicCompilation).FullName, ext.ToString())

            Dim constituents = ns.ConstituentNamespaces
            Assert.Equal(3, constituents.Length)
            Assert.True(constituents.Contains(TryCast(comp.SourceAssembly.GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))
            Assert.True(constituents.Contains(TryCast(comp.GetReferencedAssemblySymbol(compRef1).GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))
            Assert.True(constituents.Contains(TryCast(comp.GetReferencedAssemblySymbol(compRef2).GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))

            For Each constituentNs In constituents
                Assert.Equal(NamespaceKind.Module, constituentNs.Extent.Kind)
                Assert.Equal(ns.ToTestDisplayString(), constituentNs.ToTestDisplayString())
            Next
        End Sub

        <WorkItem(537310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537310")>
        <Fact>
        Public Sub MultiModulesNamespaceCorLibraries()

            Dim text1 = <![CDATA[
    Namespace N1
      Class CGoo
      End Class
    End Namespace
    ]]>.Value

            Dim text2 = <![CDATA[
    Namespace N1
      Interface IGoo
      End Interface
    End Namespace
    ]]>.Value

            Dim text3 = <![CDATA[
    Namespace N1
      Structure SGoo
      End Structure
    End Namespace
    ]]>.Value

            Dim comp1 = VisualBasicCompilation.Create("Test1", syntaxTrees:={VisualBasicSyntaxTree.ParseText(text1)})
            Dim comp2 = VisualBasicCompilation.Create("Test2", syntaxTrees:={VisualBasicSyntaxTree.ParseText(text2)})

            Dim compRef1 = New VisualBasicCompilationReference(comp1)
            Dim compRef2 = New VisualBasicCompilationReference(comp2)

            Dim comp = VisualBasicCompilation.Create("Test3", {VisualBasicSyntaxTree.ParseText(text3)}, {compRef1, compRef2})

            Dim globalNS = comp.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Assert.Equal(3, ns.GetTypeMembers().Length())
            Assert.Equal(NamespaceKind.Compilation, ns.Extent.Kind)

            Dim constituents = ns.ConstituentNamespaces
            Assert.Equal(3, constituents.Length)
            Assert.True(constituents.Contains(TryCast(comp.SourceAssembly.GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))
            Assert.True(constituents.Contains(TryCast(comp.GetReferencedAssemblySymbol(compRef1).GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))
            Assert.True(constituents.Contains(TryCast(comp.GetReferencedAssemblySymbol(compRef2).GlobalNamespace.GetMembers("N1").Single(), NamespaceSymbol)))

            For Each constituentNs In constituents
                Assert.Equal(NamespaceKind.Module, constituentNs.Extent.Kind)
                Assert.Equal(ns.ToTestDisplayString(), constituentNs.ToTestDisplayString())
            Next
        End Sub

        <WorkItem(690871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690871")>
        <Fact>
        Public Sub SpecialTypesAndAliases()
            Dim source =
                <compilation name="C">
                    <file>
Public Class C
End Class
                    </file>
                </compilation>

            Dim aliasedCorlib = NetFramework.mscorlib.WithAliases(ImmutableArray.Create("Goo"))

            Dim comp = CreateEmptyCompilationWithReferences(source, {aliasedCorlib})

            ' NOTE: this doesn't compile in dev11 - it reports that it cannot find System.Object.
            ' However, we've already changed how special type lookup works, so this is not a major issue.
            comp.AssertNoDiagnostics()

            Dim objectType = comp.GetSpecialType(SpecialType.System_Object)
            Assert.Equal(TypeKind.Class, objectType.TypeKind)
            Assert.Equal("System.Object", objectType.ToTestDisplayString())

            Assert.Equal(objectType, comp.Assembly.GetSpecialType(SpecialType.System_Object))
            Assert.Equal(objectType, comp.Assembly.CorLibrary.GetSpecialType(SpecialType.System_Object))
        End Sub

        <WorkItem(690871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690871")>
        <Fact>
        Public Sub WellKnownTypesAndAliases()
            Dim [lib] =
                <compilation name="lib">
                    <file>
Namespace System.Threading.Tasks
    Public Class Task
        Public Status As Integer
    End Class
End Namespace
                    </file>
                </compilation>

            Dim source =
                <compilation name="test">
                    <file>
Imports System.Threading.Tasks

Public Class App
    Public T as Task
End Class
                    </file>
                </compilation>

            Dim libComp = CreateEmptyCompilationWithReferences([lib], {MscorlibRef_v4_0_30316_17626})
            Dim libRef = libComp.EmitToImageReference(aliases:=ImmutableArray.Create("myTask"))

            Dim comp = CreateEmptyCompilationWithReferences(source, {libRef, MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929})

            ' NOTE: Unlike in C#, aliases on metadata references are ignored, so the
            ' reference to System.Threading.Tasks is ambiguous.
            comp.AssertTheseDiagnostics(
                <expected>
BC30560: 'Task' is ambiguous in the namespace 'System.Threading.Tasks'.
    Public T as Task
                ~~~~
                </expected>)
        End Sub

        <Fact, WorkItem(54836, "https://github.com/dotnet/roslyn/issues/54836")>
        Public Sub RetargetableAttributeIsRespectedInSource()
            Dim code = <![CDATA[
Imports System.Reflection
<Assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)>
]]>

            Dim comp = CreateCompilation(code.Value)
            Assert.True(comp.Assembly.Identity.IsRetargetable)
            AssertTheseEmitDiagnostics(comp)
        End Sub

    End Class

End Namespace
