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
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class CodeGenRefReadOnlyParametersTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnParamAccess()
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

            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RefReadOnlyParamPassLValue()
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

    static ref readonly int M(ref readonly int x)
    {
        return ref x;
    }


    struct S1
    {
        public int X;

        public static S1 operator +(ref readonly S1 x, ref readonly S1 y)
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
  IL_0005:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""Program.S1""
  IL_0018:  ldloca.s   V_1
  IL_001a:  ldc.i4.s   42
  IL_001c:  stfld      ""int Program.S1.X""
  IL_0021:  ldloca.s   V_1
  IL_0023:  ldloca.s   V_1
  IL_0025:  call       ""Program.S1 Program.S1.op_Addition(ref readonly Program.S1, ref readonly Program.S1)""
  IL_002a:  stloc.1
  IL_002b:  ldloc.1
  IL_002c:  ldfld      ""int Program.S1.X""
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void RefReadOnlyParamPassRValue()
        {
            var text = @"
class Program
{
    public static void Main()
    {
        System.Console.WriteLine(M(42));
        System.Console.WriteLine(new Program()[5, 6]);
        System.Console.WriteLine(M(42));
        System.Console.WriteLine(M(42));
    }

    static ref readonly int M(ref readonly int x)
    {
        return ref x;
    }

    int this[ref readonly int x, ref readonly int y] => x + y;
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"42
11
42
42");

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       72 (0x48)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  newobj     ""Program..ctor()""
  IL_0015:  ldc.i4.5
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.6
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""int Program.this[ref readonly int, ref readonly int].get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ldc.i4.s   42
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_0031:  ldind.i4
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ldc.i4.s   42
  IL_0039:  stloc.0
  IL_003a:  ldloca.s   V_0
  IL_003c:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_0041:  ldind.i4
  IL_0042:  call       ""void System.Console.WriteLine(int)""
  IL_0047:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void RefReadOnlyParamPassRoField()
        {
            var text = @"
class Program
{
    public static readonly int F = 42;

    public static void Main()
    {
        System.Console.WriteLine(M(F));
    }

    static ref readonly int M(ref readonly int x)
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
  IL_0005:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ret
}");


            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

            comp = CompileAndVerify(text, verify: false, expectedOutput: "42", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldsfld     ""int Program.F""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""ref readonly int Program.M(ref readonly int)""
  IL_000d:  ldind.i4
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ret
}");

        }

        [Fact]
        public void RefReadOnlyParamPassRoParamReturn()
        {
            var text = @"
class Program
{
    public static readonly int F = 42;

    public static void Main()
    {
        System.Console.WriteLine(M(F));
    }

    static ref readonly int M(ref readonly int x)
    {
        return ref M1(x);
    }

    static ref readonly int M1(ref readonly int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: "42");

            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref readonly int Program.M1(ref readonly int)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void RefReadOnlyParamBase()
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

    public Program(ref readonly string x)
    {
       SI = x;
    }

    public virtual ref readonly int M(ref readonly int x)
    {
        return ref x;
    }
}

class P1 : Program
{
    public P1(ref readonly string x) : base(x){}

    public override ref readonly int M(ref readonly int x)
    {
        return ref base.M(x);
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"hi
42");

            comp.VerifyIL("P1..ctor(ref readonly string)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""Program..ctor(ref readonly string)""
  IL_0007:  ret
}");

            comp.VerifyIL("P1.M(ref readonly int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""ref readonly int Program.M(ref readonly int)""
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

            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void ReadonlyParamCannotAssign()
        {
            var text = @"
class Program
{
    static void M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
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
                // (6,9): error CS8408: Cannot assign to variable 'ref readonly int' because it is a readonly variable
                //         arg1 = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int").WithLocation(6, 9),
                // (7,9): error CS8409: Cannot assign to a member of variable 'ref readonly (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)").WithLocation(7, 9),
                // (9,9): error CS8408: Cannot assign to variable 'ref readonly int' because it is a readonly variable
                //         arg1 ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int").WithLocation(9, 9),
                // (10,9): error CS8409: Cannot assign to a member of variable 'ref readonly (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)").WithLocation(10, 9),
                // (12,9): error CS8408: Cannot assign to variable 'ref readonly int' because it is a readonly variable
                //         arg1 += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int"),
                // (13,9): error CS8409: Cannot assign to a member of variable 'ref readonly (int Alice, int Bob)' because it is a readonly variable
                //         arg2.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)"));
        }

        [Fact]
        public void ReadonlyParamCannotRefReturn()
        {
            var text = @"
class Program
{
    static ref readonly int M1_baseline(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        // valid
        return ref arg1;
    }

    static ref readonly int M2_baseline(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        // valid
        return ref arg2.Alice;
    }

    static ref int M1(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        return ref arg1;
    }

    static ref int M2(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        return ref arg2.Alice;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (18,20): error CS8410: Cannot return variable 'ref readonly int' by writable reference because it is a readonly variable
                //         return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int").WithLocation(18, 20),
                // (23,20): error CS8411: Members of variable 'ref readonly (int Alice, int Bob)' cannot be returned by writable reference because it is a readonly variable
                //         return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)").WithLocation(23, 20)
            );
        }

        [Fact]
        public void ReadonlyParamCannotAssignByref()
        {
            var text = @"
class Program
{
    static void M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        ref var y = ref arg1;
        ref int a = ref arg2.Alice;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8406: Cannot use variable 'ref readonly int' as a ref or out value because it is a readonly variable
                //         ref var y = ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int"),
                // (7,25): error CS8407: Members of variable 'ref readonly (int Alice, int Bob)' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a = ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)"));
        }

        [Fact]
        public void ReadonlyParamCannotTakePtr()
        {
            var text = @"
class Program
{
    unsafe static void M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
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
    static ref int M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
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
                // (10,24): error CS8410: Cannot return variable 'ref readonly int' by writable reference because it is a readonly variable
                //             return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg1").WithArguments("variable", "ref readonly int").WithLocation(10, 24),
                // (14,24): error CS8411: Members of variable 'ref readonly (int Alice, int Bob)' cannot be returned by writable reference because it is a readonly variable
                //             return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg2.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)").WithLocation(14, 24)
            );
        }

        [Fact]
        public void ReadonlyParamCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    static ref readonly int M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
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
    static ref readonly int M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        ref readonly int M1(ref readonly int arg11, ref readonly (int Alice, int Bob) arg21)
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

            comp.VerifyIL("Program.<M>g__M1|0_0(ref readonly int, ref readonly (int Alice, int Bob))", @"
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
    static ref readonly int M(ref readonly int arg1, ref readonly (int Alice, int Bob) arg2)
    {
        ref int M1(ref readonly int arg11, ref readonly (int Alice, int Bob) arg21)
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
                // (12,28): error CS8410: Cannot return variable 'ref readonly int' by writable reference because it is a readonly variable
                //                 return ref arg11;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg11").WithArguments("variable", "ref readonly int").WithLocation(12, 28),
                // (16,28): error CS8411: Members of variable 'ref readonly (int Alice, int Bob)' cannot be returned by writable reference because it is a readonly variable
                //                 return ref arg21.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg21.Alice").WithArguments("variable", "ref readonly (int Alice, int Bob)").WithLocation(16, 28)
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

    static int M(ref readonly int x = 42) => x;
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
  IL_0005:  call       ""int Program.M(ref readonly int)""
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

    static double M(ref readonly double x) => x;
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
  IL_0006:  call       ""double Program.M(ref readonly double)""
  IL_000b:  call       ""void System.Console.WriteLine(double)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void ReadonlyParamAsyncSpill1()
        {
            var text = @"
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
        }

        public static async Task Test()
        {
            M1(1, await GetT(2), 3);
        }

        public static async Task<T> GetT<T>(T val)
        {
            await Task.Yield();
            return val;
        }

        public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"6");
        }

        [Fact]
        public void ReadonlyParamAsyncSpill2()
        {
            var text = @"
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
        }

        public static async Task Test()
        {
            M1(await GetT(1), await GetT(2), 3);
        }

        public static async Task<T> GetT<T>(T val)
        {
            await Task.Yield();
            return val;
        }

        public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"6");
        }

        [WorkItem(20764, "https://github.com/dotnet/roslyn/issues/20764")]
        [Fact]
        public void ReadonlyParamAsyncSpillMethods()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // BASELINE - without an await
        // prints   3 42 3 3       note the aliasing, 3 is the last state of the local.f
        M1(GetLocal(ref local).f,             42, GetLocal(ref local).f, GetLocal(ref local).f);

        local = new S1();

        // prints   1 42 3 3       note no aliasing for the first argument because of spilling of calls
        M1(GetLocal(ref local).f, await GetT(42), GetLocal(ref local).f, GetLocal(ref local).f);
    }

    private static ref readonly S1 GetLocal(ref S1 local)
    {
        local.f++;
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }

    public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(arg1);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}

public struct S1
{
    public int f;
}

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
3
42
3
3
1
42
3
3");
        }

        [WorkItem(20764, "https://github.com/dotnet/roslyn/issues/20764")]
        [Fact]
        public void ReadonlyParamAsyncSpillMethodsWriteable()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // BASELINE - without an await
        // prints   3 42 3 3       note the aliasing, 3 is the last state of the local.f
        M1(GetLocalWriteable(ref local).f,             42, GetLocalWriteable(ref local).f, GetLocalWriteable(ref local).f);

        local = new S1();

        // prints   1 42 3 3       note no aliasing for the first argument because of spilling of calls
        M1(GetLocalWriteable(ref local).f, await GetT(42), GetLocalWriteable(ref local).f, GetLocalWriteable(ref local).f);
    }

    private static ref S1 GetLocalWriteable(ref S1 local)
    {
        local.f++;
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }

    public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(arg1);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}

public struct S1
{
    public int f;
}

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
3
42
3
3
1
42
3
3");
        }


        [WorkItem(20764, "https://github.com/dotnet/roslyn/issues/20764")]
        [Fact]
        public void ReadonlyParamAsyncSpillStructField()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // prints   2 42 2 2       note aliasing for all arguments regardless of spilling
        M1(local.f, await GetT(42), GetLocal(ref local).f, GetLocal(ref local).f);
    }

        private static ref readonly S1 GetLocal(ref S1 local)
    {
        local.f++;
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }

    public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(arg1);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}

public struct S1
{
    public int f;
}

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        public void ReadonlyParamAsyncSpillClassField()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // prints   2 42 2 2       note aliasing for all arguments regardless of spilling
        M1(local.f, await GetT(42), GetLocal(ref local).f, GetLocal(ref local).f);
    }

    private static ref readonly S1 GetLocal(ref S1 local)
    {
        local.f++;
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }

    public static void M1(ref readonly int arg1, ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(arg1);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}

public class S1
{
    public int f;
}

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        public void ReadonlyParamAsyncSpillExtension()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // prints   2 42 2 2       note aliasing for all arguments regardless of spilling
        local.f.M1(await GetT(42), GetLocalWriteable(ref local).f, GetLocalWriteable(ref local).f);
    }

    private static ref S1 GetLocalWriteable(ref S1 local)
    {
        local.f++;
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }
}

static class Ext
{
    public static void M1(ref readonly this int arg1, ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(arg1);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}

public struct S1
{
    public int f;
}

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void ReadonlyParamAsyncSpillReadOnlyStructThis()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static async Task Test()
    {
        var local = new S1();

        // BASELINE - without an await
        // prints   3 42 3 3       note the aliasing, 3 is the last state of the local.f
        GetLocal(ref local).M1(            42, GetLocal(ref local).f, GetLocal(ref local).f);

        local = new S1();

        // prints   1 42 3 3       note no aliasing for the first argument because of spilling of a call
        GetLocal(ref local).M1(await GetT(42), GetLocal(ref local).f, GetLocal(ref local).f);

        local = new S1();

        // prints   1 42 3 3       note no aliasing for the first argument because of spilling of a call
        GetLocalWriteable(ref local).M1(await GetT(42), GetLocal(ref local).f, GetLocal(ref local).f);
    }

    private static ref readonly S1 GetLocal(ref S1 local)
    {
        local = new S1(local.f + 1);
        return ref local;
    }

    private static ref S1 GetLocalWriteable(ref S1 local)
    {
        local = new S1(local.f + 1);
        return ref local;
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }
}

public readonly struct S1
{
    public readonly int f;

    public S1(int val)
    {
        this.f = val;
    }

    public void M1(ref readonly int arg2, ref readonly int arg3, ref readonly int arg4)
    {
        System.Console.WriteLine(this.f);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}
";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: false, expectedOutput: @"
3
42
3
3
1
42
3
3
1
42
3
3");

            comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());
            CompileAndVerify(comp, verify: false, expectedOutput: @"
3
42
2
3
1
42
2
3
1
42
2
3");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void ReadonlyParamAsyncSpillReadOnlyStructThis_NoValCapture()
        {
            var text = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Test().Wait();
    }

    public static readonly S1 s1 = new S1(1);
    public static readonly S1 s2 = new S1(2);
    public static readonly S1 s3 = new S1(3);
    public static readonly S1 s4 = new S1(4);

    public static async Task Test()
    {
        s1.M1(s2, await GetT(s3), s4);
    }

    public static async Task<T> GetT<T>(T val)
    {
        await Task.Yield();
        return val;
    }
}

public readonly struct S1
{
    public readonly int f;

    public S1(int val)
    {
        this.f = val;
    }

    public void M1(ref readonly S1 arg2, ref readonly S1 arg3, ref readonly S1 arg4)
    {
        System.Console.WriteLine(this.f);
        System.Console.WriteLine(arg2.f);
        System.Console.WriteLine(arg3.f);
        System.Console.WriteLine(arg4.f);
    }
}
";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            var v = CompileAndVerify(comp, verify: false, expectedOutput: @"
1
2
3
4");

            // NOTE: s1, s3 and s4 are all directly loaded via ldsflda and not spilled.
            v.VerifyIL("Program.<Test>d__5.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      170 (0xaa)
  .maxstack  4
  .locals init (int V_0,
                S1 V_1,
                System.Runtime.CompilerServices.TaskAwaiter<S1> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__5.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0043
    IL_000a:  ldsfld     ""S1 Program.s3""
    IL_000f:  call       ""System.Threading.Tasks.Task<S1> Program.GetT<S1>(S1)""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<S1> System.Threading.Tasks.Task<S1>.GetAwaiter()""
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<S1>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_005f
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldarg.0
    IL_003c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<S1>, Program.<Test>d__5>(ref System.Runtime.CompilerServices.TaskAwaiter<S1>, ref Program.<Test>d__5)""
    IL_0041:  leave.s    IL_00a9
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0049:  stloc.2
    IL_004a:  ldarg.0
    IL_004b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0050:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<S1>""
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""S1 System.Runtime.CompilerServices.TaskAwaiter<S1>.GetResult()""
    IL_0066:  stloc.1
    IL_0067:  ldsflda    ""S1 Program.s1""
    IL_006c:  ldsflda    ""S1 Program.s2""
    IL_0071:  ldloca.s   V_1
    IL_0073:  ldsflda    ""S1 Program.s4""
    IL_0078:  call       ""void S1.M1(ref readonly S1, ref readonly S1, ref readonly S1)""
    IL_007d:  leave.s    IL_0096
  }
  catch System.Exception
  {
    IL_007f:  stloc.3
    IL_0080:  ldarg.0
    IL_0081:  ldc.i4.s   -2
    IL_0083:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_0088:  ldarg.0
    IL_0089:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
    IL_008e:  ldloc.3
    IL_008f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0094:  leave.s    IL_00a9
  }
  IL_0096:  ldarg.0
  IL_0097:  ldc.i4.s   -2
  IL_0099:  stfld      ""int Program.<Test>d__5.<>1__state""
  IL_009e:  ldarg.0
  IL_009f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
  IL_00a4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00a9:  ret
}
");

            comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());
            v = CompileAndVerify(comp, verify: true, expectedOutput: @"
1
2
3
4");

            // NOTE: s1, s3 and s4 are all directly loaded via ldsflda and not spilled.
            v.VerifyIL("Program.<Test>d__5.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      183 (0xb7)
  .maxstack  4
  .locals init (int V_0,
                S1 V_1,
                System.Runtime.CompilerServices.TaskAwaiter<S1> V_2,
                S1 V_3,
                S1 V_4,
                S1 V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__5.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0043
    IL_000a:  ldsfld     ""S1 Program.s3""
    IL_000f:  call       ""System.Threading.Tasks.Task<S1> Program.GetT<S1>(S1)""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<S1> System.Threading.Tasks.Task<S1>.GetAwaiter()""
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<S1>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_005f
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldarg.0
    IL_003c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<S1>, Program.<Test>d__5>(ref System.Runtime.CompilerServices.TaskAwaiter<S1>, ref Program.<Test>d__5)""
    IL_0041:  leave.s    IL_00b6
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0049:  stloc.2
    IL_004a:  ldarg.0
    IL_004b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<S1> Program.<Test>d__5.<>u__1""
    IL_0050:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<S1>""
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""S1 System.Runtime.CompilerServices.TaskAwaiter<S1>.GetResult()""
    IL_0066:  stloc.1
    IL_0067:  ldsfld     ""S1 Program.s1""
    IL_006c:  stloc.3
    IL_006d:  ldloca.s   V_3
    IL_006f:  ldsfld     ""S1 Program.s2""
    IL_0074:  stloc.s    V_4
    IL_0076:  ldloca.s   V_4
    IL_0078:  ldloca.s   V_1
    IL_007a:  ldsfld     ""S1 Program.s4""
    IL_007f:  stloc.s    V_5
    IL_0081:  ldloca.s   V_5
    IL_0083:  call       ""void S1.M1(ref readonly S1, ref readonly S1, ref readonly S1)""
    IL_0088:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_008a:  stloc.s    V_6
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int Program.<Test>d__5.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
    IL_009a:  ldloc.s    V_6
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a1:  leave.s    IL_00b6
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Program.<Test>d__5.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__5.<>t__builder""
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b6:  ret
}
");
        }

        [Fact]
        public void InParamGenericReadonly()
        {
            var text = @"

    class Program
    {
        static void Main(string[] args)
        {
            var o = new D();
            var s = new S1();
            o.M1(s);

            // should not be mutated.
            System.Console.WriteLine(s.field);
        }
    }

    abstract class C<U>
    {
        public abstract void M1<T>(in T arg) where T : U, I1;
    }

    class D: C<S1>
    {
        public override void M1<T>(in T arg)
        {
            arg.M3();
        }
    }

    public struct S1: I1
    {
        public int field;

        public void M3()
        {
            field = 42;
        }
    }

    interface I1
    {
        void M3();
    }
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"0");

            comp.VerifyIL("D.M1<T>(in T)", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldobj      ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""void I1.M3()""
  IL_0014:  ret
}");
        }

        [Fact]
        public void InParamGenericReadonlyROstruct()
        {
            var text = @"

    class Program
    {
        static void Main(string[] args)
        {
            var o = new D();
            var s = new S1();
            o.M1(s);
        }
    }

    abstract class C<U>
    {
        public abstract void M1<T>(in T arg) where T : U, I1;
    }

    class D: C<S1>
    {
        public override void M1<T>(in T arg)
        {
            arg.M3();
        }
    }

    public readonly struct S1: I1
    {
        public void M3()
        {
        }
    }

    interface I1
    {
        void M3();
    }
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false, expectedOutput: @"");

            comp.VerifyIL("D.M1<T>(in T)", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldobj      ""T""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""void I1.M3()""
  IL_0014:  ret
}");
        }

    }
}
