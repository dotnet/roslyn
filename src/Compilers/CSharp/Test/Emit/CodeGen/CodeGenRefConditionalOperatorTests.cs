// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
  IL_0003:  ldsflda    ""System.ValueTuple<int, int> C.val2""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldsflda    ""System.ValueTuple<int, int> C.val1""
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

            // PEVerify: Cannot change initonly field outside its .ctor.
            var comp = CompileAndVerifyWithMscorlib40(source, references: new[] { TestMetadata.Net40.System, ValueTupleRef, TestMetadata.Net40.SystemCore }, expectedOutput: "00", verify: Verification.FailsPEVerify);
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
            // PEVerify: Cannot change initonly field outside its .ctor.
            var comp = CompileAndVerify(source, expectedOutput: "00", verify: Verification.FailsPEVerify);
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

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerIndirection_ThroughTernary_01()
        {
            var code = @"
using System;

unsafe
{
    bool b = true;
    ref int x = ref b ? ref *(int*)0 : ref *(int*)1;
    Console.WriteLine(""run"");
}
";

            verify(TestOptions.UnsafeReleaseExe, Verification.Passes, @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brtrue.s   IL_0008
  IL_0003:  ldc.i4.1
  IL_0004:  conv.i
  IL_0005:  pop
  IL_0006:  br.s       IL_000b
  IL_0008:  ldc.i4.0
  IL_0009:  conv.i
  IL_000a:  pop
  IL_000b:  ldstr      ""run""
  IL_0010:  call       ""void System.Console.WriteLine(string)""
  IL_0015:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, Verification.Fails, @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (bool V_0, //b
                int& V_1) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  brtrue.s   IL_000a
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i
  IL_0008:  br.s       IL_000c
  IL_000a:  ldc.i4.0
  IL_000b:  conv.i
  IL_000c:  stloc.1
  IL_000d:  ldstr      ""run""
  IL_0012:  call       ""void System.Console.WriteLine(string)""
  IL_0017:  nop
  IL_0018:  nop
  IL_0019:  ret
}
");

            void verify(CSharpCompilationOptions options, Verification verify, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "run", verify: verify);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerIndirection_ThroughTernary_02()
        {
            var code = @"
using System;

unsafe
{
    int i1 = 0;
    int* p1 = &i1;
    bool b = true;
    ref int x = ref b ? ref *M(*p1) : ref i1;
    Console.WriteLine(""run"");

    int* M(int i)
    {
        Console.Write(i);
        return (int*)0;
    }
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (int V_0, //i1
                int* V_1) //p1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  conv.u
  IL_0005:  stloc.1
  IL_0006:  ldc.i4.1
  IL_0007:  brfalse.s  IL_0011
  IL_0009:  ldloc.1
  IL_000a:  ldind.i4
  IL_000b:  call       ""int* Program.<<Main>$>g__M|0_0(int)""
  IL_0010:  pop
  IL_0011:  ldstr      ""run""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (int V_0, //i1
                int* V_1, //p1
                bool V_2, //b
                int& V_3) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  stloc.1
  IL_0007:  ldc.i4.1
  IL_0008:  stloc.2
  IL_0009:  ldloc.2
  IL_000a:  brtrue.s   IL_0010
  IL_000c:  ldloca.s   V_0
  IL_000e:  br.s       IL_0017
  IL_0010:  ldloc.1
  IL_0011:  ldind.i4
  IL_0012:  call       ""int* Program.<<Main>$>g__M|0_0(int)""
  IL_0017:  stloc.3
  IL_0018:  ldstr      ""run""
  IL_001d:  call       ""void System.Console.WriteLine(string)""
  IL_0022:  nop
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "0run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_01()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C();
        var c2 = new C();
        await Test(false, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);

        System.Console.Write('-');

        c1 = new C();
        c2 = new C();
        await Test(true, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        ((await GetBool(b)) ? ref GetC(c1).F: ref GetC(c2).F) = 123;
    }

    static async Task<bool> GetBool(bool b)
    {
        await Task.Yield();
        return b;
    }
    
    static C GetC(C c) => c;
}
";

            var comp = CompileAndVerify(source, expectedOutput: "0123-1230");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0044
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""bool C.<Test>d__2.b""
    IL_0010:  call       ""System.Threading.Tasks.Task<bool> C.GetBool(bool)""
    IL_0015:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_001a:  stloc.1
    IL_001b:  ldloca.s   V_1
    IL_001d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0022:  brtrue.s   IL_0060
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.0
    IL_0028:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_002d:  ldarg.0
    IL_002e:  ldloc.1
    IL_002f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.0
    IL_003d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Test>d__2)""
    IL_0042:  leave.s    IL_00ba
    IL_0044:  ldarg.0
    IL_0045:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_004a:  stloc.1
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0051:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.0
    IL_005b:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0060:  ldloca.s   V_1
    IL_0062:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0067:  brtrue.s   IL_007b
    IL_0069:  ldarg.0
    IL_006a:  ldfld      ""C C.<Test>d__2.c2""
    IL_006f:  call       ""C C.GetC(C)""
    IL_0074:  ldflda     ""int C.F""
    IL_0079:  br.s       IL_008b
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""C C.<Test>d__2.c1""
    IL_0081:  call       ""C C.GetC(C)""
    IL_0086:  ldflda     ""int C.F""
    IL_008b:  ldc.i4.s   123
    IL_008d:  stind.i4
    IL_008e:  leave.s    IL_00a7
  }
  catch System.Exception
  {
    IL_0090:  stloc.2
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.s   -2
    IL_0094:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_009f:  ldloc.2
    IL_00a0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a5:  leave.s    IL_00ba
  }
  IL_00a7:  ldarg.0
  IL_00a8:  ldc.i4.s   -2
  IL_00aa:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_00af:  ldarg.0
  IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_00b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00ba:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_02()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C();
        var c2 = new C();
        await Test(false, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);

        System.Console.Write('-');

        c1 = new C();
        c2 = new C();
        await Test(true, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        (GetBool(b) ? ref (await GetC1(c1)).F: ref GetC2(c2).F) = 123;
    }

    static bool GetBool(bool b) => b;

    static async Task<C> GetC1(C c)
    {
        await Task.Yield();
        return c;
    }
    
    static C GetC2(C c) => c;
}
";

            var comp = CompileAndVerify(source, expectedOutput: "00-00");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""bool C.<Test>d__2.b""
    IL_0010:  call       ""bool C.GetBool(bool)""
    IL_0015:  brfalse.s  IL_007c
    IL_0017:  ldarg.0
    IL_0018:  ldfld      ""C C.<Test>d__2.c1""
    IL_001d:  call       ""System.Threading.Tasks.Task<C> C.GetC1(C)""
    IL_0022:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0027:  stloc.1
    IL_0028:  ldloca.s   V_1
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_006d
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.1
    IL_003c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0047:  ldloca.s   V_1
    IL_0049:  ldarg.0
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_004f:  leave.s    IL_00b9
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0057:  stloc.1
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_006d:  ldloca.s   V_1
    IL_006f:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0074:  ldfld      ""int C.F""
    IL_0079:  pop
    IL_007a:  br.s       IL_008d
    IL_007c:  ldarg.0
    IL_007d:  ldfld      ""C C.<Test>d__2.c2""
    IL_0082:  call       ""C C.GetC2(C)""
    IL_0087:  ldfld      ""int C.F""
    IL_008c:  pop
    IL_008d:  leave.s    IL_00a6
  }
  catch System.Exception
  {
    IL_008f:  stloc.2
    IL_0090:  ldarg.0
    IL_0091:  ldc.i4.s   -2
    IL_0093:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_009e:  ldloc.2
    IL_009f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a4:  leave.s    IL_00b9
  }
  IL_00a6:  ldarg.0
  IL_00a7:  ldc.i4.s   -2
  IL_00a9:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_00b4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b9:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_03()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C();
        var c2 = new C();
        await Test(false, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);

        System.Console.Write('-');

        c1 = new C();
        c2 = new C();
        await Test(true, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        (GetBool(b) ? ref GetC1(c1).F: ref (await GetC2(c2)).F) = 123;
    }

    static bool GetBool(bool b) => b;

    static C GetC1(C c) => c;

    static async Task<C> GetC2(C c)
    {
        await Task.Yield();
        return c;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "00-00");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""bool C.<Test>d__2.b""
    IL_0010:  call       ""bool C.GetBool(bool)""
    IL_0015:  brfalse.s  IL_002a
    IL_0017:  ldarg.0
    IL_0018:  ldfld      ""C C.<Test>d__2.c1""
    IL_001d:  call       ""C C.GetC1(C)""
    IL_0022:  ldfld      ""int C.F""
    IL_0027:  pop
    IL_0028:  br.s       IL_008d
    IL_002a:  ldarg.0
    IL_002b:  ldfld      ""C C.<Test>d__2.c2""
    IL_0030:  call       ""System.Threading.Tasks.Task<C> C.GetC2(C)""
    IL_0035:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_003a:  stloc.1
    IL_003b:  ldloca.s   V_1
    IL_003d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0042:  brtrue.s   IL_0080
    IL_0044:  ldarg.0
    IL_0045:  ldc.i4.0
    IL_0046:  dup
    IL_0047:  stloc.0
    IL_0048:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_004d:  ldarg.0
    IL_004e:  ldloc.1
    IL_004f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0054:  ldarg.0
    IL_0055:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_005a:  ldloca.s   V_1
    IL_005c:  ldarg.0
    IL_005d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0062:  leave.s    IL_00b9
    IL_0064:  ldarg.0
    IL_0065:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_006a:  stloc.1
    IL_006b:  ldarg.0
    IL_006c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0080:  ldloca.s   V_1
    IL_0082:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0087:  ldfld      ""int C.F""
    IL_008c:  pop
    IL_008d:  leave.s    IL_00a6
  }
  catch System.Exception
  {
    IL_008f:  stloc.2
    IL_0090:  ldarg.0
    IL_0091:  ldc.i4.s   -2
    IL_0093:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0098:  ldarg.0
    IL_0099:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_009e:  ldloc.2
    IL_009f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a4:  leave.s    IL_00b9
  }
  IL_00a6:  ldarg.0
  IL_00a7:  ldc.i4.s   -2
  IL_00a9:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_00b4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b9:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_04()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C();
        var c2 = new C();
        await Test(false, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);

        System.Console.Write('-');

        c1 = new C();
        c2 = new C();
        await Test(true, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        ((await GetBool(b)) ? ref (await GetC1(c1)).F: ref (await GetC2(c2)).F) = 123;
    }

    static async Task<bool> GetBool(bool b)
    {
        await Task.Yield();
        return b;
    }

    static async Task<C> GetC1(C c)
    {
        await Task.Yield();
        return c;
    }

    static async Task<C> GetC2(C c)
    {
        await Task.Yield();
        return c;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "00-00");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      371 (0x173)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_1,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0056,
        IL_00b8,
        IL_011d)
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""bool C.<Test>d__2.b""
    IL_001f:  call       ""System.Threading.Tasks.Task<bool> C.GetBool(bool)""
    IL_0024:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0029:  stloc.1
    IL_002a:  ldloca.s   V_1
    IL_002c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0031:  brtrue.s   IL_0072
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0049:  ldloca.s   V_1
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Test>d__2)""
    IL_0051:  leave      IL_0172
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_005c:  stloc.1
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0072:  ldloca.s   V_1
    IL_0074:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0079:  brfalse.s  IL_00e3
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""C C.<Test>d__2.c1""
    IL_0081:  call       ""System.Threading.Tasks.Task<C> C.GetC1(C)""
    IL_0086:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_008b:  stloc.2
    IL_008c:  ldloca.s   V_2
    IL_008e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0093:  brtrue.s   IL_00d4
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.1
    IL_0097:  dup
    IL_0098:  stloc.0
    IL_0099:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_009e:  ldarg.0
    IL_009f:  ldloc.2
    IL_00a0:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00a5:  ldarg.0
    IL_00a6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00ab:  ldloca.s   V_2
    IL_00ad:  ldarg.0
    IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_00b3:  leave      IL_0172
    IL_00b8:  ldarg.0
    IL_00b9:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00be:  stloc.2
    IL_00bf:  ldarg.0
    IL_00c0:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00c5:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00cb:  ldarg.0
    IL_00cc:  ldc.i4.m1
    IL_00cd:  dup
    IL_00ce:  stloc.0
    IL_00cf:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00d4:  ldloca.s   V_2
    IL_00d6:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00db:  ldfld      ""int C.F""
    IL_00e0:  pop
    IL_00e1:  br.s       IL_0146
    IL_00e3:  ldarg.0
    IL_00e4:  ldfld      ""C C.<Test>d__2.c2""
    IL_00e9:  call       ""System.Threading.Tasks.Task<C> C.GetC2(C)""
    IL_00ee:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_00f3:  stloc.2
    IL_00f4:  ldloca.s   V_2
    IL_00f6:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_00fb:  brtrue.s   IL_0139
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.2
    IL_00ff:  dup
    IL_0100:  stloc.0
    IL_0101:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0106:  ldarg.0
    IL_0107:  ldloc.2
    IL_0108:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_010d:  ldarg.0
    IL_010e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0113:  ldloca.s   V_2
    IL_0115:  ldarg.0
    IL_0116:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_011b:  leave.s    IL_0172
    IL_011d:  ldarg.0
    IL_011e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_0123:  stloc.2
    IL_0124:  ldarg.0
    IL_0125:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_012a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0130:  ldarg.0
    IL_0131:  ldc.i4.m1
    IL_0132:  dup
    IL_0133:  stloc.0
    IL_0134:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0139:  ldloca.s   V_2
    IL_013b:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0140:  ldfld      ""int C.F""
    IL_0145:  pop
    IL_0146:  leave.s    IL_015f
  }
  catch System.Exception
  {
    IL_0148:  stloc.3
    IL_0149:  ldarg.0
    IL_014a:  ldc.i4.s   -2
    IL_014c:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0151:  ldarg.0
    IL_0152:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0157:  ldloc.3
    IL_0158:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_015d:  leave.s    IL_0172
  }
  IL_015f:  ldarg.0
  IL_0160:  ldc.i4.s   -2
  IL_0162:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_0167:  ldarg.0
  IL_0168:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_016d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0172:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_05()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C() { F = 123};
        var c2 = new C() { F = 124};
        await Test(false, c1, c2);

        System.Console.Write('-');

        await Test(true, c1, c2);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        Test(b ? ref (await GetC(c1)).F : ref (await GetC(c2)).F, await GetC(new C()));
    }

    static void Test(int x, C y)
    {
        System.Console.Write(x);
    }

    static async Task<C> GetC(C c)
    {
        await Task.Yield();
        return c;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "124-123");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      405 (0x195)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                C V_2,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_005e,
        IL_00cd,
        IL_0136)
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""bool C.<Test>d__2.b""
    IL_001f:  brfalse.s  IL_0090
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""C C.<Test>d__2.c1""
    IL_0027:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_002c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0031:  stloc.3
    IL_0032:  ldloca.s   V_3
    IL_0034:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0039:  brtrue.s   IL_007a
    IL_003b:  ldarg.0
    IL_003c:  ldc.i4.0
    IL_003d:  dup
    IL_003e:  stloc.0
    IL_003f:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0044:  ldarg.0
    IL_0045:  ldloc.3
    IL_0046:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0051:  ldloca.s   V_3
    IL_0053:  ldarg.0
    IL_0054:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0059:  leave      IL_0194
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0064:  stloc.3
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.m1
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0081:  stloc.2
    IL_0082:  ldarg.0
    IL_0083:  ldloc.2
    IL_0084:  ldfld      ""int C.F""
    IL_0089:  stfld      ""int C.<Test>d__2.<>7__wrap1""
    IL_008e:  br.s       IL_00fd
    IL_0090:  ldarg.0
    IL_0091:  ldfld      ""C C.<Test>d__2.c2""
    IL_0096:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_009b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_00a0:  stloc.3
    IL_00a1:  ldloca.s   V_3
    IL_00a3:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_00a8:  brtrue.s   IL_00e9
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.1
    IL_00ac:  dup
    IL_00ad:  stloc.0
    IL_00ae:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00b3:  ldarg.0
    IL_00b4:  ldloc.3
    IL_00b5:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00c0:  ldloca.s   V_3
    IL_00c2:  ldarg.0
    IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_00c8:  leave      IL_0194
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00da:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.0
    IL_00e4:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00e9:  ldloca.s   V_3
    IL_00eb:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00f0:  stloc.2
    IL_00f1:  ldarg.0
    IL_00f2:  ldloc.2
    IL_00f3:  ldfld      ""int C.F""
    IL_00f8:  stfld      ""int C.<Test>d__2.<>7__wrap1""
    IL_00fd:  newobj     ""C..ctor()""
    IL_0102:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_0107:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_010c:  stloc.3
    IL_010d:  ldloca.s   V_3
    IL_010f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0114:  brtrue.s   IL_0152
    IL_0116:  ldarg.0
    IL_0117:  ldc.i4.2
    IL_0118:  dup
    IL_0119:  stloc.0
    IL_011a:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_011f:  ldarg.0
    IL_0120:  ldloc.3
    IL_0121:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_012c:  ldloca.s   V_3
    IL_012e:  ldarg.0
    IL_012f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0134:  leave.s    IL_0194
    IL_0136:  ldarg.0
    IL_0137:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_013c:  stloc.3
    IL_013d:  ldarg.0
    IL_013e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0143:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0149:  ldarg.0
    IL_014a:  ldc.i4.m1
    IL_014b:  dup
    IL_014c:  stloc.0
    IL_014d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0152:  ldloca.s   V_3
    IL_0154:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0159:  stloc.1
    IL_015a:  ldarg.0
    IL_015b:  ldfld      ""int C.<Test>d__2.<>7__wrap1""
    IL_0160:  ldloc.1
    IL_0161:  call       ""void C.Test(int, C)""
    IL_0166:  leave.s    IL_0181
  }
  catch System.Exception
  {
    IL_0168:  stloc.s    V_4
    IL_016a:  ldarg.0
    IL_016b:  ldc.i4.s   -2
    IL_016d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0172:  ldarg.0
    IL_0173:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0178:  ldloc.s    V_4
    IL_017a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_017f:  leave.s    IL_0194
  }
  IL_0181:  ldarg.0
  IL_0182:  ldc.i4.s   -2
  IL_0184:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_0189:  ldarg.0
  IL_018a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_018f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0194:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_06()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int F;

    static async Task Main()
    {
        var c1 = new C();
        var c2 = new C();
        await Test(false, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);

        System.Console.Write('-');

        c1 = new C();
        c2 = new C();
        await Test(true, c1, c2);
        System.Console.Write(c1.F);
        System.Console.Write(c2.F);
    }

    static async Task Test(bool b, C c1, C c2)
    {
        Test(ref b ? ref (await GetC(c1)).F : ref (await GetC(c2)).F, await GetC(new C()));
    }

    static void Test(ref int x, C y)
    {
        x = 123;
    }

    static async Task<C> GetC(C c)
    {
        await Task.Yield();
        return c;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "00-00");

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      405 (0x195)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                C V_2,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_005e,
        IL_00cd,
        IL_0136)
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""bool C.<Test>d__2.b""
    IL_001f:  brfalse.s  IL_0090
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""C C.<Test>d__2.c1""
    IL_0027:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_002c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0031:  stloc.3
    IL_0032:  ldloca.s   V_3
    IL_0034:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0039:  brtrue.s   IL_007a
    IL_003b:  ldarg.0
    IL_003c:  ldc.i4.0
    IL_003d:  dup
    IL_003e:  stloc.0
    IL_003f:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0044:  ldarg.0
    IL_0045:  ldloc.3
    IL_0046:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0051:  ldloca.s   V_3
    IL_0053:  ldarg.0
    IL_0054:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0059:  leave      IL_0194
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0064:  stloc.3
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.m1
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0081:  stloc.2
    IL_0082:  ldarg.0
    IL_0083:  ldloc.2
    IL_0084:  ldfld      ""int C.F""
    IL_0089:  stfld      ""int C.<Test>d__2.<>7__wrap1""
    IL_008e:  br.s       IL_00fd
    IL_0090:  ldarg.0
    IL_0091:  ldfld      ""C C.<Test>d__2.c2""
    IL_0096:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_009b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_00a0:  stloc.3
    IL_00a1:  ldloca.s   V_3
    IL_00a3:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_00a8:  brtrue.s   IL_00e9
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.1
    IL_00ac:  dup
    IL_00ad:  stloc.0
    IL_00ae:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00b3:  ldarg.0
    IL_00b4:  ldloc.3
    IL_00b5:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00c0:  ldloca.s   V_3
    IL_00c2:  ldarg.0
    IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_00c8:  leave      IL_0194
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00da:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.0
    IL_00e4:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00e9:  ldloca.s   V_3
    IL_00eb:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00f0:  stloc.2
    IL_00f1:  ldarg.0
    IL_00f2:  ldloc.2
    IL_00f3:  ldfld      ""int C.F""
    IL_00f8:  stfld      ""int C.<Test>d__2.<>7__wrap1""
    IL_00fd:  newobj     ""C..ctor()""
    IL_0102:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_0107:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_010c:  stloc.3
    IL_010d:  ldloca.s   V_3
    IL_010f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0114:  brtrue.s   IL_0152
    IL_0116:  ldarg.0
    IL_0117:  ldc.i4.2
    IL_0118:  dup
    IL_0119:  stloc.0
    IL_011a:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_011f:  ldarg.0
    IL_0120:  ldloc.3
    IL_0121:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_012c:  ldloca.s   V_3
    IL_012e:  ldarg.0
    IL_012f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0134:  leave.s    IL_0194
    IL_0136:  ldarg.0
    IL_0137:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_013c:  stloc.3
    IL_013d:  ldarg.0
    IL_013e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0143:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0149:  ldarg.0
    IL_014a:  ldc.i4.m1
    IL_014b:  dup
    IL_014c:  stloc.0
    IL_014d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0152:  ldloca.s   V_3
    IL_0154:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0159:  stloc.1
    IL_015a:  ldarg.0
    IL_015b:  ldflda     ""int C.<Test>d__2.<>7__wrap1""
    IL_0160:  ldloc.1
    IL_0161:  call       ""void C.Test(ref int, C)""
    IL_0166:  leave.s    IL_0181
  }
  catch System.Exception
  {
    IL_0168:  stloc.s    V_4
    IL_016a:  ldarg.0
    IL_016b:  ldc.i4.s   -2
    IL_016d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0172:  ldarg.0
    IL_0173:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0178:  ldloc.s    V_4
    IL_017a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_017f:  leave.s    IL_0194
  }
  IL_0181:  ldarg.0
  IL_0182:  ldc.i4.s   -2
  IL_0184:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_0189:  ldarg.0
  IL_018a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_018f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0194:  ret
}
");
        }
    }
}
