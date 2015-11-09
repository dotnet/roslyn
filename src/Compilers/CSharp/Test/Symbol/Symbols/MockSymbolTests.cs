// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SymbolTests
    {
        [Fact]
        public void TestArrayType()
        {
            CSharpCompilation compilation = CSharpCompilation.Create("Test");

            NamedTypeSymbol elementType = new MockNamedTypeSymbol("TestClass", Enumerable.Empty<Symbol>());   // this can be any type.

            ArrayTypeSymbol ats1 = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, elementType, rank: 1);
            Assert.Equal(1, ats1.Rank);
            Assert.True(ats1.IsSZArray);
            Assert.Same(elementType, ats1.ElementType);
            Assert.Equal(SymbolKind.ArrayType, ats1.Kind);
            Assert.True(ats1.IsReferenceType);
            Assert.False(ats1.IsValueType);
            Assert.Equal("TestClass[]", ats1.ToTestDisplayString());

            ArrayTypeSymbol ats2 = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, elementType, rank: 2);
            Assert.Equal(2, ats2.Rank);
            Assert.Same(elementType, ats2.ElementType);
            Assert.Equal(SymbolKind.ArrayType, ats2.Kind);
            Assert.True(ats2.IsReferenceType);
            Assert.False(ats2.IsValueType);
            Assert.Equal("TestClass[,]", ats2.ToTestDisplayString());

            ArrayTypeSymbol ats3 = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, elementType, rank: 3);
            Assert.Equal(3, ats3.Rank);
            Assert.Equal("TestClass[,,]", ats3.ToTestDisplayString());
        }

        [Fact]
        public void TestPointerType()
        {
            NamedTypeSymbol pointedAtType = new MockNamedTypeSymbol("TestClass", Enumerable.Empty<Symbol>());   // this can be any type.

            PointerTypeSymbol pts1 = new PointerTypeSymbol(pointedAtType);
            Assert.Same(pointedAtType, pts1.PointedAtType);
            Assert.Equal(SymbolKind.PointerType, pts1.Kind);
            Assert.False(pts1.IsReferenceType);
            Assert.True(pts1.IsValueType);
            Assert.Equal("TestClass*", pts1.ToTestDisplayString());
        }

        [Fact]
        public void TestMissingMetadataSymbol()
        {
            AssemblyIdentity missingAssemblyId = new AssemblyIdentity("foo");
            AssemblySymbol assem = new MockAssemblySymbol("banana");
            ModuleSymbol module = new MissingModuleSymbol(assem, ordinal: -1);
            NamedTypeSymbol container = new MockNamedTypeSymbol("TestClass", Enumerable.Empty<Symbol>(), TypeKind.Class);

            var mms1 = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(missingAssemblyId).Modules[0], "Elvis", "Lives", 2, true);
            Assert.Equal(2, mms1.Arity);
            Assert.Equal("Elvis", mms1.NamespaceName);
            Assert.Equal("Lives", mms1.Name);
            Assert.Equal("Elvis.Lives<,>[missing]", mms1.ToTestDisplayString());
            Assert.Equal("foo", mms1.ContainingAssembly.Identity.Name);

            var mms2 = new MissingMetadataTypeSymbol.TopLevel(module, "Elvis.Is", "Cool", 0, true);
            Assert.Equal(0, mms2.Arity);
            Assert.Equal("Elvis.Is", mms2.NamespaceName);
            Assert.Equal("Cool", mms2.Name);
            Assert.Equal("Elvis.Is.Cool[missing]", mms2.ToTestDisplayString());
            Assert.Same(assem, mms2.ContainingAssembly);

            // TODO: Add test for 3rd constructor.
        }

        [Fact]
        public void TestNamespaceExtent()
        {
            AssemblySymbol assem1 = new MockAssemblySymbol("foo");

            NamespaceExtent ne1 = new NamespaceExtent(assem1);
            Assert.Equal(ne1.Kind, NamespaceKind.Assembly);
            Assert.Same(ne1.Assembly, assem1);

            CSharpCompilation compilation = CSharpCompilation.Create("Test");
            NamespaceExtent ne2 = new NamespaceExtent(compilation);
            Assert.IsType<CSharpCompilation>(ne2.Compilation);
            Assert.Throws<InvalidOperationException>(() => ne1.Compilation);
        }

        private Symbol CreateMockSymbol(NamespaceExtent extent, XElement xel)
        {
            Symbol result;
            var childSymbols = from childElement in xel.Elements()
                               select CreateMockSymbol(extent, childElement);

            string name = xel.Attribute("name").Value;
            switch (xel.Name.LocalName)
            {
                case "ns":
                    result = new MockNamespaceSymbol(name, extent, childSymbols);
                    break;

                case "class":
                    result = new MockNamedTypeSymbol(name, childSymbols, TypeKind.Class);
                    break;

                default:
                    throw new ApplicationException("unexpected xml element");
            }

            foreach (IMockSymbol child in childSymbols)
            {
                child.SetContainer(result);
            }

            return result;
        }

        private void DumpSymbol(Symbol sym, StringBuilder builder, int level)
        {
            if (sym is NamespaceSymbol)
            {
                builder.AppendFormat("namespace {0} [{1}]", sym.Name, (sym as NamespaceSymbol).Extent);
            }
            else if (sym is NamedTypeSymbol)
            {
                builder.AppendFormat("{0} {1}", (sym as NamedTypeSymbol).TypeKind.ToString().ToLower(), sym.Name);
            }
            else
            {
                throw new ApplicationException("Unexpected symbol kind");
            }

            if (sym is NamespaceOrTypeSymbol && ((NamespaceOrTypeSymbol)sym).GetMembers().Any())
            {
                builder.AppendLine(" { ");
                var q = from c in ((NamespaceOrTypeSymbol)sym).GetMembers()
                        orderby c.Name
                        select c;

                foreach (Symbol child in q)
                {
                    for (int i = 0; i <= level; ++i)
                    {
                        builder.Append("    ");
                    }

                    DumpSymbol(child, builder, level + 1);
                    builder.AppendLine();
                }

                for (int i = 0; i < level; ++i)
                {
                    builder.Append("    ");
                }

                builder.Append("}");
            }
        }

        private string DumpSymbol(Symbol sym)
        {
            StringBuilder builder = new StringBuilder();
            DumpSymbol(sym, builder, 0);
            return builder.ToString();
        }

        [Fact]
        public void TestMergedNamespaces()
        {
            NamespaceSymbol root1 = (NamespaceSymbol)CreateMockSymbol(new NamespaceExtent(new MockAssemblySymbol("Assem1")),
                                                                       XElement.Parse(
@"<ns name=''> 
    <ns name='A'> 
         <ns name='D'/>
         <ns name='E'/>
         <ns name='F'>
             <ns name='G'/>
         </ns>
    </ns> 
    <ns name='B'/>
    <ns name='C'/>
    <ns name='U'/>
    <class name='X'/>
</ns>"));

            NamespaceSymbol root2 = (NamespaceSymbol)CreateMockSymbol(new NamespaceExtent(new MockAssemblySymbol("Assem2")),
                                                                       XElement.Parse(
@"<ns name=''>
    <ns name='B'>
         <ns name='K'/>
    </ns>
    <ns name='C'/>
    <class name='X'/>
    <class name='Y'/>
</ns>"));

            NamespaceSymbol root3 = (NamespaceSymbol)CreateMockSymbol(new NamespaceExtent(new MockAssemblySymbol("Assem3")),
                                                                       XElement.Parse(
@"<ns name=''> 
    <ns name='A'>
        <ns name='D'/>
        <ns name='E'>
           <ns name='H'/>
        </ns>
    </ns> 
    <ns name='B'>
        <ns name='K'>
            <ns name='L'/>
            <class name='L'/>
        </ns>
    </ns> 
    <class name='Z'/>
</ns>"));

            NamespaceSymbol merged = MergedNamespaceSymbol.Create(new NamespaceExtent(new MockAssemblySymbol("Merged")), null,
                                                                  new NamespaceSymbol[] { root1, root2, root3 }.AsImmutable());
            string expected =
@"namespace  [Assembly: Merged] { 
    namespace A [Assembly: Merged] { 
        namespace D [Assembly: Merged]
        namespace E [Assembly: Merged] { 
            namespace H [Assembly: Assem3]
        }
        namespace F [Assembly: Assem1] { 
            namespace G [Assembly: Assem1]
        }
    }
    namespace B [Assembly: Merged] { 
        namespace K [Assembly: Merged] { 
            class L
            namespace L [Assembly: Assem3]
        }
    }
    namespace C [Assembly: Merged]
    namespace U [Assembly: Assem1]
    class X
    class X
    class Y
    class Z
}".Replace("Assembly: Merged", "Assembly: Merged, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
  .Replace("Assembly: Assem1", "Assembly: Assem1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
  .Replace("Assembly: Assem3", "Assembly: Assem3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            Assert.Equal(expected, DumpSymbol(merged));

            NamespaceSymbol merged2 = MergedNamespaceSymbol.Create(new NamespaceExtent(new MockAssemblySymbol("Merged2")), null,
                                                                  new NamespaceSymbol[] { root1 }.AsImmutable());
            Assert.Same(merged2, root1);
        }
    }

    internal interface IMockSymbol
    {
        void SetContainer(Symbol container);
    }
}
