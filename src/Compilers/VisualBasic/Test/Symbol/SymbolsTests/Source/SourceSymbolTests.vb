' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SourceSymbolTests
        Inherits BasicTestBase

        <WorkItem(539740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539740")>
        <Fact()>
        Public Sub NamespaceWithoutName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="TST">
                    <file name="a.vb">
                        Namespace' A
                        'End Namespace
                    </file>
                </compilation>
            )
            Dim errors = compilation.GetDiagnostics().ToArray()
            Assert.Equal(2, errors.Count)
            Assert.Equal(1, compilation.SyntaxTrees.Length)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim nsArray = tree.GetRoot().DescendantNodes().Where(Function(node) (node.Kind = SyntaxKind.NamespaceStatement)).ToArray()
            Assert.Equal(1, nsArray.Length)
            Dim nsSyntax = DirectCast(nsArray(0), NamespaceStatementSyntax)
            Dim symbol = model.GetDeclaredSymbol(nsSyntax)
            Assert.Equal(String.Empty, symbol.Name)
        End Sub

        <WorkItem(540447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540447")>
        <Fact()>
        Public Sub FunctionWithoutName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="TST">
                    <file name="a.vb">
Class Program
    Private Function (q As Integer) As Integer
        Return q
    End Function
End Class
                    </file>
                </compilation>
            )
            CompilationUtils.AssertTheseDiagnostics(compilation,
                <errors>
BC30203: Identifier expected.
    Private Function (q As Integer) As Integer
                     ~
                </errors>)
        End Sub

        <WorkItem(540655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540655")>
        <Fact()>
        Public Sub Bug6998()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="TST">
                    <file name="a.vb">
Class Program
    Public Delegate Sub Foo(Of R)(r1 As R)
    Public Delegate Function Bar(Of R)(r2 As R) As Integer
End Class
                    </file>
                </compilation>
            )

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim r1 = TryCast(tree.FindNodeOrTokenByKind(SyntaxKind.ModifiedIdentifier, 1).AsNode(), ModifiedIdentifierSyntax)
            Dim r1Type = model.GetDeclaredSymbol(r1)
            Assert.NotNull(r1Type)
            Assert.Equal(SymbolKind.Parameter, r1Type.Kind)
            Assert.Equal("r1", r1Type.Name)

            Dim r2 = TryCast(tree.FindNodeOrTokenByKind(SyntaxKind.ModifiedIdentifier, 2).AsNode(), ModifiedIdentifierSyntax)
            Dim r2Type = model.GetDeclaredSymbol(r2)
            Assert.NotNull(r2Type)
            Assert.Equal(SymbolKind.Parameter, r2Type.Kind)
            Assert.Equal("r2", r2Type.Name)

        End Sub

        <WorkItem(546566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546566")>
        <Fact()>
        Public Sub Bug16199()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="TST">
    <file name="a.vb">
Namespace NS
    Namespace Name1
    End Namespace

    Class Name2
    End Class

    Namespace Name3
    End Namespace

    Class Name4
    End Class

    Namespace Name5
    End Namespace
End Namespace
    </file>
    <file name="b.vb">
Namespace NS
    Namespace Name3
    End Namespace

    Class Name5
    End Class

    Namespace Name5
    End Namespace
End Namespace
    </file>
    <file name="c.vb">
Namespace NS
    Structure Name4
    End Structure

    Structure Name5
    End Structure
End Namespace
    </file>
</compilation>
            )

            Dim ns = DirectCast(compilation.GlobalNamespace.GetMembers("NS").Single(), NamespaceSymbol)

            Dim members1 = ns.GetMembers("Name1")
            Dim types1 = ns.GetTypeMembers("Name1")
            Assert.Equal(1, members1.Length)
            Assert.True(types1.IsEmpty)

            Dim members2 = ns.GetMembers("Name2")
            Dim types2 = ns.GetTypeMembers("Name2")
            Assert.Equal(1, members2.Length)
            Assert.Equal(1, types2.Length)

            Dim members3 = ns.GetMembers("Name3")
            Dim types3 = ns.GetTypeMembers("Name3")
            Assert.Equal(1, members3.Length)
            Assert.True(types1.IsEmpty)

            Dim members4 = ns.GetMembers("Name4")
            Dim types4 = ns.GetTypeMembers("Name4")
            Assert.Equal(2, members4.Length)
            Assert.Equal(2, types4.Length)

            Dim members5 = ns.GetMembers("Name5")
            Dim types5 = ns.GetTypeMembers("Name5")
            Assert.Equal(3, members5.Length)
            Assert.Equal(2, types5.Length)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30179: class 'Name4' and structure 'Name4' conflict in namespace 'NS'.
    Class Name4
          ~~~~~
BC30179: class 'Name5' and namespace 'Name5' conflict in namespace 'NS'.
    Class Name5
          ~~~~~
BC30179: structure 'Name4' and class 'Name4' conflict in namespace 'NS'.
    Structure Name4
              ~~~~~
BC30179: structure 'Name5' and namespace 'Name5' conflict in namespace 'NS'.
    Structure Name5
              ~~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub EmptyCompilation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Banana">
</compilation>)

            Assert.Equal("Banana", compilation.Assembly.Name)
            Assert.Equal(0, compilation.SyntaxTrees.Length)
            Dim mscorlibAssembly = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorlibAssembly.Name)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact()>
        Public Sub DefaultBaseClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                <compilation name="C">
                    <file name="a.vb">
Class C
End Class
Structure S
End Structure
Interface I
End Interface
Enum E
   Enumerator
End Enum
Module M
End Module
Delegate Sub D()
                    </file>
                </compilation>)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNS.GetMembers("C").First(), NamedTypeSymbol)
            Dim structureS = DirectCast(globalNS.GetMembers("S").First(), NamedTypeSymbol)
            Dim interfaceI = DirectCast(globalNS.GetMembers("I").First(), NamedTypeSymbol)
            Dim enumE = DirectCast(globalNS.GetMembers("E").First(), NamedTypeSymbol)
            Dim moduleM = DirectCast(globalNS.GetMembers("M").First(), NamedTypeSymbol)
            Dim delegateD = DirectCast(globalNS.GetMembers("D").First(), NamedTypeSymbol)

            Assert.Equal("System.Object", classC.BaseType.ToTestDisplayString())
            Assert.Equal("System.ValueType", structureS.BaseType.ToTestDisplayString())
            Assert.Null(interfaceI.BaseType)
            Assert.Equal("System.Enum", enumE.BaseType.ToTestDisplayString())
            Assert.Equal("System.Object", moduleM.BaseType.ToTestDisplayString())
            Assert.Equal("System.MulticastDelegate", delegateD.BaseType.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact()>
        Public Sub BaseClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C">
    <file name="a.vb">
Class Y
    Class Inner
    End Class
End Class

Partial Class X
    Inherits Y
    Public f1 as Inner
End Class
    </file>
    <file name="b.vb">
Partial Class X
    Inherits Y 
    Public f2 as Inner
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classX = DirectCast(globalNS.GetMembers("X").First(), NamedTypeSymbol)
            Dim classY = DirectCast(globalNS.GetMembers("Y").First(), NamedTypeSymbol)
            Dim inner = DirectCast(classY.GetMembers("Inner").First(), NamedTypeSymbol)

            Assert.Equal(globalNS, classX.ContainingSymbol)
            Assert.Equal("X", classX.Name, IdentifierComparison.Comparer)
            Assert.Equal(classY, classX.BaseType)

            Dim f1Field = DirectCast(classX.GetMembers("f1").First(), FieldSymbol)
            Assert.Equal(inner, f1Field.Type)
            Dim f2Field = DirectCast(classX.GetMembers("f2").First(), FieldSymbol)
            Assert.Equal(inner, f2Field.Type)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact()>
        Public Sub SymbolLocations()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C">
    <file name="a.vb">
Public Partial Class C
    Public Sub m1(x as Integer)
    End Sub

    Public Partial Class D(Of T)
        Public v1$, v2$
    End Class
End Class

Namespace N1
    Namespace N2
        Namespace N3
        End Namespace
    End Namespace
End Namespace
    </file>
    <file name="b.vb">
Option Strict On        
Public Partial Class C
    Public Partial Class D(Of T)
    End Class
End Class

Namespace N1.N2.N3
End Namespace
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(m) m.Name).ToArray()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim classD = DirectCast(membersOfC(1), NamedTypeSymbol)
            Dim membersOfD = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim m1 = DirectCast(membersOfC(2), MethodSymbol)
            Dim p1 = m1.Parameters(0)
            Dim v1 = DirectCast(membersOfD(1), FieldSymbol)
            Dim tp1 = DirectCast(classD.TypeParameters(0), TypeParameterSymbol)

            Dim n1 = DirectCast(globalNSmembers(1), NamespaceSymbol)
            Dim membersOfN1 = n1.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim n2 = DirectCast(membersOfN1(0), NamespaceSymbol)
            Dim membersOfN2 = n2.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim n3 = DirectCast(membersOfN2(0), NamespaceSymbol)

            Dim locs As Location()

            locs = (From l In classC.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("C", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("C", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In classD.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("D", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("D", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In tp1.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("T", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("T", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In m1.Locations
                    Select l).ToArray()
            Assert.Equal(1, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("m1", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))

            locs = (From l In p1.Locations
                    Select l).ToArray()
            Assert.Equal(1, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("x", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))

            locs = (From l In v1.Locations
                    Select l).ToArray()
            Assert.Equal(1, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("v1$", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))

            locs = (From l In n1.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("N1", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("N1", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In n2.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("N2", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("N2", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In n3.Locations.AsEnumerable()
                    Order By l.SourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(2, locs.Length)
            Assert.Equal("a.vb", locs(0).SourceTree.FilePath)
            Assert.Equal("N3", locs(0).SourceTree.GetText().ToString(locs(0).SourceSpan))
            Assert.Equal("b.vb", locs(1).SourceTree.FilePath)
            Assert.Equal("N3", locs(1).SourceTree.GetText().ToString(locs(1).SourceSpan))

            locs = (From l In globalNS.Locations.AsEnumerable()
                    Order By l.PossiblyEmbeddedOrMySourceTree.FilePath
                    Select l).ToArray()
            Assert.Equal(3, locs.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537442")>
        <Fact()>
        Public Sub InvalidOptionCompare()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="C">
                    <file name="a.vb">
option Compare <![CDATA[qqqqqq]]>

Namespace Misc003aErr
    Friend Module Misc003aErrmod
        Function foo(x as Integer) as Boolean
            return x = 42
        End Function 
    End Module
End Namespace
                    </file>
                </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("Misc003aErr").First(), NamespaceSymbol)
            Dim errmod = DirectCast(ns.GetMembers("Misc003aErrmod").First(), NamedTypeSymbol)
            Dim mems = errmod.GetMembers()
            Assert.Equal(1, mems.Length)

        End Sub

        <WorkItem(527175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527175")>
        <Fact()>
        Public Sub DoubleBracketsNames()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="C">
                    <file name="a.vb">
Option Strict Off
Option Explicit On
Imports VB6 = Microsoft.VisualBasic

Namespace SharedGen007
    Friend Class [[ident1]] 

        'Scenario1
        public shared [[ident3]](9) as Short                                     

        'Scenario3
        Friend Shared [[ident5]]() as double

        'Scenario4
        protected friend shared [[ident6]]() as integer

        'Scenario5
        public shared [[ident7]](2, 3) as object                                      

        'Scenario6
        protected friend shared [[ident8]](,) as string                                     

        'Scenario7
        Public shared [[ident9]]() as double
    End Class
End Namespace
                    </file>
                </compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim ns = DirectCast(globalNS.GetMembers("SharedGen007").First(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers().First(), NamedTypeSymbol)
            Assert.Equal("SharedGen007.?", type1.ToDisplayString())
            ' Won't fix
            'Assert.Equal("[ident1]", type1.Name)
            'For Each m In type1.GetMembers()
            '    Assert.NotEmpty(m.Name)
            'Next
        End Sub


        <WorkItem(542508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542508")>
        <Fact()>
        Public Sub LocalsWithoutAsClauseInForStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="LocalsWithoutAsClauseInForStatement">
                    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        For a = 1 To 6
            Console.WriteLine(a)
        Next
    End Sub
End Module
                    </file>
                </compilation>
            )

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim list = tree.GetRoot().DescendantNodes().Where(Function(n) (n.Kind = SyntaxKind.ForStatement)).ToArray()
            Assert.Equal(1, list.Length)
            Dim node = DirectCast(list(0), ForStatementSyntax)
            Dim symbol = model.GetDeclaredSymbol(node)
            Assert.Null(symbol)
        End Sub

        <WorkItem(543720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543720")>
        <Fact()>
        Public Sub InvalidLocalsWithColon()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                <compilation name="InvalidLocalsWithColon">
                    <file name="a.vb">
Module Program
    Sub Main()
        Dim [foo as integer : Dim [goo As Char
    End Sub
End Module
                    </file>
                </compilation>
            )

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                Diagnostic(ERRID.ERR_MissingEndBrack, "[foo"),
                Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                Diagnostic(ERRID.ERR_MissingEndBrack, "[goo"))

        End Sub

        <WorkItem(187865, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/187865")>
        <Fact>
        Public Sub DifferentMembersMetadataName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Delegate Sub D()
Class C
    Function Get_P(o As Object) As Object
        Return o
    End Function
    Function p(o As Object) As Object
        Return o
    End Function
    ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
    Event E As D
    Sub Add_E()
    End Sub
    ReadOnly Property add_e As Object
        Get
            Return Nothing
        End Get
    End Property
    Declare Sub ADD_E Lib "A" (o As Object)
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC31061: function 'Get_P' conflicts with a member implicitly declared for property 'P' in class 'C'.
    Function Get_P(o As Object) As Object
             ~~~~~
BC30260: 'P' is already declared as 'Public Function p(o As Object) As Object' in this class.
    ReadOnly Property P As Object
                      ~
BC31060: event 'E' implicitly defines 'add_E', which conflicts with a member of the same name in class 'C'.
    Event E As D
          ~
BC31060: event 'E' implicitly defines 'add_E', which conflicts with a member of the same name in class 'C'.
    Event E As D
          ~
BC31060: event 'E' implicitly defines 'add_E', which conflicts with a member of the same name in class 'C'.
    Event E As D
          ~
</expected>)

            Dim type = compilation.GetMember(Of NamedTypeSymbol)("C")

            Dim members = type.GetMembers("P")
            Assert.Equal(2, members.Length)
            Assert.Equal("p", members(0).MetadataName)
            Assert.Equal("P", members(1).MetadataName)

            members = type.GetMembers("get_P")
            Assert.Equal(2, members.Length)
            Assert.Equal("Get_P", members(0).MetadataName)
            Assert.Equal("get_P", members(1).MetadataName)

            members = type.GetMembers("add_E")
            Assert.Equal(4, members.Length)
            Assert.Equal("add_E", members(0).MetadataName)
            Assert.Equal("Add_E", members(1).MetadataName)
            Assert.Equal("add_e", members(2).MetadataName)
            Assert.Equal("Add_E", members(3).MetadataName)
        End Sub

    End Class
End Namespace
