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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol structType = @class.GetMember<FieldSymbol>("i").Type;
            TypeSymbol classType = @class.GetMember<FieldSymbol>("s").Type;
            TypeSymbol interfaceType = @class.GetMember<FieldSymbol>("f").Type;
            TypeSymbol enumType = @class.GetMember<FieldSymbol>("e").Type;
            TypeSymbol errorType = @class.GetMember<FieldSymbol>("err").Type;
            TypeSymbol voidType = @class.GetMember<MethodSymbol>("M").ReturnType;

            TypeSymbol arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            TypeSymbol arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            TypeSymbol arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;
            TypeSymbol pointerType1 = @class.GetMember<FieldSymbol>("p1").Type;
            TypeSymbol pointerType2 = @class.GetMember<FieldSymbol>("p2").Type;
            TypeSymbol genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            TypeSymbol genericType2 = @class.GetMember<FieldSymbol>("g2").Type;

            TypeSymbol[] types = new[]
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

            foreach (TypeSymbol t1 in types)
            {
                foreach (TypeSymbol t2 in types)
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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol structType = @class.GetMember<FieldSymbol>("i").Type;
            TypeSymbol classType = @class.GetMember<FieldSymbol>("s").Type;
            TypeSymbol interfaceType = @class.GetMember<FieldSymbol>("f").Type;
            TypeSymbol errorType = @class.GetMember<FieldSymbol>("e").Type;
            TypeSymbol voidType = @class.GetMember<MethodSymbol>("M").ReturnType;

            TypeSymbol arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            TypeSymbol arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            TypeSymbol arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;
            TypeSymbol pointerType1 = @class.GetMember<FieldSymbol>("p1").Type;
            TypeSymbol pointerType2 = @class.GetMember<FieldSymbol>("p2").Type;
            TypeSymbol genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            TypeSymbol genericType2 = @class.GetMember<FieldSymbol>("g2").Type;

            TypeSymbol typeParam1 = @class.GetMember<FieldSymbol>("tp1").Type;
            TypeSymbol typeParam2 = @class.GetMember<FieldSymbol>("tp2").Type;

            TypeSymbol[] substitutableTypes = new[]
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

            foreach (TypeSymbol t in substitutableTypes)
            {
                AssertCanUnify(typeParam1, t);
            }

            TypeSymbol[] unsubstitutableTypes = new[]
            {
                voidType,
                //UNDONE: pointerType1,
                //UNDONE: pointerType2,
            };

            foreach (TypeSymbol t in unsubstitutableTypes)
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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol arrayType1 = @class.GetMember<FieldSymbol>("a1").Type;
            TypeSymbol arrayType2 = @class.GetMember<FieldSymbol>("a2").Type;
            TypeSymbol arrayType3 = @class.GetMember<FieldSymbol>("a3").Type;

            TypeSymbol genericType1 = @class.GetMember<FieldSymbol>("g1").Type;
            TypeSymbol genericType2 = @class.GetMember<FieldSymbol>("g2").Type;
            TypeSymbol genericType3 = @class.GetMember<FieldSymbol>("g3").Type;

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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol type1 = @class.GetMember<FieldSymbol>("g1").Type;
            TypeSymbol type2 = @class.GetMember<FieldSymbol>("g2").Type;
            TypeSymbol type3 = @class.GetMember<FieldSymbol>("g3").Type;
            TypeSymbol type4 = @class.GetMember<FieldSymbol>("g4").Type;
            TypeSymbol type5 = @class.GetMember<FieldSymbol>("g5").Type;
            TypeSymbol type6 = @class.GetMember<FieldSymbol>("g6").Type;
            TypeSymbol type7 = @class.GetMember<FieldSymbol>("g7").Type;

            TypeSymbol[] types1To4 = new[] { type1, type2, type3, type4 };

            foreach (TypeSymbol t1 in types1To4)
            {
                foreach (TypeSymbol t2 in types1To4)
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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol type1 = @class.GetMember<FieldSymbol>("g1").Type;
            TypeSymbol type2 = @class.GetMember<FieldSymbol>("g2").Type;
            TypeSymbol type3 = @class.GetMember<FieldSymbol>("g3").Type;
            TypeSymbol type4 = @class.GetMember<FieldSymbol>("g4").Type;
            TypeSymbol type5 = @class.GetMember<FieldSymbol>("g5").Type;
            TypeSymbol type6 = @class.GetMember<FieldSymbol>("g6").Type;

            TypeSymbol[] types1To4 = new[] { type1, type2, type3, type4 };

            foreach (TypeSymbol t1 in types1To4)
            {
                foreach (TypeSymbol t2 in types1To4)
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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol containedType = @class.GetMember<FieldSymbol>("contained").Type;
            TypeSymbol containingType = @class.GetMember<FieldSymbol>("containing").Type;

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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;

            NamedTypeSymbol @class = global.GetMember<NamedTypeSymbol>("C");

            TypeSymbol type1 = @class.GetMember<FieldSymbol>("t1").Type;
            TypeSymbol type2 = @class.GetMember<FieldSymbol>("t2").Type;
            TypeSymbol type3 = @class.GetMember<FieldSymbol>("t3").Type;
            TypeSymbol type4 = @class.GetMember<FieldSymbol>("t4").Type;
            TypeSymbol type5 = @class.GetMember<FieldSymbol>("t5").Type;
            TypeSymbol type6 = @class.GetMember<FieldSymbol>("t6").Type;

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
            CSharpCompilation comp = CreateCompilation(text);
            NamedTypeSymbol type = comp.GetMember<NamedTypeSymbol>("IB");
            AssertCanUnify(type.Interfaces()[0], type.Interfaces()[1]);
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
