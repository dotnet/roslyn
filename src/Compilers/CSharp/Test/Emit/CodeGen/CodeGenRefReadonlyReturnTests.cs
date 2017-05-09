// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadonlyReferences)]
    public class CodeGenRefReadonlyReturnTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnArrayAccess()
        {
            var text = @"
class Program
{
    static ref readonly int M()
    {
        return ref (new int[1])[0];
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular);

            comp.VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  ret
}");
        }

        [Fact]
        public void BindingInvalidRefRoCombination()
        {
            var text = @"
class Program
{
    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly ref int M(int x)
    {
        return ref M(x);
    }

    // should be a syntax error
    // just make sure binder is ok with this
    static readonly int M1(int x)
    {
        return ref M(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS1031: Type expected
                //     static ref readonly ref int M(int x)
                Diagnostic(ErrorCode.ERR_TypeExpected, "ref").WithLocation(6, 25),
                // (13,25): error CS0106: The modifier 'readonly' is not valid for this item
                //     static readonly int M1(int x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M1").WithArguments("readonly").WithLocation(13, 25),
                // (15,20): error CS0120: An object reference is required for the non-static field, method, or property 'Program.M(int)'
                //         return ref M(x);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M").WithArguments("Program.M(int)").WithLocation(15, 20),
                // (15,9): error CS8149: By-reference returns may only be used in methods that return by reference
                //         return ref M(x);
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(15, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssign()
        {
            var text = @"
class Program
{
    static void Test()
    {
        M() = 1;
        M1().Alice = 2;

        M() ++;
        M1().Alice --;

        M() += 1;
        M1().Alice -= 2;
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8408: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(6, 9),
                // (7,9): error CS8409: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(7, 9),
                // (9,9): error CS8408: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(9, 9),
                // (10,9): error CS8409: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(10, 9),
                // (12,9): error CS8408: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(12, 9),
                // (13,9): error CS8409: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssign1()
        {
            var text = @"
class Program
{
    static void Test()
    {
        P = 1;
        P1.Alice = 2;

        P ++;
        P1.Alice --;

        P += 1;
        P1.Alice -= 2;
    }

    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8408: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(6, 9),
                // (7,9): error CS8409: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(7, 9),
                // (9,9): error CS8408: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(9, 9),
                // (10,9): error CS8409: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(10, 9),
                // (12,9): error CS8408: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(12, 9),
                // (13,9): error CS8409: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssignByref()
        {
            var text = @"
class Program
{
    static void Test()
    {
        ref var y = ref M();
        ref int a = ref M1.Alice;
        ref var y1 = ref P;
        ref int a1 = ref P1.Alice;
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8406: Cannot use method 'Program.M()' as a ref or out value because it is a readonly variable
                //         ref var y = ref M();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(6, 25),
                // (7,25): error CS0119: 'Program.M1()' is a method, which is not valid in the given context
                //         ref int a = ref M1.Alice;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "M1").WithArguments("Program.M1()", "method").WithLocation(7, 25),
                // (8,26): error CS8406: Cannot use property 'Program.P' as a ref or out value because it is a readonly variable
                //         ref var y1 = ref P;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(8, 26),
                // (9,26): error CS8407: Members of property 'Program.P1' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a1 = ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(9, 26)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotTakePtr()
        {
            var text = @"
class Program
{
    unsafe static void Test()
    {
        int* a = & M();
        int* b = & M1().Alice;

        int* a1 = & P;
        int* b2 = & P1.Alice;

        fixed(int* c = & M())
        {
        }

        fixed(int* d = & M1().Alice)
        {
        }

        fixed(int* c = & P)
        {
        }

        fixed(int* d = & P1.Alice)
        {
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;

}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,20): error CS0211: Cannot take the address of the given expression
                //         int* a = & M();
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M()").WithLocation(6, 20),
                // (7,20): error CS0211: Cannot take the address of the given expression
                //         int* b = & M1().Alice;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M1().Alice").WithLocation(7, 20),
                // (9,21): error CS0211: Cannot take the address of the given expression
                //         int* a1 = & P;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P").WithLocation(9, 21),
                // (10,21): error CS0211: Cannot take the address of the given expression
                //         int* b2 = & P1.Alice;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P1.Alice").WithLocation(10, 21),
                // (12,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* c = & M())
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M()").WithLocation(12, 26),
                // (16,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* d = & M1().Alice)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M1().Alice").WithLocation(16, 26),
                // (20,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* c = & P)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P").WithLocation(20, 26),
                // (24,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* d = & P1.Alice)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P1.Alice").WithLocation(24, 26)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotReturnByOrdinaryRef()
        {
            var text = @"
class Program
{
    static ref int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref M();
            }
            else
            {
                return ref M1().Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref P;
            }
            else
            {
                return ref P1.Alice;
            }        
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (12,28): error CS8406: Cannot use method 'Program.M()' as a ref or out value because it is a readonly variable
                //                 return ref M();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(12, 28),
                // (16,28): error CS8407: Members of method 'Program.M1()' cannot be used as a ref or out value because it is a readonly variable
                //                 return ref M1().Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(16, 28),
                // (23,28): error CS8406: Cannot use property 'Program.P' as a ref or out value because it is a readonly variable
                //                 return ref P;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(23, 28),
                // (27,28): error CS8407: Members of property 'Program.P1' cannot be used as a ref or out value because it is a readonly variable
                //                 return ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(27, 28)
            );
        }

        [Fact]
        public void ReadonlyReturnCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    static ref readonly int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref M();
            }
            else
            {
                return ref M1().Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref P;
            }
            else
            {
                return ref P1.Alice;
            }        
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.Test", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (bool V_0) //b
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_0019
  IL_0005:  ldloc.0
  IL_0006:  brfalse.s  IL_000e
  IL_0008:  call       ""ref readonly int Program.M()""
  IL_000d:  ret
  IL_000e:  call       ""ref readonly (int Alice, int Bob) Program.M1()""
  IL_0013:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0018:  ret
  IL_0019:  ldloc.0
  IL_001a:  brfalse.s  IL_0022
  IL_001c:  call       ""ref readonly int Program.P.get""
  IL_0021:  ret
  IL_0022:  call       ""ref readonly (int Alice, int Bob) Program.P1.get""
  IL_0027:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_002c:  ret
}");
        }

        [Fact]
        public void ReadonlyFieldCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref F;
            }
            else
            {
                return ref F1.Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref S1.F;
            }
            else
            {
                return ref S2.F1.Alice;
            }        
        }
    }

    readonly int F = 1;
    static readonly (int Alice, int Bob) F1 = (2,3);

    readonly S S1 = new S();
    static readonly S S2 = new S();

    struct S
    {
        public readonly int F;
        public readonly (int Alice, int Bob) F1;
    }
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.Test", @"
{
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (bool V_0) //b
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_001a
  IL_0005:  ldloc.0
  IL_0006:  brfalse.s  IL_000f
  IL_0008:  ldarg.0
  IL_0009:  ldflda     ""int Program.F""
  IL_000e:  ret
  IL_000f:  ldsflda    ""(int Alice, int Bob) Program.F1""
  IL_0014:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0019:  ret
  IL_001a:  ldloc.0
  IL_001b:  brfalse.s  IL_0029
  IL_001d:  ldarg.0
  IL_001e:  ldflda     ""Program.S Program.S1""
  IL_0023:  ldflda     ""int Program.S.F""
  IL_0028:  ret
  IL_0029:  ldsflda    ""Program.S Program.S2""
  IL_002e:  ldflda     ""(int Alice, int Bob) Program.S.F1""
  IL_0033:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0038:  ret
}");
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyLocalSafety()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        bool b = true;
        int local = 42;

        if (b)
        {
            return ref M(ref local);
        }
        else
        {
            return ref M1(out local).Alice;
        }        
    }

    static ref readonly int M(ref int x) => throw null;
    static ref readonly (int Alice, int Bob) M1(out int x) => throw null;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (11,30): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(11, 30),
                // (11,24): error CS8164: Cannot return by reference a result of 'Program.M(ref int)' because the argument passed to parameter 'x' cannot be returned by reference
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref local)").WithArguments("Program.M(ref int)", "x").WithLocation(11, 24),
                // (15,31): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(15, 31),
                // (15,24): error CS8165: Cannot return by reference a member of result of 'Program.M1(out int)' because the argument passed to parameter 'x' cannot be returned by reference
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_RefReturnCall2, "M1(out local)").WithArguments("Program.M1(out int)", "x").WithLocation(15, 24)
            );
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyLocalSafety1()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        int local = 42;

        return ref this[local];
    }

    ref readonly int this[in int x] => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,25): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //         return ref this[local];
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(8, 25),
                // (8,24): error CS8164: Cannot return by reference a result of 'Program.this[in int]' because the argument passed to parameter 'x' cannot be returned by reference
                //         return ref this[local];
                Diagnostic(ErrorCode.ERR_RefReturnCall, "[local]").WithArguments("Program.this[in int]", "x").WithLocation(8, 24)
            );
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyLiteralSafety1()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        return ref this[42];
    }

    ref readonly int this[in int x] => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref this[42];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(6, 25),
                // (6,24): error CS8164: Cannot return by reference a result of 'Program.this[in int]' because the argument passed to parameter 'x' cannot be returned by reference
                //         return ref this[42];
                Diagnostic(ErrorCode.ERR_RefReturnCall, "[42]").WithArguments("Program.this[in int]", "x").WithLocation(6, 24)
            );
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyLiteralSafety2()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        return ref M(42);
    }

    ref readonly int M(in int x) => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,22): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref M(42);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(6, 22),
                // (6,20): error CS8164: Cannot return by reference a result of 'Program.M(in int)' because the argument passed to parameter 'x' cannot be returned by reference
                //         return ref M(42);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(42)").WithArguments("Program.M(in int)", "x").WithLocation(6, 20)
            );
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyOptSafety()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        return ref M();
    }

    ref readonly int M(in int x = 42) => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,20): error CS8164: Cannot return by reference a result of 'Program.M(in int)' because the argument passed to parameter 'x' cannot be returned by reference
                //         return ref M();
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M()").WithArguments("Program.M(in int)", "x").WithLocation(6, 20)
            );
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyConvSafety()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        byte b = 42;
        return ref M(b);
    }

    ref readonly int M(in int x) => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (7,22): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref M(b);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "b").WithLocation(7, 22),
                // (7,20): error CS8164: Cannot return by reference a result of 'Program.M(in int)' because the argument passed to parameter 'x' cannot be returned by reference
                //         return ref M(b);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(b)").WithArguments("Program.M(in int)", "x").WithLocation(7, 20)
            );
        }
    }
}
