// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Async)]
    public class CodeGenAsyncSpillTests : EmitMetadataTestBase
    {
        public CodeGenAsyncSpillTests()
        {
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, IEnumerable<MetadataReference> references = null, CSharpCompilationOptions options = null)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, references: references, options: options);
        }

        [Fact]
        public void AsyncWithTernary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        int c = 0;
        c = c + (b1 ? 1 : await F(2));
        c = c + (b2 ? await F(4) : 8);
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, false));
        Console.WriteLine(H(false, true));
        Console.WriteLine(H(true, false));
        Console.WriteLine(H(true, true));
    }
}";
            var expectedOutput = @"
F(2)
F(10)
10
F(2)
F(4)
F(6)
6
F(9)
9
F(4)
F(5)
5
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [G]: Unexpected type on the stack. { Offset = 0x35, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(bool, bool)", """
                {
                  // Code size       54 (0x36)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldc.i4.0
                  IL_0001:  ldarg.0
                  IL_0002:  brfalse.s  IL_0008
                  IL_0004:  ldc.i4.1
                  IL_0005:  stloc.0
                  IL_0006:  br.s       IL_0014
                  IL_0008:  ldc.i4.2
                  IL_0009:  call       "System.Threading.Tasks.Task<int> Test.F<int>(int)"
                  IL_000e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0013:  stloc.0
                  IL_0014:  ldloc.0
                  IL_0015:  add
                  IL_0016:  ldarg.1
                  IL_0017:  brfalse.s  IL_0027
                  IL_0019:  ldc.i4.4
                  IL_001a:  call       "System.Threading.Tasks.Task<int> Test.F<int>(int)"
                  IL_001f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0024:  stloc.0
                  IL_0025:  br.s       IL_0029
                  IL_0027:  ldc.i4.8
                  IL_0028:  stloc.0
                  IL_0029:  ldloc.0
                  IL_002a:  add
                  IL_002b:  call       "System.Threading.Tasks.Task<int> Test.F<int>(int)"
                  IL_0030:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0035:  ret
                }
                """);
        }

        [Fact]
        public void AsyncWithAnd()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 && await F(true);
        bool x2 = b1 && await F(false);
        bool x3 = b2 && await F(true);
        bool x4 = b2 && await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expectedOutput = @"
F(True)
F(False)
F(4)
4
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [G]: Unexpected type on the stack. { Offset = 0x83, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(bool, bool)", """
                {
                  // Code size      132 (0x84)
                  .maxstack  3
                  .locals init (bool V_0, //x1
                                bool V_1, //x2
                                bool V_2, //x3
                                int V_3, //c
                                bool V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.s    V_4
                  IL_0003:  ldloc.s    V_4
                  IL_0005:  brfalse.s  IL_0014
                  IL_0007:  ldc.i4.1
                  IL_0008:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_000d:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0012:  stloc.s    V_4
                  IL_0014:  ldloc.s    V_4
                  IL_0016:  stloc.0
                  IL_0017:  ldarg.0
                  IL_0018:  stloc.s    V_4
                  IL_001a:  ldloc.s    V_4
                  IL_001c:  brfalse.s  IL_002b
                  IL_001e:  ldc.i4.0
                  IL_001f:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_0024:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0029:  stloc.s    V_4
                  IL_002b:  ldloc.s    V_4
                  IL_002d:  stloc.1
                  IL_002e:  ldarg.1
                  IL_002f:  stloc.s    V_4
                  IL_0031:  ldloc.s    V_4
                  IL_0033:  brfalse.s  IL_0042
                  IL_0035:  ldc.i4.1
                  IL_0036:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_003b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0040:  stloc.s    V_4
                  IL_0042:  ldloc.s    V_4
                  IL_0044:  stloc.2
                  IL_0045:  ldarg.1
                  IL_0046:  stloc.s    V_4
                  IL_0048:  ldloc.s    V_4
                  IL_004a:  brfalse.s  IL_0059
                  IL_004c:  ldc.i4.0
                  IL_004d:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_0052:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0057:  stloc.s    V_4
                  IL_0059:  ldloc.s    V_4
                  IL_005b:  ldc.i4.0
                  IL_005c:  stloc.3
                  IL_005d:  ldloc.0
                  IL_005e:  brfalse.s  IL_0064
                  IL_0060:  ldloc.3
                  IL_0061:  ldc.i4.1
                  IL_0062:  add
                  IL_0063:  stloc.3
                  IL_0064:  ldloc.1
                  IL_0065:  brfalse.s  IL_006b
                  IL_0067:  ldloc.3
                  IL_0068:  ldc.i4.2
                  IL_0069:  add
                  IL_006a:  stloc.3
                  IL_006b:  ldloc.2
                  IL_006c:  brfalse.s  IL_0072
                  IL_006e:  ldloc.3
                  IL_006f:  ldc.i4.4
                  IL_0070:  add
                  IL_0071:  stloc.3
                  IL_0072:  brfalse.s  IL_0078
                  IL_0074:  ldloc.3
                  IL_0075:  ldc.i4.8
                  IL_0076:  add
                  IL_0077:  stloc.3
                  IL_0078:  ldloc.3
                  IL_0079:  call       "System.Threading.Tasks.Task<int> Test.F<int>(int)"
                  IL_007e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0083:  ret
                }
                """);
        }

        [Fact]
        public void AsyncWithOr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<T> F<T>(T x)
    {
        Console.WriteLine(""F("" + x + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(bool b1, bool b2)
    {
        bool x1 = b1 || await F(true);
        bool x2 = b1 || await F(false);
        bool x3 = b2 || await F(true);
        bool x4 = b2 || await F(false);
        int c = 0;
        if (x1) c += 1;
        if (x2) c += 2;
        if (x3) c += 4;
        if (x4) c += 8;
        return await F(c);
    }

    public static int H(bool b1, bool b2)
    {
        Task<int> t = G(b1, b2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        Console.WriteLine(H(false, true));
    }
}";
            var expectedOutput = @"
F(True)
F(False)
F(13)
13
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [G]: Unexpected type on the stack. { Offset = 0x83, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(bool, bool)", """
                {
                  // Code size      132 (0x84)
                  .maxstack  3
                  .locals init (bool V_0, //x1
                                bool V_1, //x2
                                bool V_2, //x3
                                int V_3, //c
                                bool V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.s    V_4
                  IL_0003:  ldloc.s    V_4
                  IL_0005:  brtrue.s   IL_0014
                  IL_0007:  ldc.i4.1
                  IL_0008:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_000d:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0012:  stloc.s    V_4
                  IL_0014:  ldloc.s    V_4
                  IL_0016:  stloc.0
                  IL_0017:  ldarg.0
                  IL_0018:  stloc.s    V_4
                  IL_001a:  ldloc.s    V_4
                  IL_001c:  brtrue.s   IL_002b
                  IL_001e:  ldc.i4.0
                  IL_001f:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_0024:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0029:  stloc.s    V_4
                  IL_002b:  ldloc.s    V_4
                  IL_002d:  stloc.1
                  IL_002e:  ldarg.1
                  IL_002f:  stloc.s    V_4
                  IL_0031:  ldloc.s    V_4
                  IL_0033:  brtrue.s   IL_0042
                  IL_0035:  ldc.i4.1
                  IL_0036:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_003b:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0040:  stloc.s    V_4
                  IL_0042:  ldloc.s    V_4
                  IL_0044:  stloc.2
                  IL_0045:  ldarg.1
                  IL_0046:  stloc.s    V_4
                  IL_0048:  ldloc.s    V_4
                  IL_004a:  brtrue.s   IL_0059
                  IL_004c:  ldc.i4.0
                  IL_004d:  call       "System.Threading.Tasks.Task<bool> Test.F<bool>(bool)"
                  IL_0052:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0057:  stloc.s    V_4
                  IL_0059:  ldloc.s    V_4
                  IL_005b:  ldc.i4.0
                  IL_005c:  stloc.3
                  IL_005d:  ldloc.0
                  IL_005e:  brfalse.s  IL_0064
                  IL_0060:  ldloc.3
                  IL_0061:  ldc.i4.1
                  IL_0062:  add
                  IL_0063:  stloc.3
                  IL_0064:  ldloc.1
                  IL_0065:  brfalse.s  IL_006b
                  IL_0067:  ldloc.3
                  IL_0068:  ldc.i4.2
                  IL_0069:  add
                  IL_006a:  stloc.3
                  IL_006b:  ldloc.2
                  IL_006c:  brfalse.s  IL_0072
                  IL_006e:  ldloc.3
                  IL_006f:  ldc.i4.4
                  IL_0070:  add
                  IL_0071:  stloc.3
                  IL_0072:  brfalse.s  IL_0078
                  IL_0074:  ldloc.3
                  IL_0075:  ldc.i4.8
                  IL_0076:  add
                  IL_0077:  stloc.3
                  IL_0078:  ldloc.3
                  IL_0079:  call       "System.Threading.Tasks.Task<int> Test.F<int>(int)"
                  IL_007e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0083:  ret
                }
                """);
        }

        [Fact]
        public void AsyncWithCoalesce()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static Task<string> F(string x)
    {
        Console.WriteLine(""F("" + (x ?? ""null"") + "")"");
        return Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<string> G(string s1, string s2)
    {
        var result = await F(s1) ?? await F(s2);
        Console.WriteLine("" "" + (result ?? ""null""));
        return result;
    }

    public static string H(string s1, string s2)
    {
        Task<string> t = G(s1, s2);
        t.Wait(1000 * 60);
        return t.Result;
    }

    public static void Main()
    {
        H(null, null);
        H(null, ""a"");
        H(""b"", null);
        H(""c"", ""d"");
    }
}";
            var expectedOutput = @"
F(null)
F(null)
 null
F(null)
F(a)
 a
F(b)
 b
F(c)
 c
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [G]: Unexpected type on the stack. { Offset = 0x37, Found = ref 'string', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<string>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(string, string)", """
                {
                  // Code size       56 (0x38)
                  .maxstack  3
                  .locals init (string V_0, //result
                                string V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "System.Threading.Tasks.Task<string> Test.F(string)"
                  IL_0006:  call       "string System.Runtime.CompilerServices.AsyncHelpers.Await<string>(System.Threading.Tasks.Task<string>)"
                  IL_000b:  stloc.1
                  IL_000c:  ldloc.1
                  IL_000d:  brtrue.s   IL_001b
                  IL_000f:  ldarg.1
                  IL_0010:  call       "System.Threading.Tasks.Task<string> Test.F(string)"
                  IL_0015:  call       "string System.Runtime.CompilerServices.AsyncHelpers.Await<string>(System.Threading.Tasks.Task<string>)"
                  IL_001a:  stloc.1
                  IL_001b:  ldloc.1
                  IL_001c:  stloc.0
                  IL_001d:  ldstr      " "
                  IL_0022:  ldloc.0
                  IL_0023:  dup
                  IL_0024:  brtrue.s   IL_002c
                  IL_0026:  pop
                  IL_0027:  ldstr      "null"
                  IL_002c:  call       "string string.Concat(string, string)"
                  IL_0031:  call       "void System.Console.WriteLine(string)"
                  IL_0036:  ldloc.0
                  IL_0037:  ret
                }
                """);
        }

        [Fact]
        public void AwaitInExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return await Task.Factory.StartNew(() => 21);
    }

    public static async Task<int> G()
    {
        int c = 0;
        c = (await F()) + 21;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G();
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x2e, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Unexpected type on the stack. { Offset = 0xd, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       14 (0xe)
                  .maxstack  2
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0005:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000a:  ldc.i4.s   21
                  IL_000c:  add
                  IL_000d:  ret
                }
                """);
        }

        [Fact]
        public void SpillNestedUnary()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F()
    {
        return 1;
    }

    public static async Task<int> G1()
    {
        return -(await F());
    }

    public static async Task<int> G2()
    {
        return -(-(await F()));
    }

    public static async Task<int> G3()
    {
        return -(-(-(await F())));
    }

    public static void WaitAndPrint(Task<int> t)
    {
        t.Wait();
        Console.WriteLine(t.Result);
    }

    public static void Main()
    {
        WaitAndPrint(G1());
        WaitAndPrint(G2());
        WaitAndPrint(G3());
    }
}";
            var expectedOutput = @"
-1
1
-1
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G1]: Unexpected type on the stack. { Offset = 0xb, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G2]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G3]: Unexpected type on the stack. { Offset = 0xd, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G1()", """
                {
                  // Code size       12 (0xc)
                  .maxstack  1
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0005:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000a:  neg
                  IL_000b:  ret
                }
                """);

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G2()", """
                {
                  // Code size       13 (0xd)
                  .maxstack  1
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0005:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000a:  neg
                  IL_000b:  neg
                  IL_000c:  ret
                }
                """);

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G3()", """
                {
                  // Code size       14 (0xe)
                  .maxstack  1
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Test.F()"
                  IL_0005:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000a:  neg
                  IL_000b:  neg
                  IL_000c:  neg
                  IL_000d:  ret
                }
                """);
        }

        [Fact]
        public void AsyncWithParamsAndLocals_DoubleAwait_Spilling()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => { return x; });
    }

    public static async Task<int> G(int x)
    {
        int c = 0;
        c = (await F(x)) + c;
        c = (await F(x)) + c;
        return c;
    }

    public static void Main()
    {
        Task<int> t = G(21);
        t.Wait(1000 * 60);
        Console.WriteLine(t.Result);
    }
}";
            var expectedOutput = @"
42
";
            // When the local 'c' gets hoisted, the statement:
            //   c = (await F(x)) + c;
            // Gets rewritten to:
            //   this.c_field = (await F(x)) + this.c_field;
            //
            // The code-gen for the assignment is something like this:
            //   ldarg0  // load the 'this' reference to the stack
            //   <emitted await expression>
            //   stfld
            //
            // What we really want is to evaluate any parts of the lvalue that have side-effects (which is this case is
            // nothing), and then defer loading the address for the field reference until after the await expression:
            //   <emitted await expression>
            //   <store to tmp>
            //   ldarg0
            //   <load tmp>
            //   stfld
            //
            // So this case actually requires stack spilling, which is not yet implemented. This has the unfortunate
            // consequence of preventing await expressions from being assigned to hoisted locals.
            //
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Unexpected type on the stack. { Offset = 0x1f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G(int)", """
                {
                  // Code size       32 (0x20)
                  .maxstack  2
                  .locals init (int V_0) //c
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  IL_0002:  ldarg.0
                  IL_0003:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0008:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000d:  ldloc.0
                  IL_000e:  add
                  IL_000f:  stloc.0
                  IL_0010:  ldarg.0
                  IL_0011:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0016:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001b:  ldloc.0
                  IL_001c:  add
                  IL_001d:  stloc.0
                  IL_001e:  ldloc.0
                  IL_001f:  ret
                }
                """);
        }

        [Fact]
        public void SpillCall()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), Get(222), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expectedOutput = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Return value missing on the stack. { Offset = 0x44 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       69 (0x45)
                  .maxstack  5
                  .locals init (int V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldc.i4.s   111
                  IL_0002:  call       "int Test.Get(int)"
                  IL_0007:  ldc.i4     0xde
                  IL_000c:  call       "int Test.Get(int)"
                  IL_0011:  stloc.0
                  IL_0012:  ldc.i4     0x14d
                  IL_0017:  call       "int Test.Get(int)"
                  IL_001c:  stloc.1
                  IL_001d:  ldc.i4     0x1bc
                  IL_0022:  call       "int Test.Get(int)"
                  IL_0027:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_002c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0031:  stloc.2
                  IL_0032:  ldloc.0
                  IL_0033:  ldloc.1
                  IL_0034:  ldloc.2
                  IL_0035:  ldc.i4     0x22b
                  IL_003a:  call       "int Test.Get(int)"
                  IL_003f:  call       "void Test.Printer(int, int, int, int, int)"
                  IL_0044:  ret
                }
                """);
        }

        [Fact]
        public void SpillCall2()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e)
    {
        foreach (var x in new List<int>() { a, b, c, d, e })
        {
            Console.WriteLine(x);
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(Get(111), await F(Get(222)), Get(333), await F(Get(444)), Get(555));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expectedOutput = @"
> 111
> 222
> 333
> 444
> 555
111
222
333
444
555
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Return value missing on the stack. { Offset = 0x4e }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       79 (0x4f)
                  .maxstack  5
                  .locals init (int V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldc.i4.s   111
                  IL_0002:  call       "int Test.Get(int)"
                  IL_0007:  ldc.i4     0xde
                  IL_000c:  call       "int Test.Get(int)"
                  IL_0011:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0016:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001b:  stloc.0
                  IL_001c:  ldc.i4     0x14d
                  IL_0021:  call       "int Test.Get(int)"
                  IL_0026:  stloc.1
                  IL_0027:  ldc.i4     0x1bc
                  IL_002c:  call       "int Test.Get(int)"
                  IL_0031:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0036:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_003b:  stloc.2
                  IL_003c:  ldloc.0
                  IL_003d:  ldloc.1
                  IL_003e:  ldloc.2
                  IL_003f:  ldc.i4     0x22b
                  IL_0044:  call       "int Test.Get(int)"
                  IL_0049:  call       "void Test.Printer(int, int, int, int, int)"
                  IL_004e:  ret
                }
                """);
        }

        [Fact]
        public void SpillCall3()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b, int c, int d, int e, int f)
    {
        foreach (var x in new List<int>(){a, b, c, d, e, f})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(2), 3, await F(await F(await F(await F(4)))), await F(5), 6);
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expectedOutput = @"
1
2
3
4
5
6
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Return value missing on the stack. { Offset = 0x4d }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       78 (0x4e)
                  .maxstack  6
                  .locals init (int V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldc.i4.2
                  IL_0001:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000b:  stloc.0
                  IL_000c:  ldc.i4.4
                  IL_000d:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0012:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0017:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_001c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0021:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0026:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_002b:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0030:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0035:  stloc.1
                  IL_0036:  ldc.i4.5
                  IL_0037:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_003c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0041:  stloc.2
                  IL_0042:  ldc.i4.1
                  IL_0043:  ldloc.0
                  IL_0044:  ldc.i4.3
                  IL_0045:  ldloc.1
                  IL_0046:  ldloc.2
                  IL_0047:  ldc.i4.6
                  IL_0048:  call       "void Test.Printer(int, int, int, int, int, int)"
                  IL_004d:  ret
                }
                """);
        }

        [Fact]
        public void SpillCall4()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Test
{
    public static void Printer(int a, int b)
    {
        foreach (var x in new List<int>(){a, b})
        {
            Console.WriteLine(x);
        }
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task G()
    {
        Printer(1, await F(await F(2)));
    }

    public static void Main()
    {
        Task t = G();
        t.Wait();
    }
}";
            var expectedOutput = @"
1
2
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Return value missing on the stack. { Offset = 0x1d }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldc.i4.2
                  IL_0001:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000b:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0010:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0015:  stloc.0
                  IL_0016:  ldc.i4.1
                  IL_0017:  ldloc.0
                  IL_0018:  call       "void Test.Printer(int, int)"
                  IL_001d:  ret
                }
                """);
        }

        [Fact]
        public void SpillSequences1()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        return a;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(array[1] += 2, array[3] += await G(), 4);
        return 1;
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // https://github.com/dotnet/roslyn/issues/80147 - There's an extra unneeded array
            // load before the await that could be removed
            v.VerifyIL("Test.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"{
  // Code size      273 (0x111)
  .maxstack  5
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                Test.<F>d__2 V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0088
   -IL_000e:  nop
   -IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0016:  ldc.i4.1
    IL_0017:  ldelema    ""int""
    IL_001c:  dup
    IL_001d:  ldind.i4
    IL_001e:  ldc.i4.2
    IL_001f:  add
    IL_0020:  dup
    IL_0021:  stloc.2
    IL_0022:  stind.i4
    IL_0023:  ldloc.2
    IL_0024:  stfld      ""int Test.<F>d__2.<>s__1""
    IL_0029:  ldarg.0
    IL_002a:  ldarg.0
    IL_002b:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0030:  stfld      ""int[] Test.<F>d__2.<>s__4""
    IL_0035:  ldarg.0
    IL_0036:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_003b:  ldc.i4.3
    IL_003c:  ldelem.i4
    IL_003d:  pop
    IL_003e:  ldarg.0
    IL_003f:  ldarg.0
    IL_0040:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_0045:  ldc.i4.3
    IL_0046:  ldelem.i4
    IL_0047:  stfld      ""int Test.<F>d__2.<>s__2""
    IL_004c:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_0051:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0056:  stloc.3
   ~IL_0057:  ldloca.s   V_3
    IL_0059:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_005e:  brtrue.s   IL_00a4
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      ""int Test.<F>d__2.<>1__state""
   <IL_0069:  ldarg.0
    IL_006a:  ldloc.3
    IL_006b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0070:  ldarg.0
    IL_0071:  stloc.s    V_4
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_0079:  ldloca.s   V_3
    IL_007b:  ldloca.s   V_4
    IL_007d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__2)""
    IL_0082:  nop
    IL_0083:  leave      IL_0110
   >IL_0088:  ldarg.0
    IL_0089:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_008e:  stloc.3
    IL_008f:  ldarg.0
    IL_0090:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0095:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.m1
    IL_009d:  dup
    IL_009e:  stloc.0
    IL_009f:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldloca.s   V_3
    IL_00a7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ac:  stfld      ""int Test.<F>d__2.<>s__3""
    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      ""int Test.<F>d__2.<>s__1""
    IL_00b7:  ldarg.0
    IL_00b8:  ldfld      ""int[] Test.<F>d__2.<>s__4""
    IL_00bd:  ldc.i4.3
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      ""int Test.<F>d__2.<>s__2""
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      ""int Test.<F>d__2.<>s__3""
    IL_00ca:  add
    IL_00cb:  dup
    IL_00cc:  stloc.2
    IL_00cd:  stelem.i4
    IL_00ce:  ldloc.2
    IL_00cf:  ldc.i4.4
    IL_00d0:  call       ""int Test.H(int, int, int)""
    IL_00d5:  pop
    IL_00d6:  ldarg.0
    IL_00d7:  ldnull
    IL_00d8:  stfld      ""int[] Test.<F>d__2.<>s__4""
   -IL_00dd:  ldc.i4.1
    IL_00de:  stloc.1
    IL_00df:  leave.s    IL_00fb
  }
  catch System.Exception
  {
   ~IL_00e1:  stloc.s    V_5
    IL_00e3:  ldarg.0
    IL_00e4:  ldc.i4.s   -2
    IL_00e6:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00eb:  ldarg.0
    IL_00ec:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_00f1:  ldloc.s    V_5
    IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f8:  nop
    IL_00f9:  leave.s    IL_0110
  }
 -IL_00fb:  ldarg.0
  IL_00fc:  ldc.i4.s   -2
  IL_00fe:  stfld      ""int Test.<F>d__2.<>1__state""
 ~IL_0103:  ldarg.0
  IL_0104:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
  IL_0109:  ldloc.1
  IL_010a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_010f:  nop
  IL_0110:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Minimal);

            var comp = CreateRuntimeAsyncCompilation(source);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x40, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.F(int[])", """
                {
                  // Code size       65 (0x41)
                  .maxstack  4
                  .locals init (int[] V_0,
                                int& V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  ldelema    "int"
                  IL_0007:  dup
                  IL_0008:  ldind.i4
                  IL_0009:  ldc.i4.2
                  IL_000a:  add
                  IL_000b:  dup
                  IL_000c:  stloc.s    V_4
                  IL_000e:  stind.i4
                  IL_000f:  ldloc.s    V_4
                  IL_0011:  ldarg.0
                  IL_0012:  stloc.0
                  IL_0013:  ldloc.0
                  IL_0014:  ldc.i4.3
                  IL_0015:  ldelem.i4
                  IL_0016:  pop
                  IL_0017:  ldloc.0
                  IL_0018:  ldc.i4.3
                  IL_0019:  ldelem.i4
                  IL_001a:  stloc.2
                  IL_001b:  call       "System.Threading.Tasks.Task<int> Test.G()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  stloc.3
                  IL_0026:  ldloc.0
                  IL_0027:  ldc.i4.3
                  IL_0028:  ldelema    "int"
                  IL_002d:  stloc.1
                  IL_002e:  ldloc.1
                  IL_002f:  ldloc.2
                  IL_0030:  ldloc.3
                  IL_0031:  add
                  IL_0032:  dup
                  IL_0033:  stloc.s    V_4
                  IL_0035:  stind.i4
                  IL_0036:  ldloc.s    V_4
                  IL_0038:  ldc.i4.4
                  IL_0039:  call       "int Test.H(int, int, int)"
                  IL_003e:  pop
                  IL_003f:  ldc.i4.1
                  IL_0040:  ret
                }
                """);
        }

        [Fact]
        public void SpillSequencesRelease()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        return a;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(array[1] += 2, array[3] += await G(), 4);
        return 1;
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            v.VerifyIL("Test.<F>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      251 (0xfb)
  .maxstack  5
  .locals init (int V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Test.<F>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_007d
   -IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""int[] Test.<F>d__2.array""
    IL_0011:  ldc.i4.1
    IL_0012:  ldelema    ""int""
    IL_0017:  dup
    IL_0018:  ldind.i4
    IL_0019:  ldc.i4.2
    IL_001a:  add
    IL_001b:  dup
    IL_001c:  stloc.3
    IL_001d:  stind.i4
    IL_001e:  ldloc.3
    IL_001f:  stfld      ""int Test.<F>d__2.<>7__wrap1""
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldfld      ""int[] Test.<F>d__2.array""
    IL_002b:  stfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0030:  ldarg.0
    IL_0031:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0036:  ldc.i4.3
    IL_0037:  ldelem.i4
    IL_0038:  pop
    IL_0039:  ldarg.0
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_0040:  ldc.i4.3
    IL_0041:  ldelem.i4
    IL_0042:  stfld      ""int Test.<F>d__2.<>7__wrap2""
    IL_0047:  call       ""System.Threading.Tasks.Task<int> Test.G()""
    IL_004c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0051:  stloc.s    V_4
   ~IL_0053:  ldloca.s   V_4
    IL_0055:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_005a:  brtrue.s   IL_009a
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.0
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int Test.<F>d__2.<>1__state""
   <IL_0065:  ldarg.0
    IL_0066:  ldloc.s    V_4
    IL_0068:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_0073:  ldloca.s   V_4
    IL_0075:  ldarg.0
    IL_0076:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Test.<F>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Test.<F>d__2)""
    IL_007b:  leave.s    IL_00fa
   >IL_007d:  ldarg.0
    IL_007e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldarg.0
    IL_0086:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Test.<F>d__2.<>u__1""
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.m1
    IL_0093:  dup
    IL_0094:  stloc.0
    IL_0095:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_009a:  ldloca.s   V_4
    IL_009c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""int Test.<F>d__2.<>7__wrap1""
    IL_00a8:  ldarg.0
    IL_00a9:  ldfld      ""int[] Test.<F>d__2.<>7__wrap3""
    IL_00ae:  ldc.i4.3
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""int Test.<F>d__2.<>7__wrap2""
    IL_00b5:  ldloc.2
    IL_00b6:  add
    IL_00b7:  dup
    IL_00b8:  stloc.3
    IL_00b9:  stelem.i4
    IL_00ba:  ldloc.3
    IL_00bb:  ldc.i4.4
    IL_00bc:  call       ""int Test.H(int, int, int)""
    IL_00c1:  pop
    IL_00c2:  ldarg.0
    IL_00c3:  ldnull
    IL_00c4:  stfld      ""int[] Test.<F>d__2.<>7__wrap3""
   -IL_00c9:  ldc.i4.1
    IL_00ca:  stloc.1
    IL_00cb:  leave.s    IL_00e6
  }
  catch System.Exception
  {
   ~IL_00cd:  stloc.s    V_5
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.s   -2
    IL_00d2:  stfld      ""int Test.<F>d__2.<>1__state""
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
    IL_00dd:  ldloc.s    V_5
    IL_00df:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00e4:  leave.s    IL_00fa
  }
 -IL_00e6:  ldarg.0
  IL_00e7:  ldc.i4.s   -2
  IL_00e9:  stfld      ""int Test.<F>d__2.<>1__state""
 ~IL_00ee:  ldarg.0
  IL_00ef:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Test.<F>d__2.<>t__builder""
  IL_00f4:  ldloc.1
  IL_00f5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00fa:  ret
}", sequencePointDisplay: SequencePointDisplayMode.Minimal);

            var comp = CreateRuntimeAsyncCompilation(source);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x40, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.F(int[])", """
                {
                  // Code size       65 (0x41)
                  .maxstack  4
                  .locals init (int[] V_0,
                                int& V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  ldelema    "int"
                  IL_0007:  dup
                  IL_0008:  ldind.i4
                  IL_0009:  ldc.i4.2
                  IL_000a:  add
                  IL_000b:  dup
                  IL_000c:  stloc.s    V_4
                  IL_000e:  stind.i4
                  IL_000f:  ldloc.s    V_4
                  IL_0011:  ldarg.0
                  IL_0012:  stloc.0
                  IL_0013:  ldloc.0
                  IL_0014:  ldc.i4.3
                  IL_0015:  ldelem.i4
                  IL_0016:  pop
                  IL_0017:  ldloc.0
                  IL_0018:  ldc.i4.3
                  IL_0019:  ldelem.i4
                  IL_001a:  stloc.2
                  IL_001b:  call       "System.Threading.Tasks.Task<int> Test.G()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  stloc.3
                  IL_0026:  ldloc.0
                  IL_0027:  ldc.i4.3
                  IL_0028:  ldelema    "int"
                  IL_002d:  stloc.1
                  IL_002e:  ldloc.1
                  IL_002f:  ldloc.2
                  IL_0030:  ldloc.3
                  IL_0031:  add
                  IL_0032:  dup
                  IL_0033:  stloc.s    V_4
                  IL_0035:  stind.i4
                  IL_0036:  ldloc.s    V_4
                  IL_0038:  ldc.i4.4
                  IL_0039:  call       "int Test.H(int, int, int)"
                  IL_003e:  pop
                  IL_003f:  ldc.i4.1
                  IL_0040:  ret
                }
                """);
        }

        [Fact]
        public void SpillSequencesInConditionalExpression1()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, int b, int c)
    {
        Console.Write($""H{a},{b},{c};"");
        return a;
    }

    public static Task<int> G(int i)
    {
        Console.Write($""G{i}"");
        return Task.FromResult(i);
    }

    public static async Task<int> F(int[] array)
    {
        H(0, (1 == await G(1)) ? array[3] += await G(2) : 1, 4);
        return 1;
    }

    public static async Task Main()
    {
        await F(new int[4]);
    }
}
";

            var expectedOutput = "G1G2H0,2,4;";
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x43, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [Main]: Return value missing on the stack. { Offset = 0x11 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.F(int[])", """
                {
                  // Code size       68 (0x44)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldc.i4.1
                  IL_0001:  call       "System.Threading.Tasks.Task<int> Test.G(int)"
                  IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000b:  stloc.0
                  IL_000c:  ldc.i4.1
                  IL_000d:  ldloc.0
                  IL_000e:  bne.un.s   IL_0037
                  IL_0010:  ldarg.0
                  IL_0011:  dup
                  IL_0012:  ldc.i4.3
                  IL_0013:  ldelem.i4
                  IL_0014:  pop
                  IL_0015:  dup
                  IL_0016:  ldc.i4.3
                  IL_0017:  ldelem.i4
                  IL_0018:  stloc.2
                  IL_0019:  ldc.i4.2
                  IL_001a:  call       "System.Threading.Tasks.Task<int> Test.G(int)"
                  IL_001f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0024:  stloc.3
                  IL_0025:  ldc.i4.3
                  IL_0026:  ldelema    "int"
                  IL_002b:  ldloc.2
                  IL_002c:  ldloc.3
                  IL_002d:  add
                  IL_002e:  dup
                  IL_002f:  stloc.s    V_4
                  IL_0031:  stind.i4
                  IL_0032:  ldloc.s    V_4
                  IL_0034:  stloc.1
                  IL_0035:  br.s       IL_0039
                  IL_0037:  ldc.i4.1
                  IL_0038:  stloc.1
                  IL_0039:  ldc.i4.0
                  IL_003a:  ldloc.1
                  IL_003b:  ldc.i4.4
                  IL_003c:  call       "int Test.H(int, int, int)"
                  IL_0041:  pop
                  IL_0042:  ldc.i4.1
                  IL_0043:  ret
                }
                """);
        }

        [Fact]
        public void SpillSequencesInNullCoalescingOperator1()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class C
{
    public static int H(int a, object b, int c)
    {
        Console.Write($""H{a},{b},{c};"");
        return a;
    }

    public static object O(int a)
    {
        return null;
    }

    public static Task<int> G(int i)
    {
        Console.Write($""G{i};"");
        return Task.FromResult(i);
    }

    public static async Task<int> F(int[] array)
    {
        H(0, O(array[0] += await G(1)) ?? (array[1] += await G(2)), 4);
        return 1;
    }

    public static async Task Main()
    {
        await F(new int[4]);
    }
}
";

            var expectedOutput = "G1;G2;H0,2,4;";
            CompileAndVerify(source, options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), expectedOutput: expectedOutput, symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "array",
                    "<>7__wrap1",
                    "<>7__wrap2",
                    "<>u__1",
                    "<>7__wrap3",
                    "<>7__wrap4",
                }, module.GetFieldNames("C.<F>d__3"));
            });

            CompileAndVerify(source, verify: Verification.Passes, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), expectedOutput: expectedOutput, symbolValidator: module =>
             {
                 AssertEx.Equal(new[]
                 {
                    "<>1__state",
                    "<>t__builder",
                    "array",
                    "<>s__1",
                    "<>s__2",
                    "<>s__3",
                    "<>s__4",
                    "<>s__5",
                    "<>s__6",
                    "<>s__7",
                    "<>u__1",
                    "<>s__8"
                 }, module.GetFieldNames("C.<F>d__3"));
             });

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x61, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [Main]: Return value missing on the stack. { Offset = 0x11 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.F(int[])", """
                {
                  // Code size       98 (0x62)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                object V_2,
                                int V_3,
                                int V_4,
                                int V_5)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  ldc.i4.0
                  IL_0003:  ldelem.i4
                  IL_0004:  pop
                  IL_0005:  dup
                  IL_0006:  ldc.i4.0
                  IL_0007:  ldelem.i4
                  IL_0008:  stloc.0
                  IL_0009:  ldc.i4.1
                  IL_000a:  call       "System.Threading.Tasks.Task<int> C.G(int)"
                  IL_000f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0014:  stloc.1
                  IL_0015:  ldc.i4.0
                  IL_0016:  ldelema    "int"
                  IL_001b:  ldloc.0
                  IL_001c:  ldloc.1
                  IL_001d:  add
                  IL_001e:  dup
                  IL_001f:  stloc.3
                  IL_0020:  stind.i4
                  IL_0021:  ldloc.3
                  IL_0022:  call       "object C.O(int)"
                  IL_0027:  stloc.2
                  IL_0028:  ldloc.2
                  IL_0029:  brtrue.s   IL_0057
                  IL_002b:  ldarg.0
                  IL_002c:  dup
                  IL_002d:  ldc.i4.1
                  IL_002e:  ldelem.i4
                  IL_002f:  pop
                  IL_0030:  dup
                  IL_0031:  ldc.i4.1
                  IL_0032:  ldelem.i4
                  IL_0033:  stloc.3
                  IL_0034:  ldc.i4.2
                  IL_0035:  call       "System.Threading.Tasks.Task<int> C.G(int)"
                  IL_003a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_003f:  stloc.s    V_4
                  IL_0041:  ldc.i4.1
                  IL_0042:  ldelema    "int"
                  IL_0047:  ldloc.3
                  IL_0048:  ldloc.s    V_4
                  IL_004a:  add
                  IL_004b:  dup
                  IL_004c:  stloc.s    V_5
                  IL_004e:  stind.i4
                  IL_004f:  ldloc.s    V_5
                  IL_0051:  box        "int"
                  IL_0056:  stloc.2
                  IL_0057:  ldc.i4.0
                  IL_0058:  ldloc.2
                  IL_0059:  ldc.i4.4
                  IL_005a:  call       "int C.H(int, object, int)"
                  IL_005f:  pop
                  IL_0060:  ldc.i4.1
                  IL_0061:  ret
                }
                """);
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting001()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug {
  class Program {
    private readonly bool b=true;

    private async Task AsyncMethod() {
      Console.WriteLine(b && await Task.FromResult(false));
      Console.WriteLine(b); 
    }

    static void Main(string[] args) {
      new Program().AsyncMethod().Wait();
    }
  }
}";
            var expectedOutput = @"
False
True
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [AsyncMethod]: Return value missing on the stack. { Offset = 0x27 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("AsyncConditionalBug.Program.AsyncMethod()", """
                {
                  // Code size       40 (0x28)
                  .maxstack  1
                  .locals init (bool V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "bool AsyncConditionalBug.Program.b"
                  IL_0006:  stloc.0
                  IL_0007:  ldloc.0
                  IL_0008:  brfalse.s  IL_0016
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "System.Threading.Tasks.Task<bool> System.Threading.Tasks.Task.FromResult<bool>(bool)"
                  IL_0010:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0015:  stloc.0
                  IL_0016:  ldloc.0
                  IL_0017:  call       "void System.Console.WriteLine(bool)"
                  IL_001c:  ldarg.0
                  IL_001d:  ldfld      "bool AsyncConditionalBug.Program.b"
                  IL_0022:  call       "void System.Console.WriteLine(bool)"
                  IL_0027:  ret
                }
                """);
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug {
  class Program {
    private static readonly bool b=true;

    private async Task AsyncMethod() {
      Console.WriteLine(b && await Task.FromResult(false));
      Console.WriteLine(b); 
    }

    static void Main(string[] args) {
      new Program().AsyncMethod().Wait();
    }
  }
}";
            var expectedOutput = @"
False
True
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [AsyncMethod]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("AsyncConditionalBug.Program.AsyncMethod()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  1
                  .locals init (bool V_0)
                  IL_0000:  ldsfld     "bool AsyncConditionalBug.Program.b"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  brfalse.s  IL_0015
                  IL_0009:  ldc.i4.0
                  IL_000a:  call       "System.Threading.Tasks.Task<bool> System.Threading.Tasks.Task.FromResult<bool>(bool)"
                  IL_000f:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_0014:  stloc.0
                  IL_0015:  ldloc.0
                  IL_0016:  call       "void System.Console.WriteLine(bool)"
                  IL_001b:  ldsfld     "bool AsyncConditionalBug.Program.b"
                  IL_0020:  call       "void System.Console.WriteLine(bool)"
                  IL_0025:  ret
                }
                """);
        }

        [WorkItem(4628, "https://github.com/dotnet/roslyn/issues/4628")]
        [Fact]
        public void AsyncWithShortCircuiting003()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug
{
    class Program
    {
        private readonly string NULL = null;

        private async Task AsyncMethod()
        {
            Console.WriteLine(NULL ?? await Task.FromResult(""hello""));
            Console.WriteLine(NULL);
        }

        static void Main(string[] args)
        {
            new Program().AsyncMethod().Wait();
        }
    }
}";
            var expectedOutput = @"
hello
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [AsyncMethod]: Return value missing on the stack. { Offset = 0x2b }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("AsyncConditionalBug.Program.AsyncMethod()", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  1
                  .locals init (string V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "string AsyncConditionalBug.Program.NULL"
                  IL_0006:  stloc.0
                  IL_0007:  ldloc.0
                  IL_0008:  brtrue.s   IL_001a
                  IL_000a:  ldstr      "hello"
                  IL_000f:  call       "System.Threading.Tasks.Task<string> System.Threading.Tasks.Task.FromResult<string>(string)"
                  IL_0014:  call       "string System.Runtime.CompilerServices.AsyncHelpers.Await<string>(System.Threading.Tasks.Task<string>)"
                  IL_0019:  stloc.0
                  IL_001a:  ldloc.0
                  IL_001b:  call       "void System.Console.WriteLine(string)"
                  IL_0020:  ldarg.0
                  IL_0021:  ldfld      "string AsyncConditionalBug.Program.NULL"
                  IL_0026:  call       "void System.Console.WriteLine(string)"
                  IL_002b:  ret
                }
                """);
        }

        [WorkItem(4638, "https://github.com/dotnet/roslyn/issues/4638")]
        [Fact]
        public void AsyncWithShortCircuiting004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace AsyncConditionalBug
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DoSomething(Tuple.Create(1.ToString(), Guid.NewGuid())).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Console.Write(ex.Message);
            }
        }

        public static async Task DoSomething(Tuple<string, Guid> item)
        {
            if (item.Item2 != null || await IsValid(item.Item2))
            {
                throw new Exception(""Not Valid!"");
            };
        }

        private static async Task<bool> IsValid(Guid id)
        {
            return false;
        }
    }
}";
            var expectedOutput = @"
Not Valid!
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [DoSomething]: Return value missing on the stack. { Offset = 0x2b }
                    [IsValid]: Unexpected type on the stack. { Offset = 0x1, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });

            verifier.VerifyDiagnostics(
                // (23,17): warning CS8073: The result of the expression is always 'true' since a value of type 'Guid' is never equal to 'null' of type 'Guid?'
                //             if (item.Item2 != null || await IsValid(item.Item2))
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "item.Item2 != null").WithArguments("true", "System.Guid", "System.Guid?").WithLocation(23, 17)
            );
            verifier.VerifyIL("AsyncConditionalBug.Program.DoSomething(System.Tuple<string, System.Guid>)", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  1
                  .locals init (bool V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  callvirt   "System.Guid System.Tuple<string, System.Guid>.Item2.get"
                  IL_0006:  pop
                  IL_0007:  ldc.i4.1
                  IL_0008:  stloc.0
                  IL_0009:  ldloc.0
                  IL_000a:  brtrue.s   IL_001d
                  IL_000c:  ldarg.0
                  IL_000d:  callvirt   "System.Guid System.Tuple<string, System.Guid>.Item2.get"
                  IL_0012:  call       "System.Threading.Tasks.Task<bool> AsyncConditionalBug.Program.IsValid(System.Guid)"
                  IL_0017:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_001c:  stloc.0
                  IL_001d:  ldloc.0
                  IL_001e:  brfalse.s  IL_002b
                  IL_0020:  ldstr      "Not Valid!"
                  IL_0025:  newobj     "System.Exception..ctor(string)"
                  IL_002a:  throw
                  IL_002b:  ret
                }
                """);
        }

        [Fact]
        public void SpillSequencesInLogicalBinaryOperator1()
        {
            var source = @"
using System.Threading.Tasks;

public class Test
{
    public static int H(int a, bool b, int c)
    {
        return a;
    }

    public static bool B(int a)
    {
        return true;
    }

    public static Task<int> G()
    {
        return null;
    }

    public static async Task<int> F(int[] array)
    {
        H(0, B(array[0] += await G()) || B(array[1] += await G()), 4);
        return 1;
    }
}
";
            CompileAndVerify(source, options: TestOptions.DebugDll);

            var comp = CreateRuntimeAsyncCompilation(source);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x5f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.F(int[])", """
                {
                  // Code size       96 (0x60)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                bool V_2,
                                int V_3,
                                int V_4,
                                int V_5)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  ldc.i4.0
                  IL_0003:  ldelem.i4
                  IL_0004:  pop
                  IL_0005:  dup
                  IL_0006:  ldc.i4.0
                  IL_0007:  ldelem.i4
                  IL_0008:  stloc.0
                  IL_0009:  call       "System.Threading.Tasks.Task<int> Test.G()"
                  IL_000e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0013:  stloc.1
                  IL_0014:  ldc.i4.0
                  IL_0015:  ldelema    "int"
                  IL_001a:  ldloc.0
                  IL_001b:  ldloc.1
                  IL_001c:  add
                  IL_001d:  dup
                  IL_001e:  stloc.3
                  IL_001f:  stind.i4
                  IL_0020:  ldloc.3
                  IL_0021:  call       "bool Test.B(int)"
                  IL_0026:  stloc.2
                  IL_0027:  ldloc.2
                  IL_0028:  brtrue.s   IL_0055
                  IL_002a:  ldarg.0
                  IL_002b:  dup
                  IL_002c:  ldc.i4.1
                  IL_002d:  ldelem.i4
                  IL_002e:  pop
                  IL_002f:  dup
                  IL_0030:  ldc.i4.1
                  IL_0031:  ldelem.i4
                  IL_0032:  stloc.3
                  IL_0033:  call       "System.Threading.Tasks.Task<int> Test.G()"
                  IL_0038:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_003d:  stloc.s    V_4
                  IL_003f:  ldc.i4.1
                  IL_0040:  ldelema    "int"
                  IL_0045:  ldloc.3
                  IL_0046:  ldloc.s    V_4
                  IL_0048:  add
                  IL_0049:  dup
                  IL_004a:  stloc.s    V_5
                  IL_004c:  stind.i4
                  IL_004d:  ldloc.s    V_5
                  IL_004f:  call       "bool Test.B(int)"
                  IL_0054:  stloc.2
                  IL_0055:  ldc.i4.0
                  IL_0056:  ldloc.2
                  IL_0057:  ldc.i4.4
                  IL_0058:  call       "int Test.H(int, bool, int)"
                  IL_005d:  pop
                  IL_005e:  ldc.i4.1
                  IL_005f:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;

            //multidimensional
            tests++;
            decimal[,] arr2 = new decimal[await GetVal(4), await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            arr2 = new decimal[4, await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            tests++;
            arr2 = new decimal[await GetVal(4), 4];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;


            //jagged array
            tests++;
            decimal?[][] arr3 = new decimal?[await GetVal(4)][];
            if (arr3.Rank == 2 && arr3.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray01WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;

            //multidimensional
            tests++;
            decimal[,] arr2 = new decimal[await GetVal(4), await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            arr2 = new decimal[4, await GetVal(4)];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;

            tests++;
            arr2 = new decimal[await GetVal(4), 4];
            if (arr2.Rank == 2 && arr2.Length == 16)
                Driver.Count++;


            //jagged array
            tests++;
            decimal?[][] arr3 = new decimal?[await GetVal(4)][];
            if (arr3.Rank == 2 && arr3.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x120 }
                    """
            });

            verifier.VerifyDiagnostics(
                // (63,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         t.Run(6);
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "t.Run(6)").WithLocation(63, 9)
            );
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size      289 (0x121)
                  .maxstack  3
                  .locals init (int V_0, //tests
                                decimal[,] V_1, //arr2
                                decimal?[][] V_2, //arr3
                                int V_3)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.0
                    IL_0006:  ldarg.0
                    IL_0007:  ldc.i4.4
                    IL_0008:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0012:  newarr     "int"
                    IL_0017:  ldlen
                    IL_0018:  conv.i4
                    IL_0019:  ldc.i4.4
                    IL_001a:  bne.un.s   IL_0028
                    IL_001c:  ldsfld     "int Driver.Count"
                    IL_0021:  ldc.i4.1
                    IL_0022:  add
                    IL_0023:  stsfld     "int Driver.Count"
                    IL_0028:  ldloc.0
                    IL_0029:  ldc.i4.1
                    IL_002a:  add
                    IL_002b:  stloc.0
                    IL_002c:  ldarg.0
                    IL_002d:  ldc.i4.4
                    IL_002e:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0033:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0038:  ldarg.0
                    IL_0039:  ldc.i4.4
                    IL_003a:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_003f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0044:  stloc.3
                    IL_0045:  ldloc.3
                    IL_0046:  newobj     "decimal[*,*]..ctor"
                    IL_004b:  stloc.1
                    IL_004c:  ldloc.1
                    IL_004d:  callvirt   "int System.Array.Rank.get"
                    IL_0052:  ldc.i4.2
                    IL_0053:  bne.un.s   IL_006b
                    IL_0055:  ldloc.1
                    IL_0056:  callvirt   "int System.Array.Length.get"
                    IL_005b:  ldc.i4.s   16
                    IL_005d:  bne.un.s   IL_006b
                    IL_005f:  ldsfld     "int Driver.Count"
                    IL_0064:  ldc.i4.1
                    IL_0065:  add
                    IL_0066:  stsfld     "int Driver.Count"
                    IL_006b:  ldarg.0
                    IL_006c:  ldc.i4.4
                    IL_006d:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0072:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0077:  stloc.3
                    IL_0078:  ldc.i4.4
                    IL_0079:  ldloc.3
                    IL_007a:  newobj     "decimal[*,*]..ctor"
                    IL_007f:  stloc.1
                    IL_0080:  ldloc.1
                    IL_0081:  callvirt   "int System.Array.Rank.get"
                    IL_0086:  ldc.i4.2
                    IL_0087:  bne.un.s   IL_009f
                    IL_0089:  ldloc.1
                    IL_008a:  callvirt   "int System.Array.Length.get"
                    IL_008f:  ldc.i4.s   16
                    IL_0091:  bne.un.s   IL_009f
                    IL_0093:  ldsfld     "int Driver.Count"
                    IL_0098:  ldc.i4.1
                    IL_0099:  add
                    IL_009a:  stsfld     "int Driver.Count"
                    IL_009f:  ldloc.0
                    IL_00a0:  ldc.i4.1
                    IL_00a1:  add
                    IL_00a2:  stloc.0
                    IL_00a3:  ldarg.0
                    IL_00a4:  ldc.i4.4
                    IL_00a5:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00aa:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00af:  ldc.i4.4
                    IL_00b0:  newobj     "decimal[*,*]..ctor"
                    IL_00b5:  stloc.1
                    IL_00b6:  ldloc.1
                    IL_00b7:  callvirt   "int System.Array.Rank.get"
                    IL_00bc:  ldc.i4.2
                    IL_00bd:  bne.un.s   IL_00d5
                    IL_00bf:  ldloc.1
                    IL_00c0:  callvirt   "int System.Array.Length.get"
                    IL_00c5:  ldc.i4.s   16
                    IL_00c7:  bne.un.s   IL_00d5
                    IL_00c9:  ldsfld     "int Driver.Count"
                    IL_00ce:  ldc.i4.1
                    IL_00cf:  add
                    IL_00d0:  stsfld     "int Driver.Count"
                    IL_00d5:  ldloc.0
                    IL_00d6:  ldc.i4.1
                    IL_00d7:  add
                    IL_00d8:  stloc.0
                    IL_00d9:  ldarg.0
                    IL_00da:  ldc.i4.4
                    IL_00db:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00e0:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00e5:  newarr     "decimal?[]"
                    IL_00ea:  stloc.2
                    IL_00eb:  ldloc.2
                    IL_00ec:  callvirt   "int System.Array.Rank.get"
                    IL_00f1:  ldc.i4.2
                    IL_00f2:  bne.un.s   IL_0106
                    IL_00f4:  ldloc.2
                    IL_00f5:  ldlen
                    IL_00f6:  conv.i4
                    IL_00f7:  ldc.i4.4
                    IL_00f8:  bne.un.s   IL_0106
                    IL_00fa:  ldsfld     "int Driver.Count"
                    IL_00ff:  ldc.i4.1
                    IL_0100:  add
                    IL_0101:  stsfld     "int Driver.Count"
                    IL_0106:  leave.s    IL_0120
                  }
                  finally
                  {
                    IL_0108:  ldsfld     "int Driver.Count"
                    IL_010d:  ldloc.0
                    IL_010e:  sub
                    IL_010f:  stsfld     "int Driver.Result"
                    IL_0114:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0119:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_011e:  pop
                    IL_011f:  endfinally
                  }
                  IL_0120:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_1WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            tests++;
            int[] arr = new int[await GetVal(4)];
            if (arr.Length == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x42 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       67 (0x43)
                  .maxstack  2
                  .locals init (int V_0) //tests
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.0
                    IL_0006:  ldarg.0
                    IL_0007:  ldc.i4.4
                    IL_0008:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0012:  newarr     "int"
                    IL_0017:  ldlen
                    IL_0018:  conv.i4
                    IL_0019:  ldc.i4.4
                    IL_001a:  bne.un.s   IL_0028
                    IL_001c:  ldsfld     "int Driver.Count"
                    IL_0021:  ldc.i4.1
                    IL_0022:  add
                    IL_0023:  stsfld     "int Driver.Count"
                    IL_0028:  leave.s    IL_0042
                  }
                  finally
                  {
                    IL_002a:  ldsfld     "int Driver.Count"
                    IL_002f:  ldloc.0
                    IL_0030:  sub
                    IL_0031:  stsfld     "int Driver.Result"
                    IL_0036:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_003b:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0040:  pop
                    IL_0041:  endfinally
                  }
                  IL_0042:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];    
        
            tests++;
            arr[0] = await GetVal(4);
            if (arr[0] == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_2WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];    
        
            tests++;
            arr[0] = await GetVal(4);
            if (arr[0] == 4)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x48 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       73 (0x49)
                  .maxstack  4
                  .locals init (int V_0, //tests
                                int V_1)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldc.i4.4
                    IL_0003:  newarr     "int"
                    IL_0008:  ldloc.0
                    IL_0009:  ldc.i4.1
                    IL_000a:  add
                    IL_000b:  stloc.0
                    IL_000c:  dup
                    IL_000d:  ldarg.0
                    IL_000e:  ldc.i4.4
                    IL_000f:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0014:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0019:  stloc.1
                    IL_001a:  ldc.i4.0
                    IL_001b:  ldloc.1
                    IL_001c:  stelem.i4
                    IL_001d:  ldc.i4.0
                    IL_001e:  ldelem.i4
                    IL_001f:  ldc.i4.4
                    IL_0020:  bne.un.s   IL_002e
                    IL_0022:  ldsfld     "int Driver.Count"
                    IL_0027:  ldc.i4.1
                    IL_0028:  add
                    IL_0029:  stsfld     "int Driver.Count"
                    IL_002e:  leave.s    IL_0048
                  }
                  finally
                  {
                    IL_0030:  ldsfld     "int Driver.Count"
                    IL_0035:  ldloc.0
                    IL_0036:  sub
                    IL_0037:  stsfld     "int Driver.Result"
                    IL_003c:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0041:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0046:  pop
                    IL_0047:  endfinally
                  }
                  IL_0048:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_3()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];  
            arr[0] = 4;  
            
            tests++;
            arr[0] += await GetVal(4);
            if (arr[0] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_3WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];  
            arr[0] = 4;  
            
            tests++;
            arr[0] += await GetVal(4);
            if (arr[0] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x5b }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       92 (0x5c)
                  .maxstack  4
                  .locals init (int V_0, //tests
                                int V_1,
                                int V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldc.i4.4
                    IL_0003:  newarr     "int"
                    IL_0008:  dup
                    IL_0009:  ldc.i4.0
                    IL_000a:  ldc.i4.4
                    IL_000b:  stelem.i4
                    IL_000c:  ldloc.0
                    IL_000d:  ldc.i4.1
                    IL_000e:  add
                    IL_000f:  stloc.0
                    IL_0010:  dup
                    IL_0011:  dup
                    IL_0012:  ldc.i4.0
                    IL_0013:  ldelem.i4
                    IL_0014:  pop
                    IL_0015:  dup
                    IL_0016:  ldc.i4.0
                    IL_0017:  ldelem.i4
                    IL_0018:  stloc.1
                    IL_0019:  ldarg.0
                    IL_001a:  ldc.i4.4
                    IL_001b:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0025:  stloc.2
                    IL_0026:  ldc.i4.0
                    IL_0027:  ldelema    "int"
                    IL_002c:  ldloc.1
                    IL_002d:  ldloc.2
                    IL_002e:  add
                    IL_002f:  stind.i4
                    IL_0030:  ldc.i4.0
                    IL_0031:  ldelem.i4
                    IL_0032:  ldc.i4.8
                    IL_0033:  bne.un.s   IL_0041
                    IL_0035:  ldsfld     "int Driver.Count"
                    IL_003a:  ldc.i4.1
                    IL_003b:  add
                    IL_003c:  stsfld     "int Driver.Count"
                    IL_0041:  leave.s    IL_005b
                  }
                  finally
                  {
                    IL_0043:  ldsfld     "int Driver.Count"
                    IL_0048:  ldloc.0
                    IL_0049:  sub
                    IL_004a:  stsfld     "int Driver.Result"
                    IL_004f:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0054:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0059:  pop
                    IL_005a:  endfinally
                  }
                  IL_005b:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_4()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 0, 0, 0 };

            tests++;
            arr[1] += await (GetVal(arr[0]));
            if (arr[1] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_4WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 0, 0, 0 };

            tests++;
            arr[1] += await (GetVal(arr[0]));
            if (arr[1] == 8)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x5f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       96 (0x60)
                  .maxstack  4
                  .locals init (int V_0, //tests
                                int[] V_1, //arr
                                int V_2,
                                int V_3)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldc.i4.4
                    IL_0003:  newarr     "int"
                    IL_0008:  dup
                    IL_0009:  ldc.i4.0
                    IL_000a:  ldc.i4.8
                    IL_000b:  stelem.i4
                    IL_000c:  stloc.1
                    IL_000d:  ldloc.0
                    IL_000e:  ldc.i4.1
                    IL_000f:  add
                    IL_0010:  stloc.0
                    IL_0011:  ldloc.1
                    IL_0012:  dup
                    IL_0013:  ldc.i4.1
                    IL_0014:  ldelem.i4
                    IL_0015:  pop
                    IL_0016:  dup
                    IL_0017:  ldc.i4.1
                    IL_0018:  ldelem.i4
                    IL_0019:  stloc.2
                    IL_001a:  ldarg.0
                    IL_001b:  ldloc.1
                    IL_001c:  ldc.i4.0
                    IL_001d:  ldelem.i4
                    IL_001e:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0023:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0028:  stloc.3
                    IL_0029:  ldc.i4.1
                    IL_002a:  ldelema    "int"
                    IL_002f:  ldloc.2
                    IL_0030:  ldloc.3
                    IL_0031:  add
                    IL_0032:  stind.i4
                    IL_0033:  ldloc.1
                    IL_0034:  ldc.i4.1
                    IL_0035:  ldelem.i4
                    IL_0036:  ldc.i4.8
                    IL_0037:  bne.un.s   IL_0045
                    IL_0039:  ldsfld     "int Driver.Count"
                    IL_003e:  ldc.i4.1
                    IL_003f:  add
                    IL_0040:  stsfld     "int Driver.Count"
                    IL_0045:  leave.s    IL_005f
                  }
                  finally
                  {
                    IL_0047:  ldsfld     "int Driver.Count"
                    IL_004c:  ldloc.0
                    IL_004d:  sub
                    IL_004e:  stsfld     "int Driver.Result"
                    IL_0053:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0058:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_005d:  pop
                    IL_005e:  endfinally
                  }
                  IL_005f:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_5()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 8, 0, 0 };
            
            tests++;
            arr[1] += await (GetVal(arr[await GetVal(0)]));
            if (arr[1] == 16)
                Driver.Count++;

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_5WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[] { 8, 8, 0, 0 };
            
            tests++;
            arr[1] += await (GetVal(arr[await GetVal(0)]));
            if (arr[1] == 16)
                Driver.Count++;

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0xa8 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size      169 (0xa9)
                  .maxstack  4
                  .locals init (int[] V_0,
                                int V_1, //tests
                                int V_2,
                                int V_3,
                                int[] V_4,
                                int V_5)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.1
                  .try
                  {
                    IL_0002:  ldc.i4.4
                    IL_0003:  newarr     "int"
                    IL_0008:  dup
                    IL_0009:  ldc.i4.0
                    IL_000a:  ldc.i4.8
                    IL_000b:  stelem.i4
                    IL_000c:  dup
                    IL_000d:  ldc.i4.1
                    IL_000e:  ldc.i4.8
                    IL_000f:  stelem.i4
                    IL_0010:  ldloc.1
                    IL_0011:  ldc.i4.1
                    IL_0012:  add
                    IL_0013:  stloc.1
                    IL_0014:  dup
                    IL_0015:  stloc.0
                    IL_0016:  ldloc.0
                    IL_0017:  ldc.i4.1
                    IL_0018:  ldelem.i4
                    IL_0019:  pop
                    IL_001a:  ldloc.0
                    IL_001b:  ldc.i4.1
                    IL_001c:  ldelem.i4
                    IL_001d:  stloc.2
                    IL_001e:  dup
                    IL_001f:  stloc.s    V_4
                    IL_0021:  ldarg.0
                    IL_0022:  ldc.i4.0
                    IL_0023:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0028:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_002d:  stloc.s    V_5
                    IL_002f:  ldarg.0
                    IL_0030:  ldloc.s    V_4
                    IL_0032:  ldloc.s    V_5
                    IL_0034:  ldelem.i4
                    IL_0035:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_003a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003f:  stloc.3
                    IL_0040:  ldloc.0
                    IL_0041:  ldc.i4.1
                    IL_0042:  ldelema    "int"
                    IL_0047:  ldloc.2
                    IL_0048:  ldloc.3
                    IL_0049:  add
                    IL_004a:  stind.i4
                    IL_004b:  dup
                    IL_004c:  ldc.i4.1
                    IL_004d:  ldelem.i4
                    IL_004e:  ldc.i4.s   16
                    IL_0050:  bne.un.s   IL_005e
                    IL_0052:  ldsfld     "int Driver.Count"
                    IL_0057:  ldc.i4.1
                    IL_0058:  add
                    IL_0059:  stsfld     "int Driver.Count"
                    IL_005e:  ldloc.1
                    IL_005f:  ldc.i4.1
                    IL_0060:  add
                    IL_0061:  stloc.1
                    IL_0062:  dup
                    IL_0063:  ldarg.0
                    IL_0064:  ldc.i4.2
                    IL_0065:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_006a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_006f:  stloc.3
                    IL_0070:  ldloc.3
                    IL_0071:  ldelema    "int"
                    IL_0076:  dup
                    IL_0077:  ldind.i4
                    IL_0078:  stloc.2
                    IL_0079:  ldloc.2
                    IL_007a:  ldc.i4.1
                    IL_007b:  add
                    IL_007c:  stind.i4
                    IL_007d:  ldc.i4.2
                    IL_007e:  ldelem.i4
                    IL_007f:  ldc.i4.1
                    IL_0080:  bne.un.s   IL_008e
                    IL_0082:  ldsfld     "int Driver.Count"
                    IL_0087:  ldc.i4.1
                    IL_0088:  add
                    IL_0089:  stsfld     "int Driver.Count"
                    IL_008e:  leave.s    IL_00a8
                  }
                  finally
                  {
                    IL_0090:  ldsfld     "int Driver.Count"
                    IL_0095:  ldloc.1
                    IL_0096:  sub
                    IL_0097:  stsfld     "int Driver.Result"
                    IL_009c:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_00a1:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_00a6:  pop
                    IL_00a7:  endfinally
                  }
                  IL_00a8:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray02_6()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray02_6WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[] arr = new int[4];

            tests++;
            arr[await GetVal(2)]++;
            if (arr[2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x52 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       83 (0x53)
                  .maxstack  4
                  .locals init (int V_0, //tests
                                int V_1,
                                int V_2)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldc.i4.4
                    IL_0003:  newarr     "int"
                    IL_0008:  ldloc.0
                    IL_0009:  ldc.i4.1
                    IL_000a:  add
                    IL_000b:  stloc.0
                    IL_000c:  dup
                    IL_000d:  ldarg.0
                    IL_000e:  ldc.i4.2
                    IL_000f:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0014:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0019:  stloc.1
                    IL_001a:  ldloc.1
                    IL_001b:  ldelema    "int"
                    IL_0020:  dup
                    IL_0021:  ldind.i4
                    IL_0022:  stloc.2
                    IL_0023:  ldloc.2
                    IL_0024:  ldc.i4.1
                    IL_0025:  add
                    IL_0026:  stind.i4
                    IL_0027:  ldc.i4.2
                    IL_0028:  ldelem.i4
                    IL_0029:  ldc.i4.1
                    IL_002a:  bne.un.s   IL_0038
                    IL_002c:  ldsfld     "int Driver.Count"
                    IL_0031:  ldc.i4.1
                    IL_0032:  add
                    IL_0033:  stsfld     "int Driver.Count"
                    IL_0038:  leave.s    IL_0052
                  }
                  finally
                  {
                    IL_003a:  ldsfld     "int Driver.Count"
                    IL_003f:  ldloc.0
                    IL_0040:  sub
                    IL_0041:  stsfld     "int Driver.Result"
                    IL_0046:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_004b:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0050:  pop
                    IL_0051:  endfinally
                  }
                  IL_0052:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[,] arr = new int[await GetVal(4), await GetVal(4)];

            tests++;
            arr[0, 0] = await GetVal(4);
            if (arr[0, await (GetVal(0))] == 4)
                Driver.Count++;

            tests++;
            arr[0, 0] += await GetVal(4);
            if (arr[0, 0] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, 0]));
            if (arr[1, 1] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, await GetVal(0)]));
            if (arr[1, 1] == 16)
                Driver.Count++;

            tests++;
            arr[2, await GetVal(2)]++;
            if (arr[2, 2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArray03WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int tests = 0;
        try
        {
            int[,] arr = new int[await GetVal(4), await GetVal(4)];

            tests++;
            arr[0, 0] = await GetVal(4);
            if (arr[0, await (GetVal(0))] == 4)
                Driver.Count++;

            tests++;
            arr[0, 0] += await GetVal(4);
            if (arr[0, 0] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, 0]));
            if (arr[1, 1] == 8)
                Driver.Count++;

            tests++;
            arr[1, 1] += await (GetVal(arr[0, await GetVal(0)]));
            if (arr[1, 1] == 16)
                Driver.Count++;

            tests++;
            arr[2, await GetVal(2)]++;
            if (arr[2, 2] == 1)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x1a5 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size      422 (0x1a6)
                  .maxstack  5
                  .locals init (int V_0, //tests
                                int[,] V_1, //arr
                                int V_2,
                                int V_3,
                                int[,] V_4,
                                int V_5)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldarg.0
                    IL_0003:  ldc.i4.4
                    IL_0004:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0009:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_000e:  ldarg.0
                    IL_000f:  ldc.i4.4
                    IL_0010:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0015:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_001a:  stloc.2
                    IL_001b:  ldloc.2
                    IL_001c:  newobj     "int[*,*]..ctor"
                    IL_0021:  stloc.1
                    IL_0022:  ldloc.0
                    IL_0023:  ldc.i4.1
                    IL_0024:  add
                    IL_0025:  stloc.0
                    IL_0026:  ldloc.1
                    IL_0027:  ldarg.0
                    IL_0028:  ldc.i4.4
                    IL_0029:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_002e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0033:  stloc.2
                    IL_0034:  ldc.i4.0
                    IL_0035:  ldc.i4.0
                    IL_0036:  ldloc.2
                    IL_0037:  call       "int[*,*].Set"
                    IL_003c:  ldloc.1
                    IL_003d:  ldarg.0
                    IL_003e:  ldc.i4.0
                    IL_003f:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0044:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0049:  stloc.2
                    IL_004a:  ldc.i4.0
                    IL_004b:  ldloc.2
                    IL_004c:  call       "int[*,*].Get"
                    IL_0051:  ldc.i4.4
                    IL_0052:  bne.un.s   IL_0060
                    IL_0054:  ldsfld     "int Driver.Count"
                    IL_0059:  ldc.i4.1
                    IL_005a:  add
                    IL_005b:  stsfld     "int Driver.Count"
                    IL_0060:  ldloc.0
                    IL_0061:  ldc.i4.1
                    IL_0062:  add
                    IL_0063:  stloc.0
                    IL_0064:  ldloc.1
                    IL_0065:  dup
                    IL_0066:  ldc.i4.0
                    IL_0067:  ldc.i4.0
                    IL_0068:  call       "int[*,*].Get"
                    IL_006d:  pop
                    IL_006e:  dup
                    IL_006f:  ldc.i4.0
                    IL_0070:  ldc.i4.0
                    IL_0071:  call       "int[*,*].Get"
                    IL_0076:  stloc.2
                    IL_0077:  ldarg.0
                    IL_0078:  ldc.i4.4
                    IL_0079:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_007e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0083:  stloc.3
                    IL_0084:  ldc.i4.0
                    IL_0085:  ldc.i4.0
                    IL_0086:  call       "int[*,*].Address"
                    IL_008b:  ldloc.2
                    IL_008c:  ldloc.3
                    IL_008d:  add
                    IL_008e:  stind.i4
                    IL_008f:  ldloc.1
                    IL_0090:  ldc.i4.0
                    IL_0091:  ldc.i4.0
                    IL_0092:  call       "int[*,*].Get"
                    IL_0097:  ldc.i4.8
                    IL_0098:  bne.un.s   IL_00a6
                    IL_009a:  ldsfld     "int Driver.Count"
                    IL_009f:  ldc.i4.1
                    IL_00a0:  add
                    IL_00a1:  stsfld     "int Driver.Count"
                    IL_00a6:  ldloc.0
                    IL_00a7:  ldc.i4.1
                    IL_00a8:  add
                    IL_00a9:  stloc.0
                    IL_00aa:  ldloc.1
                    IL_00ab:  dup
                    IL_00ac:  ldc.i4.1
                    IL_00ad:  ldc.i4.1
                    IL_00ae:  call       "int[*,*].Get"
                    IL_00b3:  pop
                    IL_00b4:  dup
                    IL_00b5:  ldc.i4.1
                    IL_00b6:  ldc.i4.1
                    IL_00b7:  call       "int[*,*].Get"
                    IL_00bc:  stloc.3
                    IL_00bd:  ldarg.0
                    IL_00be:  ldloc.1
                    IL_00bf:  ldc.i4.0
                    IL_00c0:  ldc.i4.0
                    IL_00c1:  call       "int[*,*].Get"
                    IL_00c6:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00cb:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00d0:  stloc.2
                    IL_00d1:  ldc.i4.1
                    IL_00d2:  ldc.i4.1
                    IL_00d3:  call       "int[*,*].Address"
                    IL_00d8:  ldloc.3
                    IL_00d9:  ldloc.2
                    IL_00da:  add
                    IL_00db:  stind.i4
                    IL_00dc:  ldloc.1
                    IL_00dd:  ldc.i4.1
                    IL_00de:  ldc.i4.1
                    IL_00df:  call       "int[*,*].Get"
                    IL_00e4:  ldc.i4.8
                    IL_00e5:  bne.un.s   IL_00f3
                    IL_00e7:  ldsfld     "int Driver.Count"
                    IL_00ec:  ldc.i4.1
                    IL_00ed:  add
                    IL_00ee:  stsfld     "int Driver.Count"
                    IL_00f3:  ldloc.0
                    IL_00f4:  ldc.i4.1
                    IL_00f5:  add
                    IL_00f6:  stloc.0
                    IL_00f7:  ldloc.1
                    IL_00f8:  dup
                    IL_00f9:  ldc.i4.1
                    IL_00fa:  ldc.i4.1
                    IL_00fb:  call       "int[*,*].Get"
                    IL_0100:  pop
                    IL_0101:  dup
                    IL_0102:  ldc.i4.1
                    IL_0103:  ldc.i4.1
                    IL_0104:  call       "int[*,*].Get"
                    IL_0109:  stloc.2
                    IL_010a:  ldloc.1
                    IL_010b:  stloc.s    V_4
                    IL_010d:  ldarg.0
                    IL_010e:  ldc.i4.0
                    IL_010f:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0114:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0119:  stloc.s    V_5
                    IL_011b:  ldarg.0
                    IL_011c:  ldloc.s    V_4
                    IL_011e:  ldc.i4.0
                    IL_011f:  ldloc.s    V_5
                    IL_0121:  call       "int[*,*].Get"
                    IL_0126:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_012b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0130:  stloc.3
                    IL_0131:  ldc.i4.1
                    IL_0132:  ldc.i4.1
                    IL_0133:  call       "int[*,*].Address"
                    IL_0138:  ldloc.2
                    IL_0139:  ldloc.3
                    IL_013a:  add
                    IL_013b:  stind.i4
                    IL_013c:  ldloc.1
                    IL_013d:  ldc.i4.1
                    IL_013e:  ldc.i4.1
                    IL_013f:  call       "int[*,*].Get"
                    IL_0144:  ldc.i4.s   16
                    IL_0146:  bne.un.s   IL_0154
                    IL_0148:  ldsfld     "int Driver.Count"
                    IL_014d:  ldc.i4.1
                    IL_014e:  add
                    IL_014f:  stsfld     "int Driver.Count"
                    IL_0154:  ldloc.0
                    IL_0155:  ldc.i4.1
                    IL_0156:  add
                    IL_0157:  stloc.0
                    IL_0158:  ldloc.1
                    IL_0159:  ldarg.0
                    IL_015a:  ldc.i4.2
                    IL_015b:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0160:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0165:  stloc.3
                    IL_0166:  ldc.i4.2
                    IL_0167:  ldloc.3
                    IL_0168:  call       "int[*,*].Address"
                    IL_016d:  dup
                    IL_016e:  ldind.i4
                    IL_016f:  stloc.2
                    IL_0170:  ldloc.2
                    IL_0171:  ldc.i4.1
                    IL_0172:  add
                    IL_0173:  stind.i4
                    IL_0174:  ldloc.1
                    IL_0175:  ldc.i4.2
                    IL_0176:  ldc.i4.2
                    IL_0177:  call       "int[*,*].Get"
                    IL_017c:  ldc.i4.1
                    IL_017d:  bne.un.s   IL_018b
                    IL_017f:  ldsfld     "int Driver.Count"
                    IL_0184:  ldc.i4.1
                    IL_0185:  add
                    IL_0186:  stsfld     "int Driver.Count"
                    IL_018b:  leave.s    IL_01a5
                  }
                  finally
                  {
                    IL_018d:  ldsfld     "int Driver.Count"
                    IL_0192:  ldloc.0
                    IL_0193:  sub
                    IL_0194:  stsfld     "int Driver.Result"
                    IL_0199:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_019e:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_01a3:  pop
                    IL_01a4:  endfinally
                  }
                  IL_01a5:  ret
                }
                """);
        }

        [Fact]
        public void SpillArray04()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

struct MyStruct<T>
{
    T t { get; set; }
    public T this[T index]
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
        }
    }
}

struct TestCase
{
    public async void Run()
    {
        try
        {
            MyStruct<int> ms = new MyStruct<int>();
            var x = ms[index: await Goo()];
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
    public async Task<int> Goo()
    {
        await Task.Delay(1);
        return 1;
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();
        CompletedSignal.WaitOne();
    }
}";
            CompileAndVerify(source, "");
        }

        [Fact]
        public void SpillArray04WithTaskAndRuntimeAsync()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

struct MyStruct<T>
{
    T t { get; set; }
    public T this[T index]
    {
        get
        {
            return t;
        }
        set
        {
            t = value;
        }
    }
}

struct TestCase
{
    public async Task Run()
    {
        try
        {
            MyStruct<int> ms = new MyStruct<int>();
            var x = ms[index: await Goo()];
        }
        finally
        {
            Driver.CompletedSignal.Set();
        }
    }
    public async Task<int> Goo()
    {
        await Task.Delay(1);
        return 1;
    }
}

class Driver
{
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014
        CompletedSignal.WaitOne();
    }
}";
            CompileAndVerify(source, "");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(""), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Return value missing on the stack. { Offset = 0x33 }
                    [Goo]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size       52 (0x34)
                  .maxstack  2
                  .locals init (TestCase V_0,
                                MyStruct<int> V_1, //ms
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "TestCase"
                  IL_0006:  stloc.0
                  .try
                  {
                    IL_0007:  ldloca.s   V_1
                    IL_0009:  initobj    "MyStruct<int>"
                    IL_000f:  ldloca.s   V_0
                    IL_0011:  call       "System.Threading.Tasks.Task<int> TestCase.Goo()"
                    IL_0016:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_001b:  stloc.2
                    IL_001c:  ldloca.s   V_1
                    IL_001e:  ldloc.2
                    IL_001f:  call       "int MyStruct<int>.this[int].get"
                    IL_0024:  pop
                    IL_0025:  leave.s    IL_0033
                  }
                  finally
                  {
                    IL_0027:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_002c:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0031:  pop
                    IL_0032:  endfinally
                  }
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayAssign()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class TestCase
{
    static int[] arr = new int[4];

    static async Task Run()
    {
        arr[0] = await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Task task = Run();
        task.Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Return value missing on the stack. { Offset = 0x37 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size       56 (0x38)
                  .maxstack  4
                  .locals init (int V_0)
                  IL_0000:  ldsfld     "int[] TestCase.arr"
                  IL_0005:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_000a:  ldsfld     "System.Func<int> TestCase.<>c.<>9__1_0"
                  IL_000f:  dup
                  IL_0010:  brtrue.s   IL_0029
                  IL_0012:  pop
                  IL_0013:  ldsfld     "TestCase.<>c TestCase.<>c.<>9"
                  IL_0018:  ldftn      "int TestCase.<>c.<Run>b__1_0()"
                  IL_001e:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0023:  dup
                  IL_0024:  stsfld     "System.Func<int> TestCase.<>c.<>9__1_0"
                  IL_0029:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_002e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0033:  stloc.0
                  IL_0034:  ldc.i4.0
                  IL_0035:  ldloc.0
                  IL_0036:  stelem.i4
                  IL_0037:  ret
                }
                """);
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void SpillArrayAssign2()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static int[] array = new int[5];

    static void Main(string[] args)
    {
        try
        {
            System.Console.WriteLine(""test not awaited"");
            TestNotAwaited().Wait();
        }
        catch
        {
            System.Console.WriteLine(""exception thrown"");
        }

    System.Console.WriteLine();

        try
        {
            System.Console.WriteLine(""test awaited"");
            TestAwaited().Wait();
        }
        catch
        {
            System.Console.WriteLine(""exception thrown"");
        }

    }

    static async Task TestNotAwaited()
    {
        array[6] = Moo1();
    }

    static async Task TestAwaited()
    {
        array[6] = await Moo();
    }

    static int Moo1()
    {
        System.Console.WriteLine(""hello"");
        return 123;
    }

    static async Task<int> Moo()
    {
        System.Console.WriteLine(""hello"");
        return 123;
    }
}";

            var expectedOutput = @"
test not awaited
hello
exception thrown

test awaited
hello
exception thrown
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [TestNotAwaited]: Return value missing on the stack. { Offset = 0xc }
                    [TestAwaited]: Return value missing on the stack. { Offset = 0x13 }
                    [Moo]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.TestNotAwaited()", """
                {
                  // Code size       13 (0xd)
                  .maxstack  3
                  IL_0000:  ldsfld     "int[] Program.array"
                  IL_0005:  ldc.i4.6
                  IL_0006:  call       "int Program.Moo1()"
                  IL_000b:  stelem.i4
                  IL_000c:  ret
                }
                """);
            verifier.VerifyIL("Program.TestAwaited()", """
                {
                  // Code size       20 (0x14)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldsfld     "int[] Program.array"
                  IL_0005:  call       "System.Threading.Tasks.Task<int> Program.Moo()"
                  IL_000a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000f:  stloc.0
                  IL_0010:  ldc.i4.6
                  IL_0011:  ldloc.0
                  IL_0012:  stelem.i4
                  IL_0013:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayLocal()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run<T>(T t) where T : struct
    {
        int[] arr = new int[2] { -1, 42 };

        int tests = 0;
        try
        {
            tests++;
            int t1 = arr[await GetVal(1)];
            if (t1 == 42)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run(6);

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArrayLocalWithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run<T>(T t) where T : struct
    {
        int[] arr = new int[2] { -1, 42 };

        int tests = 0;
        try
        {
            tests++;
            int t1 = arr[await GetVal(1)];
            if (t1 == 42)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run(6);
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x50 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run<T>(T)", """
                {
                  // Code size       81 (0x51)
                  .maxstack  4
                  .locals init (int[] V_0, //arr
                                int V_1, //tests
                                int V_2)
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.m1
                  IL_0009:  stelem.i4
                  IL_000a:  dup
                  IL_000b:  ldc.i4.1
                  IL_000c:  ldc.i4.s   42
                  IL_000e:  stelem.i4
                  IL_000f:  stloc.0
                  IL_0010:  ldc.i4.0
                  IL_0011:  stloc.1
                  .try
                  {
                    IL_0012:  ldloc.1
                    IL_0013:  ldc.i4.1
                    IL_0014:  add
                    IL_0015:  stloc.1
                    IL_0016:  ldloc.0
                    IL_0017:  ldarg.0
                    IL_0018:  ldc.i4.1
                    IL_0019:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_001e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0023:  stloc.2
                    IL_0024:  ldloc.2
                    IL_0025:  ldelem.i4
                    IL_0026:  ldc.i4.s   42
                    IL_0028:  bne.un.s   IL_0036
                    IL_002a:  ldsfld     "int Driver.Count"
                    IL_002f:  ldc.i4.1
                    IL_0030:  add
                    IL_0031:  stsfld     "int Driver.Count"
                    IL_0036:  leave.s    IL_0050
                  }
                  finally
                  {
                    IL_0038:  ldsfld     "int Driver.Count"
                    IL_003d:  ldloc.1
                    IL_003e:  sub
                    IL_003f:  stsfld     "int Driver.Result"
                    IL_0044:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0049:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_004e:  pop
                    IL_004f:  endfinally
                  }
                  IL_0050:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValue()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[0] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Return value missing on the stack. { Offset = 0x51 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Driver.Run()", """
                {
                  // Code size       82 (0x52)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1)
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "int"
                  IL_0006:  stsfld     "int[] Driver.arr"
                  IL_000b:  ldsfld     "int[] Driver.arr"
                  IL_0010:  dup
                  IL_0011:  ldc.i4.0
                  IL_0012:  ldelem.i4
                  IL_0013:  pop
                  IL_0014:  dup
                  IL_0015:  ldc.i4.0
                  IL_0016:  ldelem.i4
                  IL_0017:  stloc.0
                  IL_0018:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_001d:  ldsfld     "System.Func<int> Driver.<>c.<>9__1_0"
                  IL_0022:  dup
                  IL_0023:  brtrue.s   IL_003c
                  IL_0025:  pop
                  IL_0026:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_002b:  ldftn      "int Driver.<>c.<Run>b__1_0()"
                  IL_0031:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0036:  dup
                  IL_0037:  stsfld     "System.Func<int> Driver.<>c.<>9__1_0"
                  IL_003c:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0041:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0046:  stloc.1
                  IL_0047:  ldc.i4.0
                  IL_0048:  ldelema    "int"
                  IL_004d:  ldloc.0
                  IL_004e:  ldloc.1
                  IL_004f:  add
                  IL_0050:  stind.i4
                  IL_0051:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Driver
{
    static int[] arr;

    static async Task Run()
    {
        arr = new int[1];
        arr[await Task.Factory.StartNew(() => 0)] += await Task.Factory.StartNew(() => 42);
    }

    static void Main()
    {
        Run().Wait();
        Console.WriteLine(arr[0]);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Return value missing on the stack. { Offset = 0x82 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Driver.Run()", """
                {
                  // Code size      131 (0x83)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1,
                                int V_2,
                                int V_3)
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "int"
                  IL_0006:  stsfld     "int[] Driver.arr"
                  IL_000b:  ldsfld     "int[] Driver.arr"
                  IL_0010:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_0015:  ldsfld     "System.Func<int> Driver.<>c.<>9__1_0"
                  IL_001a:  dup
                  IL_001b:  brtrue.s   IL_0034
                  IL_001d:  pop
                  IL_001e:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_0023:  ldftn      "int Driver.<>c.<Run>b__1_0()"
                  IL_0029:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_002e:  dup
                  IL_002f:  stsfld     "System.Func<int> Driver.<>c.<>9__1_0"
                  IL_0034:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0039:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_003e:  stloc.1
                  IL_003f:  ldloc.1
                  IL_0040:  stloc.0
                  IL_0041:  dup
                  IL_0042:  ldloc.0
                  IL_0043:  ldelem.i4
                  IL_0044:  pop
                  IL_0045:  dup
                  IL_0046:  ldloc.0
                  IL_0047:  ldelem.i4
                  IL_0048:  stloc.2
                  IL_0049:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_004e:  ldsfld     "System.Func<int> Driver.<>c.<>9__1_1"
                  IL_0053:  dup
                  IL_0054:  brtrue.s   IL_006d
                  IL_0056:  pop
                  IL_0057:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_005c:  ldftn      "int Driver.<>c.<Run>b__1_1()"
                  IL_0062:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0067:  dup
                  IL_0068:  stsfld     "System.Func<int> Driver.<>c.<>9__1_1"
                  IL_006d:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0072:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0077:  stloc.3
                  IL_0078:  ldloc.0
                  IL_0079:  ldelema    "int"
                  IL_007e:  ldloc.2
                  IL_007f:  ldloc.3
                  IL_0080:  add
                  IL_0081:  stind.i4
                  IL_0082:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayCompoundAssignmentLValueAwait2()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42);
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Unexpected type on the stack. { Offset = 0xe2, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Driver.Run()", """
                {
                  // Code size      227 (0xe3)
                  .maxstack  5
                  .locals init (int V_0,
                                int V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "S2"
                  IL_0006:  dup
                  IL_0007:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_000c:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_0"
                  IL_0011:  dup
                  IL_0012:  brtrue.s   IL_002b
                  IL_0014:  pop
                  IL_0015:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_001a:  ldftn      "int Driver.<>c.<Run>b__0_0()"
                  IL_0020:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0025:  dup
                  IL_0026:  stsfld     "System.Func<int> Driver.<>c.<>9__0_0"
                  IL_002b:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0030:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0035:  stloc.2
                  IL_0036:  ldloc.2
                  IL_0037:  stloc.0
                  IL_0038:  dup
                  IL_0039:  ldloc.0
                  IL_003a:  ldelema    "S2"
                  IL_003f:  pop
                  IL_0040:  ldloc.0
                  IL_0041:  stloc.1
                  IL_0042:  dup
                  IL_0043:  ldloc.1
                  IL_0044:  ldelema    "S2"
                  IL_0049:  pop
                  IL_004a:  dup
                  IL_004b:  ldloc.1
                  IL_004c:  ldelema    "S2"
                  IL_0051:  ldflda     "S1 S2.s1"
                  IL_0056:  ldfld      "int S1.x"
                  IL_005b:  stloc.3
                  IL_005c:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_0061:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_1"
                  IL_0066:  dup
                  IL_0067:  brtrue.s   IL_0080
                  IL_0069:  pop
                  IL_006a:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_006f:  ldftn      "int Driver.<>c.<Run>b__0_1()"
                  IL_0075:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_007a:  dup
                  IL_007b:  stsfld     "System.Func<int> Driver.<>c.<>9__0_1"
                  IL_0080:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0085:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_008a:  stloc.s    V_4
                  IL_008c:  ldloc.1
                  IL_008d:  ldelema    "S2"
                  IL_0092:  ldflda     "S1 S2.s1"
                  IL_0097:  ldflda     "int S1.x"
                  IL_009c:  ldloc.3
                  IL_009d:  ldloc.s    V_4
                  IL_009f:  add
                  IL_00a0:  stind.i4
                  IL_00a1:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_00a6:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_2"
                  IL_00ab:  dup
                  IL_00ac:  brtrue.s   IL_00c5
                  IL_00ae:  pop
                  IL_00af:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_00b4:  ldftn      "int Driver.<>c.<Run>b__0_2()"
                  IL_00ba:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_00bf:  dup
                  IL_00c0:  stsfld     "System.Func<int> Driver.<>c.<>9__0_2"
                  IL_00c5:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_00ca:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_00cf:  stloc.s    V_4
                  IL_00d1:  ldloc.s    V_4
                  IL_00d3:  ldelema    "S2"
                  IL_00d8:  ldflda     "S1 S2.s1"
                  IL_00dd:  ldfld      "int S1.x"
                  IL_00e2:  ret
                }
                """);
        }

        [Fact]
        public void DoubleSpillArrayCompoundAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct S1
{
    public int x;
}

struct S2
{
    public S1 s1;
}

class Driver
{
    static async Task<int> Run()
    {
        var arr = new S2[1];
        arr[await Task.Factory.StartNew(() => 0)].s1.x += (arr[await Task.Factory.StartNew(() => 0)].s1.x += await Task.Factory.StartNew(() => 42));
        return arr[await Task.Factory.StartNew(() => 0)].s1.x;
    }

    static void Main()
    {
        var t = Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Unexpected type on the stack. { Offset = 0x168, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Driver.Run()", """
                {
                  // Code size      361 (0x169)
                  .maxstack  6
                  .locals init (int V_0,
                                S2[] V_1,
                                int V_2,
                                int V_3,
                                S2[] V_4,
                                int V_5,
                                int V_6,
                                int V_7,
                                int V_8,
                                int& V_9,
                                int V_10,
                                int V_11,
                                int V_12)
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "S2"
                  IL_0006:  dup
                  IL_0007:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_000c:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_0"
                  IL_0011:  dup
                  IL_0012:  brtrue.s   IL_002b
                  IL_0014:  pop
                  IL_0015:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_001a:  ldftn      "int Driver.<>c.<Run>b__0_0()"
                  IL_0020:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0025:  dup
                  IL_0026:  stsfld     "System.Func<int> Driver.<>c.<>9__0_0"
                  IL_002b:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0030:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0035:  stloc.s    V_6
                  IL_0037:  ldloc.s    V_6
                  IL_0039:  stloc.0
                  IL_003a:  dup
                  IL_003b:  ldloc.0
                  IL_003c:  ldelema    "S2"
                  IL_0041:  pop
                  IL_0042:  stloc.1
                  IL_0043:  ldloc.0
                  IL_0044:  stloc.2
                  IL_0045:  ldloc.1
                  IL_0046:  ldloc.2
                  IL_0047:  ldelema    "S2"
                  IL_004c:  pop
                  IL_004d:  ldloc.1
                  IL_004e:  ldloc.2
                  IL_004f:  ldelema    "S2"
                  IL_0054:  ldflda     "S1 S2.s1"
                  IL_0059:  ldfld      "int S1.x"
                  IL_005e:  stloc.s    V_7
                  IL_0060:  dup
                  IL_0061:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_0066:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_1"
                  IL_006b:  dup
                  IL_006c:  brtrue.s   IL_0085
                  IL_006e:  pop
                  IL_006f:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_0074:  ldftn      "int Driver.<>c.<Run>b__0_1()"
                  IL_007a:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_007f:  dup
                  IL_0080:  stsfld     "System.Func<int> Driver.<>c.<>9__0_1"
                  IL_0085:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_008a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_008f:  stloc.s    V_8
                  IL_0091:  ldloc.s    V_8
                  IL_0093:  stloc.3
                  IL_0094:  dup
                  IL_0095:  ldloc.3
                  IL_0096:  ldelema    "S2"
                  IL_009b:  pop
                  IL_009c:  stloc.s    V_4
                  IL_009e:  ldloc.3
                  IL_009f:  stloc.s    V_5
                  IL_00a1:  ldloc.s    V_4
                  IL_00a3:  ldloc.s    V_5
                  IL_00a5:  ldelema    "S2"
                  IL_00aa:  pop
                  IL_00ab:  ldloc.s    V_4
                  IL_00ad:  ldloc.s    V_5
                  IL_00af:  ldelema    "S2"
                  IL_00b4:  ldflda     "S1 S2.s1"
                  IL_00b9:  ldfld      "int S1.x"
                  IL_00be:  stloc.s    V_10
                  IL_00c0:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_00c5:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_2"
                  IL_00ca:  dup
                  IL_00cb:  brtrue.s   IL_00e4
                  IL_00cd:  pop
                  IL_00ce:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_00d3:  ldftn      "int Driver.<>c.<Run>b__0_2()"
                  IL_00d9:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_00de:  dup
                  IL_00df:  stsfld     "System.Func<int> Driver.<>c.<>9__0_2"
                  IL_00e4:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_00e9:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_00ee:  stloc.s    V_11
                  IL_00f0:  ldloc.1
                  IL_00f1:  ldloc.2
                  IL_00f2:  ldelema    "S2"
                  IL_00f7:  ldflda     "S1 S2.s1"
                  IL_00fc:  ldflda     "int S1.x"
                  IL_0101:  ldloc.s    V_7
                  IL_0103:  ldloc.s    V_4
                  IL_0105:  ldloc.s    V_5
                  IL_0107:  ldelema    "S2"
                  IL_010c:  ldflda     "S1 S2.s1"
                  IL_0111:  ldflda     "int S1.x"
                  IL_0116:  stloc.s    V_9
                  IL_0118:  ldloc.s    V_9
                  IL_011a:  ldloc.s    V_10
                  IL_011c:  ldloc.s    V_11
                  IL_011e:  add
                  IL_011f:  dup
                  IL_0120:  stloc.s    V_12
                  IL_0122:  stind.i4
                  IL_0123:  ldloc.s    V_12
                  IL_0125:  add
                  IL_0126:  stind.i4
                  IL_0127:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_012c:  ldsfld     "System.Func<int> Driver.<>c.<>9__0_3"
                  IL_0131:  dup
                  IL_0132:  brtrue.s   IL_014b
                  IL_0134:  pop
                  IL_0135:  ldsfld     "Driver.<>c Driver.<>c.<>9"
                  IL_013a:  ldftn      "int Driver.<>c.<Run>b__0_3()"
                  IL_0140:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0145:  dup
                  IL_0146:  stsfld     "System.Func<int> Driver.<>c.<>9__0_3"
                  IL_014b:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0150:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0155:  stloc.s    V_11
                  IL_0157:  ldloc.s    V_11
                  IL_0159:  ldelema    "S2"
                  IL_015e:  ldflda     "S1 S2.s1"
                  IL_0163:  ldfld      "int S1.x"
                  IL_0168:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79415")]
        public void CompoundAssignment()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                interface I1 
                {
                    int this[int i] { get; set; }
                }

                struct S1 : I1
                {
                    public int F1;

                    public int this[int i]
                    {
                        get
                        {
                            Console.Write(" g" + i);
                            return 0;
                        }
                        set
                        {
                            Console.Write(" s" + i + value);
                        }
                    }
                }

                class Program
                {
                    static async Task Main()
                    {
                        await Test3<S1>();
                    }

                    static T GetT<T>() where T : I1
                    {
                        Console.Write(" t");
                        return (T)(object)new S1 { F1 = 123 };
                    }

                    static async Task Test3<T>() where T : I1
                    {
                        GetT<T>()[Index()] += await Get1Async();
                    }

                    static async Task<int> Get1Async()
                    {
                        Console.Write(" v");
                        await Task.Yield();
                        return 1;
                    }

                    static int Index()
                    {
                        Console.Write(" i");
                        return 2;
                    }
                }
                """;
            var expectedOutput = "t i g2 v s21";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xa }
                    [Test3]: Return value missing on the stack. { Offset = 0x7a }
                    [Get1Async]: Unexpected type on the stack. { Offset = 0x2f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Test3<T>()", """
                {
                  // Code size      123 (0x7b)
                  .maxstack  4
                  .locals init (T V_0,
                                T V_1,
                                int V_2,
                                int V_3,
                                int V_4,
                                int V_5,
                                T V_6)
                  IL_0000:  call       "T Program.GetT<T>()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloca.s   V_6
                  IL_0008:  initobj    "T"
                  IL_000e:  ldloc.s    V_6
                  IL_0010:  box        "T"
                  IL_0015:  brtrue.s   IL_0019
                  IL_0017:  ldloc.0
                  IL_0018:  stloc.1
                  IL_0019:  call       "int Program.Index()"
                  IL_001e:  stloc.2
                  IL_001f:  ldloc.2
                  IL_0020:  stloc.3
                  IL_0021:  ldloca.s   V_6
                  IL_0023:  initobj    "T"
                  IL_0029:  ldloc.s    V_6
                  IL_002b:  box        "T"
                  IL_0030:  brtrue.s   IL_0036
                  IL_0032:  ldloca.s   V_1
                  IL_0034:  br.s       IL_0038
                  IL_0036:  ldloca.s   V_0
                  IL_0038:  ldloc.2
                  IL_0039:  constrained. "T"
                  IL_003f:  callvirt   "int I1.this[int].get"
                  IL_0044:  stloc.s    V_4
                  IL_0046:  call       "System.Threading.Tasks.Task<int> Program.Get1Async()"
                  IL_004b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0050:  stloc.s    V_5
                  IL_0052:  ldloca.s   V_6
                  IL_0054:  initobj    "T"
                  IL_005a:  ldloc.s    V_6
                  IL_005c:  box        "T"
                  IL_0061:  brtrue.s   IL_0067
                  IL_0063:  ldloca.s   V_1
                  IL_0065:  br.s       IL_0069
                  IL_0067:  ldloca.s   V_0
                  IL_0069:  ldloc.3
                  IL_006a:  ldloc.s    V_4
                  IL_006c:  ldloc.s    V_5
                  IL_006e:  add
                  IL_006f:  constrained. "T"
                  IL_0075:  callvirt   "void I1.this[int].set"
                  IL_007a:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayInitializers1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    new []{4,await GetVal(5),await GetVal(6)}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 5 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            int[][] arr2 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArrayInitializers1WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    new []{4,await GetVal(5),await GetVal(6)}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 5 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            int[][] arr2 = new[]
                {
                    new []{await GetVal(2),await GetVal(3)},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x11b }
                    [Goo]: Unexpected type on the stack. { Offset = 0x1c, Found = ref 'int32[]', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32[]>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      284 (0x11c)
                  .maxstack  7
                  .locals init (int V_0, //tests
                                int[][] V_1, //arr1
                                int[][] V_2, //arr2
                                int V_3,
                                int V_4,
                                int[] V_5,
                                int V_6,
                                int V_7,
                                int[] V_8)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.0
                    IL_0006:  ldarg.0
                    IL_0007:  ldc.i4.2
                    IL_0008:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0012:  stloc.3
                    IL_0013:  ldarg.0
                    IL_0014:  ldc.i4.3
                    IL_0015:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_001a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_001f:  stloc.s    V_4
                    IL_0021:  ldc.i4.2
                    IL_0022:  newarr     "int"
                    IL_0027:  dup
                    IL_0028:  ldc.i4.0
                    IL_0029:  ldloc.3
                    IL_002a:  stelem.i4
                    IL_002b:  dup
                    IL_002c:  ldc.i4.1
                    IL_002d:  ldloc.s    V_4
                    IL_002f:  stelem.i4
                    IL_0030:  stloc.s    V_5
                    IL_0032:  ldarg.0
                    IL_0033:  ldc.i4.5
                    IL_0034:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0039:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003e:  stloc.s    V_6
                    IL_0040:  ldarg.0
                    IL_0041:  ldc.i4.6
                    IL_0042:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0047:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_004c:  stloc.s    V_7
                    IL_004e:  ldc.i4.2
                    IL_004f:  newarr     "int[]"
                    IL_0054:  dup
                    IL_0055:  ldc.i4.0
                    IL_0056:  ldloc.s    V_5
                    IL_0058:  stelem.ref
                    IL_0059:  dup
                    IL_005a:  ldc.i4.1
                    IL_005b:  ldc.i4.3
                    IL_005c:  newarr     "int"
                    IL_0061:  dup
                    IL_0062:  ldc.i4.0
                    IL_0063:  ldc.i4.4
                    IL_0064:  stelem.i4
                    IL_0065:  dup
                    IL_0066:  ldc.i4.1
                    IL_0067:  ldloc.s    V_6
                    IL_0069:  stelem.i4
                    IL_006a:  dup
                    IL_006b:  ldc.i4.2
                    IL_006c:  ldloc.s    V_7
                    IL_006e:  stelem.i4
                    IL_006f:  stelem.ref
                    IL_0070:  stloc.1
                    IL_0071:  ldloc.1
                    IL_0072:  ldc.i4.0
                    IL_0073:  ldelem.ref
                    IL_0074:  ldc.i4.1
                    IL_0075:  ldelem.i4
                    IL_0076:  ldc.i4.3
                    IL_0077:  bne.un.s   IL_0095
                    IL_0079:  ldloc.1
                    IL_007a:  ldc.i4.1
                    IL_007b:  ldelem.ref
                    IL_007c:  ldc.i4.1
                    IL_007d:  ldelem.i4
                    IL_007e:  ldc.i4.5
                    IL_007f:  bne.un.s   IL_0095
                    IL_0081:  ldloc.1
                    IL_0082:  ldc.i4.1
                    IL_0083:  ldelem.ref
                    IL_0084:  ldc.i4.2
                    IL_0085:  ldelem.i4
                    IL_0086:  ldc.i4.6
                    IL_0087:  bne.un.s   IL_0095
                    IL_0089:  ldsfld     "int Driver.Count"
                    IL_008e:  ldc.i4.1
                    IL_008f:  add
                    IL_0090:  stsfld     "int Driver.Count"
                    IL_0095:  ldloc.0
                    IL_0096:  ldc.i4.1
                    IL_0097:  add
                    IL_0098:  stloc.0
                    IL_0099:  ldarg.0
                    IL_009a:  ldc.i4.2
                    IL_009b:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00a0:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00a5:  stloc.s    V_7
                    IL_00a7:  ldarg.0
                    IL_00a8:  ldc.i4.3
                    IL_00a9:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00ae:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00b3:  stloc.s    V_6
                    IL_00b5:  ldc.i4.2
                    IL_00b6:  newarr     "int"
                    IL_00bb:  dup
                    IL_00bc:  ldc.i4.0
                    IL_00bd:  ldloc.s    V_7
                    IL_00bf:  stelem.i4
                    IL_00c0:  dup
                    IL_00c1:  ldc.i4.1
                    IL_00c2:  ldloc.s    V_6
                    IL_00c4:  stelem.i4
                    IL_00c5:  stloc.s    V_5
                    IL_00c7:  ldarg.0
                    IL_00c8:  call       "System.Threading.Tasks.Task<int[]> TestCase.Goo()"
                    IL_00cd:  call       "int[] System.Runtime.CompilerServices.AsyncHelpers.Await<int[]>(System.Threading.Tasks.Task<int[]>)"
                    IL_00d2:  stloc.s    V_8
                    IL_00d4:  ldc.i4.2
                    IL_00d5:  newarr     "int[]"
                    IL_00da:  dup
                    IL_00db:  ldc.i4.0
                    IL_00dc:  ldloc.s    V_5
                    IL_00de:  stelem.ref
                    IL_00df:  dup
                    IL_00e0:  ldc.i4.1
                    IL_00e1:  ldloc.s    V_8
                    IL_00e3:  stelem.ref
                    IL_00e4:  stloc.2
                    IL_00e5:  ldloc.2
                    IL_00e6:  ldc.i4.0
                    IL_00e7:  ldelem.ref
                    IL_00e8:  ldc.i4.1
                    IL_00e9:  ldelem.i4
                    IL_00ea:  ldc.i4.3
                    IL_00eb:  bne.un.s   IL_0101
                    IL_00ed:  ldloc.2
                    IL_00ee:  ldc.i4.1
                    IL_00ef:  ldelem.ref
                    IL_00f0:  ldc.i4.1
                    IL_00f1:  ldelem.i4
                    IL_00f2:  ldc.i4.2
                    IL_00f3:  bne.un.s   IL_0101
                    IL_00f5:  ldsfld     "int Driver.Count"
                    IL_00fa:  ldc.i4.1
                    IL_00fb:  add
                    IL_00fc:  stsfld     "int Driver.Count"
                    IL_0101:  leave.s    IL_011b
                  }
                  finally
                  {
                    IL_0103:  ldsfld     "int Driver.Count"
                    IL_0108:  ldloc.0
                    IL_0109:  sub
                    IL_010a:  stsfld     "int Driver.Result"
                    IL_010f:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0114:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0119:  pop
                    IL_011a:  endfinally
                  }
                  IL_011b:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayInitializers2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[,] arr1 = 
                {
                    {await GetVal(2),await GetVal(3)},
                    {await GetVal(5),await GetVal(6)}
                };
            if (arr1[0, 1] == 3 && arr1[1, 0] == 5 && arr1[1, 1] == 6)
                Driver.Count++;

            tests++;
            int[,] arr2 = 
                {
                    {await GetVal(2),3},
                    {4,await GetVal(5)}
                };
            if (arr2[0, 1] == 3 && arr2[1, 1] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillArrayInitializers2WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[,] arr1 = 
                {
                    {await GetVal(2),await GetVal(3)},
                    {await GetVal(5),await GetVal(6)}
                };
            if (arr1[0, 1] == 3 && arr1[1, 0] == 5 && arr1[1, 1] == 6)
                Driver.Count++;

            tests++;
            int[,] arr2 = 
                {
                    {await GetVal(2),3},
                    {4,await GetVal(5)}
                };
            if (arr2[0, 1] == 3 && arr2[1, 1] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x123 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      292 (0x124)
                  .maxstack  5
                  .locals init (int V_0, //tests
                                int[,] V_1, //arr1
                                int[,] V_2, //arr2
                                int V_3,
                                int V_4,
                                int V_5,
                                int V_6)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.0
                    IL_0006:  ldarg.0
                    IL_0007:  ldc.i4.2
                    IL_0008:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0012:  stloc.3
                    IL_0013:  ldarg.0
                    IL_0014:  ldc.i4.3
                    IL_0015:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_001a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_001f:  stloc.s    V_4
                    IL_0021:  ldarg.0
                    IL_0022:  ldc.i4.5
                    IL_0023:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0028:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_002d:  stloc.s    V_5
                    IL_002f:  ldarg.0
                    IL_0030:  ldc.i4.6
                    IL_0031:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0036:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003b:  stloc.s    V_6
                    IL_003d:  ldc.i4.2
                    IL_003e:  ldc.i4.2
                    IL_003f:  newobj     "int[*,*]..ctor"
                    IL_0044:  dup
                    IL_0045:  ldc.i4.0
                    IL_0046:  ldc.i4.0
                    IL_0047:  ldloc.3
                    IL_0048:  call       "int[*,*].Set"
                    IL_004d:  dup
                    IL_004e:  ldc.i4.0
                    IL_004f:  ldc.i4.1
                    IL_0050:  ldloc.s    V_4
                    IL_0052:  call       "int[*,*].Set"
                    IL_0057:  dup
                    IL_0058:  ldc.i4.1
                    IL_0059:  ldc.i4.0
                    IL_005a:  ldloc.s    V_5
                    IL_005c:  call       "int[*,*].Set"
                    IL_0061:  dup
                    IL_0062:  ldc.i4.1
                    IL_0063:  ldc.i4.1
                    IL_0064:  ldloc.s    V_6
                    IL_0066:  call       "int[*,*].Set"
                    IL_006b:  stloc.1
                    IL_006c:  ldloc.1
                    IL_006d:  ldc.i4.0
                    IL_006e:  ldc.i4.1
                    IL_006f:  call       "int[*,*].Get"
                    IL_0074:  ldc.i4.3
                    IL_0075:  bne.un.s   IL_0099
                    IL_0077:  ldloc.1
                    IL_0078:  ldc.i4.1
                    IL_0079:  ldc.i4.0
                    IL_007a:  call       "int[*,*].Get"
                    IL_007f:  ldc.i4.5
                    IL_0080:  bne.un.s   IL_0099
                    IL_0082:  ldloc.1
                    IL_0083:  ldc.i4.1
                    IL_0084:  ldc.i4.1
                    IL_0085:  call       "int[*,*].Get"
                    IL_008a:  ldc.i4.6
                    IL_008b:  bne.un.s   IL_0099
                    IL_008d:  ldsfld     "int Driver.Count"
                    IL_0092:  ldc.i4.1
                    IL_0093:  add
                    IL_0094:  stsfld     "int Driver.Count"
                    IL_0099:  ldloc.0
                    IL_009a:  ldc.i4.1
                    IL_009b:  add
                    IL_009c:  stloc.0
                    IL_009d:  ldarg.0
                    IL_009e:  ldc.i4.2
                    IL_009f:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00a4:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00a9:  stloc.s    V_6
                    IL_00ab:  ldarg.0
                    IL_00ac:  ldc.i4.5
                    IL_00ad:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00b2:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00b7:  stloc.s    V_5
                    IL_00b9:  ldc.i4.2
                    IL_00ba:  ldc.i4.2
                    IL_00bb:  newobj     "int[*,*]..ctor"
                    IL_00c0:  dup
                    IL_00c1:  ldc.i4.0
                    IL_00c2:  ldc.i4.0
                    IL_00c3:  ldloc.s    V_6
                    IL_00c5:  call       "int[*,*].Set"
                    IL_00ca:  dup
                    IL_00cb:  ldc.i4.0
                    IL_00cc:  ldc.i4.1
                    IL_00cd:  ldc.i4.3
                    IL_00ce:  call       "int[*,*].Set"
                    IL_00d3:  dup
                    IL_00d4:  ldc.i4.1
                    IL_00d5:  ldc.i4.0
                    IL_00d6:  ldc.i4.4
                    IL_00d7:  call       "int[*,*].Set"
                    IL_00dc:  dup
                    IL_00dd:  ldc.i4.1
                    IL_00de:  ldc.i4.1
                    IL_00df:  ldloc.s    V_5
                    IL_00e1:  call       "int[*,*].Set"
                    IL_00e6:  stloc.2
                    IL_00e7:  ldloc.2
                    IL_00e8:  ldc.i4.0
                    IL_00e9:  ldc.i4.1
                    IL_00ea:  call       "int[*,*].Get"
                    IL_00ef:  ldc.i4.3
                    IL_00f0:  bne.un.s   IL_0109
                    IL_00f2:  ldloc.2
                    IL_00f3:  ldc.i4.1
                    IL_00f4:  ldc.i4.1
                    IL_00f5:  call       "int[*,*].Get"
                    IL_00fa:  ldc.i4.5
                    IL_00fb:  bne.un.s   IL_0109
                    IL_00fd:  ldsfld     "int Driver.Count"
                    IL_0102:  ldc.i4.1
                    IL_0103:  add
                    IL_0104:  stsfld     "int Driver.Count"
                    IL_0109:  leave.s    IL_0123
                  }
                  finally
                  {
                    IL_010b:  ldsfld     "int Driver.Count"
                    IL_0110:  ldloc.0
                    IL_0111:  sub
                    IL_0112:  stsfld     "int Driver.Result"
                    IL_0117:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_011c:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0121:  pop
                    IL_0122:  endfinally
                  }
                  IL_0123:  ret
                }
                """);
        }

        [Fact]
        public void SpillArrayInitializers3()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await Task.Run<int>(async()=>{await Task.Delay(1);return 3;})},
                    new []{await GetVal(5),4,await Task.Run<int>(async()=>{await Task.Delay(1);return 6;})}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 4 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            dynamic arr2 = new[]
                {
                    new []{await GetVal(2),3},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput, references: new[] { CSharpRef });
        }

        [Fact]
        public void SpillArrayInitializers3WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;
        try
        {
            //jagged array
            tests++;
            int[][] arr1 = new[]
                {
                    new []{await GetVal(2),await Task.Run<int>(async()=>{await Task.Delay(1);return 3;})},
                    new []{await GetVal(5),4,await Task.Run<int>(async()=>{await Task.Delay(1);return 6;})}
                };
            if (arr1[0][1] == 3 && arr1[1][1] == 4 && arr1[1][2] == 6)
                Driver.Count++;

            tests++;
            dynamic arr2 = new[]
                {
                    new []{await GetVal(2),3},
                    await Goo()
                };
            if (arr2[0][1] == 3 && arr2[1][1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public async Task<int[]> Goo()
    {
        await Task.Delay(1);
        return new int[] { 1, 2, 3 };
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput, references: new[] { CSharpRef });

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x40b }
                    [Goo]: Unexpected type on the stack. { Offset = 0x1c, Found = ref 'int32[]', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32[]>' }
                    [<Run>b__1_0]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<Run>b__1_1]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics(
                // (61,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         t.Run();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "t.Run()").WithLocation(61, 9)
            );
        }

        [Fact]
        public void SpillNestedExpressionInArrayInitializer()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static async Task<int[,]> Run()
    {
        return new int[,] {
            {1, 2, 21 + (await Task.Factory.StartNew(() => 21)) },
        };
    }

    public static void Main()
    {
        var t = Run();
        t.Wait();
        foreach (var xs in t.Result)
        {
            Console.WriteLine(xs);
        }
    }
}";
            var expectedOutput = @"
1
2
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Unexpected type on the stack. { Offset = 0x54, Found = ref 'int32[,]', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32[,]>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.Run()", """
                {
                  // Code size       85 (0x55)
                  .maxstack  6
                  .locals init (int V_0)
                  IL_0000:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_0005:  ldsfld     "System.Func<int> Test.<>c.<>9__0_0"
                  IL_000a:  dup
                  IL_000b:  brtrue.s   IL_0024
                  IL_000d:  pop
                  IL_000e:  ldsfld     "Test.<>c Test.<>c.<>9"
                  IL_0013:  ldftn      "int Test.<>c.<Run>b__0_0()"
                  IL_0019:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_001e:  dup
                  IL_001f:  stsfld     "System.Func<int> Test.<>c.<>9__0_0"
                  IL_0024:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0029:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_002e:  stloc.0
                  IL_002f:  ldc.i4.1
                  IL_0030:  ldc.i4.3
                  IL_0031:  newobj     "int[*,*]..ctor"
                  IL_0036:  dup
                  IL_0037:  ldc.i4.0
                  IL_0038:  ldc.i4.0
                  IL_0039:  ldc.i4.1
                  IL_003a:  call       "int[*,*].Set"
                  IL_003f:  dup
                  IL_0040:  ldc.i4.0
                  IL_0041:  ldc.i4.1
                  IL_0042:  ldc.i4.2
                  IL_0043:  call       "int[*,*].Set"
                  IL_0048:  dup
                  IL_0049:  ldc.i4.0
                  IL_004a:  ldc.i4.2
                  IL_004b:  ldc.i4.s   21
                  IL_004d:  ldloc.0
                  IL_004e:  add
                  IL_004f:  call       "int[*,*].Set"
                  IL_0054:  ret
                }
                """);
        }

        [Fact]
        public void SpillConditionalAccess()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Test
{

    class C1
    {
        public int M(int x)
        {
            return x;
        }
    }

    public static int Get(int x)
    {
        Console.WriteLine(""> "" + x);
        return x;
    }

    public static async Task<int> F(int x)
    {
        return await Task.Factory.StartNew(() => x);
    }

    public static async Task<int?> G()
    {
        var c = new C1();
        return c?.M(await F(Get(42)));
    }

    public static void Main()
    {
        var t = G();
        System.Console.WriteLine(t.Result);
    }
}";
            var expectedOutput = @"
> 42
42";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F]: Unexpected type on the stack. { Offset = 0x28, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [G]: Unexpected type on the stack. { Offset = 0x34, Found = value '[System.Runtime]System.Nullable`1<int32>', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<System.Nullable`1<int32>>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.G()", """
                {
                  // Code size       53 (0x35)
                  .maxstack  3
                  .locals init (Test.C1 V_0,
                                int? V_1,
                                int V_2)
                  IL_0000:  newobj     "Test.C1..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  brfalse.s  IL_002b
                  IL_0009:  ldc.i4.s   42
                  IL_000b:  call       "int Test.Get(int)"
                  IL_0010:  call       "System.Threading.Tasks.Task<int> Test.F(int)"
                  IL_0015:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001a:  stloc.2
                  IL_001b:  ldloca.s   V_1
                  IL_001d:  ldloc.0
                  IL_001e:  ldloc.2
                  IL_001f:  callvirt   "int Test.C1.M(int)"
                  IL_0024:  call       "int?..ctor(int)"
                  IL_0029:  br.s       IL_0033
                  IL_002b:  ldloca.s   V_1
                  IL_002d:  initobj    "int?"
                  IL_0033:  ldloc.1
                  IL_0034:  ret
                }
                """);
        }

        [Fact]
        public void AssignToAwait()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = 42;
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetS]: Unexpected type on the stack. { Offset = 0x2e, Found = ref 'S', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<S>' }
                    [Run]: Return value missing on the stack. { Offset = 0x20 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.Run()", """
                {
                  // Code size       33 (0x21)
                  .maxstack  2
                  IL_0000:  call       "System.Threading.Tasks.Task<S> Test.GetS()"
                  IL_0005:  call       "S System.Runtime.CompilerServices.AsyncHelpers.Await<S>(System.Threading.Tasks.Task<S>)"
                  IL_000a:  ldc.i4.s   42
                  IL_000c:  stfld      "int S.x"
                  IL_0011:  ldsfld     "S Test._s"
                  IL_0016:  ldfld      "int S.x"
                  IL_001b:  call       "void System.Console.WriteLine(int)"
                  IL_0020:  ret
                }
                """);
        }

        [Fact]
        public void AssignAwaitToAwait()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class S
{
    public int x = -1;
}

class Test
{
    static S _s = new S();

    public static async Task<S> GetS()
    {
        return await Task.Factory.StartNew(() => _s);
    }

    public static async Task Run()
    {
        (await GetS()).x = await Task.Factory.StartNew(() => 42);
        Console.WriteLine(_s.x);
    }
}

class Driver
{
    static void Main()
    {
        Test.Run().Wait();
    }
}";
            var expectedOutput = @"
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetS]: Unexpected type on the stack. { Offset = 0x2e, Found = ref 'S', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<S>' }
                    [Run]: Return value missing on the stack. { Offset = 0x4e }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Test.Run()", """
                {
                  // Code size       79 (0x4f)
                  .maxstack  4
                  .locals init (int V_0)
                  IL_0000:  call       "System.Threading.Tasks.Task<S> Test.GetS()"
                  IL_0005:  call       "S System.Runtime.CompilerServices.AsyncHelpers.Await<S>(System.Threading.Tasks.Task<S>)"
                  IL_000a:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_000f:  ldsfld     "System.Func<int> Test.<>c.<>9__2_0"
                  IL_0014:  dup
                  IL_0015:  brtrue.s   IL_002e
                  IL_0017:  pop
                  IL_0018:  ldsfld     "Test.<>c Test.<>c.<>9"
                  IL_001d:  ldftn      "int Test.<>c.<Run>b__2_0()"
                  IL_0023:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0028:  dup
                  IL_0029:  stsfld     "System.Func<int> Test.<>c.<>9__2_0"
                  IL_002e:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0033:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0038:  stloc.0
                  IL_0039:  ldloc.0
                  IL_003a:  stfld      "int S.x"
                  IL_003f:  ldsfld     "S Test._s"
                  IL_0044:  ldfld      "int S.x"
                  IL_0049:  call       "void System.Console.WriteLine(int)"
                  IL_004e:  ret
                }
                """);
        }

        [Fact]
        public void SpillArglist()
        {
            var source = @"
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    static StringBuilder sb = new StringBuilder();
    public async Task Run()
    {
        try
        {
            Bar(__arglist(One(), await Two()));
            if (sb.ToString() == ""OneTwo"")
                Driver.Result = 0;
        }
        finally
        {
            Driver.CompleteSignal.Set();
        }
    }
    int One()
    {
        sb.Append(""One"");
        return 1;
    }
    async Task<int> Two()
    {
        await Task.Delay(1);
        sb.Append(""Two"");
        return 2;
    }
    void Bar(__arglist)
    {
        var ai = new ArgIterator(__arglist);
        while (ai.GetRemainingCount() > 0)
            Console.WriteLine( __refvalue(ai.GetNextArg(), int));
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
        tc.Run();
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expectedOutput = ExecutionConditionUtil.IsDesktop ? @"
1
2
0
" : null;
            CompileAndVerify(source, targetFramework: TargetFramework.NetFramework, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);

            var comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyDiagnostics(
                // (14,17): error CS9328: Method 'TestCase.Run()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //             Bar(__arglist(One(), await Two()));
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "__arglist(One(), await Two())").WithArguments("TestCase.Run()").WithLocation(14, 17),
                // (48,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         tc.Run();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "tc.Run()").WithLocation(48, 9)
            );
        }

        [Fact]
        public void SpillObjectInitializer1()
        {
            var source = @"
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;


struct TestCase : IEnumerable
{
    int X;
    public async Task Run()
    {
        int test = 0;
        int count = 0;
        try
        {
            test++;
            var x = new TestCase { X = await Bar() };
            if (x.X == 1)
                count++;
        }
        finally
        {
            Driver.Result = test - count;
            Driver.CompleteSignal.Set();
        }
    }
    async Task<int> Bar()
    {
        await Task.Delay(1);
        return 1;
    }

    public IEnumerator GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}
class Driver
{
    static public AutoResetEvent CompleteSignal = new AutoResetEvent(false);
    public static int Result = -1;
    public static void Main()
    {
        TestCase tc = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        tc.Run();
#pragma warning restore CS4014
        CompleteSignal.WaitOne();

        Console.WriteLine(Result);
    }
}";
            var expectedOutput = @"
0
";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Return value missing on the stack. { Offset = 0x50 }
                    [Bar]: Unexpected type on the stack. { Offset = 0xc, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size       81 (0x51)
                  .maxstack  2
                  .locals init (TestCase V_0,
                                int V_1, //test
                                int V_2, //count
                                int V_3,
                                TestCase V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "TestCase"
                  IL_0006:  stloc.0
                  IL_0007:  ldc.i4.0
                  IL_0008:  stloc.1
                  IL_0009:  ldc.i4.0
                  IL_000a:  stloc.2
                  .try
                  {
                    IL_000b:  ldloc.1
                    IL_000c:  ldc.i4.1
                    IL_000d:  add
                    IL_000e:  stloc.1
                    IL_000f:  ldloca.s   V_4
                    IL_0011:  initobj    "TestCase"
                    IL_0017:  ldloca.s   V_0
                    IL_0019:  call       "System.Threading.Tasks.Task<int> TestCase.Bar()"
                    IL_001e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0023:  stloc.3
                    IL_0024:  ldloca.s   V_4
                    IL_0026:  ldloc.3
                    IL_0027:  stfld      "int TestCase.X"
                    IL_002c:  ldloc.s    V_4
                    IL_002e:  ldfld      "int TestCase.X"
                    IL_0033:  ldc.i4.1
                    IL_0034:  bne.un.s   IL_003a
                    IL_0036:  ldloc.2
                    IL_0037:  ldc.i4.1
                    IL_0038:  add
                    IL_0039:  stloc.2
                    IL_003a:  leave.s    IL_0050
                  }
                  finally
                  {
                    IL_003c:  ldloc.1
                    IL_003d:  ldloc.2
                    IL_003e:  sub
                    IL_003f:  stsfld     "int Driver.Result"
                    IL_0044:  ldsfld     "System.Threading.AutoResetEvent Driver.CompleteSignal"
                    IL_0049:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_004e:  pop
                    IL_004f:  endfinally
                  }
                  IL_0050:  ret
                }
                """);
        }

        [Fact]
        public void SpillWithByRefArguments01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class BaseTestCase
{
    public void GooRef(ref decimal d, int x, out decimal od)
    {
        od = d;
        d++;
    }

    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }
}

class TestCase : BaseTestCase
{
    public async void Run()
    {
        int tests = 0;
        try
        {
            decimal d = 1;
            decimal od;

            tests++;
            base.GooRef(ref d, await base.GetVal(4), out od);
            if (d == 2 && od == 1) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillWithByRefArguments01WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class BaseTestCase
{
    public void GooRef(ref decimal d, int x, out decimal od)
    {
        od = d;
        d++;
    }

    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }
}

class TestCase : BaseTestCase
{
    public async Task Run()
    {
        int tests = 0;
        try
        {
            decimal d = 1;
            decimal od;

            tests++;
            base.GooRef(ref d, await base.GetVal(4), out od);
            if (d == 2 && od == 1) Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";

            var expectedOutput = "0";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x65 }
                    """
            });

            verifier.VerifyDiagnostics(
                // (52,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         t.Run();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "t.Run()").WithLocation(52, 9)
            );
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      102 (0x66)
                  .maxstack  4
                  .locals init (int V_0, //tests
                                decimal V_1, //d
                                decimal V_2, //od
                                int V_3)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldsfld     "decimal decimal.One"
                    IL_0007:  stloc.1
                    IL_0008:  ldloc.0
                    IL_0009:  ldc.i4.1
                    IL_000a:  add
                    IL_000b:  stloc.0
                    IL_000c:  ldarg.0
                    IL_000d:  ldc.i4.4
                    IL_000e:  call       "System.Threading.Tasks.Task<int> BaseTestCase.GetVal<int>(int)"
                    IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0018:  stloc.3
                    IL_0019:  ldarg.0
                    IL_001a:  ldloca.s   V_1
                    IL_001c:  ldloc.3
                    IL_001d:  ldloca.s   V_2
                    IL_001f:  call       "void BaseTestCase.GooRef(ref decimal, int, out decimal)"
                    IL_0024:  ldloc.1
                    IL_0025:  ldc.i4.2
                    IL_0026:  newobj     "decimal..ctor(int)"
                    IL_002b:  call       "bool decimal.op_Equality(decimal, decimal)"
                    IL_0030:  brfalse.s  IL_004b
                    IL_0032:  ldloc.2
                    IL_0033:  ldsfld     "decimal decimal.One"
                    IL_0038:  call       "bool decimal.op_Equality(decimal, decimal)"
                    IL_003d:  brfalse.s  IL_004b
                    IL_003f:  ldsfld     "int Driver.Count"
                    IL_0044:  ldc.i4.1
                    IL_0045:  add
                    IL_0046:  stsfld     "int Driver.Count"
                    IL_004b:  leave.s    IL_0065
                  }
                  finally
                  {
                    IL_004d:  ldsfld     "int Driver.Count"
                    IL_0052:  ldloc.0
                    IL_0053:  sub
                    IL_0054:  stsfld     "int Driver.Result"
                    IL_0059:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_005e:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0063:  pop
                    IL_0064:  endfinally
                  }
                  IL_0065:  ret
                }
                """);
        }

        [Fact]
        public void SpillOperator_Compound1()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillOperator_Compound1WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x73 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      116 (0x74)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1, //tests
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.1
                  .try
                  {
                    IL_0002:  ldloc.1
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.1
                    IL_0006:  ldc.i4.4
                    IL_0007:  newarr     "int"
                    IL_000c:  dup
                    IL_000d:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
                    IL_0012:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                    IL_0017:  dup
                    IL_0018:  ldarg.0
                    IL_0019:  ldc.i4.0
                    IL_001a:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_001f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0024:  stloc.2
                    IL_0025:  ldloc.2
                    IL_0026:  stloc.0
                    IL_0027:  dup
                    IL_0028:  ldloc.0
                    IL_0029:  ldelem.i4
                    IL_002a:  pop
                    IL_002b:  dup
                    IL_002c:  ldloc.0
                    IL_002d:  ldelem.i4
                    IL_002e:  stloc.3
                    IL_002f:  ldarg.0
                    IL_0030:  ldc.i4.4
                    IL_0031:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0036:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003b:  stloc.s    V_4
                    IL_003d:  ldloc.0
                    IL_003e:  ldelema    "int"
                    IL_0043:  ldloc.3
                    IL_0044:  ldloc.s    V_4
                    IL_0046:  add
                    IL_0047:  stind.i4
                    IL_0048:  ldc.i4.0
                    IL_0049:  ldelem.i4
                    IL_004a:  ldc.i4.5
                    IL_004b:  bne.un.s   IL_0059
                    IL_004d:  ldsfld     "int Driver.Count"
                    IL_0052:  ldc.i4.1
                    IL_0053:  add
                    IL_0054:  stsfld     "int Driver.Count"
                    IL_0059:  leave.s    IL_0073
                  }
                  finally
                  {
                    IL_005b:  ldsfld     "int Driver.Count"
                    IL_0060:  ldloc.1
                    IL_0061:  sub
                    IL_0062:  stsfld     "int Driver.Result"
                    IL_0067:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_006c:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0071:  pop
                    IL_0072:  endfinally
                  }
                  IL_0073:  ret
                }
                """);
        }

        [Fact]
        public void SpillOperator_Compound2()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillOperator_Compound2WithRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;
        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x73 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      116 (0x74)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1, //tests
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.1
                  .try
                  {
                    IL_0002:  ldloc.1
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.1
                    IL_0006:  ldc.i4.4
                    IL_0007:  newarr     "int"
                    IL_000c:  dup
                    IL_000d:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
                    IL_0012:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                    IL_0017:  dup
                    IL_0018:  ldarg.0
                    IL_0019:  ldc.i4.0
                    IL_001a:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_001f:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0024:  stloc.2
                    IL_0025:  ldloc.2
                    IL_0026:  stloc.0
                    IL_0027:  dup
                    IL_0028:  ldloc.0
                    IL_0029:  ldelem.i4
                    IL_002a:  pop
                    IL_002b:  dup
                    IL_002c:  ldloc.0
                    IL_002d:  ldelem.i4
                    IL_002e:  stloc.3
                    IL_002f:  ldarg.0
                    IL_0030:  ldc.i4.4
                    IL_0031:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0036:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_003b:  stloc.s    V_4
                    IL_003d:  ldloc.0
                    IL_003e:  ldelema    "int"
                    IL_0043:  ldloc.3
                    IL_0044:  ldloc.s    V_4
                    IL_0046:  add
                    IL_0047:  stind.i4
                    IL_0048:  ldc.i4.0
                    IL_0049:  ldelem.i4
                    IL_004a:  ldc.i4.5
                    IL_004b:  bne.un.s   IL_0059
                    IL_004d:  ldsfld     "int Driver.Count"
                    IL_0052:  ldc.i4.1
                    IL_0053:  add
                    IL_0054:  stsfld     "int Driver.Count"
                    IL_0059:  leave.s    IL_0073
                  }
                  finally
                  {
                    IL_005b:  ldsfld     "int Driver.Count"
                    IL_0060:  ldloc.1
                    IL_0061:  sub
                    IL_0062:  stsfld     "int Driver.Result"
                    IL_0067:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_006c:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_0071:  pop
                    IL_0072:  endfinally
                  }
                  IL_0073:  ret
                }
                """);
        }

        [Fact]
        public void Async_StackSpill_Argument_Generic04()
        {
            var source = @"
using System;
using System.Threading.Tasks;
public class MC<T>
{
    async public System.Threading.Tasks.Task<dynamic> Goo<V>(T t, V u) { await Task.Delay(1); return u; }
}

class Test
{
    static async Task<int> Goo()
    {
        dynamic mc = new MC<string>();
        var rez = await mc.Goo<string>(null, await ((Func<Task<string>>)(async () => { await Task.Delay(1); return ""Test""; }))());
        if (rez == ""Test"")
            return 0;
        return 1;
    }

    static void Main()
    {
        Console.WriteLine(Goo().Result);
    }
}";
            CompileAndVerify(source, "0", references: new[] { CSharpRef });

            var comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,19): error CS9328: Method 'Test.Goo()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //         var rez = await mc.Goo<string>(null, await ((Func<Task<string>>)(async () => { await Task.Delay(1); return "Test"; }))());
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, @"await mc.Goo<string>(null, await ((Func<Task<string>>)(async () => { await Task.Delay(1); return ""Test""; }))())").WithArguments("Test.Goo()").WithLocation(14, 19)
            );
        }

        [Fact]
        public void AsyncStackSpill_assign01()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct TestCase
{
    private int val;
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            val = x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5 && val == await GetVal(5))
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void AsyncStackSpill_assign01WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

struct TestCase
{
    private int val;
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    public async Task Run()
    {
        int tests = 0;

        try
        {
            tests++;
            int[] x = new int[] { 1, 2, 3, 4 };
            val = x[await GetVal(0)] += await GetVal(4);
            if (x[0] == 5 && val == await GetVal(5))
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0xb7 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      184 (0xb8)
                  .maxstack  5
                  .locals init (TestCase V_0,
                                int[] V_1,
                                int V_2,
                                int V_3, //tests
                                int[] V_4,
                                int& V_5,
                                int V_6,
                                int V_7,
                                int V_8,
                                bool V_9)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "TestCase"
                  IL_0006:  stloc.0
                  IL_0007:  ldc.i4.0
                  IL_0008:  stloc.3
                  .try
                  {
                    IL_0009:  ldloc.3
                    IL_000a:  ldc.i4.1
                    IL_000b:  add
                    IL_000c:  stloc.3
                    IL_000d:  ldc.i4.4
                    IL_000e:  newarr     "int"
                    IL_0013:  dup
                    IL_0014:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
                    IL_0019:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                    IL_001e:  dup
                    IL_001f:  stloc.s    V_4
                    IL_0021:  ldloca.s   V_0
                    IL_0023:  ldc.i4.0
                    IL_0024:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0029:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_002e:  ldloc.s    V_4
                    IL_0030:  stloc.1
                    IL_0031:  stloc.2
                    IL_0032:  ldloc.1
                    IL_0033:  ldloc.2
                    IL_0034:  ldelem.i4
                    IL_0035:  pop
                    IL_0036:  ldloc.1
                    IL_0037:  ldloc.2
                    IL_0038:  ldelem.i4
                    IL_0039:  stloc.s    V_6
                    IL_003b:  ldloca.s   V_0
                    IL_003d:  ldc.i4.4
                    IL_003e:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0043:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0048:  stloc.s    V_7
                    IL_004a:  ldloca.s   V_0
                    IL_004c:  ldloc.1
                    IL_004d:  ldloc.2
                    IL_004e:  ldelema    "int"
                    IL_0053:  stloc.s    V_5
                    IL_0055:  ldloc.s    V_5
                    IL_0057:  ldloc.s    V_6
                    IL_0059:  ldloc.s    V_7
                    IL_005b:  add
                    IL_005c:  dup
                    IL_005d:  stloc.s    V_8
                    IL_005f:  stind.i4
                    IL_0060:  ldloc.s    V_8
                    IL_0062:  stfld      "int TestCase.val"
                    IL_0067:  ldc.i4.0
                    IL_0068:  ldelem.i4
                    IL_0069:  ldc.i4.5
                    IL_006a:  ceq
                    IL_006c:  stloc.s    V_9
                    IL_006e:  ldloc.s    V_9
                    IL_0070:  brfalse.s  IL_008d
                    IL_0072:  ldloc.0
                    IL_0073:  ldfld      "int TestCase.val"
                    IL_0078:  ldloca.s   V_0
                    IL_007a:  ldc.i4.5
                    IL_007b:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0080:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0085:  stloc.s    V_7
                    IL_0087:  ldloc.s    V_7
                    IL_0089:  ceq
                    IL_008b:  stloc.s    V_9
                    IL_008d:  ldloc.s    V_9
                    IL_008f:  brfalse.s  IL_009d
                    IL_0091:  ldsfld     "int Driver.Count"
                    IL_0096:  ldc.i4.1
                    IL_0097:  add
                    IL_0098:  stsfld     "int Driver.Count"
                    IL_009d:  leave.s    IL_00b7
                  }
                  finally
                  {
                    IL_009f:  ldsfld     "int Driver.Count"
                    IL_00a4:  ldloc.3
                    IL_00a5:  sub
                    IL_00a6:  stsfld     "int Driver.Result"
                    IL_00ab:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_00b0:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_00b5:  pop
                    IL_00b6:  endfinally
                  }
                  IL_00b7:  ret
                }
                """);
        }

        [Fact]
        public void SpillCollectionInitializer()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct PrivateCollection : IEnumerable
{
    public List<int> lst; //public so we can check the values
    public void Add(int x)
    {
        if (lst == null)
            lst = new List<int>();
        lst.Add(x);
    }

    public IEnumerator GetEnumerator()
    {
        return lst as IEnumerator;
    }
}

class TestCase
{
    public async Task<T> GetValue<T>(T x)
    {
        await Task.Delay(1);
        return x;
    }

    public async void Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myCol = new PrivateCollection() { 
                await GetValue(1),
                await GetValue(2)
            };
            if (myCol.lst[0] == 1 && myCol.lst[1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test completes, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public int Goo { get; set; }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillCollectionInitializerWithRuntimeAsync()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

struct PrivateCollection : IEnumerable
{
    public List<int> lst; //public so we can check the values
    public void Add(int x)
    {
        if (lst == null)
            lst = new List<int>();
        lst.Add(x);
    }

    public IEnumerator GetEnumerator()
    {
        return lst as IEnumerator;
    }
}

class TestCase
{
    public async Task<T> GetValue<T>(T x)
    {
        await Task.Delay(1);
        return x;
    }

    public async Task Run()
    {
        int tests = 0;

        try
        {
            tests++;
            var myCol = new PrivateCollection() { 
                await GetValue(1),
                await GetValue(2)
            };
            if (myCol.lst[0] == 1 && myCol.lst[1] == 2)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test completes, set the flag.
            Driver.CompletedSignal.Set();
        }
    }

    public int Goo { get; set; }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetValue]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x7f }
                    """
            });

            verifier.VerifyDiagnostics(
                // (65,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         t.Run();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "t.Run()").WithLocation(65, 9)
            );
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      128 (0x80)
                  .maxstack  2
                  .locals init (int V_0, //tests
                                PrivateCollection V_1, //myCol
                                int V_2,
                                int V_3,
                                PrivateCollection V_4)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.0
                  .try
                  {
                    IL_0002:  ldloc.0
                    IL_0003:  ldc.i4.1
                    IL_0004:  add
                    IL_0005:  stloc.0
                    IL_0006:  ldloca.s   V_4
                    IL_0008:  initobj    "PrivateCollection"
                    IL_000e:  ldarg.0
                    IL_000f:  ldc.i4.1
                    IL_0010:  call       "System.Threading.Tasks.Task<int> TestCase.GetValue<int>(int)"
                    IL_0015:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_001a:  stloc.2
                    IL_001b:  ldloca.s   V_4
                    IL_001d:  ldloc.2
                    IL_001e:  call       "void PrivateCollection.Add(int)"
                    IL_0023:  ldarg.0
                    IL_0024:  ldc.i4.2
                    IL_0025:  call       "System.Threading.Tasks.Task<int> TestCase.GetValue<int>(int)"
                    IL_002a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_002f:  stloc.3
                    IL_0030:  ldloca.s   V_4
                    IL_0032:  ldloc.3
                    IL_0033:  call       "void PrivateCollection.Add(int)"
                    IL_0038:  ldloc.s    V_4
                    IL_003a:  stloc.1
                    IL_003b:  ldloc.1
                    IL_003c:  ldfld      "System.Collections.Generic.List<int> PrivateCollection.lst"
                    IL_0041:  ldc.i4.0
                    IL_0042:  callvirt   "int System.Collections.Generic.List<int>.this[int].get"
                    IL_0047:  ldc.i4.1
                    IL_0048:  bne.un.s   IL_0065
                    IL_004a:  ldloc.1
                    IL_004b:  ldfld      "System.Collections.Generic.List<int> PrivateCollection.lst"
                    IL_0050:  ldc.i4.1
                    IL_0051:  callvirt   "int System.Collections.Generic.List<int>.this[int].get"
                    IL_0056:  ldc.i4.2
                    IL_0057:  bne.un.s   IL_0065
                    IL_0059:  ldsfld     "int Driver.Count"
                    IL_005e:  ldc.i4.1
                    IL_005f:  add
                    IL_0060:  stsfld     "int Driver.Count"
                    IL_0065:  leave.s    IL_007f
                  }
                  finally
                  {
                    IL_0067:  ldsfld     "int Driver.Count"
                    IL_006c:  ldloc.0
                    IL_006d:  sub
                    IL_006e:  stsfld     "int Driver.Result"
                    IL_0073:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_0078:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_007d:  pop
                    IL_007e:  endfinally
                  }
                  IL_007f:  ret
                }
                """);
        }

        [Fact]
        public void SpillRefExpr()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyClass
{
    public int Field;
}

class TestCase
{
    public static int Goo(ref int x, int y)
    {
        return x + y;
    }

    public async Task<int> Run()
    {
        return Goo(
            ref (new MyClass() { Field = 21 }.Field),
            await Task.Factory.StartNew(() => 21));
    }
}

static class Driver
{
    static void Main()
    {
        var t = new TestCase().Run();
        t.Wait();
        Console.WriteLine(t.Result);
    }
}";

            var expectedOutput = "42";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Run]: Unexpected type on the stack. { Offset = 0x47, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size       72 (0x48)
                  .maxstack  4
                  .locals init (int V_0)
                  IL_0000:  newobj     "MyClass..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.s   21
                  IL_0008:  stfld      "int MyClass.Field"
                  IL_000d:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_0012:  ldsfld     "System.Func<int> TestCase.<>c.<>9__1_0"
                  IL_0017:  dup
                  IL_0018:  brtrue.s   IL_0031
                  IL_001a:  pop
                  IL_001b:  ldsfld     "TestCase.<>c TestCase.<>c.<>9"
                  IL_0020:  ldftn      "int TestCase.<>c.<Run>b__1_0()"
                  IL_0026:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_002b:  dup
                  IL_002c:  stsfld     "System.Func<int> TestCase.<>c.<>9__1_0"
                  IL_0031:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0036:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_003b:  stloc.0
                  IL_003c:  ldflda     "int MyClass.Field"
                  IL_0041:  ldloc.0
                  IL_0042:  call       "int TestCase.Goo(ref int, int)"
                  IL_0047:  ret
                }
                """);
        }

        [Fact]
        public void SpillManagedPointerAssign03()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    class PrivClass
    {
        internal struct ValueT
        {
            public int Field;
        }

        internal ValueT[] arr = new ValueT[3];
    }

    private PrivClass myClass;

    public async void Run()
    {
        int tests = 0;
        this.myClass = new PrivClass();

        try
        {
            tests++;
            this.myClass.arr[0].Field = await GetVal(4);
            if (myClass.arr[0].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[0].Field += await GetVal(4);
            if (myClass.arr[0].Field == 8)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field += await GetVal(4);
            if (myClass.arr[1].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field++;
            if (myClass.arr[1].Field == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
        t.Run();

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");
        }

        [Fact]
        public void SpillManagedPointerAssign03WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class TestCase
{
    public async Task<T> GetVal<T>(T t)
    {
        await Task.Delay(1);
        return t;
    }

    class PrivClass
    {
        internal struct ValueT
        {
            public int Field;
        }

        internal ValueT[] arr = new ValueT[3];
    }

    private PrivClass myClass;

    public async Task Run()
    {
        int tests = 0;
        this.myClass = new PrivClass();

        try
        {
            tests++;
            this.myClass.arr[0].Field = await GetVal(4);
            if (myClass.arr[0].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[0].Field += await GetVal(4);
            if (myClass.arr[0].Field == 8)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field += await GetVal(4);
            if (myClass.arr[1].Field == 4)
                Driver.Count++;

            tests++;
            this.myClass.arr[await GetVal(1)].Field++;
            if (myClass.arr[1].Field == 5)
                Driver.Count++;
        }
        finally
        {
            Driver.Result = Driver.Count - tests;
            //When test complete, set the flag.
            Driver.CompletedSignal.Set();
        }
    }
}

class Driver
{
    public static int Result = -1;
    public static int Count = 0;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static void Main()
    {
        var t = new TestCase();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        t.Run();
#pragma warning restore CS4014

        CompletedSignal.WaitOne();
        // 0 - success
        // 1 - failed (test completed)
        // -1 - failed (test incomplete - deadlock, etc)
        Console.WriteLine(Driver.Result);
    }
}";
            CompileAndVerify(source, "0");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput("0"), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [GetVal]: Unexpected type on the stack. { Offset = 0xc, Found = value 'T', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<T0>' }
                    [Run]: Return value missing on the stack. { Offset = 0x1c2 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("TestCase.Run()", """
                {
                  // Code size      451 (0x1c3)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1,
                                int V_2, //tests
                                int V_3,
                                int V_4,
                                int V_5)
                  IL_0000:  ldc.i4.0
                  IL_0001:  stloc.2
                  IL_0002:  ldarg.0
                  IL_0003:  newobj     "TestCase.PrivClass..ctor()"
                  IL_0008:  stfld      "TestCase.PrivClass TestCase.myClass"
                  .try
                  {
                    IL_000d:  ldloc.2
                    IL_000e:  ldc.i4.1
                    IL_000f:  add
                    IL_0010:  stloc.2
                    IL_0011:  ldarg.0
                    IL_0012:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_0017:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_001c:  dup
                    IL_001d:  ldc.i4.0
                    IL_001e:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0023:  pop
                    IL_0024:  ldarg.0
                    IL_0025:  ldc.i4.4
                    IL_0026:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_002b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_0030:  stloc.3
                    IL_0031:  ldc.i4.0
                    IL_0032:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0037:  ldloc.3
                    IL_0038:  stfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_003d:  ldarg.0
                    IL_003e:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_0043:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_0048:  ldc.i4.0
                    IL_0049:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_004e:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_0053:  ldc.i4.4
                    IL_0054:  bne.un.s   IL_0062
                    IL_0056:  ldsfld     "int Driver.Count"
                    IL_005b:  ldc.i4.1
                    IL_005c:  add
                    IL_005d:  stsfld     "int Driver.Count"
                    IL_0062:  ldloc.2
                    IL_0063:  ldc.i4.1
                    IL_0064:  add
                    IL_0065:  stloc.2
                    IL_0066:  ldarg.0
                    IL_0067:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_006c:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_0071:  dup
                    IL_0072:  ldc.i4.0
                    IL_0073:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0078:  pop
                    IL_0079:  dup
                    IL_007a:  ldc.i4.0
                    IL_007b:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0080:  pop
                    IL_0081:  dup
                    IL_0082:  ldc.i4.0
                    IL_0083:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0088:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_008d:  stloc.3
                    IL_008e:  ldarg.0
                    IL_008f:  ldc.i4.4
                    IL_0090:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0095:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_009a:  stloc.s    V_4
                    IL_009c:  ldc.i4.0
                    IL_009d:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_00a2:  ldflda     "int TestCase.PrivClass.ValueT.Field"
                    IL_00a7:  ldloc.3
                    IL_00a8:  ldloc.s    V_4
                    IL_00aa:  add
                    IL_00ab:  stind.i4
                    IL_00ac:  ldarg.0
                    IL_00ad:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_00b2:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_00b7:  ldc.i4.0
                    IL_00b8:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_00bd:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_00c2:  ldc.i4.8
                    IL_00c3:  bne.un.s   IL_00d1
                    IL_00c5:  ldsfld     "int Driver.Count"
                    IL_00ca:  ldc.i4.1
                    IL_00cb:  add
                    IL_00cc:  stsfld     "int Driver.Count"
                    IL_00d1:  ldloc.2
                    IL_00d2:  ldc.i4.1
                    IL_00d3:  add
                    IL_00d4:  stloc.2
                    IL_00d5:  ldarg.0
                    IL_00d6:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_00db:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_00e0:  ldarg.0
                    IL_00e1:  ldc.i4.1
                    IL_00e2:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_00e7:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_00ec:  stloc.s    V_4
                    IL_00ee:  ldloc.s    V_4
                    IL_00f0:  stloc.0
                    IL_00f1:  dup
                    IL_00f2:  ldloc.0
                    IL_00f3:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_00f8:  pop
                    IL_00f9:  ldloc.0
                    IL_00fa:  stloc.1
                    IL_00fb:  dup
                    IL_00fc:  ldloc.1
                    IL_00fd:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0102:  pop
                    IL_0103:  dup
                    IL_0104:  ldloc.1
                    IL_0105:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_010a:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_010f:  stloc.3
                    IL_0110:  ldarg.0
                    IL_0111:  ldc.i4.4
                    IL_0112:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0117:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_011c:  stloc.s    V_5
                    IL_011e:  ldloc.1
                    IL_011f:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0124:  ldflda     "int TestCase.PrivClass.ValueT.Field"
                    IL_0129:  ldloc.3
                    IL_012a:  ldloc.s    V_5
                    IL_012c:  add
                    IL_012d:  stind.i4
                    IL_012e:  ldarg.0
                    IL_012f:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_0134:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_0139:  ldc.i4.1
                    IL_013a:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_013f:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_0144:  ldc.i4.4
                    IL_0145:  bne.un.s   IL_0153
                    IL_0147:  ldsfld     "int Driver.Count"
                    IL_014c:  ldc.i4.1
                    IL_014d:  add
                    IL_014e:  stsfld     "int Driver.Count"
                    IL_0153:  ldloc.2
                    IL_0154:  ldc.i4.1
                    IL_0155:  add
                    IL_0156:  stloc.2
                    IL_0157:  ldarg.0
                    IL_0158:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_015d:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_0162:  ldarg.0
                    IL_0163:  ldc.i4.1
                    IL_0164:  call       "System.Threading.Tasks.Task<int> TestCase.GetVal<int>(int)"
                    IL_0169:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                    IL_016e:  stloc.s    V_5
                    IL_0170:  ldloc.s    V_5
                    IL_0172:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0177:  ldflda     "int TestCase.PrivClass.ValueT.Field"
                    IL_017c:  dup
                    IL_017d:  ldind.i4
                    IL_017e:  stloc.3
                    IL_017f:  ldloc.3
                    IL_0180:  ldc.i4.1
                    IL_0181:  add
                    IL_0182:  stind.i4
                    IL_0183:  ldarg.0
                    IL_0184:  ldfld      "TestCase.PrivClass TestCase.myClass"
                    IL_0189:  ldfld      "TestCase.PrivClass.ValueT[] TestCase.PrivClass.arr"
                    IL_018e:  ldc.i4.1
                    IL_018f:  ldelema    "TestCase.PrivClass.ValueT"
                    IL_0194:  ldfld      "int TestCase.PrivClass.ValueT.Field"
                    IL_0199:  ldc.i4.5
                    IL_019a:  bne.un.s   IL_01a8
                    IL_019c:  ldsfld     "int Driver.Count"
                    IL_01a1:  ldc.i4.1
                    IL_01a2:  add
                    IL_01a3:  stsfld     "int Driver.Count"
                    IL_01a8:  leave.s    IL_01c2
                  }
                  finally
                  {
                    IL_01aa:  ldsfld     "int Driver.Count"
                    IL_01af:  ldloc.2
                    IL_01b0:  sub
                    IL_01b1:  stsfld     "int Driver.Result"
                    IL_01b6:  ldsfld     "System.Threading.AutoResetEvent Driver.CompletedSignal"
                    IL_01bb:  callvirt   "bool System.Threading.EventWaitHandle.Set()"
                    IL_01c0:  pop
                    IL_01c1:  endfinally
                  }
                  IL_01c2:  ret
                }
                """);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;
struct S
{
    int? i;

    static async Task Main()
    {
        S s = default;
        Console.WriteLine(s.i += await GetInt());
    }

    static Task<int?> GetInt() => Task.FromResult((int?)1);
}";
            var expectedOutput = "";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x63 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.Main()", """
                {
                  // Code size      100 (0x64)
                  .maxstack  3
                  .locals init (S V_0, //s
                                int? V_1,
                                int? V_2,
                                int? V_3)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S"
                  IL_0008:  ldloc.0
                  IL_0009:  ldfld      "int? S.i"
                  IL_000e:  stloc.1
                  IL_000f:  call       "System.Threading.Tasks.Task<int?> S.GetInt()"
                  IL_0014:  call       "int? System.Runtime.CompilerServices.AsyncHelpers.Await<int?>(System.Threading.Tasks.Task<int?>)"
                  IL_0019:  stloc.2
                  IL_001a:  ldloca.s   V_0
                  IL_001c:  ldflda     "int? S.i"
                  IL_0021:  ldloca.s   V_1
                  IL_0023:  call       "readonly bool int?.HasValue.get"
                  IL_0028:  ldloca.s   V_2
                  IL_002a:  call       "readonly bool int?.HasValue.get"
                  IL_002f:  and
                  IL_0030:  brtrue.s   IL_003d
                  IL_0032:  ldloca.s   V_3
                  IL_0034:  initobj    "int?"
                  IL_003a:  ldloc.3
                  IL_003b:  br.s       IL_0051
                  IL_003d:  ldloca.s   V_1
                  IL_003f:  call       "readonly int int?.GetValueOrDefault()"
                  IL_0044:  ldloca.s   V_2
                  IL_0046:  call       "readonly int int?.GetValueOrDefault()"
                  IL_004b:  add
                  IL_004c:  newobj     "int?..ctor(int)"
                  IL_0051:  dup
                  IL_0052:  stloc.3
                  IL_0053:  stobj      "int?"
                  IL_0058:  ldloc.3
                  IL_0059:  box        "int?"
                  IL_005e:  call       "void System.Console.WriteLine(object)"
                  IL_0063:  ret
                }
                """);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_02()
        {
            var source = @"
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await new C().M();
    }

    int field = 1;
    async System.Threading.Tasks.Task M()
    {
         this.field += await M2();
         System.Console.Write(this.field);
    }

    async System.Threading.Tasks.Task<int> M2()
    {
         await System.Threading.Tasks.Task.Yield();
         return 42;
    }
}
";
            var expectedOutput = "43";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xf }
                    [M]: Return value missing on the stack. { Offset = 0x27 }
                    [M2]: Unexpected type on the stack. { Offset = 0x26, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M()", """
                {
                  // Code size       40 (0x28)
                  .maxstack  3
                  .locals init (int V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int C.field"
                  IL_0006:  stloc.0
                  IL_0007:  ldarg.0
                  IL_0008:  call       "System.Threading.Tasks.Task<int> C.M2()"
                  IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0012:  stloc.1
                  IL_0013:  ldarg.0
                  IL_0014:  ldloc.0
                  IL_0015:  ldloc.1
                  IL_0016:  add
                  IL_0017:  stfld      "int C.field"
                  IL_001c:  ldarg.0
                  IL_001d:  ldfld      "int C.field"
                  IL_0022:  call       "void System.Console.Write(int)"
                  IL_0027:  ret
                }
                """);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_03()
        {
            var source = @"
class C
{
    static async System.Threading.Tasks.Task Main()
    {
        await new C().M();
    }

    int? field = 1;
    async System.Threading.Tasks.Task M()
    {
         this.field += await M2();
         System.Console.Write(this.field);
    }

    async System.Threading.Tasks.Task<int?> M2()
    {
         await System.Threading.Tasks.Task.Yield();
         return 42;
    }
}
";
            var expectedOutput = "43";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xf }
                    [M]: Return value missing on the stack. { Offset = 0x59 }
                    [M2]: Unexpected type on the stack. { Offset = 0x2b, Found = value '[System.Runtime]System.Nullable`1<int32>', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<System.Nullable`1<int32>>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M()", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  3
                  .locals init (int? V_0,
                                int? V_1,
                                int? V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int? C.field"
                  IL_0006:  stloc.0
                  IL_0007:  ldarg.0
                  IL_0008:  call       "System.Threading.Tasks.Task<int?> C.M2()"
                  IL_000d:  call       "int? System.Runtime.CompilerServices.AsyncHelpers.Await<int?>(System.Threading.Tasks.Task<int?>)"
                  IL_0012:  stloc.1
                  IL_0013:  ldarg.0
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  call       "readonly bool int?.HasValue.get"
                  IL_001b:  ldloca.s   V_1
                  IL_001d:  call       "readonly bool int?.HasValue.get"
                  IL_0022:  and
                  IL_0023:  brtrue.s   IL_0030
                  IL_0025:  ldloca.s   V_2
                  IL_0027:  initobj    "int?"
                  IL_002d:  ldloc.2
                  IL_002e:  br.s       IL_0044
                  IL_0030:  ldloca.s   V_0
                  IL_0032:  call       "readonly int int?.GetValueOrDefault()"
                  IL_0037:  ldloca.s   V_1
                  IL_0039:  call       "readonly int int?.GetValueOrDefault()"
                  IL_003e:  add
                  IL_003f:  newobj     "int?..ctor(int)"
                  IL_0044:  stfld      "int? C.field"
                  IL_0049:  ldarg.0
                  IL_004a:  ldfld      "int? C.field"
                  IL_004f:  box        "int?"
                  IL_0054:  call       "void System.Console.Write(object)"
                  IL_0059:  ret
                }
                """);
        }

        [Fact, WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void SpillCompoundAssignmentToNullableMemberOfLocal_04()
        {
            var source = @"
using System;
using System.Threading.Tasks;
struct S
{
    int? i;

    static async Task M(S s = default)
    {
        s = default;
        Console.WriteLine(s.i += await GetInt());
    }

    static async Task Main()
    {
        M();
    }

    static Task<int?> GetInt() => Task.FromResult((int?)1);
}";
            var expectedOutput = "";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x63 }
                    [Main]: Return value missing on the stack. { Offset = 0xf }
                    """
            });

            verifier.VerifyDiagnostics(
                // (16,9): warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                //         M();
                Diagnostic(ErrorCode.WRN_UnobservedAwaitableExpression, "M()").WithLocation(16, 9)
            );
            verifier.VerifyIL("S.M(S)", """
                {
                  // Code size      100 (0x64)
                  .maxstack  3
                  .locals init (int? V_0,
                                int? V_1,
                                int? V_2)
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  initobj    "S"
                  IL_0008:  ldarg.0
                  IL_0009:  ldfld      "int? S.i"
                  IL_000e:  stloc.0
                  IL_000f:  call       "System.Threading.Tasks.Task<int?> S.GetInt()"
                  IL_0014:  call       "int? System.Runtime.CompilerServices.AsyncHelpers.Await<int?>(System.Threading.Tasks.Task<int?>)"
                  IL_0019:  stloc.1
                  IL_001a:  ldarga.s   V_0
                  IL_001c:  ldflda     "int? S.i"
                  IL_0021:  ldloca.s   V_0
                  IL_0023:  call       "readonly bool int?.HasValue.get"
                  IL_0028:  ldloca.s   V_1
                  IL_002a:  call       "readonly bool int?.HasValue.get"
                  IL_002f:  and
                  IL_0030:  brtrue.s   IL_003d
                  IL_0032:  ldloca.s   V_2
                  IL_0034:  initobj    "int?"
                  IL_003a:  ldloc.2
                  IL_003b:  br.s       IL_0051
                  IL_003d:  ldloca.s   V_0
                  IL_003f:  call       "readonly int int?.GetValueOrDefault()"
                  IL_0044:  ldloca.s   V_1
                  IL_0046:  call       "readonly int int?.GetValueOrDefault()"
                  IL_004b:  add
                  IL_004c:  newobj     "int?..ctor(int)"
                  IL_0051:  dup
                  IL_0052:  stloc.2
                  IL_0053:  stobj      "int?"
                  IL_0058:  ldloc.2
                  IL_0059:  box        "int?"
                  IL_005e:  call       "void System.Console.WriteLine(object)"
                  IL_0063:  ret
                }
                """);
        }

        [Fact]
        public void SpillAssignmentToThisStruct_01()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    async Task M()
                    {
                        i = 1; // Not observable outside of the method
                        return;
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "0";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0xf }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  .locals init (S V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  stloc.0
                  IL_0007:  ldloca.s   V_0
                  IL_0009:  ldc.i4.1
                  IL_000a:  stfld      "int S.i"
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void SpillAssignmentToThisStruct_02()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I
                {
                    public int P { get; set; }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.P);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I
                {
                    public int P { get; set; }
                }

                static class Extensions
                {
                    extension<T>(T t) where T : I
                    {
                        public async Task M()
                        {
                            t.P = 1;
                            return;
                        }
                    }
                }
                """;

            var expectedOutput = "0";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    [M]: Return value missing on the stack. { Offset = 0xe }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Extensions.M<T>(this T)", """
                {
                  // Code size       15 (0xf)
                  .maxstack  2
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  ldc.i4.1
                  IL_0003:  constrained. "T"
                  IL_0009:  callvirt   "void I.P.set"
                  IL_000e:  ret
                }
                """);
        }

        [Fact]
        public void SpillAssignmentToThisStruct_03()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I
                {
                    public int P { get; set; }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.P);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I
                {
                    public int P { get; set; }
                }

                static class Extensions
                {
                    extension<T>(ref T t) where T : struct, I
                    {
                        public async Task M()
                        {
                            t.P = 1;
                            return;
                        }
                    }
                }
                """;

            var expectedDiagnostics = new[] {
                // (26,27): error CS1988: Async methods cannot have ref, in or out parameters
                //         public async Task M()
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "M").WithLocation(26, 27)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular14);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void SpillAssignmentToThisStruct_04()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I
                {
                    public int P { get; set; }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.P);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I
                {
                    public int P { get; set; }
                }

                static class Extensions
                {
                    extension<T>(ref T t) where T : I
                    {
                        public async Task M()
                        {
                            t.P = 1;
                            return;
                        }
                    }
                }
                """;

            var expectedDiagnostics = new[] {
                // (10,17): error CS1061: 'S' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'S' could be found (are you missing a using directive or an assembly reference?)
                //         await s.M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("S", "M").WithLocation(10, 17),
                // (24,22): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension<T>(ref T t) where T : I
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "T").WithLocation(24, 22),
                // (26,27): error CS1988: Async methods cannot have ref, in or out parameters
                //         public async Task M()
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "M").WithLocation(26, 27)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular14);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void SpillAssignmentToThisStruct_05()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    readonly async Task M()
                    {
                        await Task.Yield();
                        Console.Write(i);
                        return;
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "0";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x34 }
                    [Main]: Return value missing on the stack. { Offset = 0x14 }
                    """
            });

            verifier.VerifyDiagnostics(
                // (5,9): warning CS0649: Field 'S.i' is never assigned to, and will always have its default value 0
                //     int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("S.i", "0").WithLocation(5, 9)
            );
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       53 (0x35)
                  .maxstack  2
                  .locals init (System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_0,
                                System.Runtime.CompilerServices.YieldAwaitable V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                  IL_000b:  stloc.1
                  IL_000c:  ldloca.s   V_1
                  IL_000e:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                  IL_0013:  stloc.0
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                  IL_001b:  brtrue.s   IL_0023
                  IL_001d:  ldloc.0
                  IL_001e:  call       "void System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)"
                  IL_0023:  ldloca.s   V_0
                  IL_0025:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                  IL_002a:  ldfld      "int S.i"
                  IL_002f:  call       "void System.Console.Write(int)"
                  IL_0034:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToThisStruct_01()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    async Task M()
                    {
                        Console.Write(i += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x2b }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       44 (0x2c)
                  .maxstack  3
                  .locals init (S V_0,
                                int V_1,
                                int V_2,
                                int V_3)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  stloc.0
                  IL_0007:  ldloc.0
                  IL_0008:  ldfld      "int S.i"
                  IL_000d:  stloc.1
                  IL_000e:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0018:  stloc.2
                  IL_0019:  ldloca.s   V_0
                  IL_001b:  ldloc.1
                  IL_001c:  ldloc.2
                  IL_001d:  add
                  IL_001e:  dup
                  IL_001f:  stloc.3
                  IL_0020:  stfld      "int S.i"
                  IL_0025:  ldloc.3
                  IL_0026:  call       "void System.Console.Write(int)"
                  IL_002b:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToThisStruct_02()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    public static S operator +(S s, int value)
                    {
                        s.i += value;
                        return s;
                    }

                    async Task M()
                    {
                        Console.Write((this += await GetInt()).i);
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x21 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0010:  stloc.0
                  IL_0011:  ldloc.0
                  IL_0012:  call       "S S.op_Addition(S, int)"
                  IL_0017:  ldfld      "int S.i"
                  IL_001c:  call       "void System.Console.Write(int)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToThisStruct_03()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    public static S operator +(S s, int value)
                    {
                        s.i += value;
                        return s;
                    }

                    async Task M()
                    {
                        this += await GetInt();
                        Console.Write(i);
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x21 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       34 (0x22)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0010:  stloc.0
                  IL_0011:  ldloc.0
                  IL_0012:  call       "S S.op_Addition(S, int)"
                  IL_0017:  ldfld      "int S.i"
                  IL_001c:  call       "void System.Console.Write(int)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToThisStruct_04()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    public static S operator +(S s, int value)
                    {
                        s.i += value;
                        return s;
                    }

                    async Task M()
                    {
                        this += await GetInt();
                        this += await GetInt();
                        Console.Write(i);
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "20";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x32 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       51 (0x33)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0010:  stloc.0
                  IL_0011:  ldloc.0
                  IL_0012:  call       "S S.op_Addition(S, int)"
                  IL_0017:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_001c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0021:  stloc.0
                  IL_0022:  ldloc.0
                  IL_0023:  call       "S S.op_Addition(S, int)"
                  IL_0028:  ldfld      "int S.i"
                  IL_002d:  call       "void System.Console.Write(int)"
                  IL_0032:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToThisStruct_05()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S
                {
                    int i;

                    public void operator +=(int value)
                    {
                        this.i += value;
                    }

                    async Task M()
                    {
                        this += await GetInt();
                        Console.Write(i);
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await s.M();
                        Console.Write(s.i);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x25 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  2
                  .locals init (S V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S"
                  IL_0006:  stloc.0
                  IL_0007:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_000c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0011:  stloc.1
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldloc.1
                  IL_0015:  call       "void S.op_AdditionAssignment(int)"
                  IL_001a:  ldloc.0
                  IL_001b:  ldfld      "int S.i"
                  IL_0020:  call       "void System.Console.Write(int)"
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_01()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I
                {
                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : I
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I
                {
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x6e }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size      111 (0x6f)
                  .maxstack  3
                  .locals init (T V_0,
                                int V_1,
                                int V_2,
                                int V_3,
                                T V_4)
                  IL_0000:  ldloca.s   V_4
                  IL_0002:  initobj    "T"
                  IL_0008:  ldloc.s    V_4
                  IL_000a:  box        "T"
                  IL_000f:  brtrue.s   IL_0013
                  IL_0011:  ldarg.0
                  IL_0012:  stloc.0
                  IL_0013:  ldloca.s   V_4
                  IL_0015:  initobj    "T"
                  IL_001b:  ldloc.s    V_4
                  IL_001d:  box        "T"
                  IL_0022:  brtrue.s   IL_0028
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  br.s       IL_002a
                  IL_0028:  ldarga.s   V_0
                  IL_002a:  constrained. "T"
                  IL_0030:  callvirt   "int I.Prop.get"
                  IL_0035:  stloc.1
                  IL_0036:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_003b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0040:  stloc.2
                  IL_0041:  ldloca.s   V_4
                  IL_0043:  initobj    "T"
                  IL_0049:  ldloc.s    V_4
                  IL_004b:  box        "T"
                  IL_0050:  brtrue.s   IL_0056
                  IL_0052:  ldloca.s   V_0
                  IL_0054:  br.s       IL_0058
                  IL_0056:  ldarga.s   V_0
                  IL_0058:  ldloc.1
                  IL_0059:  ldloc.2
                  IL_005a:  add
                  IL_005b:  dup
                  IL_005c:  stloc.3
                  IL_005d:  constrained. "T"
                  IL_0063:  callvirt   "void I.Prop.set"
                  IL_0068:  ldloc.3
                  IL_0069:  call       "void System.Console.Write(int)"
                  IL_006e:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_02()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I<S>
                {
                    public static S operator +(S left, int value)
                    {
                        left.Prop += value;
                        return left;
                    }

                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : I<T>
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I<T> where T : I<T>
                {
                    public static abstract T operator +(T left, int value);
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "10" : null;
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x6e }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size      111 (0x6f)
                  .maxstack  3
                  .locals init (T V_0,
                                int V_1,
                                int V_2,
                                int V_3,
                                T V_4)
                  IL_0000:  ldloca.s   V_4
                  IL_0002:  initobj    "T"
                  IL_0008:  ldloc.s    V_4
                  IL_000a:  box        "T"
                  IL_000f:  brtrue.s   IL_0013
                  IL_0011:  ldarg.0
                  IL_0012:  stloc.0
                  IL_0013:  ldloca.s   V_4
                  IL_0015:  initobj    "T"
                  IL_001b:  ldloc.s    V_4
                  IL_001d:  box        "T"
                  IL_0022:  brtrue.s   IL_0028
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  br.s       IL_002a
                  IL_0028:  ldarga.s   V_0
                  IL_002a:  constrained. "T"
                  IL_0030:  callvirt   "int I<T>.Prop.get"
                  IL_0035:  stloc.1
                  IL_0036:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_003b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0040:  stloc.2
                  IL_0041:  ldloca.s   V_4
                  IL_0043:  initobj    "T"
                  IL_0049:  ldloc.s    V_4
                  IL_004b:  box        "T"
                  IL_0050:  brtrue.s   IL_0056
                  IL_0052:  ldloca.s   V_0
                  IL_0054:  br.s       IL_0058
                  IL_0056:  ldarga.s   V_0
                  IL_0058:  ldloc.1
                  IL_0059:  ldloc.2
                  IL_005a:  add
                  IL_005b:  dup
                  IL_005c:  stloc.3
                  IL_005d:  constrained. "T"
                  IL_0063:  callvirt   "void I<T>.Prop.set"
                  IL_0068:  ldloc.3
                  IL_0069:  call       "void System.Console.Write(int)"
                  IL_006e:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_03()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                struct S : I<S>
                {
                    public void operator +=(int value)
                    {
                        this.Prop += value;
                    }

                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : I<T>
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = default;
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I<T> where T : I<T>
                {
                    public abstract void operator +=(int value);
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = "10";
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x6e }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size      111 (0x6f)
                  .maxstack  3
                  .locals init (T V_0,
                                int V_1,
                                int V_2,
                                int V_3,
                                T V_4)
                  IL_0000:  ldloca.s   V_4
                  IL_0002:  initobj    "T"
                  IL_0008:  ldloc.s    V_4
                  IL_000a:  box        "T"
                  IL_000f:  brtrue.s   IL_0013
                  IL_0011:  ldarg.0
                  IL_0012:  stloc.0
                  IL_0013:  ldloca.s   V_4
                  IL_0015:  initobj    "T"
                  IL_001b:  ldloc.s    V_4
                  IL_001d:  box        "T"
                  IL_0022:  brtrue.s   IL_0028
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  br.s       IL_002a
                  IL_0028:  ldarga.s   V_0
                  IL_002a:  constrained. "T"
                  IL_0030:  callvirt   "int I<T>.Prop.get"
                  IL_0035:  stloc.1
                  IL_0036:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_003b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0040:  stloc.2
                  IL_0041:  ldloca.s   V_4
                  IL_0043:  initobj    "T"
                  IL_0049:  ldloc.s    V_4
                  IL_004b:  box        "T"
                  IL_0050:  brtrue.s   IL_0056
                  IL_0052:  ldloca.s   V_0
                  IL_0054:  br.s       IL_0058
                  IL_0056:  ldarga.s   V_0
                  IL_0058:  ldloc.1
                  IL_0059:  ldloc.2
                  IL_005a:  add
                  IL_005b:  dup
                  IL_005c:  stloc.3
                  IL_005d:  constrained. "T"
                  IL_0063:  callvirt   "void I<T>.Prop.set"
                  IL_0068:  ldloc.3
                  IL_0069:  call       "void System.Console.Write(int)"
                  IL_006e:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_04()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class S : I
                {
                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : class, I
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = new();
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I
                {
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = "11";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x3a }
                    [Main]: Return value missing on the stack. { Offset = 0x1a }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size       59 (0x3b)
                  .maxstack  3
                  .locals init (T V_0,
                                T V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldloca.s   V_0
                  IL_0004:  dup
                  IL_0005:  ldobj      "T"
                  IL_000a:  stloc.1
                  IL_000b:  constrained. "T"
                  IL_0011:  callvirt   "int I.Prop.get"
                  IL_0016:  stloc.2
                  IL_0017:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_001c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0021:  stloc.3
                  IL_0022:  ldloc.1
                  IL_0023:  box        "T"
                  IL_0028:  ldloc.2
                  IL_0029:  ldloc.3
                  IL_002a:  add
                  IL_002b:  dup
                  IL_002c:  stloc.s    V_4
                  IL_002e:  callvirt   "void I.Prop.set"
                  IL_0033:  ldloc.s    V_4
                  IL_0035:  call       "void System.Console.Write(int)"
                  IL_003a:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_05()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class S : I<S>
                {
                    public static S operator +(S left, int value)
                    {
                        left.Prop += value;
                        return left;
                    }

                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : class, I<T>
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = new();
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I<T> where T : I<T>
                {
                    public static abstract T operator +(T left, int value);
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = ExecutionConditionUtil.IsCoreClr ? "11" : null;
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x3a }
                    [Main]: Return value missing on the stack. { Offset = 0x1a }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size       59 (0x3b)
                  .maxstack  3
                  .locals init (T V_0,
                                T V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldloca.s   V_0
                  IL_0004:  dup
                  IL_0005:  ldobj      "T"
                  IL_000a:  stloc.1
                  IL_000b:  constrained. "T"
                  IL_0011:  callvirt   "int I<T>.Prop.get"
                  IL_0016:  stloc.2
                  IL_0017:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_001c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0021:  stloc.3
                  IL_0022:  ldloc.1
                  IL_0023:  box        "T"
                  IL_0028:  ldloc.2
                  IL_0029:  ldloc.3
                  IL_002a:  add
                  IL_002b:  dup
                  IL_002c:  stloc.s    V_4
                  IL_002e:  callvirt   "void I<T>.Prop.set"
                  IL_0033:  ldloc.s    V_4
                  IL_0035:  call       "void System.Console.Write(int)"
                  IL_003a:  ret
                }
                """);
        }

        [Fact]
        public void SpillCompoundAssignmentToTypeParameter_06()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class S : I<S>
                {
                    public void operator +=(int value)
                    {
                        this.Prop += value;
                    }

                    public int Prop { get; set; }

                    static async Task M<T>(T t) where T : class, I<T>
                    {
                        Console.Write(t.Prop += await GetInt());
                    }

                    static async Task Main()
                    {
                        S s = new();
                        await M(s);
                        Console.Write(s.Prop);
                    }

                    static Task<int> GetInt() => Task.FromResult(1);
                }

                interface I<T> where T : I<T>
                {
                    public abstract void operator +=(int value);
                    public int Prop { get; set; }
                }
                """;

            var expectedOutput = "11";
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);
            CompileAndVerify([source, CompilerFeatureRequiredAttribute], expectedOutput: expectedOutput, options: TestOptions.DebugExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x3a }
                    [Main]: Return value missing on the stack. { Offset = 0x1a }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("S.M<T>(T)", """
                {
                  // Code size       59 (0x3b)
                  .maxstack  3
                  .locals init (T V_0,
                                T V_1,
                                int V_2,
                                int V_3,
                                int V_4)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldloca.s   V_0
                  IL_0004:  dup
                  IL_0005:  ldobj      "T"
                  IL_000a:  stloc.1
                  IL_000b:  constrained. "T"
                  IL_0011:  callvirt   "int I<T>.Prop.get"
                  IL_0016:  stloc.2
                  IL_0017:  call       "System.Threading.Tasks.Task<int> S.GetInt()"
                  IL_001c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0021:  stloc.3
                  IL_0022:  ldloc.1
                  IL_0023:  box        "T"
                  IL_0028:  ldloc.2
                  IL_0029:  ldloc.3
                  IL_002a:  add
                  IL_002b:  dup
                  IL_002c:  stloc.s    V_4
                  IL_002e:  callvirt   "void I<T>.Prop.set"
                  IL_0033:  ldloc.s    V_4
                  IL_0035:  call       "void System.Console.Write(int)"
                  IL_003a:  ret
                }
                """);
        }

        [Fact]
        public void SpillSacrificialRead()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void F1(ref int x, int y, int z)
    {
        x += y + z;
    }

    static int F0()
    {
        Console.WriteLine(-1);
        return 0;
    }

    static async Task<int> F2()
    {
        int[] x = new int[1] { 21 };
        ClearX();
        F1(ref x[0], F0(), await Task.Factory.StartNew(() => 21));
        return x[0];

        void ClearX() => x = null;
    }

    public static void Main()
    {
        var t = F2();
        try
        {
            t.Wait();   
        }
        catch(Exception)
        {
            Console.WriteLine(0);
            return;
        }

        Console.WriteLine(-1);
    }
}";
            var expectedOutput = "0";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [F2]: Unexpected type on the stack. { Offset = 0x6d, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.F2()", """
                {
                  // Code size      110 (0x6e)
                  .maxstack  5
                  .locals init (C.<>c__DisplayClass2_0 V_0, //CS$<>8__locals0
                                int V_1,
                                int V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  ldc.i4.1
                  IL_0003:  newarr     "int"
                  IL_0008:  dup
                  IL_0009:  ldc.i4.0
                  IL_000a:  ldc.i4.s   21
                  IL_000c:  stelem.i4
                  IL_000d:  stfld      "int[] C.<>c__DisplayClass2_0.x"
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  call       "void C.<F2>g__ClearX|2_0(ref C.<>c__DisplayClass2_0)"
                  IL_0019:  ldloc.0
                  IL_001a:  ldfld      "int[] C.<>c__DisplayClass2_0.x"
                  IL_001f:  dup
                  IL_0020:  ldc.i4.0
                  IL_0021:  ldelem.i4
                  IL_0022:  pop
                  IL_0023:  call       "int C.F0()"
                  IL_0028:  stloc.1
                  IL_0029:  call       "System.Threading.Tasks.TaskFactory System.Threading.Tasks.Task.Factory.get"
                  IL_002e:  ldsfld     "System.Func<int> C.<>c.<>9__2_1"
                  IL_0033:  dup
                  IL_0034:  brtrue.s   IL_004d
                  IL_0036:  pop
                  IL_0037:  ldsfld     "C.<>c C.<>c.<>9"
                  IL_003c:  ldftn      "int C.<>c.<F2>b__2_1()"
                  IL_0042:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
                  IL_0047:  dup
                  IL_0048:  stsfld     "System.Func<int> C.<>c.<>9__2_1"
                  IL_004d:  callvirt   "System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>)"
                  IL_0052:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0057:  stloc.2
                  IL_0058:  ldc.i4.0
                  IL_0059:  ldelema    "int"
                  IL_005e:  ldloc.1
                  IL_005f:  ldloc.2
                  IL_0060:  call       "void C.F1(ref int, int, int)"
                  IL_0065:  ldloc.0
                  IL_0066:  ldfld      "int[] C.<>c__DisplayClass2_0.x"
                  IL_006b:  ldc.i4.0
                  IL_006c:  ldelem.i4
                  IL_006d:  ret
                }
                """);
        }

        [Fact]
        public void SpillRefThisStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct s1
{
    public int X;

    public async void Goo1()
    {
        Bar(ref this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(ref this, 42);
    }

    public void Bar(ref s1 x, int y)
    {
        x.X = 42;
    }
}

class c1
{
    public int X;

    public async void Goo1()
    {
        Bar(this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(this, 42);
    }

    public void Bar(c1 x, int y)
    {
        x.X = 42;
    }
}

class C
{
    public static void Main()
    {
        {
            s1 s;
            s.X = -1;
            s.Goo1();
            Console.WriteLine(s.X);
        }

        {
            s1 s;
            s.X = -1;
            s.Goo2();
            Console.WriteLine(s.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Goo1();
            Console.WriteLine(c.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Goo2();
            Console.WriteLine(c.X);
        }
    }
}";
            var expectedOutput = @"
-1
42
42
42
";
            CompileAndVerify(source, expectedOutput);
        }

        [Fact]
        public void SpillRefThisStruct_WithTaskAndRuntimeAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

struct s1
{
    public int X;

    public async Task Goo1()
    {
        Bar(ref this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(ref this, 42);
    }

    public void Bar(ref s1 x, int y)
    {
        x.X = 42;
    }
}

class c1
{
    public int X;

    public async Task Goo1()
    {
        Bar(this, await Task<int>.FromResult(42));
    }

    public void Goo2()
    {
        Bar(this, 42);
    }

    public void Bar(c1 x, int y)
    {
        x.X = 42;
    }
}

class C
{
    public static void Main()
    {
        {
            s1 s;
            s.X = -1;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            s.Goo1();
#pragma warning restore CS4014
            Console.WriteLine(s.X);
        }

        {
            s1 s;
            s.X = -1;
            s.Goo2();
            Console.WriteLine(s.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            c.Goo1();
#pragma warning restore CS4014
            Console.WriteLine(c.X);
        }

        {
            c1 c = new c1();
            c.X = -1;
            c.Goo2();
            Console.WriteLine(c.X);
        }
    }
}";
            var expectedOutput = @"
-1
42
42
42
";
            CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Goo1]: Return value missing on the stack. { Offset = 0x1e }
                    [Goo1]: Return value missing on the stack. { Offset = 0x15 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("s1.Goo1()", """
                {
                  // Code size       31 (0x1f)
                  .maxstack  3
                  .locals init (s1 V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "s1"
                  IL_0006:  stloc.0
                  IL_0007:  ldc.i4.s   42
                  IL_0009:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                  IL_000e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0013:  stloc.1
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldloca.s   V_0
                  IL_0018:  ldloc.1
                  IL_0019:  call       "void s1.Bar(ref s1, int)"
                  IL_001e:  ret
                }
                """);

            verifier.VerifyIL("c1.Goo1()", """
                {
                  // Code size       22 (0x16)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldc.i4.s   42
                  IL_0002:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                  IL_0007:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000c:  stloc.0
                  IL_000d:  ldarg.0
                  IL_000e:  ldarg.0
                  IL_000f:  ldloc.0
                  IL_0010:  call       "void c1.Bar(c1, int)"
                  IL_0015:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(13734, "https://github.com/dotnet/roslyn/issues/13734")]
        public void MethodGroupConversionNoSpill()
        {
            string source = @"
using System.Threading.Tasks;
using System;

public class AsyncBug {
    public static void Main() 
    {
        Boom().GetAwaiter().GetResult();
    }
    public static async Task Boom()
    {
        Func<Type> f = (await Task.FromResult(1)).GetType;
        Console.WriteLine(f());
    }
}
";

            // See tracking issue https://github.com/dotnet/runtime/issues/96695
            var expectedOutput = "System.Int32";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput,
                verify: Verification.FailsILVerify with { ILVerifyMessage = "[MoveNext]: Unrecognized arguments for delegate .ctor. { Offset = 0x6d }" });

            verifier.VerifyIL("AsyncBug.<Boom>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
{
  // Code size      169 (0xa9)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int AsyncBug.<Boom>d__1.<>1__state"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003f
    IL_000a:  ldc.i4.1
    IL_000b:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
    IL_0010:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
    IL_0015:  stloc.1
    IL_0016:  ldloca.s   V_1
    IL_0018:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
    IL_001d:  brtrue.s   IL_005b
    IL_001f:  ldarg.0
    IL_0020:  ldc.i4.0
    IL_0021:  dup
    IL_0022:  stloc.0
    IL_0023:  stfld      "int AsyncBug.<Boom>d__1.<>1__state"
    IL_0028:  ldarg.0
    IL_0029:  ldloc.1
    IL_002a:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> AsyncBug.<Boom>d__1.<>u__1"
    IL_002f:  ldarg.0
    IL_0030:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder AsyncBug.<Boom>d__1.<>t__builder"
    IL_0035:  ldloca.s   V_1
    IL_0037:  ldarg.0
    IL_0038:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, AsyncBug.<Boom>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref AsyncBug.<Boom>d__1)"
    IL_003d:  leave.s    IL_00a8
    IL_003f:  ldarg.0
    IL_0040:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> AsyncBug.<Boom>d__1.<>u__1"
    IL_0045:  stloc.1
    IL_0046:  ldarg.0
    IL_0047:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> AsyncBug.<Boom>d__1.<>u__1"
    IL_004c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
    IL_0052:  ldarg.0
    IL_0053:  ldc.i4.m1
    IL_0054:  dup
    IL_0055:  stloc.0
    IL_0056:  stfld      "int AsyncBug.<Boom>d__1.<>1__state"
    IL_005b:  ldloca.s   V_1
    IL_005d:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
    IL_0062:  box        "int"
    IL_0067:  ldftn      "System.Type object.GetType()"
    IL_006d:  newobj     "System.Func<System.Type>..ctor(object, System.IntPtr)"
    IL_0072:  callvirt   "System.Type System.Func<System.Type>.Invoke()"
    IL_0077:  call       "void System.Console.WriteLine(object)"
    IL_007c:  leave.s    IL_0095
  }
  catch System.Exception
  {
    IL_007e:  stloc.2
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.s   -2
    IL_0082:  stfld      "int AsyncBug.<Boom>d__1.<>1__state"
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder AsyncBug.<Boom>d__1.<>t__builder"
    IL_008d:  ldloc.2
    IL_008e:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0093:  leave.s    IL_00a8
  }
  IL_0095:  ldarg.0
  IL_0096:  ldc.i4.s   -2
  IL_0098:  stfld      "int AsyncBug.<Boom>d__1.<>1__state"
  IL_009d:  ldarg.0
  IL_009e:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder AsyncBug.<Boom>d__1.<>t__builder"
  IL_00a3:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00a8:  ret
}
""");

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Boom]: Unrecognized arguments for delegate .ctor. { Offset = 0x16 }
                    [Boom]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("AsyncBug.Boom()", """
                {
                  // Code size       38 (0x26)
                  .maxstack  2
                  IL_0000:  ldc.i4.1
                  IL_0001:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                  IL_0006:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000b:  box        "int"
                  IL_0010:  ldftn      "System.Type object.GetType()"
                  IL_0016:  newobj     "System.Func<System.Type>..ctor(object, System.IntPtr)"
                  IL_001b:  callvirt   "System.Type System.Func<System.Type>.Invoke()"
                  IL_0020:  call       "void System.Console.WriteLine(object)"
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(13734, "https://github.com/dotnet/roslyn/issues/13734")]
        public void MethodGroupConversionWithSpill()
        {
            string source = @"
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
namespace AsyncBug
{
    class Program
    {
        private class SomeClass
        {
            public bool Method(int value)
            {
                return value % 2 == 0;
            }
        }

        private async Task<SomeClass> Danger()
        {
            await Task.Yield();
            return new SomeClass();
        }

        private async Task<IEnumerable<bool>> Killer()
        {
            return (new int[] {1, 2, 3, 4, 5}).Select((await Danger()).Method);
        }

        static void Main(string[] args)
        {
            foreach (var b in new Program().Killer().GetAwaiter().GetResult()) {
                Console.WriteLine(b);
            }
        }
    }
}
";
            var expectedOutput = new bool[] { false, true, false, true, false }.Aggregate("", (str, next) => str += $"{next}{Environment.NewLine}");
            var v = CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Danger]: Unexpected type on the stack. { Offset = 0x29, Found = ref 'AsyncBug.Program+SomeClass', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<AsyncBug.Program+SomeClass>' }
                    [Killer]: Unexpected type on the stack. { Offset = 0x2e, Found = ref '[System.Runtime]System.Collections.Generic.IEnumerable`1<bool>', Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<System.Collections.Generic.IEnumerable`1<bool>>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("AsyncBug.Program.Killer()", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  3
                  .locals init (AsyncBug.Program.SomeClass V_0)
                  IL_0000:  ldc.i4.5
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.4F6ADDC9659D6FB90FE94B6688A79F2A1FA8D36EC43F8F3E1D9B6528C448A384"
                  IL_000c:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                  IL_0011:  ldarg.0
                  IL_0012:  call       "System.Threading.Tasks.Task<AsyncBug.Program.SomeClass> AsyncBug.Program.Danger()"
                  IL_0017:  call       "AsyncBug.Program.SomeClass System.Runtime.CompilerServices.AsyncHelpers.Await<AsyncBug.Program.SomeClass>(System.Threading.Tasks.Task<AsyncBug.Program.SomeClass>)"
                  IL_001c:  stloc.0
                  IL_001d:  ldloc.0
                  IL_001e:  ldftn      "bool AsyncBug.Program.SomeClass.Method(int)"
                  IL_0024:  newobj     "System.Func<int, bool>..ctor(object, System.IntPtr)"
                  IL_0029:  call       "System.Collections.Generic.IEnumerable<bool> System.Linq.Enumerable.Select<int, bool>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)"
                  IL_002e:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(17706, "https://github.com/dotnet/roslyn/issues/17706")]
        public void SpillAwaitBeforeRefReordered()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
    private static int i;

    static ref int P => ref i;

    static void Assign(ref int first, int second)
    {
        first = second;
    }

    public static async Task M(Task<int> t)
    {
        // OK: await goes before the ref
        Assign(second: await t, first: ref P);
    }

    public static void Main()
    {
        M(Task.FromResult(42)).Wait();

        System.Console.WriteLine(i);
    }
}
";

            var expectedOutput = "42";
            var v = CompileAndVerify(source, expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x12 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M(System.Threading.Tasks.Task<int>)", """
                {
                  // Code size       19 (0x13)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0006:  stloc.0
                  IL_0007:  call       "ref int C.P.get"
                  IL_000c:  ldloc.0
                  IL_000d:  call       "void C.Assign(ref int, int)"
                  IL_0012:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(17706, "https://github.com/dotnet/roslyn/issues/17706")]
        public void SpillRefBeforeAwaitReordered()
        {
            string source = @"
using System.Threading.Tasks;

public class C
{
    private static int i;

    static ref int P => ref i;

    static void Assign(int first, ref int second)
    {
        second = first;
    }

    public static async Task M(Task<int> t)
    {
        // ERROR: await goes after the ref
        Assign(second: ref P, first: await t);
    }

    public static void Main()
    {
        M(Task.FromResult(42)).Wait();

        System.Console.WriteLine(i);
    }
}
";

            var expected =
                // (18,28): error CS8178: A reference returned by a call to 'C.P.get' cannot be preserved across 'await' or 'yield' boundary.
                //         Assign(second: ref P, first: await t);
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "P").WithArguments("C.P.get").WithLocation(18, 28);

            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(expected);

            comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        [WorkItem(27831, "https://github.com/dotnet/roslyn/issues/27831")]
        public void AwaitWithInParameter_ArgModifier()
        {
            CreateCompilation(@"
using System.Threading.Tasks;
class Foo
{
    async Task A(string s, Task<int> task)
    {
        C(in s, await task);
    }

    void C(in object obj, int length) {}
}").VerifyDiagnostics(
                // (7,14): error CS1503: Argument 1: cannot convert from 'in string' to 'in object'
                //         C(in s, await task);
                Diagnostic(ErrorCode.ERR_BadArgType, "s").WithArguments("1", "in string", "in object").WithLocation(7, 14));
        }

        [Fact]
        [WorkItem(27831, "https://github.com/dotnet/roslyn/issues/27831")]
        public void AwaitWithInParameter_NoArgModifier()
        {
            var source = """
                using System;
                using System.Threading.Tasks;
                class Goo
                {
                    static async Task Main()
                    {
                        await A("test", Task.FromResult(4));
                    }

                    static async Task A(string s, Task<int> task)
                    {
                        B(s, await task);
                    }

                    static void B(in object obj, int v)
                    {
                        Console.WriteLine(obj);
                        Console.WriteLine(v);
                    }
                }
                """;
            var expectedOutput = """
                test
                4
                """;
            CompileAndVerify(source, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x15 }
                    [A]: Return value missing on the stack. { Offset = 0x11 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Goo.A(string, System.Threading.Tasks.Task<int>)", """
                {
                  // Code size       18 (0x12)
                  .maxstack  2
                  .locals init (object V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldarg.1
                  IL_0003:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0008:  stloc.1
                  IL_0009:  ldloca.s   V_0
                  IL_000b:  ldloc.1
                  IL_000c:  call       "void Goo.B(in object, int)"
                  IL_0011:  ret
                }
                """);
        }

        [Fact, WorkItem(36856, "https://github.com/dotnet/roslyn/issues/36856")]
        public void Crash36856()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    private static async Task Serialize()
    {
        System.Text.Json.Serialization.JsonSerializer.Parse<string>(await TestAsync());
    }

    private static Task<byte[]> TestAsync()
    {
        return null;
    }
}
namespace System.Text.Json.Serialization
{
    public static class JsonSerializer
    {
        public static TValue Parse<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
        {
            throw null;
        }
    }
    public sealed class JsonSerializerOptions
    {
    }
}
";

            var span = @"
namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        public static implicit operator ReadOnlySpan<T>(T[] array)
        {
            throw null;
        }
    }
}
";
            var v = CompileAndVerify(source + span, options: TestOptions.DebugExe);

            v.VerifyMethodBody("Program.<Serialize>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<byte[]> V_1,
                Program.<Serialize>d__1 V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Serialize>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0047
    // sequence point: {
    IL_000e:  nop
    // sequence point: System.Text.Json.Serialization.JsonSerializer.Parse<string>(await TestAsync());
    IL_000f:  call       ""System.Threading.Tasks.Task<byte[]> Program.TestAsync()""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> System.Threading.Tasks.Task<byte[]>.GetAwaiter()""
    IL_0019:  stloc.1
    // sequence point: <hidden>
    IL_001a:  ldloca.s   V_1
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<byte[]>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_0063
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Serialize>d__1.<>1__state""
    // async: yield
    IL_002c:  ldarg.0
    IL_002d:  ldloc.1
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  stloc.2
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
    IL_003b:  ldloca.s   V_1
    IL_003d:  ldloca.s   V_2
    IL_003f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<byte[]>, Program.<Serialize>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<byte[]>, ref Program.<Serialize>d__1)""
    IL_0044:  nop
    IL_0045:  leave.s    IL_00b7
    // async: resume
    IL_0047:  ldarg.0
    IL_0048:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
    IL_004d:  stloc.1
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<byte[]> Program.<Serialize>d__1.<>u__1""
    IL_0054:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<byte[]>""
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      ""int Program.<Serialize>d__1.<>1__state""
    IL_0063:  ldarg.0
    IL_0064:  ldloca.s   V_1
    IL_0066:  call       ""byte[] System.Runtime.CompilerServices.TaskAwaiter<byte[]>.GetResult()""
    IL_006b:  stfld      ""byte[] Program.<Serialize>d__1.<>s__1""
    IL_0070:  ldarg.0
    IL_0071:  ldfld      ""byte[] Program.<Serialize>d__1.<>s__1""
    IL_0076:  call       ""System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])""
    IL_007b:  ldnull
    IL_007c:  call       ""string System.Text.Json.Serialization.JsonSerializer.Parse<string>(System.ReadOnlySpan<byte>, System.Text.Json.Serialization.JsonSerializerOptions)""
    IL_0081:  pop
    IL_0082:  ldarg.0
    IL_0083:  ldnull
    IL_0084:  stfld      ""byte[] Program.<Serialize>d__1.<>s__1""
    IL_0089:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int Program.<Serialize>d__1.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
    IL_009a:  ldloc.3
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a0:  nop
    IL_00a1:  leave.s    IL_00b7
  }
  // sequence point: }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Program.<Serialize>d__1.<>1__state""
  // sequence point: <hidden>
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Serialize>d__1.<>t__builder""
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b6:  nop
  IL_00b7:  ret
}
");

            var comp = CreateRuntimeAsyncCompilation(source);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Serialize]: Return value missing on the stack. { Offset = 0x16 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Serialize()", """
                {
                  // Code size       23 (0x17)
                  .maxstack  2
                  IL_0000:  call       "System.Threading.Tasks.Task<byte[]> Program.TestAsync()"
                  IL_0005:  call       "byte[] System.Runtime.CompilerServices.AsyncHelpers.Await<byte[]>(System.Threading.Tasks.Task<byte[]>)"
                  IL_000a:  call       "System.ReadOnlySpan<byte> System.ReadOnlySpan<byte>.op_Implicit(byte[])"
                  IL_000f:  ldnull
                  IL_0010:  call       "string System.Text.Json.Serialization.JsonSerializer.Parse<string>(System.ReadOnlySpan<byte>, System.Text.Json.Serialization.JsonSerializerOptions)"
                  IL_0015:  pop
                  IL_0016:  ret
                }
                """);
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 40, 500, 6000 }));
    }

    static int F1() => 70000;
    static int F2() => 800000;
    static int G(int k, Span<int> span) => k + span.Length + span[0] + span[1] + span[2];
    static Task Async1(int k, int i)
    {
        Console.WriteLine(k + i);
        return Task.Delay(1);
    }
}
";
            var expectedOutput = @"876543";

            var comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
            comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                   [Main]: Instruction cannot be verified. { Offset = 0xf }
                   [Main]: Instruction cannot be verified. { Offset = 0x2a }
                   [Main]: Return value missing on the stack. { Offset = 0x45 }
                   """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("P.Main()", """
                {
                  // Code size       70 (0x46)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1,
                                System.Span<int> V_2,
                                System.ReadOnlySpan<int> V_3)
                  IL_0000:  call       "int P.F1()"
                  IL_0005:  stloc.0
                  IL_0006:  call       "int P.F2()"
                  IL_000b:  stloc.1
                  IL_000c:  ldc.i4.s   12
                  IL_000e:  conv.u
                  IL_000f:  localloc
                  IL_0011:  dup
                  IL_0012:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.358467287387D6976FA2AD2813A4D1AE4F0A0865C5125FB87D822D9432AA423D4"
                  IL_0017:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
                  IL_001c:  stloc.3
                  IL_001d:  ldloca.s   V_3
                  IL_001f:  ldc.i4.0
                  IL_0020:  call       "ref readonly int System.ReadOnlySpan<int>.this[int].get"
                  IL_0025:  ldc.i4.s   12
                  IL_0027:  unaligned. 4
                  IL_002a:  cpblk
                  IL_002c:  ldc.i4.3
                  IL_002d:  newobj     "System.Span<int>..ctor(void*, int)"
                  IL_0032:  stloc.2
                  IL_0033:  ldloc.0
                  IL_0034:  ldloc.1
                  IL_0035:  ldloc.2
                  IL_0036:  call       "int P.G(int, System.Span<int>)"
                  IL_003b:  call       "System.Threading.Tasks.Task P.Async1(int, int)"
                  IL_0040:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_0045:  ret
                }
                """);
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 40, await Task.FromResult(500), 6000 }));
    }

    static int F1() => 70000;
    static int F2() => 800000;
    static int G(int k, Span<int> span) => k + span.Length + span[0] + span[1] + span[2];
    static Task Async1(int k, int i)
    {
        Console.WriteLine(k + i);
        return Task.Delay(1);
    }
}
";
            var expectedOutput = @"876543";

            var comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );
            comp = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            v = CompileAndVerify(
                compilation: comp,
                expectedOutput: expectedOutput,
                verify: Verification.Fails // localloc is not verifiable.
                );

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Instruction cannot be verified. { Offset = 0x1f }
                    [Main]: Expected ByRef on the stack. { Offset = 0x24, Found = Native Int }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("P.Main()", """
                {
                  // Code size       80 (0x50)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1,
                                System.Span<int> V_2,
                                int V_3)
                  IL_0000:  call       "int P.F1()"
                  IL_0005:  stloc.0
                  IL_0006:  call       "int P.F2()"
                  IL_000b:  stloc.1
                  IL_000c:  ldc.i4     0x1f4
                  IL_0011:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                  IL_0016:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001b:  stloc.3
                  IL_001c:  ldc.i4.s   12
                  IL_001e:  conv.u
                  IL_001f:  localloc
                  IL_0021:  dup
                  IL_0022:  ldc.i4.s   40
                  IL_0024:  stind.i4
                  IL_0025:  dup
                  IL_0026:  ldc.i4.4
                  IL_0027:  add
                  IL_0028:  ldloc.3
                  IL_0029:  stind.i4
                  IL_002a:  dup
                  IL_002b:  ldc.i4.2
                  IL_002c:  conv.i
                  IL_002d:  ldc.i4.4
                  IL_002e:  mul
                  IL_002f:  add
                  IL_0030:  ldc.i4     0x1770
                  IL_0035:  stind.i4
                  IL_0036:  ldc.i4.3
                  IL_0037:  newobj     "System.Span<int>..ctor(void*, int)"
                  IL_003c:  stloc.2
                  IL_003d:  ldloc.0
                  IL_003e:  ldloc.1
                  IL_003f:  ldloc.2
                  IL_0040:  call       "int P.G(int, System.Span<int>)"
                  IL_0045:  call       "System.Threading.Tasks.Task P.Async1(int, int)"
                  IL_004a:  call       "void System.Runtime.CompilerServices.AsyncHelpers.Await(System.Threading.Tasks.Task)"
                  IL_004f:  ret
                }
                """);
        }

        [Fact, WorkItem(37461, "https://github.com/dotnet/roslyn/issues/37461")]
        public void ShouldNotSpillStackallocToField_03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public class P
{
    static async Task Main()
    {
        await Async1(F1(), G(F2(), stackalloc int[] { 1, 2, 3 }, await F3()));
    }

    static object F1() => 1;
    static object F2() => 1;
    static Task<object> F3() => Task.FromResult<object>(1);
    static int G(object obj, Span<int> span, object o2) => span.Length;
    static async Task Async1(Object obj, int i) { await Task.Delay(1); }
}
";
            foreach (var options in new[] { TestOptions.DebugExe, TestOptions.ReleaseExe })
            {
                var comp = CreateCompilationWithMscorlibAndSpan(source, options: options);
                comp.VerifyDiagnostics();
                comp.VerifyEmitDiagnostics(
                    // (8,5): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
                    //     {
                    Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"{
        await Async1(F1(), G(F2(), stackalloc int[] { 1, 2, 3 }, await F3()));
    }").WithArguments("System.Span<int>").WithLocation(8, 5)
                    );
            }
        }

        [Fact]
        public void SpillStateMachineTemps()
        {
            var source = @"using System;
using System.Threading.Tasks;

public class C {
    public static void Main()
    {
        Console.WriteLine(M1(new Q(), SF()).Result);
    }
    public static async Task<int> M1(object o, Task<bool> c)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => 1, // cached Q.F is alive
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    public static async Task<bool> SF()
    {
        await Task.Delay(10);
        return false;
    }
}

class Q
{
    public F F => new F(true);
}

struct F
{
    bool _result;
    public F(bool result)
    {
        _result = result;
    }
    public bool P1 => _result;
    public bool P2 => _result;
}
";
            var expectedOutput = "2";
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
            CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: expectedOutput);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M1]: Unexpected type on the stack. { Offset = 0x38, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [SF]: Unexpected type on the stack. { Offset = 0xd, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<bool>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1(object, System.Threading.Tasks.Task<bool>)", """
                {
                  // Code size       57 (0x39)
                  .maxstack  1
                  .locals init (int V_0,
                                Q V_1,
                                F V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  isinst     "Q"
                  IL_0006:  stloc.1
                  IL_0007:  ldloc.1
                  IL_0008:  brfalse.s  IL_0035
                  IL_000a:  ldloc.1
                  IL_000b:  callvirt   "F Q.F.get"
                  IL_0010:  stloc.2
                  IL_0011:  ldloca.s   V_2
                  IL_0013:  call       "bool F.P1.get"
                  IL_0018:  brtrue.s   IL_0025
                  IL_001a:  ldloca.s   V_2
                  IL_001c:  call       "bool F.P2.get"
                  IL_0021:  brtrue.s   IL_0031
                  IL_0023:  br.s       IL_0035
                  IL_0025:  ldarg.1
                  IL_0026:  call       "bool System.Runtime.CompilerServices.AsyncHelpers.Await<bool>(System.Threading.Tasks.Task<bool>)"
                  IL_002b:  brfalse.s  IL_001a
                  IL_002d:  ldc.i4.1
                  IL_002e:  stloc.0
                  IL_002f:  br.s       IL_0037
                  IL_0031:  ldc.i4.2
                  IL_0032:  stloc.0
                  IL_0033:  br.s       IL_0037
                  IL_0035:  ldc.i4.3
                  IL_0036:  stloc.0
                  IL_0037:  ldloc.0
                  IL_0038:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(37713, "https://github.com/dotnet/roslyn/issues/37713")]
        public void RefStructInAsyncStateMachineWithWhenClause()
        {
            var source = @"
using System.Threading.Tasks;
class Program
{
    async Task<int> M1(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M2(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // ok: only Q.P1 is live
            Q { F: { P1: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M3(object o, bool c, Task<int> r)
    {
        return o switch
        {
            Q { F: { P1: true } } when c => await r, // ok: nothing alive at await
            Q { F: { P2: true } } => 2,
            _ => 3,
        };
    }
    async Task<int> M4(object o, Task<bool> c, int r)
    {
        return o switch
        {
            Q { F: { P1: true } } when await c => r, // ok: no switch state is alive
            _ => 3,
        };
    }
}
public class Q
{
    public S F => throw null!;
}
public ref struct S
{
    public bool P1 => true;
    public bool P2 => true;
}
";

            var expectedDiagnostics = new[]
            {
                // (9,17): error CS4007: Instance of type 'S' cannot be preserved across 'await' or 'yield' boundary.
                //             Q { F: { P1: true } } when await c => r, // error: cached Q.F is alive
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "F").WithArguments("S").WithLocation(9, 17)
            };

            CreateCompilation(source, options: TestOptions.DebugDll).VerifyDiagnostics().VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics().VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(37783, "https://github.com/dotnet/roslyn/issues/37783")]
        public void ExpressionLambdaWithObjectInitializer()
        {
            var source =
@"using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

class Program
{
    public static async Task Main()
    {
        int value = 42;
        Console.WriteLine(await M(() => new Box<int>() { Value = value }));
    }

    static Task<int> M(Expression<Func<Box<int>>> e)
    {
        return Task.FromResult(e.Compile()().Value);
    }
}

class Box<T>
{
    public T Value;
}
";
            var expectedOutput = "42";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0x77 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main()", """
                {
                  // Code size      120 (0x78)
                  .maxstack  7
                  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
                  IL_0000:  newobj     "Program.<>c__DisplayClass0_0..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.s   42
                  IL_0009:  stfld      "int Program.<>c__DisplayClass0_0.value"
                  IL_000e:  ldtoken    "Box<int>"
                  IL_0013:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0018:  call       "System.Linq.Expressions.NewExpression System.Linq.Expressions.Expression.New(System.Type)"
                  IL_001d:  ldc.i4.1
                  IL_001e:  newarr     "System.Linq.Expressions.MemberBinding"
                  IL_0023:  dup
                  IL_0024:  ldc.i4.0
                  IL_0025:  ldtoken    "int Box<int>.Value"
                  IL_002a:  ldtoken    "Box<int>"
                  IL_002f:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle, System.RuntimeTypeHandle)"
                  IL_0034:  ldloc.0
                  IL_0035:  ldtoken    "Program.<>c__DisplayClass0_0"
                  IL_003a:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_003f:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
                  IL_0044:  ldtoken    "int Program.<>c__DisplayClass0_0.value"
                  IL_0049:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
                  IL_004e:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
                  IL_0053:  call       "System.Linq.Expressions.MemberAssignment System.Linq.Expressions.Expression.Bind(System.Reflection.MemberInfo, System.Linq.Expressions.Expression)"
                  IL_0058:  stelem.ref
                  IL_0059:  call       "System.Linq.Expressions.MemberInitExpression System.Linq.Expressions.Expression.MemberInit(System.Linq.Expressions.NewExpression, params System.Linq.Expressions.MemberBinding[])"
                  IL_005e:  call       "System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()"
                  IL_0063:  call       "System.Linq.Expressions.Expression<System.Func<Box<int>>> System.Linq.Expressions.Expression.Lambda<System.Func<Box<int>>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
                  IL_0068:  call       "System.Threading.Tasks.Task<int> Program.M(System.Linq.Expressions.Expression<System.Func<Box<int>>>)"
                  IL_006d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0072:  call       "void System.Console.WriteLine(int)"
                  IL_0077:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(38309, "https://github.com/dotnet/roslyn/issues/38309")]
        public void ExpressionLambdaWithUserDefinedControlFlow()
        {
            var source =
@"using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RoslynFailFastReproduction
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            await MainAsync(args);
        }
        static async Task MainAsync(string[] args)
        {
            Expression<Func<AltBoolean, AltBoolean>> expr = x => x && x;

            var result = await Task.FromResult(true);
            Console.WriteLine(result);
        }

        class AltBoolean
        {
            public static AltBoolean operator &(AltBoolean x, AltBoolean y) => default;
            public static bool operator true(AltBoolean x) => default;
            public static bool operator false(AltBoolean x) => default;
        }
    }
}
";
            var expectedOutput = "True";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.DebugExe);
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.ReleaseExe);

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Main]: Return value missing on the stack. { Offset = 0xb }
                    [MainAsync]: Return value missing on the stack. { Offset = 0x4b }
                    """
            });
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassFieldAccessOnProperty()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.B.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestPropertyAccessThrows();
        await TestFieldAccessThrows();
        await TestPropertyAccessSucceeds();
    }

    static async Task TestPropertyAccessThrows()
    {
        Console.WriteLine(nameof(TestPropertyAccessThrows));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestFieldAccessThrows()
    {
        Console.WriteLine(nameof(TestFieldAccessThrows));
        
        var a = new A();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestPropertyAccessSucceeds()
    {
        Console.WriteLine(nameof(TestPropertyAccessSucceeds));

        var a = new A{ B = new B() };
        Console.WriteLine(""Before Assignment a.B.x is: "" + a.B.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.B.x is: "" + a.B.x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B B { get; set; }
}

class B
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestPropertyAccessThrows
                Before Assignment
                Caught NullReferenceException
                TestFieldAccessThrows
                Before Assignment
                RHS
                Caught NullReferenceException
                TestPropertyAccessSucceeds
                Before Assignment a.B.x is: 0
                RHS
                After Assignment a.B.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0054
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.B.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldstr      ""RHS""
    IL_0020:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0070
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_004a:  ldloca.s   V_2
    IL_004c:  ldarg.0
    IL_004d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0052:  leave.s    IL_00b7
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0070:  ldloca.s   V_2
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stloc.1
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_007e:  ldloc.1
    IL_007f:  stfld      ""int B.x""
    IL_0084:  ldarg.0
    IL_0085:  ldnull
    IL_0086:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008b:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008d:  stloc.3
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_009c:  ldloc.3
    IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a2:  leave.s    IL_00b7
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b7:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x1c }
                    [Main]: Return value missing on the stack. { Offset = 0x1e }
                    [TestPropertyAccessThrows]: Return value missing on the stack. { Offset = 0x30 }
                    [TestFieldAccessThrows]: Return value missing on the stack. { Offset = 0x34 }
                    [TestPropertyAccessSucceeds]: Return value missing on the stack. { Offset = 0x64 }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       29 (0x1d)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  callvirt   "B A.B.get"
                  IL_0006:  ldstr      "RHS"
                  IL_000b:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0010:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0015:  stloc.0
                  IL_0016:  ldloc.0
                  IL_0017:  stfld      "int B.x"
                  IL_001c:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassFieldAccessOnArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A[] arr)
    {
        arr[0].x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestAssignmentThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
        await TestReassignsTargetDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new A[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestAssignmentThrows()
    {
        Console.WriteLine(nameof(TestAssignmentThrows));
        
        var arr = new A[1];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new A[1]{ new A() };
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var a = new A();
        var arr = new A[1]{ a };
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        arr[index].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new A[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task TestReassignsTargetDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsTargetDuringAwait));

        var a = new A();
        var arr = new A[1]{ a };
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        arr[0].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""After Assignment arr[0].y is: "" + arr[0].y);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr[0] = new A{ y = true };
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;

    public bool y;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestIndexerThrows
                Before Assignment
                Caught IndexOutOfRangeException
                TestAssignmentThrows
                Before Assignment
                RHS
                Caught NullReferenceException
                TestIndexerSucceeds
                Before Assignment arr[0].x is: 0
                RHS
                After Assignment arr[0].x is: 42
                TestReassignsArrayAndIndexerDuringAwait
                Before Assignment arr.Length is: 1
                Before Assignment a.x is: 0
                RHS
                After Assignment arr.Length is: 0
                After Assignment a.x is: 42
                TestReassignsTargetDuringAwait
                Before Assignment arr[0].x is: 0
                Before Assignment arr[0].y is: False
                Before Assignment a.x is: 0
                RHS
                After Assignment arr[0].x is: 0
                After Assignment arr[0].y is: True
                After Assignment a.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      181 (0xb5)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A[] Program.<Assign>d__0.arr""
    IL_0011:  ldc.i4.0
    IL_0012:  ldelem.ref
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldstr      ""RHS""
    IL_001d:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0022:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0027:  stloc.2
    IL_0028:  ldloca.s   V_2
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_006d
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.2
    IL_003c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0047:  ldloca.s   V_2
    IL_0049:  ldarg.0
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004f:  leave.s    IL_00b4
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0057:  stloc.2
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0074:  stloc.1
    IL_0075:  ldarg.0
    IL_0076:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_007b:  ldloc.1
    IL_007c:  stfld      ""int A.x""
    IL_0081:  ldarg.0
    IL_0082:  ldnull
    IL_0083:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0088:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_008a:  stloc.3
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.s   -2
    IL_008e:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0099:  ldloc.3
    IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009f:  leave.s    IL_00b4
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b4:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x19 }
                    [Main]: Return value missing on the stack. { Offset = 0x32 }
                    [TestIndexerThrows]: Return value missing on the stack. { Offset = 0x35 }
                    [TestAssignmentThrows]: Return value missing on the stack. { Offset = 0x35 }
                    [TestIndexerSucceeds]: Return value missing on the stack. { Offset = 0x5c }
                    [TestReassignsArrayAndIndexerDuringAwait]: Return value missing on the stack. { Offset = 0xc3 }
                    [TestReassignsTargetDuringAwait]: Return value missing on the stack. { Offset = 0xfd }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<TestReassignsArrayAndIndexerDuringAwait>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x3f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<TestReassignsTargetDuringAwait>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x40, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A[])", """
                {
                  // Code size       26 (0x1a)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.0
                  IL_0002:  ldelem.ref
                  IL_0003:  ldstr      "RHS"
                  IL_0008:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_000d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0012:  stloc.0
                  IL_0013:  ldloc.0
                  IL_0014:  stfld      "int A.x"
                  IL_0019:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_StructFieldAccessOnArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A[] arr)
    {
        arr[0].x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
        await TestReassignsTargetDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new A[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new A[1];
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var arr = new A[1];
        var arrCopy = arr;
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment arrCopy[0].x is: "" + arrCopy[0].x);
        arr[index].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment arrCopy[0].x is: "" + arrCopy[0].x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new A[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task TestReassignsTargetDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsTargetDuringAwait));

        var arr = new A[1];
        Console.WriteLine(""Before Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);
        arr[0].x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr[0].x is: "" + arr[0].x);
        Console.WriteLine(""Before Assignment arr[0].y is: "" + arr[0].y);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr[0] = new A{y = true };
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

struct A
{
    public int x;

    public bool y;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestIndexerThrows
                Before Assignment
                Caught IndexOutOfRangeException
                TestIndexerSucceeds
                Before Assignment arr[0].x is: 0
                RHS
                After Assignment arr[0].x is: 42
                TestReassignsArrayAndIndexerDuringAwait
                Before Assignment arr.Length is: 1
                Before Assignment arrCopy[0].x is: 0
                RHS
                After Assignment arr.Length is: 0
                After Assignment arrCopy[0].x is: 42
                TestReassignsTargetDuringAwait
                Before Assignment arr[0].x is: 0
                Before Assignment arr[0].y is: False
                RHS
                After Assignment arr[0].x is: 42
                Before Assignment arr[0].y is: True
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005c
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A[] Program.<Assign>d__0.arr""
    IL_0011:  stfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_001c:  ldc.i4.0
    IL_001d:  ldelema    ""A""
    IL_0022:  pop
    IL_0023:  ldstr      ""RHS""
    IL_0028:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0032:  stloc.2
    IL_0033:  ldloca.s   V_2
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003a:  brtrue.s   IL_0078
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.2
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0052:  ldloca.s   V_2
    IL_0054:  ldarg.0
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005a:  leave.s    IL_00c5
    IL_005c:  ldarg.0
    IL_005d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0062:  stloc.2
    IL_0063:  ldarg.0
    IL_0064:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0069:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.m1
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007f:  stloc.1
    IL_0080:  ldarg.0
    IL_0081:  ldfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0086:  ldc.i4.0
    IL_0087:  ldelema    ""A""
    IL_008c:  ldloc.1
    IL_008d:  stfld      ""int A.x""
    IL_0092:  ldarg.0
    IL_0093:  ldnull
    IL_0094:  stfld      ""A[] Program.<Assign>d__0.<>7__wrap1""
    IL_0099:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_009b:  stloc.3
    IL_009c:  ldarg.0
    IL_009d:  ldc.i4.s   -2
    IL_009f:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00aa:  ldloc.3
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b0:  leave.s    IL_00c5
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ba:  ldarg.0
  IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c5:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x25 }
                    [Main]: Return value missing on the stack. { Offset = 0x28 }
                    [TestIndexerThrows]: Return value missing on the stack. { Offset = 0x35 }
                    [TestIndexerSucceeds]: Return value missing on the stack. { Offset = 0x5c }
                    [TestReassignsArrayAndIndexerDuringAwait]: Return value missing on the stack. { Offset = 0xda }
                    [TestReassignsTargetDuringAwait]: Return value missing on the stack. { Offset = 0xdb }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<TestReassignsArrayAndIndexerDuringAwait>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x3f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<TestReassignsTargetDuringAwait>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x49, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A[])", """
                {
                  // Code size       38 (0x26)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  ldc.i4.0
                  IL_0003:  ldelema    "A"
                  IL_0008:  pop
                  IL_0009:  ldstr      "RHS"
                  IL_000e:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0018:  stloc.0
                  IL_0019:  ldc.i4.0
                  IL_001a:  ldelema    "A"
                  IL_001f:  ldloc.0
                  IL_0020:  stfld      "int A.x"
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_AssignmentToArray()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(int[] arr)
    {
        arr[0] = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestIndexerThrows();
        await TestIndexerSucceeds();
        await TestReassignsArrayAndIndexerDuringAwait();
    }

    static async Task TestIndexerThrows()
    {
        Console.WriteLine(nameof(TestIndexerThrows));
        
        var arr = new int[0];
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(arr);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine(""Caught IndexOutOfRangeException"");
        }
    }

    static async Task TestIndexerSucceeds()
    {
        Console.WriteLine(nameof(TestIndexerSucceeds));

        var arr = new int[1];
        Console.WriteLine(""Before Assignment arr[0] is: "" + arr[0]);
        await Assign(arr);
        Console.WriteLine(""After Assignment arr[0] is: "" + arr[0]);
    }

    static async Task TestReassignsArrayAndIndexerDuringAwait()
    {
        Console.WriteLine(nameof(TestReassignsArrayAndIndexerDuringAwait));

        var arr = new int[1];
        var arrCopy = arr;
        var index = 0;
        Console.WriteLine(""Before Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""Before Assignment arrCopy[0] is: "" + arrCopy[0]);
        arr[index] = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment arr.Length is: "" + arr.Length);
        Console.WriteLine(""After Assignment arrCopy[0] is: "" + arrCopy[0]);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            arr = new int[0];
            index = 1;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestIndexerThrows
                Before Assignment
                RHS
                Caught IndexOutOfRangeException
                TestIndexerSucceeds
                Before Assignment arr[0] is: 0
                RHS
                After Assignment arr[0] is: 42
                TestReassignsArrayAndIndexerDuringAwait
                Before Assignment arr.Length is: 1
                Before Assignment arrCopy[0] is: 0
                RHS
                After Assignment arr.Length is: 0
                After Assignment arrCopy[0] is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004f
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""int[] Program.<Assign>d__0.arr""
    IL_0011:  stfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldstr      ""RHS""
    IL_001b:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0025:  stloc.2
    IL_0026:  ldloca.s   V_2
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002d:  brtrue.s   IL_006b
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0038:  ldarg.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004d:  leave.s    IL_00af
    IL_004f:  ldarg.0
    IL_0050:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0055:  stloc.2
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006b:  ldloca.s   V_2
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0079:  ldc.i4.0
    IL_007a:  ldloc.1
    IL_007b:  stelem.i4
    IL_007c:  ldarg.0
    IL_007d:  ldnull
    IL_007e:  stfld      ""int[] Program.<Assign>d__0.<>7__wrap1""
    IL_0083:  leave.s    IL_009c
  }
  catch System.Exception
  {
    IL_0085:  stloc.3
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.s   -2
    IL_0089:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_008e:  ldarg.0
    IL_008f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0094:  ldloc.3
    IL_0095:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009a:  leave.s    IL_00af
  }
  IL_009c:  ldarg.0
  IL_009d:  ldc.i4.s   -2
  IL_009f:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a4:  ldarg.0
  IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00af:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x14 }
                    [Main]: Return value missing on the stack. { Offset = 0x1e }
                    [TestIndexerThrows]: Return value missing on the stack. { Offset = 0x35 }
                    [TestIndexerSucceeds]: Return value missing on the stack. { Offset = 0x52 }
                    [TestReassignsArrayAndIndexerDuringAwait]: Return value missing on the stack. { Offset = 0xbf }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<TestReassignsArrayAndIndexerDuringAwait>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x3f, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(int[])", """
                {
                  // Code size       21 (0x15)
                  .maxstack  3
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldstr      "RHS"
                  IL_0006:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0010:  stloc.0
                  IL_0011:  ldc.i4.0
                  IL_0012:  ldloc.0
                  IL_0013:  stelem.i4
                  IL_0014:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_StructFieldAccessOnStructFieldAccessOnClassField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.b.c.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            // Sacrificial read should ensure that `await` never happens
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A();
        Console.WriteLine(""Before Assignment a.b.c.x is: "" + a.b.c.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.b.c.x is: "" + a.b.c.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A();
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.b.c.x is: "" + aCopy.b.c.x);
        a.b.c.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.b.c.x is: "" + aCopy.b.c.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B b;
}

struct B
{
    public C c;
}

struct C
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNull
                Before Assignment a.b.c.x is: 0
                RHS
                After Assignment a.b.c.x is: 42
                ReassignADuringAssignment
                Before Assignment a is null == False
                Before Assignment aCopy.b.c.x is: 0
                RHS
                After Assignment a is null == True
                After Assignment aCopy.b.c.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      201 (0xc9)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_001c:  ldfld      ""B A.b""
    IL_0021:  pop
    IL_0022:  ldstr      ""RHS""
    IL_0027:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0031:  stloc.2
    IL_0032:  ldloca.s   V_2
    IL_0034:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0039:  brtrue.s   IL_0077
    IL_003b:  ldarg.0
    IL_003c:  ldc.i4.0
    IL_003d:  dup
    IL_003e:  stloc.0
    IL_003f:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0044:  ldarg.0
    IL_0045:  ldloc.2
    IL_0046:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0051:  ldloca.s   V_2
    IL_0053:  ldarg.0
    IL_0054:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0059:  leave.s    IL_00c8
    IL_005b:  ldarg.0
    IL_005c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  stloc.2
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0068:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.0
    IL_0072:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0085:  ldflda     ""B A.b""
    IL_008a:  ldflda     ""C B.c""
    IL_008f:  ldloc.1
    IL_0090:  stfld      ""int C.x""
    IL_0095:  ldarg.0
    IL_0096:  ldnull
    IL_0097:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009c:  leave.s    IL_00b5
  }
  catch System.Exception
  {
    IL_009e:  stloc.3
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.3
    IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b3:  leave.s    IL_00c8
  }
  IL_00b5:  ldarg.0
  IL_00b6:  ldc.i4.s   -2
  IL_00b8:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c8:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x28 }
                    [Main]: Return value missing on the stack. { Offset = 0x1e }
                    [TestAIsNull]: Return value missing on the stack. { Offset = 0x30 }
                    [TestAIsNotNull]: Return value missing on the stack. { Offset = 0x63 }
                    [ReassignADuringAssignment]: Return value missing on the stack. { Offset = 0xd8 }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignADuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x33, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       41 (0x29)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  ldfld      "B A.b"
                  IL_0007:  pop
                  IL_0008:  ldstr      "RHS"
                  IL_000d:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0012:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0017:  stloc.0
                  IL_0018:  ldflda     "B A.b"
                  IL_001d:  ldflda     "C B.c"
                  IL_0022:  ldloc.0
                  IL_0023:  stfld      "int C.x"
                  IL_0028:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_ClassPropertyAssignmentOnClassProperty()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            // Sacrificial read should ensure that `await` never happens
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A{ _b = new B() };
        Console.WriteLine(""Before Assignment a._b._x is: "" + a._b._x);
        await Assign(a);
        Console.WriteLine(""After Assignment a._b._x is: "" + a._b._x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A{ _b = new B() };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy._b._x is: "" + aCopy._b._x);
        a.b.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy._b._x is: "" + aCopy._b._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B _b;
    public B b { get { Console.WriteLine(""GetB""); return _b; } set { Console.WriteLine(""SetB""); _b = value; }}
}

class B
{
    public int _x;
    public int x { get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNull
                Before Assignment a._b._x is: 0
                GetB
                RHS
                SetX
                After Assignment a._b._x is: 42
                ReassignADuringAssignment
                Before Assignment a is null == False
                Before Assignment aCopy._b._x is: 0
                GetB
                RHS
                SetX
                After Assignment a is null == True
                After Assignment aCopy._b._x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      184 (0xb8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0054
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.b.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldstr      ""RHS""
    IL_0020:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0070
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_004a:  ldloca.s   V_2
    IL_004c:  ldarg.0
    IL_004d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0052:  leave.s    IL_00b7
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.m1
    IL_0069:  dup
    IL_006a:  stloc.0
    IL_006b:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0070:  ldloca.s   V_2
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stloc.1
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_007e:  ldloc.1
    IL_007f:  callvirt   ""void B.x.set""
    IL_0084:  ldarg.0
    IL_0085:  ldnull
    IL_0086:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008b:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008d:  stloc.3
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_009c:  ldloc.3
    IL_009d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a2:  leave.s    IL_00b7
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00b2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b7:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x1c }
                    [Main]: Return value missing on the stack. { Offset = 0x1e }
                    [TestAIsNull]: Return value missing on the stack. { Offset = 0x30 }
                    [TestAIsNotNull]: Return value missing on the stack. { Offset = 0x64 }
                    [ReassignADuringAssignment]: Return value missing on the stack. { Offset = 0xcd }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignADuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x33, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       29 (0x1d)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  callvirt   "B A.b.get"
                  IL_0006:  ldstr      "RHS"
                  IL_000b:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0010:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0015:  stloc.0
                  IL_0016:  ldloc.0
                  IL_0017:  callvirt   "void B.x.set"
                  IL_001c:  ret
                }
                """);
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_FieldAccessOnClass()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A();
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A();
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.x is: "" + aCopy.x);
        a.x = await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.x is: "" + aCopy.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNull
                Before Assignment
                RHS
                Caught NullReferenceException
                TestAIsNotNull
                Before Assignment a.x is: 0
                RHS
                After Assignment a.x is: 42
                ReassignADuringAssignment
                Before Assignment a is null == False
                Before Assignment aCopy.x is: 0
                RHS
                After Assignment a is null == True
                After Assignment aCopy.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      179 (0xb3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004f
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0016:  ldstr      ""RHS""
    IL_001b:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0025:  stloc.2
    IL_0026:  ldloca.s   V_2
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002d:  brtrue.s   IL_006b
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0038:  ldarg.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_004d:  leave.s    IL_00b2
    IL_004f:  ldarg.0
    IL_0050:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0055:  stloc.2
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_006b:  ldloca.s   V_2
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0079:  ldloc.1
    IL_007a:  stfld      ""int A.x""
    IL_007f:  ldarg.0
    IL_0080:  ldnull
    IL_0081:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0086:  leave.s    IL_009f
  }
  catch System.Exception
  {
    IL_0088:  stloc.3
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0097:  ldloc.3
    IL_0098:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009d:  leave.s    IL_00b2
  }
  IL_009f:  ldarg.0
  IL_00a0:  ldc.i4.s   -2
  IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00a7:  ldarg.0
  IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00ad:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b2:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x17 }
                    [Main]: Return value missing on the stack. { Offset = 0x1e }
                    [TestAIsNull]: Return value missing on the stack. { Offset = 0x30 }
                    [TestAIsNotNull]: Return value missing on the stack. { Offset = 0x4f }
                    [ReassignADuringAssignment]: Return value missing on the stack. { Offset = 0xb3 }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignADuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x33, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       24 (0x18)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldstr      "RHS"
                  IL_0006:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_000b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0010:  stloc.0
                  IL_0011:  ldloc.0
                  IL_0012:  stfld      "int A.x"
                  IL_0017:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_CompoundAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x += await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
        await ReassignXDuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A(){ x = 1 };
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        await Assign(a);
        Console.WriteLine(""After Assignment a.x is: "" + a.x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A(){ x = 1 };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy.x is: "" + aCopy.x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy.x is: "" + aCopy.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task ReassignXDuringAssignment()
    {
        Console.WriteLine(nameof(ReassignXDuringAssignment));

        var a = new A(){ x = 1 };
        Console.WriteLine(""Before Assignment a.x is: "" + a.x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a.x is: "" + a.x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a.x = 100;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNull
                Before Assignment a.x is: 1
                RHS
                After Assignment a.x is: 43
                ReassignADuringAssignment
                Before Assignment a is null == False
                Before Assignment aCopy.x is: 1
                RHS
                After Assignment a is null == True
                After Assignment aCopy.x is: 43
                ReassignXDuringAssignment
                Before Assignment a.x is: 1
                RHS
                After Assignment a.x is: 43
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                A V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldloc.1
    IL_001a:  ldfld      ""int A.x""
    IL_001f:  stfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_0024:  ldstr      ""RHS""
    IL_0029:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0033:  stloc.3
    IL_0034:  ldloca.s   V_3
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.3
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0053:  ldloca.s   V_3
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005b:  leave.s    IL_00c9
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0063:  stloc.3
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0080:  stloc.2
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_008d:  ldloc.2
    IL_008e:  add
    IL_008f:  stfld      ""int A.x""
    IL_0094:  ldarg.0
    IL_0095:  ldnull
    IL_0096:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009b:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x22 }
                    [Main]: Return value missing on the stack. { Offset = 0x28 }
                    [TestAIsNull]: Return value missing on the stack. { Offset = 0x30 }
                    [TestAIsNotNull]: Return value missing on the stack. { Offset = 0x56 }
                    [ReassignADuringAssignment]: Return value missing on the stack. { Offset = 0xc9 }
                    [ReassignXDuringAssignment]: Return value missing on the stack. { Offset = 0x88 }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignADuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x33, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignXDuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x39, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       35 (0x23)
                  .maxstack  3
                  .locals init (A V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  stloc.0
                  IL_0003:  ldfld      "int A.x"
                  IL_0008:  stloc.1
                  IL_0009:  ldstr      "RHS"
                  IL_000e:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0018:  stloc.2
                  IL_0019:  ldloc.0
                  IL_001a:  ldloc.1
                  IL_001b:  ldloc.2
                  IL_001c:  add
                  IL_001d:  stfld      "int A.x"
                  IL_0022:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void KeepLtrSemantics_CompoundAssignmentProperties()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a)
    {
        a.x += await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNull();
        await TestAIsNotNull();
        await ReassignADuringAssignment();
        await ReassignXDuringAssignment();
    }

    static async Task TestAIsNull()
    {
        Console.WriteLine(nameof(TestAIsNull));
        
        A a = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNotNull));

        var a = new A(){ _x = 1 };
        Console.WriteLine(""Before Assignment a._x is: "" + a._x);
        await Assign(a);
        Console.WriteLine(""After Assignment a._x is: "" + a._x);
    }

    static async Task ReassignADuringAssignment()
    {
        Console.WriteLine(nameof(ReassignADuringAssignment));

        var a = new A(){ _x = 1 };
        var aCopy = a;
        Console.WriteLine(""Before Assignment a is null == "" + (a is null));
        Console.WriteLine(""Before Assignment aCopy._x is: "" + aCopy._x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a is null == "" + (a is null));
        Console.WriteLine(""After Assignment aCopy._x is: "" + aCopy._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a = null;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task ReassignXDuringAssignment()
    {
        Console.WriteLine(nameof(ReassignXDuringAssignment));

        var a = new A(){ _x = 1 };
        Console.WriteLine(""Before Assignment a._x is: "" + a._x);
        a.x += await WriteAndReassign(""RHS"");
        Console.WriteLine(""After Assignment a._x is: "" + a._x);

        async Task<int> WriteAndReassign(string s)
        {
            await Task.Yield();
            a._x = 100;
            Console.WriteLine(s);
            return 42;
        }
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public int _x;
    public int x { get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNull
                Before Assignment a._x is: 1
                GetX
                RHS
                SetX
                After Assignment a._x is: 43
                ReassignADuringAssignment
                Before Assignment a is null == False
                Before Assignment aCopy._x is: 1
                GetX
                RHS
                SetX
                After Assignment a is null == True
                After Assignment aCopy._x is: 43
                ReassignXDuringAssignment
                Before Assignment a._x is: 1
                GetX
                RHS
                SetX
                After Assignment a._x is: 43
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0,
                A V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005d
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldloc.1
    IL_001a:  callvirt   ""int A.x.get""
    IL_001f:  stfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_0024:  ldstr      ""RHS""
    IL_0029:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_002e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0033:  stloc.3
    IL_0034:  ldloca.s   V_3
    IL_0036:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003b:  brtrue.s   IL_0079
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.3
    IL_0048:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0053:  ldloca.s   V_3
    IL_0055:  ldarg.0
    IL_0056:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005b:  leave.s    IL_00c9
    IL_005d:  ldarg.0
    IL_005e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0063:  stloc.3
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0080:  stloc.2
    IL_0081:  ldarg.0
    IL_0082:  ldfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_0087:  ldarg.0
    IL_0088:  ldfld      ""int Program.<Assign>d__0.<>7__wrap2""
    IL_008d:  ldloc.2
    IL_008e:  add
    IL_008f:  callvirt   ""void A.x.set""
    IL_0094:  ldarg.0
    IL_0095:  ldnull
    IL_0096:  stfld      ""A Program.<Assign>d__0.<>7__wrap1""
    IL_009b:  leave.s    IL_00b6
  }
  catch System.Exception
  {
    IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b4:  leave.s    IL_00c9
  }
  IL_00b6:  ldarg.0
  IL_00b7:  ldc.i4.s   -2
  IL_00b9:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00c4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c9:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x22 }
                    [Main]: Return value missing on the stack. { Offset = 0x28 }
                    [TestAIsNull]: Return value missing on the stack. { Offset = 0x30 }
                    [TestAIsNotNull]: Return value missing on the stack. { Offset = 0x56 }
                    [ReassignADuringAssignment]: Return value missing on the stack. { Offset = 0xc9 }
                    [ReassignXDuringAssignment]: Return value missing on the stack. { Offset = 0x88 }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignADuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x33, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [<ReassignXDuringAssignment>g__WriteAndReassign|0]: Unexpected type on the stack. { Offset = 0x39, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A)", """
                {
                  // Code size       35 (0x23)
                  .maxstack  3
                  .locals init (A V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  dup
                  IL_0002:  stloc.0
                  IL_0003:  callvirt   "int A.x.get"
                  IL_0008:  stloc.1
                  IL_0009:  ldstr      "RHS"
                  IL_000e:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0013:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0018:  stloc.2
                  IL_0019:  ldloc.0
                  IL_001a:  ldloc.1
                  IL_001b:  ldloc.2
                  IL_001c:  add
                  IL_001d:  callvirt   "void A.x.set"
                  IL_0022:  ret
                }
                """);
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_AssignmentToAssignment()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a, B b)
    {
        a.b.x = b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNullBIsNull();
        await TestAIsNullBIsNotNull();
        await TestAIsNotNullBIsNull();
        await TestADotBIsNullBIsNotNull();
        await TestADotBIsNotNullBIsNotNull();
    }

    static async Task TestAIsNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNull));
        
        A a = null;
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNotNull));
        
        A a = null;
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNotNullBIsNull));
        
        A a = new A{ b = new B() };
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNullBIsNotNull));
        
        A a = new A();
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNotNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNotNullBIsNotNull));

        A a = new A{ b = new B() };
        B b = new B();
        Console.WriteLine(""Before Assignment a.b.x is: "" + a.b.x);
        Console.WriteLine(""Before Assignment b.x is: "" + b.x);
        await Assign(a, b);
        Console.WriteLine(""After Assignment a.b.x is: "" + a.b.x);
        Console.WriteLine(""After Assignment b.x is: "" + b.x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B b;
}

class B
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNullBIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNullBIsNotNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNullBIsNull
                Before Assignment
                RHS
                Caught NullReferenceException
                TestADotBIsNullBIsNotNull
                Before Assignment
                RHS
                Caught NullReferenceException
                TestADotBIsNotNullBIsNotNull
                Before Assignment a.b.x is: 0
                Before Assignment b.x is: 0
                RHS
                After Assignment a.b.x is: 42
                After Assignment b.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      219 (0xdb)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  ldfld      ""B A.b""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""B Program.<Assign>d__0.b""
    IL_0022:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0027:  ldstr      ""RHS""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.2
    IL_0037:  ldloca.s   V_2
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.2
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0056:  ldloca.s   V_2
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005e:  leave.s    IL_00da
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0090:  ldloc.1
    IL_0091:  dup
    IL_0092:  stloc.3
    IL_0093:  stfld      ""int B.x""
    IL_0098:  ldloc.3
    IL_0099:  stfld      ""int B.x""
    IL_009e:  ldarg.0
    IL_009f:  ldnull
    IL_00a0:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_00a5:  ldarg.0
    IL_00a6:  ldnull
    IL_00a7:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x27 }
                    [Main]: Return value missing on the stack. { Offset = 0x32 }
                    [TestAIsNullBIsNull]: Return value missing on the stack. { Offset = 0x33 }
                    [TestAIsNullBIsNotNull]: Return value missing on the stack. { Offset = 0x37 }
                    [TestAIsNotNullBIsNull]: Return value missing on the stack. { Offset = 0x42 }
                    [TestADotBIsNullBIsNotNull]: Return value missing on the stack. { Offset = 0x3b }
                    [TestADotBIsNotNullBIsNotNull]: Return value missing on the stack. { Offset = 0x9f }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A, B)", """
                {
                  // Code size       40 (0x28)
                  .maxstack  4
                  .locals init (B V_0,
                                int V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "B A.b"
                  IL_0006:  ldarg.1
                  IL_0007:  stloc.0
                  IL_0008:  ldstr      "RHS"
                  IL_000d:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0012:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0017:  stloc.1
                  IL_0018:  ldloc.0
                  IL_0019:  ldloc.1
                  IL_001a:  dup
                  IL_001b:  stloc.2
                  IL_001c:  stfld      "int B.x"
                  IL_0021:  ldloc.2
                  IL_0022:  stfld      "int B.x"
                  IL_0027:  ret
                }
                """);
        }

        [WorkItem(19609, "https://github.com/dotnet/roslyn/issues/19609")]
        [Fact]
        public void KeepLtrSemantics_AssignmentToAssignmentProperties()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign(A a, B b)
    {
        a.b.x = b.x = await Write(""RHS"");
    }

    static async Task Main(string[] args)
    {
        await TestAIsNullBIsNull();
        await TestAIsNullBIsNotNull();
        await TestAIsNotNullBIsNull();
        await TestADotBIsNullBIsNotNull();
        await TestADotBIsNotNullBIsNotNull();
    }

    static async Task TestAIsNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNull));
        
        A a = null;
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestAIsNullBIsNotNull));
        
        A a = null;
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestAIsNotNullBIsNull()
    {
        Console.WriteLine(nameof(TestAIsNotNullBIsNull));
        
        A a = new A{ _b = new B() };
        B b = null;
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNullBIsNotNull));
        
        A a = new A();
        B b = new B();
        Console.WriteLine(""Before Assignment"");
        try
        {
            await Assign(a, b);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(""Caught NullReferenceException"");
        }
    }

    static async Task TestADotBIsNotNullBIsNotNull()
    {
        Console.WriteLine(nameof(TestADotBIsNotNullBIsNotNull));

        A a = new A{ _b = new B() };
        B b = new B();
        Console.WriteLine(""Before Assignment a._b._x is: "" + a._b._x);
        Console.WriteLine(""Before Assignment b._x is: "" + b._x);
        await Assign(a, b);
        Console.WriteLine(""After Assignment a._b._x is: "" + a._b._x);
        Console.WriteLine(""After Assignment b._x is: "" + b._x);
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }
}

class A
{
    public B _b;
    public B b { get { Console.WriteLine(""GetB""); return _b; } set { Console.WriteLine(""SetB""); _b = value; }}
}

class B
{
    public int _x;
    public int x {  get { Console.WriteLine(""GetX""); return _x; } set { Console.WriteLine(""SetX""); _x = value; } }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                TestAIsNullBIsNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNullBIsNotNull
                Before Assignment
                Caught NullReferenceException
                TestAIsNotNullBIsNull
                Before Assignment
                GetB
                RHS
                Caught NullReferenceException
                TestADotBIsNullBIsNotNull
                Before Assignment
                GetB
                RHS
                SetX
                Caught NullReferenceException
                TestADotBIsNotNullBIsNotNull
                Before Assignment a._b._x is: 0
                Before Assignment b._x is: 0
                GetB
                RHS
                SetX
                SetX
                After Assignment a._b._x is: 42
                After Assignment b._x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      219 (0xdb)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""A Program.<Assign>d__0.a""
    IL_0011:  callvirt   ""B A.b.get""
    IL_0016:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""B Program.<Assign>d__0.b""
    IL_0022:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_0027:  ldstr      ""RHS""
    IL_002c:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0031:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0036:  stloc.3
    IL_0037:  ldloca.s   V_3
    IL_0039:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldloc.3
    IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0056:  ldloca.s   V_3
    IL_0058:  ldarg.0
    IL_0059:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_005e:  leave.s    IL_00da
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0066:  stloc.3
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007c:  ldloca.s   V_3
    IL_007e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0083:  stloc.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_008a:  ldloc.1
    IL_008b:  dup
    IL_008c:  stloc.2
    IL_008d:  callvirt   ""void B.x.set""
    IL_0092:  ldarg.0
    IL_0093:  ldfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_0098:  ldloc.2
    IL_0099:  callvirt   ""void B.x.set""
    IL_009e:  ldarg.0
    IL_009f:  ldnull
    IL_00a0:  stfld      ""B Program.<Assign>d__0.<>7__wrap1""
    IL_00a5:  ldarg.0
    IL_00a6:  ldnull
    IL_00a7:  stfld      ""B Program.<Assign>d__0.<>7__wrap2""
    IL_00ac:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c5:  leave.s    IL_00da
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_00cf:  ldarg.0
  IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00da:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x25 }
                    [Main]: Return value missing on the stack. { Offset = 0x32 }
                    [TestAIsNullBIsNull]: Return value missing on the stack. { Offset = 0x33 }
                    [TestAIsNullBIsNotNull]: Return value missing on the stack. { Offset = 0x37 }
                    [TestAIsNotNullBIsNull]: Return value missing on the stack. { Offset = 0x42 }
                    [TestADotBIsNullBIsNotNull]: Return value missing on the stack. { Offset = 0x3b }
                    [TestADotBIsNotNullBIsNotNull]: Return value missing on the stack. { Offset = 0x9f }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign(A, B)", """
                {
                  // Code size       38 (0x26)
                  .maxstack  4
                  .locals init (int V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  callvirt   "B A.b.get"
                  IL_0006:  ldarg.1
                  IL_0007:  ldstr      "RHS"
                  IL_000c:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_0011:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0016:  stloc.0
                  IL_0017:  ldloc.0
                  IL_0018:  dup
                  IL_0019:  stloc.1
                  IL_001a:  callvirt   "void B.x.set"
                  IL_001f:  ldloc.1
                  IL_0020:  callvirt   "void B.x.set"
                  IL_0025:  ret
                }
                """);
        }

        [Fact]
        [WorkItem(42755, "https://github.com/dotnet/roslyn/issues/42755")]
        public void AssignmentToFieldOfStaticFieldOfStruct()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Assign()
    {
        A.b.x = await Write(""RHS"");
    }

    static async Task<int> Write(string s)
    {
        await Task.Yield();
        Console.WriteLine(s);
        return 42;
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine(""Before Assignment A.b.x is: "" + A.b.x);
        await Assign();
        Console.WriteLine(""After Assignment A.b.x is: "" + A.b.x);
    }
}

struct A
{
    public static B b;
}

struct B
{
    public int x;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var expectedOutput = """
                Before Assignment A.b.x is: 0
                RHS
                After Assignment A.b.x is: 42
                """;
            CompileAndVerify(comp, expectedOutput: expectedOutput)
                .VerifyIL("Program.<Assign>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      159 (0x9f)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0043
    IL_000a:  ldstr      ""RHS""
    IL_000f:  call       ""System.Threading.Tasks.Task<int> Program.Write(string)""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_005f
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldarg.0
    IL_003c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Assign>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Assign>d__0)""
    IL_0041:  leave.s    IL_009e
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0049:  stloc.2
    IL_004a:  ldarg.0
    IL_004b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Assign>d__0.<>u__1""
    IL_0050:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0066:  stloc.1
    IL_0067:  ldsflda    ""B A.b""
    IL_006c:  ldloc.1
    IL_006d:  stfld      ""int B.x""
    IL_0072:  leave.s    IL_008b
  }
  catch System.Exception
  {
    IL_0074:  stloc.3
    IL_0075:  ldarg.0
    IL_0076:  ldc.i4.s   -2
    IL_0078:  stfld      ""int Program.<Assign>d__0.<>1__state""
    IL_007d:  ldarg.0
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
    IL_0083:  ldloc.3
    IL_0084:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0089:  leave.s    IL_009e
  }
  IL_008b:  ldarg.0
  IL_008c:  ldc.i4.s   -2
  IL_008e:  stfld      ""int Program.<Assign>d__0.<>1__state""
  IL_0093:  ldarg.0
  IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Assign>d__0.<>t__builder""
  IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_009e:  ret
}");

            comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [Assign]: Return value missing on the stack. { Offset = 0x1b }
                    [Write]: Unexpected type on the stack. { Offset = 0x2c, Found = Int32, Expected = ref '[System.Runtime]System.Threading.Tasks.Task`1<int32>' }
                    [Main]: Return value missing on the stack. { Offset = 0x46 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Assign()", """
                {
                  // Code size       28 (0x1c)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldstr      "RHS"
                  IL_0005:  call       "System.Threading.Tasks.Task<int> Program.Write(string)"
                  IL_000a:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000f:  stloc.0
                  IL_0010:  ldsflda    "B A.b"
                  IL_0015:  ldloc.0
                  IL_0016:  stfld      "int B.x"
                  IL_001b:  ret
                }
                """);
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignStaticStructField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public struct S1
{
    public int Field;
}

public class C
{
    public static S1 s1;
    static async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        await M(Task.FromResult(1));
        Console.Write(s1.Field);
    }
}";
            var expectedOutput = "1";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x12 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M(System.Threading.Tasks.Task<int>)", """
                {
                  // Code size       19 (0x13)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0006:  stloc.0
                  IL_0007:  ldsflda    "S1 C.s1"
                  IL_000c:  ldloc.0
                  IL_000d:  stfld      "int S1.Field"
                  IL_0012:  ret
                }
                """);
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignStaticStructField_ViaUsingStatic()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using static C;

public struct S1
{
    public int Field;
}

public class C
{
    public static S1 s1;
}

public class Program
{
    static async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        await M(Task.FromResult(1));
        Console.Write(s1.Field);
    }
}
";
            var expectedOutput = "1";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x12 }
                    [Main]: Return value missing on the stack. { Offset = 0x1f }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M(System.Threading.Tasks.Task<int>)", """
                {
                  // Code size       19 (0x13)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0006:  stloc.0
                  IL_0007:  ldsflda    "S1 C.s1"
                  IL_000c:  ldloc.0
                  IL_000d:  stfld      "int S1.Field"
                  IL_0012:  ret
                }
                """);
        }

        [Fact, WorkItem(47191, "https://github.com/dotnet/roslyn/issues/47191")]
        public void AssignInstanceStructField()
        {
            var source = @"
using System;
using System.Threading.Tasks;

public struct S1
{
    public int Field;
}

public class C
{
    public S1 s1;
    async Task M(Task<int> t)
    {
        s1.Field = await t;
    }

    static async Task Main()
    {
        var c = new C();
        await c.M(Task.FromResult(1));
        Console.Write(c.s1.Field);
    }
}";
            var expectedOutput = "1";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Fails with
            {
                ILVerifyMessage = """
                    [M]: Return value missing on the stack. { Offset = 0x1a }
                    [Main]: Return value missing on the stack. { Offset = 0x25 }
                    """
            });

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M(System.Threading.Tasks.Task<int>)", """
                {
                  // Code size       27 (0x1b)
                  .maxstack  2
                  .locals init (int V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "S1 C.s1"
                  IL_0006:  pop
                  IL_0007:  ldarg.1
                  IL_0008:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_000d:  stloc.0
                  IL_000e:  ldarg.0
                  IL_000f:  ldflda     "S1 C.s1"
                  IL_0014:  ldloc.0
                  IL_0015:  stfld      "int S1.Field"
                  IL_001a:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81212")]
        public void CompoundRightShiftWithAwait()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                public struct S0
                {
                    public long F2;
                    public S0(long f2) : this()
                    {
                        F2 = f2;
                    }

                    public async Task M0_ThisRef()
                    {
                        Console.Write(this.F2);
                        this.F2 >>= await Program.M1();
                        Console.Write(this.F2);
                    }
                }

                public class Program
                {
                    private S0 _s0 = new S0(1);

                    public static async Task Main()
                    {
                        await M0_Local();
                        Console.WriteLine();
                        var s0 = new S0(1);
                        Console.Write(s0.F2);
                        await M0_Param(s0);
                        Console.Write(s0.F2);
                        Console.WriteLine();
                        s0 = new S0(1);
                        Console.Write(s0.F2);
                        await s0.M0_ThisRef();
                        Console.Write(s0.F2);
                        Console.WriteLine();
                        var p = new Program();
                        Console.Write(p._s0.F2);
                        await p.M0_Field();
                        Console.Write(p._s0.F2);
                    }

                    public static async Task M0_Local()
                    {
                        S0 var0 = new S0(1);
                        Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        Console.Write(var0.F2);
                    }

                    public static async Task M0_Param(S0 var0)
                    {
                        Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        Console.Write(var0.F2);
                    }

                    public async Task M0_Field()
                    {
                        Console.Write(_s0.F2);
                        _s0.F2 >>= await M1();
                        Console.Write(_s0.F2);
                    }

                    public static async Task<int> M1()
                    {
                        return 1;
                    }
                }
                """;
            var expectedOutput = """
                10
                1101
                1101
                1100
                """;

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M0_Local()", """
                {
                  // Code size       64 (0x40)
                  .maxstack  4
                  .locals init (S0 V_0, //var0
                                long V_1,
                                int V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  ldc.i4.1
                  IL_0003:  conv.i8
                  IL_0004:  call       "S0..ctor(long)"
                  IL_0009:  ldloc.0
                  IL_000a:  ldfld      "long S0.F2"
                  IL_000f:  call       "void System.Console.Write(long)"
                  IL_0014:  ldloc.0
                  IL_0015:  ldfld      "long S0.F2"
                  IL_001a:  stloc.1
                  IL_001b:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0020:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0025:  stloc.2
                  IL_0026:  ldloca.s   V_0
                  IL_0028:  ldflda     "long S0.F2"
                  IL_002d:  ldloc.1
                  IL_002e:  ldloc.2
                  IL_002f:  ldc.i4.s   63
                  IL_0031:  and
                  IL_0032:  shr
                  IL_0033:  stind.i8
                  IL_0034:  ldloc.0
                  IL_0035:  ldfld      "long S0.F2"
                  IL_003a:  call       "void System.Console.Write(long)"
                  IL_003f:  ret
                }
                """);

            verifier.VerifyIL("Program.M0_Param(S0)", """
                {
                  // Code size       55 (0x37)
                  .maxstack  4
                  .locals init (long V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "long S0.F2"
                  IL_0006:  call       "void System.Console.Write(long)"
                  IL_000b:  ldarg.0
                  IL_000c:  ldfld      "long S0.F2"
                  IL_0011:  stloc.0
                  IL_0012:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0017:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_001c:  stloc.1
                  IL_001d:  ldarga.s   V_0
                  IL_001f:  ldflda     "long S0.F2"
                  IL_0024:  ldloc.0
                  IL_0025:  ldloc.1
                  IL_0026:  ldc.i4.s   63
                  IL_0028:  and
                  IL_0029:  shr
                  IL_002a:  stind.i8
                  IL_002b:  ldarg.0
                  IL_002c:  ldfld      "long S0.F2"
                  IL_0031:  call       "void System.Console.Write(long)"
                  IL_0036:  ret
                }
                """);

            verifier.VerifyIL("S0.M0_ThisRef()", """
                {
                  // Code size       61 (0x3d)
                  .maxstack  4
                  .locals init (S0 V_0,
                                long V_1,
                                int V_2)
                  IL_0000:  ldarg.0
                  IL_0001:  ldobj      "S0"
                  IL_0006:  stloc.0
                  IL_0007:  ldloc.0
                  IL_0008:  ldfld      "long S0.F2"
                  IL_000d:  call       "void System.Console.Write(long)"
                  IL_0012:  ldloc.0
                  IL_0013:  ldfld      "long S0.F2"
                  IL_0018:  stloc.1
                  IL_0019:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_001e:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0023:  stloc.2
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  ldloc.1
                  IL_0027:  ldloc.2
                  IL_0028:  ldc.i4.s   63
                  IL_002a:  and
                  IL_002b:  shr
                  IL_002c:  stfld      "long S0.F2"
                  IL_0031:  ldloc.0
                  IL_0032:  ldfld      "long S0.F2"
                  IL_0037:  call       "void System.Console.Write(long)"
                  IL_003c:  ret
                }
                """);

            verifier.VerifyIL("Program.M0_Field()", """
                {
                  // Code size       74 (0x4a)
                  .maxstack  4
                  .locals init (long V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "S0 Program._s0"
                  IL_0006:  ldfld      "long S0.F2"
                  IL_000b:  call       "void System.Console.Write(long)"
                  IL_0010:  ldarg.0
                  IL_0011:  ldflda     "S0 Program._s0"
                  IL_0016:  ldfld      "long S0.F2"
                  IL_001b:  stloc.0
                  IL_001c:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0021:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0026:  stloc.1
                  IL_0027:  ldarg.0
                  IL_0028:  ldflda     "S0 Program._s0"
                  IL_002d:  ldflda     "long S0.F2"
                  IL_0032:  ldloc.0
                  IL_0033:  ldloc.1
                  IL_0034:  ldc.i4.s   63
                  IL_0036:  and
                  IL_0037:  shr
                  IL_0038:  stind.i8
                  IL_0039:  ldarg.0
                  IL_003a:  ldflda     "S0 Program._s0"
                  IL_003f:  ldfld      "long S0.F2"
                  IL_0044:  call       "void System.Console.Write(long)"
                  IL_0049:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81212")]
        public void CompoundRightShiftWithAwait_TypeParameter_Struct()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                public interface IHasField
                {
                    long F2 { get; set; }
                }

                public struct S0 : IHasField
                {
                    public long F2 { get; set; }
                    public S0(long f2) : this()
                    {
                        F2 = f2;
                    }
                }

                public class FieldHolder<T>
                    where T : struct, IHasField
                {
                    public T _value;

                    public FieldHolder(T value)
                    {
                        _value = value;
                    }

                    public async Task M0_Field()
                    {
                        Console.Write(_value.F2);
                        _value.F2 >>= await Program.M1();
                        Console.Write(_value.F2);
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        await M0_Local<S0>();
                        Console.WriteLine();
                        var s0 = new S0(1);
                        Console.Write(s0.F2);
                        await M0_Param(s0);
                        Console.Write(s0.F2);
                        Console.WriteLine();
                        var f = new FieldHolder<S0>(new S0(1));
                        Console.Write(f._value.F2);
                        await f.M0_Field();
                        Console.Write(f._value.F2);
                    }

                    public static async Task M0_Local<T>() where T : struct, IHasField
                    {
                        T var0 = default;
                        var0.F2 = 1;
                        Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        Console.Write(var0.F2);
                    }

                    public static async Task M0_Param<T>(T var0) where T : struct, IHasField
                    {
                        Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        Console.Write(var0.F2);
                    }

                    public static async Task<int> M1()
                    {
                        return 1;
                    }
                }
                """;
            var expectedOutput = """
                10
                1101
                1100
                """;

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M0_Local<T>()", """
                {
                  // Code size      104 (0x68)
                  .maxstack  4
                  .locals init (T V_0, //var0
                                long V_1,
                                int V_2)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "T"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.1
                  IL_000b:  conv.i8
                  IL_000c:  constrained. "T"
                  IL_0012:  callvirt   "void IHasField.F2.set"
                  IL_0017:  ldloca.s   V_0
                  IL_0019:  constrained. "T"
                  IL_001f:  callvirt   "long IHasField.F2.get"
                  IL_0024:  call       "void System.Console.Write(long)"
                  IL_0029:  ldloca.s   V_0
                  IL_002b:  constrained. "T"
                  IL_0031:  callvirt   "long IHasField.F2.get"
                  IL_0036:  stloc.1
                  IL_0037:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_003c:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0041:  stloc.2
                  IL_0042:  ldloca.s   V_0
                  IL_0044:  ldloc.1
                  IL_0045:  ldloc.2
                  IL_0046:  ldc.i4.s   63
                  IL_0048:  and
                  IL_0049:  shr
                  IL_004a:  constrained. "T"
                  IL_0050:  callvirt   "void IHasField.F2.set"
                  IL_0055:  ldloca.s   V_0
                  IL_0057:  constrained. "T"
                  IL_005d:  callvirt   "long IHasField.F2.get"
                  IL_0062:  call       "void System.Console.Write(long)"
                  IL_0067:  ret
                }
                """);

            verifier.VerifyIL("Program.M0_Param<T>(T)", """
                {
                  // Code size       81 (0x51)
                  .maxstack  4
                  .locals init (long V_0,
                                int V_1)
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  constrained. "T"
                  IL_0008:  callvirt   "long IHasField.F2.get"
                  IL_000d:  call       "void System.Console.Write(long)"
                  IL_0012:  ldarga.s   V_0
                  IL_0014:  constrained. "T"
                  IL_001a:  callvirt   "long IHasField.F2.get"
                  IL_001f:  stloc.0
                  IL_0020:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0025:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_002a:  stloc.1
                  IL_002b:  ldarga.s   V_0
                  IL_002d:  ldloc.0
                  IL_002e:  ldloc.1
                  IL_002f:  ldc.i4.s   63
                  IL_0031:  and
                  IL_0032:  shr
                  IL_0033:  constrained. "T"
                  IL_0039:  callvirt   "void IHasField.F2.set"
                  IL_003e:  ldarga.s   V_0
                  IL_0040:  constrained. "T"
                  IL_0046:  callvirt   "long IHasField.F2.get"
                  IL_004b:  call       "void System.Console.Write(long)"
                  IL_0050:  ret
                }
                """);

            verifier.VerifyIL("FieldHolder<T>.M0_Field()", """
                {
                  // Code size       97 (0x61)
                  .maxstack  4
                  .locals init (long V_0,
                                int V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "T FieldHolder<T>._value"
                  IL_0006:  constrained. "T"
                  IL_000c:  callvirt   "long IHasField.F2.get"
                  IL_0011:  call       "void System.Console.Write(long)"
                  IL_0016:  ldarg.0
                  IL_0017:  ldflda     "T FieldHolder<T>._value"
                  IL_001c:  constrained. "T"
                  IL_0022:  callvirt   "long IHasField.F2.get"
                  IL_0027:  stloc.0
                  IL_0028:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_002d:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0032:  stloc.1
                  IL_0033:  ldarg.0
                  IL_0034:  ldflda     "T FieldHolder<T>._value"
                  IL_0039:  ldloc.0
                  IL_003a:  ldloc.1
                  IL_003b:  ldc.i4.s   63
                  IL_003d:  and
                  IL_003e:  shr
                  IL_003f:  constrained. "T"
                  IL_0045:  callvirt   "void IHasField.F2.set"
                  IL_004a:  ldarg.0
                  IL_004b:  ldflda     "T FieldHolder<T>._value"
                  IL_0050:  constrained. "T"
                  IL_0056:  callvirt   "long IHasField.F2.get"
                  IL_005b:  call       "void System.Console.Write(long)"
                  IL_0060:  ret
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81212")]
        public void CompoundRightShiftWithAwait_TypeParameter_Unconstrained()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                public interface IHasField
                {
                    long F2 { get; set; }
                }

                public struct S0 : IHasField
                {
                    public long F2 { get; set; }
                    public S0(long f2) : this()
                    {
                        F2 = f2;
                    }
                }

                public class FieldHolder<T>
                    where T : IHasField
                {
                    public T _value;

                    public FieldHolder(T value)
                    {
                        _value = value;
                    }

                    public async Task M0_Field()
                    {
                        System.Console.Write(_value.F2);
                        _value.F2 >>= await Program.M1();
                        System.Console.Write(_value.F2);
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        await M0_Local<S0>();
                        Console.WriteLine();
                        var s0 = new S0(1);
                        Console.Write(s0.F2);
                        await M0_Param(s0);
                        Console.Write(s0.F2);
                        Console.WriteLine();
                        var f = new FieldHolder<S0>(new S0(1));
                        Console.Write(f._value.F2);
                        await f.M0_Field();
                        Console.Write(f._value.F2);
                    }

                    public static async Task M0_Local<T>() where T : IHasField, new()
                    {
                        T var0 = new();
                        var0.F2 = 1;
                        System.Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        System.Console.Write(var0.F2);
                    }

                    public static async Task M0_Param<T>(T var0) where T : IHasField
                    {
                        System.Console.Write(var0.F2);
                        var0.F2 >>= await M1();
                        System.Console.Write(var0.F2);
                    }

                    public static async Task<int> M1()
                    {
                        return 1;
                    }
                }
                """;
            var expectedOutput = """
                10
                1101
                1100
                """;

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);
            verifier.VerifyDiagnostics();

            var comp = CreateRuntimeAsyncCompilation(source, TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M0_Local<T>()", """
                {
                  // Code size      163 (0xa3)
                  .maxstack  4
                  .locals init (T V_0, //var0
                                T V_1,
                                long V_2,
                                int V_3,
                                T V_4)
                  IL_0000:  call       "T System.Activator.CreateInstance<T>()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  ldc.i4.1
                  IL_0009:  conv.i8
                  IL_000a:  constrained. "T"
                  IL_0010:  callvirt   "void IHasField.F2.set"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  constrained. "T"
                  IL_001d:  callvirt   "long IHasField.F2.get"
                  IL_0022:  call       "void System.Console.Write(long)"
                  IL_0027:  ldloca.s   V_4
                  IL_0029:  initobj    "T"
                  IL_002f:  ldloc.s    V_4
                  IL_0031:  box        "T"
                  IL_0036:  brtrue.s   IL_003a
                  IL_0038:  ldloc.0
                  IL_0039:  stloc.1
                  IL_003a:  ldloca.s   V_4
                  IL_003c:  initobj    "T"
                  IL_0042:  ldloc.s    V_4
                  IL_0044:  box        "T"
                  IL_0049:  brtrue.s   IL_004f
                  IL_004b:  ldloca.s   V_1
                  IL_004d:  br.s       IL_0051
                  IL_004f:  ldloca.s   V_0
                  IL_0051:  constrained. "T"
                  IL_0057:  callvirt   "long IHasField.F2.get"
                  IL_005c:  stloc.2
                  IL_005d:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0062:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0067:  stloc.3
                  IL_0068:  ldloca.s   V_4
                  IL_006a:  initobj    "T"
                  IL_0070:  ldloc.s    V_4
                  IL_0072:  box        "T"
                  IL_0077:  brtrue.s   IL_007d
                  IL_0079:  ldloca.s   V_1
                  IL_007b:  br.s       IL_007f
                  IL_007d:  ldloca.s   V_0
                  IL_007f:  ldloc.2
                  IL_0080:  ldloc.3
                  IL_0081:  ldc.i4.s   63
                  IL_0083:  and
                  IL_0084:  shr
                  IL_0085:  constrained. "T"
                  IL_008b:  callvirt   "void IHasField.F2.set"
                  IL_0090:  ldloca.s   V_0
                  IL_0092:  constrained. "T"
                  IL_0098:  callvirt   "long IHasField.F2.get"
                  IL_009d:  call       "void System.Console.Write(long)"
                  IL_00a2:  ret
                }
                """);

            verifier.VerifyIL("Program.M0_Param<T>(T)", """
                {
                  // Code size      139 (0x8b)
                  .maxstack  4
                  .locals init (T V_0,
                                long V_1,
                                int V_2,
                                T V_3)
                  IL_0000:  ldarga.s   V_0
                  IL_0002:  constrained. "T"
                  IL_0008:  callvirt   "long IHasField.F2.get"
                  IL_000d:  call       "void System.Console.Write(long)"
                  IL_0012:  ldloca.s   V_3
                  IL_0014:  initobj    "T"
                  IL_001a:  ldloc.3
                  IL_001b:  box        "T"
                  IL_0020:  brtrue.s   IL_0024
                  IL_0022:  ldarg.0
                  IL_0023:  stloc.0
                  IL_0024:  ldloca.s   V_3
                  IL_0026:  initobj    "T"
                  IL_002c:  ldloc.3
                  IL_002d:  box        "T"
                  IL_0032:  brtrue.s   IL_0038
                  IL_0034:  ldloca.s   V_0
                  IL_0036:  br.s       IL_003a
                  IL_0038:  ldarga.s   V_0
                  IL_003a:  constrained. "T"
                  IL_0040:  callvirt   "long IHasField.F2.get"
                  IL_0045:  stloc.1
                  IL_0046:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_004b:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0050:  stloc.2
                  IL_0051:  ldloca.s   V_3
                  IL_0053:  initobj    "T"
                  IL_0059:  ldloc.3
                  IL_005a:  box        "T"
                  IL_005f:  brtrue.s   IL_0065
                  IL_0061:  ldloca.s   V_0
                  IL_0063:  br.s       IL_0067
                  IL_0065:  ldarga.s   V_0
                  IL_0067:  ldloc.1
                  IL_0068:  ldloc.2
                  IL_0069:  ldc.i4.s   63
                  IL_006b:  and
                  IL_006c:  shr
                  IL_006d:  constrained. "T"
                  IL_0073:  callvirt   "void IHasField.F2.set"
                  IL_0078:  ldarga.s   V_0
                  IL_007a:  constrained. "T"
                  IL_0080:  callvirt   "long IHasField.F2.get"
                  IL_0085:  call       "void System.Console.Write(long)"
                  IL_008a:  ret
                }
                """);

            verifier.VerifyIL("FieldHolder<T>.M0_Field()", """
                {
                  // Code size      169 (0xa9)
                  .maxstack  4
                  .locals init (T V_0,
                                long V_1,
                                int V_2,
                                T V_3)
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "T FieldHolder<T>._value"
                  IL_0006:  constrained. "T"
                  IL_000c:  callvirt   "long IHasField.F2.get"
                  IL_0011:  call       "void System.Console.Write(long)"
                  IL_0016:  ldloca.s   V_3
                  IL_0018:  initobj    "T"
                  IL_001e:  ldloc.3
                  IL_001f:  box        "T"
                  IL_0024:  brtrue.s   IL_002f
                  IL_0026:  ldarg.0
                  IL_0027:  ldfld      "T FieldHolder<T>._value"
                  IL_002c:  stloc.0
                  IL_002d:  br.s       IL_0036
                  IL_002f:  ldarg.0
                  IL_0030:  ldfld      "T FieldHolder<T>._value"
                  IL_0035:  pop
                  IL_0036:  ldloca.s   V_3
                  IL_0038:  initobj    "T"
                  IL_003e:  ldloc.3
                  IL_003f:  box        "T"
                  IL_0044:  brtrue.s   IL_004a
                  IL_0046:  ldloca.s   V_0
                  IL_0048:  br.s       IL_0050
                  IL_004a:  ldarg.0
                  IL_004b:  ldflda     "T FieldHolder<T>._value"
                  IL_0050:  constrained. "T"
                  IL_0056:  callvirt   "long IHasField.F2.get"
                  IL_005b:  stloc.1
                  IL_005c:  call       "System.Threading.Tasks.Task<int> Program.M1()"
                  IL_0061:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
                  IL_0066:  stloc.2
                  IL_0067:  ldloca.s   V_3
                  IL_0069:  initobj    "T"
                  IL_006f:  ldloc.3
                  IL_0070:  box        "T"
                  IL_0075:  brtrue.s   IL_007b
                  IL_0077:  ldloca.s   V_0
                  IL_0079:  br.s       IL_0081
                  IL_007b:  ldarg.0
                  IL_007c:  ldflda     "T FieldHolder<T>._value"
                  IL_0081:  ldloc.1
                  IL_0082:  ldloc.2
                  IL_0083:  ldc.i4.s   63
                  IL_0085:  and
                  IL_0086:  shr
                  IL_0087:  constrained. "T"
                  IL_008d:  callvirt   "void IHasField.F2.set"
                  IL_0092:  ldarg.0
                  IL_0093:  ldflda     "T FieldHolder<T>._value"
                  IL_0098:  constrained. "T"
                  IL_009e:  callvirt   "long IHasField.F2.get"
                  IL_00a3:  call       "void System.Console.Write(long)"
                  IL_00a8:  ret
                }
                """);
        }
    }
}
