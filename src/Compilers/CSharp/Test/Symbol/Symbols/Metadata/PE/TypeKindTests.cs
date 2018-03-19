// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class TypeKindTests : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            AssemblySymbol assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);

            TestTypeKindHelper(assembly);
        }

        private void TestTypeKindHelper(AssemblySymbol assembly)
        {
            ModuleSymbol module0 = assembly.Modules[0];

            NamespaceSymbol system = (from n in module0.GlobalNamespace.GetMembers()
                          where n.Name.Equals("System")
                          select n).Cast<NamespaceSymbol>().Single();

            NamedTypeSymbol obj = (from t in system.GetTypeMembers()
                       where t.Name.Equals("Object")
                       select t).Single();

            Assert.Equal(TypeKind.Class, obj.TypeKind);

            NamedTypeSymbol @enum = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Enum")
                         select t).Single();

            Assert.Equal(TypeKind.Class, @enum.TypeKind);

            NamedTypeSymbol int32 = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Int32")
                         select t).Single();

            Assert.Equal(TypeKind.Struct, int32.TypeKind);

            NamedTypeSymbol func = (from t in system.GetTypeMembers()
                        where t.Name.Equals("Func") && t.Arity == 1
                        select t).Single();

            Assert.Equal(TypeKind.Delegate, func.TypeKind);

            NamespaceSymbol collections = (from n in system.GetMembers()
                               where n.Name.Equals("Collections")
                               select n).Cast<NamespaceSymbol>().Single();

            NamedTypeSymbol ienumerable = (from t in collections.GetTypeMembers()
                               where t.Name.Equals("IEnumerable")
                               select t).Single();

            Assert.Equal(TypeKind.Interface, ienumerable.TypeKind);
            Assert.Null(ienumerable.BaseType());

            NamedTypeSymbol typeCode = (from t in system.GetTypeMembers()
                            where t.Name.Equals("TypeCode")
                            select t).Single();

            Assert.Equal(TypeKind.Enum, typeCode.TypeKind);

            Assert.False(obj.IsAbstract);
            Assert.False(obj.IsSealed);
            Assert.False(obj.IsStatic);

            Assert.True(@enum.IsAbstract);
            Assert.False(@enum.IsSealed);
            Assert.False(@enum.IsStatic);

            Assert.False(func.IsAbstract);
            Assert.True(func.IsSealed);
            Assert.False(func.IsStatic);

            NamedTypeSymbol console = system.GetTypeMembers("Console").Single();

            Assert.False(console.IsAbstract);
            Assert.False(console.IsSealed);
            Assert.True(console.IsStatic);
        }
    }
}
