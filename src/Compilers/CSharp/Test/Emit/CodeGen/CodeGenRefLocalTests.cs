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
    public class RefLocalTests : CompilingTestBase
    {
        [Fact]
        public void RefAssignArrayAccess()
        {
            var text = @"
class Program
{
    static void M()
    {
        ref int rl = ref (new int[1])[0];
        rl = ref  (new int[1])[0];
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""int""
  IL_0007:  ldc.i4.0
  IL_0008:  ldelema    ""int""
  IL_000d:  stloc.0
  IL_000e:  ldc.i4.1
  IL_000f:  newarr     ""int""
  IL_0014:  ldc.i4.0
  IL_0015:  ldelema    ""int""
  IL_001a:  stloc.0
  IL_001b:  ret
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
        rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  stloc.0
  IL_0005:  ret
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
        rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(out int)", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stind.i4
  IL_0004:  ldarg.0
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  stloc.0
  IL_0008:  ret
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
        rl = ref local;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (int& V_0, //local
                int& V_1) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ldloc.0
  IL_0006:  stloc.1
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefAssignRefAssign()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void M()
    {
        ref int a, b, c, d, e, f;
        a = ref b = ref c = ref d = ref e = ref f = ref field;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int& V_0, //a
                int& V_1, //b
                int& V_2, //c
                int& V_3, //d
                int& V_4, //e
                int& V_5) //f
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  dup
  IL_0007:  stloc.s    V_5
  IL_0009:  dup
  IL_000a:  stloc.s    V_4
  IL_000c:  dup
  IL_000d:  stloc.3
  IL_000e:  dup
  IL_000f:  stloc.2
  IL_0010:  dup
  IL_0011:  stloc.1
  IL_0012:  stloc.0
  IL_0013:  ret
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
        rl = ref P;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  stloc.0
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
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
        rl = ref P;
    }

    void M1()
    {
        ref int rl = ref new Program().P;
        rl = ref new Program().P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  call       ""ref int Program.P.get""
  IL_000e:  stloc.0
  IL_000f:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.P.get""
  IL_000b:  stloc.0
  IL_000c:  newobj     ""Program..ctor()""
  IL_0011:  call       ""ref int Program.P.get""
  IL_0016:  stloc.0
  IL_0017:  ret
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
        rl = ref P;
    }

    void M1(ref Program program)
    {
        ref int rl = ref program.P;
        rl = ref program.P;
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
        rl = ref program.P;
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program.P;
        rl = ref program.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  call       ""ref int Program.P.get""
  IL_000e:  stloc.0
  IL_000f:  ret
}");
            comp.VerifyIL("Program.M1(ref Program)", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ldarg.1
  IL_0009:  call       ""ref int Program.P.get""
  IL_000e:  stloc.0
  IL_000f:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Program Program2.program""
  IL_0013:  call       ""ref int Program.P.get""
  IL_0018:  stloc.0
  IL_0019:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Program Program3.program""
  IL_0013:  call       ""ref int Program.P.get""
  IL_0018:  stloc.0
  IL_0019:  ret
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
        rl = ref t.P;
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t.P;
        rl = ref t.P;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t.P;
        rl = ref t.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ldarg.0
  IL_0014:  ldflda     ""T Program<T>.t""
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""ref int I.P.get""
  IL_0024:  stloc.0
  IL_0025:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.P.get""
  IL_000c:  stloc.0
  IL_000d:  ldarg.1
  IL_000e:  box        ""T""
  IL_0013:  callvirt   ""ref int I.P.get""
  IL_0018:  stloc.0
  IL_0019:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ldarg.0
  IL_0014:  ldflda     ""T Program3<T>.t""
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""ref int I.P.get""
  IL_0024:  stloc.0
  IL_0025:  ret
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
        rl = ref this[0];
    }

    void M1()
    {
        ref int rl = ref new Program()[0];
        rl = ref new Program()[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""ref int Program.this[int].get""
  IL_0010:  stloc.0
  IL_0011:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref int Program.this[int].get""
  IL_000c:  stloc.0
  IL_000d:  newobj     ""Program..ctor()""
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""ref int Program.this[int].get""
  IL_0018:  stloc.0
  IL_0019:  ret
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
        rl = ref this[0];
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
        rl = ref program[0];
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program[0];
        rl = ref program[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""ref int Program.this[int].get""
  IL_0010:  stloc.0
  IL_0011:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ldarg.0
  IL_000f:  ldflda     ""Program Program2.program""
  IL_0014:  ldc.i4.0
  IL_0015:  call       ""ref int Program.this[int].get""
  IL_001a:  stloc.0
  IL_001b:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ldarg.0
  IL_000f:  ldflda     ""Program Program3.program""
  IL_0014:  ldc.i4.0
  IL_0015:  call       ""ref int Program.this[int].get""
  IL_001a:  stloc.0
  IL_001b:  ret
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
        rl = ref t[0];
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t[0];
        rl = ref t[0];
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t[0];
        rl = ref t[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""T Program<T>.t""
  IL_001a:  ldc.i4.0
  IL_001b:  constrained. ""T""
  IL_0021:  callvirt   ""ref int I.this[int].get""
  IL_0026:  stloc.0
  IL_0027:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  ldc.i4.0
  IL_0008:  callvirt   ""ref int I.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ldarg.1
  IL_000f:  box        ""T""
  IL_0014:  ldc.i4.0
  IL_0015:  callvirt   ""ref int I.this[int].get""
  IL_001a:  stloc.0
  IL_001b:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""T Program3<T>.t""
  IL_001a:  ldc.i4.0
  IL_001b:  constrained. ""T""
  IL_0021:  callvirt   ""ref int I.this[int].get""
  IL_0026:  stloc.0
  IL_0027:  ret
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
        rl = ref d;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""D Program.d""
  IL_0006:  stloc.0
  IL_0007:  ldsflda    ""D Program.d""
  IL_000c:  stloc.0
  IL_000d:  ret
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
        rl = ref d;
    }

    void M1()
    {
        ref D rl = ref new Program().d;
        rl = ref new Program().d;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""D Program.d""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  ldflda     ""D Program.d""
  IL_000e:  stloc.0
  IL_000f:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""D Program.d""
  IL_000b:  stloc.0
  IL_000c:  newobj     ""Program..ctor()""
  IL_0011:  ldflda     ""D Program.d""
  IL_0016:  stloc.0
  IL_0017:  ret
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
        rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.i""
  IL_0006:  stloc.0
  IL_0007:  ldsflda    ""int Program.i""
  IL_000c:  stloc.0
  IL_000d:  ret
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
        rl = ref i;
    }

    void M1()
    {
        ref int rl = ref new Program().i;
        rl = ref new Program().i;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""int Program.i""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  ldflda     ""int Program.i""
  IL_000e:  stloc.0
  IL_000f:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""int Program.i""
  IL_000b:  stloc.0
  IL_000c:  newobj     ""Program..ctor()""
  IL_0011:  ldflda     ""int Program.i""
  IL_0016:  stloc.0
  IL_0017:  ret
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
        rl = ref program.i;
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
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldflda     ""int Program.i""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Program Program2.program""
  IL_0013:  ldflda     ""int Program.i""
  IL_0018:  stloc.0
  IL_0019:  ret
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
        rl = ref M();
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.M()""
  IL_0006:  stloc.0
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
        public void RefAssignClassInstanceCallWithoutArguments()
        {
            var text = @"
class Program
{
    ref int M()
    {
        ref int rl = ref M();
        rl = ref M();
        return ref rl;
    }

    ref int M1()
    {
        ref int rl = ref new Program().M();
        rl = ref new Program().M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  call       ""ref int Program.M()""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.1
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.1
  IL_0014:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.M()""
  IL_000b:  stloc.0
  IL_000c:  newobj     ""Program..ctor()""
  IL_0011:  call       ""ref int Program.M()""
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_001b
  IL_001b:  ldloc.1
  IL_001c:  ret
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
        rl = ref M();
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        rl = ref program.M();
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        rl = ref program.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  call       ""ref int Program.M()""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.1
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.1
  IL_0014:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Program Program2.program""
  IL_0013:  call       ""ref int Program.M()""
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.1
  IL_001e:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Program Program3.program""
  IL_0013:  call       ""ref int Program.M()""
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.1
  IL_001e:  ret
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
        rl = ref t.M();
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        ref int rl = ref t.M();
        rl = ref t.M();
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
        rl = ref t.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldarg.0
  IL_0014:  ldflda     ""T Program<T>.t""
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""ref int I.M()""
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  stloc.1
  IL_0027:  br.s       IL_0029
  IL_0029:  ldloc.1
  IL_002a:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.M()""
  IL_000c:  stloc.0
  IL_000d:  ldarg.1
  IL_000e:  box        ""T""
  IL_0013:  callvirt   ""ref int I.M()""
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.1
  IL_001e:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldarg.0
  IL_0014:  ldflda     ""T Program3<T>.t""
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""ref int I.M()""
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  stloc.1
  IL_0027:  br.s       IL_0029
  IL_0029:  ldloc.1
  IL_002a:  ret
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0009:  stloc.0
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  ldarg.2
  IL_000d:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.1
  IL_0018:  ret
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }

    ref int M1(ref int i, ref int j, object o)
    {
        ref int rl = ref new Program().M(ref i, ref j, o);
        rl = ref new Program().M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp =  CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       27 (0x1b)
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
  IL_000b:  ldarg.0
  IL_000c:  ldarg.1
  IL_000d:  ldarg.2
  IL_000e:  ldarg.3
  IL_000f:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  stloc.1
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.1
  IL_001a:  ret
}");
            comp.VerifyIL("Program.M1(ref int, ref int, object)", @"
{
  // Code size       35 (0x23)
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
  IL_000f:  newobj     ""Program..ctor()""
  IL_0014:  ldarg.1
  IL_0015:  ldarg.2
  IL_0016:  ldarg.3
  IL_0017:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  stloc.1
  IL_001f:  br.s       IL_0021
  IL_0021:  ldloc.1
  IL_0022:  ret
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       27 (0x1b)
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
  IL_000b:  ldarg.0
  IL_000c:  ldarg.1
  IL_000d:  ldarg.2
  IL_000e:  ldarg.3
  IL_000f:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  stloc.1
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.1
  IL_001a:  ret
}");
            comp.VerifyIL("Program2.M(ref int, ref int, object)", @"
{
  // Code size       37 (0x25)
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
  IL_0010:  ldarg.0
  IL_0011:  ldflda     ""Program Program2.program""
  IL_0016:  ldarg.1
  IL_0017:  ldarg.2
  IL_0018:  ldarg.3
  IL_0019:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  stloc.1
  IL_0021:  br.s       IL_0023
  IL_0023:  ldloc.1
  IL_0024:  ret
}");
            comp.VerifyIL("Program3.M(ref int, ref int, object)", @"
{
  // Code size       37 (0x25)
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
  IL_0010:  ldarg.0
  IL_0011:  ldflda     ""Program Program3.program""
  IL_0016:  ldarg.1
  IL_0017:  ldarg.2
  IL_0018:  ldarg.3
  IL_0019:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  stloc.1
  IL_0021:  br.s       IL_0023
  IL_0023:  ldloc.1
  IL_0024:  ret
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
        rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        rl = ref t.M(ref i, ref j, o);
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
        rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program<T>.M(ref int, ref int, object)", @"
{
  // Code size       49 (0x31)
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
  IL_0016:  ldarg.0
  IL_0017:  ldflda     ""T Program<T>.t""
  IL_001c:  ldarg.1
  IL_001d:  ldarg.2
  IL_001e:  ldarg.3
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_002a:  stloc.0
  IL_002b:  ldloc.0
  IL_002c:  stloc.1
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.1
  IL_0030:  ret
}");
            comp.VerifyIL("Program2<T>.M(T, ref int, ref int, object)", @"
{
  // Code size       39 (0x27)
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
  IL_0011:  ldarg.1
  IL_0012:  box        ""T""
  IL_0017:  ldarg.2
  IL_0018:  ldarg.3
  IL_0019:  ldarg.s    V_4
  IL_001b:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  stloc.1
  IL_0023:  br.s       IL_0025
  IL_0025:  ldloc.1
  IL_0026:  ret
}");
            comp.VerifyIL("Program3<T>.M(ref int, ref int, object)", @"
{
  // Code size       49 (0x31)
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
  IL_0016:  ldarg.0
  IL_0017:  ldflda     ""T Program3<T>.t""
  IL_001c:  ldarg.1
  IL_001d:  ldarg.2
  IL_001e:  ldarg.3
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_002a:  stloc.0
  IL_002b:  ldloc.0
  IL_002c:  stloc.1
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.1
  IL_0030:  ret
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
        rl = ref d();
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(D)", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""ref int D.Invoke()""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  callvirt   ""ref int D.Invoke()""
  IL_000e:  stloc.0
  IL_000f:  ret
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
        rl = ref d(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M(D, ref int, ref int, object)", @"
{
  // Code size       27 (0x1b)
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
  IL_000b:  ldarg.0
  IL_000c:  ldarg.1
  IL_000d:  ldarg.2
  IL_000e:  ldarg.3
  IL_000f:  callvirt   ""ref int D.Invoke(ref int, ref int, object)""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  stloc.1
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.1
  IL_001a:  ret
}");
        }

        [Fact]
        public void RefAssignsAreVariables()
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

    static unsafe ref int Main()
    {
        ref int rl;
        (rl = ref field) = 0;
        (rl = ref field) += 1;
        (rl = ref field)++;
        M(ref (rl = ref field));
        N(out (rl = ref field));
        fixed (int* i = &(rl = ref field)) { }
        var tr = __makeref((rl = ref field));
        return ref (rl = ref field);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size      106 (0x6a)
  .maxstack  3
  .locals init (int& V_0, //rl
                System.TypedReference V_1, //tr
                int& V_2,
                int V_3,
                pinned int& V_4) //i
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stind.i4
  IL_000a:  ldsflda    ""int Program.field""
  IL_000f:  dup
  IL_0010:  stloc.0
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldloc.2
  IL_0014:  ldind.i4
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stind.i4
  IL_0018:  ldsflda    ""int Program.field""
  IL_001d:  dup
  IL_001e:  stloc.0
  IL_001f:  stloc.2
  IL_0020:  ldloc.2
  IL_0021:  ldloc.2
  IL_0022:  ldind.i4
  IL_0023:  stloc.3
  IL_0024:  ldloc.3
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stind.i4
  IL_0028:  ldsflda    ""int Program.field""
  IL_002d:  dup
  IL_002e:  stloc.0
  IL_002f:  call       ""void Program.M(ref int)""
  IL_0034:  nop
  IL_0035:  ldsflda    ""int Program.field""
  IL_003a:  dup
  IL_003b:  stloc.0
  IL_003c:  call       ""void Program.N(out int)""
  IL_0041:  nop
  IL_0042:  ldsflda    ""int Program.field""
  IL_0047:  dup
  IL_0048:  stloc.0
  IL_0049:  stloc.s    V_4
  IL_004b:  nop
  IL_004c:  nop
  IL_004d:  ldc.i4.0
  IL_004e:  conv.u
  IL_004f:  stloc.s    V_4
  IL_0051:  ldsflda    ""int Program.field""
  IL_0056:  dup
  IL_0057:  stloc.0
  IL_0058:  mkrefany   ""int""
  IL_005d:  stloc.1
  IL_005e:  ldsflda    ""int Program.field""
  IL_0063:  dup
  IL_0064:  stloc.0
  IL_0065:  stloc.2
  IL_0066:  br.s       IL_0068
  IL_0068:  ldloc.2
  IL_0069:  ret
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

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (int& V_0, //rl
                System.TypedReference V_1, //tr
                int V_2,
                pinned int& V_3) //i
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
  IL_0013:  stloc.2
  IL_0014:  ldloc.2
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stind.i4
  IL_0018:  ldloc.0
  IL_0019:  call       ""void Program.M(ref int)""
  IL_001e:  nop
  IL_001f:  ldloc.0
  IL_0020:  call       ""void Program.N(out int)""
  IL_0025:  nop
  IL_0026:  ldloc.0
  IL_0027:  stloc.3
  IL_0028:  nop
  IL_0029:  nop
  IL_002a:  ldc.i4.0
  IL_002b:  conv.u
  IL_002c:  stloc.3
  IL_002d:  ldloc.0
  IL_002e:  mkrefany   ""int""
  IL_0033:  stloc.1
  IL_0034:  ret
}");
        }

        [Fact]
        private void RefAssignsAreValues()
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
        ref int rl;
        var @int = (rl = ref field) + 0;
        var @string = (rl = ref field).ToString();
        var @long = (long)(rl = ref field);
        N((rl = ref field));
        return unchecked((int)((long)@int + @long));
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (int& V_0, //rl
                int V_1, //int
                string V_2, //string
                long V_3, //long
                int V_4)
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ldind.i4
  IL_0009:  ldc.i4.0
  IL_000a:  add
  IL_000b:  stloc.1
  IL_000c:  ldsflda    ""int Program.field""
  IL_0011:  dup
  IL_0012:  stloc.0
  IL_0013:  call       ""string int.ToString()""
  IL_0018:  stloc.2
  IL_0019:  ldsflda    ""int Program.field""
  IL_001e:  dup
  IL_001f:  stloc.0
  IL_0020:  ldind.i4
  IL_0021:  conv.i8
  IL_0022:  stloc.3
  IL_0023:  ldsflda    ""int Program.field""
  IL_0028:  dup
  IL_0029:  stloc.0
  IL_002a:  ldind.i4
  IL_002b:  call       ""void Program.N(int)""
  IL_0030:  nop
  IL_0031:  ldloc.1
  IL_0032:  conv.i8
  IL_0033:  ldloc.3
  IL_0034:  add
  IL_0035:  conv.i4
  IL_0036:  stloc.s    V_4
  IL_0038:  br.s       IL_003a
  IL_003a:  ldloc.s    V_4
  IL_003c:  ret
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

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size       43 (0x2b)
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
  IL_0009:  ldc.i4.0
  IL_000a:  add
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  call       ""string int.ToString()""
  IL_0012:  stloc.2
  IL_0013:  ldloc.0
  IL_0014:  ldind.i4
  IL_0015:  conv.i8
  IL_0016:  stloc.3
  IL_0017:  ldloc.0
  IL_0018:  ldind.i4
  IL_0019:  call       ""void Program.N(int)""
  IL_001e:  nop
  IL_001f:  ldloc.1
  IL_0020:  conv.i8
  IL_0021:  ldloc.3
  IL_0022:  add
  IL_0023:  conv.i4
  IL_0024:  stloc.s    V_4
  IL_0026:  br.s       IL_0028
  IL_0028:  ldloc.s    V_4
  IL_002a:  ret
}");
        }

        [Fact]
        public void RefAssignAllRelease()
        {
            var text = @"
using System;

delegate ref int D();

interface I
{
    ref int P { get; }
    ref int this[int i] { get; }
    ref int M();
}

struct S
{
    public int field;

    public S(int i)
    {
        field = i;
    }
}

class C : I
{
    static int sfield = 0;
    int field = 0;
    S s = new S();

    event D d;

    public ref int P { get { return ref field; } }
    public ref int this[int i] { get { return ref field; } }

    public ref int M()
    {
        return ref field;
    }

    public int M<T>(T t, ref int i, out int j)
        where T : I
    {
        ref D rd;
        ref int rl, rm = ref i;
        j = rm;
        Console.WriteLine(rm);
        rl = ref (new int[1])[0];
        Console.WriteLine(rl);
        rl = ref rm;
        Console.WriteLine(rl);
        rl = ref rm = ref i;
        Console.WriteLine(rl);
        rl = ref j;
        Console.WriteLine(rl);
        rl = ref P;
        Console.WriteLine(rl);
        rl = ref this[0];
        Console.WriteLine(rl);
        rl = ref M();
        Console.WriteLine(rl);
        rl = ref sfield;
        Console.WriteLine(rl);
        rl = ref field;
        Console.WriteLine(rl);
        rd = ref d;
        rd = new D(this.M);
        Console.WriteLine(rd == null);
        rl = ref rd();
        Console.WriteLine(rl);
        rl = ref s.field;
        Console.WriteLine(rl);
        rl = ref t.P;
        Console.WriteLine(rl);
        rl = ref t[0];
        Console.WriteLine(rl);
        rl = ref t.M();
        Console.WriteLine(rl);
        return rl;
    }

    public static int Main()
    {
        int i = 1;
        var c = new C();
        return c.M<C>(c, ref i, out i);
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.ReleaseExe, expectedOutput: @"
1
0
1
1
1
0
0
0
0
0
False
0
0
0
0
0
");
            comp.VerifyIL("C.M<T>(T, ref int, out int)", @"
{
  // Code size      235 (0xeb)
  .maxstack  4
  .locals init (int& V_0) //rm
  IL_0000:  ldarg.2
  IL_0001:  stloc.0
  IL_0002:  ldarg.3
  IL_0003:  ldloc.0
  IL_0004:  ldind.i4
  IL_0005:  stind.i4
  IL_0006:  ldloc.0
  IL_0007:  ldind.i4
  IL_0008:  call       ""void System.Console.WriteLine(int)""
  IL_000d:  ldc.i4.1
  IL_000e:  newarr     ""int""
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int""
  IL_0019:  ldind.i4
  IL_001a:  call       ""void System.Console.WriteLine(int)""
  IL_001f:  ldloc.0
  IL_0020:  ldind.i4
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  ldarg.2
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  ldind.i4
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldarg.3
  IL_0030:  ldind.i4
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldarg.0
  IL_0037:  call       ""ref int C.P.get""
  IL_003c:  ldind.i4
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.0
  IL_0044:  call       ""ref int C.this[int].get""
  IL_0049:  ldind.i4
  IL_004a:  call       ""void System.Console.WriteLine(int)""
  IL_004f:  ldarg.0
  IL_0050:  call       ""ref int C.M()""
  IL_0055:  ldind.i4
  IL_0056:  call       ""void System.Console.WriteLine(int)""
  IL_005b:  ldsflda    ""int C.sfield""
  IL_0060:  ldind.i4
  IL_0061:  call       ""void System.Console.WriteLine(int)""
  IL_0066:  ldarg.0
  IL_0067:  ldflda     ""int C.field""
  IL_006c:  ldind.i4
  IL_006d:  call       ""void System.Console.WriteLine(int)""
  IL_0072:  ldarg.0
  IL_0073:  ldflda     ""D C.d""
  IL_0078:  dup
  IL_0079:  ldarg.0
  IL_007a:  dup
  IL_007b:  ldvirtftn  ""ref int C.M()""
  IL_0081:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0086:  stind.ref
  IL_0087:  dup
  IL_0088:  ldind.ref
  IL_0089:  ldnull
  IL_008a:  ceq
  IL_008c:  call       ""void System.Console.WriteLine(bool)""
  IL_0091:  ldind.ref
  IL_0092:  callvirt   ""ref int D.Invoke()""
  IL_0097:  ldind.i4
  IL_0098:  call       ""void System.Console.WriteLine(int)""
  IL_009d:  ldarg.0
  IL_009e:  ldflda     ""S C.s""
  IL_00a3:  ldflda     ""int S.field""
  IL_00a8:  ldind.i4
  IL_00a9:  call       ""void System.Console.WriteLine(int)""
  IL_00ae:  ldarga.s   V_1
  IL_00b0:  constrained. ""T""
  IL_00b6:  callvirt   ""ref int I.P.get""
  IL_00bb:  ldind.i4
  IL_00bc:  call       ""void System.Console.WriteLine(int)""
  IL_00c1:  ldarga.s   V_1
  IL_00c3:  ldc.i4.0
  IL_00c4:  constrained. ""T""
  IL_00ca:  callvirt   ""ref int I.this[int].get""
  IL_00cf:  ldind.i4
  IL_00d0:  call       ""void System.Console.WriteLine(int)""
  IL_00d5:  ldarga.s   V_1
  IL_00d7:  constrained. ""T""
  IL_00dd:  callvirt   ""ref int I.M()""
  IL_00e2:  dup
  IL_00e3:  ldind.i4
  IL_00e4:  call       ""void System.Console.WriteLine(int)""
  IL_00e9:  ldind.i4
  IL_00ea:  ret
}");
        }
    }
}
