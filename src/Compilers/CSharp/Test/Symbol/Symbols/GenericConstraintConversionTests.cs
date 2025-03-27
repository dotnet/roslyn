// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class GenericConstraintConversionTests : CSharpTestBase
    {
        /// <summary>
        /// 6.1.10, bullet 1
        /// </summary>
        [Fact]
        public void ImplicitInterfaceAndBaseTypeConversions01()
        {
            var source =
@"interface I { }
interface IA : I { }
interface IB : I { }
class A : IA { }
class B : A, IB { }
class C<T>
    where T : B
{
    static I i;
    static IA ia;
    static IB ib;
    static A a;
    static B b;
    static void M<U, V>(T t, U u, V v)
        where U : struct, IA
        where V : class, IA
    {
        i = t;
        i = u;
        i = v;
        ia = t;
        ia = u;
        ia = v;
        ib = t;
        ib = u;
        ib = v;
        a = t;
        a = u;
        b = t;
        b = u;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (25,14): error CS0266: Cannot implicitly convert type 'U' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "IB").WithLocation(25, 14),
                // (26,14): error CS0266: Cannot implicitly convert type 'V' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v").WithArguments("V", "IB").WithLocation(26, 14),
                // (28,13): error CS0029: Cannot implicitly convert type 'U' to 'A'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "A").WithLocation(28, 13),
                // (30,13): error CS0029: Cannot implicitly convert type 'U' to 'B'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "B").WithLocation(30, 13));
        }

        [Fact]
        public void ImplicitInterfaceAndBaseTypeConversions02()
        {
            var source =
@"interface IA { }
interface IB { }
class A : IA { }
class B : A, IB { }
class C<T, U>
    where T : A
    where U : B, T
{
    static IA ia;
    static IB ib;
    static void M<V>(T t, U u, V v)
        where V : B, T
    {
        ia = t;
        ia = u;
        ia = v;
        ib = t;
        ib = u;
        ib = v;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,14): error CS0266: Cannot implicitly convert type 'T' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "IB").WithLocation(17, 14));
        }

        /// <summary>
        /// 6.1.10, bullet 2
        /// </summary>
        [Fact]
        public void ImplicitConversionEffectiveInterfaceSet()
        {
            var source =
@"interface IA { }
interface IB { }
class A : IA { }
class B : IB { }
class C<T, U, V>
    where T : A
    where U : IB
    where V : B, IA
{
    static IA a;
    static IB b;
    static void M(T t, U u, V v)
    {
        a = t;
        a = u;
        b = t;
        b = u;
    }
    static void M1<X>(X x) where X : T
    {
        a = x;
        b = x;
    }
    static void M2<X>(X x) where X : U
    {
        a = x;
        b = x;
    }
    static void M3<X>(X x) where X : T, U
    {
        a = x;
        b = x;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,13): error CS0266: Cannot implicitly convert type 'U' to 'IA'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "IA").WithLocation(15, 13),
                // (16,13): error CS0266: Cannot implicitly convert type 'T' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "IB").WithLocation(16, 13),
                // (22,13): error CS0266: Cannot implicitly convert type 'X' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("X", "IB").WithLocation(22, 13),
                // (26,13): error CS0266: Cannot implicitly convert type 'X' to 'IA'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("X", "IA").WithLocation(26, 13));
        }

        /// <summary>
        /// 6.1.10, bullet 3
        /// </summary>
        [Fact]
        public void ImplicitConversionToTypeParameter()
        {
            var source =
@"class C<T, U, V, W>
    where T : U, V
    where U : W
{
    static T t;
    static U u;
    static V v;
    static W w;
    static void M()
    {
        t = u;
        t = v;
        t = w;
        u = t;
        u = v;
        u = w;
        v = t;
        v = u;
        v = w;
        w = t;
        w = u;
        w = v;
    }
    static void M1<X>(X x) where X : T
    {
        t = x;
        u = x;
        v = x;
        w = x;
        x = t;
        x = u;
        x = v;
        x = w;
    }
    static void M2<X>(X x) where X : U
    {
        t = x;
        u = x;
        v = x;
        w = x;
        x = t;
        x = u;
        x = v;
        x = w;
    }
    static void M3<X>(X x) where X : U, V, W
    {
        t = x;
        u = x;
        v = x;
        w = x;
        x = t;
        x = u;
        x = v;
        x = w;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,13): error CS0266: Cannot implicitly convert type 'U' to 'T'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "T").WithLocation(11, 13),
                // (12,13): error CS0266: Cannot implicitly convert type 'V' to 'T'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v").WithArguments("V", "T").WithLocation(12, 13),
                // (13,13): error CS0266: Cannot implicitly convert type 'W' to 'T'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("W", "T").WithLocation(13, 13),
                // (15,13): error CS0029: Cannot implicitly convert type 'V' to 'U'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "U").WithLocation(15, 13),
                // (16,13): error CS0266: Cannot implicitly convert type 'W' to 'U'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("W", "U").WithLocation(16, 13),
                // (18,13): error CS0029: Cannot implicitly convert type 'U' to 'V'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "V").WithLocation(18, 13),
                // (19,13): error CS0029: Cannot implicitly convert type 'W' to 'V'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "w").WithArguments("W", "V").WithLocation(19, 13),
                // (22,13): error CS0029: Cannot implicitly convert type 'V' to 'W'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "W").WithLocation(22, 13),
                // (30,13): error CS0266: Cannot implicitly convert type 'T' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "X").WithLocation(30, 13),
                // (31,13): error CS0266: Cannot implicitly convert type 'U' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "X").WithLocation(31, 13),
                // (32,13): error CS0266: Cannot implicitly convert type 'V' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v").WithArguments("V", "X").WithLocation(32, 13),
                // (33,13): error CS0266: Cannot implicitly convert type 'W' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("W", "X").WithLocation(33, 13),
                // (37,13): error CS0029: Cannot implicitly convert type 'X' to 'T'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("X", "T").WithLocation(37, 13),
                // (39,13): error CS0029: Cannot implicitly convert type 'X' to 'V'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("X", "V").WithLocation(39, 13),
                // (41,13): error CS0029: Cannot implicitly convert type 'T' to 'X'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("T", "X").WithLocation(41, 13),
                // (42,13): error CS0266: Cannot implicitly convert type 'U' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "X").WithLocation(42, 13),
                // (43,13): error CS0029: Cannot implicitly convert type 'V' to 'X'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "X").WithLocation(43, 13),
                // (44,13): error CS0266: Cannot implicitly convert type 'W' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("W", "X").WithLocation(44, 13),
                // (48,13): error CS0029: Cannot implicitly convert type 'X' to 'T'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("X", "T").WithLocation(48, 13),
                // (52,13): error CS0029: Cannot implicitly convert type 'T' to 'X'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("T", "X").WithLocation(52, 13),
                // (53,13): error CS0266: Cannot implicitly convert type 'U' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "X").WithLocation(53, 13),
                // (54,13): error CS0266: Cannot implicitly convert type 'V' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v").WithArguments("V", "X").WithLocation(54, 13),
                // (55,13): error CS0266: Cannot implicitly convert type 'W' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "w").WithArguments("W", "X").WithLocation(55, 13));
        }

        /// <summary>
        /// 6.1.10, bullet 4
        /// </summary>
        [Fact]
        public void ImplicitConversionFromNull()
        {
            var source =
@"interface I { }
class A { }
class B<T1, T2, T3, T4, T5, T6>
    where T2 : class
    where T3 : struct
    where T4 : new()
    where T5: I
    where T6: A
{
    static T1 F1 = null;
    static T2 F2 = null;
    static T3 F3 = null;
    static T4 F4 = null;
    static T5 F5 = null;
    static T6 F6 = null;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,20): error CS0403: Cannot convert null to type parameter 'T1' because it could be a non-nullable value type. Consider using 'default(T1)' instead.
                //     static T1 F1 = null;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T1"),
                // (12,20): error CS0403: Cannot convert null to type parameter 'T3' because it could be a non-nullable value type. Consider using 'default(T3)' instead.
                //     static T3 F3 = null;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T3"),
                // (13,20): error CS0403: Cannot convert null to type parameter 'T4' because it could be a non-nullable value type. Consider using 'default(T4)' instead.
                //     static T4 F4 = null;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T4"),
                // (14,20): error CS0403: Cannot convert null to type parameter 'T5' because it could be a non-nullable value type. Consider using 'default(T5)' instead.
                //     static T5 F5 = null;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T5"),
                // (11,15): warning CS0414: The field 'B<T1, T2, T3, T4, T5, T6>.F2' is assigned but its value is never used
                //     static T2 F2 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F2").WithArguments("B<T1, T2, T3, T4, T5, T6>.F2"),
                // (15,15): warning CS0414: The field 'B<T1, T2, T3, T4, T5, T6>.F6' is assigned but its value is never used
                //     static T6 F6 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F6").WithArguments("B<T1, T2, T3, T4, T5, T6>.F6")
            );
        }

        /// <summary>
        /// 6.1.10, bullet 5
        /// </summary>
        [Fact]
        public void ImplicitReferenceConversionToInterface()
        {
            var source =
@"interface IA { }
interface IB : IA { }
class A : IA { }
class B : A, IB { }
class C<T, U>
    where T : A
    where U : B
{
    static void M(T t, U u)
    {
        IA a;
        IB b;
        a = t;
        a = u;
        b = t;
        b = u;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,13): error CS0266: Cannot implicitly convert type 'T' to 'IB'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "IB").WithLocation(15, 13));
        }

        /// <summary>
        /// 6.1.10, bullet 6
        /// </summary>
        [Fact]
        public void ImplicitInterfaceVarianceConversions01()
        {
            var source =
@"interface IIn<in T> { }
interface IOut<out T> { }
interface IInDerived<T> : IIn<T> { }
interface IOutDerived<T> : IOut<T> { }
class CIn<T> : IIn<T> { }
class COut<T> : IOut<T> { }
class C<T, U>
    where T : class
    where U : class, T
{
    static IIn<T> it;
    static IOut<T> ot;
    static IIn<U> iu;
    static IOut<U> ou;
    static void M1<X, Y>(X x, Y y)
        where X : IIn<T>
        where Y : IIn<U>
    {
        it = x;
        it = y;
        iu = x;
        iu = y;
    }
    static void M2<X, Y>(X x, Y y)
        where X : IOut<T>
        where Y : IOut<U>
    {
        ot = x;
        ot = y;
        ou = x;
        ou = y;
    }
    static void M3<X, Y>(X x, Y y)
        where X : IInDerived<T>
        where Y : IInDerived<U>
    {
        it = x;
        it = y;
        iu = x;
        iu = y;
    }
    static void M4<X, Y>(X x, Y y)
        where X : IOutDerived<T>
        where Y : IOutDerived<U>
    {
        ot = x;
        ot = y;
        ou = x;
        ou = y;
    }
    static void M5<X, Y>(X x, Y y)
        where X : CIn<T>
        where Y : CIn<U>
    {
        it = x;
        it = y;
        iu = x;
        iu = y;
    }
    static void M6<X, Y>(X x, Y y)
        where X : COut<T>
        where Y : COut<U>
    {
        ot = x;
        ot = y;
        ou = x;
        ou = y;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (20,14): error CS0266: Cannot implicitly convert type 'Y' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("Y", "IIn<T>").WithLocation(20, 14),
                // (30,14): error CS0266: Cannot implicitly convert type 'X' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("X", "IOut<U>").WithLocation(30, 14),
                // (38,14): error CS0266: Cannot implicitly convert type 'Y' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("Y", "IIn<T>").WithLocation(38, 14),
                // (48,14): error CS0266: Cannot implicitly convert type 'X' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("X", "IOut<U>").WithLocation(48, 14),
                // (56,14): error CS0266: Cannot implicitly convert type 'Y' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("Y", "IIn<T>").WithLocation(56, 14),
                // (66,14): error CS0266: Cannot implicitly convert type 'X' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("X", "IOut<U>").WithLocation(66, 14));
        }

        [Fact]
        public void ImplicitInterfaceVarianceConversions02()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class A<T, U>
    where U : T
{
    static void M(I<T> it, I<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IIn<T> it, IIn<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IOut<T> it, IOut<U> iu)
    {
        it = iu;
        iu = it;
    }
}
class B<T, U>
    where T : class
    where U : T
{
    static void M(I<T> it, I<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IIn<T> it, IIn<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IOut<T> it, IOut<U> iu)
    {
        it = iu;
        iu = it;
    }
}
class C<T, U>
    where T : class
    where U : class, T
{
    static void M(I<T> it, I<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IIn<T> it, IIn<U> iu)
    {
        it = iu;
        iu = it; // valid
    }
    static void M(IOut<T> it, IOut<U> iu)
    {
        it = iu; // valid
        iu = it;
    }
}
class D<T, U>
    where U : struct, T
{
    static void M(I<T> it, I<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IIn<T> it, IIn<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IOut<T> it, IOut<U> iu)
    {
        it = iu;
        iu = it;
    }
}
class E<T, U>
    where T : class
    where U : struct, T
{
    static void M(I<T> it, I<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IIn<T> it, IIn<U> iu)
    {
        it = iu;
        iu = it;
    }
    static void M(IOut<T> it, IOut<U> iu)
    {
        it = iu;
        iu = it;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,14): error CS0266: Cannot implicitly convert type 'I<U>' to 'I<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("I<U>", "I<T>").WithLocation(9, 14),
                // (10,14): error CS0266: Cannot implicitly convert type 'I<T>' to 'I<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("I<T>", "I<U>").WithLocation(10, 14),
                // (14,14): error CS0266: Cannot implicitly convert type 'IIn<U>' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IIn<U>", "IIn<T>").WithLocation(14, 14),
                // (15,14): error CS0266: Cannot implicitly convert type 'IIn<T>' to 'IIn<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IIn<T>", "IIn<U>").WithLocation(15, 14),
                // (19,14): error CS0266: Cannot implicitly convert type 'IOut<U>' to 'IOut<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IOut<U>", "IOut<T>").WithLocation(19, 14),
                // (20,14): error CS0266: Cannot implicitly convert type 'IOut<T>' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IOut<T>", "IOut<U>").WithLocation(20, 14),
                // (29,14): error CS0266: Cannot implicitly convert type 'I<U>' to 'I<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("I<U>", "I<T>").WithLocation(29, 14),
                // (30,14): error CS0266: Cannot implicitly convert type 'I<T>' to 'I<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("I<T>", "I<U>").WithLocation(30, 14),
                // (34,14): error CS0266: Cannot implicitly convert type 'IIn<U>' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IIn<U>", "IIn<T>").WithLocation(34, 14),
                // (35,14): error CS0266: Cannot implicitly convert type 'IIn<T>' to 'IIn<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IIn<T>", "IIn<U>").WithLocation(35, 14),
                // (39,14): error CS0266: Cannot implicitly convert type 'IOut<U>' to 'IOut<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IOut<U>", "IOut<T>").WithLocation(39, 14),
                // (40,14): error CS0266: Cannot implicitly convert type 'IOut<T>' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IOut<T>", "IOut<U>").WithLocation(40, 14),
                // (49,14): error CS0266: Cannot implicitly convert type 'I<U>' to 'I<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("I<U>", "I<T>").WithLocation(49, 14),
                // (50,14): error CS0266: Cannot implicitly convert type 'I<T>' to 'I<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("I<T>", "I<U>").WithLocation(50, 14),
                // (54,14): error CS0266: Cannot implicitly convert type 'IIn<U>' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IIn<U>", "IIn<T>").WithLocation(54, 14),
                // (60,14): error CS0266: Cannot implicitly convert type 'IOut<T>' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IOut<T>", "IOut<U>").WithLocation(60, 14),
                // (68,14): error CS0266: Cannot implicitly convert type 'I<U>' to 'I<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("I<U>", "I<T>").WithLocation(68, 14),
                // (69,14): error CS0266: Cannot implicitly convert type 'I<T>' to 'I<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("I<T>", "I<U>").WithLocation(69, 14),
                // (73,14): error CS0266: Cannot implicitly convert type 'IIn<U>' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IIn<U>", "IIn<T>").WithLocation(73, 14),
                // (74,14): error CS0266: Cannot implicitly convert type 'IIn<T>' to 'IIn<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IIn<T>", "IIn<U>").WithLocation(74, 14),
                // (78,14): error CS0266: Cannot implicitly convert type 'IOut<U>' to 'IOut<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IOut<U>", "IOut<T>").WithLocation(78, 14),
                // (79,14): error CS0266: Cannot implicitly convert type 'IOut<T>' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IOut<T>", "IOut<U>").WithLocation(79, 14),
                // (88,14): error CS0266: Cannot implicitly convert type 'I<U>' to 'I<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("I<U>", "I<T>").WithLocation(88, 14),
                // (89,14): error CS0266: Cannot implicitly convert type 'I<T>' to 'I<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("I<T>", "I<U>").WithLocation(89, 14),
                // (93,14): error CS0266: Cannot implicitly convert type 'IIn<U>' to 'IIn<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IIn<U>", "IIn<T>").WithLocation(93, 14),
                // (94,14): error CS0266: Cannot implicitly convert type 'IIn<T>' to 'IIn<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IIn<T>", "IIn<U>").WithLocation(94, 14),
                // (98,14): error CS0266: Cannot implicitly convert type 'IOut<U>' to 'IOut<T>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "iu").WithArguments("IOut<U>", "IOut<T>").WithLocation(98, 14),
                // (99,14): error CS0266: Cannot implicitly convert type 'IOut<T>' to 'IOut<U>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "it").WithArguments("IOut<T>", "IOut<U>").WithLocation(99, 14));
        }

        [Fact]
        public void ImplicitReferenceConversions()
        {
            var source =
@"class C<T, U>
    where T : U
    where U : class
{
    static void M<X>(X x, U u) where X : class, T
    {
        u = x;
        x = u;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,13): error CS0266: Cannot implicitly convert type 'U' to 'X'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "u").WithArguments("U", "X").WithLocation(8, 13));
        }

        [Fact]
        public void ImplicitBoxingConversions()
        {
            var source =
@"class C<T, U>
    where T : class
    where U : T
{
    static void M<X, Y>(Y y, T t)
        where X : U
        where Y : struct, X
    {
        t = y;
        y = t;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,13): error CS0266: Cannot implicitly convert type 'T' to 'Y'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "Y").WithLocation(10, 13));
        }

        [Fact]
        public void ImplicitInterfaceConversionsCircularConstraint()
        {
            var source =
@"interface I { }
class C<T, U>
    where T : T
    where U : U, I
{
    static void M<V>(T t, U u, V v)
        where V : U
    {
        I i;
        i = t;
        i = u;
        i = v;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,9): error CS0454: Circular constraint dependency involving 'T' and 'T'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "T").WithLocation(2, 9),
                // (2,12): error CS0454: Circular constraint dependency involving 'U' and 'U'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "U").WithArguments("U", "U").WithLocation(2, 12),
                // (10,13): error CS0266: Cannot implicitly convert type 'T' to 'I'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "I").WithLocation(10, 13));
        }

        /// <summary>
        /// 6.2.7, bullet 1
        /// </summary>
        [Fact]
        public void ExplicitBaseClassConversions()
        {
            var source =
@"class A { }
class B1<T> : A { }
class C<T> : B1<T> { }
class B2 : A { }
class D<T>
    where T : C<object>
{
    static void M<U>(object o, A a, B1<T> b1t, B1<object> b1o, B2 b2)
        where U : C<T>
    {
        T t;
        t = (T)o;
        t = (T)a;
        t = (T)b1t;
        t = (T)b1o;
        t = (T)b2;
        U u;
        u = (U)o;
        u = (U)a;
        u = (U)b1t;
        u = (U)b1o;
        u = (U)b2;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,13): error CS0030: Cannot convert type 'B1<T>' to 'T'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T)b1t").WithArguments("B1<T>", "T").WithLocation(14, 13),
                // (16,13): error CS0030: Cannot convert type 'B2' to 'T'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T)b2").WithArguments("B2", "T").WithLocation(16, 13),
                // (21,13): error CS0030: Cannot convert type 'B1<object>' to 'U'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(U)b1o").WithArguments("B1<object>", "U").WithLocation(21, 13),
                // (22,13): error CS0030: Cannot convert type 'B2' to 'U'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(U)b2").WithArguments("B2", "U").WithLocation(22, 13));
        }

        /// <summary>
        /// 6.2.7, bullet 2
        /// </summary>
        [Fact]
        public void ExplicitConversionFromInterface()
        {
            var source =
@"interface IA { }
interface IB : IA { }
interface IC<T> { }
class A<T>
    where T : IA
{
    static void M<U>(IA a, IB b, IC<object> co, IC<T> ct)
        where U : IC<T>
    {
        T t;
        t = (T)a;
        t = (T)b;
        t = (T)co;
        t = (T)ct;
        U u;
        u = (U)a;
        u = (U)b;
        u = (U)co;
        u = (U)ct;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// 6.2.7, bullet 3
        /// </summary>
        [Fact]
        public void ExplicitConversionToInterface()
        {
            var source =
@"interface IA { }
interface IB : IA { }
interface IC<T> { }
class A<T>
    where T : IA
{
    static void M<U>(T t, U u)
        where U : IC<T>
    {
        IA a;
        IB b;
        IC<object> co;
        IC<T> ct;
        b = (IB)t;
        co = (IC<object>)t;
        ct = (IC<T>)t;
        a = (IA)u;
        b = (IB)u;
        co = (IC<object>)u;
        ct = (IC<T>)u;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// 6.2.7, bullet 4
        /// </summary>
        [Fact]
        public void ExplicitConversionToTypeParameter()
        {
            var source =
@"class C<T, U, V, W>
    where T : U, V, W
    where U : V, W
{
    static T t;
    static U u;
    static V v;
    static W w;
    static void M()
    {
        t = (T)u;
        t = (T)v;
        t = (T)w;
        u = (U)v;
        u = (U)w;
        v = (V)w;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,13): error CS0030: Cannot convert type 'W' to 'V'
                //         v = (V)w;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(V)w").WithArguments("W", "V"),
                // (8,14): warning CS0649: Field 'C<T, U, V, W>.w' is never assigned to, and will always have its default value 
                //     static W w;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "w").WithArguments("C<T, U, V, W>.w", "")
            );
        }

        [Fact]
        public void NoConversionsToValueType()
        {
            var source =
@"abstract class A<T>
{
    internal abstract void M<U>(T t, U u) where U : T;
}
class B : A<int>
{
    internal override void M<U>(int t, U u)
    {
        u = t;
        u = (U)t;
        t = u;
        t = (int)u;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,13): error CS0029: Cannot implicitly convert type 'int' to 'U'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("int", "U").WithLocation(9, 13),
                // (10,13): error CS0030: Cannot convert type 'int' to 'U'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(U)t").WithArguments("int", "U").WithLocation(10, 13),
                // (11,13): error CS0029: Cannot implicitly convert type 'U' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "int").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'U' to 'int'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)u").WithArguments("U", "int").WithLocation(12, 13));
        }

        /// <summary>
        /// 6.1.10
        /// </summary>
        [ClrOnlyFact]
        public void EmitImplicitConversions()
        {
            var source =
@"interface I { }
interface I<in T> { }
class A { }
class B : A { }
class C<T1, T2, T3, T4, T5, T6>
    where T2 : I
    where T3 : T4
    where T5 : B
    where T6 : I<A>
{
    static void F1(object o) { }
    static void F2(I i) { }
    static void F3(T4 t) { }
    static void F5(A a) { }
    static void F6(I<B> b) { }
    static void M(T1 a, T2 b, T3 c, T5 d, T6 e)
    {
        // 6.1.10 bullet 1: conversion to base type.
        F1(a);
        // 6.1.10 bullet 2: conversion to interface.
        F2(b);
        // 6.1.10 bullet 3: conversion to type parameter.
        F3(c);
        // 6.1.10 bullet 4: conversion from null to reference type.
        // ... no test
        // 6.1.10 bullet 5: conversion to reference type.
        F5(d);
        // 6.1.10 bullet 6: conversion to variance-convertible interface.
        F6(e);
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C<T1, T2, T3, T4, T5, T6>.M(T1, T2, T3, T5, T6)",
@"{
  // Code size       62 (0x3e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T1""
  IL_0006:  call       ""void C<T1, T2, T3, T4, T5, T6>.F1(object)""
  IL_000b:  ldarg.1
  IL_000c:  box        ""T2""
  IL_0011:  call       ""void C<T1, T2, T3, T4, T5, T6>.F2(I)""
  IL_0016:  ldarg.2
  IL_0017:  box        ""T3""
  IL_001c:  unbox.any  ""T4""
  IL_0021:  call       ""void C<T1, T2, T3, T4, T5, T6>.F3(T4)""
  IL_0026:  ldarg.3
  IL_0027:  box        ""T5""
  IL_002c:  call       ""void C<T1, T2, T3, T4, T5, T6>.F5(A)""
  IL_0031:  ldarg.s    V_4
  IL_0033:  box        ""T6""
  IL_0038:  call       ""void C<T1, T2, T3, T4, T5, T6>.F6(I<B>)""
  IL_003d:  ret
}");
        }

        /// <summary>
        /// 6.2.7
        /// </summary>
        [ClrOnlyFact]
        public void EmitExplicitConversions()
        {
            var source =
@"interface I { }
class C<T1, T2, T3, T4, T5>
    where T2 : I
    where T5 : T4
{
    static void F1(T1 t) { }
    static void F2(T2 t) { }
    static void F3(I i) { }
    static void F5(T5 t) { }
    static void M(object a, I b, T3 c, T4 d)
    {
        // 6.2.7 bullet 1: conversion from base class to type parameter.
        F1((T1)a);
        // 6.2.7 bullet 2: conversion from interface to type parameter.
        F2((T2)b);
        // 6.2.7 bullet 3: conversion from type parameter to interface
        // not in interface set.
        F3((I)c);
        // 6.2.7 bullet 4: conversion from type parameter to type parameter.
        F5((T5)d);
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C<T1, T2, T3, T4, T5>.M(object, I, T3, T4)",
@"{
  // Code size       55 (0x37)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""T1""
  IL_0006:  call       ""void C<T1, T2, T3, T4, T5>.F1(T1)""
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""T2""
  IL_0011:  call       ""void C<T1, T2, T3, T4, T5>.F2(T2)""
  IL_0016:  ldarg.2
  IL_0017:  box        ""T3""
  IL_001c:  castclass  ""I""
  IL_0021:  call       ""void C<T1, T2, T3, T4, T5>.F3(I)""
  IL_0026:  ldarg.3
  IL_0027:  box        ""T4""
  IL_002c:  unbox.any  ""T5""
  IL_0031:  call       ""void C<T1, T2, T3, T4, T5>.F5(T5)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void ImplicitUserDefinedConversion()
        {
            var source =
@"class C0 { }
class C1
{
    public static implicit operator C0(C1 o) { return null; }
}
class C2 { }
class C3<T>
{
    public static implicit operator T(C3<T> t) { return default(T); }
}
class C4<T> { }
class C
{
    // Implicit conversion from type parameter (success).
    static C0 F1<T>(T t) where T : C1 { return t; }
    // Implicit conversion from type parameter (error).
    static C0 F2<T>(T t) where T : C2 { return t; }
    // Implicit conversion to type parameter (success).
    static T F3<T>(C3<T> c) { return c; }
    // Implicit conversion to type parameter (error).
    static T F4<T>(C4<T> c) { return c; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,48): error CS0029: Cannot implicitly convert type 'T' to 'C0'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("T", "C0").WithLocation(17, 48),
                // (21,38): error CS0029: Cannot implicitly convert type 'C4<T>' to 'T'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c").WithArguments("C4<T>", "T").WithLocation(21, 38));
        }

        [Fact]
        public void ExplicitUserDefinedConversion()
        {
            var source =
@"class C0 { }
class C1
{
    public static explicit operator C0(C1 o) { return null; }
}
class C2 { }
class C3<T>
{
    public static explicit operator T(C3<T> t) { return default(T); }
}
class C4<T> { }
class C
{
    // Explicit conversion from type parameter (success).
    static C0 F1<T>(T t) where T : C1 { return (C0)t; }
    // Explicit conversion from type parameter (error).
    static C0 F2<T>(T t) where T : C2 { return (C0)t; }
    // Explicit conversion to type parameter (success).
    static T F3<T>(C3<T> c) { return (T)c; }
    // Explicit conversion to type parameter (error).
    static T F4<T>(C4<T> c) { return (T)c; }
}";
            // Note: Dev10 also reports "CS0030: Cannot convert type 'T' to 'C0'" in F1<T>(T),
            // although there is an explicit conversion from C1 to C0.
            CreateCompilation(source).VerifyDiagnostics(
                // (17,48): error CS0030: Cannot convert type 'T' to 'C0'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C0)t").WithArguments("T", "C0").WithLocation(17, 48),
                // (21,38): error CS0030: Cannot convert type 'C4<T>' to 'T'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T)c").WithArguments("C4<T>", "T").WithLocation(21, 38));
        }

        /// <summary>
        /// Dev10 does not report errors for implicit or explicit conversions between
        /// base and derived types if one of those types is a type parameter.
        /// </summary>
        [Fact]
        public void UserDefinedConversionsBaseToFromDerived()
        {
            var source =
@"class A { }
class B1 : A
{
    public static implicit operator A(B1 b) { return null; }
}
class B2 : A
{
    public static explicit operator A(B2 b) { return null; }
}
class B3 : A
{
    public static implicit operator B3(A a) { return null; }
}
class B4 : A
{
    public static explicit operator B4(A a) { return null; }
}
class C1<T> where T : C1<T>
{
    public static implicit operator C1<T>(T t) { return null; }
}
class C2<T> where T : C2<T>
{
    public static explicit operator C2<T>(T t) { return null; }
}
class C3<T> where T : C3<T>
{
    public static implicit operator T(C3<T> c) { return null; }
}
class C4<T> where T : C4<T>
{
    public static explicit operator T(C4<T> c) { return null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,37): error CS0553: 'B1.implicit operator A(B1)': user-defined conversions to or from a base type are not allowed
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "A").WithArguments("B1.implicit operator A(B1)").WithLocation(4, 37),
                // (8,37): error CS0553: 'B2.explicit operator A(B2)': user-defined conversions to or from a base type are not allowed
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "A").WithArguments("B2.explicit operator A(B2)").WithLocation(8, 37),
                // (12,37): error CS0553: 'B3.implicit operator B3(A)': user-defined conversions to or from a base type are not allowed
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "B3").WithArguments("B3.implicit operator B3(A)").WithLocation(12, 37),
                // (16,37): error CS0553: 'B4.explicit operator B4(A)': user-defined conversions to or from a base type are not allowed
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "B4").WithArguments("B4.explicit operator B4(A)").WithLocation(16, 37));
        }
    }
}
