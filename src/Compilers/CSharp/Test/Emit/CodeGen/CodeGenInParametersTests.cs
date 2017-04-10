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
    public class CodeGenInParametersTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnParamAccess()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M(in int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void InParamPassLValue()
        {
            var text = @"
struct Program
{
    public static void Main()
    {
        var local = 42;
        System.Console.WriteLine(M(local));

        S1 s1 = default(S1);
        s1.X = 42;

        s1 += s1;

        System.Console.WriteLine(s1.X);
    }

    static ref readonly int M(in int x)
    {
        return ref x;
    }


    struct S1
    {
        public int X;

        public static S1 operator +(in S1 x, in S1 y)
        {
            return new S1(){X = x.X + y.X};
        }
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"42
84");

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (int V_0, //local
                Program.S1 V_1) //s1
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""ref readonly int Program.M(in int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""Program.S1""
  IL_0018:  ldloca.s   V_1
  IL_001a:  ldc.i4.s   42
  IL_001c:  stfld      ""int Program.S1.X""
  IL_0021:  ldloca.s   V_1
  IL_0023:  ldloca.s   V_1
  IL_0025:  call       ""Program.S1 Program.S1.op_Addition(in Program.S1, in Program.S1)""
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldfld      ""int Program.S1.X""
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void InParamPassRValue()
        {
            var text = @"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(M(42));
        System.Console.WriteLine(new Program()[5, 6]);
    }

    static ref readonly int M(in int x)
    {
        return ref x;
    }

    int this[in int x, in int y] => x + y;
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"42
11");

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       40 (0x28)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""ref readonly int Program.M(in int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  newobj     ""Program..ctor()""
  IL_0015:  ldc.i4.5
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.6
  IL_001a:  stloc.2
  IL_001b:  ldloca.s   V_2
  IL_001d:  call       ""int Program.this[in int, in int].get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void InParamPassRoField()
        {
            var text = @"
class Program
{
    public static readonly int F = 42;

    public static void Main()
    {
        System.Console.WriteLine(M(F));
    }

    static ref readonly int M(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: "42");

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldsflda    ""int Program.F""
  IL_0005:  call       ""ref readonly int Program.M(in int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ret
}");


            comp.VerifyIL("Program.M(in int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void InParamPassRoParamReturn()
        {
            var text = @"
class Program
{
    public static readonly int F = 42;

    public static void Main()
    {
        System.Console.WriteLine(M(F));
    }

    static ref readonly int M(in int x)
    {
        return ref M1(x);
    }

    static ref readonly int M1(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: "42");

            comp.VerifyIL("Program.M(in int)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref readonly int Program.M1(in int)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void InParamBase()
        {
            var text = @"
class Program
{
    public static readonly string S = ""hi"";
    public string SI;

    public static void Main()
    {
        var p = new P1(S);
        System.Console.WriteLine(p.SI);

         System.Console.WriteLine(p.M(42));
    }

    public Program(in string x)
    {
       SI = x;
    }

    public virtual ref readonly int M(in int x)
    {
        return ref x;
    }
}

class P1 : Program
{
    public P1(in string x) : base(x){}

    public override ref readonly int M(in int x)
    {
        return ref base.M(x);
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"hi
42");

            comp.VerifyIL("P1..ctor(in string)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""Program..ctor(in string)""
  IL_0007:  ret
}");

            comp.VerifyIL("P1.M(in int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""ref readonly int Program.M(in int)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefReturnParamAccess1()
        {
            var text = @"
class Program
{
    static ref readonly int M(ref readonly int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M(in int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void BindingInvalidRefInCombination()
        {
            var text = @"
class Program
{
    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly int M(in ref readonly int x)
    {
        return ref x;
    }

    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly int M1( ref in readonly int x)
    {
        return ref x;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,34): error CS8404:  The parameter modifier 'ref' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(6, 34),
                // (6,38): error CS8404:  The parameter modifier 'readonly' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "readonly").WithArguments("readonly", "in").WithLocation(6, 38),
                // (13,37): error CS8404:  The parameter modifier 'in' cannot be used with 'ref' 
                //     static ref readonly int M1( ref in readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(13, 37)
            );
        }

        [Fact]
        public void ReadonlyParamCannotAssign()
        {
            var text = @"
class Program
{
    static void M(in int arg1, in (int Alice, int Bob) arg2)
    {
        arg1 = 1;
        arg2.Alice = 2;

        arg1 ++;
        arg2.Alice --;

        arg1 += 1;
        arg2.Alice -= 2;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8408: Cannot assign to variable 'in int' because it is a readonly variable
                //         arg1 = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "in int").WithLocation(6, 9),
                // (7,9): error CS8409: Cannot assign to a member of variable 'in (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "in (int Alice, int Bob)").WithLocation(7, 9),
                // (9,9): error CS8408: Cannot assign to variable 'in int' because it is a readonly variable
                //         arg1 ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "in int").WithLocation(9, 9),
                // (10,9): error CS8409: Cannot assign to a member of variable 'in (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "in (int Alice, int Bob)").WithLocation(10, 9),
                // (12,9): error CS8408: Cannot assign to variable 'in int' because it is a readonly variable
                //         arg1 += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "in int"),
                // (13,9): error CS8409: Cannot assign to a member of variable 'in (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "in (int Alice, int Bob)"));
        }

        [Fact]
        public void ReadonlyParamCannotAssignByref()
        {
            var text = @"
class Program
{
    static void M(in int arg1, in (int Alice, int Bob) arg2)
    {
        ref var y = ref arg1;
        ref int a = ref arg2.Alice;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8406: Cannot use variable 'in int' as a ref or out value because it is a readonly variable
                //         ref var y = ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "arg1").WithArguments("variable", "in int"),
                // (7,25): error CS8407: Members of variable 'in (int Alice, int Bob)' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a = ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "arg2.Alice").WithArguments("variable", "in (int Alice, int Bob)"));
        }

        [Fact]
        public void ReadonlyParamCannotTakePtr()
        {
            var text = @"
class Program
{
    unsafe static void M(in int arg1, in (int Alice, int Bob) arg2)
    {
        int* a = & arg1;
        int* b = & arg2.Alice;

        fixed(int* c = & arg1)
        {
        }

        fixed(int* d = & arg2.Alice)
        {
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,20): error CS0211: Cannot take the address of the given expression
                //         int* a = & arg1;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "arg1").WithLocation(6, 20),
                // (7,20): error CS0211: Cannot take the address of the given expression
                //         int* b = & arg2.Alice;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "arg2.Alice").WithLocation(7, 20),
                // (9,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* c = & arg1)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "arg1").WithLocation(9, 26),
                // (13,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* d = & arg2.Alice)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "arg2.Alice").WithLocation(13, 26)
            );
        }

        [Fact]
        public void ReadonlyParamCannotReturnByOrdinaryRef()
        {
            var text = @"
class Program
{
    static ref int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        bool b = true;

        if (b)
        {
            return ref arg1;
        }
        else
        {
            return ref arg2.Alice;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (10,24): error CS8406: Cannot use variable 'in int' as a ref or out value because it is a readonly variable
                //             return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "arg1").WithArguments("variable", "in int"),
                // (14,24): error CS8407: Members of variable 'in (int Alice, int Bob)' cannot be used as a ref or out value because it is a readonly variable
                //             return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "arg2.Alice").WithArguments("variable", "in (int Alice, int Bob)"));
        }

        [Fact]
        public void ReadonlyParamCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        bool b = true;

        if (b)
        {
            return ref arg1;
        }
        else
        {
            return ref arg2.Alice;
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldarg.0
  IL_0004:  ret
  IL_0005:  ldarg.1
  IL_0006:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_000b:  ret
}");
        }

        [Fact, WorkItem(18357, "https://github.com/dotnet/roslyn/issues/18357")]
        public void ReadonlyParamCanReturnByRefReadonlyNested()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        ref readonly int M1(in int arg11, in (int Alice, int Bob) arg21)
        {
            bool b = true;

            if (b)
            {
                return ref arg11;
            }
            else
            {
                return ref arg21.Alice;
            }
        }

        return ref M1(arg1, arg2);
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.<M>g__M10_0(in int, in (int Alice, int Bob))", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldarg.0
  IL_0004:  ret
  IL_0005:  ldarg.1
  IL_0006:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_000b:  ret
}");
        }

        [Fact, WorkItem(18357, "https://github.com/dotnet/roslyn/issues/18357")]
        public void ReadonlyParamCannotReturnByRefNested()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        ref int M1(in int arg11, in (int Alice, int Bob) arg21)
        {
            bool b = true;

            if (b)
            {
                return ref arg11;
            }
            else
            {
                return ref arg21.Alice;
            }
        }

        return ref M1(arg1, arg2);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (12,28): error CS8406: Cannot use variable 'in int' as a ref or out value because it is a readonly variable
                //                 return ref arg11;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "arg11").WithArguments("variable", "in int").WithLocation(12, 28),
                // (16,28): error CS8407: Members of variable 'in (int Alice, int Bob)' cannot be used as a ref or out value because it is a readonly variable
                //                 return ref arg21.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "arg21.Alice").WithArguments("variable", "in (int Alice, int Bob)").WithLocation(16, 28)
                );
        }

        [Fact]
        public void ReadonlyParamOptional()
        {
            var text = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(M());
    }

    static int M(in int x = 42) => x;
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput:@"42");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""int Program.M(in int)""
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void ReadonlyParamConv()
        {
            var text = @"
class Program
{
    static void Main()
    {
        var arg = 42;
        System.Console.WriteLine(M(arg));
    }

    static double M(in double x) => x;
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"42");

            comp.VerifyIL("Program.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (double V_0)
  IL_0000:  ldc.i4.s   42
  IL_0002:  conv.r8
  IL_0003:  stloc.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""double Program.M(in double)""
  IL_000b:  call       ""void System.Console.WriteLine(double)""
  IL_0010:  ret
}");
        }
    }
}
