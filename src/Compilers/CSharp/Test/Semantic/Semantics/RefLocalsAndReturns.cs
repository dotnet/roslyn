// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RefLocalsAndReturnsTeats : CompilingTestBase
    {

        [Fact]
        public void RefLocalMissingInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        ref int x;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (6,17): error CS8935: A declaration of a by-reference variable must have an initializer
    //         ref int x;
    Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "x").WithLocation(6, 17),
    // (6,17): warning CS0168: The variable 'x' is declared but never used
    //         ref int x;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void RefLocalHasValueInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        int a = 123;
        ref int x = a;
        var y = x;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (7,17): error CS8933: Cannot initialize a by-reference variable with a value
    //         ref int x = a;
    Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "x = a").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ValLocalHasRefInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        int a = 123;
        ref int x = ref a;
        var y = ref x;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (8,13): error CS8932: Cannot initialize a by-value variable with a reference
    //         var y = ref x;
    Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "y = ref x").WithLocation(8, 13)
                );
        }

        [Fact]
        public void RefReturnNotLValue()
        {
            var text = @"
class Test
{
    ref int A()
    {
        return ref 2 + 2;
    }

    ref int B()
    {
        return ref 2;
    }

    ref object C()
    {
        return ref null;
    }

    void VoidMethod(){}

    ref object D()
    {
        return ref VoidMethod();
    }

    int P1 {get{return 1;} set{}}

    ref int E()
    {
        return ref P1;
    }

}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (6,20): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(6, 20),
    // (11,20): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(11, 20),
    // (16,20): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         return ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(16, 20),
    // (23,20): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         return ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(23, 20),
    // (30,20): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         return ref P1;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P1").WithArguments("Test.P1").WithLocation(30, 20)
            );
        }

        [Fact]
        public void RefReturnNotLValue1()
        {
            var text = @"
class Test
{
    delegate ref int D1();
    delegate ref object D2();

    void Test1()
    {
        D1 d1 = () => ref 2 + 2;
        D1 d2 = () => ref 2;
        D2 d3 = () => ref null;
        D2 d4 = () => ref VoidMethod();
        D1 d5 = () => ref P1;
    }

    void VoidMethod(){}
    int P1 {get{return 1;} set{}}
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (9,27): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d1 = () => ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(9, 27),
    // (10,27): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d2 = () => ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(10, 27),
    // (11,27): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d3 = () => ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(11, 27),
    // (12,27): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d4 = () => ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(12, 27),
    // (13,27): error CS8910: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d5 = () => ref P1;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P1").WithArguments("Test.P1").WithLocation(13, 27));
        }

        [Fact]
        public void RefByValLocalParam()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            char l = default(char);
            // valid
            ref char r = ref l;

            // invalid
            return ref l;
        }

        if (2.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref char r = ref l.x;

            // invalid
            return ref l.x;
        }

        if (3.ToString() == null)
        {
            // valid
            ref char r = ref arg1;

            // invalid
            return ref arg1;
        }

        if (4.ToString() == null)
        {
            // valid
            ref char r = ref arg2.x;

            // invalid
            return ref arg2.x;
        }

        throw null;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8924: Cannot return local 'l' by reference because it is not a ref local
    //             return ref l;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "l").WithArguments("l").WithLocation(18, 24),
    // (28,24): error CS8925: Cannot return a member of local 'l' by reference because it is not a ref local
    //             return ref l.x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "l").WithArguments("l").WithLocation(28, 24),
    // (37,24): error CS8922: Cannot return a parameter by reference 'arg1' because it is not a ref or out parameter
    //             return ref arg1;
    Diagnostic(ErrorCode.ERR_RefReturnParameter, "arg1").WithArguments("arg1").WithLocation(37, 24),
    // (46,24): error CS8923: Cannot return or a member of parameter 'arg2' by reference because it is not a ref or out parameter
    //             return ref arg2.x;
    Diagnostic(ErrorCode.ERR_RefReturnParameter2, "arg2").WithArguments("arg2").WithLocation(46, 24)
            );
        }


        [Fact]
        public void RefReadonlyLocal()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public int x;
    }

    ref char Test1()
    {
        foreach(var ro in ""qqq"")
        {
            ref char r = ref ro;
        }

        foreach(var ro in ""qqq"")
        {
            return ref ro;
        }

        foreach(var ro in new S1[1])
        {
            ref char r = ref ro.x;
        }

        foreach(var ro in new S1[1])
        {
            return ref ro.x;
        }


        throw null;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (13,30): error CS1657: Cannot use 'ro' as a ref or out value because it is a 'foreach iteration variable'
    //             ref char r = ref ro;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(13, 30),
    // (18,24): error CS8924: Cannot return local 'ro' by reference because it is not a ref local
    //             return ref ro;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "ro").WithArguments("ro").WithLocation(18, 24),
    // (23,30): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
    //             ref char r = ref ro.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(23, 30),
    // (28,24): error CS8925: Cannot return a member of local 'ro' by reference because it is not a ref local
    //             return ref ro.x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "ro").WithArguments("ro").WithLocation(28, 24)
            );
        }

        [Fact]
        public void RefRangeVar()
        {
            var text = @"
using System.Linq;

public class Test
{
    public struct S1
    {
        public char x;
    }

    delegate ref char D1();

    static void Test1()
    {
        var x = from ch in ""qqq""
            select(D1)(() => ref ch);

        var y = from s in new S1[10]
            select(D1)(() => ref s.x);
    }

}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text, references: new[] { MscorlibRef, SystemCoreRef });
            comp.VerifyDiagnostics(
    // (16,34): error CS8913: Cannot return the range variable 'ch' by reference
    //             select(D1)(() => ref ch);
    Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "ch").WithArguments("ch").WithLocation(16, 34),
    // (19,34): error CS8913: Cannot return the range variable 's' by reference
    //             select(D1)(() => ref s.x);
    Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "s.x").WithArguments("s").WithLocation(19, 34)
            );
        }
        
        [Fact]
        public void RefMethodGroup()
        {
            var text = @"

public class Test
{
    public struct S1
    {
        public char x;
    }

    delegate ref char D1();

    static ref char Test1()
    {
        ref char r = ref M;
        ref char r1 = ref MR;

        if (1.ToString() != null)
        {
            return ref M;
        }
        else
        {
            return ref MR;
        }
    }

    static char M()
    {
        return default(char);
    }

    static ref char MR()
    {
        return ref (new char[1])[0];
    }

}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text, references: new[] { MscorlibRef, SystemCoreRef });
            comp.VerifyDiagnostics(
    // (14,26): error CS1657: Cannot use 'M' as a ref or out value because it is a 'method group'
    //         ref char r = ref M;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(14, 26),
    // (15,27): error CS1657: Cannot use 'MR' as a ref or out value because it is a 'method group'
    //         ref char r1 = ref MR;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "MR").WithArguments("MR", "method group").WithLocation(15, 27),
    // (19,24): error CS1657: Cannot use 'M' as a ref or out value because it is a 'method group'
    //             return ref M;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(19, 24),
    // (23,24): error CS1657: Cannot use 'MR' as a ref or out value because it is a 'method group'
    //             return ref MR;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "MR").WithArguments("MR", "method group").WithLocation(23, 24)
            );
        }
        
        [Fact]
        public void RefReadonlyField()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    public static readonly char s1;
    public static readonly S1 s2;

    public readonly char i1;
    public readonly S1 i2;

    public Test()
    {
        if (1.ToString() != null)
        {
            // not an error
            ref char temp = ref i1;
            temp.ToString();
        }
        else
        {
            // not an error
            ref char temp = ref i2.x;
            temp.ToString();
        }

        if (1.ToString() != null)
        {
            // error
            ref char temp = ref s1;
            temp.ToString();
        }
        else
        {
            // error
            ref char temp = ref s2.x;
            temp.ToString();
        }

    }

    static Test()
    {
        if (1.ToString() != null)
        {
            // not an error
            ref char temp = ref s1;
            temp.ToString();
        }
        else
        {
            // not an error
            ref char temp = ref s2.x;
            temp.ToString();
        }
    }

    ref char Test1()
    {
        if (1.ToString() != null)
        {
            ref char temp = ref i1;
            temp.ToString();

            return ref i1;
        }
        else
        {
            ref char temp = ref i2.x;
            temp.ToString();

            return ref i2.x;
        }
    }

    ref char Test2()
    {
        if (1.ToString() != null)
        {
            ref char temp = ref s1;
            temp.ToString();

            return ref s1;
        }
        else
        {
            ref char temp = ref s2.x;
            temp.ToString();

            return ref s2.x;
        }
    }

}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (33,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s1;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(33, 33),
    // (39,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s2.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(39, 33),
    // (65,33): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
    //             ref char temp = ref i1;
    Diagnostic(ErrorCode.ERR_RefReadonly, "i1").WithLocation(65, 33),
    // (68,24): error CS8916: A readonly field cannot be returned by reference
    //             return ref i1;
    Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i1").WithLocation(68, 24),
    // (72,33): error CS1649: Members of readonly field 'Test.i2' cannot be used as a ref or out value (except in a constructor)
    //             ref char temp = ref i2.x;
    Diagnostic(ErrorCode.ERR_RefReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(72, 33),
    // (75,24): error CS8918: Members of readonly field 'Test.i2' cannot be returned by reference
    //             return ref i2.x;
    Diagnostic(ErrorCode.ERR_RefReturnReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(75, 24),
    // (83,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s1;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(83, 33),
    // (86,24): error CS8917: A static readonly field cannot be returned by reference
    //             return ref s1;
    Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic, "s1").WithLocation(86, 24),
    // (90,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s2.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(90, 33),
    // (93,24): error CS8919: Fields of static readonly field 'Test.s2' cannot be returned by reference
    //             return ref s2.x;
    Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(93, 24)
            );
        }

        [Fact]
        public void RefReadonlyCall()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;

        public ref S1 FooS()
        {
            return ref this;
        }        

        public ref char Foo()
        {
            return ref x;
        }

        public ref char Foo1()
        {
            return ref this.x;
        }
    }

    static ref T Foo<T>(ref T arg)
    {
        return ref arg;
    }

    static ref char Test1()
    {
        char M1 = default(char);
        S1   M2 = default(S1);

        if (1.ToString() != null)
        {
            return ref Foo(ref M1);
        }
        
        if (2.ToString() != null)
        {
            return ref Foo(ref M2.x);
        }

        if (3.ToString() != null)
        {
            return ref Foo(ref M2).x;
        }
        else
        {
            return ref M2.Foo();
        }
    }
  

}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (10,24): error CS8927: Struct members cannot return 'this' or other instance members by reference
    //             return ref this;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithArguments("this").WithLocation(10, 24),
    // (15,24): error CS8927: Struct members cannot return 'this' or other instance members by reference
    //             return ref x;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithArguments("this").WithLocation(15, 24),
    // (20,24): error CS8927: Struct members cannot return 'this' or other instance members by reference
    //             return ref this.x;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this.x").WithArguments("this").WithLocation(20, 24),
    // (36,32): error CS8924: Cannot return local 'M1' by reference because it is not a ref local
    //             return ref Foo(ref M1);
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "M1").WithArguments("M1").WithLocation(36, 32),
    // (36,24): error CS8920: Cannot return by reference a result of 'Test.Foo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
    //             return ref Foo(ref M1);
    Diagnostic(ErrorCode.ERR_RefReturnCall, "Foo(ref M1)").WithArguments("Test.Foo<char>(ref char)", "arg").WithLocation(36, 24),
    // (41,32): error CS8925: Cannot return a member of local 'M2' by reference because it is not a ref local
    //             return ref Foo(ref M2.x);
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "M2").WithArguments("M2").WithLocation(41, 32),
    // (41,24): error CS8920: Cannot return by reference a result of 'Test.Foo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
    //             return ref Foo(ref M2.x);
    Diagnostic(ErrorCode.ERR_RefReturnCall, "Foo(ref M2.x)").WithArguments("Test.Foo<char>(ref char)", "arg").WithLocation(41, 24),
    // (46,32): error CS8924: Cannot return local 'M2' by reference because it is not a ref local
    //             return ref Foo(ref M2).x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "M2").WithArguments("M2").WithLocation(46, 32),
    // (46,24): error CS8921: Cannot return by reference a member of result of 'Test.Foo<Test.S1>(ref Test.S1)' because the argument passed to parameter 'arg' cannot be returned by reference
    //             return ref Foo(ref M2).x;
    Diagnostic(ErrorCode.ERR_RefReturnCall2, "Foo(ref M2)").WithArguments("Test.Foo<Test.S1>(ref Test.S1)", "arg").WithLocation(46, 24));
        }

        [Fact]
        public void RefReturnUnreturnableLocalParam()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            char l = default(char);
            // valid
            ref char r = ref l;

            // invalid
            return ref r;
        }

        if (2.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref char r = ref l.x;

            // invalid
            return ref r;
        }

        if (21.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref var r = ref l;

            // invalid
            return ref r.x;
        }

        if (3.ToString() == null)
        {
            // valid
            ref char r = ref arg1;

            // invalid
            return ref r;
        }

        if (4.ToString() == null)
        {
            // valid
            ref char r = ref arg2.x;

            // invalid
            return ref r;
        }

        if (41.ToString() == null)
        {
            // valid
            ref S1 r = ref arg2;

            // invalid
            return ref r.x;
        }

        throw null;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(18, 24),
    // (28,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(28, 24),
    // (38,24): error CS8912: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(38, 24),
    // (47,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(47, 24),
    // (56,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(56, 24),
    // (65,24): error CS8912: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(65, 24)

            );
        }

        [Fact]
        public void RefReturnSelfReferringRef()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Foo(ref char a, ref char b)
    {
        return ref a;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            ref char r = ref r;
            return ref r;   //1
        }

        if (2.ToString() == null)
        {
            ref S1 r = ref r;
            return ref r.x;  //2
        }

        if (3.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char invalid = ref Foo(ref a, ref a);

            // valid
            return ref r;
        }

        if (4.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char valid = ref Foo(ref a, ref arg1);

            // valid
            return ref valid; //4
        }

        if (5.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char r = ref Foo(ref a, ref r);

            // invalid
            return ref r;  //5
        }

        throw null;
    }
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics(
    // (19,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;   //1
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(19, 24),
    // (25,24): error CS8912: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;  //2
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(25, 24),
    // (34,24): error CS0103: The name 'r' does not exist in the current context
    //             return ref r;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(34, 24),
    // (43,24): error CS8911: Cannot return 'valid' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref valid; //4
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "valid").WithArguments("valid").WithLocation(43, 24),
    // (52,24): error CS8911: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;  //5
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(52, 24),
    // (18,30): error CS0165: Use of unassigned local variable 'r'
    //             ref char r = ref r;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(18, 30),
    // (24,28): error CS0165: Use of unassigned local variable 'r'
    //             ref S1 r = ref r;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(24, 28),
    // (49,45): error CS0165: Use of unassigned local variable 'r'
    //             ref char r = ref Foo(ref a, ref r);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(49, 45)

            );
        }

    }
}
