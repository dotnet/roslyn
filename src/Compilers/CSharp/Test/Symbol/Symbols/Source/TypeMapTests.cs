// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeMapTests : CSharpTestBase
    {
        [Theory]
        [InlineData("C")]
        [InlineData("C<string>")]
        [InlineData("C<string, int>")]
        [InlineData("C<string, int, byte, char, long, short, bool, object>")]
        public void SubstituteNamedType_NoChange(string type)
        {
            var (map, previous, _) = CreateSubstitution(type, type);

            Assert.Same(previous, map.SubstituteNamedType(previous));
        }

        [Theory]
        [InlineData("C<T>", "C<int>")]
        [InlineData("C<T, string>", "C<int, string>")]
        [InlineData("C<string, T>", "C<string, int>")]
        [InlineData("C<T, string, byte, char, long, short, bool, object>", "C<int, string, byte, char, long, short, bool, object>")]
        [InlineData("C<string, byte, char, long, short, bool, object, T>", "C<string, byte, char, long, short, bool, object, int>")]
        [InlineData("C<string, C<T, T>>", "C<string, C<int, int>>")]
        [InlineData("Outer<T>.C<C<string, T>>", "Outer<int>.C<C<string, int>>")]
        public void SubstituteNamedType_ChangedArguments(string type, string substitutedType)
        {
            var (map, previous, expected) = CreateSubstitution(type, substitutedType);

            var actual = map.SubstituteNamedType(previous);

            Assert.NotSame(previous, actual);
            Assert.True(TypeSymbol.Equals(expected, actual, TypeCompareKind.ConsiderEverything));
            Assert.Same(previous.OriginalDefinition, actual.OriginalDefinition);
            Assert.True(TypeSymbol.Equals(actual, map.SubstituteNamedType(actual), TypeCompareKind.ConsiderEverything));
        }

        [Theory]
        [InlineData("Outer<T>.C", "Outer<int>.C")]
        [InlineData("Outer<T>.C<string>", "Outer<int>.C<string>")]
        [InlineData("Outer<T>.C<string, byte>", "Outer<int>.C<string, byte>")]
        [InlineData("Outer<T>.C<string, byte, char, long, short, bool, object, double>", "Outer<int>.C<string, byte, char, long, short, bool, object, double>")]
        public void SubstituteNamedType_ContainingTypeOnly(string type, string substitutedType)
        {
            var (map, previous, expected) = CreateSubstitution(type, substitutedType);

            var actual = map.SubstituteNamedType(previous);

            Assert.NotSame(previous, actual);
            Assert.True(TypeSymbol.Equals(expected, actual, TypeCompareKind.ConsiderEverything));
            Assert.Same(previous.OriginalDefinition, actual.OriginalDefinition);
            Assert.Equal(SpecialType.System_Int32, actual.ContainingType.TypeArguments().Single().SpecialType);
            var oldArguments = previous.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            var newArguments = actual.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            Assert.Equal(oldArguments.Length, newArguments.Length);
            for (int i = 0; i < oldArguments.Length; i++)
            {
                Assert.True(oldArguments[i].Equals(newArguments[i], TypeCompareKind.ConsiderEverything));
            }
        }

        private static (TypeMap map, NamedTypeSymbol previous, NamedTypeSymbol expected) CreateSubstitution(string type, string substitutedType)
        {
            var compilation = CreateCompilation($$"""
                public class C { }
                public class C<T> { }
                public class C<T1, T2> { }
                public class C<T1, T2, T3, T4, T5, T6, T7, T8> { }
                public class Outer<T>
                {
                    public class C { }
                    public class C<T1> { }
                    public class C<T1, T2> { }
                    public class C<T1, T2, T3, T4, T5, T6, T7, T8> { }
                }
                public class Context<T>
                {
                    public {{type}} Previous { get; set; }
                    public {{substitutedType}} Expected { get; set; }
                }
                """);
            compilation.VerifyEmitDiagnostics();
            var context = compilation.GetTypeByMetadataName("Context`1");
            var map = new TypeMap(context.TypeParameters,
                ImmutableArray.Create(TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Int32))));
            var previous = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Previous").Single()).Type;
            var expected = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Expected").Single()).Type;
            return (map, previous, expected);
        }

        [Fact]
        public void SubstituteNamedType_TupleNamesAndNullableArguments()
        {
            var compilation = CreateCompilation("""
                #nullable enable
                public class C<T1, T2> { }
                public class Context<T> where T : class
                {
                    public C<string?, (T? first, C<string?, T> second)> Previous => throw null!;
                    public C<string?, (object? first, C<string?, object> second)> Expected => throw null!;
                }
                """, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyEmitDiagnostics();
            var context = compilation.GetTypeByMetadataName("Context`1");
            var previous = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Previous").Single()).Type;
            var expected = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Expected").Single()).Type;
            var map = new TypeMap(context.TypeParameters,
                ImmutableArray.Create(TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), NullableAnnotation.NotAnnotated)));

            var actual = map.SubstituteNamedType(previous);

            Assert.True(TypeSymbol.Equals(expected, actual, TypeCompareKind.ConsiderEverything));
            var arguments = actual.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            Assert.Equal(NullableAnnotation.Annotated, arguments[0].NullableAnnotation);
            var tuple = (NamedTypeSymbol)arguments[1].Type;
            Assert.Equal(new[] { "first", "second" }, tuple.TupleElementNames);
            Assert.Equal(NullableAnnotation.Annotated, tuple.TupleElements[0].TypeWithAnnotations.NullableAnnotation);
            Assert.Equal(SpecialType.System_Object, tuple.TupleElements[0].Type.SpecialType);
        }

        [Fact]
        public void SubstituteNamedType_NullabilityOnly()
        {
            var compilation = CreateCompilation("""
                #nullable enable
                public class C<T1, T2> { }
                public class Context<T> where T : class
                {
                    public C<string?, T> Previous => throw null!;
                    public C<string?, T?> Expected => throw null!;
                }
                """);
            compilation.VerifyEmitDiagnostics();
            var context = compilation.GetTypeByMetadataName("Context`1");
            var previous = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Previous").Single()).Type;
            var expected = (NamedTypeSymbol)((PropertySymbol)context.GetMembers("Expected").Single()).Type;
            var map = new TypeMap(context.TypeParameters,
                ImmutableArray.Create(TypeWithAnnotations.Create(context.TypeParameters.Single(), NullableAnnotation.Annotated)));

            var actual = map.SubstituteNamedType(previous);

            Assert.NotSame(previous, actual);
            Assert.True(TypeSymbol.Equals(expected, actual, TypeCompareKind.ConsiderEverything));
            var oldArgument = previous.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[1];
            var newArgument = actual.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[1];
            Assert.Same(oldArgument.Type, newArgument.Type);
            Assert.Equal(NullableAnnotation.NotAnnotated, oldArgument.NullableAnnotation);
            Assert.Equal(NullableAnnotation.Annotated, newArgument.NullableAnnotation);
            Assert.False(oldArgument.IsSameAs(newArgument));
        }

        [Fact]
        public void SubstituteNamedType_CustomModifierOnly()
        {
            var compilation = CreateCompilation("""
                public class C<T1, T2> { }
                public class Modifier<T> { }
                """);
            compilation.VerifyEmitDiagnostics();
            var definition = compilation.GetTypeByMetadataName("C`2");
            var modifier = compilation.GetTypeByMetadataName("Modifier`1");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var previous = definition.Construct(ImmutableArray.Create(
                TypeWithAnnotations.Create(stringType, NullableAnnotation.Annotated),
                TypeWithAnnotations.Create(intType, customModifiers: ImmutableArray.Create<CustomModifier>(
                    CSharpCustomModifier.CreateOptional(modifier),
                    CSharpCustomModifier.CreateRequired(stringType)))));
            var map = new TypeMap(modifier.TypeParameters, ImmutableArray.Create(TypeWithAnnotations.Create(intType)));

            var actual = map.SubstituteNamedType(previous);

            Assert.NotSame(previous, actual);
            var arguments = actual.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            Assert.Same(stringType, arguments[0].Type);
            Assert.Equal(NullableAnnotation.Annotated, arguments[0].NullableAnnotation);
            Assert.Same(intType, arguments[1].Type);
            Assert.Collection(arguments[1].CustomModifiers,
                m =>
                {
                    Assert.True(m.IsOptional);
                    Assert.True(TypeSymbol.Equals(modifier.Construct(intType), ((CSharpCustomModifier)m).ModifierSymbol, TypeCompareKind.ConsiderEverything));
                },
                m =>
                {
                    Assert.False(m.IsOptional);
                    Assert.Same(stringType, ((CSharpCustomModifier)m).ModifierSymbol);
                });
            Assert.False(previous.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[1].IsSameAs(arguments[1]));
        }

        // take a type of the form Something<X> and return the type X.
        private TypeSymbol TypeArg(TypeSymbol t)
        {
            var nts = t as NamedTypeSymbol;
            Assert.NotNull(nts);
            Assert.Equal(1, nts.Arity);
            return nts.TypeArguments()[0];
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
            var comp = CreateEmptyCompilation(text);
            var global = comp.GlobalNamespace;
            var at = global.GetTypeMembers("A", 1).Single(); // A<T>
            var t = at.TypeParameters[0];
            Assert.Equal(t, TypeArg(at.GetTypeMembers("TBox", 0).Single().BaseType()));
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
            var ae = top.BaseType(); // A<E>
            Assert.Equal(at, ae.OriginalDefinition);
            Assert.Equal(at, at.ConstructedFrom);
            Assert.Equal(e, TypeArg(ae));
            var bf = top.GetTypeMembers("BF", 0).Single(); // Top.BF
            Assert.Equal(top, bf.ContainingType);
            var aebf = bf.BaseType();
            Assert.Equal(f, TypeArg(aebf));
            Assert.Equal(ae, aebf.ContainingType);
            var aebfc = aebf.GetTypeMembers("C", 0).Single(); // A<E>.B<F>.C
            Assert.Equal(c, aebfc.OriginalDefinition);
            Assert.NotEqual(c, aebfc.ConstructedFrom);
            Assert.Equal(f, TypeArg(aebfc.ContainingType));
            Assert.Equal(e, TypeArg(aebfc.ContainingType.ContainingType));
            Assert.Equal(e, TypeArg(aebfc.GetTypeMembers("TBox", 0).Single().BaseType()));
            Assert.Equal(f, TypeArg(aebfc.GetTypeMembers("UBox", 0).Single().BaseType())); // exercises alpha-renaming.
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
            return new TypeMap(allTypeParameters.ToImmutableAndFree(), typeArguments.SelectAsArray(t => TypeWithAnnotations.Create(t))).SubstituteNamedType(type);
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
            var comp = CreateCompilation(tree);

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
            var field = c.GetMembers("field").Single() as FieldSymbol;
            var neti = field.Type as NamedTypeSymbol;
            Assert.Equal(SpecialType.System_Int32, neti.TypeArguments()[0].SpecialType);
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
            var compilation = CreateCompilation(source);

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
            var type = v1.TypeWithAnnotations;

            Assert.Equal("C1<System.Int32, System.Byte>.C2<System.Byte, System.Byte>.C3<System.Char, System.Byte>", type.Type.ToTestDisplayString());
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

            var compilation = CreateCompilation(source);

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
