// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class VarianceTests : CompilingTestBase
    {
        /// <summary>
        /// Test generic interface assignment with type parameter variance.
        /// </summary>
        [Fact]
        public void TestInterfaceAssignment()
        {
            var text = @"
interface I<in TIn, out TOut, T> {{ }}

class A {{ }}
class B : A {{ }}
class C : B {{ }}

class Test
{{
    static I<A, A, A> i01 = null;
    static I<A, A, B> i02 = null;
    static I<A, A, C> i03 = null;
    static I<A, B, A> i04 = null;
    static I<A, B, B> i05 = null;
    static I<A, B, C> i06 = null;
    static I<A, C, A> i07 = null;
    static I<A, C, B> i08 = null;
    static I<A, C, C> i09 = null;

    static I<B, A, A> i10 = null;
    static I<B, A, B> i11 = null;
    static I<B, A, C> i12 = null;
    static I<B, B, A> i13 = null;
    static I<B, B, B> i14 = null;
    static I<B, B, C> i15 = null;
    static I<B, C, A> i16 = null;
    static I<B, C, B> i17 = null;
    static I<B, C, C> i18 = null;

    static I<C, A, A> i19 = null;
    static I<C, A, B> i20 = null;
    static I<C, A, C> i21 = null;
    static I<C, B, A> i22 = null;
    static I<C, B, B> i23 = null;
    static I<C, B, C> i24 = null;
    static I<C, C, A> i25 = null;
    static I<C, C, B> i26 = null;
    static I<C, C, C> i27 = null;

    static void Main()
    {{
        i{0:d2} = i{1:d2};
    }}
}}";

            // Table comes from manual Dev10 testing
            int[][] validAssignments = new int[][]
            {
                /*filler for 1-indexing*/ new int[0],
                /*01*/
                       new [] { 1, 4, 7 },
                /*02*/ new [] { 2, 5, 8 },
                /*03*/ new [] { 3, 6, 9 },
                /*04*/ new [] { 4, 7  },
                /*05*/ new [] { 5, 8 },
                /*06*/ new [] { 6, 9 },
                /*07*/ new [] { 7 },
                /*08*/ new [] { 8 },
                /*09*/ new [] { 9 },
                /*10*/ new [] { 1, 4, 7, 10, 13, 16 },
                /*11*/ new [] { 2, 5, 8, 11, 14, 17 },
                /*12*/ new [] { 3, 6, 9, 12, 15, 18 },
                /*13*/ new [] { 4, 7, 13, 16 },
                /*14*/ new [] { 5, 8, 14, 17 },
                /*15*/ new [] { 6, 9, 15, 18 },
                /*16*/ new [] { 7, 16 },
                /*17*/ new [] { 8, 17 },
                /*18*/ new [] { 9, 18 },
                /*19*/ new [] { 1, 4, 7, 10, 13, 16, 19, 22, 25 },
                /*20*/ new [] { 2, 5, 8, 11, 14, 17, 20, 23, 26 },
                /*21*/ new [] { 3, 6, 9, 12, 15, 18, 21, 24, 27 },
                /*22*/ new [] { 4, 7, 13, 16, 22, 25 },
                /*23*/ new [] { 5, 8, 14, 17, 23, 26 },
                /*24*/ new [] { 6, 9, 15, 18, 24, 27 },
                /*25*/ new [] { 7, 16, 25 },
                /*26*/ new [] { 8, 17, 26 },
                /*27*/ new [] { 9, 18, 27 },
            };

            int numFields = validAssignments.Length - 1;

            for (int i = 1; i <= numFields; i++)
            {
                for (int j = 1; j <= numFields; j++)
                {
                    try
                    {
                        var comp = CreateCompilation(string.Format(text, i, j));
                        var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
                        if (!validAssignments[i].Contains(j))
                        {
                            Assert.Equal(ErrorCode.ERR_NoImplicitConvCast, (ErrorCode)errors.Single().Code);
                        }
                        else
                        {
                            Assert.Empty(errors);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed on assignment i{0:d2} = i{1:d2}", i, j);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Test generic interface assignment with type parameter variance.
        /// </summary>
        [Fact]
        public void TestDelegateAssignment()
        {
            var text = @"
delegate TOut D<in TIn, out TOut, T>(TIn tIn, T t);

class A {{ }}
class B : A {{ }}
class C : B {{ }}

class Test
{{
    static D<A, A, A> d01 = null;
    static D<A, A, B> d02 = null;
    static D<A, A, C> d03 = null;
    static D<A, B, A> d04 = null;
    static D<A, B, B> d05 = null;
    static D<A, B, C> d06 = null;
    static D<A, C, A> d07 = null;
    static D<A, C, B> d08 = null;
    static D<A, C, C> d09 = null;

    static D<B, A, A> d10 = null;
    static D<B, A, B> d11 = null;
    static D<B, A, C> d12 = null;
    static D<B, B, A> d13 = null;
    static D<B, B, B> d14 = null;
    static D<B, B, C> d15 = null;
    static D<B, C, A> d16 = null;
    static D<B, C, B> d17 = null;
    static D<B, C, C> d18 = null;

    static D<C, A, A> d19 = null;
    static D<C, A, B> d20 = null;
    static D<C, A, C> d21 = null;
    static D<C, B, A> d22 = null;
    static D<C, B, B> d23 = null;
    static D<C, B, C> d24 = null;
    static D<C, C, A> d25 = null;
    static D<C, C, B> d26 = null;
    static D<C, C, C> d27 = null;

    static void Main()
    {{
        d{0:d2} = d{1:d2};
    }}
}}";

            // Table comes from manual Dev10 testing
            int[][] validAssignments = new int[][]
            {
                /*filler for 1-indexing*/ new int[0],
                /*01*/
                       new [] { 1, 4, 7 },
                /*02*/ new [] { 2, 5, 8 },
                /*03*/ new [] { 3, 6, 9 },
                /*04*/ new [] { 4, 7  },
                /*05*/ new [] { 5, 8 },
                /*06*/ new [] { 6, 9 },
                /*07*/ new [] { 7 },
                /*08*/ new [] { 8 },
                /*09*/ new [] { 9 },
                /*10*/ new [] { 1, 4, 7, 10, 13, 16 },
                /*11*/ new [] { 2, 5, 8, 11, 14, 17 },
                /*12*/ new [] { 3, 6, 9, 12, 15, 18 },
                /*13*/ new [] { 4, 7, 13, 16 },
                /*14*/ new [] { 5, 8, 14, 17 },
                /*15*/ new [] { 6, 9, 15, 18 },
                /*16*/ new [] { 7, 16 },
                /*17*/ new [] { 8, 17 },
                /*18*/ new [] { 9, 18 },
                /*19*/ new [] { 1, 4, 7, 10, 13, 16, 19, 22, 25 },
                /*20*/ new [] { 2, 5, 8, 11, 14, 17, 20, 23, 26 },
                /*21*/ new [] { 3, 6, 9, 12, 15, 18, 21, 24, 27 },
                /*22*/ new [] { 4, 7, 13, 16, 22, 25 },
                /*23*/ new [] { 5, 8, 14, 17, 23, 26 },
                /*24*/ new [] { 6, 9, 15, 18, 24, 27 },
                /*25*/ new [] { 7, 16, 25 },
                /*26*/ new [] { 8, 17, 26 },
                /*27*/ new [] { 9, 18, 27 },
            };

            int numFields = validAssignments.Length - 1;

            for (int i = 1; i <= numFields; i++)
            {
                for (int j = 1; j <= numFields; j++)
                {
                    try
                    {
                        var comp = CreateCompilation(string.Format(text, i, j));
                        var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
                        if (!validAssignments[i].Contains(j))
                        {
                            var code = (ErrorCode)errors.Single().Code;
                            // UNDONE: which one will be used is predictable, but confirming that the behavior
                            // exactly matches dev10 is probably too tedious to be worthwhile
                            Assert.True(code == ErrorCode.ERR_NoImplicitConvCast || code == ErrorCode.ERR_NoImplicitConv, "Unexpected error code " + code);
                        }
                        else
                        {
                            Assert.Empty(errors);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed on assignment d{0:d2} = d{1:d2}", i, j);
                        throw;
                    }
                }
            }
        }

        /// <remarks>Based on LambdaTests.TestLambdaErrors03</remarks>
        [Fact]
        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        public void TestVarianceConversionCycle()
        {
            // To determine which overload is better, we have to try to convert D<IIn<I>> to D<I>
            // and vice versa.  The former is impossible and the latter is possible if and only
            // if it is possible (i.e. cycle).
            var text = @"
interface IIn<in TIn> { }
interface I : IIn<IIn<I>> { }

delegate T D<out T>();

class C
{
    static void Goo(D<IIn<I>> x) { }
    static void Goo(D<I> x) { }
    static void M()
    {
        Goo(null);
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Goo(D<IIn<I>>)' and 'C.Goo(D<I>)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("C.Goo(D<IIn<I>>)", "C.Goo(D<I>)"));
        }

        /// <remarks>http://blogs.msdn.com/b/ericlippert/archive/2008/05/07/covariance-and-contravariance-part-twelve-to-infinity-but-not-beyond.aspx</remarks>
        [Fact]
        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        public void TestVarianceConversionInfiniteExpansion01()
        {
            // IC<double> is convertible to IN<IC<string>> if and only
            // if IC<IC<double>> is convertible to IN<IC<IC<string>>>.
            var text = @"
public interface IN<in U> {}
public interface IC<X> : IN<IN<IC<IC<X>>>> {}

class C
{
    static void M()
    {
        IC<double> bar = null;
        IN<IC<string>> goo = bar;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,30): error CS0266: Cannot implicitly convert type 'IC<double>' to 'IN<IC<string>>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "bar").WithArguments("IC<double>", "IN<IC<string>>"));
        }

        /// <remarks>http://blogs.msdn.com/b/ericlippert/archive/2008/05/07/covariance-and-contravariance-part-twelve-to-infinity-but-not-beyond.aspx</remarks>
        [Fact]
        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        public void TestVarianceConversionInfiniteExpansion02()
        {
            var text = @"
interface A<in B> where B : class
{
}

interface B<in A> where A : class
{
}

class X : A<B<X>>
{
}

class Y : B<A<Y>>
{
}

class Test
{
     static void Main ()
     {
         A<Y> x = new X ();
     }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (22,19): error CS0266: Cannot implicitly convert type 'X' to 'A<Y>'. An explicit conversion exists (are you missing a cast?)
                //          A<Y> x = new X ();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new X ()").WithArguments("X", "A<Y>")
                );
        }

        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        [Fact]
        public void TestVarianceConversionLongFailure()
        {
            // IC<double> is convertible to IN<IC<string>> if and only
            // if IC<IC<double>> is convertible to IN<IC<IC<string>>>.
            var text = @"
interface A : B<B<A>> {}
interface B<T> : C<C<T>> {}
interface C<T> : D<D<T>> {}
interface D<T> : E<E<T>> {}
interface E<T> : F<F<T>> {}
interface F<T> : N<N<T>> {}

interface X : Y<Y<N<N<X>>>> {}
interface Y<T> : Z<Z<T>> {}
interface Z<T> : W<W<T>> {}
interface W<T> : U<U<T>> {}
interface U<T> : V<V<T>> {}
interface V<T> : N<N<T>> {}

interface N<in T> {}

class M {

  static void Main() {
    A a  = null;
    N<X> nx = a;
  }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (22,15): error CS0266: Cannot implicitly convert type 'A' to 'N<X>'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("A", "N<X>"));
        }

        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        [WorkItem(529488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529488")]
        [Fact]
        public void TestVarianceConversionLongSuccess_Breaking()
        {
            // IC<double> is convertible to IN<IC<string>> if and only
            // if IC<IC<double>> is convertible to IN<IC<IC<string>>>.
            var text = @"
interface A : B<B<A>> { }
interface B<T> : C<C<T>> { }
interface C<T> : D<D<T>> { }
interface D<T> : E<E<T>> { }
interface E<T> : F<F<T>> { }
interface F<T> : N<N<T>> { }

interface X : Y<Y<N<F<E<D<C<B<A>>>>>>>> { }
interface Y<T> : Z<Z<T>> { }
interface Z<T> : W<W<T>> { }
interface W<T> : U<U<T>> { }
interface U<T> : V<V<T>> { }
interface V<T> : N<N<T>> { }

interface N<in T> { }

class M
{

    static void Main()
    {
        A a = null;
        N<X> nx = a;
    }
}
";
            // There should not be any diagnostics, but we blow our stack and make a guess.
            // NB: this is a breaking change.
            CreateCompilation(text).VerifyDiagnostics(
                // (24,19): error CS0266: Cannot implicitly convert type 'A' to 'N<X>'. An explicit conversion exists (are you missing a cast?)
                //         N<X> nx = a;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("A", "N<X>"));
        }

        [WorkItem(542482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542482")]
        [Fact]
        public void CS1961ERR_UnexpectedVariance_ConstraintTypes()
        {
            var text =
@"interface IIn<in T> { }
interface IOut<out T> { }
class C<T> { }
interface I1<out T, in U, V>
{
    void M<X>() where X : T, U, V;
}
interface I2<out T, in U, V>
{
    void M<X>() where X : IOut<T>, IOut<U>, IOut<V>;
}
interface I3<out T, in U, V>
{
    void M<X>() where X : IIn<T>, IIn<U>, IIn<V>;
}
interface I4<out T, in U, V>
{
    void M1<X>() where X : C<T>;
    void M2<X>() where X : C<U>;
    void M3<X>() where X : C<V>;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,12): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'I1<T, U, V>.M<X>()'. 'T' is covariant.
                //     void M<X>() where X : T, U, V;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I1<T, U, V>.M<X>()", "T", "covariant", "contravariantly").WithLocation(6, 12),
                // (10,12): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'I2<T, U, V>.M<X>()'. 'T' is covariant.
                //     void M<X>() where X : IOut<T>, IOut<U>, IOut<V>;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I2<T, U, V>.M<X>()", "T", "covariant", "contravariantly").WithLocation(10, 12),
                // (14,12): error CS1961: Invalid variance: The type parameter 'U' must be covariantly valid on 'I3<T, U, V>.M<X>()'. 'U' is contravariant.
                //     void M<X>() where X : IIn<T>, IIn<U>, IIn<V>;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I3<T, U, V>.M<X>()", "U", "contravariant", "covariantly").WithLocation(14, 12),
                // (18,13): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I4<T, U, V>.M1<X>()'. 'T' is covariant.
                //     void M1<X>() where X : C<T>;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I4<T, U, V>.M1<X>()", "T", "covariant", "invariantly").WithLocation(18, 13),
                // (19,13): error CS1961: Invalid variance: The type parameter 'U' must be invariantly valid on 'I4<T, U, V>.M2<X>()'. 'U' is contravariant.
                //     void M2<X>() where X : C<U>;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I4<T, U, V>.M2<X>()", "U", "contravariant", "invariantly").WithLocation(19, 13));
        }

        [Fact]
        public void CS1961ERR_UnexpectedVariance_ClassesAndStructs()
        {
            var text =
@"class C<T> { }
struct S<T> { }
interface I1<out T, U>
{
    C<T> M(C<U> o);
}
interface I2<out T, U>
{
    C<U> M(C<T> o);
}
interface I3<out T, U>
{
    S<T> M(S<U> o);
}
interface I4<out T, U>
{
    S<U> M(S<T> o);
}
interface I5<in T, U>
{
    C<T> M(C<U> o);
}
interface I6<in T, U>
{
    C<U> M(C<T> o);
}
interface I7<in T, U>
{
    S<T> M(S<U> o);
}
interface I8<in T, U>
{
    S<U> M(S<T> o);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I1<T, U>.M(C<U>)'. 'T' is covariant.
                //     C<T> M(C<U> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>").WithArguments("I1<T, U>.M(C<U>)", "T", "covariant", "invariantly").WithLocation(5, 5),
                // (9,12): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I2<T, U>.M(C<T>)'. 'T' is covariant.
                //     C<U> M(C<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>").WithArguments("I2<T, U>.M(C<T>)", "T", "covariant", "invariantly").WithLocation(9, 12),
                // (13,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I3<T, U>.M(S<U>)'. 'T' is covariant.
                //     S<T> M(S<U> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "S<T>").WithArguments("I3<T, U>.M(S<U>)", "T", "covariant", "invariantly").WithLocation(13, 5),
                // (17,12): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I4<T, U>.M(S<T>)'. 'T' is covariant.
                //     S<U> M(S<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "S<T>").WithArguments("I4<T, U>.M(S<T>)", "T", "covariant", "invariantly").WithLocation(17, 12),
                // (21,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I5<T, U>.M(C<U>)'. 'T' is contravariant.
                //     C<T> M(C<U> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>").WithArguments("I5<T, U>.M(C<U>)", "T", "contravariant", "invariantly").WithLocation(21, 5),
                // (25,12): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I6<T, U>.M(C<T>)'. 'T' is contravariant.
                //     C<U> M(C<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>").WithArguments("I6<T, U>.M(C<T>)", "T", "contravariant", "invariantly").WithLocation(25, 12),
                // (29,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I7<T, U>.M(S<U>)'. 'T' is contravariant.
                //     S<T> M(S<U> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "S<T>").WithArguments("I7<T, U>.M(S<U>)", "T", "contravariant", "invariantly").WithLocation(29, 5),
                // (33,12): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I8<T, U>.M(S<T>)'. 'T' is contravariant.
                //     S<U> M(S<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "S<T>").WithArguments("I8<T, U>.M(S<T>)", "T", "contravariant", "invariantly").WithLocation(33, 12));
        }

        [WorkItem(602022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602022")]
        [Fact]
        public void CS1961ERR_UnexpectedVariance_Enums()
        {
            var text =
@"class C<T>
{
    public enum E { }
}
interface I1<out T, U>
{
    C<T>.E M(C<U>.E o);
}
interface I2<out T, U>
{
    C<U>.E M(C<T>.E o);
}
interface I3<in T, U>
{
    C<T>.E M(C<U>.E o);
}
interface I4<in T, U>
{
    C<U>.E M(C<T>.E o);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I1<T, U>.M(C<U>.E)'. 'T' is covariant.
                //     C<T>.E M(C<U>.E o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>.E").WithArguments("I1<T, U>.M(C<U>.E)", "T", "covariant", "invariantly").WithLocation(7, 5),
                // (11,14): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I2<T, U>.M(C<T>.E)'. 'T' is covariant.
                //     C<U>.E M(C<T>.E o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>.E").WithArguments("I2<T, U>.M(C<T>.E)", "T", "covariant", "invariantly").WithLocation(11, 14),
                // (15,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I3<T, U>.M(C<U>.E)'. 'T' is contravariant.
                //     C<T>.E M(C<U>.E o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>.E").WithArguments("I3<T, U>.M(C<U>.E)", "T", "contravariant", "invariantly").WithLocation(15, 5),
                // (19,14): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I4<T, U>.M(C<T>.E)'. 'T' is contravariant.
                //     C<U>.E M(C<T>.E o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>.E").WithArguments("I4<T, U>.M(C<T>.E)", "T", "contravariant", "invariantly").WithLocation(19, 14));
        }

        [WorkItem(542794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542794")]
        [Fact]
        public void ContravariantBaseInterface()
        {
            var text =
@"
interface IA<in T> { }
interface IB<in T> : IA<IB<T>> { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,17): error CS1961: Invalid variance: The type parameter 'T' must be covariantly valid on 'IA<IB<T>>'. 'T' is contravariant.
                // interface IB<in T> : IA<IB<T>> { }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T").WithArguments("IA<IB<T>>", "T", "contravariant", "covariantly").WithLocation(3, 17));
        }

        /// <summary>
        /// Report errors on type parameter use
        /// rather than declaration.
        /// </summary>
        [WorkItem(855750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/855750")]
        [Fact]
        public void ErrorLocations()
        {
            var text =
@"interface IIn<in T> { }
interface IOut<out T> { }
class C<T> { }
delegate void D<in T>();
interface I<out T, in U>
{
    C<U> M(C<T> o);
    IIn<T>[] P { get; }
    U this[object o] { get; }
    event D<U> E;
    void M<X>()
        where X : C<IIn<U>>, IOut<T>;
}
interface I<out T> :
    I<T, T>
{
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,5): error CS1961: Invalid variance: The type parameter 'U' must be invariantly valid on 'I<T, U>.M(C<T>)'. 'U' is contravariant.
                //     C<U> M(C<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<U>").WithArguments("I<T, U>.M(C<T>)", "U", "contravariant", "invariantly").WithLocation(7, 5),
                // (7,12): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'I<T, U>.M(C<T>)'. 'T' is covariant.
                //     C<U> M(C<T> o);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "C<T>").WithArguments("I<T, U>.M(C<T>)", "T", "covariant", "invariantly").WithLocation(7, 12),
                // (8,5): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'I<T, U>.P'. 'T' is covariant.
                //     IIn<T>[] P { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "IIn<T>[]").WithArguments("I<T, U>.P", "T", "covariant", "contravariantly").WithLocation(8, 5),
                // (9,5): error CS1961: Invalid variance: The type parameter 'U' must be covariantly valid on 'I<T, U>.this[object]'. 'U' is contravariant.
                //     U this[object o] { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "U").WithArguments("I<T, U>.this[object]", "U", "contravariant", "covariantly").WithLocation(9, 5),
                // (10,16): error CS1961: Invalid variance: The type parameter 'U' must be covariantly valid on 'I<T, U>.E'. 'U' is contravariant.
                //     event D<U> E;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "E").WithArguments("I<T, U>.E", "U", "contravariant", "covariantly").WithLocation(10, 16),
                // (11,12): error CS1961: Invalid variance: The type parameter 'U' must be invariantly valid on 'I<T, U>.M<X>()'. 'U' is contravariant.
                //     void M<X>()
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I<T, U>.M<X>()", "U", "contravariant", "invariantly").WithLocation(11, 12),
                // (11,12): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'I<T, U>.M<X>()'. 'T' is covariant.
                //     void M<X>()
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "X").WithArguments("I<T, U>.M<X>()", "T", "covariant", "contravariantly").WithLocation(11, 12),
                // (14,17): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'I<T, T>'. 'T' is covariant.
                // interface I<out T> :
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T").WithArguments("I<T, T>", "T", "covariant", "contravariantly").WithLocation(14, 17));
        }

        [Fact]
        public void CovarianceBoundariesForRefReadOnly_Parameters()
        {
            CreateCompilation(@"
interface ITest<in T>
{
    void M(in T p);
}").VerifyDiagnostics(
                // (4,15): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'ITest<T>.M(in T)'. 'T' is contravariant.
                //     void M(in T p);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T").WithArguments("ITest<T>.M(in T)", "T", "contravariant", "invariantly").WithLocation(4, 15));
        }

        [Fact]
        public void CovarianceBoundariesForRefReadOnly_ReturnType()
        {
            CreateCompilation(@"
interface ITest<in T>
{
    ref readonly T M();
}").VerifyDiagnostics(
                // (4,5): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'ITest<T>.M()'. 'T' is contravariant.
                //     ref readonly T M();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref readonly T").WithArguments("ITest<T>.M()", "T", "contravariant", "invariantly").WithLocation(4, 5));
        }
    }
}
