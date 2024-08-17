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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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
            var comp = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe);

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

            var comp = CreateCompilationWithMscorlib461(source, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: TestOptions.ReleaseExe);

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

            var comp = CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.DebugExe);

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

            var comp = CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.DebugExe);

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      209 (0xd1)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""bool C.<Test>d__2.b""
    IL_0011:  call       ""bool C.GetBool(bool)""
    IL_0016:  stfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0021:  brfalse.s  IL_0081
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""C C.<Test>d__2.c1""
    IL_0029:  call       ""System.Threading.Tasks.Task<C> C.GetC1(C)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0033:  stloc.2
    IL_0034:  ldloca.s   V_2
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.2
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0053:  ldloca.s   V_2
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_005b:  leave.s    IL_00d0
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0063:  stloc.2
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0079:  ldloca.s   V_2
    IL_007b:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0080:  stloc.1
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0087:  brtrue.s   IL_009b
    IL_0089:  ldarg.0
    IL_008a:  ldfld      ""C C.<Test>d__2.c2""
    IL_008f:  call       ""C C.GetC2(C)""
    IL_0094:  ldflda     ""int C.F""
    IL_0099:  br.s       IL_00a1
    IL_009b:  ldloc.1
    IL_009c:  ldflda     ""int C.F""
    IL_00a1:  ldc.i4.s   123
    IL_00a3:  stind.i4
    IL_00a4:  leave.s    IL_00bd
  }
  catch System.Exception
  {
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00b5:  ldloc.3
    IL_00b6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00bb:  leave.s    IL_00d0
  }
  IL_00bd:  ldarg.0
  IL_00be:  ldc.i4.s   -2
  IL_00c0:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_00c5:  ldarg.0
  IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_00cb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00d0:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.DebugExe);

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      209 (0xd1)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""bool C.<Test>d__2.b""
    IL_0011:  call       ""bool C.GetBool(bool)""
    IL_0016:  stfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0021:  brtrue.s   IL_0081
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""C C.<Test>d__2.c2""
    IL_0029:  call       ""System.Threading.Tasks.Task<C> C.GetC2(C)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0033:  stloc.2
    IL_0034:  ldloca.s   V_2
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.2
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0053:  ldloca.s   V_2
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_005b:  leave.s    IL_00d0
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0063:  stloc.2
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0079:  ldloca.s   V_2
    IL_007b:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0080:  stloc.1
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0087:  brtrue.s   IL_0091
    IL_0089:  ldloc.1
    IL_008a:  ldflda     ""int C.F""
    IL_008f:  br.s       IL_00a1
    IL_0091:  ldarg.0
    IL_0092:  ldfld      ""C C.<Test>d__2.c1""
    IL_0097:  call       ""C C.GetC1(C)""
    IL_009c:  ldflda     ""int C.F""
    IL_00a1:  ldc.i4.s   123
    IL_00a3:  stind.i4
    IL_00a4:  leave.s    IL_00bd
  }
  catch System.Exception
  {
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00b5:  ldloc.3
    IL_00b6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00bb:  leave.s    IL_00d0
  }
  IL_00bd:  ldarg.0
  IL_00be:  ldc.i4.s   -2
  IL_00c0:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_00c5:  ldarg.0
  IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_00cb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00d0:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "0123-1230", options: TestOptions.DebugExe);

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      439 (0x1b7)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0056,
        IL_00c6,
        IL_012e)
    IL_0019:  ldarg.0
    IL_001a:  ldfld      ""bool C.<Test>d__2.b""
    IL_001f:  call       ""System.Threading.Tasks.Task<bool> C.GetBool(bool)""
    IL_0024:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0029:  stloc.2
    IL_002a:  ldloca.s   V_2
    IL_002c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0031:  brtrue.s   IL_0072
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.2
    IL_003e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0049:  ldloca.s   V_2
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<Test>d__2)""
    IL_0051:  leave      IL_01b6
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_005c:  stloc.2
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<Test>d__2.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0072:  ldloca.s   V_2
    IL_0074:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0079:  stloc.1
    IL_007a:  ldarg.0
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0087:  brfalse.s  IL_00f1
    IL_0089:  ldarg.0
    IL_008a:  ldfld      ""C C.<Test>d__2.c1""
    IL_008f:  call       ""System.Threading.Tasks.Task<C> C.GetC1(C)""
    IL_0094:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0099:  stloc.3
    IL_009a:  ldloca.s   V_3
    IL_009c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_00a1:  brtrue.s   IL_00e2
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.1
    IL_00a5:  dup
    IL_00a6:  stloc.0
    IL_00a7:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00ac:  ldarg.0
    IL_00ad:  ldloc.3
    IL_00ae:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00b9:  ldloca.s   V_3
    IL_00bb:  ldarg.0
    IL_00bc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_00c1:  leave      IL_01b6
    IL_00c6:  ldarg.0
    IL_00c7:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00cc:  stloc.3
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_00d3:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00d9:  ldarg.0
    IL_00da:  ldc.i4.m1
    IL_00db:  dup
    IL_00dc:  stloc.0
    IL_00dd:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00e2:  ldarg.0
    IL_00e3:  ldloca.s   V_3
    IL_00e5:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00ea:  stfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_00ef:  br.s       IL_0157
    IL_00f1:  ldarg.0
    IL_00f2:  ldfld      ""C C.<Test>d__2.c2""
    IL_00f7:  call       ""System.Threading.Tasks.Task<C> C.GetC2(C)""
    IL_00fc:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0101:  stloc.3
    IL_0102:  ldloca.s   V_3
    IL_0104:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0109:  brtrue.s   IL_014a
    IL_010b:  ldarg.0
    IL_010c:  ldc.i4.2
    IL_010d:  dup
    IL_010e:  stloc.0
    IL_010f:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0114:  ldarg.0
    IL_0115:  ldloc.3
    IL_0116:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_011b:  ldarg.0
    IL_011c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0121:  ldloca.s   V_3
    IL_0123:  ldarg.0
    IL_0124:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0129:  leave      IL_01b6
    IL_012e:  ldarg.0
    IL_012f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_0134:  stloc.3
    IL_0135:  ldarg.0
    IL_0136:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__2""
    IL_013b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0141:  ldarg.0
    IL_0142:  ldc.i4.m1
    IL_0143:  dup
    IL_0144:  stloc.0
    IL_0145:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_014a:  ldarg.0
    IL_014b:  ldloca.s   V_3
    IL_014d:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0152:  stfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_0157:  ldarg.0
    IL_0158:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_015d:  brtrue.s   IL_016c
    IL_015f:  ldarg.0
    IL_0160:  ldfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_0165:  ldflda     ""int C.F""
    IL_016a:  br.s       IL_0177
    IL_016c:  ldarg.0
    IL_016d:  ldfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_0172:  ldflda     ""int C.F""
    IL_0177:  ldc.i4.s   123
    IL_0179:  stind.i4
    IL_017a:  ldarg.0
    IL_017b:  ldnull
    IL_017c:  stfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_0181:  ldarg.0
    IL_0182:  ldnull
    IL_0183:  stfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_0188:  leave.s    IL_01a3
  }
  catch System.Exception
  {
    IL_018a:  stloc.s    V_4
    IL_018c:  ldarg.0
    IL_018d:  ldc.i4.s   -2
    IL_018f:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0194:  ldarg.0
    IL_0195:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_019a:  ldloc.s    V_4
    IL_019c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01a1:  leave.s    IL_01b6
  }
  IL_01a3:  ldarg.0
  IL_01a4:  ldc.i4.s   -2
  IL_01a6:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_01ab:  ldarg.0
  IL_01ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_01b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01b6:  ret
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

            var comp = CompileAndVerify(source, expectedOutput: "124-123", options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: "124-123", options: TestOptions.DebugExe);

            comp.VerifyIL("C.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      453 (0x1c5)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_006a,
        IL_00d2,
        IL_015a)
    IL_0019:  ldarg.0
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""bool C.<Test>d__2.b""
    IL_0020:  stfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0025:  ldarg.0
    IL_0026:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_002b:  brfalse.s  IL_0095
    IL_002d:  ldarg.0
    IL_002e:  ldfld      ""C C.<Test>d__2.c1""
    IL_0033:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_0038:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_003d:  stloc.2
    IL_003e:  ldloca.s   V_2
    IL_0040:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0045:  brtrue.s   IL_0086
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.0
    IL_0049:  dup
    IL_004a:  stloc.0
    IL_004b:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0050:  ldarg.0
    IL_0051:  ldloc.2
    IL_0052:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0057:  ldarg.0
    IL_0058:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_005d:  ldloca.s   V_2
    IL_005f:  ldarg.0
    IL_0060:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0065:  leave      IL_01c4
    IL_006a:  ldarg.0
    IL_006b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0070:  stloc.2
    IL_0071:  ldarg.0
    IL_0072:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0077:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_007d:  ldarg.0
    IL_007e:  ldc.i4.m1
    IL_007f:  dup
    IL_0080:  stloc.0
    IL_0081:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0086:  ldarg.0
    IL_0087:  ldloca.s   V_2
    IL_0089:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_008e:  stfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_0093:  br.s       IL_00fb
    IL_0095:  ldarg.0
    IL_0096:  ldfld      ""C C.<Test>d__2.c2""
    IL_009b:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_00a0:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_00a5:  stloc.2
    IL_00a6:  ldloca.s   V_2
    IL_00a8:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_00ad:  brtrue.s   IL_00ee
    IL_00af:  ldarg.0
    IL_00b0:  ldc.i4.1
    IL_00b1:  dup
    IL_00b2:  stloc.0
    IL_00b3:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldloc.2
    IL_00ba:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00bf:  ldarg.0
    IL_00c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_00c5:  ldloca.s   V_2
    IL_00c7:  ldarg.0
    IL_00c8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_00cd:  leave      IL_01c4
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00d8:  stloc.2
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_00df:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00e5:  ldarg.0
    IL_00e6:  ldc.i4.m1
    IL_00e7:  dup
    IL_00e8:  stloc.0
    IL_00e9:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_00ee:  ldarg.0
    IL_00ef:  ldloca.s   V_2
    IL_00f1:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00f6:  stfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_00fb:  ldarg.0
    IL_00fc:  ldarg.0
    IL_00fd:  ldfld      ""bool C.<Test>d__2.<>7__wrap1""
    IL_0102:  brtrue.s   IL_0111
    IL_0104:  ldarg.0
    IL_0105:  ldfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_010a:  ldfld      ""int C.F""
    IL_010f:  br.s       IL_011c
    IL_0111:  ldarg.0
    IL_0112:  ldfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_0117:  ldfld      ""int C.F""
    IL_011c:  stfld      ""int C.<Test>d__2.<>7__wrap4""
    IL_0121:  newobj     ""C..ctor()""
    IL_0126:  call       ""System.Threading.Tasks.Task<C> C.GetC(C)""
    IL_012b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0130:  stloc.2
    IL_0131:  ldloca.s   V_2
    IL_0133:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0138:  brtrue.s   IL_0176
    IL_013a:  ldarg.0
    IL_013b:  ldc.i4.2
    IL_013c:  dup
    IL_013d:  stloc.0
    IL_013e:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0143:  ldarg.0
    IL_0144:  ldloc.2
    IL_0145:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_014a:  ldarg.0
    IL_014b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_0150:  ldloca.s   V_2
    IL_0152:  ldarg.0
    IL_0153:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<Test>d__2)""
    IL_0158:  leave.s    IL_01c4
    IL_015a:  ldarg.0
    IL_015b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0160:  stloc.2
    IL_0161:  ldarg.0
    IL_0162:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<Test>d__2.<>u__1""
    IL_0167:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_016d:  ldarg.0
    IL_016e:  ldc.i4.m1
    IL_016f:  dup
    IL_0170:  stloc.0
    IL_0171:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_0176:  ldloca.s   V_2
    IL_0178:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_017d:  stloc.1
    IL_017e:  ldarg.0
    IL_017f:  ldfld      ""int C.<Test>d__2.<>7__wrap4""
    IL_0184:  ldloc.1
    IL_0185:  call       ""void C.Test(int, C)""
    IL_018a:  ldarg.0
    IL_018b:  ldnull
    IL_018c:  stfld      ""C C.<Test>d__2.<>7__wrap2""
    IL_0191:  ldarg.0
    IL_0192:  ldnull
    IL_0193:  stfld      ""C C.<Test>d__2.<>7__wrap3""
    IL_0198:  leave.s    IL_01b1
  }
  catch System.Exception
  {
    IL_019a:  stloc.3
    IL_019b:  ldarg.0
    IL_019c:  ldc.i4.s   -2
    IL_019e:  stfld      ""int C.<Test>d__2.<>1__state""
    IL_01a3:  ldarg.0
    IL_01a4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
    IL_01a9:  ldloc.3
    IL_01aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01af:  leave.s    IL_01c4
  }
  IL_01b1:  ldarg.0
  IL_01b2:  ldc.i4.s   -2
  IL_01b4:  stfld      ""int C.<Test>d__2.<>1__state""
  IL_01b9:  ldarg.0
  IL_01ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Test>d__2.<>t__builder""
  IL_01bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01c4:  ret
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
        Test(ref b ? ref c1.F : ref c2.F, await GetC(new C()));
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

            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (27,18): error CS8325: 'await' cannot be used in an expression containing a ref conditional operator
                //         Test(ref b ? ref (await GetC(c1)).F : ref (await GetC(c2)).F, await GetC(new C()));
                Diagnostic(ErrorCode.ERR_RefConditionalAndAwait, "b ? ref (await GetC(c1)).F : ref (await GetC(c2)).F").WithLocation(27, 18),
                // (28,18): error CS8325: 'await' cannot be used in an expression containing a ref conditional operator
                //         Test(ref b ? ref c1.F : ref c2.F, await GetC(new C()));
                Diagnostic(ErrorCode.ERR_RefConditionalAndAwait, "b ? ref c1.F : ref c2.F").WithLocation(28, 18)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void AwaitInRefConditional_07()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class C
{
    static void Main()
    {
        _ = NumberBuggy().Result;
        Console.WriteLine(""--------"");
        _ = NumberNotBuggy();
    }

    public static async Task<int> NumberBuggy()
    {
        string str = ""a2"";
        int x = 1;
        int y = 2;

        ref int r =
              ref await EvalAsync(str, 1) is ""a1"" ? ref x
            : ref await EvalAsync(str, 2) is ""a2"" ? ref y
            : ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();
        
        r++;
        r++;
        r++;
        int xxx = r;
        System.Console.WriteLine(xxx);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y); //should be 5 now!
        return xxx;
    }
    
    public static ValueTask<int> NumberNotBuggy()
    {
        string str = ""a2"";
        int x = 1;
        int y = 2;

        ref int r =
              ref EvalAsync(str, 1).Result is ""a1"" ? ref x
            : ref EvalAsync(str, 2).Result is ""a2"" ? ref y
            : ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();
        
        r++;
        r++;
        r++;
        int xxx = r;
        System.Console.WriteLine(xxx);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
        return new(xxx);
    }
    static ValueTask<string> EvalAsync(string s, int i)
    {
        System.Console.WriteLine($""{s} {i}"");
        return ValueTask.FromResult(s);
    }
}
";
            var expectedOutput = @"
a2 1
a2 2
5
1
5
--------
a2 1
a2 2
5
1
5
";
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.ReleaseExe, verify: Verification.FailsPEVerify);
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.DebugExe, verify: Verification.FailsPEVerify);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void SpillingInRefConditional_01()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        string str = ""a2"";
        int x = 1;
        int y = 2;

        ref int r =
              ref str is ""Hallo"" ? ref x
            : ref str is { Length: >= 2 and <= 10 or 22 } ? ref y
            : ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();

        r++;
        r++;
        r++;
        int xxx = r;
        System.Console.WriteLine(xxx);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}
";
            var expectedOutput = @"
5
1
5
";
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.ReleaseExe, verify: Verification.FailsPEVerify);
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.DebugExe, verify: Verification.FailsPEVerify);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74115")]
        public void SpillingInRefConditional_02()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        string str = ""a2"";
        int x = 1;
        int y = 2;

        ref readonly var xx = ref x; 

        ref readonly int r =
              ref Eval(str, 1) is ""Hallo"" ? ref xx
            : ref Eval(str, 2) is { Length: >= 2 and <= 10 or 22 } ? ref y
            : ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();

        ref var rx = ref System.Runtime.CompilerServices.Unsafe.AsRef(in r);

        rx++;
        rx++;
        rx++;
        int xxx = r;
        System.Console.WriteLine(xxx);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }

    static T Eval<T>(T s, int i)
    {
        System.Console.WriteLine($""{s} {i}"");
        return s;
    }
}
";
            var expectedOutput = @"
a2 1
a2 2
5
1
5
";
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.ReleaseExe, verify: Verification.FailsPEVerify);
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null, options: TestOptions.DebugExe, verify: Verification.FailsPEVerify);
        }
    }
}
