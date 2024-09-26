' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingNamespacesAndTypes
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assembly = LoadFromBytes(Net40.Resources.mscorlib)
            Dim dumpXML As XElement = LoadChildNamespace1(assembly.Modules(0).GlobalNamespace)

            Dim baseLine = XElement.Load(New MemoryStream(TestResources.SymbolsTests.Metadata.MscorlibNamespacesAndTypes))
            Assert.Equal(dumpXML.ToString(), baseLine.ToString())

            ' Do it again
            dumpXML = LoadChildNamespace1(assembly.Modules(0).GlobalNamespace)
            Assert.Equal(dumpXML.ToString(), baseLine.ToString())
        End Sub

        <Fact>
        Public Sub Test2()
            Dim assembly = LoadFromBytes(Net40.Resources.mscorlib)
            Dim dumpXML As XElement = LoadChildNamespace2(assembly.Modules(0).GlobalNamespace)

            Dim baseLine = XElement.Load(New MemoryStream(TestResources.SymbolsTests.Metadata.MscorlibNamespacesAndTypes))
            Assert.Equal(dumpXML.ToString(), baseLine.ToString())

            ' Do it again
            dumpXML = LoadChildNamespace2(assembly.Modules(0).GlobalNamespace)
            Assert.Equal(dumpXML.ToString(), baseLine.ToString())
        End Sub

        Private Function LoadChildNamespace1(n As NamespaceSymbol) As XElement
            Dim elem As XElement = New XElement(If(n.Name.Length = 0, "Global", n.Name))

            Dim childrenTypes = n.GetTypeMembers().AsEnumerable().OrderBy(Function(t) t, New TypeComparer())

            elem.Add(From t In childrenTypes Select LoadChildType(t))

            Dim childrenNS = n.GetMembers().
                                Select(Function(m) TryCast(m, NamespaceSymbol)).
                                Where(Function(m) m IsNot Nothing).
                                OrderBy(Function(child) child.Name, IdentifierComparison.Comparer)

            elem.Add(From c In childrenNS Select LoadChildNamespace1(c))

            Return elem
        End Function

        Private Function LoadChildNamespace2(n As NamespaceSymbol) As XElement
            Dim elem As XElement = New XElement(If(n.Name.Length = 0, "Global", n.Name))

            Dim children As ImmutableArray(Of Symbol) = n.GetMembers()
            n = Nothing

            Dim types As New List(Of NamedTypeSymbol)
            Dim namespaces As New List(Of NamespaceSymbol)

            For Each c In children
                Dim t As NamedTypeSymbol = TryCast(c, NamedTypeSymbol)

                If t IsNot Nothing Then
                    types.Add(t)
                Else
                    namespaces.Add(DirectCast(c, NamespaceSymbol))
                End If
            Next

            Dim childrenTypes = types.OrderBy(Function(t) t, New TypeComparer())

            elem.Add(From t In childrenTypes Select LoadChildType(t))

            Dim childrenNS = namespaces.OrderBy(Function(child) child.Name, IdentifierComparison.Comparer)

            elem.Add(From c In childrenNS Select LoadChildNamespace2(c))

            Return elem
        End Function

        Private Function LoadChildType(t As NamedTypeSymbol) As XElement

            Dim elem As XElement = <type name=<%= t.Name %>/>

            If t.Arity > 0 Then
                elem.Add(New XAttribute("arity", t.Arity))
            End If

            Dim childrenTypes = t.GetTypeMembers().AsEnumerable().OrderBy(Function(c) c, New TypeComparer())

            elem.Add(From c In childrenTypes Select LoadChildType(c))

            Return elem
        End Function

        <Fact>
        Public Sub Test3()
            Dim assembly = LoadFromBytes(Net40.Resources.mscorlib)
            Dim module0 = assembly.Modules(0)
            Dim globalNS = module0.GlobalNamespace

            Assert.Same(globalNS.ContainingAssembly, assembly)
            Assert.Same(globalNS.ContainingSymbol, module0)
            Assert.True(globalNS.IsGlobalNamespace)

            Dim extent = globalNS.Extent

            Assert.Equal(extent.Kind, NamespaceKind.Module)
            Assert.Same(extent.Module, module0)
            Assert.Equal(1, globalNS.ConstituentNamespaces.Length)
            Assert.Same(globalNS, globalNS.ConstituentNamespaces(0))

            Dim systemNS = DirectCast(globalNS.GetMembers("System").Single(), NamespaceSymbol)

            Assert.Same(systemNS.ContainingAssembly, assembly)
            Assert.Same(systemNS.ContainingSymbol, globalNS)
            Assert.False(systemNS.IsGlobalNamespace)

            extent = systemNS.Extent

            Assert.Equal(extent.Kind, NamespaceKind.Module)
            Assert.Same(extent.Module, module0)
            Assert.Equal(1, systemNS.ConstituentNamespaces.Length)
            Assert.Same(systemNS, systemNS.ConstituentNamespaces(0))

            Dim collectionsNS = DirectCast(systemNS.GetMembers("Collections").Single(), NamespaceSymbol)

            Assert.Same(collectionsNS.ContainingAssembly, assembly)
            Assert.Same(collectionsNS.ContainingSymbol, systemNS)
            Assert.False(collectionsNS.IsGlobalNamespace)

            extent = collectionsNS.Extent

            Assert.Equal(extent.Kind, NamespaceKind.Module)
            Assert.Same(extent.Module, module0)
            Assert.Equal(1, collectionsNS.ConstituentNamespaces.Length)
            Assert.Same(collectionsNS, collectionsNS.ConstituentNamespaces(0))
        End Sub

        <Fact()>
        Public Sub Test4()
            Dim assembly = LoadFromBytes(Net40.Resources.mscorlib)
            TestGetMembersOfName(assembly.Modules(0))

            Dim assembly2 = LoadFromBytes(TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase)
            TypeAndNamespaceDifferByCase(assembly2.Modules(0))
        End Sub

        Private Sub TypeAndNamespaceDifferByCase(module0 As ModuleSymbol)
            Dim someName = module0.GlobalNamespace.GetMembers("SomenamE")
            Assert.Equal(someName.Length, 2)
            Assert.NotNull(TryCast(someName(0), NamespaceSymbol))
            Assert.NotNull(TryCast(someName(1), NamedTypeSymbol))

            Dim somEnamE1 = module0.GlobalNamespace.GetTypeMembers("somEnamE1").AsEnumerable().OrderBy(Function(t) t.Name).ToArray()
            Dim SomeName1_ = module0.GlobalNamespace.GetTypeMembers("SomeName1").AsEnumerable().OrderBy(Function(t) t.Name).ToArray()

            Assert.Equal(2, somEnamE1.Length)
            Assert.Equal("somEnamE1", somEnamE1(0).Name)
            Assert.Equal("SomeName1", somEnamE1(1).Name)
            Assert.Equal(2, SomeName1_.Length)
            Assert.Same(somEnamE1(0), SomeName1_(0))
            Assert.Same(somEnamE1(1), SomeName1_(1))

            Dim somEnamE2 = module0.GlobalNamespace.GetMembers("somEnamE2").OfType(Of NamespaceSymbol)().OrderBy(Function(t) t.Name).ToArray()
            Dim SomeName2_ = module0.GlobalNamespace.GetMembers("SomeName2").OfType(Of NamespaceSymbol)().OrderBy(Function(t) t.Name).ToArray()
            Assert.Equal(1, somEnamE2.Length)
            Assert.Equal("somEnamE2", somEnamE2(0).Name)
            Assert.Equal(1, SomeName2_.Length)
            Assert.Same(somEnamE2(0), SomeName2_(0))

            Dim OtherName = somEnamE2(0).GetTypeMembers("OtherName").AsEnumerable().OrderBy(Function(t) t.ToTestDisplayString()).ToArray()

            Assert.Equal(2, OtherName.Length)
            Assert.NotEqual(OtherName(0), OtherName(1))
            Assert.Equal("somEnamE2.OtherName", OtherName(0).ToTestDisplayString())
            Assert.Equal("SomeName2.OtherName", OtherName(1).ToTestDisplayString())

            Dim nested1 = OtherName(0).GetTypeMembers("Nested").Single()
            Dim nested2 = OtherName(1).GetTypeMembers("Nested").Single()

            Assert.Equal("somEnamE2.OtherName.Nested", nested1.ToTestDisplayString())
            Assert.Equal("SomeName2.OtherName.Nested", nested2.ToTestDisplayString())

            Dim NestingClass = module0.GlobalNamespace.GetTypeMembers("NestingClass").Single()
            Dim SomeName3 = NestingClass.GetTypeMembers("SomeName3").AsEnumerable.OrderBy(Function(t) t.Name).ToArray()

            Assert.Equal(2, SomeName3.Length)
            Assert.Equal("somEnamE3", SomeName3(0).Name)
            Assert.Equal("SomeName3", SomeName3(1).Name)

        End Sub

        Private Sub TestGetMembersOfName(module0 As ModuleSymbol)

            Dim sys = module0.GlobalNamespace.GetMembers("SYSTEM")
            Assert.Equal(sys.Length, 1)

            Dim system = TryCast(sys(0), NamespaceSymbol)
            Assert.NotNull(system)

            Assert.Equal(system.GetMembers("Action").Length(), 9)
            Assert.Equal(system.GetMembers("ActionThatDoesntExist").Length(), 0)

            Assert.Equal(system.GetTypeMembers("Action").Length(), 9)
            Assert.Equal(system.GetTypeMembers("ActionThatDoesntExist").Length(), 0)

            Assert.Equal(system.GetTypeMembers("Action", 20).Length(), 0)
            Dim ActionOf0 = system.GetTypeMembers("Action", 0).Single()
            Dim ActionOf4 = system.GetTypeMembers("Action", 4).Single()
            Assert.True(IdentifierComparison.Equals(ActionOf0.Name, "Action"))
            Assert.True(IdentifierComparison.Equals(ActionOf4.Name, "Action"))
            Assert.Equal(ActionOf0.Arity, 0)
            Assert.Equal(ActionOf4.Arity, 4)

            Assert.Equal(system.GetTypeMembers("ActionThatDoesntExist", 1).Length(), 0)

            Dim collectionsArray = DirectCast(sys(0), NamespaceSymbol).GetMembers("CollectionS")
            Assert.Equal(collectionsArray.Length, 1)

            Dim collections = TryCast(collectionsArray(0), NamespaceSymbol)
            Assert.NotNull(collections)

            Assert.Equal(0, collections.GetAttributes().Length())

            Dim enumerable = collections.GetMembers("IEnumerable")
            Assert.Equal(enumerable.Length, 1)
            Assert.True(IdentifierComparison.Equals(DirectCast(enumerable(0), NamedTypeSymbol).ToTestDisplayString(),
                                                      "System.Collections.IEnumerable"))

            Dim generic = collections.GetMembers("Generic")
            Assert.Equal(generic.Length, 1)
            Assert.NotNull(TryCast(generic(0), NamespaceSymbol))

            Dim dictionaryArray = DirectCast(generic(0), NamespaceSymbol).GetMembers("Dictionary")
            Assert.Equal(dictionaryArray.Length, 1)

            Dim dictionary = DirectCast(dictionaryArray(0), NamedTypeSymbol)
            Assert.Equal(dictionary.Arity, 2)
            Assert.Same(dictionary, dictionary.ConstructedFrom)
            Assert.True(dictionary.Name.Equals("Dictionary"))

            Assert.Equal(0, collections.GetAttributes(dictionary).Count())

            Assert.Equal(dictionary.GetTypeMembers("ValueCollectionThatDoesntExist").Length(), 0)

            Dim valueCollection = dictionary.GetTypeMembers("ValueCollection")
            Assert.Equal(valueCollection.Length, 1)
            Assert.True(DirectCast(valueCollection(0), NamedTypeSymbol).Name.Equals("ValueCollection"))
            Assert.Equal(DirectCast(valueCollection(0), NamedTypeSymbol).Arity, 0)

            Assert.Equal(dictionary.GetTypeMembers("ValueCollectionThatDoesntExist", 1).Length(), 0)
            Assert.Same(valueCollection(0), dictionary.GetTypeMembers("ValueCollection", 0).Single())
            Assert.Equal(dictionary.GetTypeMembers("ValueCollection", 1).Length(), 0)
        End Sub

        <Fact>
        Public Sub TestStructParameterlessConstructor_Explicit()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
  {
	ret
  } // end of method S::.ctor
} // end of class S
]]>

            CompileWithCustomILSource(<compilation name="TestStructParameterlessConstructor_Explicit"/>, ilSource.Value, TestOptions.ReleaseDll,
                                                       Sub(compilation)
                                                           Dim structType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("S")
                                                           Dim constructor = structType.InstanceConstructors.Single()
                                                           Assert.False(constructor.IsImplicitlyDeclared)
                                                       End Sub)

        End Sub

        <Fact>
        Public Sub TestStructParameterlessConstructor_Implicit1()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S
]]>

            CompileWithCustomILSource(<compilation name="TestStructParameterlessConstructor_Explicit"/>, ilSource.Value, TestOptions.ReleaseDll,
                                                       Sub(compilation)
                                                           Dim structType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("S")
                                                           Dim constructor = structType.InstanceConstructors.Single()
                                                           Assert.True(constructor.IsImplicitlyDeclared)
                                                       End Sub)

        End Sub

        <Fact>
        Public Sub TestStructParameterlessConstructor_Implicit2()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname 
        instance void  .ctor(int32 x) cil managed
  {
	ret
  } // end of method S::.ctor
} // end of class S
]]>

            CompileWithCustomILSource(<compilation name="TestStructParameterlessConstructor_Explicit"/>, ilSource.Value, TestOptions.ReleaseDll,
                                                       Sub(compilation)
                                                           Dim structType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("S")
                                                           Dim constructors = structType.InstanceConstructors
                                                           Assert.Equal(2, constructors.Length)

                                                           Dim withParameterIndex = If(constructors(0).Parameters.Any(), 0, 1)
                                                           Dim withoutParameterIndex = 1 - withParameterIndex

                                                           Assert.Equal(0, constructors(withoutParameterIndex).Parameters.Length)
                                                           Assert.False(constructors(withParameterIndex).IsImplicitlyDeclared)
                                                           Assert.True(constructors(withoutParameterIndex).IsImplicitlyDeclared)
                                                       End Sub)
        End Sub
    End Class
End Namespace
