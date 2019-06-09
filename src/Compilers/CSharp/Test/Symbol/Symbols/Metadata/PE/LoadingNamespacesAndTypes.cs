// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingNamespacesAndTypes : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);
            XElement dumpXML = LoadChildNamespace1(assembly.Modules[0].GlobalNamespace);

            var baseLine = XElement.Load(new MemoryStream(TestResources.SymbolsTests.Metadata.MscorlibNamespacesAndTypes));
            Assert.Equal(baseLine.ToString(), dumpXML.ToString());

            // Do it again
            dumpXML = LoadChildNamespace1(assembly.Modules[0].GlobalNamespace);
            Assert.Equal(baseLine.ToString(), dumpXML.ToString());
        }

        [Fact]
        public void Test2()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);
            XElement dumpXML = LoadChildNamespace2(assembly.Modules[0].GlobalNamespace);

            var baseLine = XElement.Load(new MemoryStream(TestResources.SymbolsTests.Metadata.MscorlibNamespacesAndTypes));
            Assert.Equal(baseLine.ToString(), dumpXML.ToString());

            // Do it again
            dumpXML = LoadChildNamespace2(assembly.Modules[0].GlobalNamespace);
            Assert.Equal(baseLine.ToString(), dumpXML.ToString());
        }

        internal static XElement LoadChildNamespace1(NamespaceSymbol n)
        {
            XElement elem = new XElement((n.Name.Length == 0 ? "Global" : n.Name));

            var childrenTypes = n.GetTypeMembers().OrderBy((t) => t, new NameAndArityComparer());

            elem.Add(from t in childrenTypes select LoadChildType(t));

            var childrenNS = n.GetMembers().
                                Select((m) => (m as NamespaceSymbol)).
                                Where((m) => m != null).
                                OrderBy((child) => child.Name, StringComparer.OrdinalIgnoreCase);

            elem.Add(from c in childrenNS select LoadChildNamespace1(c));

            return elem;
        }

        private XElement LoadChildNamespace2(NamespaceSymbol n)
        {
            XElement elem = new XElement((n.Name.Length == 0 ? "Global" : n.Name));

            var children = n.GetMembers();
            n = null;

            var types = new List<NamedTypeSymbol>();
            var namespaces = new List<NamespaceSymbol>();

            foreach (var c in children)
            {
                NamedTypeSymbol t = c as NamedTypeSymbol;

                if ((object)t != null)
                {
                    types.Add(t);
                }
                else
                {
                    namespaces.Add(((NamespaceSymbol)c));
                }
            }

            var childrenTypes = types.OrderBy(t => t, new NameAndArityComparer());

            elem.Add(from t in childrenTypes select LoadChildType(t));

            var childrenNS = namespaces.OrderBy((child) => child.Name, StringComparer.OrdinalIgnoreCase);

            elem.Add(from c in childrenNS select LoadChildNamespace2(c));

            return elem;
        }

        private static XElement LoadChildType(NamedTypeSymbol t)
        {
            XElement elem = new XElement("type");

            elem.Add(new XAttribute("name", t.Name));

            if (t.Arity > 0)
            {
                elem.Add(new XAttribute("arity", t.Arity));
            }

            var childrenTypes = t.GetTypeMembers().OrderBy((c) => c, new NameAndArityComparer());

            elem.Add(from c in childrenTypes select LoadChildType(c));

            return elem;
        }

        [Fact]
        public void Test3()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);
            var module0 = assembly.Modules[0];
            var globalNS = module0.GlobalNamespace;

            Assert.Same(globalNS.ContainingAssembly, assembly);
            Assert.Same(globalNS.ContainingSymbol, module0);
            Assert.True(globalNS.IsGlobalNamespace);

            var extent = globalNS.Extent;

            Assert.Equal(extent.Kind, NamespaceKind.Module);
            Assert.Same(extent.Module, module0);
            Assert.Equal(1, globalNS.ConstituentNamespaces.Length);
            Assert.Same(globalNS, globalNS.ConstituentNamespaces[0]);

            var systemNS = (NamespaceSymbol)globalNS.GetMembers("System").Single();

            Assert.Same(systemNS.ContainingAssembly, assembly);
            Assert.Same(systemNS.ContainingSymbol, globalNS);
            Assert.False(systemNS.IsGlobalNamespace);

            extent = systemNS.Extent;

            Assert.Equal(extent.Kind, NamespaceKind.Module);
            Assert.Same(extent.Module, module0);
            Assert.Equal(1, systemNS.ConstituentNamespaces.Length);
            Assert.Same(systemNS, systemNS.ConstituentNamespaces[0]);

            var collectionsNS = (NamespaceSymbol)systemNS.GetMembers("Collections").Single();

            Assert.Same(collectionsNS.ContainingAssembly, assembly);
            Assert.Same(collectionsNS.ContainingSymbol, systemNS);
            Assert.False(collectionsNS.IsGlobalNamespace);

            extent = collectionsNS.Extent;

            Assert.Equal(extent.Kind, NamespaceKind.Module);
            Assert.Same(extent.Module, module0);
            Assert.Equal(1, collectionsNS.ConstituentNamespaces.Length);
            Assert.Same(collectionsNS, collectionsNS.ConstituentNamespaces[0]);
        }

        [Fact]
        public void Test4()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);
            TestGetMembersOfName(assembly.Modules[0]);

            var assembly2 = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase);
            TypeAndNamespaceDifferByCase(assembly2.Modules[0]);
        }

        private void TypeAndNamespaceDifferByCase(ModuleSymbol module0)
        {
            var someName = module0.GlobalNamespace.GetMembers("SomenamE");
            Assert.Equal(someName.Length, 0);

            someName = module0.GlobalNamespace.GetMembers("somEnamE");
            Assert.Equal(someName.Length, 1);
            Assert.NotNull((someName[0] as NamedTypeSymbol));

            someName = module0.GlobalNamespace.GetMembers("SomeName");
            Assert.Equal(someName.Length, 1);
            Assert.NotNull((someName[0] as NamespaceSymbol));

            var someName1_1 = module0.GlobalNamespace.GetTypeMembers("somEnamE1").OrderBy((t) => t.Name).ToArray();
            var someName1_2 = module0.GlobalNamespace.GetTypeMembers("SomeName1").OrderBy((t) => t.Name).ToArray();

            Assert.Equal(1, someName1_1.Length);
            Assert.Equal("somEnamE1", someName1_1[0].Name);
            Assert.Equal(1, someName1_2.Length);
            Assert.Equal("SomeName1", someName1_2[0].Name);
            Assert.NotEqual(someName1_1[0], someName1_2[0]);

            var someName2_1 = module0.GlobalNamespace.GetMembers("somEnamE2").OfType<NamespaceSymbol>().OrderBy((t) => t.Name).ToArray();
            var someName2_2 = module0.GlobalNamespace.GetMembers("SomeName2").OfType<NamespaceSymbol>().OrderBy((t) => t.Name).ToArray();
            Assert.Equal(1, someName2_1.Length);
            Assert.Equal("somEnamE2", someName2_1[0].Name);
            Assert.Equal(1, someName2_2.Length);
            Assert.Equal("SomeName2", someName2_2[0].Name);
            Assert.NotEqual(someName2_1[0], someName2_2[0]);

            var otherName_1 = someName2_1[0].GetTypeMembers("OtherName");
            var otherName_2 = someName2_2[0].GetTypeMembers("OtherName");

            Assert.Equal(1, otherName_1.Length);
            Assert.Equal(1, otherName_2.Length);
            Assert.NotEqual(otherName_1[0], otherName_2[0]);

            var nestingClass = module0.GlobalNamespace.GetTypeMembers("NestingClass").Single();
            var someName3_1 = nestingClass.GetTypeMembers("SomeName3").OrderBy((t) => t.Name).ToArray();
            var someName3_2 = nestingClass.GetTypeMembers("somEnamE3").OrderBy((t) => t.Name).ToArray();

            Assert.Equal(1, someName3_1.Length);
            Assert.Equal(1, someName3_2.Length);
            Assert.Equal("somEnamE3", someName3_2[0].Name);
            Assert.Equal("SomeName3", someName3_1[0].Name);
        }

        private void TestGetMembersOfName(ModuleSymbol module0)
        {
            var sys = module0.GlobalNamespace.GetMembers("SYSTEM");
            Assert.Equal(sys.Length, 0);

            sys = module0.GlobalNamespace.GetMembers("System");
            Assert.Equal(sys.Length, 1);

            var system = sys[0] as NamespaceSymbol;
            Assert.NotNull(system);

            Assert.Equal(system.GetMembers("Action").Length, 9);
            Assert.Equal(system.GetMembers("ActionThatDoesntExist").Length, 0);

            Assert.Equal(system.GetTypeMembers("Action").Length, 9);
            Assert.Equal(system.GetTypeMembers("ActionThatDoesntExist").Length, 0);

            Assert.Equal(system.GetTypeMembers("Action", 20).Length, 0);
            var actionOf0 = system.GetTypeMembers("Action", 0).Single();
            var actionOf4 = system.GetTypeMembers("Action", 4).Single();
            Assert.Equal("Action", actionOf0.Name);
            Assert.Equal("Action", actionOf4.Name);
            Assert.Equal(actionOf0.Arity, 0);
            Assert.Equal(actionOf4.Arity, 4);

            Assert.Equal(system.GetTypeMembers("ActionThatDoesntExist", 1).Length, 0);

            var collectionsArray = ((NamespaceSymbol)sys[0]).GetMembers("CollectionS");
            Assert.Equal(collectionsArray.Length, 0);

            collectionsArray = ((NamespaceSymbol)sys[0]).GetMembers("Collections");
            Assert.Equal(collectionsArray.Length, 1);

            var collections = collectionsArray[0] as NamespaceSymbol;
            Assert.NotNull(collections);

            Assert.Equal(0, collections.GetAttributes().Length);

            var enumerable = collections.GetMembers("IEnumerable");
            Assert.Equal(enumerable.Length, 1);
            Assert.Equal("System.Collections.IEnumerable", ((NamedTypeSymbol)enumerable[0]).ToTestDisplayString());

            var generic = collections.GetMembers("Generic");
            Assert.Equal(generic.Length, 1);
            Assert.NotNull((generic[0] as NamespaceSymbol));

            var dictionaryArray = ((NamespaceSymbol)generic[0]).GetMembers("Dictionary");
            Assert.Equal(dictionaryArray.Length, 1);

            var dictionary = (NamedTypeSymbol)dictionaryArray[0];
            Assert.Equal(dictionary.Arity, 2);
            Assert.Same(dictionary.ConstructedFrom, dictionary);
            Assert.Equal("Dictionary", dictionary.Name);

            Assert.Equal(0, collections.GetAttributes(dictionary).Count());

            Assert.Equal(dictionary.GetTypeMembers("ValueCollectionThatDoesntExist").Length, 0);
            Assert.Equal(dictionary.GetTypeMembers("ValueCollectioN").Length, 0);

            var valueCollection = dictionary.GetTypeMembers("ValueCollection");
            Assert.Equal(valueCollection.Length, 1);
            Assert.Equal("ValueCollection", ((NamedTypeSymbol)valueCollection[0]).Name);
            Assert.Equal(((NamedTypeSymbol)valueCollection[0]).Arity, 0);

            Assert.Equal(dictionary.GetTypeMembers("ValueCollectionThatDoesntExist", 1).Length, 0);
            Assert.Equal(valueCollection[0], dictionary.GetTypeMembers("ValueCollection", 0).Single());
            Assert.Equal(dictionary.GetTypeMembers("ValueCollection", 1).Length, 0);
        }

        [ClrOnlyFact]
        public void TestStructParameterlessConstructor_Explicit()
        {
            var ilSource = @"
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
";
            CompileWithCustomILSource(string.Empty, ilSource, comp =>
            {
                var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
                var constructor = structType.InstanceConstructors.Single();
                Assert.False(constructor.IsImplicitlyDeclared);
            });
        }

        [ClrOnlyFact]
        public void TestStructParameterlessConstructor_Implicit1()
        {
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S
";
            CompileWithCustomILSource(string.Empty, ilSource, comp =>
            {
                var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
                var constructor = structType.InstanceConstructors.Single();
                Assert.True(constructor.IsImplicitlyDeclared);
            });
        }

        [ClrOnlyFact]
        public void TestStructParameterlessConstructor_Implicit2()
        {
            var ilSource = @"
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
";
            CompileWithCustomILSource(string.Empty, ilSource, comp =>
            {
                var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
                var constructors = structType.InstanceConstructors;
                Assert.Equal(2, constructors.Length);

                int withParameterIndex = constructors[0].Parameters.Any() ? 0 : 1;
                int withoutParameterIndex = 1 - withParameterIndex;

                Assert.Equal(0, constructors[withoutParameterIndex].Parameters.Length);
                Assert.False(constructors[withParameterIndex].IsImplicitlyDeclared);
                Assert.True(constructors[withoutParameterIndex].IsImplicitlyDeclared);
            });
        }

        [Fact]
        public void TestAssemblyNameWithSpace1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.WithSpaces);
            Assert.NotNull(assembly);
            Assert.Equal("With Spaces", assembly.Name);
            Assert.Equal("With Spaces", assembly.MetadataName);
        }

        [Fact]
        public void TestAssemblyNameWithSpace2()
        {
            var compilation = CSharpCompilation.Create("C1", references:
                new[]
                {
                    TestReferences.NetFx.v4_0_21006.mscorlib,
                    TestReferences.SymbolsTests.WithSpaces
                });

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var assembly = type.ContainingAssembly;

            Assert.Equal("With Spaces", assembly.Name);
            Assert.Equal("With Spaces", assembly.MetadataName);
        }

        [Fact]
        public void TestNetModuleNameWithSpace()
        {
            var compilation = CSharpCompilation.Create("C1", references:
                new[]
                {
                    TestReferences.NetFx.v4_0_21006.mscorlib,
                    TestReferences.SymbolsTests.WithSpacesModule
                });

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var module = type.ContainingModule;

            // ilasm seems to tack on the file extension; all we really care about is the space
            Assert.Equal("With Spaces.netmodule", module.Name);
            Assert.Equal("With Spaces.netmodule", module.MetadataName);
        }
    }
}
