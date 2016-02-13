// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeUnificationTests : CSharpTestBase
    {
        [Fact]
        public void TestNoTypeParameters()
        {
            var text =
@"
enum E
{
    Element,
}

class C
{
    //atomic
    int i;
    string s;
    System.IFormattable f;
    E e;
    AnErrorType err;
    void M() { }

    //recursive
    int[] a1;
    int[][] a2;
    int[,] a3;
    int* p1;
    int** p2;
    System.Collections.Generic.Dictionary<int, long> g1;
    System.Collections.Generic.Dictionary<long, int> g2;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var structType = @class.GetMember<FieldSymbol>("i").Type;
            var classType = @class.GetMember<FieldSymbol>("s").Type;
            var interfaceType = @class.GetMember<FieldSymbol>("f").Type;
            var enumType = @class.GetMember<FieldSymbol>("e").Type;
            var errorType = @class.GetMember<FieldSymbol>("err").Type;
            var voidType = @class.GetMember<MethodSymbol>("M").ReturnType;

            var arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            var arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            var arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;
            var pointerType1 = @class.GetMember<FieldSymbol>("p1").Type;
            var pointerType2 = @class.GetMember<FieldSymbol>("p2").Type;
            var genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            var genericType2 = @class.GetMember<FieldSymbol>("g2").Type;

            var types = new[]
            {
                structType,
                classType,
                interfaceType,
                enumType,
                errorType,
                voidType,
                arrayType1,
                arrayType2,
                arrayType3,
                //UNDONE: pointerType1,
                //UNDONE: pointerType2,
                genericType1,
                genericType2,
            };

            foreach (var t1 in types)
            {
                foreach (var t2 in types)
                {
                    if (ReferenceEquals(t1, t2))
                    {
                        AssertCanUnify(t1, t2);
                    }
                    else
                    {
                        AssertCannotUnify(t1, t2);
                    }
                }
            }

            AssertCanUnify(null, null);
            AssertCannotUnify(classType, null);
        }

        [Fact]
        public void TestJustTypeParameter()
        {
            var text =
@"
class C<T, U>
{
    //atomic
    int i;
    string s;
    System.IFormattable f;
    AnErrorType e;
    void M() { }

    //recursive
    int[] a1;
    int[][] a2;
    int[,] a3;
    int* p1;
    int** p2;
    System.Collections.Generic.Dictionary<int, long> g1;
    System.Collections.Generic.Dictionary<long, int> g2;

    T tp1;
    U tp2;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var structType = @class.GetMember<FieldSymbol>("i").Type;
            var classType = @class.GetMember<FieldSymbol>("s").Type;
            var interfaceType = @class.GetMember<FieldSymbol>("f").Type;
            var errorType = @class.GetMember<FieldSymbol>("e").Type;
            var voidType = @class.GetMember<MethodSymbol>("M").ReturnType;

            var arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            var arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            var arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;
            var pointerType1 = @class.GetMember<FieldSymbol>("p1").Type;
            var pointerType2 = @class.GetMember<FieldSymbol>("p2").Type;
            var genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            var genericType2 = @class.GetMember<FieldSymbol>("g2").Type;

            var typeParam1 = @class.GetMember<FieldSymbol>("tp1").Type;
            var typeParam2 = @class.GetMember<FieldSymbol>("tp2").Type;

            var substitutableTypes = new[]
            {
                structType,
                classType,
                interfaceType,
                errorType,
                arrayType1,
                arrayType2,
                arrayType3,
                genericType1,
                genericType2,
            };

            foreach (var t in substitutableTypes)
            {
                AssertCanUnify(typeParam1, t);
            }

            var unsubstitutableTypes = new[]
            {
                voidType,
                //UNDONE: pointerType1,
                //UNDONE: pointerType2,
            };

            foreach (var t in unsubstitutableTypes)
            {
                AssertCannotUnify(typeParam1, t);
            }

            AssertCanUnify(typeParam1, typeParam2);
        }

        [Fact]
        public void TestArrayTypes()
        {
            var text =
@"
class C<T>
{
    int[] a1;
    int[][] a2;
    int[,] a3;

    T[] g1;
    T[][] g2;
    T[,] g3;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            var arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            var arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;

            var genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            var genericType2 = @class.GetMember<FieldSymbol>("g2").Type;
            var genericType3 = @class.GetMember<FieldSymbol>("g3").Type;

            AssertCanUnify(genericType1, arrayType1);
            AssertCanUnify(genericType2, arrayType2);
            AssertCanUnify(genericType3, arrayType3);
        }

        //UNDONE: public void TestPointerTypes()

        [Fact]
        public void TestNamedTypes()
        {
            var text =
@"
class C<W, X>
{
    C<int, short> g1;
    C<W, short> g2;
    C<int, X> g3;
    C<W, X> g4;

    C<W, W> g5;
    C<int, W> g6;

    D<T> g7;
}

class D<T>
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var type1 = @class.GetMember<FieldSymbol>("g1").Type;
            var type2 = @class.GetMember<FieldSymbol>("g2").Type;
            var type3 = @class.GetMember<FieldSymbol>("g3").Type;
            var type4 = @class.GetMember<FieldSymbol>("g4").Type;
            var type5 = @class.GetMember<FieldSymbol>("g5").Type;
            var type6 = @class.GetMember<FieldSymbol>("g6").Type;
            var type7 = @class.GetMember<FieldSymbol>("g7").Type;

            var types1To4 = new[] { type1, type2, type3, type4 };

            foreach (var t1 in types1To4)
            {
                foreach (var t2 in types1To4)
                {
                    AssertCanUnify(t1, t2);
                }
            }

            AssertCannotUnify(type5, type1);
            AssertCannotUnify(type6, type2);
            AssertCannotUnify(type7, type3);
        }

        [Fact]
        public void TestNestedNamedTypes()
        {
            var text =
@"
class C<X, Y>
{
    L<int>.M<short> g1;
    L<X>.M<short> g2;
    L<int>.M<Y> g3;
    L<X>.M<Y> g4;

    L<X>.M<X> g5;
    L<int>.M<X> g6;
}

public class L<T>
{
    public class M<U>
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var type1 = @class.GetMember<FieldSymbol>("g1").Type;
            var type2 = @class.GetMember<FieldSymbol>("g2").Type;
            var type3 = @class.GetMember<FieldSymbol>("g3").Type;
            var type4 = @class.GetMember<FieldSymbol>("g4").Type;
            var type5 = @class.GetMember<FieldSymbol>("g5").Type;
            var type6 = @class.GetMember<FieldSymbol>("g6").Type;

            var types1To4 = new[] { type1, type2, type3, type4 };

            foreach (var t1 in types1To4)
            {
                foreach (var t2 in types1To4)
                {
                    AssertCanUnify(t1, t2);
                }
            }

            AssertCannotUnify(type5, type1);
            AssertCannotUnify(type6, type2);
        }

        [Fact]
        public void TestOccursCheck()
        {
            var text =
@"
class C<T>
{
    T contained;
    C<T> containing;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var containedType = @class.GetMember<FieldSymbol>("contained").Type;
            var containingType = @class.GetMember<FieldSymbol>("containing").Type;

            AssertCannotUnify(containedType, containingType);
        }

        [Fact]
        public void TestRecursiveCases()
        {
            var text =
@"
class C<W, X, Y, Z>
{
    L<L<X[]>.M<Y>[]>.M<L<Y[,]>.M<X[]>[,]> t1;
    L<L<int[]>.M<short>[]>.M<L<short[,]>.M<int[]>[,]> t2;
    L<Y>.M<Z> t3;
    L<W>.M<Z> t4;
    L<W[]>.M<Z[,]> t5;
    L<W[,]>.M<Z[]> t6;
}

public class L<T>
{
    public class M<U>
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var type1 = @class.GetMember<FieldSymbol>("t1").Type;
            var type2 = @class.GetMember<FieldSymbol>("t2").Type;
            var type3 = @class.GetMember<FieldSymbol>("t3").Type;
            var type4 = @class.GetMember<FieldSymbol>("t4").Type;
            var type5 = @class.GetMember<FieldSymbol>("t5").Type;
            var type6 = @class.GetMember<FieldSymbol>("t6").Type;

            AssertCanUnify(type1, type1);
            AssertCanUnify(type1, type2);
            AssertCannotUnify(type1, type3);
            AssertCanUnify(type1, type4);
            AssertCanUnify(type1, type5);
            AssertCannotUnify(type1, type6);

            AssertCanUnify(type2, type2);
            AssertCanUnify(type2, type3);
            AssertCanUnify(type2, type4);
            AssertCanUnify(type2, type5);
            AssertCannotUnify(type2, type6);

            AssertCanUnify(type3, type3);
            AssertCanUnify(type3, type4);
            AssertCannotUnify(type3, type5);
            AssertCannotUnify(type3, type6);

            AssertCanUnify(type4, type4);
            AssertCannotUnify(type4, type5);
            AssertCannotUnify(type4, type6);

            AssertCanUnify(type5, type5);
            AssertCannotUnify(type5, type6);

            AssertCanUnify(type6, type6);
        }

        [WorkItem(1042692, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042692")]
        [Fact]
        public void SubstituteWithOtherTypeParameter()
        {
            var text =
@"interface IA<T, U>
{
}
interface IB<T, U> : IA<U, object>, IA<T, U>
{
}";
            var comp = CreateCompilationWithMscorlib(text);
            var type = comp.GetMember<NamedTypeSymbol>("IB");
            AssertCanUnify(type.Interfaces[0], type.Interfaces[1]);
            DiagnosticsUtils.VerifyErrorCodes(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 4, Column = 11 });
        }

        private static void AssertCanUnify(TypeSymbol t1, TypeSymbol t2)
        {
            Assert.True(TypeUnification.CanUnify(t1, t2), string.Format("{0} vs {1}", t1, t2));
            Assert.True(TypeUnification.CanUnify(t2, t1), string.Format("{0} vs {1}", t2, t1));
        }

        private static void AssertCannotUnify(TypeSymbol t1, TypeSymbol t2)
        {
            Assert.False(TypeUnification.CanUnify(t1, t2), string.Format("{0} vs {1}", t1, t2));
            Assert.False(TypeUnification.CanUnify(t2, t1), string.Format("{0} vs {1}", t2, t1));
        }
    }
}
