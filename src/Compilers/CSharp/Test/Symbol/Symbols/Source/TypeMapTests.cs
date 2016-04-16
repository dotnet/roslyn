// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeMapTests : CSharpTestBase
    {
        // take a type of the form Something<X> and return the type X.
        private TypeSymbol TypeArg(TypeSymbol t)
        {
            var nts = t as NamedTypeSymbol;
            Assert.NotEqual(null, nts);
            Assert.Equal(1, nts.Arity);
            return nts.TypeArguments[0];
        }

        [Fact]
        public void TestMap1()
        {
            var text =
@"
public class Box<T> {}
public class A<T> {
  public class TBox : Box<T> {}
  public class B<U> {
    public class TBox : Box<T> {}
    public class UBox : Box<U> {}
    public class C {
      public class TBox : Box<T> {}
      public class UBox : Box<U> {}
    }
  }
}
public class E {}
public class F {}
public class Top : A<E> { // base is A<E>
  public class BF : B<F> {} // base is A<E>.B<F>
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var at = global.GetTypeMembers("A", 1).Single(); // A<T>
            var t = at.TypeParameters[0];
            Assert.Equal(t, TypeArg(at.GetTypeMembers("TBox", 0).Single().BaseType));
            var atbu = at.GetTypeMembers("B", 1).Single(); // A<T>.B<U>
            var u = atbu.TypeParameters[0];
            var c = atbu.GetTypeMembers("C", 0).Single(); // A<T>.B<U>.C
            Assert.Equal(atbu, c.ContainingType);
            Assert.Equal(u, TypeArg(c.ContainingType));
            Assert.Equal(at, c.ContainingType.ContainingType);
            Assert.Equal(t, TypeArg(c.ContainingType.ContainingType));
            var e = global.GetTypeMembers("E", 0).Single(); // E
            var f = global.GetTypeMembers("F", 0).Single(); // F
            var top = global.GetTypeMembers("Top", 0).Single(); // Top
            var ae = top.BaseType; // A<E>
            Assert.Equal(at, ae.OriginalDefinition);
            Assert.Equal(at, at.ConstructedFrom);
            Assert.Equal(e, TypeArg(ae));
            var bf = top.GetTypeMembers("BF", 0).Single(); // Top.BF
            Assert.Equal(top, bf.ContainingType);
            var aebf = bf.BaseType;
            Assert.Equal(f, TypeArg(aebf));
            Assert.Equal(ae, aebf.ContainingType);
            var aebfc = aebf.GetTypeMembers("C", 0).Single(); // A<E>.B<F>.C
            Assert.Equal(c, aebfc.OriginalDefinition);
            Assert.NotEqual(c, aebfc.ConstructedFrom);
            Assert.Equal(f, TypeArg(aebfc.ContainingType));
            Assert.Equal(e, TypeArg(aebfc.ContainingType.ContainingType));
            Assert.Equal(e, TypeArg(aebfc.GetTypeMembers("TBox", 0).Single().BaseType));
            Assert.Equal(f, TypeArg(aebfc.GetTypeMembers("UBox", 0).Single().BaseType)); // exercises alpha-renaming.
            Assert.Equal(aebfc, DeepConstruct(c, ImmutableArray.Create<TypeSymbol>(e, f))); // exercise DeepConstruct
        }

        /// <summary>
        /// Returns a constructed type given the type it is constructed from and type arguments for its enclosing types and itself.
        /// </summary>
        /// <param name="typeArguments">the type arguments that will replace the type parameters, starting with those for enclosing types</param>
        /// <returns></returns>
        private static NamedTypeSymbol DeepConstruct(NamedTypeSymbol type, ImmutableArray<TypeSymbol> typeArguments)
        {
            Assert.True(type.IsDefinition);
            var allTypeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            type.GetAllTypeParameters(allTypeParameters);
            return new TypeMap(allTypeParameters.ToImmutableAndFree(), typeArguments.SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers)).SubstituteNamedType(type);
        }

        [Fact]
        public void ConstructedError()
        {
            var text =
@"
class C
{
    NonExistentType<int> field;
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
            var field = c.GetMembers("field").Single() as FieldSymbol;
            var neti = field.Type as NamedTypeSymbol;
            Assert.Equal(SpecialType.System_Int32, neti.TypeArguments[0].SpecialType);
        }

        [Fact]
        public void Generics4()
        {
            string source = @"
class C1<C1T1, C1T2>
{
    public class C2<C2T1, C2T2>
    {
        public class C3<C3T1, C3T2>
        {
            public C1<int, C3T2>.C2<byte, C3T2>.C3<char, C3T2> V1;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            var _int = compilation.GetSpecialType(SpecialType.System_Int32);
            var _byte = compilation.GetSpecialType(SpecialType.System_Byte);
            var _char = compilation.GetSpecialType(SpecialType.System_Char);
            var C1 = compilation.GetTypeByMetadataName("C1`2");
            var c1OfByteChar = C1.Construct(_byte, _char);

            Assert.Equal("C1<System.Byte, System.Char>", c1OfByteChar.ToTestDisplayString());

            var c1OfByteChar_c2 = (NamedTypeSymbol)(c1OfByteChar.GetMembers()[0]);
            var c1OfByteChar_c2OfIntInt = c1OfByteChar_c2.Construct(_int, _int);

            Assert.Equal("C1<System.Byte, System.Char>.C2<System.Int32, System.Int32>", c1OfByteChar_c2OfIntInt.ToTestDisplayString());

            var c1OfByteChar_c2OfIntInt_c3 = (NamedTypeSymbol)(c1OfByteChar_c2OfIntInt.GetMembers()[0]);
            var c1OfByteChar_c2OfIntInt_c3OfIntByte = c1OfByteChar_c2OfIntInt_c3.Construct(_int, _byte);

            Assert.Equal("C1<System.Byte, System.Char>.C2<System.Int32, System.Int32>.C3<System.Int32, System.Byte>", c1OfByteChar_c2OfIntInt_c3OfIntByte.ToTestDisplayString());

            var v1 = c1OfByteChar_c2OfIntInt_c3OfIntByte.GetMembers().OfType<FieldSymbol>().First();
            var type = v1.Type;

            Assert.Equal("C1<System.Int32, System.Byte>.C2<System.Byte, System.Byte>.C3<System.Char, System.Byte>", type.ToTestDisplayString());
        }

        [Fact]
        public void Generics5()
        {
            string source = @"
class C1<C1T1, C1T2>
{
    public class C2<C2T1, C2T2>
    {
        public class C3<C3T1, C3T2>
        {
            public C1<int, C3T2>.C2<byte, C3T2>.C3<char, C3T2> V1;
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);

            var _int = compilation.GetSpecialType(SpecialType.System_Int32);
            var _byte = compilation.GetSpecialType(SpecialType.System_Byte);
            var _char = compilation.GetSpecialType(SpecialType.System_Char);
            var C1 = compilation.GetTypeByMetadataName("C1`2");

            var c1OfByteChar = C1.Construct(_byte, _char);

            Assert.Equal("C1<System.Byte, System.Char>", c1OfByteChar.ToTestDisplayString());
            var c1OfByteChar_c2 = (NamedTypeSymbol)(c1OfByteChar.GetMembers()[0]);
            Assert.Throws<ArgumentException>(() =>
            {
                var c1OfByteChar_c2OfIntInt = c1OfByteChar_c2.Construct(_byte, _char, _int, _int);
            });
        }
    }
}
