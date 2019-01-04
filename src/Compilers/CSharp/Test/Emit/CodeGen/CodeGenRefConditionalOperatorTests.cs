// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.RefConditionalOperator)]
    public class CodeGenRefConditionalOperatorTests : CSharpTestBase
    {
        [Fact]
        public void TestRefConditionalOperatorInRefReturn()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write(Test1(true));
        System.Console.Write(Test2(false));
    }

    static int val1 = 33;
    static int val2 = 44;

    static ref int Test1(bool b)
    {
        return ref b ? ref val1 : ref val2;
    }

    static ref int Test2(bool b) => ref b ? ref val1 : ref val2;

}";
            var comp = CompileAndVerify(source, expectedOutput: "3344");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Test1", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsflda    ""int C.val2""
  IL_0008:  ret
  IL_0009:  ldsflda    ""int C.val1""
  IL_000e:  ret
}
");

            comp.VerifyIL("C.Test2", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldsflda    ""int C.val2""
  IL_0008:  ret
  IL_0009:  ldsflda    ""int C.val1""
  IL_000e:  ret
}
");

        }

        [Fact]
        public void TestRefConditionalOperatorInRefArgument()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;
        System.Console.Write(
                            M1(ref b? 
                                   ref val1: 
                                   ref val2));
    }

    static int val1 = 33;
    static int val2 = 44;

    static ref int M1(ref int arg) => ref arg;

}";
            var comp = CompileAndVerify(source, expectedOutput: "44", verify: Verification.Fails);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsflda    ""int C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""int C.val1""
  IL_000f:  call       ""ref int C.M1(ref int)""
  IL_0014:  ldind.i4
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorAsValue()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        System.Console.Write(b? ref val1: ref val2);
    }

    static int val1 = 33;
    static int val2 = 44;
}";
            var comp = CompileAndVerify(source, expectedOutput: "44", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsfld     ""int C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsfld     ""int C.val1""
  IL_000f:  call       ""void System.Console.Write(int)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorAssignmentTarget()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        (b? ref val1: ref val2) = 55;

        System.Console.Write(val2);
    }

    static int val1 = 33;
    static int val2 = 44;
}";
            var comp = CompileAndVerify(source, expectedOutput: "55", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsflda    ""int C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""int C.val1""
  IL_000f:  ldc.i4.s   55
  IL_0011:  stind.i4
  IL_0012:  ldsfld     ""int C.val2""
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorAssignmentTargetUsed()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;
         System.Console.Write((b? ref val1: ref val2) = 55);

        System.Console.Write(val2);
    }

    static int val1 = 33;
    static int val2 = 44;
}";
            var comp = CompileAndVerify(source, expectedOutput: "5555", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsflda    ""int C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""int C.val1""
  IL_000f:  ldc.i4.s   55
  IL_0011:  dup
  IL_0012:  stloc.0
  IL_0013:  stind.i4
  IL_0014:  ldloc.0
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ldsfld     ""int C.val2""
  IL_001f:  call       ""void System.Console.Write(int)""
  IL_0024:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorIncrement()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;
        (b? ref val1: ref M1(ref val2)) ++;
        (b? ref val1: ref M1(ref val2)) += 22;

        System.Console.Write(val2);
    }

    static int val1 = 33;
    static int val2 = 44;

    static ref int M1(ref int arg) => ref arg;

}";
            var comp = CompileAndVerify(source, expectedOutput: "67", verify: Verification.Fails);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  4
  IL_0000:  ldc.i4.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0010
  IL_0004:  ldsflda    ""int C.val2""
  IL_0009:  call       ""ref int C.M1(ref int)""
  IL_000e:  br.s       IL_0015
  IL_0010:  ldsflda    ""int C.val1""
  IL_0015:  dup
  IL_0016:  ldind.i4
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  stind.i4
  IL_001a:  brtrue.s   IL_0028
  IL_001c:  ldsflda    ""int C.val2""
  IL_0021:  call       ""ref int C.M1(ref int)""
  IL_0026:  br.s       IL_002d
  IL_0028:  ldsflda    ""int C.val1""
  IL_002d:  dup
  IL_002e:  ldind.i4
  IL_002f:  ldc.i4.s   22
  IL_0031:  add
  IL_0032:  stind.i4
  IL_0033:  ldsfld     ""int C.val2""
  IL_0038:  call       ""void System.Console.Write(int)""
  IL_003d:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorIncrementUsed()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;
        System.Console.Write((b? ref val1: ref val2) ++);
        System.Console.Write((b? ref val1: ref val2) += 22);

        System.Console.Write(val2);
    }

    static int val1 = 33;
    static int val2 = 44;
}";
            var comp = CompileAndVerify(source, expectedOutput: "446767", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000b
  IL_0004:  ldsflda    ""int C.val2""
  IL_0009:  br.s       IL_0010
  IL_000b:  ldsflda    ""int C.val1""
  IL_0010:  dup
  IL_0011:  ldind.i4
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  stind.i4
  IL_0017:  ldloc.0
  IL_0018:  call       ""void System.Console.Write(int)""
  IL_001d:  brtrue.s   IL_0026
  IL_001f:  ldsflda    ""int C.val2""
  IL_0024:  br.s       IL_002b
  IL_0026:  ldsflda    ""int C.val1""
  IL_002b:  dup
  IL_002c:  ldind.i4
  IL_002d:  ldc.i4.s   22
  IL_002f:  add
  IL_0030:  dup
  IL_0031:  stloc.0
  IL_0032:  stind.i4
  IL_0033:  ldloc.0
  IL_0034:  call       ""void System.Console.Write(int)""
  IL_0039:  ldsfld     ""int C.val2""
  IL_003e:  call       ""void System.Console.Write(int)""
  IL_0043:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorInRefAssignment()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        int x = 1;
        int y = 2;

        ref var local = ref b ? ref x : ref y;

        System.Console.WriteLine(local);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "1");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  stloc.1
  IL_0005:  brtrue.s   IL_000b
  IL_0007:  ldloca.s   V_1
  IL_0009:  br.s       IL_000d
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldind.i4
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOperatorElvis()
        {
            var source = @"

class Program
{
    interface IDisposable1
    {
        void Dispose();
    }

    class C1 : IDisposable1
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    struct S1 : IDisposable1
    {
        private bool disposed;

        public void Dispose()
        {
            System.Console.WriteLine(disposed);
            disposed = true;
        }
    }

    static void Main(string[] args)
    {
        C1 c = new C1();
        Test(ref c, ref c);

        S1 s = new S1();
        Test(ref s, ref s);
    }

    static void Test<T>(ref T x, ref T y) where T : IDisposable1
    {
        bool b = true;
        (b? ref x: ref y)?.Dispose();
        (!b? ref x: ref y)?.Dispose();
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"False
True
False
True");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Test<T>(ref T, ref T)", @"
{
  // Code size      106 (0x6a)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0007
  IL_0004:  ldarg.1
  IL_0005:  br.s       IL_0008
  IL_0007:  ldarg.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  initobj    ""T""
  IL_0010:  ldloc.0
  IL_0011:  box        ""T""
  IL_0016:  brtrue.s   IL_002b
  IL_0018:  ldobj      ""T""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  ldloc.0
  IL_0021:  box        ""T""
  IL_0026:  brtrue.s   IL_002b
  IL_0028:  pop
  IL_0029:  br.s       IL_0036
  IL_002b:  constrained. ""T""
  IL_0031:  callvirt   ""void Program.IDisposable1.Dispose()""
  IL_0036:  brfalse.s  IL_003b
  IL_0038:  ldarg.1
  IL_0039:  br.s       IL_003c
  IL_003b:  ldarg.0
  IL_003c:  ldloca.s   V_0
  IL_003e:  initobj    ""T""
  IL_0044:  ldloc.0
  IL_0045:  box        ""T""
  IL_004a:  brtrue.s   IL_005e
  IL_004c:  ldobj      ""T""
  IL_0051:  stloc.0
  IL_0052:  ldloca.s   V_0
  IL_0054:  ldloc.0
  IL_0055:  box        ""T""
  IL_005a:  brtrue.s   IL_005e
  IL_005c:  pop
  IL_005d:  ret
  IL_005e:  constrained. ""T""
  IL_0064:  callvirt   ""void Program.IDisposable1.Dispose()""
  IL_0069:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalAsyncIncrement()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Test().Wait();
        System.Console.Write(val1);
    }

    static async Task<int> Test()
    {
        bool b = true;

        (b? ref val1: ref val2) += await One();

        return 1;
    }

    static int val1 = 33;
    static int val2 = 44;

    static ref int M1() => ref val1;

    static async Task<int> One()
    {
        await Task.Yield();

        return 1;
    }

}";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (16,10): error CS8325: 'await' cannot be used in an expression containing a ref conditional operator
                //         (b? ref val1: ref val2) += await One();
                Diagnostic(ErrorCode.ERR_RefConditionalAndAwait, "b? ref val1: ref val2").WithLocation(16, 10)
                );
        }

        [Fact]
        public void TestRefConditionalAsyncAssign()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Test().Wait();
        System.Console.Write(val1);
    }

    static async Task<int> Test()
    {
        bool b = true;

        (b? ref val1: ref val2) = await One();

        return 1;
    }

    static int val1 = 33;
    static int val2 = 44;

    static ref int M1() => ref val1;

    static async Task<int> One()
    {
        await Task.Yield();

        return 1;
    }

}";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (16,10): error CS8325: 'await' cannot be used in an expression containing a ref conditional operator
                //         (b? ref val1: ref val2) = await One();
                Diagnostic(ErrorCode.ERR_RefConditionalAndAwait, "b? ref val1: ref val2").WithLocation(16, 10)
               );
        }

        [Fact]
        public void TestRefConditionalOneRef()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        System.Console.Write(b? val1: ref val2);
        System.Console.Write(b? ref val1: val2);
    }

    static int val1 = 33;
    static int val2 = 44;
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (8,43): error CS8326: Both conditional operator values must be ref values or neither may be a ref value
                //         System.Console.Write(b? val1: ref val2);
                Diagnostic(ErrorCode.ERR_RefConditionalNeedsTwoRefs, "val2").WithLocation(8, 43),
                // (9,37): error CS8326: Both conditional operator values must be ref values or neither may be a ref value
                //         System.Console.Write(b? ref val1: val2);
                Diagnostic(ErrorCode.ERR_RefConditionalNeedsTwoRefs, "val1").WithLocation(9, 37)
               );
        }

        [Fact]
        public void TestRefConditionalRValue()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        (b? ref val1: ref 42) = 1;

        ref var local = ref b? ref val1: ref 42;

    }

    static int val1 = 33;
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (8,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         (b? ref val1: ref 42) = 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(8, 27),
                // (10,46): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         ref var local = ref b? ref val1: ref 42;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(10, 46)
               );
        }

        [Fact]
        [WorkItem(24306, "https://github.com/dotnet/roslyn/issues/24306")]
        public void TestRefConditional_71()
        {
            var source = @"
class C
{
    static void Main()
    {

    }

    void Test()
    {
        int local1 = 1;
        int local2 = 2;
        bool b = true;

        ref int r = ref b? ref local1: ref local2;
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);

            comp.VerifyEmitDiagnostics(
                // (15,25): error CS8302: Feature 'ref conditional expression' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         ref int r = ref b? ref local1: ref local2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "b? ref local1: ref local2").WithArguments("ref conditional expression", "7.2").WithLocation(15, 25)
               );
        }

        [Fact]
        public void TestRefConditionalUnsafeToReturn1()
        {
            var source = @"
class C
{
    static void Main()
    {

    }

    ref int Test()
    {
        int local1 = 1;
        int local2 = 2;
        bool b = true;

        return ref b? ref local1: ref local2;
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (15,27): error CS8168: Cannot return local 'local1' by reference because it is not a ref local
                //         return ref b? ref local1: ref local2;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local1").WithArguments("local1").WithLocation(15, 27)
               );
        }

        [Fact]
        public void TestRefConditionalUnsafeToReturn2()
        {
            var source = @"
class C
{
    static void Main()
    {

    }

    ref int Test()
    {
        int local2 = 1;
        bool b = true;

        return ref b? ref val1: ref local2;
    }

    static int val1 = 33;
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (14,37): error CS8168: Cannot return local 'local2' by reference because it is not a ref local
                //         return ref b? ref val1: ref local2;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local2").WithArguments("local2").WithLocation(14, 37)
               );
        }

        [Fact]
        public void TestRefConditionalUnsafeToReturn3()
        {
            var source = @"
class C
{
    static void Main()
    {

    }

    ref int Test()
    {
        S1 local2 = default(S1);
        bool b = true;

        return ref (b? ref val1: ref local2).x;
    }

    static S1 val1;

    struct S1
    {
        public int x;
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (14,38): error CS8168: Cannot return local 'local2' by reference because it is not a ref local
                //         return ref (b? ref val1: ref local2).x;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local2").WithArguments("local2").WithLocation(14, 38)
               );
        }

        [Fact]
        public void TestRefConditionalUnsafeToReturn4()
        {
            var source = @"
class C
{
    static void Main()
    {

    }

    ref int Test()
    {
        S1 local2 = default(S1);
        bool b = true;

        ref var temp = ref (b? ref val1: ref local2).x;
        return ref temp;
    }

    static S1 val1;

    struct S1
    {
        public int x;
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (15,20): error CS8157: Cannot return 'temp' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref temp;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "temp").WithArguments("temp").WithLocation(15, 20)
               );
        }

        [Fact]
        public void TestRefConditionalSafeToReturn1()
        {
            var source = @"
class C
{
    static void Main()
    {
        Test() ++;
        System.Console.WriteLine(val1.x);
    }

    static ref int Test()
    {
        bool b = true;

        return ref (b? ref val1: ref val2).x;
    }

    static S1 val1;
    static S1 val2;

    struct S1
    {
        public int x;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Test", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsflda    ""C.S1 C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""C.S1 C.val1""
  IL_000f:  ldflda     ""int C.S1.x""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalSafeToReturn2()
        {
            var source = @"
class C
{
    static void Main()
    {
        Test() ++;
        System.Console.WriteLine(val1.x);
    }

    static ref int Test()
    {
        return ref (true? ref val1: ref val2).x;
    }

    static S1 val1;
    static S1 val2;

    struct S1
    {
        public int x;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Test", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsflda    ""C.S1 C.val1""
  IL_0005:  ldflda     ""int C.S1.x""
  IL_000a:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalSafeToReturn3()
        {
            var source = @"
class C
{
    static void Main()
    {
        (false? ref val1: ref val2) = (true? 1: val2);
        (true? ref val1: ref val2) = (false? ref val1: ref val2);
        System.Console.WriteLine(val1);
    }

    static int val1;
    static int val2;
}
";
            var comp = CompileAndVerify(source, expectedOutput: "1", verify: Verification.Passes);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main()", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  stsfld     ""int C.val2""
  IL_0006:  ldsfld     ""int C.val2""
  IL_000b:  stsfld     ""int C.val1""
  IL_0010:  ldsfld     ""int C.val1""
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalDifferentTypes1()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        System.Console.Write(b? ref val1: ref val2);
    }

    static int val1 = 33;
    static short val2 = 44;
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (8,47): error CS8327: The expression must be of type 'int' to match the alternative ref value
                //         System.Console.Write(b? ref val1: ref val2);
                Diagnostic(ErrorCode.ERR_RefConditionalDifferentTypes, "val2").WithArguments("int").WithLocation(8, 47)
               );
        }

        [Fact]
        public void TestRefConditionalDifferentTypes2()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = false;

        System.Console.Write(b? ref val1: ref ()=>1);
    }

    static System.Func<int> val1 = null;
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (8,47): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         System.Console.Write(b? ref val1: ref ()=>1);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "()=>1").WithLocation(8, 47)
               );
        }

        [Fact]
        public void TestRefConditionalDifferentTypes3()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;

        ref var x = ref b? ref val1: ref val2;
        System.Console.Write(x.Alice);
    }

    static (int Alice, int) val1 = (1,2);
    static (int Alice, int Bob) val2 = (3,4);
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1");
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldsflda    ""(int Alice, int Bob) C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""(int Alice, int) C.val1""
  IL_000f:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0014:  call       ""void System.Console.Write(int)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalHomelessBranches()
        {
            var source = @"
    class Program
    {
        static void Main(string[] args)
        {
            var o = new C1();
            var o1 = new C1();

            (args != null ? ref o.field : ref o1.field).ToString();
            System.Console.Write(o.field.value);

            // no copying expected
            (args != null ? ref o.field : ref o1.field).RoExtension();
            System.Console.Write(o.field.value);
        }
    }

    class C1
    {
        public readonly S1 field;
    }

    public struct S1
    {
        public int value;

        public override string ToString()
        {
            value = 42;
            return base.ToString();
        }
    }

    public static class S1Ext
    {
        public static void RoExtension(in this S1 self)
        {
            // do nothing
        }
    }
";

            var comp = CompileAndVerifyWithMscorlib40(source, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef, SystemCoreRef }, expectedOutput: "00", verify: Verification.Fails);
            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Main", @"
{
  // Code size       99 (0x63)
  .maxstack  1
  .locals init (C1 V_0, //o
                C1 V_1, //o1
                S1 V_2)
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""C1..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldarg.0
  IL_000d:  brtrue.s   IL_0017
  IL_000f:  ldloc.1
  IL_0010:  ldfld      ""S1 C1.field""
  IL_0015:  br.s       IL_001d
  IL_0017:  ldloc.0
  IL_0018:  ldfld      ""S1 C1.field""
  IL_001d:  stloc.2
  IL_001e:  ldloca.s   V_2
  IL_0020:  constrained. ""S1""
  IL_0026:  callvirt   ""string object.ToString()""
  IL_002b:  pop
  IL_002c:  ldloc.0
  IL_002d:  ldflda     ""S1 C1.field""
  IL_0032:  ldfld      ""int S1.value""
  IL_0037:  call       ""void System.Console.Write(int)""
  IL_003c:  ldarg.0
  IL_003d:  brtrue.s   IL_0047
  IL_003f:  ldloc.1
  IL_0040:  ldflda     ""S1 C1.field""
  IL_0045:  br.s       IL_004d
  IL_0047:  ldloc.0
  IL_0048:  ldflda     ""S1 C1.field""
  IL_004d:  call       ""void S1Ext.RoExtension(in S1)""
  IL_0052:  ldloc.0
  IL_0053:  ldflda     ""S1 C1.field""
  IL_0058:  ldfld      ""int S1.value""
  IL_005d:  call       ""void System.Console.Write(int)""
  IL_0062:  ret
}
");
        }

        [Fact]
        public void TestRefConditionalOneHomelessBranch()
        {
            var source = @"
    class Program
    {
        static void Main(string[] args)
        {
            var o = new C1();

            Test(true, o);
            Test(false, o);

            System.Console.Write(o.field.value);
            System.Console.Write(o.field1.value);
        }

        private static void Test(bool flag, C1 o)
        {
            (flag ? ref o.field : ref o.field1).ToString();
        }
    }

    class C1
    {
        public readonly S1 field;
        public S1 field1;
    }

    struct S1
    {
        public int value;

        public override string ToString()
        {
            value = 42;
            return base.ToString();
        }
    }
";

            var comp = CompileAndVerify(source, expectedOutput: "00", verify: Verification.Fails);
            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000b
  IL_0003:  ldarg.1
  IL_0004:  ldfld      ""S1 C1.field1""
  IL_0009:  br.s       IL_0011
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""S1 C1.field""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  constrained. ""S1""
  IL_001a:  callvirt   ""string object.ToString()""
  IL_001f:  pop
  IL_0020:  ret
}
");

        }

        [Fact]
        public void TestRefConditionalDifferentTypes4()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;

        ref var x = ref b? ref val1: ref val2;
        System.Console.Write(x.Bob);
    }

    static (int Alice, int) val1 = (1,2);
    static (int Alice, int Bob) val2 = (3,4);
}
";

            var comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (9,32): error CS1061: '(int Alice, int)' does not contain a definition for 'Bob' and no extension method 'Bob' accepting a first argument of type '(int Alice, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.Write(x.Bob);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Bob").WithArguments("(int Alice, int)", "Bob").WithLocation(9, 32)
               );
        }

    }
}
