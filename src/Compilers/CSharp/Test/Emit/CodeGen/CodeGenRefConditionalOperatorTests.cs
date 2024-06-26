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
        public void TestRefConditionalWithSpilling()
        {
            var source = """
                class C
                {
                    public static void Main()
                    {
                        string str = "a2";
                        int x = 1;
                        int y = 2;
                        int z = 777;

                        ref int r =
                              ref str is "whatever" ? ref x
                            : ref str is { Length: >= 2 and <= 10 or 22 } ? ref y
                            : ref z;

                        r++;
                        r++;
                        r++;
                        int xxx = r;
                        System.Console.WriteLine(xxx); //5
                        System.Console.WriteLine(x); //1
                        System.Console.WriteLine(y); //expected 5 - but we get 2
                    }
                }
                """;

            var comp = CompileAndVerify(source,
                targetFramework: TargetFramework.NetLatest,
                verify: Verification.Skipped,
                expectedOutput: """
                5
                1
                5
                """);
            comp.VerifyDiagnostics();

            comp.VerifyIL("C.Main", @"
{
  // Code size      122 (0x7a)
  .maxstack  4
  .locals init (string V_0, //str
                int V_1, //x
                int V_2, //y
                int V_3, //z
                int& V_4,
                int V_5,
                bool V_6)
  IL_0000:  ldstr      ""a2""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.2
  IL_0009:  stloc.2
  IL_000a:  ldc.i4     0x309
  IL_000f:  stloc.3
  IL_0010:  ldloc.0
  IL_0011:  ldstr      ""whatever""
  IL_0016:  call       ""bool string.op_Equality(string, string)""
  IL_001b:  brfalse.s  IL_0023
  IL_001d:  ldloca.s   V_1
  IL_001f:  stloc.s    V_4
  IL_0021:  br.s       IL_0053
  IL_0023:  ldloc.0
  IL_0024:  brfalse.s  IL_0044
  IL_0026:  ldloc.0
  IL_0027:  callvirt   ""int string.Length.get""
  IL_002c:  stloc.s    V_5
  IL_002e:  ldloc.s    V_5
  IL_0030:  ldc.i4.2
  IL_0031:  blt.s      IL_0044
  IL_0033:  ldloc.s    V_5
  IL_0035:  ldc.i4.s   10
  IL_0037:  ble.s      IL_003f
  IL_0039:  ldloc.s    V_5
  IL_003b:  ldc.i4.s   22
  IL_003d:  bne.un.s   IL_0044
  IL_003f:  ldc.i4.1
  IL_0040:  stloc.s    V_6
  IL_0042:  br.s       IL_0047
  IL_0044:  ldc.i4.0
  IL_0045:  stloc.s    V_6
  IL_0047:  ldloc.s    V_6
  IL_0049:  brtrue.s   IL_004f
  IL_004b:  ldloca.s   V_3
  IL_004d:  br.s       IL_0051
  IL_004f:  ldloca.s   V_2
  IL_0051:  stloc.s    V_4
  IL_0053:  ldloc.s    V_4
  IL_0055:  dup
  IL_0056:  dup
  IL_0057:  ldind.i4
  IL_0058:  ldc.i4.1
  IL_0059:  add
  IL_005a:  stind.i4
  IL_005b:  dup
  IL_005c:  dup
  IL_005d:  ldind.i4
  IL_005e:  ldc.i4.1
  IL_005f:  add
  IL_0060:  stind.i4
  IL_0061:  dup
  IL_0062:  dup
  IL_0063:  ldind.i4
  IL_0064:  ldc.i4.1
  IL_0065:  add
  IL_0066:  stind.i4
  IL_0067:  ldind.i4
  IL_0068:  call       ""void System.Console.WriteLine(int)""
  IL_006d:  ldloc.1
  IL_006e:  call       ""void System.Console.WriteLine(int)""
  IL_0073:  ldloc.2
  IL_0074:  call       ""void System.Console.WriteLine(int)""
  IL_0079:  ret
}
");
        }

        [Fact]
        public void TestReadonlyRefConditionalWithSpilling()
        {
            var source = """
                string str = "a2";
                int x = 1;
                int y = 2;

                ref readonly int roy = ref x;

                ref readonly int r =
                      ref str is "whatever" ? ref roy
                    : ref str is { Length: >= 2 and <= 10 or 22 } ? ref y
                    : ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();

                ref var castedRef = ref System.Runtime.CompilerServices.Unsafe.AsRef(in r);

                castedRef++;
                castedRef++;
                castedRef++;
                int xxx = r;
                System.Console.WriteLine(xxx); //5
                System.Console.WriteLine(x); //1
                System.Console.WriteLine(y); //expected 5 - but we get 2
                """;

            var comp = CompileAndVerify(source,
                targetFramework: TargetFramework.NetLatest,
                expectedOutput: """
                5
                1
                5
                """);
            comp.VerifyDiagnostics();

            comp.VerifyIL("<top-level-statements-entry-point>", @"
            {
  // Code size      126 (0x7e)
  .maxstack  5
  .locals init (string V_0, //str
                int V_1, //x
                int V_2, //y
                int& V_3, //roy
                int& V_4,
                int V_5,
                bool V_6)
  IL_0000:  ldstr      ""a2""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.2
  IL_0009:  stloc.2
  IL_000a:  ldloca.s   V_1
  IL_000c:  stloc.3
  IL_000d:  ldloc.0
  IL_000e:  ldstr      ""whatever""
  IL_0013:  call       ""bool string.op_Equality(string, string)""
  IL_0018:  brfalse.s  IL_001f
  IL_001a:  ldloc.3
  IL_001b:  stloc.s    V_4
  IL_001d:  br.s       IL_0052
  IL_001f:  ldloc.0
  IL_0020:  brfalse.s  IL_0040
  IL_0022:  ldloc.0
  IL_0023:  callvirt   ""int string.Length.get""
  IL_0028:  stloc.s    V_5
  IL_002a:  ldloc.s    V_5
  IL_002c:  ldc.i4.2
  IL_002d:  blt.s      IL_0040
  IL_002f:  ldloc.s    V_5
  IL_0031:  ldc.i4.s   10
  IL_0033:  ble.s      IL_003b
  IL_0035:  ldloc.s    V_5
  IL_0037:  ldc.i4.s   22
  IL_0039:  bne.un.s   IL_0040
  IL_003b:  ldc.i4.1
  IL_003c:  stloc.s    V_6
  IL_003e:  br.s       IL_0043
  IL_0040:  ldc.i4.0
  IL_0041:  stloc.s    V_6
  IL_0043:  ldloc.s    V_6
  IL_0045:  brtrue.s   IL_004e
  IL_0047:  call       ""ref int System.Runtime.CompilerServices.Unsafe.NullRef<int>()""
  IL_004c:  br.s       IL_0050
  IL_004e:  ldloca.s   V_2
  IL_0050:  stloc.s    V_4
  IL_0052:  ldloc.s    V_4
  IL_0054:  dup
  IL_0055:  call       ""ref int System.Runtime.CompilerServices.Unsafe.AsRef<int>(scoped in int)""
  IL_005a:  dup
  IL_005b:  dup
  IL_005c:  ldind.i4
  IL_005d:  ldc.i4.1
  IL_005e:  add
  IL_005f:  stind.i4
  IL_0060:  dup
  IL_0061:  dup
  IL_0062:  ldind.i4
  IL_0063:  ldc.i4.1
  IL_0064:  add
  IL_0065:  stind.i4
  IL_0066:  dup
  IL_0067:  ldind.i4
  IL_0068:  ldc.i4.1
  IL_0069:  add
  IL_006a:  stind.i4
  IL_006b:  ldind.i4
  IL_006c:  call       ""void System.Console.WriteLine(int)""
  IL_0071:  ldloc.1
  IL_0072:  call       ""void System.Console.WriteLine(int)""
  IL_0077:  ldloc.2
  IL_0078:  call       ""void System.Console.WriteLine(int)""
  IL_007d:  ret
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
    }
}
