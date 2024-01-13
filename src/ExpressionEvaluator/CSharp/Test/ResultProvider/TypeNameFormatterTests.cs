// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class TypeNameFormatterTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Primitives()
        {
            Assert.Equal("object", typeof(object).GetTypeName());
            Assert.Equal("bool", typeof(bool).GetTypeName());
            Assert.Equal("char", typeof(char).GetTypeName());
            Assert.Equal("sbyte", typeof(sbyte).GetTypeName());
            Assert.Equal("byte", typeof(byte).GetTypeName());
            Assert.Equal("short", typeof(short).GetTypeName());
            Assert.Equal("ushort", typeof(ushort).GetTypeName());
            Assert.Equal("int", typeof(int).GetTypeName());
            Assert.Equal("uint", typeof(uint).GetTypeName());
            Assert.Equal("long", typeof(long).GetTypeName());
            Assert.Equal("ulong", typeof(ulong).GetTypeName());
            Assert.Equal("float", typeof(float).GetTypeName());
            Assert.Equal("double", typeof(double).GetTypeName());
            Assert.Equal("decimal", typeof(decimal).GetTypeName());
            Assert.Equal("string", typeof(string).GetTypeName());
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016796")]
        public void NestedTypes()
        {
            var source = @"
public class A
{
    public class B { }
}

namespace N
{
    public class A
    {
        public class B { }
    }
    public class G1<T>
    {
        public class G2<T>
        {
            public class G3<U> { }
            class G4<U, V> { }
        }
    }
}
";

            var assembly = GetAssembly(source);

            Assert.Equal("A", assembly.GetType("A").GetTypeName());
            Assert.Equal("A.B", assembly.GetType("A+B").GetTypeName());
            Assert.Equal("N.A", assembly.GetType("N.A").GetTypeName());
            Assert.Equal("N.A.B", assembly.GetType("N.A+B").GetTypeName());
            Assert.Equal("N.G1<int>.G2<float>.G3<double>", assembly.GetType("N.G1`1+G2`1+G3`1").MakeGenericType(typeof(int), typeof(float), typeof(double)).GetTypeName());
            Assert.Equal("N.G1<int>.G2<float>.G4<double, ushort>", assembly.GetType("N.G1`1+G2`1+G4`2").MakeGenericType(typeof(int), typeof(float), typeof(double), typeof(ushort)).GetTypeName());
        }

        [Fact]
        public void GenericTypes()
        {
            var source = @"
public class A
{
    public class B { }
}

namespace N
{
    public class C<T, U>
    {
        public class D<V, W>
        {
        }
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A");
            var typeB = typeA.GetNestedType("B");
            var typeC = assembly.GetType("N.C`2");
            var typeD = typeC.GetNestedType("D`2");
            var typeInt = typeof(int);
            var typeString = typeof(string);
            var typeCIntString = typeC.MakeGenericType(typeInt, typeString);

            Assert.Equal("N.C<T, U>", typeC.GetTypeName());
            Assert.Equal("N.C<int, string>", typeCIntString.GetTypeName());
            Assert.Equal("N.C<A, A.B>", typeC.MakeGenericType(typeA, typeB).GetTypeName());
            Assert.Equal("N.C<int, string>.D<A, A.B>", typeD.MakeGenericType(typeInt, typeString, typeA, typeB).GetTypeName());
            Assert.Equal("N.C<A, N.C<int, string>>.D<N.C<int, string>, A.B>", typeD.MakeGenericType(typeA, typeCIntString, typeCIntString, typeB).GetTypeName());
        }

        [Fact]
        public void NonGenericInGeneric()
        {
            var source = @"
public class A<T>
{
    public class B { }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A`1");
            var typeB = typeA.GetNestedType("B");

            Assert.Equal("A<int>.B", typeB.MakeGenericType(typeof(int)).GetTypeName());
        }

        [Fact]
        public void PrimitiveNullableTypes()
        {
            Assert.Equal("int?", typeof(int?).GetTypeName());
            Assert.Equal("bool?", typeof(bool?).GetTypeName());
        }

        [Fact]
        public void NullableTypes()
        {
            var source = @"
namespace N
{
    public struct A<T>
    {
        public struct B<U>
        {
        }
    }

    public struct C
    {
    }
}
";
            var typeNullable = typeof(System.Nullable<>);

            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var typeC = assembly.GetType("N.C");

            Assert.Equal("N.C?", typeNullable.MakeGenericType(typeC).GetTypeName());
            Assert.Equal("N.A<N.C>?", typeNullable.MakeGenericType(typeA.MakeGenericType(typeC)).GetTypeName());
            Assert.Equal("N.A<N.C>.B<N.C>?", typeNullable.MakeGenericType(typeB.MakeGenericType(typeC, typeC)).GetTypeName());
        }

        [Fact]
        public void PrimitiveArrayTypes()
        {
            Assert.Equal("int[]", typeof(int[]).GetTypeName());
            Assert.Equal("int[,]", typeof(int[,]).GetTypeName());
            Assert.Equal("int[][,]", typeof(int[][,]).GetTypeName());
            Assert.Equal("int[,][]", typeof(int[,][]).GetTypeName());
        }

        [Fact]
        public void ArrayTypes()
        {
            var source = @"
namespace N
{
    public class A<T>
    {
        public class B<U>
        {
        }
    }

    public class C
    {
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var typeC = assembly.GetType("N.C");

            Assert.NotEqual(typeC.MakeArrayType(), typeC.MakeArrayType(1));

            Assert.Equal("N.C[]", typeC.MakeArrayType().GetTypeName());
            Assert.Equal("N.C[]", typeC.MakeArrayType(1).GetTypeName()); // NOTE: Multi-dimensional array that happens to exactly one dimension.
            Assert.Equal("N.A<N.C>[,]", typeA.MakeGenericType(typeC).MakeArrayType(2).GetTypeName());
            Assert.Equal("N.A<N.C[]>.B<N.C>[,,]", typeB.MakeGenericType(typeC.MakeArrayType(), typeC).MakeArrayType(3).GetTypeName());
        }

        [Fact]
        public void CustomBoundsArrayTypes()
        {
            Array instance = Array.CreateInstance(typeof(int), [1, 2, 3,], [4, 5, 6,]);

            Assert.Equal("int[,,]", instance.GetType().GetTypeName());
            Assert.Equal("int[][,,]", instance.GetType().MakeArrayType().GetTypeName());
        }

        [Fact]
        public void PrimitivePointerTypes()
        {
            Assert.Equal("int*", typeof(int).MakePointerType().GetTypeName());
            Assert.Equal("int**", typeof(int).MakePointerType().MakePointerType().GetTypeName());
            Assert.Equal("int*[]", typeof(int).MakePointerType().MakeArrayType().GetTypeName());
        }

        [Fact]
        public void PointerTypes()
        {
            var source = @"
namespace N
{
    public struct A<T>
    {
        public struct B<U>
        {
        }
    }

    public struct C
    {
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var typeC = assembly.GetType("N.C");

            Assert.Equal("N.C*", typeC.MakePointerType().GetTypeName());
            Assert.Equal("N.A<N.C>*", typeA.MakeGenericType(typeC).MakePointerType().GetTypeName());
            Assert.Equal("N.A<N.C>.B<N.C>*", typeB.MakeGenericType(typeC, typeC).MakePointerType().GetTypeName());
        }

        [Fact]
        public void Void()
        {
            Assert.Equal("void", typeof(void).GetTypeName());
            Assert.Equal("void*", typeof(void).MakePointerType().GetTypeName());
        }

        [Fact]
        public void KeywordIdentifiers()
        {
            var source = @"
public class @object
{
    public class @true { }
}

namespace @return
{
    public class @yield<@async>
    {
        public class @await { }
    }

    namespace @false
    {
        public class @null { }
    }
}
";

            var assembly = GetAssembly(source);
            var objectType = assembly.GetType("object");
            var trueType = objectType.GetNestedType("true");
            var nullType = assembly.GetType("return.false.null");
            var yieldType = assembly.GetType("return.yield`1");
            var constructedYieldType = yieldType.MakeGenericType(nullType);
            var awaitType = yieldType.GetNestedType("await");
            var constructedAwaitType = awaitType.MakeGenericType(nullType);

            Assert.Equal("object", objectType.GetTypeName(escapeKeywordIdentifiers: false));
            Assert.Equal("object.true", trueType.GetTypeName(escapeKeywordIdentifiers: false));
            Assert.Equal("return.false.null", nullType.GetTypeName(escapeKeywordIdentifiers: false));
            Assert.Equal("return.yield<async>", yieldType.GetTypeName(escapeKeywordIdentifiers: false));
            Assert.Equal("return.yield<return.false.null>", constructedYieldType.GetTypeName(escapeKeywordIdentifiers: false));
            Assert.Equal("return.yield<return.false.null>.await", constructedAwaitType.GetTypeName(escapeKeywordIdentifiers: false));

            Assert.Equal("@object", objectType.GetTypeName(escapeKeywordIdentifiers: true));
            Assert.Equal("@object.@true", trueType.GetTypeName(escapeKeywordIdentifiers: true));
            Assert.Equal("@return.@false.@null", nullType.GetTypeName(escapeKeywordIdentifiers: true));
            Assert.Equal("@return.@yield<@async>", yieldType.GetTypeName(escapeKeywordIdentifiers: true));
            Assert.Equal("@return.@yield<@return.@false.@null>", constructedYieldType.GetTypeName(escapeKeywordIdentifiers: true));
            Assert.Equal("@return.@yield<@return.@false.@null>.@await", constructedAwaitType.GetTypeName(escapeKeywordIdentifiers: true));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void DynamicAttribute_KeywordEscaping()
        {
            var attributes = new[] { true };
            Assert.Equal("dynamic", typeof(object).GetTypeName(MakeCustomTypeInfo(attributes), escapeKeywordIdentifiers: false));
            Assert.Equal("dynamic", typeof(object).GetTypeName(MakeCustomTypeInfo(attributes), escapeKeywordIdentifiers: true));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void DynamicAttribute_Locations()
        {
            // Standalone.
            Assert.Equal("dynamic", typeof(object).GetTypeName(MakeCustomTypeInfo(true)));

            // Array element type.
            Assert.Equal("dynamic[]", typeof(object[]).GetTypeName(MakeCustomTypeInfo(false, true)));
            Assert.Equal("dynamic[][]", typeof(object[][]).GetTypeName(MakeCustomTypeInfo(false, false, true)));

            // Type argument of top-level type.
            Assert.Equal("System.Func<dynamic>", typeof(Func<object>).GetTypeName(MakeCustomTypeInfo(false, true)));

            var source = @"
namespace N
{
    public struct A<T>
    {
        public struct B<U>
        {
        }
    }

    public struct C
    {
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var typeBConstructed = typeB.MakeGenericType(typeof(object), typeof(object));

            // Type argument of nested type.
            Assert.Equal("N.A<object>.B<dynamic>", typeBConstructed.GetTypeName(MakeCustomTypeInfo(false, false, true)));
            Assert.Equal("N.A<dynamic>.B<object>", typeBConstructed.GetTypeName(MakeCustomTypeInfo(false, true, false)));
            Assert.Equal("N.A<dynamic>.B<dynamic>[]", typeBConstructed.MakeArrayType().GetTypeName(MakeCustomTypeInfo(false, false, true, true)));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void DynamicAttribute_InvalidFlags()
        {
            // Invalid true.
            Assert.Equal("int", typeof(int).GetTypeName(MakeCustomTypeInfo(true)));
            Assert.Equal("dynamic[]", typeof(object[]).GetTypeName(MakeCustomTypeInfo(true, true)));

            // Too many.
            Assert.Equal("dynamic", typeof(object).GetTypeName(MakeCustomTypeInfo(true, true)));
            Assert.Equal("object", typeof(object).GetTypeName(MakeCustomTypeInfo(false, true)));

            // Too few.
            Assert.Equal("object[]", typeof(object[]).GetTypeName(MakeCustomTypeInfo(true)));
            Assert.Equal("object[]", typeof(object[]).GetTypeName(MakeCustomTypeInfo(false)));

            // Type argument of top-level type.
            Assert.Equal("System.Func<object>", typeof(Func<object>).GetTypeName(MakeCustomTypeInfo(true)));

            var source = @"
namespace N
{
    public struct A<T>
    {
        public struct B<U>
        {
        }
    }

    public struct C
    {
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var typeBConstructed = typeB.MakeGenericType(typeof(object), typeof(object));

            // Type argument of nested type.
            Assert.Equal("N.A<object>.B<object>", typeBConstructed.GetTypeName(MakeCustomTypeInfo(false)));
            Assert.Equal("N.A<dynamic>.B<object>", typeBConstructed.GetTypeName(MakeCustomTypeInfo(false, true)));
            Assert.Equal("N.A<dynamic>.B<object>[]", typeBConstructed.MakeArrayType().GetTypeName(MakeCustomTypeInfo(false, false, true)));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087216")]
        public void DynamicAttribute_OtherGuid()
        {
            var typeInfo = DkmClrCustomTypeInfo.Create(Guid.NewGuid(), new ReadOnlyCollection<byte>(new byte[] { 1 }));
            Assert.Equal("object", typeof(object).GetTypeName(typeInfo));
            Assert.Equal("object[]", typeof(object[]).GetTypeName(typeInfo));
        }

        [Fact]
        public void MangledTypeParameterName()
        {
            var il = @"
.class public auto ansi beforefieldinit Type`1<'<>Mangled'>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(il, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);
            var type = assembly.GetType("Type`1");
            var typeName = type.GetTypeName();
            Assert.Equal("Type<<>Mangled>", typeName);
        }
    }
}
