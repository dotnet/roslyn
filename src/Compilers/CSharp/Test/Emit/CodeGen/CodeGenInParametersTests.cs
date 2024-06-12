// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class CodeGenInParametersTests : CompilingTestBase
    {
        [Fact]
        public void ThreeParamReorder()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public struct S
    {
        public int X;
    }

    private static S _field;

    public static ref S GetField(int order)
    {
        Console.WriteLine(""GetField "" + _field.X++ + "" "" + order);
        return ref _field;
    }

    public static void Main()
    {
        M(y: in GetField(0).X, z: GetField(1).X, x: GetField(2).X);
    }

    static void M(in int x, in int y, in int z)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
    }
}", expectedOutput: @"GetField 0 0
GetField 1 1
GetField 2 2
3
3
3");
            comp.VerifyIL("C.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (int& V_0,
                int& V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""ref C.S C.GetField(int)""
  IL_0006:  ldflda     ""int C.S.X""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""ref C.S C.GetField(int)""
  IL_0012:  ldflda     ""int C.S.X""
  IL_0017:  stloc.1
  IL_0018:  ldc.i4.2
  IL_0019:  call       ""ref C.S C.GetField(int)""
  IL_001e:  ldflda     ""int C.S.X""
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  call       ""void C.M(in int, in int, in int)""
  IL_002a:  ret
}");

        }

        [Fact]
        public void InParamReadonlyFieldReorder()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    private static readonly int _f = 0;
    public C()
    {
        M(y: _f, x: _f + 1);
        M(y: in _f, x: _f + 1);
    }

    public static void Main()
    {
        M(y: _f, x: _f + 1);
        M(y: in _f, x: _f + 1);
        _ = new C();
    }

    static void M(in int x, in int y)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"1
0
1
0
1
0
1
0", verify: Verification.Fails);
            comp.VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (int& V_0,
                int V_1)
  IL_0000:  ldsflda    ""int C._f""
  IL_0005:  stloc.0
  IL_0006:  ldsfld     ""int C._f""
  IL_000b:  ldc.i4.1
  IL_000c:  add
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldloc.0
  IL_0011:  call       ""void C.M(in int, in int)""
  IL_0016:  ldsflda    ""int C._f""
  IL_001b:  stloc.0
  IL_001c:  ldsfld     ""int C._f""
  IL_0021:  ldc.i4.1
  IL_0022:  add
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  ldloc.0
  IL_0027:  call       ""void C.M(in int, in int)""
  IL_002c:  newobj     ""C..ctor()""
  IL_0031:  pop
  IL_0032:  ret
}");
        }

        [Fact]
        public void InParamCallOptionalArg()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        int x = 1;
        M(in x);
    }
    static void M(in int x, int y = 0)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"1
0");
            comp.VerifyIL("C.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.0
  IL_0005:  call       ""void C.M(in int, int)""
  IL_000a:  ret
}");
        }

        [Fact]
        public void InParamCtorOptionalArg()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        int x = 1;
        new C(in x);
    }

    public C(in int x, int y = 0)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"1
0");
            comp.VerifyIL("C.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.0
  IL_0005:  newobj     ""C..ctor(in int, int)""
  IL_000a:  pop
  IL_000b:  ret
}");
        }

        [Fact]
        public void InParamInitializerOptionalArg()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static int _x = 1;
    public int _f = M(in _x);

    public static void Main()
    {
        new C();
    }

    public static int M(in int x, int y = 0)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
        return x;
    }
}", expectedOutput: @"1
0");
            comp.VerifyIL("C..ctor", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsflda    ""int C._x""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""int C.M(in int, int)""
  IL_000c:  stfld      ""int C._f""
  IL_0011:  ldarg.0
  IL_0012:  call       ""object..ctor()""
  IL_0017:  ret
}");

        }

        [Fact]
        public void InParamCollectionInitializerOptionalArg()
        {
            var comp = CompileAndVerify(@"
using System;
using System.Collections;
class C : IEnumerable
{
    public static void Main()
    {
        int x = 1;
        new C() { x };
    }

    public IEnumerator GetEnumerator() => null;

    public void Add(in int x, int y = 0)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}");
            comp.VerifyIL("C.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  newobj     ""C..ctor()""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.0
  IL_000a:  callvirt   ""void C.Add(in int, int)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void InParamSetter()
        {
            var comp = CompileAndVerify(@"
using System;
using System.Collections;
class C
{
    static int _f = 1;
    public static void Main()
    {
        new C()[in _f] = 0;
    }

    public IEnumerator GetEnumerator() => null;

    public int this[in int x, int y = 0]
    {
        get => x;
        set
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
            _f++;
            Console.WriteLine(x);
        }
    }
}", expectedOutput: @"1
0
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       18 (0x12)
  .maxstack  4
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldsflda    ""int C._f""
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.0
  IL_000c:  call       ""void C.this[in int, int].set""
  IL_0011:  ret
}");
        }

        [Fact]
        public void InParamParamsArg()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        int x = 1;
        M(in x);
    }
    public static void M(in int x, params int[] p)
    {
        Console.WriteLine(x);
        Console.WriteLine(p.Length);
    }
}", expectedOutput: @"1
0");
            comp.VerifyIL("C.Main", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""int[] System.Array.Empty<int>()""
  IL_0009:  call       ""void C.M(in int, params int[])""
  IL_000e:  ret
}");
        }

        [Fact]
        public void InParamReorder()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public struct S
    {
        public int X;
    }

    private static S _field;

    public static ref S GetField(int order)
    {
        Console.WriteLine(""GetField "" + _field.X++ + "" "" + order);
        return ref _field;
    }

    public static void Main()
    {
        M(y: in GetField(0).X, x: GetField(1).X);
    }

    static void M(in int x, in int y)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"GetField 0 0
GetField 1 1
2
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (int& V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""ref C.S C.GetField(int)""
  IL_0006:  ldflda     ""int C.S.X""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""ref C.S C.GetField(int)""
  IL_0012:  ldflda     ""int C.S.X""
  IL_0017:  ldloc.0
  IL_0018:  call       ""void C.M(in int, in int)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void InParamCtorReorder()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    public struct S
    {
        public int X;
    }

    private static S _field;

    public static ref S GetField(int order)
    {
        Console.WriteLine(""GetField "" + _field.X++ + "" "" + order);
        return ref _field;
    }

    public static void Main()
    {
        new C(y: in GetField(0).X, x: GetField(1).X);
    }

    public C(in int x, in int y)
    {
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"GetField 0 0
GetField 1 1
2
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (int& V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""ref C.S C.GetField(int)""
  IL_0006:  ldflda     ""int C.S.X""
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  call       ""ref C.S C.GetField(int)""
  IL_0012:  ldflda     ""int C.S.X""
  IL_0017:  ldloc.0
  IL_0018:  newobj     ""C..ctor(in int, in int)""
  IL_001d:  pop
  IL_001e:  ret
}");
        }

        [Fact]
        public void InIndexerReorder()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    public struct S
    {
        public int X;
    }

    private static S _field;

    public static ref S GetField(int order)
    {
        Console.WriteLine(""GetField "" + _field.X++ + "" "" + order);
        return ref _field;
    }

    public static void Main()
    {
        var c = new C();
        _ = c[y: in GetField(0).X, x: in GetField(1).X];
    }

    int this[in int x, in int y]
    {
        get
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
            return x;
        }
    }
}", expectedOutput: @"GetField 0 0
GetField 1 1
2
2");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (int& V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldc.i4.0
  IL_0006:  call       ""ref C.S C.GetField(int)""
  IL_000b:  ldflda     ""int C.S.X""
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.1
  IL_0012:  call       ""ref C.S C.GetField(int)""
  IL_0017:  ldflda     ""int C.S.X""
  IL_001c:  ldloc.0
  IL_001d:  callvirt   ""int C.this[in int, in int].get""
  IL_0022:  pop
  IL_0023:  ret
}");
        }

        [Fact]
        public void InIndexerReorderWithCopy()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    public struct S
    {
        public int X;
    }

    private static S _field;

    public static ref S GetField(int order)
    {
        Console.WriteLine(""GetField "" + _field.X++ + "" "" + order);
        return ref _field;
    }

    public static void Main()
    {
        var c = new C();
        _ = c[y: GetField(0).X, x: GetField(1).X + 1];
        _ = c[y: GetField(0).X + 2, x: GetField(1).X];
    }

    int this[in int x, in int y]
    {
        get
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
            return x;
        }
    }
}", expectedOutput: @"GetField 0 0
GetField 1 1
3
2
GetField 2 0
GetField 3 1
4
5");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       75 (0x4b)
  .maxstack  4
  .locals init (int& V_0,
                int V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref C.S C.GetField(int)""
  IL_000c:  ldflda     ""int C.S.X""
  IL_0011:  stloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  call       ""ref C.S C.GetField(int)""
  IL_0018:  ldfld      ""int C.S.X""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.0
  IL_0023:  callvirt   ""int C.this[in int, in int].get""
  IL_0028:  pop
  IL_0029:  ldc.i4.0
  IL_002a:  call       ""ref C.S C.GetField(int)""
  IL_002f:  ldfld      ""int C.S.X""
  IL_0034:  ldc.i4.2
  IL_0035:  add
  IL_0036:  stloc.1
  IL_0037:  ldc.i4.1
  IL_0038:  call       ""ref C.S C.GetField(int)""
  IL_003d:  ldflda     ""int C.S.X""
  IL_0042:  ldloca.s   V_1
  IL_0044:  callvirt   ""int C.this[in int, in int].get""
  IL_0049:  pop
  IL_004a:  ret
}");
        }

        [Fact]
        public void InParamSetterReorder()
        {
            var comp = CompileAndVerify(@"
using System;
using System.Collections;
class C
{
    static int _f = 1;
    public static void Main()
    {
        new C()[y: in _f, x: _f] = 0;
    }

    public IEnumerator GetEnumerator() => null;

    public int this[in int x, in int y]
    {
        get => x;
        set
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
            _f++;
            Console.WriteLine(x);
            Console.WriteLine(y);
        }
    }
}", expectedOutput: @"1
1
2
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       24 (0x18)
  .maxstack  4
  .locals init (int& V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldsflda    ""int C._f""
  IL_000a:  stloc.0
  IL_000b:  ldsflda    ""int C._f""
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  call       ""void C.this[in int, in int].set""
  IL_0017:  ret
}");
        }

        [Fact]
        public void InParamMemberInitializerReorder()
        {
            var comp = CompileAndVerify(@"
using System;
using System.Collections;
class C
{
    static int _f = 1;
    public static void Main()
    {
        new C() { [y: in _f, x: _f] = 0 };
    }

    public IEnumerator GetEnumerator() => null;

    public int this[in int x, in int y]
    {
        get => x;
        set
        {
            Console.WriteLine(x);
            Console.WriteLine(y);
            _f++;
            Console.WriteLine(x);
            Console.WriteLine(y);
        }
    }
}", expectedOutput: @"1
1
2
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  4
  .locals init (int& V_0,
                int& V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldsflda    ""int C._f""
  IL_000a:  stloc.0
  IL_000b:  ldsflda    ""int C._f""
  IL_0010:  stloc.1
  IL_0011:  ldloc.1
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  callvirt   ""void C.this[in int, in int].set""
  IL_0019:  ret
}");
        }

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails);

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"42
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
        System.Console.WriteLine(M(42));
        System.Console.WriteLine(M(42));
    }

    static ref readonly int M(in int x)
    {
        return ref x;
    }

    int this[in int x, in int y] => x + y;
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"42
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
  IL_0005:  call       ""ref readonly int Program.M(in int)""
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  newobj     ""Program..ctor()""
  IL_0015:  ldc.i4.5
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.6
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""int Program.this[in int, in int].get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ldc.i4.s   42
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  call       ""ref readonly int Program.M(in int)""
  IL_0031:  ldind.i4
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ldc.i4.s   42
  IL_0039:  stloc.0
  IL_003a:  ldloca.s   V_0
  IL_003c:  call       ""ref readonly int Program.M(in int)""
  IL_0041:  ldind.i4
  IL_0042:  call       ""void System.Console.WriteLine(int)""
  IL_0047:  ret
}");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: "42");

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

            comp = CompileAndVerify(text, verify: Verification.Fails, expectedOutput: "42", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            comp.VerifyIL("Program.Main()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldsfld     ""int Program.F""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""ref readonly int Program.M(in int)""
  IL_000d:  ldind.i4
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ret
}");

        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void InParamPassRoField1()
        {
            var text = @"
class Program
{
    public static readonly int F = 42;

    public static void Main()
    {
        System.Console.WriteLine(M(in F));
    }

    static ref readonly int M(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: "42");

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

            comp = CompileAndVerify(text, verify: Verification.Fails, expectedOutput: "42", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: "42");

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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails, expectedOutput: @"hi
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
    static ref readonly int M(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails);

            comp.VerifyIL("Program.M(in int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void InParamCannotAssign()
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
                // (6,9): error CS8331: Cannot assign to variable 'arg1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg1 = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(6, 9),
                // (7,9): error CS8332: Cannot assign to a member of variable 'arg2' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg2.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(7, 9),
                // (9,9): error CS8331: Cannot assign to variable 'arg1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg1 ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(9, 9),
                // (10,9): error CS8332: Cannot assign to a member of variable 'arg2' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg2.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(10, 9),
                // (12,9): error CS8331: Cannot assign to variable 'arg1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg1 += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(12, 9),
                // (13,9): error CS8332: Cannot assign to a member of variable 'arg2' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         arg2.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(13, 9));
        }

        [Fact]
        public void InParamCannotRefReturn()
        {
            var text = @"
class Program
{
    static ref readonly int M1_baseline(in int arg1, in (int Alice, int Bob) arg2)
    {
        // valid
        return ref arg1;
    }

    static ref readonly int M2_baseline(in int arg1, in (int Alice, int Bob) arg2)
    {
        // valid
        return ref arg2.Alice;
    }

    static ref int M1(in int arg1, in (int Alice, int Bob) arg2)
    {
        return ref arg1;
    }

    static ref int M2(in int arg1, in (int Alice, int Bob) arg2)
    {
        return ref arg2.Alice;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (18,20): error CS8333: Cannot return variable 'arg1' by writable reference because it is a readonly variable
                //         return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(18, 20),
                // (23,20): error CS8334: Members of variable 'arg2' cannot be returned by writable reference because it is a readonly variable
                //         return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(23, 20)
            );
        }

        [Fact]
        public void InParamCannotAssignByref()
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
                // (6,25): error CS8329: Cannot use variable 'arg1' as a ref or out value because it is a readonly variable
                //         ref var y = ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(6, 25),
                // (7,25): error CS8330: Members of variable 'arg2' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a = ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(7, 25));
        }

        [WorkItem(22306, "https://github.com/dotnet/roslyn/issues/22306")]
        [Fact]
        public void InParamCannotTakePtr()
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
                // (6,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* a = & arg1;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& arg1").WithLocation(6, 18),
                // (7,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* b = & arg2.Alice;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "& arg2.Alice").WithLocation(7, 18)
            );
        }

        [Fact]
        public void InParamCannotReturnByOrdinaryRef()
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
                // (10,24): error CS8333: Cannot return variable 'arg1' by writable reference because it is a readonly variable
                //             return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg1").WithArguments("variable", "arg1").WithLocation(10, 24),
                // (14,24): error CS8334: Members of variable 'arg2' cannot be returned by writable reference because it is a readonly variable
                //             return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg2.Alice").WithArguments("variable", "arg2").WithLocation(14, 24)
            );
        }

        [Fact]
        public void InParamCanReturnByRefReadonly()
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails);

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
        public void InParamCanReturnByRefReadonlyNested()
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Fails);

            comp.VerifyIL("Program.<M>g__M1|0_0(in int, in System.ValueTuple<int, int>)", @"
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
        public void InParamCannotReturnByRefNested()
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
                // (12,28): error CS8333: Cannot return variable 'arg11' by writable reference because it is a readonly variable
                //                 return ref arg11;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "arg11").WithArguments("variable", "arg11").WithLocation(12, 28),
                // (16,28): error CS8334: Members of variable 'arg21' cannot be returned by writable reference because it is a readonly variable
                //                 return ref arg21.Alice;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField2, "arg21.Alice").WithArguments("variable", "arg21").WithLocation(16, 28)
                );
        }

        [Fact]
        public void InParamOptional()
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"42");

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
        public void InParamConv()
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"42");

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

        [Fact]
        public void InParamAsyncSpill1()
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

        public static void M1(in int arg1, in int arg2, in int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"6");
        }

        [Fact]
        public void ReadonlyParamAsyncSpillIn()
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
            int local = 1;
            M1(in RefReturning(ref local), await GetT(2), 3);
        }

        private static ref int RefReturning(ref int arg)
        {
            return ref arg;
        }    

        public static async Task<T> GetT<T>(T val)
        {
            await Task.Yield();
            return val;
        }

        public static void M1(in int arg1, in int arg2, in int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (14,19): error CS8178: A reference returned by a call to 'Program.RefReturning(ref int)' cannot be preserved across 'await' or 'yield' boundary.
                //             M1(in RefReturning(ref local), await GetT(2), 3);
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "RefReturning(ref local)").WithArguments("Program.RefReturning(ref int)").WithLocation(14, 19)
                );
        }

        [Fact]
        public void ReadonlyParamAsyncSpillIn2()
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
            int local = 1;
            M1(arg3: 3, arg1: RefReturning(ref local), arg2: await GetT(2));
        }

        private static ref int RefReturning(ref int arg)
        {
            return ref arg;
        }

        public static async Task<T> GetT<T>(T val)
        {
            await Task.Yield();
            return val;
        }

        public static void M1(in int arg1, in int arg2, in int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var verifier = CompileAndVerify(text, verify: Verification.Fails, expectedOutput: "6");
            verifier.VerifyIL("Program.<Test>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      180 (0xb4)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //local
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004f
    IL_000a:  ldc.i4.1
    IL_000b:  stloc.1
    IL_000c:  ldarg.0
    IL_000d:  ldloca.s   V_1
    IL_000f:  call       ""ref int Program.RefReturning(ref int)""
    IL_0014:  ldind.i4
    IL_0015:  stfld      ""int Program.<Test>d__1.<>7__wrap1""
    IL_001a:  ldc.i4.2
    IL_001b:  call       ""System.Threading.Tasks.Task<int> Program.GetT<int>(int)""
    IL_0020:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0025:  stloc.3
    IL_0026:  ldloca.s   V_3
    IL_0028:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002d:  brtrue.s   IL_006b
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_0038:  ldarg.0
    IL_0039:  ldloc.3
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__1.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
    IL_0045:  ldloca.s   V_3
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Test>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Test>d__1)""
    IL_004d:  leave.s    IL_00b3
    IL_004f:  ldarg.0
    IL_0050:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__1.<>u__1""
    IL_0055:  stloc.3
    IL_0056:  ldarg.0
    IL_0057:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__1.<>u__1""
    IL_005c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_006b:  ldloca.s   V_3
    IL_006d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0072:  stloc.2
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""int Program.<Test>d__1.<>7__wrap1""
    IL_0079:  ldloca.s   V_2
    IL_007b:  ldc.i4.3
    IL_007c:  stloc.s    V_4
    IL_007e:  ldloca.s   V_4
    IL_0080:  call       ""void Program.M1(in int, in int, in int)""
    IL_0085:  leave.s    IL_00a0
  }
  catch System.Exception
  {
    IL_0087:  stloc.s    V_5
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_0091:  ldarg.0
    IL_0092:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
    IL_0097:  ldloc.s    V_5
    IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009e:  leave.s    IL_00b3
  }
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  stfld      ""int Program.<Test>d__1.<>1__state""
  IL_00a8:  ldarg.0
  IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
  IL_00ae:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b3:  ret
}");
        }

        [Fact]
        public void ReadonlyParamAsyncSpillInRoField()
        {
            var text = @"
    using System.Threading.Tasks;

    class Program
    {
        public static readonly int F = 5;

        static void Main(string[] args)
        {
            Test().Wait();
        }

        public static async Task Test()
        {
            int local = 1;
            M1(in F, await GetT(2), 3);
        } 

        public static async Task<T> GetT<T>(T val)
        {
            await Task.Yield();
            MutateReadonlyField();
            return val;
        }

        private static unsafe void MutateReadonlyField()
        {
            fixed(int* ptr = &F)
            {
                *ptr = 42;
            }
        }

        public static void M1(in int arg1, in int arg2, in int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseExe);
            var result = CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"47");

            var expectedIL = @"
{
  // Code size      162 (0xa2)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003f
    IL_000a:  ldc.i4.2
    IL_000b:  call       ""System.Threading.Tasks.Task<int> Program.GetT<int>(int)""
    IL_0010:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0015:  stloc.2
    IL_0016:  ldloca.s   V_2
    IL_0018:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_001d:  brtrue.s   IL_005b
    IL_001f:  ldarg.0
    IL_0020:  ldc.i4.0
    IL_0021:  dup
    IL_0022:  stloc.0
    IL_0023:  stfld      ""int Program.<Test>d__2.<>1__state""
    IL_0028:  ldarg.0
    IL_0029:  ldloc.2
    IL_002a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__2.<>u__1""
    IL_002f:  ldarg.0
    IL_0030:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__2.<>t__builder""
    IL_0035:  ldloca.s   V_2
    IL_0037:  ldarg.0
    IL_0038:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<Test>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<Test>d__2)""
    IL_003d:  leave.s    IL_00a1
    IL_003f:  ldarg.0
    IL_0040:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__2.<>u__1""
    IL_0045:  stloc.2
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<Test>d__2.<>u__1""
    IL_004c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0052:  ldarg.0
    IL_0053:  ldc.i4.m1
    IL_0054:  dup
    IL_0055:  stloc.0
    IL_0056:  stfld      ""int Program.<Test>d__2.<>1__state""
    IL_005b:  ldloca.s   V_2
    IL_005d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0062:  stloc.1
    IL_0063:  ldsflda    ""int Program.F""
    IL_0068:  ldloca.s   V_1
    IL_006a:  ldc.i4.3
    IL_006b:  stloc.3
    IL_006c:  ldloca.s   V_3
    IL_006e:  call       ""void Program.M1(in int, in int, in int)""
    IL_0073:  leave.s    IL_008e
  }
  catch System.Exception
  {
    IL_0075:  stloc.s    V_4
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.s   -2
    IL_007a:  stfld      ""int Program.<Test>d__2.<>1__state""
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__2.<>t__builder""
    IL_0085:  ldloc.s    V_4
    IL_0087:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_008c:  leave.s    IL_00a1
  }
  IL_008e:  ldarg.0
  IL_008f:  ldc.i4.s   -2
  IL_0091:  stfld      ""int Program.<Test>d__2.<>1__state""
  IL_0096:  ldarg.0
  IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__2.<>t__builder""
  IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00a1:  ret
}
";

            result.VerifyIL("Program.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", expectedIL);

            comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());
            result = CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"47");

            result.VerifyIL("Program.<Test>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", expectedIL);

        }

        [Fact]
        public void InParamAsyncSpill2()
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

        public static void M1(in int arg1, in int arg2, in int arg3)
        {
            System.Console.WriteLine(arg1 + arg2 + arg3);
        }
    }

";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"6");
        }

        [WorkItem(20764, "https://github.com/dotnet/roslyn/issues/20764")]
        [Fact]
        public void InParamAsyncSpillMethods()
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

    public static void M1(in int arg1, in int arg2, in int arg3, in int arg4)
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
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
        public void InParamAsyncSpillMethodsWriteable()
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

    public static void M1(in int arg1, in int arg2, in int arg3, in int arg4)
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
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
        public void InParamAsyncSpillStructField()
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

    public static void M1(in int arg1, in int arg2, in int arg3, in int arg4)
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        public void InParamAsyncSpillClassField()
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

    public static void M1(in int arg1, in int arg2, in int arg3, in int arg4)
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        public void InParamAsyncSpillExtension()
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
    public static void M1(in this int arg1, in int arg2, in int arg3, in int arg4)
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
2
42
2
2");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.PEVerifyCompat)]
        public void InParamAsyncSpillReadOnlyStructThis()
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

    public void M1(in int arg2, in int arg3, in int arg4)
    {
        System.Console.WriteLine(this.f);
        System.Console.WriteLine(arg2);
        System.Console.WriteLine(arg3);
        System.Console.WriteLine(arg4);
    }
}
";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
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
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
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
        public void InParamAsyncSpillReadOnlyStructThis_NoValCapture()
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

    public void M1(in S1 arg2, in S1 arg3, in S1 arg4)
    {
        System.Console.WriteLine(this.f);
        System.Console.WriteLine(arg2.f);
        System.Console.WriteLine(arg3.f);
        System.Console.WriteLine(arg4.f);
    }
}
";

            var comp = CreateCompilationWithMscorlib46(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseExe);
            var v = CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: @"
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
    IL_0078:  call       ""void S1.M1(in S1, in S1, in S1)""
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
            v = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"
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
    IL_0083:  call       ""void S1.M1(in S1, in S1, in S1)""
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

        [ConditionalTheory(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10834"), CombinatorialData]
        public void InParamGenericReadonly([CombinatorialValues("in", "ref readonly")] string modifier)
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
        public abstract void M1<T>(" + modifier + @" T arg) where T : U, I1;
    }

    class D: C<S1>
    {
        public override void M1<T>(" + modifier + @" T arg)
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"0");

            comp.VerifyIL($"D.M1<T>({modifier} T)", @"
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

        [ConditionalTheory(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10834"), CombinatorialData]
        public void InParamGenericReadonlyROstruct([CombinatorialValues("in", "ref readonly")] string modifier)
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
        public abstract void M1<T>(" + modifier + @" T arg) where T : U, I1;
    }

    class D: C<S1>
    {
        public override void M1<T>(" + modifier + @" T arg)
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

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"");

            comp.VerifyIL($"D.M1<T>({modifier} T)", @"
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
        public void RefReadOnlyOptionalParameters()
        {
            CompileAndVerify(@"
using System;
class Program
{
    static void Print(in int p = 5)
    {
        Console.Write(p);
    }
    static void Main()
    {
        Print();
        Console.Write(""-"");
        Print(9);
    }
}", expectedOutput: "5-9");
        }

        [WorkItem(23338, "https://github.com/dotnet/roslyn/issues/23338")]
        [Theory, CombinatorialData]
        public void InParamsNullable([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            var text = @"

class Program
{
    static void Main(string[] args)
    {
        S1 s = new S1();
        s.val = 42;

        S1? ns = s;

        Test1(in ns);
        Test2(ref ns);
    }

    static void Test1(" + modifier + @" S1? arg)
    {
        // cannot not mutate
        System.Console.Write(arg.GetValueOrDefault());
        // should not mutate
        arg.ToString();
        // cannot not mutate
        System.Console.Write(arg.GetValueOrDefault());
    }

    static void Test2(ref S1? arg)
    {
        // cannot not mutate
        System.Console.Write(arg.GetValueOrDefault());
        // can mutate
        arg.ToString();
        // cannot not mutate
        System.Console.Write(arg.GetValueOrDefault());
    }

}

struct S1
{
    public int val;

    public override string ToString()
    {
        var result = val.ToString();
        val = 0;

        return result;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: Verification.Passes, expectedOutput: @"4242420");

            comp.VerifyIL($"Program.Test1({modifier} S1?)", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (S1? V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1 S1?.GetValueOrDefault()""
  IL_0006:  box        ""S1""
  IL_000b:  call       ""void System.Console.Write(object)""
  IL_0010:  ldarg.0
  IL_0011:  ldobj      ""S1?""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  constrained. ""S1?""
  IL_001f:  callvirt   ""string object.ToString()""
  IL_0024:  pop
  IL_0025:  ldarg.0
  IL_0026:  call       ""S1 S1?.GetValueOrDefault()""
  IL_002b:  box        ""S1""
  IL_0030:  call       ""void System.Console.Write(object)""
  IL_0035:  ret
}");

            comp.VerifyIL("Program.Test2(ref S1?)", @"
{
  // Code size       46 (0x2e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1 S1?.GetValueOrDefault()""
  IL_0006:  box        ""S1""
  IL_000b:  call       ""void System.Console.Write(object)""
  IL_0010:  ldarg.0
  IL_0011:  constrained. ""S1?""
  IL_0017:  callvirt   ""string object.ToString()""
  IL_001c:  pop
  IL_001d:  ldarg.0
  IL_001e:  call       ""S1 S1?.GetValueOrDefault()""
  IL_0023:  box        ""S1""
  IL_0028:  call       ""void System.Console.Write(object)""
  IL_002d:  ret
}");
        }

        [Fact]
        [WorkItem(530136, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=530136")]
        public void OperatorsWithInParametersFromMetadata_Binary()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public int Value { get; set; }

    public static int operator +(in Test a, in Test b)
    {
        return a.Value + b.Value;
    }
}");

            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var a = new Test { Value = 3 };
        var b = new Test { Value = 6 };

        System.Console.WriteLine(a + b);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "9");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "9");
        }

        [Fact]
        [WorkItem(530136, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=530136")]
        public void OperatorsWithInParametersFromMetadata_Binary_Right()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public int Value { get; set; }

    public static int operator +(Test a, in Test b)
    {
        return a.Value + b.Value;
    }
}");

            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var a = new Test { Value = 3 };
        var b = new Test { Value = 6 };

        System.Console.WriteLine(a + b);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "9");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "9");
        }

        [Fact]
        [WorkItem(530136, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=530136")]
        public void OperatorsWithInParametersFromMetadata_Binary_Left()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public int Value { get; set; }

    public static int operator +(in Test a, Test b)
    {
        return a.Value + b.Value;
    }
}");

            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var a = new Test { Value = 3 };
        var b = new Test { Value = 6 };

        System.Console.WriteLine(a + b);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "9");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "9");
        }

        [Fact]
        [WorkItem(530136, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=530136")]
        public void OperatorsWithInParametersFromMetadata_Unary()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public bool Value { get; set; }

    public static bool operator !(in Test a)
    {
        return !a.Value;
    }
}");

            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var a = new Test { Value = true };

        System.Console.WriteLine(!a);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "False");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "False");
        }

        [Fact]
        [WorkItem(530136, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=530136")]
        public void OperatorsWithInParametersFromMetadata_Conversion()
        {
            var reference = CreateCompilation(@"
public class Test
{
    public bool Value { get; set; }

    public static explicit operator int(in Test a)
    {
        return a.Value ? 3 : 5;
    }
}");

            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var a = new Test { Value = true };

        System.Console.WriteLine((int)a);
    }
}";

            CompileAndVerify(code, references: new[] { reference.ToMetadataReference() }, expectedOutput: "3");
            CompileAndVerify(code, references: new[] { reference.EmitToImageReference() }, expectedOutput: "3");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Optional_NoArgs()
        {
            var code = @"
class Program
{
    static void Test(in int value = 5)
    {
        System.Console.WriteLine(value);
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test()/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "5").VerifyIL("Program.Main", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""void Program.Test(in int)""
  IL_0009:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test([in System.Int32 value = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test()')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: 'Test()')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Optional_OneArg()
        {
            var code = @"
class Program
{
    static void Test(in int value = 5)
    {
        System.Console.WriteLine(value);
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test(10)/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "10").VerifyIL("Program.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""void Program.Test(in int)""
  IL_000a:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test([in System.Int32 value = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(10)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '10')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Optional_Optional_NoArgs()
        {
            var code = @"
class Program
{
    static void Test(in int value1 = 1, in int value2 = 5)
    {
        System.Console.WriteLine($""({value1}, {value2})"");
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test()/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "(1, 5)").VerifyIL("Program.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.5
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       ""void Program.Test(in int, in int)""
  IL_000d:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test([in System.Int32 value1 = 1], [in System.Int32 value2 = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test()')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'Test()')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: 'Test()')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Optional_Optional_OneArg()
        {
            var code = @"
class Program
{
    static void Test(in int value1 = 1, in int value2 = 5)
    {
        System.Console.WriteLine($""({value1}, {value2})"");
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test(2)/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "(2, 5)").VerifyIL("Program.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.5
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       ""void Program.Test(in int, in int)""
  IL_000d:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test([in System.Int32 value1 = 1], [in System.Int32 value2 = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(2)')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value1) (OperationKind.Argument, Type: null) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(2)')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: 'Test(2)')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Optional_Optional_TwoArgs()
        {
            var code = @"
class Program
{
    static void Test(in int value1 = 1, in int value2 = 5)
    {
        System.Console.WriteLine($""({value1}, {value2})"");
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test(3, 10)/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "(3, 10)").VerifyIL("Program.Main", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   10
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void Program.Test(in int, in int)""
  IL_000e:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test([in System.Int32 value1 = 1], [in System.Int32 value2 = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(3, 10)')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value1) (OperationKind.Argument, Type: null) (Syntax: '3')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value2) (OperationKind.Argument, Type: null) (Syntax: '10')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Required_Optional_OneArg()
        {
            var code = @"
class Program
{
    static void Test(in int value1, in int value2 = 5)
    {
        System.Console.WriteLine($""({value1}, {value2})"");
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test(1)/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "(1, 5)").VerifyIL("Program.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.5
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       ""void Program.Test(in int, in int)""
  IL_000d:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test(in System.Int32 value1, [in System.Int32 value2 = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(1)')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value1) (OperationKind.Argument, Type: null) (Syntax: '1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(1)')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: 'Test(1)')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_Method_Required_Optional_TwoArgs()
        {
            var code = @"
class Program
{
    static void Test(in int value1, in int value2 = 5)
    {
        System.Console.WriteLine($""({value1}, {value2})"");
    }

    static void Main(string[] args)
    {
        /*<bind>*/Test(2, 10)/*<bind>*/;
    }
}";

            CompileAndVerify(code, expectedOutput: "(2, 10)").VerifyIL("Program.Main", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   10
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""void Program.Test(in int, in int)""
  IL_000e:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.Test(in System.Int32 value1, [in System.Int32 value2 = 5])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(2, 10)')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value1) (OperationKind.Argument, Type: null) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value2) (OperationKind.Argument, Type: null) (Syntax: '10')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_CompoundAssignment_Optional_Optional_OneArg()
        {
            var code = @"
class Program
{
    public int this[in int p1 = 1, in int p2 = 2]
    {
        get
        {
            System.Console.WriteLine($""get p1={p1} p2={p2}"");
            return 0;
        }
        set
        {
            System.Console.WriteLine($""set p1={p1} p2={p2} to {value}"");
        }
    }

    static void Main(string[] args)
    {
        var obj = new Program();

        /*<bind>*/obj[3]/*<bind>*/ += 10;
    }
}";

            CompileAndVerify(code, expectedOutput: @"
get p1=3 p2=2
set p1=3 p2=2 to 10
").VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  6
  .locals init (Program V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.2
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.3
  IL_0011:  stloc.3
  IL_0012:  ldloca.s   V_3
  IL_0014:  ldc.i4.2
  IL_0015:  stloc.s    V_4
  IL_0017:  ldloca.s   V_4
  IL_0019:  callvirt   ""int Program.this[in int, in int].get""
  IL_001e:  ldc.i4.s   10
  IL_0020:  add
  IL_0021:  callvirt   ""void Program.this[in int, in int].set""
  IL_0026:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(code, @"
IPropertyReferenceOperation: System.Int32 Program.this[[in System.Int32 p1 = 1], [in System.Int32 p2 = 2]] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'obj[3]')
  Instance Receiver: 
    ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: Program) (Syntax: 'obj')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: '3')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: p2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'obj[3]')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'obj[3]')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_CompoundAssignment_Optional_Optional_TwoArgs()
        {
            var code = @"
class Program
{
    public int this[in int p1 = 1, in int p2 = 2]
    {
        get
        {
            System.Console.WriteLine($""get p1={p1} p2={p2}"");
            return 0;
        }
        set
        {
            System.Console.WriteLine($""set p1={p1} p2={p2} to {value}"");
        }
    }

    static void Main(string[] args)
    {
        var obj = new Program();

        /*<bind>*/obj[4, 5]/*<bind>*/ += 11;
    }
}";

            CompileAndVerify(code, expectedOutput: @"
get p1=4 p2=5
set p1=4 p2=5 to 11
").VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  6
  .locals init (Program V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.4
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.5
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.4
  IL_0011:  stloc.3
  IL_0012:  ldloca.s   V_3
  IL_0014:  ldc.i4.5
  IL_0015:  stloc.s    V_4
  IL_0017:  ldloca.s   V_4
  IL_0019:  callvirt   ""int Program.this[in int, in int].get""
  IL_001e:  ldc.i4.s   11
  IL_0020:  add
  IL_0021:  callvirt   ""void Program.this[in int, in int].set""
  IL_0026:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(code, @"
IPropertyReferenceOperation: System.Int32 Program.this[[in System.Int32 p1 = 1], [in System.Int32 p2 = 2]] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'obj[4, 5]')
  Instance Receiver: 
    ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: Program) (Syntax: 'obj')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: '4')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p2) (OperationKind.Argument, Type: null) (Syntax: '5')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_CompoundAssignment_Required_Optional_OneArg()
        {
            var code = @"
class Program
{
    public int this[in int p1, in int p2 = 2]
    {
        get
        {
            System.Console.WriteLine($""get p1={p1} p2={p2}"");
            return 0;
        }
        set
        {
            System.Console.WriteLine($""set p1={p1} p2={p2} to {value}"");
        }
    }

    static void Main(string[] args)
    {
        var obj = new Program();

        /*<bind>*/obj[3]/*<bind>*/ += 10;
    }
}";

            CompileAndVerify(code, expectedOutput: @"
get p1=3 p2=2
set p1=3 p2=2 to 10
").VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  6
  .locals init (Program V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.2
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.3
  IL_0011:  stloc.3
  IL_0012:  ldloca.s   V_3
  IL_0014:  ldc.i4.2
  IL_0015:  stloc.s    V_4
  IL_0017:  ldloca.s   V_4
  IL_0019:  callvirt   ""int Program.this[in int, in int].get""
  IL_001e:  ldc.i4.s   10
  IL_0020:  add
  IL_0021:  callvirt   ""void Program.this[in int, in int].set""
  IL_0026:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(code, @"
IPropertyReferenceOperation: System.Int32 Program.this[in System.Int32 p1, [in System.Int32 p2 = 2]] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'obj[3]')
  Instance Receiver: 
    ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: Program) (Syntax: 'obj')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: '3')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: p2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'obj[3]')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'obj[3]')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void OptionalInParameters_CompoundAssignment_Required_Optional_TwoArgs()
        {
            var code = @"
class Program
{
    public int this[in int p1, in int p2 = 2]
    {
        get
        {
            System.Console.WriteLine($""get p1={p1} p2={p2}"");
            return 0;
        }
        set
        {
            System.Console.WriteLine($""set p1={p1} p2={p2} to {value}"");
        }
    }

    static void Main(string[] args)
    {
        var obj = new Program();

        /*<bind>*/obj[4, 5]/*<bind>*/ += 11;
    }
}";

            CompileAndVerify(code, expectedOutput: @"
get p1=4 p2=5
set p1=4 p2=5 to 11
").VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  6
  .locals init (Program V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.4
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.5
  IL_000c:  stloc.2
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.4
  IL_0011:  stloc.3
  IL_0012:  ldloca.s   V_3
  IL_0014:  ldc.i4.5
  IL_0015:  stloc.s    V_4
  IL_0017:  ldloca.s   V_4
  IL_0019:  callvirt   ""int Program.this[in int, in int].get""
  IL_001e:  ldc.i4.s   11
  IL_0020:  add
  IL_0021:  callvirt   ""void Program.this[in int, in int].set""
  IL_0026:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(code, @"
IPropertyReferenceOperation: System.Int32 Program.this[in System.Int32 p1, [in System.Int32 p2 = 2]] { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'obj[4, 5]')
  Instance Receiver: 
    ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: Program) (Syntax: 'obj')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: '4')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p2) (OperationKind.Argument, Type: null) (Syntax: '5')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void Issue23691_PassingInOptionalArgumentsByRef_OneArg()
        {
            var code = @"
class Program
{
    static void Main()
    {
        /*<bind>*/A(1)/*<bind>*/;
    }

    static void A(in double x = 1, in string y = ""test"") => System.Console.WriteLine(y);
    static void B(in float x, in float y, in float z = 3.0f) => System.Console.WriteLine(x * y * z);

}";

            CompileAndVerify(code, expectedOutput: "test").VerifyIL("Program.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (double V_0,
                string V_1)
  IL_0000:  ldc.r8     1
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldstr      ""test""
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_1
  IL_0014:  call       ""void Program.A(in double, in string)""
  IL_0019:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.A([in System.Double x = 1], [in System.String y = ""test""])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(1)')
  Instance Receiver: 
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 1, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'A(1)')
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"", IsImplicit) (Syntax: 'A(1)')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.ReadOnlyReferences)]
        [WorkItem(23691, "https://github.com/dotnet/roslyn/issues/23691")]
        public void Issue23691_PassingInOptionalArgumentsByRef_TwoArgs()
        {
            var code = @"
class Program
{
    static void Main()
    {
        /*<bind>*/B(1, 2)/*<bind>*/;
    }

    static void A(in double x = 1, in string y = ""test"") => System.Console.WriteLine(y);
    static void B(in float x, in float y, in float z = 3.0f) => System.Console.WriteLine(x * y * z);

}";

            CompileAndVerify(code, expectedOutput: "6").VerifyIL("Program.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (float V_0,
                float V_1,
                float V_2)
  IL_0000:  ldc.r4     1
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.r4     2
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.r4     3
  IL_0015:  stloc.2
  IL_0016:  ldloca.s   V_2
  IL_0018:  call       ""void Program.B(in float, in float, in float)""
  IL_001d:  ret
}");

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(code, @"
IInvocationOperation (void Program.B(in System.Single x, in System.Single y, [in System.Single z = 3])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'B(1, 2)')
  Instance Receiver: 
    null
  Arguments(3):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 1, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: '2')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 2, IsImplicit) (Syntax: '2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: z) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'B(1, 2)')
        ILiteralOperation (OperationKind.Literal, Type: System.Single, Constant: 3, IsImplicit) (Syntax: 'B(1, 2)')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)",
        DiagnosticDescription.None);
        }

        [WorkItem(23692, "https://github.com/dotnet/roslyn/issues/23692")]
        [Fact]
        public void ThisToInParam()
        {
            var code = @"
using System;

static class Ex
{
    public static void InMethod(in X arg) => Console.Write(arg);
}

class X
{
    public void M()
    {
        // pass `this` by in-parameter.
        Ex.InMethod(this);
    }
}

class Program
{
    static void Main()
    {
        var x = new X();

        // baseline
        Ex.InMethod(x);

        x.M();
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "XX");

            verifier.VerifyIL("X.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""void Ex.InMethod(in X)""
  IL_0007:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_Local()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        int x = 50;
        Moo(x + 0, () => x = 60);
    }
    
    static void Moo(in int y, Action change)
    {
        Console.Write(y);
        change();
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "5050");

            verifier.VerifyIL("Test.Main(string[])", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (Test.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1)
  IL_0000:  newobj     ""Test.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   50
  IL_0009:  stfld      ""int Test.<>c__DisplayClass0_0.x""
  IL_000e:  ldloc.0
  IL_000f:  ldfld      ""int Test.<>c__DisplayClass0_0.x""
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldloc.0
  IL_0018:  ldftn      ""void Test.<>c__DisplayClass0_0.<Main>b__0()""
  IL_001e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0023:  call       ""void Test.Moo(in int, System.Action)""
  IL_0028:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_ArrayAccess()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        int[] x = new int[] { 50 };
        Moo(x[0] + 0, () => x[0] = 60);
    }

    static void Moo(in int y, Action change)
    {
        Console.Write(y);
        change();
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "5050");

            verifier.VerifyIL("Test.Main(string[])", @"
{
  // Code size       52 (0x34)
  .maxstack  5
  .locals init (Test.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1)
  IL_0000:  newobj     ""Test.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""int""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.s   50
  IL_0011:  stelem.i4
  IL_0012:  stfld      ""int[] Test.<>c__DisplayClass0_0.x""
  IL_0017:  ldloc.0
  IL_0018:  ldfld      ""int[] Test.<>c__DisplayClass0_0.x""
  IL_001d:  ldc.i4.0
  IL_001e:  ldelem.i4
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.0
  IL_0023:  ldftn      ""void Test.<>c__DisplayClass0_0.<Main>b__0()""
  IL_0029:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002e:  call       ""void Test.Moo(in int, System.Action)""
  IL_0033:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_ArrayAccessReordered()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        int[] x = new int[] { 50 };
        Moo(change: () => x[0] = 60, y: x[0] + 0);
    }

    static void Moo(in int y, Action change)
    {
        Console.Write(y);
        change();
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "5050");

            verifier.VerifyIL("Test.Main(string[])", @"
{
  // Code size       52 (0x34)
  .maxstack  5
  .locals init (Test.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1)
  IL_0000:  newobj     ""Test.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""int""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.s   50
  IL_0011:  stelem.i4
  IL_0012:  stfld      ""int[] Test.<>c__DisplayClass0_0.x""
  IL_0017:  ldloc.0
  IL_0018:  ldfld      ""int[] Test.<>c__DisplayClass0_0.x""
  IL_001d:  ldc.i4.0
  IL_001e:  ldelem.i4
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.0
  IL_0023:  ldftn      ""void Test.<>c__DisplayClass0_0.<Main>b__0()""
  IL_0029:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002e:  call       ""void Test.Moo(in int, System.Action)""
  IL_0033:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_FieldAcces()
        {
            var code = @"
using System;

public class Test
{
    struct S1
    {
        public int x;
    }

    static S1 s = new S1 { x = 555 };

    static void Main(string[] args)
    {
        Moo(s.x + 0);
    }

    static void Moo(in int y)
    {
        Console.Write(y);
        s.x = 123;
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "555555");

            verifier.VerifyIL("Test.Main(string[])", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldsflda    ""Test.S1 Test.s""
  IL_0005:  ldfld      ""int Test.S1.x""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""void Test.Moo(in int)""
  IL_0012:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_RoFieldAcces()
        {
            var code = @"
using System;

public class Test
{
    struct S1
    {
        public int x;
    }

    readonly S1 s;

    static void Main(string[] args)
    {
        var x = new Test();
    }

    public Test()
    {
        Test1(s.x + 0, ref s.x);
    }

    private void Test1(in int y, ref int f)
    {
        Console.Write(y);
        f = 1;
        Console.Write(y);

        Test2(s.x + 0, ref f);
    }

    private void Test2(in int y, ref int f)
    {
        Console.Write(y);
        f = 2;
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            // PEVerify: Cannot change initonly field outside its .ctor.
            var verifier = CompileAndVerify(compilation, expectedOutput: "0011", verify: Verification.FailsPEVerify);

            verifier.VerifyIL("Test..ctor()", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  ldflda     ""Test.S1 Test.s""
  IL_000d:  ldfld      ""int Test.S1.x""
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  ldarg.0
  IL_0016:  ldflda     ""Test.S1 Test.s""
  IL_001b:  ldflda     ""int Test.S1.x""
  IL_0020:  call       ""void Test.Test1(in int, ref int)""
  IL_0025:  ret
}
");

            verifier.VerifyIL("Test.Test1(in int, ref int)", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldind.i4
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  ldarg.2
  IL_0008:  ldc.i4.1
  IL_0009:  stind.i4
  IL_000a:  ldarg.1
  IL_000b:  ldind.i4
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldarg.0
  IL_0012:  ldarg.0
  IL_0013:  ldflda     ""Test.S1 Test.s""
  IL_0018:  ldfld      ""int Test.S1.x""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  ldarg.2
  IL_0021:  call       ""void Test.Test2(in int, ref int)""
  IL_0026:  ret
}
");

        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_ThisAcces()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        var x = new Test();
    }

    public Test()
    {
        Test3(null ?? this);
    }

    private void Test3(in Test y)
    {
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "");

            verifier.VerifyIL("Test..ctor()", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (Test V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       ""void Test.Test3(in Test)""
  IL_0010:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_RefMethod()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        var x = new Test();
    }

    private string s = ""hi"";

    private ref string M1()
    {
        return ref s;
    }

    public Test()
    {
        Test3(null ?? M1());
    }

    private void Test3(in string y)
    {
        Console.Write(y);
        s = ""bye"";
        Console.Write(y);
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "hihi");

            verifier.VerifyIL("Test..ctor()", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""hi""
  IL_0006:  stfld      ""string Test.s""
  IL_000b:  ldarg.0
  IL_000c:  call       ""object..ctor()""
  IL_0011:  ldarg.0
  IL_0012:  ldarg.0
  IL_0013:  call       ""ref string Test.M1()""
  IL_0018:  ldind.ref
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       ""void Test.Test3(in string)""
  IL_0021:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_InOperator()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        var x = new Test();
    }

    private static string s = ""hi"";

    public Test()
    {
        var dummy = (null ?? s) + this;
    }

    public static string operator +(in string y, in Test t)
    {
        Console.Write(y);
        s = ""bye"";
        Console.Write(y);

        return y;
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "hihi");

            verifier.VerifyIL("Test..ctor()", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldsfld     ""string Test.s""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldarga.s   V_0
  IL_0010:  call       ""string Test.op_Addition(in string, in Test)""
  IL_0015:  pop
  IL_0016:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_InOperatorLifted()
        {
            var code = @"
using System;

public struct Test
{
    static void Main(string[] args)
    {
        var x = new Test();
        x.Test1();
    }

    private Action change;
    public Test(Action change)
    {
        this.change = change;
    }


    public void Test1()
    {
        int s = 1;
        Test? t = new Test(() => s = 42);

        var dummy = s + t;
    }

    public static int operator +(in int y, in Test t)
    {
        Console.Write(y);
        t.change();
        Console.Write(y);

        return 88;
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "11");

            verifier.VerifyIL("Test.Test1()", @"
{
  // Code size       71 (0x47)
  .maxstack  2
  .locals init (Test.<>c__DisplayClass3_0 V_0, //CS$<>8__locals0
                int V_1,
                Test? V_2,
                Test V_3)
  IL_0000:  newobj     ""Test.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      ""int Test.<>c__DisplayClass3_0.s""
  IL_000d:  ldloc.0
  IL_000e:  ldftn      ""void Test.<>c__DisplayClass3_0.<Test1>b__0()""
  IL_0014:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0019:  newobj     ""Test..ctor(System.Action)""
  IL_001e:  newobj     ""Test?..ctor(Test)""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int Test.<>c__DisplayClass3_0.s""
  IL_0029:  stloc.1
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  call       ""bool Test?.HasValue.get""
  IL_0032:  brfalse.s  IL_0046
  IL_0034:  ldloca.s   V_1
  IL_0036:  ldloca.s   V_2
  IL_0038:  call       ""Test Test?.GetValueOrDefault()""
  IL_003d:  stloc.3
  IL_003e:  ldloca.s   V_3
  IL_0040:  call       ""int Test.op_Addition(in int, in Test)""
  IL_0045:  pop
  IL_0046:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_InOperatorUnary()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        Test1();
    }

    private static Test s = new Test();

    public static void Test1()
    {
        var dummy = +(null ?? s);
    }

    public static Test operator +(in Test y)
    {
        Console.Write(y);
        s = default;
        Console.Write(y);

        return y;
    }
}
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "TestTest");

            verifier.VerifyIL("Test.Test1()", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Test V_0)
  IL_0000:  ldsfld     ""Test Test.s""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""Test Test.op_UnaryPlus(in Test)""
  IL_000d:  pop
  IL_000e:  ret
}
");
        }

        [WorkItem(24806, "https://github.com/dotnet/roslyn/issues/24806")]
        [Fact]
        public void OptimizedRValueToIn_InConversion()
        {
            var code = @"
using System;

public class Test
{
    static void Main(string[] args)
    {
        Test1();
    }

    private static Test s = new Test();

    public static void Test1()
    {
        int dummyI = (null ?? s);

        s = new Derived();

        long dummyL = (null ?? s);
    }

    public static implicit operator int(in Test y)
    {
        Console.Write(y.ToString());
        s = default;
        Console.Write(y.ToString());

        return 1;
    }
}

class Derived : Test { }
";

            var compilation = CreateCompilation(code, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(compilation, expectedOutput: "TestTestDerivedDerived");

            verifier.VerifyIL("Test.Test1()", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (Test V_0)
  IL_0000:  ldsfld     ""Test Test.s""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""int Test.op_Implicit(in Test)""
  IL_000d:  pop
  IL_000e:  newobj     ""Derived..ctor()""
  IL_0013:  stsfld     ""Test Test.s""
  IL_0018:  ldsfld     ""Test Test.s""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""int Test.op_Implicit(in Test)""
  IL_0025:  pop
  IL_0026:  ret
}
");
        }

        [Theory, CombinatorialData, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void ConstrainedCallOnInParameter([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static void Main()
    {
        S value = new();
        ref readonly S valueRef = ref value;
        Console.Write(valueRef);
        M(in valueRef);
        Console.Write(valueRef);
    }
    public static void M(" + modifier + @" S value)
    {
        foreach (var x in value) { }
    }
}

public struct S : IEnumerable<int>
{
    int a;
    public readonly override string ToString() => a.ToString();
    private IEnumerator<int> GetEnumerator() => Enumerable.Range(0, ++a).GetEnumerator();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}";
            var verifier = CompileAndVerify(source, expectedOutput: "00");
            // Note: we use a temp instead of directly doing a constrained call on `in` parameter
            verifier.VerifyIL("C.M", """
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator<int> V_0,
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S"
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  constrained. "S"
  IL_000f:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_001e
    IL_0017:  ldloc.0
    IL_0018:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_001d:  pop
    IL_001e:  ldloc.0
    IL_001f:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0024:  brtrue.s   IL_0017
    IL_0026:  leave.s    IL_0032
  }
  finally
  {
    IL_0028:  ldloc.0
    IL_0029:  brfalse.s  IL_0031
    IL_002b:  ldloc.0
    IL_002c:  callvirt   "void System.IDisposable.Dispose()"
    IL_0031:  endfinally
  }
  IL_0032:  ret
}
""");
        }

        [Theory, CombinatorialData, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void ConstrainedCallOnInParameter_ConstrainedGenericReceiver([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static void Main()
    {
        S value = new();
        ref readonly S valueRef = ref value;
        Console.Write(valueRef);
        M(in valueRef);
        Console.Write(valueRef);
    }
    public static void M<T>(" + modifier + @" T value) where T : struct, IEnumerable<int>
    {
        foreach (var x in value) { }
    }
}

public struct S : IEnumerable<int>
{
    int a;
    public readonly override string ToString() => a.ToString();
    private IEnumerator<int> GetEnumerator() => Enumerable.Range(0, ++a).GetEnumerator();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}";
            var verifier = CompileAndVerify(source, expectedOutput: "00");
            verifier.VerifyIL($"C.M<T>({modifier} T)", """
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator<int> V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "T"
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_001e
    IL_0017:  ldloc.0
    IL_0018:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_001d:  pop
    IL_001e:  ldloc.0
    IL_001f:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0024:  brtrue.s   IL_0017
    IL_0026:  leave.s    IL_0032
  }
  finally
  {
    IL_0028:  ldloc.0
    IL_0029:  brfalse.s  IL_0031
    IL_002b:  ldloc.0
    IL_002c:  callvirt   "void System.IDisposable.Dispose()"
    IL_0031:  endfinally
  }
  IL_0032:  ret
}
""");
        }

        [Fact, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void ConstrainedCallOnReadonlyField()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static void Main()
    {
        S s = new();
        var d = new D(s);
        d.M();
        d.M();
    }
}

public class D
{
    readonly S field;
    public D(S s) { field = s; }

    public void M()
    {
        foreach (var x in field) { }
        System.Console.Write(field.ToString());
    }
}

public struct S : IEnumerable<int>
{
    int a;
    public readonly override string ToString() => a.ToString();
    private IEnumerator<int> GetEnumerator() => Enumerable.Range(0, ++a).GetEnumerator();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}";
            var verifier = CompileAndVerify(source, expectedOutput: "00", verify: Verification.FailsPEVerify);
            // Note: we use a temp instead of directly doing a constrained call on readonly field
            verifier.VerifyIL("D.M", """
{
  // Code size       73 (0x49)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator<int> V_0,
                S V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "S D.field"
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  constrained. "S"
  IL_000f:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
  IL_0014:  stloc.0
  .try
  {
    IL_0015:  br.s       IL_001e
    IL_0017:  ldloc.0
    IL_0018:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_001d:  pop
    IL_001e:  ldloc.0
    IL_001f:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0024:  brtrue.s   IL_0017
    IL_0026:  leave.s    IL_0032
  }
  finally
  {
    IL_0028:  ldloc.0
    IL_0029:  brfalse.s  IL_0031
    IL_002b:  ldloc.0
    IL_002c:  callvirt   "void System.IDisposable.Dispose()"
    IL_0031:  endfinally
  }
  IL_0032:  ldarg.0
  IL_0033:  ldflda     "S D.field"
  IL_0038:  constrained. "S"
  IL_003e:  callvirt   "string object.ToString()"
  IL_0043:  call       "void System.Console.Write(string)"
  IL_0048:  ret
}
""");
        }

        [Fact, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void ConstrainedCallOnField()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class C
{
    public static void Main()
    {
        S s = new();
        var d = new D(s);
        d.M();
        d.M();
    }
}

public class D
{
    S field;
    public D(S s) { field = s; }

    public void M()
    {
        foreach (var x in field) { }
        System.Console.Write(field.ToString());
    }
}

public struct S : IEnumerable<int>
{
    int a;
    public readonly override string ToString() => a.ToString();
    private IEnumerator<int> GetEnumerator() => Enumerable.Range(0, ++a).GetEnumerator();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            // Note: we do a constrained call directly on the field
            verifier.VerifyIL("D.M", """
{
  // Code size       70 (0x46)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "S D.field"
  IL_0006:  constrained. "S"
  IL_000c:  callvirt   "System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()"
  IL_0011:  stloc.0
  .try
  {
    IL_0012:  br.s       IL_001b
    IL_0014:  ldloc.0
    IL_0015:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_001a:  pop
    IL_001b:  ldloc.0
    IL_001c:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0021:  brtrue.s   IL_0014
    IL_0023:  leave.s    IL_002f
  }
  finally
  {
    IL_0025:  ldloc.0
    IL_0026:  brfalse.s  IL_002e
    IL_0028:  ldloc.0
    IL_0029:  callvirt   "void System.IDisposable.Dispose()"
    IL_002e:  endfinally
  }
  IL_002f:  ldarg.0
  IL_0030:  ldflda     "S D.field"
  IL_0035:  constrained. "S"
  IL_003b:  callvirt   "string object.ToString()"
  IL_0040:  call       "void System.Console.Write(string)"
  IL_0045:  ret
}
""");
        }

        [Theory, CombinatorialData, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void InvokeStructToStringOverrideOnInParameter([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            var text = @"
using System;

class C
{
    public static void Main()
    {
        S1 s = new S1();
        Console.Write(M(in s));
        Console.Write(M(in s));
    }
    static string M(" + modifier + @" S1 s)
    {
        return s.ToString();
    }
}
struct S1
{
    int i;
    public override string ToString() => (i++).ToString();
}
";

            var comp = CompileAndVerify(text, expectedOutput: "00");

            comp.VerifyIL("C.M", """
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S1"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. "S1"
  IL_000f:  callvirt   "string object.ToString()"
  IL_0014:  ret
}
""");
        }

        [Theory, CombinatorialData, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void InvokeAddedStructToStringOverrideOnInParameter([CombinatorialValues("in", "ref readonly")] string modifier)
        {
            var libOrig_cs = """
public struct S
{
    int i;
    public void Report() { throw null; }
}
""";
            var libOrig = CreateCompilation(libOrig_cs, assemblyName: "lib");

            var libChanged_cs = """
public struct S
{
    int i;
    public override string ToString() => (i++).ToString();
    public void Report() { System.Console.Write("RAN "); }
}
""";
            var libChanged = CreateCompilation(libChanged_cs, assemblyName: "lib");

            var libUser_cs = $$"""
public class C
{
    public static string M({{modifier}} S s)
    {
        return s.ToString();
    }
}
""";
            var libUser = CreateCompilation(libUser_cs, references: new[] { libOrig.EmitToImageReference() });
            CompileAndVerify(libUser).VerifyIL("C.M", """
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. "S"
  IL_000f:  callvirt   "string object.ToString()"
  IL_0014:  ret
}
""");

            var src = """
using System;

S s = new S();
s.Report();
Console.Write(C.M(in s));
Console.Write(C.M(in s));
""";

            var comp = CreateCompilation(src, references: new[] { libChanged.EmitToImageReference(), libUser.EmitToImageReference() });
            CompileAndVerify(comp, expectedOutput: "RAN 00");
        }

        [Fact, WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")]
        public void InvokeAddedStructToStringOverrideOnReadonlyField()
        {
            var libOrig_cs = """
public struct S
{
    int i;
    public void Report() { throw null; }
}
""";
            var libOrig = CreateCompilation(libOrig_cs, assemblyName: "lib");

            var libChanged_cs = """
public struct S
{
    int i;
    public override string ToString() => (i++).ToString();
    public void Report() { System.Console.Write($"Report{i} "); }
}
""";
            var libChanged = CreateCompilation(libChanged_cs, assemblyName: "lib");

            var libUser_cs = """
public class C
{
    readonly S field;
    public C(int i)
    {
        field.ToString();
    }
    public string M()
    {
        return field.ToString();
    }
    public void Report() { field.Report(); }
}
""";
            var libUser = CreateCompilation(libUser_cs, references: new[] { libOrig.EmitToImageReference() });
            var verifier = CompileAndVerify(libUser);
            verifier.VerifyIL("C.M", """
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "S C.field"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. "S"
  IL_000f:  callvirt   "string object.ToString()"
  IL_0014:  ret
}
""");

            verifier.VerifyIL("C..ctor(int)", """
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldflda     "S C.field"
  IL_000c:  constrained. "S"
  IL_0012:  callvirt   "string object.ToString()"
  IL_0017:  pop
  IL_0018:  ret
}
""");

            var src = """
C c = new C(42);
c.Report();
System.Console.Write(c.M());
System.Console.Write(c.M());
""";

            var comp = CreateCompilation(src, references: new[] { libChanged.EmitToImageReference(), libUser.EmitToImageReference() });
            CompileAndVerify(comp, expectedOutput: "Report1 11");
        }
    }
}
