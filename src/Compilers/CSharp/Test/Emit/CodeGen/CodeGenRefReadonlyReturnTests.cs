// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class CodeGenRefReadOnlyReturnTests : CompilingTestBase
    {
        [Fact]
        public void RefReadonlyLocalToField()
        {
            var source = @"
struct S
{
    public int X;
    public S(int x) => X = x;

    public void AddOne() => this.X++;
}

readonly struct S2
{
    public readonly int X;
    public S2(int x) => X = x;

    public void AddOne() { }
}

class C
{
    static S s1 = new S(0);
    readonly static S s2 = new S(0);

    static S2 s3 = new S2(0);
    readonly S2 s4 = new S2(0);

    ref readonly S M()
    {
        ref readonly S rs1 = ref s1;
        rs1.AddOne();
        ref readonly S rs2 = ref s2;
        rs2.AddOne();

        ref readonly S2 rs3 = ref s3;
        rs3.AddOne();
        ref readonly S2 rs4 = ref s4;
        rs4.AddOne();

        return ref rs1;
    }
}";

            // WithPEVerifyCompatFeature should not cause us to get a ref of a temp in ref assignments
            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Fails);
            comp.VerifyIL("C.M", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldsflda    ""S C.s1""
  IL_0005:  dup
  IL_0006:  ldobj      ""S""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""void S.AddOne()""
  IL_0013:  ldsflda    ""S C.s2""
  IL_0018:  ldobj      ""S""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""void S.AddOne()""
  IL_0025:  ldsflda    ""S2 C.s3""
  IL_002a:  call       ""void S2.AddOne()""
  IL_002f:  ldarg.0
  IL_0030:  ldflda     ""S2 C.s4""
  IL_0035:  call       ""void S2.AddOne()""
  IL_003a:  ret
}");

            comp = CompileAndVerify(source, verify: Verification.Fails);
            comp.VerifyIL("C.M", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldsflda    ""S C.s1""
  IL_0005:  dup
  IL_0006:  ldobj      ""S""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""void S.AddOne()""
  IL_0013:  ldsflda    ""S C.s2""
  IL_0018:  ldobj      ""S""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""void S.AddOne()""
  IL_0025:  ldsflda    ""S2 C.s3""
  IL_002a:  call       ""void S2.AddOne()""
  IL_002f:  ldarg.0
  IL_0030:  ldflda     ""S2 C.s4""
  IL_0035:  call       ""void S2.AddOne()""
  IL_003a:  ret
}");
        }

        [Fact]
        public void CallsOnRefReadonlyCopyReceiver()
        {
            var comp = CompileAndVerify(@"
using System;

struct S
{
    public int X;
    public S(int x) => X = x;

    public void AddOne() => this.X++;
}

class C
{
    public static void Main()
    {
        S s = new S(0);
        ref readonly S rs = ref s;
        Console.WriteLine(rs.X);
        rs.AddOne();
        Console.WriteLine(rs.X);
        rs.AddOne();
        rs.AddOne();
        rs.AddOne();
    }
}", expectedOutput: @"0
0");
            comp.VerifyIL("C.Main", @"
{
  // Code size       88 (0x58)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  dup
  IL_000b:  ldfld      ""int S.X""
  IL_0010:  call       ""void System.Console.WriteLine(int)""
  IL_0015:  dup
  IL_0016:  ldobj      ""S""
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       ""void S.AddOne()""
  IL_0023:  dup
  IL_0024:  ldfld      ""int S.X""
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  dup
  IL_002f:  ldobj      ""S""
  IL_0034:  stloc.1
  IL_0035:  ldloca.s   V_1
  IL_0037:  call       ""void S.AddOne()""
  IL_003c:  dup
  IL_003d:  ldobj      ""S""
  IL_0042:  stloc.1
  IL_0043:  ldloca.s   V_1
  IL_0045:  call       ""void S.AddOne()""
  IL_004a:  ldobj      ""S""
  IL_004f:  stloc.1
  IL_0050:  ldloca.s   V_1
  IL_0052:  call       ""void S.AddOne()""
  IL_0057:  ret
}");
            // This should generate similar IL to the previous
            comp = CompileAndVerify(@"
using System;

struct S
{
    public int X;
    public S(int x) => X = x;

    public void AddOne() => this.X++;
}

class C
{
    public static void Main()
    {
        S s = new S(0);
        ref S sr = ref s;
        var temp = sr;
        temp.AddOne();
        Console.WriteLine(temp.X);
        temp = sr;
        temp.AddOne();
        Console.WriteLine(temp.X);
    }
}", expectedOutput: @"1
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (S V_0, //s
                S V_1) //temp
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  dup
  IL_000b:  ldobj      ""S""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""void S.AddOne()""
  IL_0018:  ldloc.1
  IL_0019:  ldfld      ""int S.X""
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ldobj      ""S""
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""void S.AddOne()""
  IL_0030:  ldloc.1
  IL_0031:  ldfld      ""int S.X""
  IL_0036:  call       ""void System.Console.WriteLine(int)""
  IL_003b:  ret
}");
        }

        [Fact]
        public void RefReadOnlyParamCopyReceiver()
        {
            var comp = CompileAndVerify(@"
using System;

struct S
{
    public int X;
    public S(int x) => X = x;

    public void AddOne() => this.X++;
}

class C
{
    public static void Main()
    {
        M(new S(0));
    }
    static void M(in S rs)
    {
        Console.WriteLine(rs.X);
        rs.AddOne();
        Console.WriteLine(rs.X);
    }
}", expectedOutput: @"0
0");
            comp.VerifyIL(@"C.M", @"
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S.X""
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  ldarg.0
  IL_000c:  ldobj      ""S""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""void S.AddOne()""
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""int S.X""
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  ret
}");
        }

        [Fact]
        public void CarryThroughLifetime()
        {
            var comp = CompileAndVerify(@"
class C
{
    static ref readonly int M(ref int p)
    {
        ref readonly int rp = ref p;
        return ref rp;
    }
}", verify: Verification.Fails);
            comp.VerifyIL("C.M", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void TempForReadonly()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        void L(in int p)
        {
            Console.WriteLine(p);
        }
        for (int i = 0; i < 3; i++)
        {
            L(10);
            L(i);
        }
    }
}", expectedOutput: @"10
0
10
1
10
2");
            comp.VerifyIL("C.Main()", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int V_0, //i
                int V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0019
  IL_0004:  ldc.i4.s   10
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void C.<Main>g__L|0_0(in int)""
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""void C.<Main>g__L|0_0(in int)""
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  add
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.3
  IL_001b:  blt.s      IL_0004
  IL_001d:  ret
}");
        }

        [Fact]
        public void RefReturnAssign()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void M()
    {
        ref readonly int x = ref Helper();
        int y = x + 1;
    }

    static ref readonly int Helper()
        => ref (new int[1])[0];
}");
            verifier.VerifyIL("C.M()", """
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  call       "ref readonly int C.Helper()"
  IL_0005:  ldind.i4
  IL_0006:  pop
  IL_0007:  ret
}
""");
        }

        [Fact]
        public void RefReturnAssign2()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void M()
    {
        ref readonly int x = ref Helper();
        int y = x + 1;
    }

    static ref int Helper()
        => ref (new int[1])[0];
}");
            verifier.VerifyIL("C.M()", """
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  call       "ref int C.Helper()"
  IL_0005:  ldind.i4
  IL_0006:  pop
  IL_0007:  ret
}
""");
        }

        [Fact]
        public void RefReturnAssign3()
        {
            var verifier = CompileAndVerify(@"
try
{
    C.M();
}
catch (System.NullReferenceException)
{
    System.Console.WriteLine(""NullReferenceException"");
}

class C
{
    public static void M()
    {
        ref readonly int x = ref Helper();
        ref readonly int y = ref Helper();
        _ = x + y;
    }

    static unsafe ref int Helper()
        => ref *(int*)0;
}", options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "NullReferenceException");

            verifier.VerifyIL("C.M()", """
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (int& V_0) //y
  IL_0000:  call       "ref int C.Helper()"
  IL_0005:  call       "ref int C.Helper()"
  IL_000a:  stloc.0
  IL_000b:  ldind.i4
  IL_000c:  pop
  IL_000d:  ldloc.0
  IL_000e:  ldind.i4
  IL_000f:  pop
  IL_0010:  ret
}
""");
        }

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
                // (6,9): error CS8331: Cannot assign to method 'M' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M() = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "M").WithLocation(6, 9),
                // (7,9): error CS8332: Cannot assign to a member of method 'M1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M1().Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "M1").WithLocation(7, 9),
                // (9,9): error CS8331: Cannot assign to method 'M' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M() ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "M").WithLocation(9, 9),
                // (10,9): error CS8332: Cannot assign to a member of method 'M1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M1().Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "M1").WithLocation(10, 9),
                // (12,9): error CS8331: Cannot assign to method 'M' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M() += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "M").WithLocation(12, 9),
                // (13,9): error CS8332: Cannot assign to a member of method 'M1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         M1().Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "M1").WithLocation(13, 9)
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
                // (6,9): error CS8331: Cannot assign to property 'P' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "P").WithLocation(6, 9),
                // (7,9): error CS8332: Cannot assign to a member of property 'P1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P1.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "P1").WithLocation(7, 9),
                // (9,9): error CS8331: Cannot assign to property 'P' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "P").WithLocation(9, 9),
                // (10,9): error CS8332: Cannot assign to a member of property 'P1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P1.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "P1").WithLocation(10, 9),
                // (12,9): error CS8331: Cannot assign to property 'P' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "P").WithLocation(12, 9),
                // (13,9): error CS8332: Cannot assign to a member of property 'P1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         P1.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "P1").WithLocation(13, 9)
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
                // (6,25): error CS8329: Cannot use method 'M' as a ref or out value because it is a readonly variable
                //         ref var y = ref M();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M()").WithArguments("method", "M").WithLocation(6, 25),
                // (7,25): error CS0119: 'Program.M1()' is a method, which is not valid in the given context
                //         ref int a = ref M1.Alice;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "M1").WithArguments("Program.M1()", "method").WithLocation(7, 25),
                // (8,26): error CS8329: Cannot use property 'P' as a ref or out value because it is a readonly variable
                //         ref var y1 = ref P;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "P").WithArguments("property", "P").WithLocation(8, 26),
                // (9,26): error CS8330: Members of property 'P1' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a1 = ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "P1.Alice").WithArguments("property", "P1").WithLocation(9, 26)
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
                // (6,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* a = & M();
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& M()").WithLocation(6, 18),
                // (7,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* b = & M1().Alice;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& M1().Alice").WithLocation(7, 18),
                // (9,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* a1 = & P;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& P").WithLocation(9, 19),
                // (10,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* b2 = & P1.Alice;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& P1.Alice").WithLocation(10, 19)
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
                // (12,28): error CS8333: Cannot return method 'M' by writable reference because it is a readonly variable
                //                 return ref M();
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "M()").WithArguments("method", "M").WithLocation(12, 28),
                // (16,28): error CS8334: Members of method 'M1' cannot be returned by writable reference because it is a readonly variable
                //                 return ref M1().Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "M1().Alice").WithArguments("method", "M1").WithLocation(16, 28),
                // (23,28): error CS8333: Cannot return property 'P' by writable reference because it is a readonly variable
                //                 return ref P;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "P").WithArguments("property", "P").WithLocation(23, 28),
                // (27,28): error CS8334: Members of property 'P1' cannot be returned by writable reference because it is a readonly variable
                //                 return ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "P1.Alice").WithArguments("property", "P1").WithLocation(27, 28)
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

            var comp = CompileAndVerifyWithMscorlib40(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: Verification.Passes);

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
  IL_000e:  call       ""ref readonly System.ValueTuple<int, int> Program.M1()""
  IL_0013:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0018:  ret
  IL_0019:  ldloc.0
  IL_001a:  brfalse.s  IL_0022
  IL_001c:  call       ""ref readonly int Program.P.get""
  IL_0021:  ret
  IL_0022:  call       ""ref readonly System.ValueTuple<int, int> Program.P1.get""
  IL_0027:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_002c:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp = CompileAndVerifyWithMscorlib40(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: Verification.Fails);

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
  IL_000f:  ldsflda    ""System.ValueTuple<int, int> Program.F1""
  IL_0014:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0019:  ret
  IL_001a:  ldloc.0
  IL_001b:  brfalse.s  IL_0029
  IL_001d:  ldarg.0
  IL_001e:  ldflda     ""Program.S Program.S1""
  IL_0023:  ldflda     ""int Program.S.F""
  IL_0028:  ret
  IL_0029:  ldsflda    ""Program.S Program.S2""
  IL_002e:  ldflda     ""System.ValueTuple<int, int> Program.S.F1""
  IL_0033:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0038:  ret
}");

            // WithPEVerifyCompatFeature should not cause us to get a ref of a temp in ref returns
            comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(), verify: Verification.Fails, targetFramework: TargetFramework.Mscorlib40);
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
  IL_000f:  ldsflda    ""System.ValueTuple<int, int> Program.F1""
  IL_0014:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0019:  ret
  IL_001a:  ldloc.0
  IL_001b:  brfalse.s  IL_0029
  IL_001d:  ldarg.0
  IL_001e:  ldflda     ""Program.S Program.S1""
  IL_0023:  ldflda     ""int Program.S.F""
  IL_0028:  ret
  IL_0029:  ldsflda    ""Program.S Program.S2""
  IL_002e:  ldflda     ""System.ValueTuple<int, int> Program.S.F1""
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

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (11,30): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(11, 30),
                // (11,24): error CS8347: Cannot use a result of 'Program.M(ref int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M(ref local)").WithArguments("Program.M(ref int)", "x").WithLocation(11, 24),
                // (15,31): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(15, 31),
                // (15,24): error CS8348: Cannot use a member of result of 'Program.M1(out int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_EscapeCall2, "M1(out local)").WithArguments("Program.M1(out int)", "x").WithLocation(15, 24)
            );

            comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (11,30): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(11, 30),
                // (11,24): error CS8347: Cannot use a result of 'Program.M(ref int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M(ref local)").WithArguments("Program.M(ref int)", "x").WithLocation(11, 24));
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
                // (8,20): error CS8521: Cannot use a result of 'Program.this[in int]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref this[local];
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[local]").WithArguments("Program.this[in int]", "x").WithLocation(8, 20)
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
                // (6,20): error CS8521: Cannot use a result of 'Program.this[in int]' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref this[42];
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[42]").WithArguments("Program.this[in int]", "x").WithLocation(6, 20)
            );
        }

        [WorkItem(19930, "https://github.com/dotnet/roslyn/issues/19930")]
        [Fact]
        public void ReadonlyReturnByRefInStruct()
        {
            var text = @"
struct S1
{
    readonly int x;

    ref readonly S1 Test()
    {
        return ref this;
    }

    ref readonly int this[in int i] => ref x;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (8,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(8, 20),
                // (11,44): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     in int this[in int i] => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithLocation(11, 44)
            );
        }

        [WorkItem(19930, "https://github.com/dotnet/roslyn/issues/19930")]
        [Fact]
        public void ReadonlyReturnByRefRValue()
        {
            var text = @"
struct S1
{
    ref readonly int Test()
    {
        return ref 42;
    }
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref 42;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(6, 20)
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
                // (6,20): error CS8521: Cannot use a result of 'Program.M(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref M(42);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M(42)").WithArguments("Program.M(in int)", "x").WithLocation(6, 20)
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
                // (6,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref M();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M()").WithLocation(6, 20),
                // (6,20): error CS8347: Cannot use a result of 'Program.M(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref M();
                Diagnostic(ErrorCode.ERR_EscapeCall, "M()").WithArguments("Program.M(in int)", "x").WithLocation(6, 20)
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
                // (7,20): error CS8521: Cannot use a result of 'Program.M(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ref M(b);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M(b)").WithArguments("Program.M(in int)", "x").WithLocation(7, 20)
            );
        }

        [Fact]
        public void RefReturnThrow()
        {
            var text = @"
class Program
{
    static ref readonly int M1() => throw null;
    static ref int M2() => throw null;
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular);

            comp.VerifyIL("Program.M1()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  throw
}");

            comp.VerifyIL("Program.M2()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  throw
}");
        }

        [Fact]
        public void RefExtensionMethod_PassThrough_LocalNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref int M(ref this int p) => ref p;
}
class Test
{
    void M()
    {
        int x = 5;
        x.M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""ref int Ext.M(ref int)""
  IL_0009:  pop
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefExtensionMethod_PassThrough_FieldNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref int M(ref this int p) => ref p;
}
class Test
{
    private int x = 5;
    void M()
    {
        x.M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""int Test.x""
  IL_0006:  call       ""ref int Ext.M(ref int)""
  IL_000b:  pop
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefExtensionMethod_PassThrough_ChainNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref int M(ref this int p) => ref p;
}
class Test
{
    private int x = 5;
    void M()
    {
        x.M().M().M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""int Test.x""
  IL_0006:  call       ""ref int Ext.M(ref int)""
  IL_000b:  call       ""ref int Ext.M(ref int)""
  IL_0010:  call       ""ref int Ext.M(ref int)""
  IL_0015:  pop
  IL_0016:  ret
}");
        }

        [Fact]
        public void RefReadOnlyExtensionMethod_PassThrough_TempCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref readonly int M(in this int p) => ref p;
}
class Test
{
    void M()
    {
        5.M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""ref readonly int Ext.M(in int)""
  IL_0009:  pop
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReadOnlyExtensionMethod_PassThrough_LocalNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref readonly int M(in this int p) => ref p;
}
class Test
{
    void M()
    {
        int x = 5;
        x.M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""ref readonly int Ext.M(in int)""
  IL_0009:  pop
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReadOnlyExtensionMethod_PassThrough_FieldNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref readonly int M(in this int p) => ref p;
}
class Test
{
    private int x = 5;
    void M()
    {
        x.M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""int Test.x""
  IL_0006:  call       ""ref readonly int Ext.M(in int)""
  IL_000b:  pop
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefReadOnlyExtensionMethod_PassThrough_ChainNoCopying()
        {
            CompileAndVerify(@"
public static class Ext
{
    public static ref readonly int M(in this int p) => ref p;
}
class Test
{
    private int x = 5;
    void M()
    {
        x.M().M().M();
    }
}", verify: Verification.Fails).VerifyIL("Test.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""int Test.x""
  IL_0006:  call       ""ref readonly int Ext.M(in int)""
  IL_000b:  call       ""ref readonly int Ext.M(in int)""
  IL_0010:  call       ""ref readonly int Ext.M(in int)""
  IL_0015:  pop
  IL_0016:  ret
}");
        }

        [Fact]
        public void RefReadOnlyMethod_PassThrough_ChainNoCopying()
        {
            CompileAndVerify(@"
public struct S
{
    public readonly ref readonly S M() => throw null;
}
class Test
{
    private S x;
    void M()
    {
        x.M().M().M();
    }
}").VerifyIL("Test.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""S Test.x""
  IL_0006:  call       ""readonly ref readonly S S.M()""
  IL_000b:  call       ""readonly ref readonly S S.M()""
  IL_0010:  call       ""readonly ref readonly S S.M()""
  IL_0015:  pop
  IL_0016:  ret
}");
        }

        [Fact]
        public void RefReadOnlyReturnOptionalValue()
        {
            CompileAndVerify(@"
class Program
{
    static ref readonly string M(in string s = ""optional"") => ref s;

    static void Main()
    {
        System.Console.Write(M());
        System.Console.Write(""-"");
        System.Console.Write(M(""provided""));
    }
}", verify: Verification.Fails, expectedOutput: "optional-provided");
        }
    }
}
