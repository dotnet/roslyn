// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RefLocalsAndReturnsTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilationRef(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            return CreateCompilationWithMscorlib45(text);
        }

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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (6,17): error CS8174: A declaration of a by-reference variable must have an initializer
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (7,17): error CS8172: Cannot initialize a by-reference variable with a value
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (8,13): error CS8171: Cannot initialize a by-value variable with a reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (6,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(6, 20),
    // (11,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(11, 20),
    // (16,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(16, 20),
    // (23,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(23, 20),
    // (30,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (9,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d1 = () => ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(9, 27),
    // (10,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d2 = () => ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(10, 27),
    // (11,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d3 = () => ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(11, 27),
    // (12,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d4 = () => ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(12, 27),
    // (13,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8168: Cannot return local 'l' by reference because it is not a ref local
    //             return ref l;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "l").WithArguments("l").WithLocation(18, 24),
    // (28,24): error CS8169: Cannot return a member of local 'l' by reference because it is not a ref local
    //             return ref l.x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "l").WithArguments("l").WithLocation(28, 24),
    // (37,24): error CS8166: Cannot return a parameter by reference 'arg1' because it is not a ref or out parameter
    //             return ref arg1;
    Diagnostic(ErrorCode.ERR_RefReturnParameter, "arg1").WithArguments("arg1").WithLocation(37, 24),
    // (46,24): error CS8167: Cannot return or a member of parameter 'arg2' by reference because it is not a ref or out parameter
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (13,30): error CS1657: Cannot use 'ro' as a ref or out value because it is a 'foreach iteration variable'
    //             ref char r = ref ro;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(13, 30),
    // (18,24): error CS8168: Cannot return local 'ro' by reference because it is not a ref local
    //             return ref ro;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "ro").WithArguments("ro").WithLocation(18, 24),
    // (23,30): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
    //             ref char r = ref ro.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(23, 30),
    // (28,24): error CS8169: Cannot return a member of local 'ro' by reference because it is not a ref local
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
            var comp = CreateCompilationRef(text, references: new[] { MscorlibRef, SystemCoreRef });
            comp.VerifyDiagnostics(
                // (2,14): error CS0234: The type or namespace name 'Linq' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // using System.Linq;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Linq").WithArguments("Linq", "System").WithLocation(2, 14),
                // (15,28): error CS1935: Could not find an implementation of the query pattern for source type 'string'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
                //         var x = from ch in "qqq"
                Diagnostic(ErrorCode.ERR_QueryNoProviderStandard, @"""qqq""").WithArguments("string", "Select").WithLocation(15, 28),
                // (18,27): error CS1935: Could not find an implementation of the query pattern for source type 'Test.S1[]'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
                //         var y = from s in new S1[10]
                Diagnostic(ErrorCode.ERR_QueryNoProviderStandard, "new S1[10]").WithArguments("Test.S1[]", "Select").WithLocation(18, 27),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;").WithLocation(2, 1)
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
            var comp = CreateCompilationRef(text, references: new[] { MscorlibRef, SystemCoreRef });
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
            var comp = CreateCompilationRef(text);
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
    // (68,24): error CS8160: A readonly field cannot be returned by reference
    //             return ref i1;
    Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i1").WithLocation(68, 24),
    // (72,33): error CS1649: Members of readonly field 'Test.i2' cannot be used as a ref or out value (except in a constructor)
    //             ref char temp = ref i2.x;
    Diagnostic(ErrorCode.ERR_RefReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(72, 33),
    // (75,24): error CS8162: Members of readonly field 'Test.i2' cannot be returned by reference
    //             return ref i2.x;
    Diagnostic(ErrorCode.ERR_RefReturnReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(75, 24),
    // (83,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s1;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(83, 33),
    // (86,24): error CS8161: A static readonly field cannot be returned by reference
    //             return ref s1;
    Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic, "s1").WithLocation(86, 24),
    // (90,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
    //             ref char temp = ref s2.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(90, 33),
    // (93,24): error CS8163: Fields of static readonly field 'Test.s2' cannot be returned by reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (10,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
    //             return ref this;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithArguments("this").WithLocation(10, 24),
    // (15,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
    //             return ref x;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithArguments("this").WithLocation(15, 24),
    // (20,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
    //             return ref this.x;
    Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this.x").WithArguments("this").WithLocation(20, 24),
    // (36,32): error CS8168: Cannot return local 'M1' by reference because it is not a ref local
    //             return ref Foo(ref M1);
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "M1").WithArguments("M1").WithLocation(36, 32),
    // (36,24): error CS8164: Cannot return by reference a result of 'Test.Foo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
    //             return ref Foo(ref M1);
    Diagnostic(ErrorCode.ERR_RefReturnCall, "Foo(ref M1)").WithArguments("Test.Foo<char>(ref char)", "arg").WithLocation(36, 24),
    // (41,32): error CS8169: Cannot return a member of local 'M2' by reference because it is not a ref local
    //             return ref Foo(ref M2.x);
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "M2").WithArguments("M2").WithLocation(41, 32),
    // (41,24): error CS8164: Cannot return by reference a result of 'Test.Foo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
    //             return ref Foo(ref M2.x);
    Diagnostic(ErrorCode.ERR_RefReturnCall, "Foo(ref M2.x)").WithArguments("Test.Foo<char>(ref char)", "arg").WithLocation(41, 24),
    // (46,32): error CS8168: Cannot return local 'M2' by reference because it is not a ref local
    //             return ref Foo(ref M2).x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "M2").WithArguments("M2").WithLocation(46, 32),
    // (46,24): error CS8165: Cannot return by reference a member of result of 'Test.Foo<Test.S1>(ref Test.S1)' because the argument passed to parameter 'arg' cannot be returned by reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(18, 24),
    // (28,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(28, 24),
    // (38,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(38, 24),
    // (47,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(47, 24),
    // (56,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(56, 24),
    // (65,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
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
            var comp = CreateCompilationRef(text);
            comp.VerifyDiagnostics(
    // (19,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;   //1
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(19, 24),
    // (25,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;  //2
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(25, 24),
    // (34,24): error CS0103: The name 'r' does not exist in the current context
    //             return ref r;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(34, 24),
    // (43,24): error CS8157: Cannot return 'valid' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref valid; //4
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "valid").WithArguments("valid").WithLocation(43, 24),
    // (52,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
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

        [Fact]
        public void RefReturnNested()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        ref char Foo(ref char a, ref char b)
        {
            // valid
            return ref a;
        }
        
        char Foo1(ref char a, ref char b)
        {
            return ref b;
        }

        ref char Foo2(ref char c, ref char b)
        {
            return c;
        }
    }
}";
            var options = TestOptions.Regular;
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: options);
            comp.VerifyDiagnostics(
                // (14,13): error CS8149: By-reference returns may only be used in methods that return by reference
                //             return ref b;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(14, 13),
                // (19,13): error CS8150: By-value returns may only be used in methods that return by value
                //             return c;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(19, 13),
                // (6,18): warning CS0168: The variable 'Foo' is declared but never used
                //         ref char Foo(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo").WithArguments("Foo").WithLocation(6, 18),
                // (12,14): warning CS0168: The variable 'Foo1' is declared but never used
                //         char Foo1(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo1").WithArguments("Foo1").WithLocation(12, 14),
                // (17,18): warning CS0168: The variable 'Foo2' is declared but never used
                //         ref char Foo2(ref char c, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo2").WithArguments("Foo2").WithLocation(17, 18));
        }

        [Fact]
        public void RefReturnNestedArrow()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        // valid
        ref char Foo(ref char a, ref char b) => ref a;
        
        char Foo1(ref char a, ref char b) => ref b;

        ref char Foo2(ref char c, ref char b) => c;

        var arr = new int[1];
        ref var r = ref arr[0];

        ref char Moo1(ref char a, ref char b) => ref r;
        char Moo3(ref char a, ref char b) => r;
    }
}";
            var options = TestOptions.Regular;
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: options);
            comp.VerifyDiagnostics(
                // (9,50): error CS8149: By-reference returns may only be used in methods that return by reference
                //         char Foo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "b").WithLocation(9, 50),
                // (11,50): error CS8150: By-value returns may only be used in methods that return by value
                //         ref char Foo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "c").WithLocation(11, 50),
                // (16,54): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(16, 54),
                // (16,54): error CS8151: The return expression must be of type 'char' because this method returns by reference
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "r").WithArguments("char").WithLocation(16, 54),
                // (17,46): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(17, 46),
                // (17,46): error CS0266: Cannot implicitly convert type 'int' to 'char'. An explicit conversion exists (are you missing a cast?)
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "r").WithArguments("int", "char").WithLocation(17, 46),
                // (7,18): warning CS0168: The variable 'Foo' is declared but never used
                //         ref char Foo(ref char a, ref char b) => ref a;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo").WithArguments("Foo").WithLocation(7, 18),
                // (9,14): warning CS0168: The variable 'Foo1' is declared but never used
                //         char Foo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo1").WithArguments("Foo1").WithLocation(9, 14),
                // (11,18): warning CS0168: The variable 'Foo2' is declared but never used
                //         ref char Foo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo2").WithArguments("Foo2").WithLocation(11, 18),
                // (16,18): warning CS0168: The variable 'Moo1' is declared but never used
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Moo1").WithArguments("Moo1").WithLocation(16, 18),
                // (17,14): warning CS0168: The variable 'Moo3' is declared but never used
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Moo3").WithArguments("Moo3").WithLocation(17, 14));
        }

        [Fact, WorkItem(13062, "https://github.com/dotnet/roslyn/issues/13062")]
        public void NoRefInIndex()
        {
            var text = @"
class C
{
    void F(object[] a, object[,] a2, int i)
    {
        int j;
        j = a[ref i];    // error 1
        j = a[out i];    // error 2
        j = this[ref i]; // error 3
        j = a2[i, out i]; // error 4
        j = a2[i, ref i]; // error 5
        j = a2[ref i, out i]; // error 6
    }
    public int this[int i] => 1;
}
";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (7,19): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = a[ref i];    // error 1
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(7, 19),
                // (8,19): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         j = a[out i];    // error 2
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "out").WithLocation(8, 19),
                // (9,22): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = this[ref i]; // error 3
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(9, 22),
                // (10,23): error CS1615: Argument 2 may not be passed with the 'out' keyword
                //         j = a2[i, out i]; // error 4
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("2", "out").WithLocation(10, 23),
                // (11,23): error CS1615: Argument 2 may not be passed with the 'ref' keyword
                //         j = a2[i, ref i]; // error 5
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("2", "ref").WithLocation(11, 23),
                // (12,20): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = a2[ref i, out i]; // error 6
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(12, 20)
                );
        }
    }
}
