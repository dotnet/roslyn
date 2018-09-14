// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.RefLocalsReturns)]
    public class CodeGenRefLocalTests : CompilingTestBase
    {
        [Fact]
        public void RefReassignInArrayElement()
        {
            const string src = @"
using System;
class C
{
    void M()
    {
        object o = string.Empty;
        M2(o);
    }
    void M2(in object o)
    {
        o = ref (new object[1])[0];
        Console.WriteLine(o?.GetHashCode() ?? 5);
    }
}";
            var verifier = CompileAndVerify(src, verify: Verification.Fails);
            const string expectedIL = @"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""object""
  IL_0006:  ldc.i4.0
  IL_0007:  readonly.
  IL_0009:  ldelema    ""object""
  IL_000e:  starg.s    V_1
  IL_0010:  ldarg.1
  IL_0011:  ldind.ref
  IL_0012:  dup
  IL_0013:  brtrue.s   IL_0019
  IL_0015:  pop
  IL_0016:  ldc.i4.5
  IL_0017:  br.s       IL_001e
  IL_0019:  callvirt   ""int object.GetHashCode()""
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ret
}";
            verifier.VerifyIL("C.M2", expectedIL);

            // N.B. Even with PEVerify compat this generates unverifiable code.
            // Compat mode has no effect because it would generate a temp variable
            // which, if we assign to the in parameter, violates safety by allowing
            // a local to be returned outside of the method scope.
            verifier = CompileAndVerify(src,
                parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature(),
                verify: Verification.Fails);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact]
        public void ReassignmentFixed()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    unsafe static void Main()
    {
        int x = 5, y = 11;
        ref int rx = ref x;
        fixed (int* ptr = &(rx = ref y))
        {
            Console.WriteLine(*ptr);
            rx = ref *ptr;
            Console.WriteLine(rx);
            rx = ref ptr[0];
            Console.WriteLine(rx);
        }
    }
}", options: TestOptions.UnsafeReleaseExe,
verify: Verification.Fails,
expectedOutput: @"11
11
11");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                pinned int& V_2)
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.s   11
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_1
  IL_0007:  stloc.2
  IL_0008:  ldloc.2
  IL_0009:  conv.u
  IL_000a:  dup
  IL_000b:  ldind.i4
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  dup
  IL_0012:  ldind.i4
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ldind.i4
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.0
  IL_001f:  conv.u
  IL_0020:  stloc.2
  IL_0021:  ret
}");
        }

        [Fact]
        public void ReassignmentInOut()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 1, y = 2;
        ref int rx = ref x;
        M(out (rx = ref y));
        Console.WriteLine(rx);
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
    static void M(out int rx)
    {
        rx = 5;
    }
}", expectedOutput: @" 5
1
5");
        }

        [Fact]
        public void ReassignmentWithReorderParameters()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x1 = 5, x2 = 7;
        ref int rx1 = ref x2, rx2 = ref x2;
        M2(p2: (rx1 = ref x2), p1: (rx1 = ref x1));
    }

    static void M2(int p1, int p2)
    {
        Console.WriteLine(p1);
        Console.WriteLine(p2);
    }
}", expectedOutput: @"5
7");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int V_0, //x1
                int V_1, //x2
                int V_2)
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.7
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  stloc.2
  IL_0006:  ldloc.0
  IL_0007:  ldloc.2
  IL_0008:  call       ""void C.M2(int, int)""
  IL_000d:  ret
}");
        }

        [Fact]
        public void ReassignmentWithReorderRefParameters()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x1 = 5, x2 = 7;
        ref int rx1 = ref x2, rx2 = ref x2;
        M2(p2: ref (rx1 = ref x2), p1: ref (rx1 = ref x1));
    }

    static void M2(ref int p1, ref int p2)
    {
        Console.WriteLine(p1);
        Console.WriteLine(p2);
    }
}", expectedOutput: @"5
7");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0, //x1
                int V_1, //x2
                int& V_2)
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.7
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_1
  IL_0006:  stloc.2
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.2
  IL_000a:  call       ""void C.M2(ref int, ref int)""
  IL_000f:  ret
}");
        }

        [Fact]
        public void InReassignmentWithConversion()
        {
            var verifier = CompileAndVerify(@"
class C
{
    void M(string s)
    {
        ref string rs = ref s;
        M2((rs = ref s));
    }
    void M2(in object o) {}
}");
            verifier.VerifyIL("C.M(string)", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (string& V_0, //rs
                object V_1)
  IL_0000:  ldarga.s   V_1
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  ldarga.s   V_1
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ldind.ref
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""void C.M2(in object)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void RefExprNullPropagation()
        {
            var verifier = CompileAndVerify(@"
using System;
struct S : IDisposable
{
    public int F;
    public S(int f) => F = f;
    public void Dispose()
    {
        this.F++;
        Console.WriteLine(""Dispose"");
    }
}

class C
{
    public static void Main()
    {
        S s1 = new S(10);
        S s2 = new S(5);
        ref S rs = ref s1;
        (rs = ref s2).Dispose();
        Console.WriteLine(s1.F);
        Console.WriteLine(s2.F);
        M(ref s1, ref s2);
        Console.WriteLine(s1.F);
        Console.WriteLine(s2.F);
    }

    private static void M<T>(ref T t1, ref T t2) where T : IDisposable
    {
        (t2 = ref t1)?.Dispose();
    }
}", expectedOutput: @"
Dispose
10
6
Dispose
11
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (S V_0, //s1
                S V_1) //s2
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   10
  IL_0004:  call       ""S..ctor(int)""
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldc.i4.5
  IL_000c:  call       ""S..ctor(int)""
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""void S.Dispose()""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""int S.F""
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ldloc.1
  IL_0024:  ldfld      ""int S.F""
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldloca.s   V_0
  IL_0030:  ldloca.s   V_1
  IL_0032:  call       ""void C.M<S>(ref S, ref S)""
  IL_0037:  ldloc.0
  IL_0038:  ldfld      ""int S.F""
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ldloc.1
  IL_0043:  ldfld      ""int S.F""
  IL_0048:  call       ""void System.Console.WriteLine(int)""
  IL_004d:  ret
}");
            verifier.VerifyIL("C.M<T>", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  starg.s    V_1
  IL_0004:  ldloca.s   V_0
  IL_0006:  initobj    ""T""
  IL_000c:  ldloc.0
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_0026
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldloc.0
  IL_001d:  box        ""T""
  IL_0022:  brtrue.s   IL_0026
  IL_0024:  pop
  IL_0025:  ret
  IL_0026:  constrained. ""T""
  IL_002c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0031:  ret
}");
        }

        [Fact]
        public void RefExprUnaryPlus()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        int x = 10;
        int y = 5;
        ref int rx = ref x;
        Console.WriteLine((rx = ref y)++);
        Console.WriteLine(rx);
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", expectedOutput: @"5
6
10
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (int V_0, //x
                int V_1, //y
                int V_2)
  IL_0000:  ldc.i4.s   10
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.5
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_1
  IL_0007:  dup
  IL_0008:  dup
  IL_0009:  ldind.i4
  IL_000a:  stloc.2
  IL_000b:  ldloc.2
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  stind.i4
  IL_000f:  ldloc.2
  IL_0010:  call       ""void System.Console.WriteLine(int)""
  IL_0015:  ldind.i4
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  ldloc.0
  IL_001c:  call       ""void System.Console.WriteLine(int)""
  IL_0021:  ldloc.1
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void RefLocalFor()
        {
            CompileAndVerify(@"
using System;
class C
{
    class LinkedList
    {
        public int Value;
        public LinkedList Next;
    }
    public static void Main()
    {
        var list = new LinkedList() { Value = 0,
            Next = new LinkedList() { Value = 0,
                Next = new LinkedList() { Value = 0, Next = null } } };

        for (ref var cur = ref list; cur != null; cur = ref cur.Next)
        {
            Console.WriteLine(cur.Value);
        }
        for (ref var cur = ref list; cur != null; cur = ref cur.Next)
        {
            cur.Value++;
        }
        for (ref readonly var cur = ref list; cur != null; cur = ref cur.Next)
        {
            Console.WriteLine(cur.Value);
        }
    }
}", expectedOutput: @"0
0
0
1
1
1");
        }

        [Fact]
        public void RefLocalForeach()
        {
            CompileAndVerify(@"
using System;
class C
{
    public static void Main()
    {
        var re = new RefEnumerable();
        foreach (ref var x in re)
        {
            Console.WriteLine(x);
        }
        foreach (ref var x in re)
        {
            x++;
        }
        foreach (ref readonly var x in re)
        {
            Console.WriteLine(x);
        }
    }
}

class RefEnumerable
{
    private readonly int[] _arr = new int[5];
    public StructEnum GetEnumerator() => new StructEnum(_arr);

    public struct StructEnum
    {
        private readonly int[] _arr;
        private int _current;
        public StructEnum(int[] arr)
        {
            _arr = arr;
            _current = -1;
        }
        public ref int Current => ref _arr[_current];
        public bool MoveNext() => ++_current != _arr.Length;
    }
}", expectedOutput: @"0
0
0
0
0
1
1
1
1
1");
        }

        [Fact]
        public void RefReassignDifferentTupleNames()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public static void Main()
    {
        var t = (x: 0, y: 0);
        ref (int x, int y) rt = ref t;

        var t2 = (a: 2, b: 2);
        rt = ref t2;

        Console.Write(rt.x);
        Console.Write(rt.y);
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "22");
        }

        [Fact]
        public void RefReassignRefExpressions()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static readonly int _ro = 42;
    static int _rw = 42;

    static void Main()
    {
        ref readonly var rro = ref _ro;
        ref var rrw = ref _rw;

        Console.WriteLine(rro);
        Console.WriteLine(rrw);

        rrw++;
        rro = ref (rro = ref rrw);
        rrw = ref (rrw = ref rrw);

        Console.WriteLine(rro);
        Console.WriteLine(rrw);
        Console.WriteLine(_ro);
    }
}", verify: Verification.Fails, expectedOutput: @"42
42
43
43
42");
        }

        [Fact]
        public void RefReassignParamByVal()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 0;
        ref int rx = ref x;
        int y = 5;
        Console.WriteLine(rx);
        M(rx = ref y);
        Console.WriteLine(rx);
    }

    static void M(int p)
    {
        Console.WriteLine(p);
        p++;
        Console.WriteLine(p);
    }
}", expectedOutput: @"0
5
6
5
");
        }

        [Fact]
        public void RefReassignParamIn()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 0;
        ref int rx = ref x;
        int y = 5;
        Console.WriteLine(rx);
        M(rx = ref y);
        Console.WriteLine(rx);
    }
    static void M(in int p)
    {
        Console.WriteLine(p);
    }
}", expectedOutput: @"0
5
5");
            comp.VerifyIL("C.Main", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.5
  IL_0005:  stloc.1
  IL_0006:  ldind.i4
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ldloca.s   V_1
  IL_000e:  dup
  IL_000f:  call       ""void C.M(in int)""
  IL_0014:  ldind.i4
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void RefReassignParamInConversion()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 0;
        ref int rx = ref x;
        int y = 5;
        Console.WriteLine(rx);
        M(rx = ref y);
        Console.WriteLine(rx);
    }
    static void M(in long p)
    {
        Console.WriteLine(p);
    }
}", expectedOutput: @"0
5
5");
            comp.VerifyIL("C.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                long V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.5
  IL_0005:  stloc.1
  IL_0006:  ldind.i4
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ldloca.s   V_1
  IL_000e:  dup
  IL_000f:  ldind.i4
  IL_0010:  conv.i8
  IL_0011:  stloc.2
  IL_0012:  ldloca.s   V_2
  IL_0014:  call       ""void C.M(in long)""
  IL_0019:  ldind.i4
  IL_001a:  call       ""void System.Console.WriteLine(int)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void RefReassignField()
        {
            var comp = CompileAndVerify(@"
using System;
struct S
{
    public int X;
    public S(int x) => X = x;
}
class C
{
    static void Main()
    {
        S s1 = new S(0);
        S s2 = new S(5);
        ref S rs = ref s1;
        Console.WriteLine(rs.X);
        rs.X++;
        Console.WriteLine(rs.X);
        Console.WriteLine((rs = ref s2).X++);
        rs.X++;
        Console.WriteLine(rs.X);
        Console.WriteLine(s1.X);
        Console.WriteLine(s2.X);
    }
}", expectedOutput: @"0
1
5
7
1
7");
            comp.VerifyIL("C.Main", @"
{
  // Code size      115 (0x73)
  .maxstack  4
  .locals init (S V_0, //s1
                S V_1, //s2
                int V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.5
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  ldloca.s   V_0
  IL_0012:  dup
  IL_0013:  ldfld      ""int S.X""
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  dup
  IL_001e:  ldflda     ""int S.X""
  IL_0023:  dup
  IL_0024:  ldind.i4
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stind.i4
  IL_0028:  ldfld      ""int S.X""
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ldloca.s   V_1
  IL_0034:  dup
  IL_0035:  ldflda     ""int S.X""
  IL_003a:  dup
  IL_003b:  ldind.i4
  IL_003c:  stloc.2
  IL_003d:  ldloc.2
  IL_003e:  ldc.i4.1
  IL_003f:  add
  IL_0040:  stind.i4
  IL_0041:  ldloc.2
  IL_0042:  call       ""void System.Console.WriteLine(int)""
  IL_0047:  dup
  IL_0048:  ldflda     ""int S.X""
  IL_004d:  dup
  IL_004e:  ldind.i4
  IL_004f:  ldc.i4.1
  IL_0050:  add
  IL_0051:  stind.i4
  IL_0052:  ldfld      ""int S.X""
  IL_0057:  call       ""void System.Console.WriteLine(int)""
  IL_005c:  ldloc.0
  IL_005d:  ldfld      ""int S.X""
  IL_0062:  call       ""void System.Console.WriteLine(int)""
  IL_0067:  ldloc.1
  IL_0068:  ldfld      ""int S.X""
  IL_006d:  call       ""void System.Console.WriteLine(int)""
  IL_0072:  ret
}");
        }

        [Fact]
        public void RefReassignFieldReadonly()
        {
            var comp = CompileAndVerify(@"
using System;
struct S
{
    public readonly int X;
    public S(int x) => X = x;
}
class C
{
    static void Main()
    {
        S s1 = new S(0);
        S s2 = new S(5);
        ref S rs = ref s1;
        Console.WriteLine(rs.X);
        rs = new S(rs.X + 1);
        Console.WriteLine(rs.X);
        Console.WriteLine((rs = ref s2).X);
        rs = new S(rs.X + 1);
        Console.WriteLine(rs.X);
        Console.WriteLine(s1.X);
        Console.WriteLine(s2.X);
    }
}", expectedOutput: @"0
1
5
6
1
6");
            comp.VerifyIL("C.Main", @"
{
  // Code size      127 (0x7f)
  .maxstack  3
  .locals init (S V_0, //s1
                S V_1, //s2
                S& V_2) //rs
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.5
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  ldloca.s   V_0
  IL_0012:  stloc.2
  IL_0013:  ldloc.2
  IL_0014:  ldfld      ""int S.X""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldloc.2
  IL_001f:  ldloc.2
  IL_0020:  ldfld      ""int S.X""
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  newobj     ""S..ctor(int)""
  IL_002c:  stobj      ""S""
  IL_0031:  ldloc.2
  IL_0032:  ldfld      ""int S.X""
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ldloca.s   V_1
  IL_003e:  dup
  IL_003f:  stloc.2
  IL_0040:  ldfld      ""int S.X""
  IL_0045:  call       ""void System.Console.WriteLine(int)""
  IL_004a:  ldloc.2
  IL_004b:  ldloc.2
  IL_004c:  ldfld      ""int S.X""
  IL_0051:  ldc.i4.1
  IL_0052:  add
  IL_0053:  newobj     ""S..ctor(int)""
  IL_0058:  stobj      ""S""
  IL_005d:  ldloc.2
  IL_005e:  ldfld      ""int S.X""
  IL_0063:  call       ""void System.Console.WriteLine(int)""
  IL_0068:  ldloc.0
  IL_0069:  ldfld      ""int S.X""
  IL_006e:  call       ""void System.Console.WriteLine(int)""
  IL_0073:  ldloc.1
  IL_0074:  ldfld      ""int S.X""
  IL_0079:  call       ""void System.Console.WriteLine(int)""
  IL_007e:  ret
}");
        }

        [Fact]
        public void RefReassignFieldRefReadonly()
        {
            var comp = CompileAndVerify(@"
using System;
struct S
{
    public int X;
    public S(int x) => X = x;
}
class C
{
    static void Main()
    {
        S s1 = new S(0);
        S s2 = new S(5);
        ref readonly S rs = ref s1;
        Console.WriteLine(rs.X);
        Console.WriteLine((rs = ref s2).X);
        Console.WriteLine(rs.X);
    }
}", expectedOutput: @"0
5
5");
            comp.VerifyIL("C.Main", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (S V_0, //s1
                S V_1, //s2
                S& V_2) //rs
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.5
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  ldloca.s   V_0
  IL_0012:  stloc.2
  IL_0013:  ldloc.2
  IL_0014:  ldfld      ""int S.X""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldloca.s   V_1
  IL_0020:  dup
  IL_0021:  stloc.2
  IL_0022:  ldfld      ""int S.X""
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ldloc.2
  IL_002d:  ldfld      ""int S.X""
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ret
}");
        }

        [Fact]
        public void RefReadonlyStackSchedule()
        {
            var comp = CompileAndVerify(@"
using System;
struct S
{
    public int X;
    public S(int x) => X = x;
    public void AddOne() => X++;
}
class C
{
    static void Main()
    {
        S s1 = new S(5);
        ref readonly S rs = ref s1;
        rs.AddOne();
        Console.WriteLine(s1.X);
    }
}", expectedOutput: "5");
        }

        [Fact]
        public void RefReassignCall()
        {
            var comp = CompileAndVerify(@"
using System;
struct S
{
    public int X;
    public S(int x) => X = x;
    public void AddOne() => X++;
}
class C
{
    static void Main()
    {
        S s1 = new S(0);
        S s2 = new S(5);
        ref S rs = ref s1;
        (rs = ref s2).AddOne();
        Console.WriteLine(rs.X);
        Console.WriteLine(s1.X);
        Console.WriteLine(s2.X);

        ref readonly S rs2 = ref s1;
        (rs2 = ref s2).AddOne();
        Console.WriteLine(rs.X);
        Console.WriteLine(s1.X);
        Console.WriteLine(s2.X);
    }
}"
, expectedOutput: @"6
0
6
6
0
6"
);
            comp.VerifyIL("C.Main", @"
{
  // Code size      110 (0x6e)
  .maxstack  3
  .locals init (S V_0, //s1
                S V_1, //s2
                S& V_2, //rs2
                S V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.5
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  ldloca.s   V_1
  IL_0012:  dup
  IL_0013:  call       ""void S.AddOne()""
  IL_0018:  dup
  IL_0019:  ldfld      ""int S.X""
  IL_001e:  call       ""void System.Console.WriteLine(int)""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int S.X""
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldloc.1
  IL_002f:  ldfld      ""int S.X""
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  ldloca.s   V_0
  IL_003b:  stloc.2
  IL_003c:  ldloca.s   V_1
  IL_003e:  dup
  IL_003f:  stloc.2
  IL_0040:  ldobj      ""S""
  IL_0045:  stloc.3
  IL_0046:  ldloca.s   V_3
  IL_0048:  call       ""void S.AddOne()""
  IL_004d:  ldfld      ""int S.X""
  IL_0052:  call       ""void System.Console.WriteLine(int)""
  IL_0057:  ldloc.0
  IL_0058:  ldfld      ""int S.X""
  IL_005d:  call       ""void System.Console.WriteLine(int)""
  IL_0062:  ldloc.1
  IL_0063:  ldfld      ""int S.X""
  IL_0068:  call       ""void System.Console.WriteLine(int)""
  IL_006d:  ret
}");
        }

        [Fact]
        public void RefReassignTernary()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 0;
        ref int r1 = ref x;
        Console.WriteLine(r1);
        int y = 2;
        int z = 3;
        (r1 = ref string.Empty.Length == 0
            ? ref y
            : ref z) = 4;
        Console.WriteLine(x);
        Console.WriteLine(r1);
        Console.WriteLine(y);
        Console.WriteLine(z);
    }
}", expectedOutput: @"0
0
4
4
3");
        }

        [Fact]
        public void RefReassignTernaryParam()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int x = 1, y = 2;
        ref int rx = ref x;
        M(ref (rx = ref (x == 0 ? ref x : ref y)));
        Console.WriteLine(rx);
        Console.WriteLine(x);
        Console.WriteLine(y);
    }

    static void M(ref int p)
    {
        Console.WriteLine(p++);
    }
}", expectedOutput: @"2
3
1
3");
            comp.VerifyIL("C.Main", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  brfalse.s  IL_000b
  IL_0007:  ldloca.s   V_1
  IL_0009:  br.s       IL_000d
  IL_000b:  ldloca.s   V_0
  IL_000d:  dup
  IL_000e:  call       ""void C.M(ref int)""
  IL_0013:  ldind.i4
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloc.0
  IL_001a:  call       ""void System.Console.WriteLine(int)""
  IL_001f:  ldloc.1
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ret
}");
        }

        [Fact]
        public void RefReassignFor()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int[] arr = new int[] { 1, 2, 3, 4, 5 };
        int i = 0;
        ref int r = ref arr[i]; 
        for (;i < 4; r = ref arr[++i])
        {
            Console.WriteLine(i);
            Console.WriteLine(r);
        }
    }
}", expectedOutput: @"0
1
1
2
2
3
3
4");
        }

        [Fact]
        public void AssignInMethodCall()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int a = 0;
        int b = 2;

        ref int r1 = ref a;
        ref int r2 = ref a;
        ref int r3 = ref a;

        M(ref r1 = ref r2 = ref r3 = ref b);
        Console.WriteLine(b);
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);
    }

    static void M(ref int p)
    {
        Console.WriteLine(p);
        p++;
        Console.WriteLine(p);
    }
}", expectedOutput: @"2
3
3
3
3
3");
        }

        [Fact]
        public void CompoundRefAssignIsLValue()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int a = 0;
        ref int r1 = ref a;
        ref int r2 = ref a;
        ref int r3 = ref a;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);

        int b = 2;
        (r1 = ref r2 = ref r3 = ref b) = 3;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);
    }
}", expectedOutput: @"0
0
0
3
3
3");
            comp.VerifyIL(@"C.Main", @"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (int V_0, //a
                int& V_1, //r2
                int& V_2, //r3
                int V_3) //b
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldloca.s   V_0
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_0
  IL_0009:  stloc.2
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ldloc.1
  IL_0011:  ldind.i4
  IL_0012:  call       ""void System.Console.WriteLine(int)""
  IL_0017:  ldloc.2
  IL_0018:  ldind.i4
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.2
  IL_001f:  stloc.3
  IL_0020:  ldloca.s   V_3
  IL_0022:  dup
  IL_0023:  stloc.2
  IL_0024:  dup
  IL_0025:  stloc.1
  IL_0026:  dup
  IL_0027:  ldc.i4.3
  IL_0028:  stind.i4
  IL_0029:  ldind.i4
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldloc.1
  IL_0030:  ldind.i4
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldloc.2
  IL_0037:  ldind.i4
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  ret
}");
        }

        [Fact]
        public void CompoundRefAssign()
        {
            var comp = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int a = 0;
        ref int r1 = ref a;
        ref int r2 = ref a;
        ref int r3 = ref a;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);

        int b = 2;
        r1 = ref r2 = ref r3 = ref b;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);
    }
}", expectedOutput: @"0
0
0
2
2
2");
            comp.VerifyIL("C.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (int V_0, //a
                int& V_1, //r2
                int& V_2, //r3
                int V_3) //b
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldloca.s   V_0
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_0
  IL_0009:  stloc.2
  IL_000a:  ldind.i4
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ldloc.1
  IL_0011:  ldind.i4
  IL_0012:  call       ""void System.Console.WriteLine(int)""
  IL_0017:  ldloc.2
  IL_0018:  ldind.i4
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ldc.i4.2
  IL_001f:  stloc.3
  IL_0020:  ldloca.s   V_3
  IL_0022:  dup
  IL_0023:  stloc.2
  IL_0024:  dup
  IL_0025:  stloc.1
  IL_0026:  ldind.i4
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ldloc.1
  IL_002d:  ldind.i4
  IL_002e:  call       ""void System.Console.WriteLine(int)""
  IL_0033:  ldloc.2
  IL_0034:  ldind.i4
  IL_0035:  call       ""void System.Console.WriteLine(int)""
  IL_003a:  ret
}");
        }

        [Fact]
        public void RefReassignIn()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static int x = 0;
    static int y = 0;

    static void Main()
    {
        M(in x);
    }

    static void M(in int rx)
    {
        Console.WriteLine(x);
        Console.WriteLine(rx);
        x++;
        Console.WriteLine(x);
        Console.WriteLine(rx);

        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
    }
}", expectedOutput: @"0
0
1
1
1
0
0
1
1
1");
        }

        [Fact]
        public void RefReassignOut()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static int x = 0;
    static int y = 0;

    static void Main()
    {
        int z;
        M(out z);
        Console.WriteLine(z);
    }

    static void M(out int rx)
    {
        rx = 0;
        rx = ref x;
        Console.WriteLine(x);
        rx++;
        Console.WriteLine(x);
        Console.WriteLine(rx);
        x++;
        Console.WriteLine(x);
        Console.WriteLine(rx);

        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
    }
}", expectedOutput: @"0
1
1
2
2
2
0
0
2
1
1
0");
        }

        [Fact]
        public void RefReassignRefParam()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static int x = 0;
    static int y = 0;

    static void Main()
    {
        M(ref x);
    }

    static void M(ref int rx)
    {
        Console.WriteLine(x);
        rx++;
        Console.WriteLine(x);
        Console.WriteLine(rx);
        x++;
        Console.WriteLine(x);
        Console.WriteLine(rx);

        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
    }
}", expectedOutput: @"0
1
1
2
2
2
0
0
2
1
1");

        }

        [Fact]
        public void RefReassignRefReadonlyLocal()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static void Main()
    {
        int x = 0;
        ref readonly int rx = ref x;
        Console.WriteLine(x);
        Console.WriteLine(rx);
        x++;
        Console.WriteLine(x);
        Console.WriteLine(rx);

        int y = 0;
        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
    }
}", expectedOutput: @"0
0
1
1
1
0
0
1
1
1
");
        }

        [Fact]
        public void RefReassignLocal()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static void Main()
    {
        int x = 0;
        ref int rx = ref x;
        Console.WriteLine(x);
        rx++;
        Console.WriteLine(x);
        Console.WriteLine(rx);
        x++;
        Console.WriteLine(x);
        Console.WriteLine(rx);

        int y = 0;
        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
    }
}", expectedOutput: @"0
1
1
2
2
2
0
0
2
1
1");
            comp.VerifyIL("C.Main", @"
{
  // Code size       91 (0x5b)
  .maxstack  4
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldloc.0
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  dup
  IL_000b:  dup
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  dup
  IL_0017:  ldind.i4
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.1
  IL_001f:  add
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ldind.i4
  IL_0028:  call       ""void System.Console.WriteLine(int)""
  IL_002d:  ldc.i4.0
  IL_002e:  stloc.1
  IL_002f:  ldloca.s   V_1
  IL_0031:  ldloc.0
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  ldloc.1
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  dup
  IL_003e:  ldind.i4
  IL_003f:  call       ""void System.Console.WriteLine(int)""
  IL_0044:  ldloc.1
  IL_0045:  ldc.i4.1
  IL_0046:  add
  IL_0047:  stloc.1
  IL_0048:  ldloc.0
  IL_0049:  call       ""void System.Console.WriteLine(int)""
  IL_004e:  ldloc.1
  IL_004f:  call       ""void System.Console.WriteLine(int)""
  IL_0054:  ldind.i4
  IL_0055:  call       ""void System.Console.WriteLine(int)""
  IL_005a:  ret
}");
        }

        [Fact]
        public void RefReadonlyReassign()
        {
            var comp = CompileAndVerify(@"
using System;

class C
{
    static void Main()
    {
        int x = 0;
        ref int rx = ref x;
        ref readonly int rrx = ref x;
        Console.WriteLine(x);
        Console.WriteLine(rx);
        Console.WriteLine(rrx);

        int y = 1;
        rx = ref y;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        Console.WriteLine(rrx);

        rrx = ref rx;
        y++;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(rx);
        Console.WriteLine(rrx);
    }
}", expectedOutput: @"0
0
0
0
1
1
0
0
2
2
2");
        }

        [Fact]
        public void RefAssignArrayAccess()
        {
            var text = @"
class Program
{
    static void M()
    {
        ref int rl = ref (new int[1])[0];
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""int""
  IL_0007:  ldc.i4.0
  IL_0008:  ldelema    ""int""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignRefParameter()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        ref int rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ret
}");
        }

        [Fact]
        public void RefAssignOutParameter()
        {
            var text = @"
class Program
{
    static void M(out int i)
    {
        i = 0;
        ref int rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(out int)", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stind.i4
  IL_0004:  ldarg.0
  IL_0005:  stloc.0
  IL_0006:  ret
}");
        }

        [Fact]
        public void RefAssignRefLocal()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        ref int local = ref i;
        ref int rl = ref local;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (int& V_0, //local
                int& V_1) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefAssignStaticProperty()
        {
            var text = @"
class Program
{
    static int field = 0;
    static ref int P { get { return ref field; } }

    static void M()
    {
        ref int rl = ref P;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails).VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  stloc.0
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceProperty()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int P { get { return ref field; } }

    void M()
    {
        ref int rl = ref P;
    }

    void M1()
    {
        ref int rl = ref new Program().P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.P.get""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceProperty()
        {
            var text = @"
struct Program
{
    public ref int P { get { return ref (new int[1])[0]; } }

    void M()
    {
        ref int rl = ref P;
    }

    void M1(ref Program program)
    {
        ref int rl = ref program.P;
    }
}

struct Program2
{
    Program program;


    Program2(Program program)
    {
        this.program = program;
    }

    void M()
    {
        ref int rl = ref program.P;
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1(ref Program)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceProperty()
        {
            var text = @"
interface I
{
    ref int P { get; }
}

class Program<T>
    where T : I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t.P;
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t.P;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceIndexer()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int this[int i] { get { return ref field; } }

    void M()
    {
        ref int rl = ref this[0];
    }

    void M1()
    {
        ref int rl = ref new Program()[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref int Program.this[int].get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceIndexer()
        {
            var text = @"
struct Program
{
    public ref int this[int i] { get { return ref (new int[1])[0]; } }

    void M()
    {
        ref int rl = ref this[0];
    }
}

struct Program2
{
    Program program;

    Program2(Program program)
    {
        this.program = program;
    }

    void M()
    {
        ref int rl = ref program[0];
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceIndexer()
        {
            var text = @"
interface I
{
    ref int this[int i] { get; }
}

class Program<T>
    where T : I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t[0];
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t[0];
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  ldc.i4.0
  IL_0008:  callvirt   ""ref int I.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ret
}");
        }

        [Fact]
        public void RefAssignStaticFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    static event D d;

    static void M()
    {
        ref D rl = ref d;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""D Program.d""
  IL_0006:  stloc.0
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d;

    void M()
    {
        ref D rl = ref d;
    }

    void M1()
    {
        ref D rl = ref new Program().d;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""D Program.d""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""D Program.d""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStaticField()
        {
            var text = @"
class Program
{
    static int i = 0;

    static void M()
    {
        ref int rl = ref i;
        rl = i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.i""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldsfld     ""int Program.i""
  IL_000d:  stind.i4
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceField()
        {
            var text = @"
class Program
{
    int i = 0;

    void M()
    {
        ref int rl = ref i;
    }

    void M1()
    {
        ref int rl = ref new Program().i;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""int Program.i""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""int Program.i""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceField()
        {
            var text = @"
struct Program
{
    public int i;
}

class Program2
{
    Program program = default(Program);

    void M(ref Program program)
    {
        ref int rl = ref program.i;
        rl = program.i;
    }

    void M()
    {
        ref int rl = ref program.i;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program2.M(ref Program)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldflda     ""int Program.i""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldarg.1
  IL_000a:  ldfld      ""int Program.i""
  IL_000f:  stind.i4
  IL_0010:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldflda     ""int Program.i""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignStaticCallWithoutArguments()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails).VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.M()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_000b
  IL_000b:  ldloc.1
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceCallWithoutArguments()
        {
            var text = @"
class Program
{
    ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }

    ref int M1()
    {
        ref int rl = ref new Program().M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.M()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  stloc.1
  IL_000e:  br.s       IL_0010
  IL_0010:  ldloc.1
  IL_0011:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceCallWithoutArguments()
        {
            var text = @"
struct Program
{
    public ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceCallWithoutArguments()
        {
            var text = @"
interface I
{
    ref int M();
}

class Program<T>
    where T : I
{
    T t = default(T);

    ref int M()
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M()
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.1
  IL_0018:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.1
  IL_0018:  ret
}");
        }

        [Fact]
        public void RefAssignStaticCallWithArguments()
        {
            var text = @"
class Program
{
    static ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails).VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  stloc.1
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.1
  IL_000f:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceCallWithArguments()
        {
            var text = @"
class Program
{
    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }

    ref int M1(ref int i, ref int j, object o)
    {
        ref int rl = ref new Program().M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
            comp.VerifyIL("Program.M1(ref int, ref int, object)", @"
{
  // Code size       21 (0x15)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.1
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.1
  IL_0014:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceCallWithArguments()
        {
            var text = @"
struct Program
{
    public ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
            comp.VerifyIL("Program2.M(ref int, ref int, object)", @"
{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.1
  IL_0015:  ret
}");
            comp.VerifyIL("Program3.M(ref int, ref int, object)", @"
{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.1
  IL_0015:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceCallWithArguments()
        {
            var text = @"
interface I
{
    ref int M(ref int i, ref int j, object o);
}

class Program<T>
    where T : I
{
    T t = default(T);

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails);
            comp.VerifyIL("Program<T>.M(ref int, ref int, object)", @"
{
  // Code size       28 (0x1c)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_001a
  IL_001a:  ldloc.1
  IL_001b:  ret
}");
            comp.VerifyIL("Program2<T>.M(T, ref int, ref int, object)", @"
{
  // Code size       23 (0x17)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  ldarg.s    V_4
  IL_000b:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0015
  IL_0015:  ldloc.1
  IL_0016:  ret
}");
            comp.VerifyIL("Program3<T>.M(ref int, ref int, object)", @"
{
  // Code size       28 (0x1c)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_001a
  IL_001a:  ldloc.1
  IL_001b:  ret
}");
        }

        [Fact]
        public void RefAssignDelegateInvocationWithNoArguments()
        {
            var text = @"
delegate ref int D();

class Program
{
    static void M(D d)
    {
        ref int rl = ref d();
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(D)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""ref int D.Invoke()""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
        }

        [Fact]
        public void RefAssignDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, ref int i, ref int j, object o)
    {
        ref int rl = ref d(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: Verification.Fails).VerifyIL("Program.M(D, ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  callvirt   ""ref int D.Invoke(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
        }

        [Fact]
        public void RefLocalsAreVariables()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void M(ref int i)
    {
    }

    static void N(out int i)
    {
        i = 0;
    }

    static unsafe void Main()
    {
        ref int rl = ref field;
        rl = 0;
        rl += 1;
        rl++;
        M(ref rl);
        N(out rl);
        fixed (int* i = &rl) { }
        var tr = __makeref(rl);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll, verify: Verification.Fails).VerifyIL("Program.Main()", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (int& V_0, //rl
                System.TypedReference V_1, //tr
                int* V_2, //i
                pinned int& V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stind.i4
  IL_000a:  ldloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ldloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  stind.i4
  IL_0016:  ldloc.0
  IL_0017:  call       ""void Program.M(ref int)""
  IL_001c:  nop
  IL_001d:  ldloc.0
  IL_001e:  call       ""void Program.N(out int)""
  IL_0023:  nop
  IL_0024:  ldloc.0
  IL_0025:  stloc.3
  IL_0026:  ldloc.3
  IL_0027:  conv.u
  IL_0028:  stloc.2
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ldc.i4.0
  IL_002c:  conv.u
  IL_002d:  stloc.3
  IL_002e:  ldloc.0
  IL_002f:  mkrefany   ""int""
  IL_0034:  stloc.1
  IL_0035:  ret
}");
        }

        [Fact]
        private void RefLocalsAreValues()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void N(int i)
    {
    }

    static unsafe int Main()
    {
        ref int rl = ref field;
        var @int = rl + 0;
        var @string = rl.ToString();
        var @long = (long)rl;
        N(rl);
        return unchecked((int)((long)@int + @long));
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll, verify: Verification.Passes).VerifyIL("Program.Main()", @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int& V_0, //rl
                int V_1, //int
                string V_2, //string
                long V_3, //long
                int V_4)
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldind.i4
  IL_0009:  stloc.1
  IL_000a:  ldloc.0
  IL_000b:  call       ""string int.ToString()""
  IL_0010:  stloc.2
  IL_0011:  ldloc.0
  IL_0012:  ldind.i4
  IL_0013:  conv.i8
  IL_0014:  stloc.3
  IL_0015:  ldloc.0
  IL_0016:  ldind.i4
  IL_0017:  call       ""void Program.N(int)""
  IL_001c:  nop
  IL_001d:  ldloc.1
  IL_001e:  conv.i8
  IL_001f:  ldloc.3
  IL_0020:  add
  IL_0021:  conv.i4
  IL_0022:  stloc.s    V_4
  IL_0024:  br.s       IL_0026
  IL_0026:  ldloc.s    V_4
  IL_0028:  ret
}
");
        }

        [Fact]
        public void RefLocal_CSharp6()
        {
            var text = @"
class Program
{
    static void M()
    {
        ref int rl = ref (new int[1])[0];
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int rl = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(6, 9),
                // (6,22): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int rl = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(6, 22)
                );
        }

        [Fact]
        public void RefVarSemanticModel()
        {
            var text = @"
class Program
{
    static void M()
    {
        int i = 0;
        ref var x = ref i;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("System.Int32 x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refVar = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refVar.Type;
            Assert.Equal("System.Int32", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(type));

            Assert.Null(model.GetSymbolInfo(refVar).Symbol);
            Assert.Null(model.GetTypeInfo(refVar).Type);
        }

        [Fact]
        public void RefAliasVarSemanticModel()
        {
            var text = @"
using var = C;
class C
{
    static void M()
    {
        C i = null;
        ref var x = ref i;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("C x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refVar = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refVar.Type;
            Assert.Equal("C", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            var alias = model.GetAliasInfo(type);
            Assert.Equal(SymbolKind.NamedType, alias.Target.Kind);
            Assert.Equal("C", alias.Target.ToDisplayString());

            Assert.Null(model.GetSymbolInfo(refVar).Symbol);
            Assert.Null(model.GetTypeInfo(refVar).Type);
        }

        [Fact]
        public void RefIntSemanticModel()
        {
            var text = @"
class Program
{
    static void M()
    {
        int i = 0;
        ref System.Int32 x = ref i;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("System.Int32 x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refInt = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refInt.Type;
            Assert.Equal("System.Int32", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(type));

            Assert.Null(model.GetSymbolInfo(refInt).Symbol);
            Assert.Null(model.GetTypeInfo(refInt).Type);
        }

        [WorkItem(17395, "https://github.com/dotnet/roslyn/issues/17453")]
        [Fact]
        public void Regression17395()
        {
            var source = @"
using System;

public class C 
{            
    public void F()
    {
        ref int[] a = ref {1,2,3};
        Console.WriteLine(a[0]);

        ref var b = ref {4, 5, 6};
        Console.WriteLine(b[0]);

        ref object c = ref {7,8,9};
        Console.WriteLine(c);
    }        
}
";

            var c = CreateCompilation(source);

            c.VerifyDiagnostics(
                // (8,27): error CS1510: A ref or out value must be an assignable variable
                //         ref int[] a = ref {1,2,3};
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "{1,2,3}"),
                // (11,17): error CS0820: Cannot initialize an implicitly-typed variable with an array initializer
                //         ref var b = ref {4, 5, 6};
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, "b = ref {4, 5, 6}").WithLocation(11, 17),
                // (14,28): error CS0622: Can only use array initializer expressions to assign to array types. Try using a new expression instead.
                //         ref object c = ref {7,8,9};
                Diagnostic(ErrorCode.ERR_ArrayInitToNonArrayType, "{7,8,9}").WithLocation(14, 28)
            );
        }

        [Fact, WorkItem(25264, "https://github.com/dotnet/roslyn/issues/25264"), CompilerTrait(CompilerFeature.IOperation)]
        public void TestNewRefArray()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        _ = /*<bind>*/ new ref[] { 1 } /*</bind>*/ ;
    }
}
";

            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new ref[] { 1 }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ?[]) (Syntax: '{ 1 }')
        Initializers(1):
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: '1')
              Children(2):
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '1')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ?[], IsInvalid, IsImplicit) (Syntax: 'ref[]')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,31): error CS1031: Type expected
                //         _ = /*<bind>*/ new ref[] { 1 } /*</bind>*/ ;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 31),
                // file.cs(6,28): error CS8382: Invalid object creation
                //         _ = /*<bind>*/ new ref[] { 1 } /*</bind>*/ ;
                Diagnostic(ErrorCode.ERR_InvalidObjectCreation, "ref[]").WithArguments("?[]").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(text, expectedOperationTree, expectedDiagnostics);
        }
    }
}
