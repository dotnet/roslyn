// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class TypeKindTests : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(Net40.References.mscorlib);

            TestTypeKindHelper(assembly);
        }

        private void TestTypeKindHelper(AssemblySymbol assembly)
        {
            var module0 = assembly.Modules[0];

            var system = (from n in module0.GlobalNamespace.GetMembers()
                          where n.Name.Equals("System")
                          select n).Cast<NamespaceSymbol>().Single();

            var obj = (from t in system.GetTypeMembers()
                       where t.Name.Equals("Object")
                       select t).Single();

            Assert.Equal(TypeKind.Class, obj.TypeKind);

            var @enum = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Enum")
                         select t).Single();

            Assert.Equal(TypeKind.Class, @enum.TypeKind);

            var int32 = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Int32")
                         select t).Single();

            Assert.Equal(TypeKind.Struct, int32.TypeKind);

            var func = (from t in system.GetTypeMembers()
                        where t.Name.Equals("Func") && t.Arity == 1
                        select t).Single();

            Assert.Equal(TypeKind.Delegate, func.TypeKind);

            var collections = (from n in system.GetMembers()
                               where n.Name.Equals("Collections")
                               select n).Cast<NamespaceSymbol>().Single();

            var ienumerable = (from t in collections.GetTypeMembers()
                               where t.Name.Equals("IEnumerable")
                               select t).Single();

            Assert.Equal(TypeKind.Interface, ienumerable.TypeKind);
            Assert.Null(ienumerable.BaseType());

            var typeCode = (from t in system.GetTypeMembers()
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

            var console = system.GetTypeMembers("Console").Single();

            Assert.False(console.IsAbstract);
            Assert.False(console.IsSealed);
            Assert.True(console.IsStatic);
        }
    }
}
