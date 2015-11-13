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
            var comp = CreateCompilationWithMscorlib(text);
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
            var comp = CreateCompilationWithMscorlib(text);
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
            var comp = CreateCompilationWithMscorlib(text);
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
            var comp = CreateCompilationWithMscorlib(text);
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
            var comp = CreateCompilationWithMscorlib(text);
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
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
    // (13,30): error CS1657: Cannot pass 'ro' as a ref or out argument because it is a 'foreach iteration variable'
    //             ref char r = ref ro;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(13, 30),
    // (18,24): error CS8924: Cannot return a reference to local 'ro' because it is not a ref local
    //             return ref ro;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "ro").WithArguments("ro").WithLocation(18, 24),
    // (23,30): error CS1655: Cannot pass fields of 'ro' as a ref or out argument because it is a 'foreach iteration variable'
    //             ref char r = ref ro.x;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(23, 30),
    // (28,24): error CS8925: Cannot return a reference to a member of local 'ro' because it is not a ref local
    //             return ref ro.x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "ro").WithArguments("ro").WithLocation(28, 24)
            );
        }

    }
}
