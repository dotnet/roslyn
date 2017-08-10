// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;

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
    // (46,24): error CS8167: Cannot return a member of parameter 'arg2' by reference because it is not a ref or out parameter
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
                // (18,24): error CS1657: Cannot use 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             return ref ro;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(18, 24),
                // (23,30): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             ref char r = ref ro.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(23, 30),
                // (28,24): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             return ref ro.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(28, 24)
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
                // (68,24): error CS8160: A readonly field cannot be returned by writeable reference
                //             return ref i1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i1").WithLocation(68, 24),
                // (72,33): error CS1649: Members of readonly field 'Test.i2' cannot be used as a ref or out value (except in a constructor)
                //             ref char temp = ref i2.x;
                Diagnostic(ErrorCode.ERR_RefReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(72, 33),
                // (75,24): error CS8162: Members of readonly field 'Test.i2' cannot be returned by writeable reference
                //             return ref i2.x;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(75, 24),
                // (83,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s1;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(83, 33),
                // (86,24): error CS8161: A static readonly field cannot be returned by writeable reference
                //             return ref s1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic, "s1").WithLocation(86, 24),
                // (90,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s2.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(90, 33),
                // (93,24): error CS8163: Fields of static readonly field 'Test.s2' cannot be returned by writeable reference
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

        public ref S1 GooS()
        {
            return ref this;
        }        

        public ref char Goo()
        {
            return ref x;
        }

        public ref char Goo1()
        {
            return ref this.x;
        }
    }

    static ref T Goo<T>(ref T arg)
    {
        return ref arg;
    }

    static ref char Test1()
    {
        char M1 = default(char);
        S1   M2 = default(S1);

        if (1.ToString() != null)
        {
            return ref Goo(ref M1);
        }
        
        if (2.ToString() != null)
        {
            return ref Goo(ref M2.x);
        }

        if (3.ToString() != null)
        {
            return ref Goo(ref M2).x;
        }
        else
        {
            return ref M2.Goo();
        }
    }
  
    public class C
    {
        public ref C M()
        {
            return ref this;
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
                //             return ref Goo(ref M1);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "M1").WithArguments("M1").WithLocation(36, 32),
                // (36,24): error CS8164: Cannot return by reference a result of 'Test.Goo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
                //             return ref Goo(ref M1);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "Goo(ref M1)").WithArguments("Test.Goo<char>(ref char)", "arg").WithLocation(36, 24),
                // (41,32): error CS8169: Cannot return a member of local 'M2' by reference because it is not a ref local
                //             return ref Goo(ref M2.x);
                Diagnostic(ErrorCode.ERR_RefReturnLocal2, "M2").WithArguments("M2").WithLocation(41, 32),
                // (41,24): error CS8164: Cannot return by reference a result of 'Test.Goo<char>(ref char)' because the argument passed to parameter 'arg' cannot be returned by reference
                //             return ref Goo(ref M2.x);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "Goo(ref M2.x)").WithArguments("Test.Goo<char>(ref char)", "arg").WithLocation(41, 24),
                // (46,32): error CS8168: Cannot return local 'M2' by reference because it is not a ref local
                //             return ref Goo(ref M2).x;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "M2").WithArguments("M2").WithLocation(46, 32),
                // (46,24): error CS8165: Cannot return by reference a member of result of 'Test.Goo<Test.S1>(ref Test.S1)' because the argument passed to parameter 'arg' cannot be returned by reference
                //             return ref Goo(ref M2).x;
                Diagnostic(ErrorCode.ERR_RefReturnCall2, "Goo(ref M2)").WithArguments("Test.Goo<Test.S1>(ref Test.S1)", "arg").WithLocation(46, 24),
                // (58,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //             return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithArguments("this").WithLocation(58, 24)
                );
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

    ref char Goo(ref char a, ref char b)
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
            ref char invalid = ref Goo(ref a, ref a);

            // valid
            return ref r;
        }

        if (4.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char valid = ref Goo(ref a, ref arg1);

            // valid
            return ref valid; //4
        }

        if (5.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char r = ref Goo(ref a, ref r);

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
        ref char Goo(ref char a, ref char b)
        {
            // valid
            return ref a;
        }
        
        char Goo1(ref char a, ref char b)
        {
            return ref b;
        }

        ref char Goo2(ref char c, ref char b)
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
                // (6,18): warning CS8321: The local function 'Goo' is declared but never used
                //         ref char Goo(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo").WithArguments("Goo").WithLocation(6, 18),
                // (12,14): warning CS8321: The local function 'Goo1' is declared but never used
                //         char Goo1(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo1").WithArguments("Goo1").WithLocation(12, 14),
                // (17,18): warning CS8321: The local function 'Goo2' is declared but never used
                //         ref char Goo2(ref char c, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo2").WithArguments("Goo2").WithLocation(17, 18));
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
        ref char Goo(ref char a, ref char b) => ref a;
        
        char Goo1(ref char a, ref char b) => ref b;

        ref char Goo2(ref char c, ref char b) => c;

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
                //         char Goo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "b").WithLocation(9, 50),
                // (11,50): error CS8150: By-value returns may only be used in methods that return by value
                //         ref char Goo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "c").WithLocation(11, 50),
                // (16,54): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(16, 54),
                // (17,46): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(17, 46),
                // (17,46): error CS0266: Cannot implicitly convert type 'int' to 'char'. An explicit conversion exists (are you missing a cast?)
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "r").WithArguments("int", "char").WithLocation(17, 46),
                // (7,18): warning CS8321: The local function 'Goo' is declared but never used
                //         ref char Goo(ref char a, ref char b) => ref a;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo").WithArguments("Goo").WithLocation(7, 18),
                // (9,14): warning CS8321: The local function 'Goo1' is declared but never used
                //         char Goo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo1").WithArguments("Goo1").WithLocation(9, 14),
                // (11,18): warning CS8321: The local function 'Goo2' is declared but never used
                //         ref char Goo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo2").WithArguments("Goo2").WithLocation(11, 18),
                // (16,18): warning CS8321: The local function 'Moo1' is declared but never used
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Moo1").WithArguments("Moo1").WithLocation(16, 18),
                // (17,14): warning CS8321: The local function 'Moo3' is declared but never used
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Moo3").WithArguments("Moo3").WithLocation(17, 14)
                );
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

        [Fact, WorkItem(14174, "https://github.com/dotnet/roslyn/issues/14174")]
        public void RefDynamicBinding()
        {
            var text = @"
class C
{
    static object[] arr = new object[] { ""f"" };
    static void Main(string[] args)
    {
        System.Console.Write(arr[0].ToString());

        RefParam(ref arr[0]);
        System.Console.Write(arr[0].ToString());

        ref dynamic x = ref arr[0];
        x = ""o"";
        System.Console.Write(arr[0].ToString());

        RefReturn() = ""g"";
        System.Console.Write(arr[0].ToString());
    }

    static void RefParam(ref dynamic p)
    {
        p = ""r"";
    }

    static ref dynamic RefReturn()
    {
        return ref arr[0];
    }
}
";
            CompileAndVerify(text,
                expectedOutput: "frog",
                additionalRefs: new[] { SystemCoreRef, CSharpRef }).VerifyDiagnostics();
        }

        [Fact]
        public void RefQueryClause()
        {
            // a "ref" may not precede the expression of a query clause...
            // simply because the grammar doesn't permit it. Here we check
            // that the situation is diagnosed, either syntactically or semantically.
            // The precise diagnostics are not important for the purposes of this test.
            var text = @"
class C
{
    static void Main(string[] args)
    {
        var a = new[] { 1, 2, 3, 4 };
        bool b = true;
        int i = 0;
        { var za = from x in a select ref x; } // error 1
        { var zc = from x in a from y in ref a select x; } // error2
        { var zd = from x in a from int y in ref a select x; } // error 3
        { var ze = from x in a from y in ref a where true select x; } // error 4
        { var zf = from x in a from int y in ref a where true select x; } // error 5
        { var zg = from x in a let y = ref a select x; } // error 6
        { var zh = from x in a where ref b select x; } // error 7
        { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
        { var zj = from x in a join y in a on ref i equals y select x; } // error 9
        { var zk = from x in a join y in a on x equals ref i select x; } // error 10
        { var zl = from x in a orderby ref i select x; } // error 11
        { var zm = from x in a orderby x, ref i select x; } // error 12
        { var zn = from x in a group ref i by x; } // error 13
        { var zo = from x in a group x by ref i; } // error 14
    }
    public static T M<T>(T x, out T z) => z = x;

    public C Select(RefFunc<C, C> c1) => this;
    public C SelectMany(RefFunc<C, C> c1, RefFunc<C, C, C> c2) => this;
    public C Cast<T>() => this;
}
public delegate ref TR RefFunc<T1, TR>(T1 t1);
public delegate ref TR RefFunc<T1, T2, TR>(T1 t1, T2 t2);
";
            CreateCompilationWithMscorlibAndSystemCore(text)
                .GetDiagnostics()
                // It turns out each of them is diagnosed with ErrorCode.ERR_InvalidExprTerm in the midst
                // of a flurry of other syntax errors.
                .Where(d => d.Code == (int)ErrorCode.ERR_InvalidExprTerm)
                .Verify(
                // (9,39): error CS1525: Invalid expression term 'ref'
                //         { var za = from x in a select ref x; } // error 1
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(9, 39),
                // (10,42): error CS1525: Invalid expression term 'ref'
                //         { var zc = from x in a from y in ref a select x; } // error2
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(10, 42),
                // (11,46): error CS1525: Invalid expression term 'ref'
                //         { var zd = from x in a from int y in ref a select x; } // error 3
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(11, 46),
                // (12,42): error CS1525: Invalid expression term 'ref'
                //         { var ze = from x in a from y in ref a where true select x; } // error 4
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(12, 42),
                // (13,46): error CS1525: Invalid expression term 'ref'
                //         { var zf = from x in a from int y in ref a where true select x; } // error 5
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(13, 46),
                // (14,40): error CS1525: Invalid expression term 'ref'
                //         { var zg = from x in a let y = ref a select x; } // error 6
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(14, 40),
                // (15,38): error CS1525: Invalid expression term 'ref'
                //         { var zh = from x in a where ref b select x; } // error 7
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(15, 38),
                // (16,42): error CS1525: Invalid expression term 'ref'
                //         { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(16, 42),
                // (16,42): error CS1525: Invalid expression term 'ref'
                //         { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(16, 42),
                // (16,42): error CS1525: Invalid expression term 'ref'
                //         { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(16, 42),
                // (17,47): error CS1525: Invalid expression term 'ref'
                //         { var zj = from x in a join y in a on ref i equals y select x; } // error 9
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(17, 47),
                // (17,47): error CS1525: Invalid expression term 'ref'
                //         { var zj = from x in a join y in a on ref i equals y select x; } // error 9
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(17, 47),
                // (18,56): error CS1525: Invalid expression term 'ref'
                //         { var zk = from x in a join y in a on x equals ref i select x; } // error 10
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(18, 56),
                // (19,40): error CS1525: Invalid expression term 'ref'
                //         { var zl = from x in a orderby ref i select x; } // error 11
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(19, 40),
                // (20,43): error CS1525: Invalid expression term 'ref'
                //         { var zm = from x in a orderby x, ref i select x; } // error 12
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(20, 43),
                // (21,38): error CS1525: Invalid expression term 'ref'
                //         { var zn = from x in a group ref i by x; } // error 13
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(21, 38),
                // (21,38): error CS1525: Invalid expression term 'ref'
                //         { var zn = from x in a group ref i by x; } // error 13
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(21, 38),
                // (22,43): error CS1525: Invalid expression term 'ref'
                //         { var zo = from x in a group x by ref i; } // error 14
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(22, 43)
                );
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseYieldReturnInAReturnByRefFunction()
        {
            var code = @"
class TestClass
{
    int x = 0;
    ref int TestFunction()
    {
        yield return x;

        ref int localFunction()
        {
            yield return x;
        }

        yield return localFunction();
    }
}";

            CreateStandardCompilation(code).VerifyDiagnostics(
                // (9,17): error CS8154: The body of 'localFunction()' cannot be an iterator block because 'localFunction()' returns by reference
                //         ref int localFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "localFunction").WithArguments("localFunction()").WithLocation(9, 17),
                // (5,13): error CS8154: The body of 'TestClass.TestFunction()' cannot be an iterator block because 'TestClass.TestFunction()' returns by reference
                //     ref int TestFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "TestFunction").WithArguments("TestClass.TestFunction()").WithLocation(5, 13));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseRefReturnInExpressionTree_ParenthesizedLambdaExpression()
        {
            var code = @"
using System.Linq.Expressions;
class TestClass
{
    int x = 0;

    delegate ref int RefReturnIntDelegate(int y);

    void TestFunction()
    {
        Expression<RefReturnIntDelegate> lambda = (y) => ref x;
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (11,51): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<RefReturnIntDelegate> lambda = (y) => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(y) => ref x").WithLocation(11, 51));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseRefReturnInExpressionTree_SimpleLambdaExpression()
        {
            var code = @"
using System.Linq.Expressions;
class TestClass
{
    int x = 0;

    delegate ref int RefReturnIntDelegate(int y);

    void TestFunction()
    {
        Expression<RefReturnIntDelegate> lambda = y => ref x;
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (11,51): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<RefReturnIntDelegate> lambda = y => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "y => ref x").WithLocation(11, 51));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotCallExpressionThatReturnsByRefInExpressionTree()
        {
            var code = @"
using System;
using System.Linq.Expressions;
namespace TestRefReturns
{
    class TestClass
    {
        int x = 0;

        ref int RefReturnFunction()
        {
            return ref x;
        }

        ref int RefReturnProperty
        {
            get { return ref x; }
        }

        ref int this[int y]
        {
            get { return ref x; }
        }

        int TakeRefFunction(ref int y)
        {
            return y;
        }

        void TestFunction()
        {
            Expression<Func<int>> lambda1 = () => TakeRefFunction(ref RefReturnFunction());
            Expression<Func<int>> lambda2 = () => TakeRefFunction(ref RefReturnProperty);
            Expression<Func<int>> lambda3 = () => TakeRefFunction(ref this[0]);
        }
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (32,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda1 = () => TakeRefFunction(ref RefReturnFunction());
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "RefReturnFunction()").WithLocation(32, 71),
                // (33,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda2 = () => TakeRefFunction(ref RefReturnProperty);
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "RefReturnProperty").WithLocation(33, 71),
                // (34,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda3 = () => TakeRefFunction(ref this[0]);
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "this[0]").WithLocation(34, 71));
        }

        [WorkItem(19930, "https://github.com/dotnet/roslyn/issues/19930")]
        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotRefReturnQueryRangeVariable()
        {
            var code = @"
using System.Linq;
class TestClass
{
    delegate ref char RefCharDelegate();
    void TestMethod()
    {
        var x = from c in ""TestValue"" select (RefCharDelegate)(() => ref c);
    }

    delegate ref readonly char RoRefCharDelegate();
    void TestMethod1()
    {
        var x = from c in ""TestValue"" select (RoRefCharDelegate)(() => ref c);
    }
}";

            CreateCompilationWithMscorlibAndSystemCore(code).VerifyDiagnostics(
                // (8,74): error CS8159: Cannot return the range variable 'c' by reference
                //         var x = from c in "TestValue" select (RefCharDelegate)(() => ref c);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "c").WithArguments("c").WithLocation(8, 74),
                // (14,76): error CS8159: Cannot return the range variable 'c' by reference
                //         var x = from c in "TestValue" select (RoRefCharDelegate)(() => ref c);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "c").WithArguments("c").WithLocation(14, 76)
                );
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotAssignRefInNonIdentityConversion()
        {
            var code = @"
using System;
using System.Collections.Generic;

class TestClass
{
    int intVar = 0;
    string stringVar = ""TEST"";

    void TestMethod()
    {
        ref int? nullableConversion = ref intVar;
        ref dynamic dynamicConversion = ref intVar;
        ref IEnumerable<char> enumerableConversion = ref stringVar;
        ref IFormattable interpolatedStringConversion = ref stringVar;
    }
}";

            CreateStandardCompilation(code).VerifyDiagnostics(
                // (12,43): error CS8173: The expression must be of type 'int?' because it is being assigned by reference
                //         ref int? nullableConversion = ref intVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "intVar").WithArguments("int?").WithLocation(12, 43),
                // (13,45): error CS8173: The expression must be of type 'dynamic' because it is being assigned by reference
                //         ref dynamic dynamicConversion = ref intVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "intVar").WithArguments("dynamic").WithLocation(13, 45),
                // (14,58): error CS8173: The expression must be of type 'IEnumerable<char>' because it is being assigned by reference
                //         ref IEnumerable<char> enumerableConversion = ref stringVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "stringVar").WithArguments("System.Collections.Generic.IEnumerable<char>").WithLocation(14, 58),
                // (15,61): error CS8173: The expression must be of type 'IFormattable' because it is being assigned by reference
                //         ref IFormattable interpolatedStringConversion = ref stringVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "stringVar").WithArguments("System.IFormattable").WithLocation(15, 61));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void IteratorMethodsCannotHaveRefLocals()
        {
            var code = @"
using System.Collections.Generic;
class TestClass
{
    int x = 0;
    IEnumerable<int> TestMethod()
    {
        ref int y = ref x;
        yield return y;

        IEnumerable<int> localFunction()
        {
            ref int z = ref x;
            yield return z;
        }

        foreach(var item in localFunction())
        {
            yield return item;
        }
    }
}";

            CreateStandardCompilation(code).VerifyDiagnostics(
                // (13,21): error CS8176: Iterators cannot have by reference locals
                //             ref int z = ref x;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "z").WithLocation(13, 21),
                // (8,17): error CS8176: Iterators cannot have by reference locals
                //         ref int y = ref x;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "y").WithLocation(8, 17));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void AsyncMethodsCannotHaveRefLocals()
        {
            var code = @"
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    async Task TestMethod()
    {
        ref int y = ref x;
        await Task.Run(async () =>
        {
            ref int z = ref x;
            await Task.Delay(0);
        });
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyDiagnostics(
                // (8,17): error CS8177: Async methods cannot have by reference locals
                //         ref int y = ref x;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "y = ref x").WithLocation(8, 17),
                // (11,21): error CS8177: Async methods cannot have by reference locals
                //             ref int z = ref x;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "z = ref x").WithLocation(11, 21));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseAwaitExpressionInACallToAFunctionThatReturnsByRef()
        {
            var code = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    ref int Save(int y)
    {
        x = y;
        return ref x;
    }
    void Write(ref int y)
    {
        Console.WriteLine(y);
    }
    async Task TestMethod()
    {
        Write(ref Save(await Task.FromResult(0)));
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyEmitDiagnostics(
                // (18,24): error CS8178: 'await' cannot be used in an expression containing a call to 'TestClass.Save(int)' because it returns by reference
                //         Write(ref Save(await Task.FromResult(0)));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Task.FromResult(0)").WithArguments("TestClass.Save(int)").WithLocation(18, 24));
        }

        [Fact]
        public void BadRefAssignByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static void M()
    {
        ref int rl = ref P;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         ref int rl = ref P;
                Diagnostic(ErrorCode.ERR_RefProperty, "P").WithArguments("Program.P").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    void M()
    {
        ref int rl = ref this[0];
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         ref int rl = ref this[0];
                Diagnostic(ErrorCode.ERR_RefProperty, "this[0]").WithArguments("Program.this[int]").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    void M()
    {
        ref int rl = ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,26): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         ref int rl = ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 26));
        }

        [Fact]
        public void BadRefAssignReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    void M()
    {
        ref int rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReadonly, "i").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    ref int M()
    {
        ref int rl = ref i;
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (14,20): error CS8157: Cannot return 'rl' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref rl;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "rl").WithArguments("rl").WithLocation(14, 20)
            );
        }

        [Fact]
        public void BadRefAssignByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static void M()
    {
        ref int rl = ref L();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (11,26): error CS1510: A ref or out value must be an assignable variable
                //         ref int rl = ref L();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "L()").WithLocation(11, 26)
            );
        }

        [Fact]
        public void BadRefAssignByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static void M(D d)
    {
        ref int rl = ref d();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS1510: A ref or out value must be an assignable variable
                //         ref int rl = ref d();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "d()").WithLocation(8, 26)
            );
        }

        [Fact]
        public void BadRefAssignCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        ref int rl = ref M(ref j);
        return ref rl;
    }
}
";


            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8157: Cannot return 'rl' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref rl;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "rl").WithArguments("rl").WithLocation(8, 20)
            );
        }

        [Fact]
        public void BadRefAssignThisReference()
        {
            var text = @"
class Program
{
    void M()
    {
        ref int rl = ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,26): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         ref int rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(6, 26)
            );
        }

        [Fact]
        public void BadRefAssignWrongType()
        {
            var text = @"
class Program
{
    void M(ref long i)
    {
        ref int rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,26): error CS8173: The expression must be of type 'int' because it is being assigned by reference
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 26)
            );
        }

        [Fact]
        public void BadRefLocalCapturedInAnonymousMethod()
        {
            var text = @"
using System.Linq;

delegate int D();

class Program
{
    static int field = 0;

    static void M()
    {
        ref int rl = ref field;
        var d = new D(delegate { return rl; });
        d = new D(() => rl);
        rl = (from v in new int[10] where v > rl select v).Single();
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (13,41): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         var d = new D(delegate { return rl; });
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(13, 41),
                // (14,25): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         d = new D(() => rl);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(14, 25),
                // (15,47): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         rl = (from v in new int[10] where v > rl select r1).Single();
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(15, 47));
        }

        [Fact]
        public void BadRefLocalInAsyncMethod()
        {
            var text = @"
class Program
{
    static int field = 0;

    static async void Goo()
    {
        ref int i = ref field;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,17): error CS8932: Async methods cannot have by reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "i = ref field").WithLocation(8, 17),
                // (6,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async void Goo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Goo").WithLocation(6, 23));
        }

        [Fact]
        public void BadRefLocalInIteratorMethod()
        {
            var text = @"
using System.Collections;

class Program
{
    static int field = 0;

    static IEnumerable ObjEnumerable()
    {
        ref int i = ref field;
        yield return new object();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,17): error CS8931: Iterators cannot have by reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "i").WithLocation(10, 17));
        }

        [Fact]
        public void BadRefAssignByValueLocal()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        int l = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,13): error CS8922: Cannot initialize a by-value variable with a reference
                //         int l = ref i;
                Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "l = ref i").WithLocation(6, 13)
               );
        }

        [Fact]
        public void BadByValueInitRefLocal()
        {
            var text = @"
class Program
{
    static void M(int i)
    {
        ref int rl = i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,17): error CS8921: Cannot initialize a by-reference variable with a value
                //         ref int rl = i;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "rl = i").WithLocation(6, 17));
        }

        [Fact]
        public void BadRefReturnParameter()
        {
            var text = @"
class Program
{
    static ref int M(int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8911: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnLocal()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        int i = 0;
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,20): error CS8913: Cannot return or assign a reference to local 'i' because it is not a ref local
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(7, 20));
        }

        [Fact]
        public void BadRefReturnByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static ref int M()
    {
        return ref P;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref P;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P").WithArguments("Program.P").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    ref int M()
    {
        return ref this[0];
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref this[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "this[0]").WithArguments("Program.this[int]").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    ref int M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,20): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         return ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 20));
        }

        [Fact]
        public void BadRefReturnEventReceiver()
        {
            var text = @"
delegate void D();

struct Program
{
    event D d;

    ref D M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref d;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "d").WithArguments("this").WithLocation(10, 20)
            );
        }

        [Fact]
        public void BadRefReturnReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReadonly, "i").WithLocation(8, 20)
            );
        }

        [Fact]
        public void BadRefReturnFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (13,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "i").WithArguments("this").WithLocation(13, 20)
            );
        }

        [Fact]
        public void BadRefReturnByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static ref int M()
    {
        return ref L();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (11,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref L();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "L()").WithLocation(11, 20));
        }

        [Fact]
        public void BadRefReturnByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static ref int M(D d)
    {
        return ref d();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref d();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d()").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, int i, int j, object o)
    {
        return ref d(ref i, ref j, o);
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(8, 26),
                // (8,20): error CS8910: Cannot return or assign a reference to the result of 'D.Invoke(ref int, ref int, object)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "d(ref i, ref j, o)").WithArguments("D.Invoke(ref int, ref int, object)", "i").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        return ref M(ref j);
    }
}
";


            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,26): error CS8914: Cannot return or assign a reference to local 'j' because it is not a ref local
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "j").WithArguments("j").WithLocation(7, 26),
                // (7,20): error CS8910: Cannot return or assign a reference to the result of 'Program.M(ref int)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref j)").WithArguments("Program.M(ref int)", "i").WithLocation(7, 20));
        }

        [Fact]
        public void BadRefReturnStructThis()
        {
            var text = @"
struct Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithArguments("this").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnThisReference()
        {
            var text = @"
class Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithArguments("this").WithLocation(6, 20)
            );
        }

        [Fact]
        public void BadRefReturnWrongType()
        {
            var text = @"
class Program
{
    ref int M(ref long i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8085: The return expression must be of type 'int' because this method returns by reference.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 20));
        }

        [Fact]
        public void BadByRefReturnInByValueReturningMethod()
        {
            var text = @"
class Program
{
    static int M(ref int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8083: By-reference returns may only be used in by-reference returning methods.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadByValueReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8084: By-value returns may only be used in by-value returning methods.
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadEmptyReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8150: By-value returns may only be used in methods that return by value
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9),
                // (6,9): error CS0126: An object of a type convertible to 'int' is required
                //         return;
                Diagnostic(ErrorCode.ERR_RetObjectRequired, "return").WithArguments("int").WithLocation(6, 9)
            );
        }

        [Fact]
        public void BadIteratorReturnInRefReturningMethod()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

class C
{
    public ref IEnumerator ObjEnumerator()
    {
        yield return new object();
    }

    public ref IEnumerable ObjEnumerable()
    {
        yield return new object();
    }

    public ref IEnumerator<int> GenEnumerator()
    {
        yield return 0;
    }

    public ref IEnumerable<int> GenEnumerable()
    {
        yield return 0;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,28): error CS8089: The body of 'C.ObjEnumerator()' cannot be an iterator block because 'C.ObjEnumerator()' returns by reference
                //     public ref IEnumerator ObjEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerator").WithArguments("C.ObjEnumerator()").WithLocation(7, 28),
                // (12,28): error CS8089: The body of 'C.ObjEnumerable()' cannot be an iterator block because 'C.ObjEnumerable()' returns by reference
                //     public ref IEnumerable ObjEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerable").WithArguments("C.ObjEnumerable()").WithLocation(12, 28),
                // (17,33): error CS8089: The body of 'C.GenEnumerator()' cannot be an iterator block because 'C.GenEnumerator()' returns by reference
                //     public ref IEnumerator<int> GenEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerator").WithArguments("C.GenEnumerator()").WithLocation(17, 33),
                // (22,33): error CS8089: The body of 'C.GenEnumerable()' cannot be an iterator block because 'C.GenEnumerable()' returns by reference
                //     public ref IEnumerable<int> GenEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerable").WithArguments("C.GenEnumerable()").WithLocation(22, 33));
        }

        [Fact]
        public void BadRefReturnInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate ref int D();
delegate ref int E(int i);

class C
{
    static int field = 0;

    static void M()
    {
        Expression<D> d = () => ref field;
        Expression<E> e = (int i) => ref field;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (13,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<D> d = () => ref field;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "() => ref field").WithLocation(13, 27),
                // (14,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<E> e = (int i) => ref field;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(int i) => ref field").WithLocation(14, 27));
        }

        [Fact]
        public void BadRefReturningCallInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate int D(C c);

class C
{
    int field = 0;

    ref int P { get { return ref field; } }
    ref int this[int i] { get { return ref field; } }
    ref int M() { return ref field; }

    static void M1()
    {
        Expression<D> e = c => c.P;
        e = c => c[0];
        e = c => c.M();
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (16,32): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         Expression<D> e = c => c.P;
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.P").WithLocation(16, 32),
                // (17,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         e = c => c[0];
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c[0]").WithLocation(17, 18),
                // (18,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         e = c => c.M();
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.M()").WithLocation(18, 18));
        }

        [Fact]
        public void BadRefReturningCallWithAwait()
        {
            var text = @"
using System.Threading.Tasks;

struct S
{
    static S s = new S();

    public static ref S Instance { get { return ref s; } }

    public int Echo(int i)
    {
        return i;
    }
}

class C
{
    ref int Assign(ref int loc, int val)
    {
        loc = val;
        return ref loc;
    }

    public async Task<int> Do(int i)
    {
        if (i == 0)
        {
            return 0;
        }

        int temp = 0;
        var a = S.Instance.Echo(await Do(i - 1));
        var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
        return a + b;
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyEmitDiagnostics(
                // (32,33): error CS8933: 'await' cannot be used in an expression containing a call to 'S.Instance.get' because it returns by reference
                //         var a = S.Instance.Echo(await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Do(i - 1)").WithArguments("S.Instance.get").WithLocation(32, 33),
                // (33,49): error CS8933: 'await' cannot be used in an expression containing a call to 'C.Assign(ref int, int)' because it returns by reference
                //         var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Do(i - 1)").WithArguments("C.Assign(ref int, int)").WithLocation(33, 49));
        }

        [Fact]
        public void CannotUseAwaitExpressionToAssignRefReturing()
        {
            var code = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    ref int Save(int y)
    {
        x = y;
        return ref x;
    }

    void Write(ref int y)
    {
        Console.WriteLine(y);
    }

    public int this[int arg]
    {
        get { return 1; }
        set { }
    }

    public ref int this[int arg, int arg2] => ref x;

    async Task TestMethod()
    {
        Save(1) = await Task.FromResult(0);

        var inst = new TestClass();

        // valid
        inst[1] = await Task.FromResult(1);

        // invalid
        inst[1, 2] = await Task.FromResult(1);
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyEmitDiagnostics(
                    // (28,19): error CS8178: 'await' cannot be used in an expression containing a call to 'TestClass.Save(int)' because it returns by reference
                    //         Save(1) = await Task.FromResult(0);
                    Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Task.FromResult(0)").WithArguments("TestClass.Save(int)").WithLocation(28, 19),
                    // (36,22): error CS8178: 'await' cannot be used in an expression containing a call to 'TestClass.this[int, int].get' because it returns by reference
                    //         inst[1, 2] = await Task.FromResult(1);
                    Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Task.FromResult(1)").WithArguments("TestClass.this[int, int].get").WithLocation(36, 22)
            );
        }
    }
}
