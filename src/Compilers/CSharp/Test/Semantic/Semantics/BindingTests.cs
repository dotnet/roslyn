// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class BindingTests : CompilingTestBase
    {
        [Fact, WorkItem(539872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539872")]
        public void NoWRN_UnreachableCode()
        {
            var text = @"
public class Cls
{
    public static int Main()
    {
        goto Label2;
    Label1:
        return (1);
    Label2:
        goto Label1;
    }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void GenericMethodName()
        {
            var source =
@"class A
{
    class B
    {
        static void M(System.Action a)
        {
            M(M1);
            M(M2<object>);
            M(M3<int>);
        }
        static void M1() { }
        static void M2<T>() { }
    }
    static void M3<T>() { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void GenericTypeName()
        {
            var source =
@"class A
{
    class B
    {
        static void M(System.Type t)
        {
            M(typeof(C<int>));
            M(typeof(S<string, string>));
            M(typeof(C<int, int>));
        }
        class C<T> { }
    }
    struct S<T, U> { }
}
class C<T, U> { }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void GenericTypeParameterName()
        {
            var source =
@"class A<T>
{
    class B<U>
    {
        static void M<V>()
        {
            N(typeof(V));
            N(typeof(U));
            N(typeof(T));
        }
        static void N(System.Type t) { }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void WrongMethodArity()
        {
            var source =
@"class C
{
    static void M1<T>() { }
    static void M2() { }
    void M3<T>() { }
    void M4() { }
    void M()
    {
        M1<object, object>();
        C.M1<object, object>();
        M2<int>();
        C.M2<int>();
        M3<object, object>();
        this.M3<object, object>();
        M4<int>();
        this.M4<int>();
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,9): error CS0305: Using the generic method 'C.M1<T>()' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M1<object, object>").WithArguments("C.M1<T>()", "method", "1").WithLocation(9, 9),
                // (10,11): error CS0305: Using the generic method 'C.M1<T>()' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M1<object, object>").WithArguments("C.M1<T>()", "method", "1").WithLocation(10, 11),
                // (11,9): error CS0308: The non-generic method 'C.M2()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M2<int>").WithArguments("C.M2()", "method").WithLocation(11, 9),
                // (12,11): error CS0308: The non-generic method 'C.M2()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M2<int>").WithArguments("C.M2()", "method").WithLocation(12, 11),
                // (13,9): error CS0305: Using the generic method 'C.M3<T>()' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M3<object, object>").WithArguments("C.M3<T>()", "method", "1").WithLocation(13, 9),
                // (14,14): error CS0305: Using the generic method 'C.M3<T>()' requires 1 type arguments
                Diagnostic(ErrorCode.ERR_BadArity, "M3<object, object>").WithArguments("C.M3<T>()", "method", "1").WithLocation(14, 14),
                // (15,9): error CS0308: The non-generic method 'C.M4()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M4<int>").WithArguments("C.M4()", "method").WithLocation(15, 9),
                // (16,14): error CS0308: The non-generic method 'C.M4()' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "M4<int>").WithArguments("C.M4()", "method").WithLocation(16, 14));
        }

        [Fact]
        public void AmbiguousInaccessibleMethod()
        {
            var source =
@"class A
{
    protected void M1() { }
    protected void M1(object o) { }
    protected void M2(string s) { }
    protected void M2(object o) { }
}
class B
{
    static void M(A a)
    {
        a.M1();
        a.M2();
        M(a.M1);
        M(a.M2);
    }
    static void M(System.Action<object> a) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,11): error CS0122: 'A.M1()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("A.M1()").WithLocation(12, 11),
                // (13,11): error CS0122: 'A.M2(string)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M2").WithArguments("A.M2(string)").WithLocation(13, 11),
                // (14,13): error CS0122: 'A.M1()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("A.M1()").WithLocation(14, 13),
                // (15,13): error CS0122: 'A.M2(string)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M2").WithArguments("A.M2(string)").WithLocation(15, 13));
        }

        /// <summary>
        /// Should report inaccessible method, even when using
        /// method as a delegate in an invalid context.
        /// </summary>
        [Fact]
        public void InaccessibleMethodInvalidDelegateUse()
        {
            var source =
@"class A
{
    protected object F() { return null; }
}
class B
{
    static void M(A a)
    {
        if (a.F != null)
        {
            M(a.F);
            a.F.ToString();
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(9, 15),
                // (11,17): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(11, 17),
                // (12,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(12, 15));
        }

        /// <summary>
        /// Methods should be resolved correctly even
        /// in cases where a method group is not allowed.
        /// </summary>
        [Fact]
        public void InvalidUseOfMethodGroup()
        {
            var source =
@"class A
{
    internal object E() { return null; }
    private object F() { return null; }
}
class B
{
    static void M(A a)
    {
        object o;
        a.E += a.E;
        if (a.E != null)
        {
            M(a.E);
            a.E.ToString();
            o = !a.E;
            o = a.E ?? a.F;
        }
        a.F += a.F;
        if (a.F != null)
        {
            M(a.F);
            a.F.ToString();
            o = !a.F;
            o = (o != null) ? a.E : a.F;
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,9): error CS1656: Cannot assign to 'E' because it is a 'method group'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "a.E").WithArguments("E", "method group").WithLocation(11, 9),
                // (12,13): error CS0019: Operator '!=' cannot be applied to operands of type 'method group' and '<null>'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a.E != null").WithArguments("!=", "method group", "<null>").WithLocation(12, 13),
                // (14,15): error CS1503: Argument 1: cannot convert from 'method group' to 'object'
                Diagnostic(ErrorCode.ERR_BadArgType, "a.E").WithArguments("1", "method group", "A").WithLocation(14, 15),
                // (15, 15): error CS0119: 'A.E()' is a 'method', which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("A.E()", "method").WithLocation(15, 15),
                // (16,17): error CS0023: Operator '!' cannot be applied to operand of type 'method group'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!a.E").WithArguments("!", "method group").WithLocation(16, 17),
                // (17,26): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(17, 26),
                // (19,11): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(19, 11),
                // (19,18): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(19, 18),
                // (20,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(20, 15),
                // (22,17): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(22, 17),
                // (23,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(23, 15),
                // (24,20): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(24, 20),
                // (25,39): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(25, 39));
        }

        [WorkItem(528425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528425")]
        [Fact(Skip = "528425")]
        public void InaccessibleAndAccessible()
        {
            var source =
@"using System;
class A
{
    void F() { }
    internal void F(object o) { }
    static void G() { }
    internal static void G(object o) { }
    void H(object o) { }
}
class B : A
{
    static void M(A a)
    {
        a.F(null);
        a.F();
        A.G();
        M1(a.F);
        M2(a.F);
        Action<object> a1 = a.F;
        Action a2 = a.F;
    }
    void M()
    {
        G();
    }
    static void M1(Action<object> a) { }
    static void M2(Action a) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,11): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(15, 11),
                // (16,11): error CS0122: 'A.G()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G()").WithLocation(16, 11),
                // (18,12): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "a.F").WithArguments("1", "method group", "System.Action").WithLocation(18, 12),
                // (20,23): error CS0122: 'A.F()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(20, 23),
                // (24,9): error CS0122: 'A.G()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G()").WithLocation(24, 9));
        }

        [Fact]
        public void InaccessibleAndAccessibleAndAmbiguous()
        {
            var source =
@"class A
{
    void F(string x) { }
    void F(string x, string y) { }
    internal void F(object x, string y) { }
    internal void F(string x, object y) { }
    void G(object x, string y) { }
    internal void G(string x, object y) { }
    static void M(A a, string s)
    {
        a.F(s, s); // no error
    }
}
class B
{
    static void M(A a, string s, object o)
    {
        a.F(s, s); // accessible ambiguous
        a.G(s, s); // accessible and inaccessible ambiguous, no error
        a.G(o, o); // accessible and inaccessible invalid 
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (18,9): error CS0121: The call is ambiguous between the following methods or properties: 'A.F(object, string)' and 'A.F(string, object)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("A.F(object, string)", "A.F(string, object)").WithLocation(18, 11),
                // (20,13): error CS1503: Argument 1: cannot convert from 'object' to 'string'
                Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("1", "object", "string").WithLocation(20, 13));
        }

        [Fact]
        public void InaccessibleAndAccessibleValid()
        {
            var source =
@"class A
{
    void F(int i) { }
    internal void F(long l) { }
    static void M(A a)
    {
        a.F(1); // no error
    }
}
class B
{
    static void M(A a)
    {
        a.F(1); // no error
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void ParenthesizedDelegate()
        {
            var source =
@"class C
{
    System.Action<object> F = null;
    void M()
    {
        ((this.F))(null);
        (new C().F)(null, null);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "(new C().F)").WithArguments("System.Action<object>", "2").WithLocation(7, 9));
        }

        /// <summary>
        /// Report errors for invocation expressions for non-invocable expressions,
        /// and bind arguments even though invocation expression was invalid.
        /// </summary>
        [Fact]
        public void NonMethodsWithArgs()
        {
            var source =
@"namespace N
{
    class C<T>
    {
        object F;
        object P { get; set; }
        void M()
        {
            N(a);
            C<string>(b);
            N.C<int>(c);
            N.D(d);
            T(e);
            (typeof(C<int>))(f);
            P(g) = F(h);
            this.F(i) = (this).P(j);
            null.M(k);
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,15): error CS0103: The name 'a' does not exist in the current context
                //             N(a);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a"),
                // (9,13): error CS0118: 'N' is a namespace but is used like a variable
                //             N(a);
                Diagnostic(ErrorCode.ERR_BadSKknown, "N").WithArguments("N", "namespace", "variable"),
                // (10,23): error CS0103: The name 'b' does not exist in the current context
                //             C<string>(b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b"),
                // (10,13): error CS1955: Non-invocable member 'N.C<T>' cannot be used like a method.
                //             C<string>(b);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "C<string>").WithArguments("N.C<T>"),
                // (11,22): error CS0103: The name 'c' does not exist in the current context
                //             N.C<int>(c);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c"),
                // (11,15): error CS1955: Non-invocable member 'N.C<T>' cannot be used like a method.
                //             N.C<int>(c);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "C<int>").WithArguments("N.C<T>"),
                // (12,17): error CS0103: The name 'd' does not exist in the current context
                //             N.D(d);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d"),
                // (12,13): error CS0234: The type or namespace name 'D' does not exist in the namespace 'N' (are you missing an assembly reference?)
                //             N.D(d);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "N.D").WithArguments("D", "N"),
                // (13,15): error CS0103: The name 'e' does not exist in the current context
                //             T(e);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e"),
                // (13,13): error CS0103: The name 'T' does not exist in the current context
                //             T(e);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "T").WithArguments("T"),
                // (14,30): error CS0103: The name 'f' does not exist in the current context
                //             (typeof(C<int>))(f);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f"),
                // (14,13): error CS0149: Method name expected
                //             (typeof(C<int>))(f);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "(typeof(C<int>))"),
                // (15,15): error CS0103: The name 'g' does not exist in the current context
                //             P(g) = F(h);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g"),
                // (15,13): error CS1955: Non-invocable member 'N.C<T>.P' cannot be used like a method.
                //             P(g) = F(h);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P").WithArguments("N.C<T>.P"),
                // (15,22): error CS0103: The name 'h' does not exist in the current context
                //             P(g) = F(h);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "h").WithArguments("h"),
                // (15,20): error CS1955: Non-invocable member 'N.C<T>.F' cannot be used like a method.
                //             P(g) = F(h);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "F").WithArguments("N.C<T>.F"),
                // (16,20): error CS0103: The name 'i' does not exist in the current context
                //             this.F(i) = (this).P(j);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i"),
                // (16,18): error CS1955: Non-invocable member 'N.C<T>.F' cannot be used like a method.
                //             this.F(i) = (this).P(j);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "F").WithArguments("N.C<T>.F"),
                // (16,34): error CS0103: The name 'j' does not exist in the current context
                //             this.F(i) = (this).P(j);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
                // (16,32): error CS1955: Non-invocable member 'N.C<T>.P' cannot be used like a method.
                //             this.F(i) = (this).P(j);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P").WithArguments("N.C<T>.P"),
                // (17,20): error CS0103: The name 'k' does not exist in the current context
                //             null.M(k);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
                // (17,13): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
                //             null.M(k);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.M").WithArguments(".", "<null>"),
                // (5,16): warning CS0649: Field 'N.C<T>.F' is never assigned to, and will always have its default value null
                //         object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("N.C<T>.F", "null")
            );
        }

        [Fact]
        public void SimpleDelegates()
        {
            var source =
@"static class S
{
    public static void F(System.Action a) { }
}
class C
{
    void M()
    {
        S.F(this.M);
        System.Action a = this.M;
        S.F(a);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void DelegatesFromOverloads()
        {
            var source =
@"using System;
class C
{
    static void A(Action<object> a) { }
    static void M(C c)
    {
        A(C.F);
        A(c.G);
        Action<object> a;
        a = C.F;
        a = c.G;
    }
    static void F() { }
    static void F(object o) { }
    void G(object o) { }
    void G(object x, object y) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void NonViableDelegates()
        {
            var source =
@"using System;
class A
{
    static Action F = null;
    Action G = null;
}
class B
{
    static void M(A a)
    {
        A.F(x);
        a.G(y);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(    // (11,13): error CS0103: The name 'x' does not exist in the current context
                                                                        //         A.F(x);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"),
                // (11,11): error CS0122: 'A.F' is inaccessible due to its protection level
                //         A.F(x);
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F"),
                // (12,13): error CS0103: The name 'y' does not exist in the current context
                //         a.G(y);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y"),
                // (12,11): error CS0122: 'A.G' is inaccessible due to its protection level
                //         a.G(y);
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G"),
                // (4,19): warning CS0414: The field 'A.F' is assigned but its value is never used
                //     static Action F = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F").WithArguments("A.F"),
                // (5,12): warning CS0414: The field 'A.G' is assigned but its value is never used
                //     Action G = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "G").WithArguments("A.G")
            );
        }

        /// <summary>
        /// Choose one method if overloaded methods are
        /// equally invalid.
        /// </summary>
        [Fact]
        public void ChooseOneMethodIfEquallyInvalid()
        {
            var source =
@"internal static class S
{
    public static void M(double x, A y) { }
    public static void M(double x, B y) { }
}
class A { }
class B { }
class C
{
    static void M()
    {
        S.M(1.0, null); // ambiguous
        S.M(1.0, 2.0); // equally invalid
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'S.M(double, A)' and 'S.M(double, B)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("S.M(double, A)", "S.M(double, B)").WithLocation(12, 11),
                // (13,18): error CS1503: Argument 2: cannot convert from 'double' to 'A'
                Diagnostic(ErrorCode.ERR_BadArgType, "2.0").WithArguments("2", "double", "A").WithLocation(13, 18));
        }

        [Fact]
        public void ChooseExpandedFormIfBadArgCountAndBadArgument()
        {
            var source =
@"class C
{
    static void M(object o)
    {
        F();
        F(o);
        F(1, o);
        F(1, 2, o);
    }
    static void F(int i, params int[] args) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'i' of 'C.F(int, params int[])'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F").WithArguments("i", "C.F(int, params int[])").WithLocation(5, 9),
                // (6,11): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("1", "object", "int").WithLocation(6, 11),
                // (7,14): error CS1503: Argument 2: cannot convert from 'object' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("2", "object", "int").WithLocation(7, 14),
                // (8,17): error CS1503: Argument 3: cannot convert from 'object' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("3", "object", "int").WithLocation(8, 17));
        }

        [Fact]
        public void AmbiguousAndBadArgument()
        {
            var source =
@"class C
{
    static void F(int x, double y) { }
    static void F(double x, int y) { }
    static void M()
    {
        F(1, 2);
        F(1.0, 2.0);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.F(int, double)' and 'C.F(double, int)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("C.F(int, double)", "C.F(double, int)").WithLocation(7, 9),
                // (8,11): error CS1503: Argument 1: cannot convert from 'double' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "1.0").WithArguments("1", "double", "int").WithLocation(8, 11));
        }

        [WorkItem(541050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541050")]
        [Fact]
        public void IncompleteDelegateDecl()
        {
            var source =
@"namespace nms {

delegate";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,9): error CS1031: Type expected
                // delegate
                Diagnostic(ErrorCode.ERR_TypeExpected, ""),
                // (3,9): error CS1001: Identifier expected
                // delegate
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
                // (3,9): error CS1003: Syntax error, '(' expected
                // delegate
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(", ""),
                // (3,9): error CS1026: ) expected
                // delegate
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ""),
                // (3,9): error CS1002: ; expected
                // delegate
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ""),
                // (3,9): error CS1513: } expected
                // delegate
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(541213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541213")]
        [Fact]
        public void IncompleteElsePartInIfStmt()
        {
            var source =
@"public class Test
{
    public static int Main(string [] args)
    {
        if (true)
        {
        }
        else
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,13): error CS1733: Expected expression
                //         else
                Diagnostic(ErrorCode.ERR_ExpressionExpected, ""),
                // (9,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ""),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (9,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (3,23): error CS0161: 'Test.Main(string[])': not all code paths return a value
                //     public static int Main(string [] args)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("Test.Main(string[])")
                );
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest01()
        {
            var baseAssembly = CreateCompilationWithMscorlib(
@"
namespace BaseAssembly {
    public class BaseClass {
    }
}
", assemblyName: "BaseAssembly1").VerifyDiagnostics();

            var derivedAssembly = CreateCompilationWithMscorlib(
@"
namespace DerivedAssembly {
    public class DerivedClass: BaseAssembly.BaseClass {
        public static int IntField = 123;
    }
}
", assemblyName: "DerivedAssembly1", references: new List<MetadataReference>() { baseAssembly.EmitToImageReference() }).VerifyDiagnostics();

            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = DerivedAssembly.DerivedClass; 
public class Test
{
    static void Main()
    {
        int a = ClassAlias.IntField;
        int b = ClassAlias.IntField;
    }
}
", references: new List<MetadataReference>() { derivedAssembly.EmitToImageReference() })
            .VerifyDiagnostics();

            // NOTE: Dev10 errors:
            // <fine-name>(7,9): error CS0012: The type 'BaseAssembly.BaseClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'BaseAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest02()
        {
            var baseAssembly = CreateCompilationWithMscorlib(
@"
namespace BaseAssembly {
    public class BaseClass {
    }
}
", assemblyName: "BaseAssembly2").VerifyDiagnostics();

            var derivedAssembly = CreateCompilationWithMscorlib(
@"
namespace DerivedAssembly {
    public class DerivedClass: BaseAssembly.BaseClass {
        public static int IntField = 123;
    }
}
", assemblyName: "DerivedAssembly2", references: new List<MetadataReference>() { baseAssembly.EmitToImageReference() }).VerifyDiagnostics();

            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = DerivedAssembly.DerivedClass; 
public class Test
{
    static void Main()
    {
        ClassAlias a = new ClassAlias();
        ClassAlias b = new ClassAlias();
    }
}
", references: new List<MetadataReference>() { derivedAssembly.EmitToImageReference() })
            .VerifyDiagnostics();

            // NOTE: Dev10 errors:
            // <fine-name>(6,9): error CS0012: The type 'BaseAssembly.BaseClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'BaseAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest03()
        {
            var baseAssembly = CreateCompilationWithMscorlib(
@"
namespace BaseAssembly {
    public class BaseClass {
    }
}
", assemblyName: "BaseAssembly3").VerifyDiagnostics();

            var derivedAssembly = CreateCompilationWithMscorlib(
@"
namespace DerivedAssembly {
    public class DerivedClass: BaseAssembly.BaseClass {
        public static int IntField = 123;
    }
}
", assemblyName: "DerivedAssembly3", references: new List<MetadataReference>() { baseAssembly.EmitToImageReference() }).VerifyDiagnostics();

            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = DerivedAssembly.DerivedClass; 
public class Test
{
    ClassAlias a = null;
    ClassAlias b = null;
    ClassAlias m() { return null; }
    void m2(ClassAlias p) { }
}", references: new List<MetadataReference>() { derivedAssembly.EmitToImageReference() })
            .VerifyDiagnostics(
                // (5,16): warning CS0414: The field 'Test.a' is assigned but its value is never used
                //     ClassAlias a = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("Test.a"),
                // (6,16): warning CS0414: The field 'Test.b' is assigned but its value is never used
                //     ClassAlias b = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "b").WithArguments("Test.b")
            );

            // NOTE: Dev10 errors:
            // <fine-name>(4,16): error CS0012: The type 'BaseAssembly.BaseClass' is defined in an assembly that is not referenced. You must add a reference to assembly 'BaseAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_Typical()
        {
            string scenarioCode = @"
public class ITT
    : IInterfaceBase
{ }
public interface IInterfaceBase
{
    void bar();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (3,7): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.bar()'
                //     : IInterfaceBase
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase").WithArguments("ITT", "IInterfaceBase.bar()").WithLocation(3, 7));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_FullyQualified()
        {
            // Using fully Qualified names
            string scenarioCode = @"
public class ITT
    : test.IInterfaceBase
{ }

namespace test
{
    public interface IInterfaceBase
    {
        void bar();
    }
}";

            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (3,7): error CS0535: 'ITT' does not implement interface member 'test.IInterfaceBase.bar()'
                //     : test.IInterfaceBase
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "test.IInterfaceBase").WithArguments("ITT", "test.IInterfaceBase.bar()").WithLocation(3, 7));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_WithAlias()
        {
            // Using Alias            
            string scenarioCode = @"
using a1 = test;

public class ITT
    : a1.IInterfaceBase
{ }

namespace test 
{
    public interface IInterfaceBase
    {
        void bar();
    }
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (5,7): error CS0535: 'ITT' does not implement interface member 'test.IInterfaceBase.bar()'
                //     : a1.IInterfaceBase
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "a1.IInterfaceBase").WithArguments("ITT", "test.IInterfaceBase.bar()").WithLocation(5, 7));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario01()
        {
            // Two interfaces, neither implemented with alias - should have 2 errors each squiggling a different interface type.            
            string scenarioCode = @"
using a1 = test;

public class ITT
    : a1.IInterfaceBase, a1.IInterfaceBase2 
{ }

namespace test 
{
    public interface IInterfaceBase
    {
        void xyz();
    }

    public interface IInterfaceBase2
    {
        void xyz();
    }
}";

            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (5,7): error CS0535: 'ITT' does not implement interface member 'test.IInterfaceBase.xyz()'
                //     : a1.IInterfaceBase, a1.IInterfaceBase2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "a1.IInterfaceBase").WithArguments("ITT", "test.IInterfaceBase.xyz()").WithLocation(5, 7),
                // (5,26): error CS0535: 'ITT' does not implement interface member 'test.IInterfaceBase2.xyz()'
                //     : a1.IInterfaceBase, a1.IInterfaceBase2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "a1.IInterfaceBase2").WithArguments("ITT", "test.IInterfaceBase2.xyz()").WithLocation(5, 26));
        }
        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario02()
        {
            // Two interfaces, only the  second is implemented 
            string scenarioCode = @"
public class ITT
    : IInterfaceBase, IInterfaceBase2 
{
    void IInterfaceBase2.abc()
    { }
}

public interface IInterfaceBase
{
        void xyz();
}

public interface IInterfaceBase2
{
        void abc();
}";

            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (3,7): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.xyz()'
                //     : IInterfaceBase, IInterfaceBase2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase").WithArguments("ITT", "IInterfaceBase.xyz()").WithLocation(3, 7));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario03()
        {
            // Two interfaces, only the first is implemented
            string scenarioCode = @"
public class ITT
    : IInterfaceBase, IInterfaceBase2 
{
    void IInterfaceBase.xyz()
    { }
}

public interface IInterfaceBase
{
    void xyz();
}

public interface IInterfaceBase2
{
    void abc();
}
";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (3,23): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase2.abc()'
                //     : IInterfaceBase, IInterfaceBase2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase2").WithArguments("ITT", "IInterfaceBase2.abc()").WithLocation(3, 23));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario04()
        {
            // Two interfaces, neither implemented but formatting of interfaces are on different lines
            string scenarioCode = @"
public class ITT
    : IInterfaceBase, 
     IInterfaceBase2 
{ }

public interface IInterfaceBase
{
    void xyz();
}

public interface IInterfaceBase2
{
    void xyz();
}
";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (3,7): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.xyz()'
                //     : IInterfaceBase, 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase").WithArguments("ITT", "IInterfaceBase.xyz()").WithLocation(3, 7),
                // (4,6): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase2.xyz()'
                //      IInterfaceBase2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase2").WithArguments("ITT", "IInterfaceBase2.xyz()").WithLocation(4, 6));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario05()
        {
            // Inherited Interface scenario 
            // With methods not implemented in both base and derived.
            // Should reflect 2 diagnostics but both with be squiggling the derived as we are not 
            // explicitly implementing base.
            string scenarioCode = @"
public class ITT: IDerived
{ }

interface IInterfaceBase
{
    void xyzb();
}

interface IDerived : IInterfaceBase
{
    void xyzd();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IDerived.xyzd()'
                // public class ITT: IDerived
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IDerived.xyzd()").WithLocation(2, 19),
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.xyzb()'
                // public class ITT: IDerived
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IInterfaceBase.xyzb()").WithLocation(2, 19));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario06()
        {
            // Inherited Interface scenario 
            string scenarioCode = @"
public class ITT: IDerived, IInterfaceBase
{ }

interface IInterfaceBase
{
    void xyz();
}

interface IDerived : IInterfaceBase
{
    void xyzd();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IDerived.xyzd()'
                // public class ITT: IDerived, IInterfaceBase
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IDerived.xyzd()").WithLocation(2, 19),
                // (2,29): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.xyz()'
                // public class ITT: IDerived, IInterfaceBase
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase").WithArguments("ITT", "IInterfaceBase.xyz()").WithLocation(2, 29));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario07()
        {
            // Inherited Interface scenario - different order. 
            string scenarioCode = @"
public class ITT: IInterfaceBase, IDerived 
{ }

interface IDerived : IInterfaceBase
{
    void xyzd();
}
interface IInterfaceBase
{
    void xyz();
}
";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,35): error CS0535: 'ITT' does not implement interface member 'IDerived.xyzd()'
                // public class ITT: IInterfaceBase, IDerived 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IDerived.xyzd()").WithLocation(2, 35),
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IInterfaceBase.xyz()'
                // public class ITT: IInterfaceBase, IDerived 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IInterfaceBase").WithArguments("ITT", "IInterfaceBase.xyz()").WithLocation(2, 19));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario08()
        {
            // Inherited Interface scenario
            string scenarioCode = @"
public class ITT: IDerived2 
{}

interface IBase
{
    void method1();
}
interface IBase2
{
    void Method2();
}
interface IDerived2: IBase, IBase2
{}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IBase.method1()'
                // public class ITT: IDerived2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived2").WithArguments("ITT", "IBase.method1()").WithLocation(2, 19),
                // (2,19): error CS0535: 'ITT' does not implement interface member 'IBase2.Method2()'
                // public class ITT: IDerived2 
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived2").WithArguments("ITT", "IBase2.Method2()").WithLocation(2, 19));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation13UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario09()
        {
            // Inherited Interface scenario.           
            string scenarioCode = @"
public class ITT : IDerived
{
    void IBase2.method2()
    { }

    void IDerived.method3()
    { }
}

public interface IBase
{
    void method1();
}

public interface IBase2
{
    void method2();
}
public interface IDerived : IBase, IBase2
{
    void method3();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,20): error CS0535: 'ITT' does not implement interface member 'IBase.method1()'
                // public class ITT : IDerived
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IBase.method1()").WithLocation(2, 20));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario10()
        {
            // Inherited Interface scenario.
            string scenarioCode = @"
public class ITT : IDerived
{
    void IBase2.method2()
    { }    
    void IBase3.method3()
    { }
    void IDerived.method4()
    { }
}

public interface IBase
{
    void method1();
}

public interface IBase2 : IBase
{    
    void method2();
}

public interface IBase3 : IBase
{
    void method3();
}
public interface IDerived : IBase2, IBase3
{
    void method4();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,20): error CS0535: 'ITT' does not implement interface member 'IBase.method1()'
                // public class ITT : IDerived
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDerived").WithArguments("ITT", "IBase.method1()").WithLocation(2, 20));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_InterfaceInheritenceScenario11()
        {
            // Inherited Interface scenario 
            string scenarioCode = @"
static class Module1
{
    public static void Main()
    {
    }
}

interface Ibase
{
    void method1();
}

interface Ibase2
{
    void method2();
}

interface Iderived : Ibase
{
    void method3();
}

interface Iderived2 : Iderived
{
    void method4();
}

class foo : Iderived2, Iderived, Ibase, Ibase2
{
    void Ibase.method1()
    { }
    void Ibase2.method2()
    { }
    void Iderived2.method4()
    { }
}
 ";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (29,24): error CS0535: 'foo' does not implement interface member 'Iderived.method3()'
                // class foo : Iderived2, Iderived, Ibase, Ibase2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Iderived").WithArguments("foo", "Iderived.method3()").WithLocation(29, 24));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_WithPartialClass01()
        {
            // partial class - missing method. 
            // each partial implements interface but one is missing method.
            string scenarioCode = @"
 public partial class Foo : IBase
{
    void IBase.method1()
    { }

    void IBase2.method2()
    { }
}

public partial class Foo : IBase2
{
}

public partial class Foo : IBase3
{
}

public interface IBase
{
    void method1();
}

public interface IBase2
{    
    void method2();
}

public interface IBase3
{
    void method3();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (15,28): error CS0535: 'Foo' does not implement interface member 'IBase3.method3()'
                // public partial class Foo : IBase3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IBase3").WithArguments("Foo", "IBase3.method3()").WithLocation(15, 28));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_WithPartialClass02()
        {
            // partial class - missing method. diagnostic is reported in correct partial class
            // one partial class specifically does include any inherited interface 
            string scenarioCode = @"
public partial class Foo : IBase, IBase2
{
    void IBase.method1()
    { }

}

public partial class Foo
{
}

public partial class Foo : IBase3
{
}

public interface IBase
{
    void method1();
}

public interface IBase2
{
    void method2();
}

public interface IBase3
{
    void method3();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,35): error CS0535: 'Foo' does not implement interface member 'IBase2.method2()'
                // public partial class Foo : IBase, IBase2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IBase2").WithArguments("Foo", "IBase2.method2()").WithLocation(2, 35),
                // (13,28): error CS0535: 'Foo' does not implement interface member 'IBase3.method3()'
                // public partial class Foo : IBase3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IBase3").WithArguments("Foo", "IBase3.method3()").WithLocation(13, 28));
        }

        [WorkItem(911913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/911913")]
        [Fact]
        public void UnimplementedInterfaceSquiggleLocation_WithPartialClass03()
        {
            // Partial class scenario
            // One class implements multiple interfaces and is missing method.             
            string scenarioCode = @" 
public partial class Foo : IBase, IBase2
{
    void IBase.method1()
    { }

}

public partial class Foo : IBase3
{
}

public interface IBase
{
    void method1();
}

public interface IBase2
{    
    void method2();
}

public interface IBase3
{
    void method3();
}";
            var testAssembly = CreateCompilationWithMscorlib(scenarioCode);
            testAssembly.VerifyDiagnostics(
                // (2,35): error CS0535: 'Foo' does not implement interface member 'IBase2.method2()'
                // public partial class Foo : IBase, IBase2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IBase2").WithArguments("Foo", "IBase2.method2()").WithLocation(2, 35),
                // (9,28): error CS0535: 'Foo' does not implement interface member 'IBase3.method3()'
                // public partial class Foo : IBase3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IBase3").WithArguments("Foo", "IBase3.method3()").WithLocation(9, 28)
 );
        }
        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest04()
        {
            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = Class1;
public class Test
{
    void m()
    {
        int a = ClassAlias.Class1Foo();
        int b = ClassAlias.Class1Foo();
    }
}", references: new List<MetadataReference>() { TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1 })
.VerifyDiagnostics(
    // (2,20): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // using ClassAlias = Class1;
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
    // (7,28): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    //         int a = ClassAlias.Class1Foo();
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1Foo").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
    // (8,28): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    //         int b = ClassAlias.Class1Foo();
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1Foo").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            );

            // NOTE: Dev10 errors:
            // <fine-name>(8,28): error CS0117: 'Class1' does not contain a definition for 'Class1Foo'
            // <fine-name>(9,28): error CS0117: 'Class1' does not contain a definition for 'Class1Foo'
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest05()
        {
            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = Class1;
public class Test
{
    void m()
    {
        var a = new ClassAlias();
        var b = new ClassAlias();
    }
}", references: new List<MetadataReference>() { TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1 })
.VerifyDiagnostics(
    // (2,20): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // using ClassAlias = Class1;
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            );

            // NOTE: Dev10 errors:
            // <fine-name>(8,17): error CS0143: The type 'Class1' has no constructors defined
            // <fine-name>(9,17): error CS0143: The type 'Class1' has no constructors defined
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest06()
        {
            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = Class1;
public class Test
{
    ClassAlias a = null;
    ClassAlias b = null;
    ClassAlias m() { return null; }
    void m2(ClassAlias p) { }
}", references: new List<MetadataReference>() { TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1 })
.VerifyDiagnostics(
    // (2,20): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // using ClassAlias = Class1;
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
    // (6,16): warning CS0414: The field 'Test.b' is assigned but its value is never used
    //     ClassAlias b = null;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "b").WithArguments("Test.b"),
    // (5,16): warning CS0414: The field 'Test.a' is assigned but its value is never used
    //     ClassAlias a = null;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("Test.a")
            );

            // NOTE: Dev10 errors:
            // <fine-name>(4,16): error CS1772: Type 'Class1' from assembly '...\NoPIAGenerics1-Asm1.dll' cannot be used across assembly boundaries because a type in its inheritance hierarchy has a generic type parameter that is an embedded interop type.
            // <fine-name>(5,16): error CS1772: Type 'Class1' from assembly '...\NoPIAGenerics1-Asm1.dll' cannot be used across assembly boundaries because a type in its inheritance hierarchy has a generic type parameter that is an embedded interop type.
            // <fine-name>(6,16): error CS1772: Type 'Class1' from assembly '...\NoPIAGenerics1-Asm1.dll' cannot be used across assembly boundaries because a type in its inheritance hierarchy has a generic type parameter that is an embedded interop type.
            // <fine-name>(7,10): error CS1772: Type 'Class1' from assembly '...\NoPIAGenerics1-Asm1.dll' cannot be used across assembly boundaries because a type in its inheritance hierarchy has a generic type parameter that is an embedded interop type.
        }

        [WorkItem(541466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541466")]
        [Fact]
        public void UseSiteErrorViaAliasTest07()
        {
            var testAssembly = CreateCompilationWithMscorlib(
@"
using ClassAlias = Class1;
public class Test
{
    void m()
    {
        ClassAlias a = null;
        ClassAlias b = null;
        System.Console.WriteLine(a);
        System.Console.WriteLine(b);
    }
}", references: new List<MetadataReference>() { TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1 })
.VerifyDiagnostics(
                // (2,20): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
                // using ClassAlias = Class1;
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "Class1").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (9,9): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
                //         System.Console.WriteLine(a);
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "System.Console.WriteLine").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,9): error CS1769: Type 'System.Collections.Generic.List<FooStruct>' from assembly 'NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
                //         System.Console.WriteLine(b);
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "System.Console.WriteLine").WithArguments("System.Collections.Generic.List<FooStruct>", "NoPIAGenerics1-Asm1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            // NOTE: Dev10 reports NO ERRORS
        }

        [WorkItem(948674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948674")]
        [Fact]
        public void UseSiteErrorViaImplementedInterfaceMember_1()
        {
            var source1 = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct ImageMoniker
{ }";

            CSharpCompilation comp1 = CreateCompilationWithMscorlib(source1, assemblyName: "Pia948674_1");

            var source2 = @"
public interface IBar
{
    ImageMoniker? Moniker { get; }
}";

            CSharpCompilation comp2 = CreateCompilationWithMscorlib(source2, new MetadataReference[] { new CSharpCompilationReference(comp1, embedInteropTypes: true) }, assemblyName: "Bar948674_1");

            var source3 = @"
public class BarImpl : IBar
{
    public ImageMoniker? Moniker    
    {
        get { return null; }
    }
}";

            CSharpCompilation comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { new CSharpCompilationReference(comp2), new CSharpCompilationReference(comp1, embedInteropTypes: true) });

            comp3.VerifyDiagnostics(
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );

            comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { comp2.EmitToImageReference(), comp1.EmitToImageReference().WithEmbedInteropTypes(true) });

            comp3.VerifyDiagnostics(
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24),
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );
        }

        [WorkItem(948674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948674")]
        [Fact]
        public void UseSiteErrorViaImplementedInterfaceMember_2()
        {
            var source1 = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct ImageMoniker
{ }";

            CSharpCompilation comp1 = CreateCompilationWithMscorlib(source1, assemblyName: "Pia948674_2");

            var source2 = @"
public interface IBar
{
    ImageMoniker? Moniker { get; }
}";

            CSharpCompilation comp2 = CreateCompilationWithMscorlib(source2, new MetadataReference[] { new CSharpCompilationReference(comp1, embedInteropTypes: true) }, assemblyName: "Bar948674_2");

            var source3 = @"
public class BarImpl : IBar
{
    ImageMoniker? IBar.Moniker    
    {
        get { return null; }
    }
}";

            CSharpCompilation comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { new CSharpCompilationReference(comp2), new CSharpCompilationReference(comp1, embedInteropTypes: true) });

            comp3.VerifyDiagnostics(
    // (4,24): error CS0539: 'BarImpl.Moniker' in explicit interface declaration is not a member of interface
    //     ImageMoniker? IBar.Moniker    
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Moniker").WithArguments("BarImpl.Moniker").WithLocation(4, 24),
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );

            comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { comp2.EmitToImageReference(), comp1.EmitToImageReference().WithEmbedInteropTypes(true) });

            comp3.VerifyDiagnostics(
    // (4,24): error CS0539: 'BarImpl.Moniker' in explicit interface declaration is not a member of interface
    //     ImageMoniker? IBar.Moniker    
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Moniker").WithArguments("BarImpl.Moniker").WithLocation(4, 24),
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );
        }

        [WorkItem(948674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948674")]
        [Fact]
        public void UseSiteErrorViaImplementedInterfaceMember_3()
        {
            var source1 = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct ImageMoniker
{ }";

            CSharpCompilation comp1 = CreateCompilationWithMscorlib(source1, assemblyName: "Pia948674_3");

            var source2 = @"
public interface IBar
{
    void SetMoniker(ImageMoniker? moniker);
}";

            CSharpCompilation comp2 = CreateCompilationWithMscorlib(source2, new MetadataReference[] { new CSharpCompilationReference(comp1, embedInteropTypes: true) }, assemblyName: "Bar948674_3");

            var source3 = @"
public class BarImpl : IBar
{
    public void SetMoniker(ImageMoniker? moniker)
    {}
}";

            CSharpCompilation comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { new CSharpCompilationReference(comp2), new CSharpCompilationReference(comp1, embedInteropTypes: true) });

            comp3.VerifyDiagnostics(
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );

            comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { comp2.EmitToImageReference(), comp1.EmitToImageReference().WithEmbedInteropTypes(true) });

            comp3.VerifyDiagnostics(
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );
        }

        [WorkItem(948674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948674")]
        [Fact]
        public void UseSiteErrorViaImplementedInterfaceMember_4()
        {
            var source1 = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct ImageMoniker
{ }";

            CSharpCompilation comp1 = CreateCompilationWithMscorlib(source1, assemblyName: "Pia948674_4");

            var source2 = @"
public interface IBar
{
    void SetMoniker(ImageMoniker? moniker);
}";

            CSharpCompilation comp2 = CreateCompilationWithMscorlib(source2, new MetadataReference[] { new CSharpCompilationReference(comp1, embedInteropTypes: true) }, assemblyName: "Bar948674_4");

            var source3 = @"
public class BarImpl : IBar
{
    void IBar.SetMoniker(ImageMoniker? moniker)
    {}
}";

            CSharpCompilation comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { new CSharpCompilationReference(comp2), new CSharpCompilationReference(comp1, embedInteropTypes: true) });

            comp3.VerifyDiagnostics(
    // (4,15): error CS0539: 'BarImpl.SetMoniker(ImageMoniker?)' in explicit interface declaration is not a member of interface
    //     void IBar.SetMoniker(ImageMoniker? moniker)
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "SetMoniker").WithArguments("BarImpl.SetMoniker(ImageMoniker?)").WithLocation(4, 15),
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );

            comp3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { comp2.EmitToImageReference(), comp1.EmitToImageReference().WithEmbedInteropTypes(true) });

            comp3.VerifyDiagnostics(
    // (4,15): error CS0539: 'BarImpl.SetMoniker(ImageMoniker?)' in explicit interface declaration is not a member of interface
    //     void IBar.SetMoniker(ImageMoniker? moniker)
    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "SetMoniker").WithArguments("BarImpl.SetMoniker(ImageMoniker?)").WithLocation(4, 15),
    // (2,24): error CS1769: Type 'ImageMoniker?' from assembly 'Bar948674_4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
    // public class BarImpl : IBar
    Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "IBar").WithArguments("ImageMoniker?", "Bar948674_4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 24)
                );
        }

        [WorkItem(541246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541246")]
        [Fact]
        public void NamespaceQualifiedGenericTypeName()
        {
            var source =
@"namespace N
{
    public class A<T>
    {
        public static T F;
    }
}
class B
{
    static int G = N.A<int>.F;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void NamespaceQualifiedGenericTypeNameWrongArity()
        {
            var source =
@"namespace N
{
    public class A<T>
    {
        public static T F;
    }

    public class B 
    { 
        public static int F;
    }
    public class B<T1, T2> 
    { 
        public static System.Tuple<T1, T2> F;
    }
}
class C
{
    static int TooMany = N.A<int, int>.F;
    static int TooFew = N.A.F;
    static int TooIndecisive = N.B<int>;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,28): error CS0305: Using the generic type 'N.A<T>' requires '1' type arguments
                // 
                Diagnostic(ErrorCode.ERR_BadArity, "A<int, int>").WithArguments("N.A<T>", "type", "1"),
                // (20,27): error CS0305: Using the generic type 'N.A<T>' requires '1' type arguments
                // 
                Diagnostic(ErrorCode.ERR_BadArity, "A").WithArguments("N.A<T>", "type", "1"),
                // (21,34): error CS0308: The non-generic type 'N.B' cannot be used with type arguments
                // 
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "B<int>").WithArguments("N.B", "type")
                );
        }

        [WorkItem(541570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541570")]
        [Fact]
        public void EnumNotMemberInConstructor()
        {
            var source =
@"enum E { A }
class C
{
    public C(E e = E.A) { }
    public E E { get { return E.A; } }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(541638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541638")]
        [Fact]
        public void KeywordAsLabelIdentifier()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
    @int1:
        System.Console.WriteLine();
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,5): warning CS0164: This label has not been referenced
                // 
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "@int1"));
        }

        [WorkItem(541677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541677")]
        [Fact]
        public void AssignStaticEventToLocalVariable()
        {
            var source =
@"delegate void Foo();
class driver
{
    public static event Foo e;
    static void Main(string[] args)
    {
        Foo x = e;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        // Note: The locations for errors on generic methods are
        // name only, while Dev11 uses name + type parameters.
        [WorkItem(528743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528743")]
        [Fact]
        public void GenericMethodLocation()
        {
            var source =
@"interface I
{
    void M1<T>() where T : class;
}
class C : I
{
    public void M1<T>() { }
    void M2<T>(this object o) { }
    sealed void M3<T>() { }
    internal static virtual void M4<T>() { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,7): error CS1106: Extension method must be defined in a non-generic static class
                // class C : I
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "C").WithLocation(5, 7),
                // (7,17): error CS0425: The constraints for type parameter 'T' of method 'C.M1<T>()' must match the constraints for type parameter 'T' of interface method 'I.M1<T>()'. Consider using an explicit interface implementation instead.
                //     public void M1<T>() { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M1").WithArguments("T", "C.M1<T>()", "T", "I.M1<T>()").WithLocation(7, 17),
                // (9,17): error CS0238: 'C.M3<T>()' cannot be sealed because it is not an override
                //     sealed void M3<T>() { }
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M3").WithArguments("C.M3<T>()").WithLocation(9, 17),
                // (10,34): error CS0112: A static member 'C.M4<T>()' cannot be marked as override, virtual, or abstract
                //     internal static virtual void M4<T>() { }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M4").WithArguments("C.M4<T>()").WithLocation(10, 34));
        }

        [WorkItem(542391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542391")]
        [Fact]
        public void PartialMethodOptionalParameters()
        {
            var source =
@"partial class C
{
    partial void M1(object o);
    partial void M1(object o = null) { }
    partial void M2(object o = null);
    partial void M2(object o) { }
    partial void M3(object o = null);
    partial void M3(object o = null) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,28): warning CS1066: The default value specified for parameter 'o' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void M1(object o = null) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "o").WithArguments("o"),
                // (8,28): warning CS1066: The default value specified for parameter 'o' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void M3(object o = null) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "o").WithArguments("o")
                );
        }

        [Fact]
        [WorkItem(598043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598043")]
        public void PartialMethodParameterNamesFromDefinition1()
        {
            var source = @"
partial class C
{
    partial void F(int i);
}

partial class C
{
    partial void F(int j) { }
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<MethodSymbol>("F");
                Assert.Equal("i", method.Parameters[0].Name);
            });
        }

        [Fact]
        [WorkItem(598043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598043")]
        public void PartialMethodParameterNamesFromDefinition2()
        {
            var source = @"
partial class C
{
    partial void F(int j) { }
}

partial class C
{
    partial void F(int i);
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<MethodSymbol>("F");
                Assert.Equal("i", method.Parameters[0].Name);
            });
        }

        /// <summary>
        /// Handle a mix of parameter errors for default values,
        /// partial methods, and static parameter type.
        /// </summary>
        [Fact]
        public void ParameterErrorsDefaultPartialMethodStaticType()
        {
            var source =
@"static class S { }
partial class C
{
    partial void M(S s = new A());
    partial void M(S s = new B()) { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,18): error CS0721: 'S': static types cannot be used as parameters
                //     partial void M(S s = new A());
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "M").WithArguments("S"),
                // (5,18): error CS0721: 'S': static types cannot be used as parameters
                //     partial void M(S s = new B()) { }
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "M").WithArguments("S"),
                // (5,30): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //     partial void M(S s = new B()) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B"),
                // (5,22): error CS1750: A value of type 'B' cannot be used as a default parameter because there are no standard conversions to type 'S'
                //     partial void M(S s = new B()) { }
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("B", "S"),
                // (5,22): warning CS1066: The default value specified for parameter 's' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void M(S s = new B()) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "s").WithArguments("s"),
                // (4,30): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //     partial void M(S s = new A());
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A"),
                // (4,22): error CS1750: A value of type 'A' cannot be used as a default parameter because there are no standard conversions to type 'S'
                //     partial void M(S s = new A());
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("A", "S")
                );
        }

        [WorkItem(543349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543349")]
        [Fact]
        public void Fixed()
        {
            var source =
@"class C
{
    unsafe static void M(int[] arg)
    {
        fixed (int* ptr = arg) { }
        fixed (int* ptr = arg) *ptr = 0;
        fixed (int* ptr = arg) object o = null;
    }
}";
            CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,32): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         fixed (int* ptr = arg) object o = null;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "object o = null;"));
        }

        [WorkItem(1040171, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040171")]
        [Fact]
        public void Bug1040171()
        {
            const string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        foreach (string s in args)
            label: c = false;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(sourceCode);
            compilation.VerifyDiagnostics(
                // (9,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             label: c = false;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "label: c = false;").WithLocation(9, 13),
                // (9,13): warning CS0164: This label has not been referenced
                //             label: c = false;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(9, 13),
                // (6,14): warning CS0219: The variable 'c' is assigned but its value is never used
                //         bool c = true;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "c").WithArguments("c").WithLocation(6, 14));
        }
        [Fact, WorkItem(543426, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543426")]

        private void NestedInterfaceImplementationWithOuterGenericType()
        {
            CompileAndVerify(@"
namespace System.ServiceModel
{
    class Pipeline<T>
    {
        interface IStage
        {
            void Process(T context);
        }

        class AsyncStage : IStage
        {
            void IStage.Process(T context) { }
        }
    }
}");
        }

        /// <summary>
        /// Error types should be allowed as constant types.
        /// </summary>
        [Fact]
        public void ErrorTypeConst()
        {
            var source =
@"class C
{
    const C1 F1 = 0;
    const C2 F2 = null;
    static void M()
    {
        const C3 c3 = 0;
        const C4 c4 = null;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,11): error CS0246: The type or namespace name 'C1' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C1").WithArguments("C1").WithLocation(3, 11),
                // (4,11): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(4, 11),
                // (7,15): error CS0246: The type or namespace name 'C3' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C3").WithArguments("C3").WithLocation(7, 15),
                // (8,15): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(8, 15));
        }

        [WorkItem(543777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543777")]
        [Fact]
        public void DefaultParameterAtEndOfFile()
        {
            var source =
@"class C
{
    static void M(object o = null,";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,35): error CS1031: Type expected
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_TypeExpected, ""),

                // Cascading:

                // (3,35): error CS1001: Identifier expected
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
                // (3,35): error CS1026: ) expected
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ""),
                // (3,35): error CS1002: ; expected
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ""),
                // (3,35): error CS1513: } expected
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (3,35): error CS1737: Optional parameters must appear after all required parameters
                //     static void M(object o = null,
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ""),
                // (3,17): error CS0501: 'C.M(object, ?)' must declare a body because it is not
                // marked abstract, extern, or partial static void M(object o = null,
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("C.M(object, ?)"));
        }

        [WorkItem(543814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543814")]
        [Fact]
        public void DuplicateNamedArgumentNullLiteral()
        {
            var source =
@"class C
{
    static void M()
    {
        M("""",
            arg: 0,
            arg: null);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,13): error CS1740: Named argument 'arg' cannot be specified multiple times
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "arg").WithArguments("arg").WithLocation(7, 13));
        }

        [WorkItem(543820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543820")]
        [Fact]
        public void GenericAttributeClassWithMultipleParts()
        {
            var source =
@"class C<T> { }
class C<T> : System.Attribute { }";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (2,7): error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
                // class C<T> : System.Attribute { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>"),
                // (2,14): error CS0698: A generic type cannot derive from 'System.Attribute' because it is an attribute class
                // class C<T> : System.Attribute { }
                Diagnostic(ErrorCode.ERR_GenericDerivingFromAttribute, "System.Attribute").WithArguments("System.Attribute")
                );
        }

        [WorkItem(543822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543822")]
        [Fact]
        public void InterfaceWithPartialMethodExplicitImplementation()
        {
            var source =
@"interface I
{
    partial void I.M();
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,20): error CS0754: A partial method may not explicitly implement an interface method
                //     partial void I.M();
                Diagnostic(ErrorCode.ERR_PartialMethodNotExplicit, "M"),
                // (3,20): error CS0751: A partial method must be declared within a partial class or partial struct
                //     partial void I.M();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "M"),
                // (3,20): error CS0541: 'I.M()': explicit interface declaration can only be declared in a class or struct
                //     partial void I.M();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("I.M()")
                );
        }

        [WorkItem(545208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545208")]
        [Fact]
        public void PartialMethodInsidePartialInterface()
        {
            var source =
@"public partial interface IF
{
    partial void Add();
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,18): error CS0751: A partial method must be declared within a partial class or partial struct
                //    partial void Add();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "Add")
                );
        }

        [WorkItem(543827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543827")]
        [Fact]
        public void StructConstructor()
        {
            var source =
@"struct S
{
    private readonly object x;
    private readonly object y;
    S(object x, object y)
    {
        try
        {
            this.x = x;
        }
        finally
        {
            this.y = y;
        }
    }
    S(S o) : this(o.x, o.y) {}
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(543827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543827")]
        [Fact]
        public void StructVersusTryFinally()
        {
            var source =
@"struct S
{
    private object x;
    private object y;
    static void M()
    {
        S s1;
        try { s1.x = null; } finally { s1.y = null; }
        S s2 = s1;
        s1.x = s1.y; s1.y = s1.x;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(544513, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544513")]
        [Fact()]
        public void AnonTypesPropSameNameDiffType()
        {
            var source =
@"public class Test
{
    public static void Main()
    {
        var p1 = new { Price = 495.00 };
        var p2 = new { Price = ""36.50"" };

        p1 = p2;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,14): error CS0029: Cannot implicitly convert type 'AnonymousType#1' to 'AnonymousType#2'
                //        p1 = p2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "p2").WithArguments("<anonymous type: string Price>", "<anonymous type: double Price>"));
        }

        [WorkItem(545869, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545869")]
        [Fact]
        public void TestSealedOverriddenMembers()
        {
            CompileAndVerify(
@"using System;

internal abstract class Base
{
    public virtual int Property
    {
        get { return 0; }
        protected set { }
    }
    protected virtual event EventHandler Event
    {
        add { } remove { }
    }
    protected abstract void Method();
}

internal sealed class Derived : Base
{
    public override int Property
    {
        get { return 1; }
        protected set { }
    }
    protected override event EventHandler Event;
    protected override void Method()  { }

    void UseEvent() { Event(null, null); }
}

internal sealed class Derived2 : Base
{
    public override int Property
    {
        get; protected set;
    }
    protected override event EventHandler Event
    {
        add { } remove { }
    }
    protected override void Method() { }
}

class Program
{
    static void Main() { }
}").VerifyDiagnostics();
        }

        [Fact, WorkItem(1068547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068547")]
        public void Bug1068547_01()
        {
            var source =
@"
class Program
{
    [System.Diagnostics.DebuggerDisplay(this)]
    static void Main(string[] args)
    {
        
    }
}";
            var comp = CreateCompilationWithMscorlib(source);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.ThisExpression)).Cast<ThisExpressionSyntax>().Single();

            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.NotReferencable, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(1068547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068547")]
        public void Bug1068547_02()
        {
            var source =
@"
    [System.Diagnostics.DebuggerDisplay(this)]
";
            var comp = CreateCompilationWithMscorlib(source);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.ThisExpression)).Cast<ThisExpressionSyntax>().Single();

            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.NotReferencable, symbolInfo.CandidateReason);
        }

        [Fact]
        public void RefReturningDelegateCreation()
        {
            var text = @"
delegate ref int D();

class C
{
    int field = 0;

    ref int M()
    {
        return ref field;
    }

    void Test()
    {
        new D(M)();
    }
}
";

            CreateExperimentalCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReturningDelegateCreationBad()
        {
            var text = @"
delegate ref int D();

class C
{
    int field = 0;

    int M()
    {
        return field;
    }

    void Test()
    {
        new D(M)();
    }
}
";

            CreateExperimentalCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReturningDelegateArgument()
        {
            var text = @"
delegate ref int D();

class C
{
    int field = 0;

    ref int M()
    {
        return ref field;
    }

    void M(D d)
    {
    }

    void Test()
    {
        M(M);
    }
}
";

            CreateExperimentalCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReturningDelegateArgumentBad()
        {
            var text = @"
delegate ref int D();

class C
{
    int field = 0;

    int M()
    {
        return field;
    }

    void M(D d)
    {
    }

    void Test()
    {
        M(M);
    }
}
";

            CreateExperimentalCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(1078958, "DevDiv")]
        [Fact, WorkItem(1078958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078958")]
        public void Bug1078958()
        {
            const string source = @"
class C
{
    static void Foo<T>()
    {
        T();
    }
 
    static void T() { }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,9): error CS0119: 'T' is a type, which is not valid in the given context
                //         T();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type").WithLocation(6, 9));
        }

        [Fact, WorkItem(1078958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078958")]
        public void Bug1078958_2()
        {
            const string source = @"
class C
{
    static void Foo<T>()
    {
        T<T>();
    }
 
    static void T() { }

    static void T<U>() { }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961()
        {
            const string source = @"
class C
{
    const int T = 42;
    static void Foo<T>(int x = T)
    {
        System.Console.Write(x);
    }

    static void Main()
    {
        Foo<object>();
    }
}";

            CompileAndVerify(source, expectedOutput: "42");
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_2()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    static void Foo<T>([A(T)] int x)
    {
    }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMembers("C").Single();
            var t = (FieldSymbol)c.GetMembers("T").Single();
            var foo = (MethodSymbol)c.GetMembers("Foo").Single();
            var x = foo.Parameters[0];
            var a = x.GetAttributes()[0];
            var i = a.ConstructorArguments.Single();
            Assert.Equal((int)i.Value, (int)t.ConstantValue);
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_3()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    [A(T)]
    static void Foo<T>(int x)
    {
    }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMembers("C").Single();
            var t = (FieldSymbol)c.GetMembers("T").Single();
            var foo = (MethodSymbol)c.GetMembers("Foo").Single();
            var a = foo.GetAttributes()[0];
            var i = a.ConstructorArguments.Single();
            Assert.Equal((int)i.Value, (int)t.ConstantValue);
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_4()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    static void Foo<[A(T)] T>(int x)
    {
    }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMembers("C").Single();
            var t = (FieldSymbol)c.GetMembers("T").Single();
            var foo = (MethodSymbol)c.GetMembers("Foo").Single();
            var tt = foo.TypeParameters[0];
            var a = tt.GetAttributes()[0];
            var i = a.ConstructorArguments.Single();
            Assert.Equal((int)i.Value, (int)t.ConstantValue);
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_5()
        {
            const string source = @"
class C
{
    class T { }

    static void Foo<T>(T x = default(T))
    {
        System.Console.Write((object)x == null);
    }

    static void Main()
    {
        Foo<object>();
    }
}";

            CompileAndVerify(source, expectedOutput: "True");
        }

        [Fact, WorkItem(3096, "https://github.com/dotnet/roslyn/issues/3096")]
        public void CastToDelegate_01()
        {
            var sourceText = @"namespace NS
{
    public static class A
    {
        public delegate void Action();

        public static void M()
        {
            RunAction(A.B<string>.M0);
            RunAction((Action)A.B<string>.M1);
        }

        private static void RunAction(Action action) { }

        private class B<T>
        {
            public static void M0() { }
            public static void M1() { }
        }
    }
}";

            var compilation = CreateCompilationWithMscorlib(sourceText, options: TestOptions.DebugDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var identifierNameM0 = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(x => x.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) && x.Identifier.ValueText.Equals("M0"));

            Assert.Equal("A.B<string>.M0", identifierNameM0.Parent.ToString());
            var m0Symbol = model.GetSymbolInfo(identifierNameM0);

            Assert.Equal("void NS.A.B<System.String>.M0()", m0Symbol.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, m0Symbol.CandidateReason);

            var identifierNameM1 = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(x => x.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) && x.Identifier.ValueText.Equals("M1"));

            Assert.Equal("A.B<string>.M1", identifierNameM1.Parent.ToString());
            var m1Symbol = model.GetSymbolInfo(identifierNameM1);

            Assert.Equal("void NS.A.B<System.String>.M1()", m1Symbol.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, m1Symbol.CandidateReason);
        }

        [Fact, WorkItem(3096, "https://github.com/dotnet/roslyn/issues/3096")]
        public void CastToDelegate_02()
        {
            var sourceText = @"
class A
{
    public delegate void MyDelegate<T>(T a);

    public void Test()
    {
        UseMyDelegate((MyDelegate<int>)MyMethod);
        UseMyDelegate((MyDelegate<long>)MyMethod);
        UseMyDelegate((MyDelegate<float>)MyMethod);
        UseMyDelegate((MyDelegate<double>)MyMethod);
    }

    private void UseMyDelegate<T>(MyDelegate<T> f) { }

    private static void MyMethod(int a) { }
    private static void MyMethod(long a) { }
    private static void MyMethod(float a) { }
    private static void MyMethod(double a) { }
}";

            var compilation = CreateCompilationWithMscorlib(sourceText, options: TestOptions.DebugDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var identifiers = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(x => x.Identifier.ValueText.Equals("MyMethod")).ToArray();

            Assert.Equal(4, identifiers.Length);

            Assert.Equal("(MyDelegate<int>)MyMethod", identifiers[0].Parent.ToString());
            Assert.Equal("void A.MyMethod(System.Int32 a)", model.GetSymbolInfo(identifiers[0]).Symbol.ToTestDisplayString());

            Assert.Equal("(MyDelegate<long>)MyMethod", identifiers[1].Parent.ToString());
            Assert.Equal("void A.MyMethod(System.Int64 a)", model.GetSymbolInfo(identifiers[1]).Symbol.ToTestDisplayString());

            Assert.Equal("(MyDelegate<float>)MyMethod", identifiers[2].Parent.ToString());
            Assert.Equal("void A.MyMethod(System.Single a)", model.GetSymbolInfo(identifiers[2]).Symbol.ToTestDisplayString());

            Assert.Equal("(MyDelegate<double>)MyMethod", identifiers[3].Parent.ToString());
            Assert.Equal("void A.MyMethod(System.Double a)", model.GetSymbolInfo(identifiers[3]).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(3096, "https://github.com/dotnet/roslyn/issues/3096")]
        public void CastToDelegate_03()
        {
            var sourceText = @"namespace NS
{
    public static class A
    {
        public delegate void Action();

        public static void M()
        {
            var b = new A.B<string>();
            RunAction(b.M0);
            RunAction((Action)b.M1);
        }

        private static void RunAction(Action action) { }

        public class B<T>
        {
        }

        public static void M0<T>(this B<T> x) { }
        public static void M1<T>(this B<T> x) { }
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceText, options: TestOptions.DebugDll);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var identifierNameM0 = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(x => x.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) && x.Identifier.ValueText.Equals("M0"));

            Assert.Equal("b.M0", identifierNameM0.Parent.ToString());
            var m0Symbol = model.GetSymbolInfo(identifierNameM0);

            Assert.Equal("void NS.A.B<System.String>.M0<System.String>()", m0Symbol.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, m0Symbol.CandidateReason);

            var identifierNameM1 = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(x => x.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) && x.Identifier.ValueText.Equals("M1"));

            Assert.Equal("b.M1", identifierNameM1.Parent.ToString());
            var m1Symbol = model.GetSymbolInfo(identifierNameM1);

            Assert.Equal("void NS.A.B<System.String>.M1<System.String>()", m1Symbol.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, m1Symbol.CandidateReason);
        }

        [Fact, WorkItem(5170, "https://github.com/dotnet/roslyn/issues/5170")]
        public void TypeOfBinderParameter()
        {
            var sourceText = @"
using System.Linq;
using System.Text;

public static class LazyToStringExtension
{
    public static string LazyToString<T>(this T obj) where T : class
    {
        StringBuilder sb = new StringBuilder();
        typeof(T)
            .GetProperties(System.Reflection.BindingFlags.Public)
            .Select(x => x.GetValue(obj))
    }
}";
            var compilation = CreateCompilationWithMscorlib(sourceText, new[] { SystemCoreRef }, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (12,42): error CS1002: ; expected
                //             .Select(x => x.GetValue(obj))
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(12, 42),
                // (12,28): error CS1501: No overload for method 'GetValue' takes 1 arguments
                //             .Select(x => x.GetValue(obj))
                Diagnostic(ErrorCode.ERR_BadArgCount, "GetValue").WithArguments("GetValue", "1").WithLocation(12, 28),
                // (7,26): error CS0161: 'LazyToStringExtension.LazyToString<T>(T)': not all code paths return a value
                //     public static string LazyToString<T>(this T obj) where T : class
                Diagnostic(ErrorCode.ERR_ReturnExpected, "LazyToString").WithArguments("LazyToStringExtension.LazyToString<T>(T)").WithLocation(7, 26));
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SimpleLambdaExpression)).Single();
            var param = node.ChildNodes().Where(n => n.IsKind(SyntaxKind.Parameter)).Single();
            Assert.Equal("System.Reflection.PropertyInfo x", model.GetDeclaredSymbol(param).ToTestDisplayString());
        }

        [Fact, WorkItem(5128, "https://github.com/dotnet/roslyn/issues/5128")]
        public void GetMemberGroupInsideIncompleteLambda_01()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

public delegate Task RequestDelegate(HttpContext context);

public class AuthenticationResult { }

public abstract class AuthenticationManager
{
    public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme);
}

public abstract class HttpContext
{
    public abstract AuthenticationManager Authentication { get; }
}

interface IApplicationBuilder
{
    IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
}

static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder Use(this IApplicationBuilder app, Func<HttpContext, Func<Task>, Task> middleware)
    {
        return app;
    }
}

class C
{
    void M(IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            await ctx.Authentication.AuthenticateAsync();
        });
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef });

            comp.VerifyDiagnostics(
    // (41,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'authenticationScheme' of 'AuthenticationManager.AuthenticateAsync(string)'
    //             await ctx.Authentication.AuthenticateAsync();
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AuthenticateAsync").WithArguments("authenticationScheme", "AuthenticationManager.AuthenticateAsync(string)").WithLocation(38, 38)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "Use").Single().Parent;
            Assert.Equal("app.Use", node1.ToString());
            var group1 = model.GetMemberGroup(node1);
            Assert.Equal(2, group1.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", group1[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)",
                         group1[1].ToTestDisplayString());

            var symbolInfo1 = model.GetSymbolInfo(node1);
            Assert.Null(symbolInfo1.Symbol);
            Assert.Equal(1, symbolInfo1.CandidateSymbols.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", symbolInfo1.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo1.CandidateReason);

            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "AuthenticateAsync").Single().Parent;

            Assert.Equal("ctx.Authentication.AuthenticateAsync", node.ToString());

            var group = model.GetMemberGroup(node);

            Assert.Equal("System.Threading.Tasks.Task<AuthenticationResult> AuthenticationManager.AuthenticateAsync(System.String authenticationScheme)", group.Single().ToTestDisplayString());
        }

        [Fact, WorkItem(5128, "https://github.com/dotnet/roslyn/issues/5128")]
        public void GetMemberGroupInsideIncompleteLambda_02()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

public delegate Task RequestDelegate(HttpContext context);

public class AuthenticationResult { }

public abstract class AuthenticationManager
{
    public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme);
}

public abstract class HttpContext
{
    public abstract AuthenticationManager Authentication { get; }
}

interface IApplicationBuilder
{
    IApplicationBuilder Use(Func<HttpContext, Func<Task>, Task> middleware);
}

static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder Use(this IApplicationBuilder app, Func<RequestDelegate, RequestDelegate> middleware)
    {
        return app;
    }
}

class C
{
    void M(IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            await ctx.Authentication.AuthenticateAsync();
        });
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef });

            comp.VerifyDiagnostics(
    // (41,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'authenticationScheme' of 'AuthenticationManager.AuthenticateAsync(string)'
    //             await ctx.Authentication.AuthenticateAsync();
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AuthenticateAsync").WithArguments("authenticationScheme", "AuthenticationManager.AuthenticateAsync(string)").WithLocation(38, 38)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "Use").Single().Parent;
            Assert.Equal("app.Use", node1.ToString());
            var group1 = model.GetMemberGroup(node1);
            Assert.Equal(2, group1.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)",
                         group1[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", group1[1].ToTestDisplayString());

            var symbolInfo1 = model.GetSymbolInfo(node1);
            Assert.Null(symbolInfo1.Symbol);
            Assert.Equal(1, symbolInfo1.CandidateSymbols.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)", symbolInfo1.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo1.CandidateReason);

            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "AuthenticateAsync").Single().Parent;

            Assert.Equal("ctx.Authentication.AuthenticateAsync", node.ToString());

            var group = model.GetMemberGroup(node);

            Assert.Equal("System.Threading.Tasks.Task<AuthenticationResult> AuthenticationManager.AuthenticateAsync(System.String authenticationScheme)", group.Single().ToTestDisplayString());
        }

        [Fact, WorkItem(5128, "https://github.com/dotnet/roslyn/issues/5128")]
        public void GetMemberGroupInsideIncompleteLambda_03()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

public delegate Task RequestDelegate(HttpContext context);

public class AuthenticationResult { }

public abstract class AuthenticationManager
{
    public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme);
}

public abstract class HttpContext
{
    public abstract AuthenticationManager Authentication { get; }
}

interface IApplicationBuilder
{
    IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
    IApplicationBuilder Use(Func<HttpContext, Func<Task>, Task> middleware);
}

class C
{
    void M(IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            await ctx.Authentication.AuthenticateAsync();
        });
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef });

            comp.VerifyDiagnostics(
    // (41,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'authenticationScheme' of 'AuthenticationManager.AuthenticateAsync(string)'
    //             await ctx.Authentication.AuthenticateAsync();
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AuthenticateAsync").WithArguments("authenticationScheme", "AuthenticationManager.AuthenticateAsync(string)").WithLocation(31, 38)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "Use").Single().Parent;
            Assert.Equal("app.Use", node1.ToString());
            var group1 = model.GetMemberGroup(node1);
            Assert.Equal(2, group1.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", group1[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)",
                         group1[1].ToTestDisplayString());

            var symbolInfo1 = model.GetSymbolInfo(node1);
            Assert.Null(symbolInfo1.Symbol);
            Assert.Equal(2, symbolInfo1.CandidateSymbols.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", symbolInfo1.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)", symbolInfo1.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo1.CandidateReason);

            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "AuthenticateAsync").Single().Parent;

            Assert.Equal("ctx.Authentication.AuthenticateAsync", node.ToString());

            var group = model.GetMemberGroup(node);

            Assert.Equal("System.Threading.Tasks.Task<AuthenticationResult> AuthenticationManager.AuthenticateAsync(System.String authenticationScheme)", group.Single().ToTestDisplayString());
        }

        [Fact, WorkItem(5128, "https://github.com/dotnet/roslyn/issues/5128")]
        public void GetMemberGroupInsideIncompleteLambda_04()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

public delegate Task RequestDelegate(HttpContext context);

public class AuthenticationResult { }

public abstract class AuthenticationManager
{
    public abstract Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme);
}

public abstract class HttpContext
{
    public abstract AuthenticationManager Authentication { get; }
}

interface IApplicationBuilder
{
}

static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder Use(this IApplicationBuilder app, Func<RequestDelegate, RequestDelegate> middleware)
    {
        return app;
    }

    public static IApplicationBuilder Use(this IApplicationBuilder app, Func<HttpContext, Func<Task>, Task> middleware)
    {
        return app;
    }
}

class C
{
    void M(IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            await ctx.Authentication.AuthenticateAsync();
        });
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef });

            comp.VerifyDiagnostics(
    // (41,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'authenticationScheme' of 'AuthenticationManager.AuthenticateAsync(string)'
    //             await ctx.Authentication.AuthenticateAsync();
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AuthenticateAsync").WithArguments("authenticationScheme", "AuthenticationManager.AuthenticateAsync(string)").WithLocation(42, 38)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "Use").Single().Parent;
            Assert.Equal("app.Use", node1.ToString());
            var group1 = model.GetMemberGroup(node1);
            Assert.Equal(2, group1.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", group1[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)",
                         group1[1].ToTestDisplayString());

            var symbolInfo1 = model.GetSymbolInfo(node1);
            Assert.Null(symbolInfo1.Symbol);
            Assert.Equal(2, symbolInfo1.CandidateSymbols.Length);
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<RequestDelegate, RequestDelegate> middleware)", symbolInfo1.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("IApplicationBuilder IApplicationBuilder.Use(System.Func<HttpContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware)", symbolInfo1.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo1.CandidateReason);

            var node = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "AuthenticateAsync").Single().Parent;

            Assert.Equal("ctx.Authentication.AuthenticateAsync", node.ToString());

            var group = model.GetMemberGroup(node);

            Assert.Equal("System.Threading.Tasks.Task<AuthenticationResult> AuthenticationManager.AuthenticateAsync(System.String authenticationScheme)", group.Single().ToTestDisplayString());
        }

        [Fact, WorkItem(7101, "https://github.com/dotnet/roslyn/issues/7101")]
        public void UsingStatic_01()
        {
            var source =
@"
using System;
using static ClassWithNonStaticMethod;
using static Extension1;

class Program
{
    static void Main(string[] args)
    {
        var instace = new Program();
        instace.NonStaticMethod();
    }

    private void NonStaticMethod()
    {
        MathMin(0, 1);
        MathMax(0, 1);
        MathMax2(0, 1);
        
        int x;
        x = F1;
        x = F2;

        x.MathMax2(3);
    }
}

class ClassWithNonStaticMethod
{
    public static int MathMax(int a, int b)
    {
        return Math.Max(a, b);
    }

    public int MathMin(int a, int b)
    {
        return Math.Min(a, b);
    }

    public int F2 = 0;
}

static class Extension1
{
    public static int MathMax2(this int a, int b)
    {
        return Math.Max(a, b);
    }

    public static int F1 = 0;
}

static class Extension2
{
    public static int MathMax3(this int a, int b)
    {
        return Math.Max(a, b);
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source);

            comp.VerifyDiagnostics(
    // (16,9): error CS0103: The name 'MathMin' does not exist in the current context
    //         MathMin(0, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "MathMin").WithArguments("MathMin").WithLocation(16, 9),
    // (18,9): error CS0103: The name 'MathMax2' does not exist in the current context
    //         MathMax2(0, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "MathMax2").WithArguments("MathMax2").WithLocation(18, 9),
    // (22,13): error CS0103: The name 'F2' does not exist in the current context
    //         x = F2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "F2").WithArguments("F2").WithLocation(22, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "MathMin").Single().Parent;
            Assert.Equal("MathMin(0, 1)", node1.ToString());

            var names = model.LookupNames(node1.SpanStart);
            Assert.False(names.Contains("MathMin"));
            Assert.True(names.Contains("MathMax"));
            Assert.True(names.Contains("F1"));
            Assert.False(names.Contains("F2"));
            Assert.False(names.Contains("MathMax2"));
            Assert.False(names.Contains("MathMax3"));

            Assert.True(model.LookupSymbols(node1.SpanStart, name: "MathMin").IsEmpty);
            Assert.Equal(1, model.LookupSymbols(node1.SpanStart, name: "MathMax").Length);
            Assert.Equal(1, model.LookupSymbols(node1.SpanStart, name: "F1").Length);
            Assert.True(model.LookupSymbols(node1.SpanStart, name: "F2").IsEmpty);
            Assert.True(model.LookupSymbols(node1.SpanStart, name: "MathMax2").IsEmpty);
            Assert.True(model.LookupSymbols(node1.SpanStart, name: "MathMax3").IsEmpty);

            var symbols = model.LookupSymbols(node1.SpanStart);
            Assert.False(symbols.Where(s => s.Name == "MathMin").Any());
            Assert.True(symbols.Where(s => s.Name == "MathMax").Any());
            Assert.True(symbols.Where(s => s.Name == "F1").Any());
            Assert.False(symbols.Where(s => s.Name == "F2").Any());
            Assert.False(symbols.Where(s => s.Name == "MathMax2").Any());
            Assert.False(symbols.Where(s => s.Name == "MathMax3").Any());
        }

        [Fact, WorkItem(8234, "https://github.com/dotnet/roslyn/issues/8234")]
        public void EventAccessInTypeNameContext()
        {
            var source =
@"
class Program
{
    static void Main() {}

    event System.EventHandler E1;

    void Test(Program x)
    {
        System.Console.WriteLine();
        x.E1.E
        System.Console.WriteLine();
    }

    void Dummy()
    {
        E1 = null;
        var x = E1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
    // (11,15): error CS1001: Identifier expected
    //         x.E1.E
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(11, 15),
    // (11,15): error CS1002: ; expected
    //         x.E1.E
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(11, 15),
    // (11,9): error CS0118: 'x' is a variable but is used like a type
    //         x.E1.E
    Diagnostic(ErrorCode.ERR_BadSKknown, "x").WithArguments("x", "variable", "type").WithLocation(11, 9)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText == "E").Single().Parent;
            Assert.Equal("x.E1.E", node1.ToString());
            Assert.Equal(SyntaxKind.QualifiedName, node1.Kind());

            var node2 = ((QualifiedNameSyntax)node1).Left;
            Assert.Equal("x.E1", node2.ToString());

            var symbolInfo2 = model.GetSymbolInfo(node2);
            Assert.Null(symbolInfo2.Symbol);
            Assert.Equal("event System.EventHandler Program.E1", symbolInfo2.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.NotATypeOrNamespace, symbolInfo2.CandidateReason);

            var symbolInfo1 = model.GetSymbolInfo(node1);
            Assert.Null(symbolInfo1.Symbol);
            Assert.True(symbolInfo1.CandidateSymbols.IsEmpty);
        }
    }
}
